using BnbContent.Converter;
using BnbContent.Converter.Relics;
using RogueDeck.Core.Combat;
using RogueDeck.Run;
using RogueDeck.Sandbox.Composition;
using RogueDeck.Sandbox.Run;

namespace BnbContent.Tests;

// The five Act-IV Event relics in real fights — the ones the Labyrinth's first ten doors hand over.
//
// One live fight per relic, for the same reason the boss relics get one: a relic that does nothing installs,
// validates and is quietly played without. Each is measured against the SAME fight without it wherever the
// answer is a number a fight could have produced anyway (a card in hand, a point of Block).
public class ActFourEventRelicTests
{
    private const string Quiet = "ordinance_tablet";
    private const string QuietIntent = "stone_precedent";   // guards for 10, hits for nothing
    private const string Scribe = "fourfold_vessel_guardian";
    private const string NameOffice = "name_office";        // 11 damage and an Inscribed
    private const string Deed = "paper_cut";                // Deed, 1 Energy, 6 damage

    // ── Cup of the Lowest Mark ────────────────────────────────────────────────────────────────────────────

    [Fact]
    public void The_cup_fills_on_the_turn_you_very_nearly_spent()
    {
        var (play, session, target) = WithRelic(ActFourEventRelicRules.CupId, startingHealth: 100);

        Play(play, session, Deed, target);
        Play(play, session, Deed, target);      // two of three spent: the lowest mark
        Assert.Equal(1, Energy(Hero(play)));

        play.CombatDriver!.EndTurn();
        Assert.Null(session.Error);
        Assert.Equal(104, Hero(play).Health.Current);

        // …and the card it promised, at the next hand rather than into a hand about to be discarded.
        var withCup = play.CombatDriver!.Current!.Hand.Count;
        play.Dispose();
        Assert.Equal(withCup - 1, SecondHand(relicId: null));
    }

    [Fact]
    public void The_cup_stays_dry_on_a_turn_that_spent_everything()
    {
        var (play, session, target) = WithRelic(ActFourEventRelicRules.CupId, startingHealth: 100);

        for (var i = 0; i < 3; i++)
            Play(play, session, Deed, target);
        play.CombatDriver!.EndTurn();
        Assert.Null(session.Error);

        Assert.Equal(100, Hero(play).Health.Current);
        play.Dispose();
    }

    // ── Red Linen Knot ────────────────────────────────────────────────────────────────────────────────────

    [Fact]
    public void The_knot_opens_the_fight_wrapped_and_wraps_again_when_the_linen_holds()
    {
        var (play, session, _) = WithRelic(
            ActFourEventRelicRules.KnotId, heroStatuses: [("panic", 1)]);

        Assert.Equal(ActFourEventRelicRules.KnotBlock, Block(Hero(play)));
        Assert.Equal(1, FightProbe.StacksOf(Hero(play), ActFour.EmbalmedId));

        // Panic fades at the bearer's turn end — and does not, because the linen is holding it.
        play.CombatDriver!.EndTurn();
        Assert.Null(session.Error);

        Assert.Equal(1, FightProbe.StacksOf(Hero(play), "panic"));
        Assert.Equal(0, FightProbe.StacksOf(Hero(play), ActFour.EmbalmedId));
        Assert.Equal(ActFourEventRelicRules.KnotBlock, Block(Hero(play)));
        play.Dispose();
    }

    // ── Blank Cartouche ───────────────────────────────────────────────────────────────────────────────────

    [Fact]
    public void The_blank_cartouche_deals_one_more_and_holds_no_name()
    {
        var (play, session, _) = WithRelic(
            ActFourEventRelicRules.CartoucheId, enemy: Scribe, intent: NameOffice);

        var opening = play.CombatDriver!.Current!.Hand.Count;

        play.CombatDriver.EndTurn();            // the office writes: 11 damage and an Inscribed
        Assert.Null(session.Error);
        Assert.Equal(0, FightProbe.StacksOf(Hero(play), ActFour.InscribedId));
        play.Dispose();

        var (bare, bareSession, _) = WithRelic(null, enemy: Scribe, intent: NameOffice);
        Assert.Equal(opening - 1, bare.CombatDriver!.Current!.Hand.Count);
        bare.CombatDriver.EndTurn();
        Assert.Null(bareSession.Error);
        Assert.Equal(1, FightProbe.StacksOf(Hero(bare), ActFour.InscribedId));
        bare.Dispose();
    }

    // ── Jar of Borrowed Breath ────────────────────────────────────────────────────────────────────────────

