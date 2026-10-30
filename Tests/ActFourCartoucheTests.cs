using BnbContent.Converter;
using BnbContent.Converter.Cards;
using RogueDeck.Core.Combat;
using RogueDeck.Sandbox.Composition;
using RogueDeck.Sandbox.Run;

namespace BnbContent.Tests;

// Act IV, Stage 15 — The Cartouche Chambers, proved in live fights.
//
// Two bodies decide what a blessing of yours is for. The chisel decides it was a mistake in the stone and
// strikes it out before it happens; the wall decides it was always ancestral property and enters it in the
// lineage. The tests follow the distinction the whole stage rests on — ERASED is not the same as taken away,
// and what the wall takes is the fact of a blessing, never its stacks.
public class ActFourCartoucheTests
{
    private const string Wax = "waxen_surety";  // Working, 1: gain 7 Ward Wax

    private static readonly BabData Data = BabData.Load(TestData.Directory);

    private static CombatantState Hero(RunPlayback play) =>
        play.CombatDriver!.Current!.State.GetCombatant(play.CombatDriver.Current!.HeroId);

    private static IReadOnlyList<CombatantState> Enemies(RunPlayback play) =>
        [.. play.CombatDriver!.Current!.State.Combatants.Where(c => c.Id != play.CombatDriver.Current!.HeroId)];

    private static CombatantState Body(RunPlayback play, string enemyId) =>
        Enemies(play).First(c => c.DefinitionId.value.Contains(enemyId, StringComparison.Ordinal));

    private static void Play(RunPlayback play, InteractiveRunSession session, string cardId)
    {
        var card = play.CombatDriver!.Current!.Hand.First(c => c.DefinitionId.value == cardId);
        play.CombatDriver.PlayCard(card.Id, null);
        Assert.True(session.Error is null, session.Error);
    }

    private static IReadOnlyList<string> WaxDeck => [.. Enumerable.Repeat(Wax, 12)];

    private static int BlockOf(CombatantState combatant) =>
        combatant.DefensivePools.TryGetValue(StandardCombatIds.BlockDefensivePool, out var pool)
            ? pool.Current
            : 0;

    // ── the Name-Erasing Chisel Spirit ────────────────────────────────────────────────────────────────────

    // The chisel is set from the first bell. A round top-up alone would miss the opening round — a fight's
    // first round starts before its bodies are dressed — so the spirit serves it with the fight.
    [Fact]
    public void The_chisel_is_set_against_your_name_from_the_first_bell()
    {
        var (chamber, _, _) = FightProbe.Start(FightProbe.Authored("labyrinth_cartouche_01"), health: 400);
        Assert.Equal(1, FightProbe.StacksOf(Hero(chamber), ActFour.ChiselSetId));
        chamber.Dispose();

        var (wall, _, _) = FightProbe.Start(FightProbe.Authored("labyrinth_fixed_days_02"), health: 400);
        Assert.Equal(0, FightProbe.StacksOf(Hero(wall), ActFour.ChiselSetId));
        wall.Dispose();
    }

    // ERASED, not removed: the blessing never lands at all, and a doubt is cut where it would have been. The
    // second blessing of the same round is untouched — the chisel is one cut a round.
    [Fact]
    public void The_first_blessing_of_a_round_is_never_gained_and_the_second_is()
    {
        var (play, session, _) = FightProbe.Start(
            FightProbe.Solo("name_erasing_chisel_spirit", "stone_dust"), deck: WaxDeck, health: 800);

        Play(play, session, Wax);
        Assert.Equal(0, FightProbe.StacksOf(Hero(play), Keywords.WardWax));
        Assert.Equal(1, FightProbe.StacksOf(Hero(play), Keywords.Doubt));
        Assert.Equal(0, FightProbe.StacksOf(Hero(play), ActFour.ChiselSetId));

        Play(play, session, Wax);
        Assert.Equal(7, FightProbe.StacksOf(Hero(play), Keywords.WardWax));
        Assert.Equal(1, FightProbe.StacksOf(Hero(play), Keywords.Doubt));
        play.Dispose();
    }

    // …and it is set again when the round turns. One cut a round, every round the spirit is standing.
    [Fact]
    public void The_chisel_is_set_again_each_round()
    {
        var (play, session, _) = FightProbe.Start(
            FightProbe.Solo("name_erasing_chisel_spirit", "stone_dust"), deck: WaxDeck, health: 800);

        Play(play, session, Wax);              // erased
        play.CombatDriver!.EndTurn();          // the round turns
        Assert.Equal(1, FightProbe.StacksOf(Hero(play), ActFour.ChiselSetId));

        Play(play, session, Wax);
        Assert.Equal(0, FightProbe.StacksOf(Hero(play), Keywords.WardWax));
        Assert.Equal(2, FightProbe.StacksOf(Hero(play), Keywords.Doubt));
        play.Dispose();
    }

}
