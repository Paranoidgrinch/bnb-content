using BnbContent.Converter;
using BnbContent.Converter.Elites;
using RogueDeck.Core.Combat;
using RogueDeck.Sandbox.Composition;
using RogueDeck.Sandbox.Run;

namespace BnbContent.Tests;

// The Act-II content cut of 2026-10-29: the new Testator, the reworked Drawer and the Codex's desk bell.
public class ActTwoCutTests
{
    private static CombatantState Body(RunPlayback play, CombatantId id) =>
        play.CombatDriver!.Current!.State.GetCombatant(id);

    private static CombatantState Hero(RunPlayback play) =>
        play.CombatDriver!.Current!.State.GetCombatant(play.CombatDriver.Current.HeroId);

    private static int BlockOf(CombatantState body) =>
        body.DefensivePools.TryGetValue(StandardCombatIds.BlockDefensivePool, out var pool) ? pool.Current : 0;

    private static CombatantId Find(RunPlayback play, string enemyId) =>
        play.CombatDriver!.Current!.State.Combatants.First(c => c.Id.value.StartsWith(enemyId, StringComparison.Ordinal)).Id;

    private static void Play(RunPlayback play, InteractiveRunSession session, CardInstance card, CombatantId? target)
    {
        play.CombatDriver!.PlayCard(card.Id, target);
        Assert.Null(session.Error);
    }

    // ── The Testator ──────────────────────────────────────────────────────────────────────────────────────

    // It rewrites its testament at the end of each of its turns: to its heirs, to you, of its debts.
    [Fact]
    public void The_testator_rewrites_its_testament_every_turn()
    {
        var (play, session, testator) = FightProbe.Start(
            FightProbe.Roster("testament", energy: 9, ("the_testator", "witness_the_will", 70)),
            Enumerable.Repeat("cower_behind_a_desk", 10).ToList(), health: 400);

        var seen = new List<string>();
        for (var turn = 0; turn < 4; turn++)
        {
            seen.Add(Testator.Testaments.Single(t => FightProbe.StacksOf(Body(play, testator), t) > 0));
            play.CombatDriver!.EndTurn();
            Assert.Null(session.Error);
        }
        Assert.Equal([Testator.HeirsId, Testator.ClaimantId, Testator.DebtsId, Testator.HeirsId], seen);
        play.Dispose();
    }

    // Killed while the testament favours its heirs, the enemy beside it inherits.
    [Fact]
    public void Killing_the_testator_carries_out_the_testament_standing_then()
    {
        var (play, session, _) = FightProbe.Start(
            FightProbe.Roster("probate", energy: 9,
                ("the_testator", "witness_the_will", 6), ("brass_maw_of_returns", "brass_shutter", 50)),
            Enumerable.Repeat("paper_cut", 10).ToList(), health: 400);
        var testator = Find(play, "the_testator");
        var maw = Find(play, "brass_maw_of_returns");

        Play(play, session, play.CombatDriver!.Current!.Hand.First(c => c.DefinitionId.value == "paper_cut"), testator);
        Assert.False(Body(play, testator).IsAlive);
        Assert.Equal(Testator.HeirsStrength, FightProbe.StacksOf(Body(play, maw), "strength"));
        play.Dispose();
    }

    // …and on its second turn the testament is yours: Block and two cards.
    [Fact]
    public void A_testament_to_the_claimant_pays_you()
    {
        var (play, session, _) = FightProbe.Start(
            FightProbe.Roster("claimant", energy: 9,
                ("the_testator", "witness_the_will", 6), ("brass_maw_of_returns", "brass_shutter", 50)),
            Enumerable.Repeat("paper_cut", 10).ToList(), health: 400);
        var testator = Find(play, "the_testator");
        play.CombatDriver!.EndTurn();
        Assert.Null(session.Error);
        Assert.Equal(1, FightProbe.StacksOf(Body(play, testator), Testator.ClaimantId));

        // Its Block first (it witnessed the will on its turn), then the killing cut.
        while (BlockOf(Body(play, testator)) > 0)
            Play(play, session, play.CombatDriver.Current!.Hand.First(c => c.DefinitionId.value == "paper_cut"), testator);
        var hand = play.CombatDriver.Current!.Hand.Count;
        var block = BlockOf(Hero(play));
        Play(play, session, play.CombatDriver.Current!.Hand.First(c => c.DefinitionId.value == "paper_cut"), testator);
        Assert.False(Body(play, testator).IsAlive);
        Assert.Equal(block + Testator.ClaimantBlock, BlockOf(Hero(play)));
        Assert.Equal(hand - 1 + Testator.ClaimantCards, play.CombatDriver.Current!.Hand.Count);
        play.Dispose();
    }

