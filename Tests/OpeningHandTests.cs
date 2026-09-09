using BnbContent.Converter;
using RogueDeck.Core.Combat;
using RogueDeck.Run;
using RogueDeck.Sandbox.Composition;
using RogueDeck.Sandbox.Run;

namespace BnbContent.Tests;

// THE HAND A TURN OPENS WITH, proved in live fights.
//
// Everything in this game that happens "at the start of your turn, with your hand in front of you" is hung on
// the DRAW, because a turn-start rule runs before the hand exists. The engine announces every draw the same
// way, so without a guard those rules also fire for every card a card or another rule draws — and the ones
// that pay in CARDS then pay themselves, again and again, for as long as the draw pile lasts.
//
// That is what killed a real run: `--playtest 5`, seed 20260721, an Act-IV elite room, nineteen draws deep
// into one another before the engine's queue-cycle limit stopped the fight and ended the walk. These tests
// hold the three shapes of it: a turn counter that counted draws, a payment that fed itself, and several of
// them in one fight, which is how it was actually met.
public class OpeningHandTests
{
    private const string Quiet = "ordinance_tablet";       // an Act-III enemy that does nothing much
    private const string QuietIntent = "stone_precedent";
    private const string DrawsACard = "secure_misfiling";  // Working, 0: add a Misfiled Paper, draw 1

    private static CounterId BellTurn => new("brass_bell_turn");

    private static CombatantState Hero(RunPlayback play) =>
        play.CombatDriver!.Current!.State.GetCombatant(play.CombatDriver.Current!.HeroId);

    private static int Hand(RunPlayback play) => play.CombatDriver!.Current!.Hand.Count;

    private static (RunPlayback Play, InteractiveRunSession Session) Fight(
        IReadOnlyList<string> relics, string card = DrawsACard, int energy = 3)
    {
        var probe = FightProbe.Solo(Quiet, QuietIntent, energy);
        var blueprint = FightProbe.OneFight(probe, [.. Enumerable.Repeat(card, 40)]);
        blueprint = blueprint with
        {
            Start = blueprint.Start with
            {
                StartingRelics = [.. blueprint.Start.StartingRelics, .. relics],
                MaxHealth = 400,
                StartingHealth = 400,
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
        return (play, session);
    }

    // "Every third turn the bell rings." A turn is a turn, however many times the hand is added to.
    [Fact]
    public void The_bell_counts_turns_and_not_draws()
    {
        var (play, session) = Fight(["brass_service_bell"]);

        Assert.Equal(1, Hero(play).GetCounter(BellTurn));

        // A card that draws is not the start of another turn.
        var card = play.CombatDriver!.Current!.Hand.First(c => c.DefinitionId.value == DrawsACard);
        play.CombatDriver.PlayCard(card.Id, null);
        Assert.True(session.Error is null, session.Error);
        Assert.Equal(1, Hero(play).GetCounter(BellTurn));

        play.CombatDriver.EndTurn();
        Assert.Equal(2, Hero(play).GetCounter(BellTurn));

        play.CombatDriver.EndTurn();
        Assert.Equal(3, Hero(play).GetCounter(BellTurn));
        play.Dispose();
    }

    // "Take no damage on their turn and your next hand comes with an Energy and a card." Exactly one card:
    // the card it pays with is a draw like any other, and the payment must not hear its own payment.
    //
    // ⚠ This one holds either way — the engine's own re-entry guard already refuses a triggered rule that
    // would run inside its own chain, which is why a single such relic never ran away on its own. It took
    // several of them, each one's payment being a different rule's new turn, to get past that (below).
    [Fact]
    public void A_settlement_pays_exactly_one_card_at_the_hand()
    {
        var (play, session) = Fight(["signed_settlement"], card: "paper_cut");

        var opening = Hand(play);
        play.CombatDriver!.EndTurn();
        Assert.True(session.Error is null, session.Error);

        // Exactly one card more than a hand: the settlement paid once, not once per card it dealt.
        Assert.Equal(opening + 1, Hand(play));
        play.Dispose();
    }

    // The room the run died in: several of these relics at once, each one's payment another one's new turn.
    [Fact]
    public void A_hand_full_of_paying_relics_still_opens_a_turn_that_ends()
    {
        var (play, session) = Fight(
            ["brass_service_bell", "signed_settlement", "survey_cairn", "countersealed_ring_of_keeping",
             "deferred_appointment_book"]);

        for (var turn = 0; turn < 6; turn++)
        {
            Assert.True(session.Error is null, session.Error);
            Assert.True(play.Error is null, play.Error);
            Assert.True(Hand(play) < 25, $"turn {turn + 1} opened with {Hand(play)} cards in hand");
            // One tick of the bell per turn, whatever the other four relics dealt into the hand.
            Assert.Equal(turn + 1, Hero(play).GetCounter(BellTurn));
            play.CombatDriver!.EndTurn();
        }

        play.Dispose();
    }
}
