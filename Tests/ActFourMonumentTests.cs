using BnbContent.Converter;
using BnbContent.Converter.Cards;
using RogueDeck.Core.Combat;
using RogueDeck.Sandbox.Composition;
using RogueDeck.Sandbox.Run;

namespace BnbContent.Tests;

// Act IV, Stages 7 and 8 — The Monument Works and The Hall of Reed and Ink, proved in live fights.
//
// Two stages of one idea: the building remembers, and so does the ink. The tests are about what each body
// records — a stone climbing where the player can count it, a foundation keeping score of compliance, a
// palette that is only fresh once a round, and a wall that is fed by nothing lapsing.
public class ActFourMonumentTests
{
    private const string OneCost = "paper_cut";  // Deed, 1

    private static CombatantState Hero(RunPlayback play) =>
        play.CombatDriver!.Current!.State.GetCombatant(play.CombatDriver.Current!.HeroId);

    private static IReadOnlyList<CombatantState> Enemies(RunPlayback play) =>
        [.. play.CombatDriver!.Current!.State.Combatants.Where(c => c.Id != play.CombatDriver.Current!.HeroId)];

    private static CombatantState Body(RunPlayback play, string enemyId) =>
        Enemies(play).First(c => c.DefinitionId.value.Contains(enemyId, StringComparison.Ordinal));

    private static void Play(RunPlayback play, InteractiveRunSession session, string cardId, CombatantId? target)
    {
        var card = play.CombatDriver!.Current!.Hand.First(c => c.DefinitionId.value == cardId);
        play.CombatDriver.PlayCard(card.Id, target);
        Assert.True(session.Error is null, session.Error);
    }

    // ── the Fallen Capstone Golem ─────────────────────────────────────────────────────────────────────────

    // The installation climbs where the player can count it, and at three the stone comes down — then the
    // placement starts again, because a capstone that has already fallen can always be installed once more.
    [Fact]
    public void The_stone_climbs_where_it_can_be_counted_and_then_falls()
    {
        var (play, _, _) = FightProbe.Start(
            FightProbe.Authored("labyrinth_monument_01"), health: 800);

        var placement = new List<int>();
        for (var turn = 0; turn < 4; turn++)
        {
            play.CombatDriver!.EndTurn();
            placement.Add(FightProbe.StacksOf(Enemies(play)[0], ActFour.PlacementId));
        }

        // 1, 2, 3 — and the fourth turn is the drop, which spends the whole placement.
        Assert.Equal([1, 2, 3, 0], placement);
        play.Dispose();
    }

    // …and what the stone weighs is the burial the player is already carrying.
    [Fact]
    public void The_capstone_falls_as_hard_as_the_burial_beneath_it()
    {
        int Falls(int entombed)
        {
            var (play, _, _) = FightProbe.Start(
                FightProbe.SoloAgainstHero("fallen_capstone_golem", "set_the_capstone", energy: 3,
                    entombed > 0 ? [(ActFour.EntombedId, entombed)] : []),
                health: 800);

            var before = Hero(play).Health.Current;
            play.CombatDriver!.EndTurn();
            var dealt = before - Hero(play).Health.Current;
            play.Dispose();
            return dealt;
        }

        Assert.Equal(25, Falls(0));
        Assert.Equal(25 + 8, Falls(2));
        // Four is where the cap bites: 16 would be too much, so it lands at 12. Five is not worth testing —
        // that much burial takes the player's turn before the stone can fall on it, and spends itself doing
        // so, which is the two Act-IV clocks meeting.
        Assert.Equal(25 + 12, Falls(4));
    }

}