    // ── The Drawer of Infinite Returns ────────────────────────────────────────────────────────────────────

    // From the second round one card is demanded each turn. Played, it is fed to the drawer, which stands open and takes double damage.
    [Fact]
    public void Feeding_the_drawer_opens_it_to_double_damage()
    {
        var (play, session, drawer) = FightProbe.Start(
            FightProbe.Solo(DrawerOfInfiniteReturns.EnemyId, "open_another_drawer", energy: 9),
            Enumerable.Repeat("paper_cut", 10).ToList(), health: 400);

        play.CombatDriver!.EndTurn(); // the demands begin with the second round
        Assert.Null(session.Error);
        var demanded = play.CombatDriver!.Current!.Hand.Where(c => c.HasMark(new TagId(DrawerOfInfiniteReturns.DemandedMark))).ToList();
        Assert.Single(demanded);
        Play(play, session, demanded[0], drawer);
        Assert.True(FightProbe.StacksOf(Body(play, drawer), DrawerOfInfiniteReturns.DrawerOpenId) > 0);
        var exhausted = play.CombatDriver.Current!.State.GetCardZones(play.CombatDriver.Current.HeroId)
            .GetCardsInZone(CardZone.ExhaustPile);
        Assert.Contains(exhausted, c => c.HasMark(new TagId(DrawerOfInfiniteReturns.FedMark)));

        // A Paper Cut now lands as 12: what its Block does not take comes off its health.
        var before = Body(play, drawer).Health.Current + BlockOf(Body(play, drawer));
        Play(play, session, play.CombatDriver.Current!.Hand.First(c => c.DefinitionId.value == "paper_cut"), drawer);
        Assert.Equal(before - 12, Body(play, drawer).Health.Current + BlockOf(Body(play, drawer)));
        play.Dispose();
    }

    // Keeping the demanded card costs an Overdue owed to the Drawer.
    [Fact]
    public void Refusing_the_drawer_files_an_overdue()
    {
        var (play, session, _) = FightProbe.Start(
            FightProbe.Solo(DrawerOfInfiniteReturns.EnemyId, "index_the_contents", energy: 9),
            Enumerable.Repeat("paper_cut", 10).ToList(), health: 400);
        play.CombatDriver!.EndTurn();
        Assert.Equal(0, FightProbe.StacksOf(Hero(play), ActTwo.OverdueId)); // nothing demanded in round one
        play.CombatDriver.EndTurn();
        Assert.Null(session.Error);
        Assert.True(FightProbe.StacksOf(Hero(play), ActTwo.OverdueId) >= 1);
        play.Dispose();
    }

    // ── The Checkout Codex ────────────────────────────────────────────────────────────────────────────────

    // Ask at the Desk gets the held card back at once — and that card costs 1 more for it.
    [Fact]
    public void Asking_at_the_desk_returns_the_card_at_a_price()
    {
        var (play, session, codex) = FightProbe.Start(
            FightProbe.Solo("checkout_codex", "restricted_binding", energy: 9),
            ["paper_cut", "paper_cut", "paper_cut", "paper_cut", "paper_cut", "paper_cut"], health: 400);

        var ask = play.CombatDriver!.Current!.Hand.First(c => c.DefinitionId.value == ActTwo.AskAtTheDeskCardId);
        var hand = play.CombatDriver.Current.Hand.Count;
        Play(play, session, ask, null);
        // The Ask card is gone and the held card is back: the hand is as large as before.
        Assert.Equal(hand, play.CombatDriver.Current!.Hand.Count);
        var energy = Hero(play).Resources[StandardCombatIds.EnergyResource].Current;
        var returned = play.CombatDriver.Current.Hand.First(c => c.MarkCounters.GetValueOrDefault(StandardCombatIds.CardCostDeltaCounter) == 1);
        Play(play, session, returned, codex);
        Assert.Equal(energy - 2, Hero(play).Resources[StandardCombatIds.EnergyResource].Current);
        play.Dispose();
    }
}