    [Fact]
    public void The_jar_gives_the_breath_back_when_an_affliction_leaves()
    {
        var (play, session, _) = WithRelic(
            ActFourEventRelicRules.JarId, heroStatuses: [("panic", 1)], startingHealth: 100);

        play.CombatDriver!.EndTurn();           // the last stack of Panic goes, and with it the Panic
        Assert.Null(session.Error);

        Assert.Equal(0, FightProbe.StacksOf(Hero(play), "panic"));
        Assert.Equal(103, Hero(play).Health.Current);
        var withJar = play.CombatDriver!.Current!.Hand.Count;
        play.Dispose();

        var (bare, bareSession, _) = WithRelic(null, heroStatuses: [("panic", 1)], startingHealth: 100);
        bare.CombatDriver!.EndTurn();
        Assert.Null(bareSession.Error);
        Assert.Equal(100, Hero(bare).Health.Current);
        Assert.Equal(withJar - 1, bare.CombatDriver!.Current!.Hand.Count);
        bare.Dispose();
    }

    // ── Broken Royal Weight ───────────────────────────────────────────────────────────────────────────────

    [Fact]
    public void The_broken_weight_takes_the_first_missed_measure_and_is_heavier_for_it()
    {
        var (play, session, _) = WithRelic(
            ActFourEventRelicRules.WeightId, heroStatuses: [(ActFour.WeighedId, 3)]);

        Assert.Equal(ActFourEventRelicRules.WeightBlock, Block(Hero(play)));

        play.CombatDriver!.EndTurn();           // nothing spent against a measure of three
        Assert.Null(session.Error);

        Assert.Equal(1, Hero(play).Counters[ActFour.MeasuresFailed]);
        Assert.Equal(ActFourEventRelicRules.WeightBlock, Block(Hero(play)));
        Assert.Equal(1, FightProbe.StacksOf(Hero(play), ActFour.BurdenedId));
        play.Dispose();
    }

    // ── the shared shapes ─────────────────────────────────────────────────────────────────────────────────

    // The hand the second turn deals, with the relic or without it — the control every "draw one more" is
    // read against.
    private static int SecondHand(string? relicId)
    {
        var (play, session, target) = WithRelic(relicId, startingHealth: 100);
        Play(play, session, Deed, target);
        Play(play, session, Deed, target);
        play.CombatDriver!.EndTurn();
        Assert.Null(session.Error);
        var hand = play.CombatDriver!.Current!.Hand.Count;
        play.Dispose();
        return hand;
    }

    private static CombatantState Hero(RunPlayback play) =>
        play.CombatDriver!.Current!.State.GetCombatant(play.CombatDriver.Current!.HeroId);

    private static int Block(CombatantState c) =>
        c.DefensivePools.TryGetValue(StandardCombatIds.BlockDefensivePool, out var pool) ? pool.Current : 0;

    private static int Energy(CombatantState c) =>
        c.Resources[StandardCombatIds.EnergyResource].Current;

    private static void Play(RunPlayback play, InteractiveRunSession session, string cardId, CombatantId target)
    {
        var card = play.CombatDriver!.Current!.Hand.First(c => c.DefinitionId.value == cardId);
        play.CombatDriver.PlayCard(card.Id, target);
        Assert.Null(session.Error);
    }

    private static (RunPlayback Play, InteractiveRunSession Session, CombatantId EnemyId) WithRelic(
        string? relicId, IReadOnlyList<string>? deck = null, int energy = 3, string enemy = Quiet,
        string intent = QuietIntent, int startingHealth = 400, int maxHealth = 400,
        params (string Status, int Stacks)[] heroStatuses)
    {
        var probe = heroStatuses.Length == 0
            ? FightProbe.Solo(enemy, intent, energy)
            : FightProbe.SoloAgainstHero(enemy, intent, energy, heroStatuses);
        var blueprint = FightProbe.OneFight(probe, deck ?? [.. Enumerable.Repeat(Deed, 12)]);
        blueprint = blueprint with
        {
            Start = blueprint.Start with
            {
                StartingRelics = relicId is null
                    ? blueprint.Start.StartingRelics
                    : [.. blueprint.Start.StartingRelics, relicId],
                MaxHealth = maxHealth,
                StartingHealth = startingHealth,
            },
            Characters = [],
        };

        var play = new RunPlayback(() => { });
        play.Start(blueprint, seed: 1, interactive: true);
        Assert.True(play.Error is null, play.Error);
        var session = play.Session!;
        while (session.IsAwaitingInterlude)
            session.Continue();
        Assert.True(session.Error is null, session.Error);

        var combat = play.CombatDriver!.Current!;
        return (play, session, combat.State.Combatants.First(c => c.Id != combat.HeroId).Id);
    }
}
