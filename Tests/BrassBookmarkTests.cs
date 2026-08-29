using RogueDeck.Core.Combat;
using RogueDeck.Run;
using RogueDeck.Sandbox.Composition;
using RogueDeck.Sandbox.Run;

namespace BnbContent.Tests;

// "The first non-Junk card that enters your hand outside the normal draw step each turn gains Retain until the
// start of your next turn." (BnB_Final_Relics_Master §2)
//
// Every clause of that sentence is a separate way for the relic to be wrong, so each gets its own fact: the
// card has to be kept AT ALL, it has to be the card that arrived and not the hand around it, Junk has to be
// passed over, the second arrival of a turn must not be kept as well, and the keeping has to END — a mark
// nobody takes off would make the card permanent instead of lending it one turn.
//
// A card reaches a hand it was not drawn into two ways, and the relic has to hear both: a card MOVED there
// (Palimpsest Order, out of the discard pile) and a card MADE there (Moonlit Counterfeit's copy).
public class BrassBookmarkTests
{
    private const string Relic = "brass_bookmark";
    private const string Quiet = "ordinance_tablet";
    private const string QuietIntent = "stone_precedent";

    private const string Copy = "moonlit_counterfeit+";   // create a copy of a card in hand; no second prompt
    private const string TakeBack = "palimpsest_order";   // Archive one, return a non-Junk card from the discard
    private const string AddJunk = "tallow_budget";       // add 1 Red Tape (Junk) to your hand

    [Fact]
    public void A_copy_made_in_your_hand_is_the_card_that_is_kept()
    {
        using var fight = Start(withRelic: true, Copy);

        var before = fight.Hand;
        fight.Play(Copy);

        var made = fight.Hand.Except(before).Single();
        Assert.Equal([made], fight.Kept);
    }

    [Fact]
    public void A_card_taken_back_out_of_the_discard_pile_is_kept_as_well()
    {
        using var fight = Start(withRelic: true, TakeBack);

        fight.EndTurn();                          // the turn's hand goes to the discard pile, to be taken back
        var before = fight.Hand;
        fight.Play(TakeBack);

        var returned = fight.Hand.Except(before).Single();
        Assert.Equal([returned], fight.Kept);
    }

    // The point of the whole rebuild: ONE card stays, and the hand it was in does not.
    [Fact]
    public void The_kept_card_survives_the_turn_and_nothing_else_does()
    {
        using var fight = Start(withRelic: true, Copy);

        fight.Play(Copy);
        var kept = fight.Kept.Single();
        var rest = fight.Hand.Where(card => card != kept).ToList();

        fight.EndTurn();

        Assert.Contains(kept, fight.Hand);
        Assert.Empty(fight.Hand.Intersect(rest));
    }

    // "…until the START of your next turn": the loan ends, and the card is an ordinary card again.
    [Fact]
    public void The_keeping_ends_when_the_next_turn_begins()
    {
        using var fight = Start(withRelic: true, Copy);

        fight.Play(Copy);
        var kept = fight.Kept.Single();
        fight.EndTurn();

        Assert.Empty(fight.Kept);
        Assert.Contains(kept, fight.Hand);   // still there — but nothing is holding it any more

        fight.EndTurn();
        Assert.DoesNotContain(kept, fight.Hand);
    }

    [Fact]
    public void Only_the_first_arrival_of_a_turn_is_kept()
    {
        using var fight = Start(withRelic: true, Copy);

        fight.Play(Copy);
        var first = fight.Kept.Single();
        fight.Play(Copy);

        Assert.Equal([first], fight.Kept);
    }

    [Fact]
    public void Junk_is_passed_over()
    {
        using var fight = Start(withRelic: true, AddJunk);

        var before = fight.Hand;
        fight.Play(AddJunk);

        Assert.Single(fight.Hand.Except(before));   // the Red Tape did arrive…
        Assert.Empty(fight.Kept);                   // …and was not kept
    }

    // Without the relic none of this happens — so everything above is the relic's doing and not the card's.
    [Fact]
    public void Without_the_relic_nothing_is_kept()
    {
        using var fight = Start(withRelic: false, Copy);

        fight.Play(Copy);

        Assert.Empty(fight.Kept);
    }

    // ── the probe ─────────────────────────────────────────────────────────────────────────────────────────

    // A deck of nothing but the card under test, so the shuffle cannot decide whether the test can run: the
    // opening hand holds it whatever the seed does. It is deep enough for three turns' draws without the
    // discard pile being shuffled back in — a card that was dealt again would look exactly like a card that
    // was never let go.
    private static Fight Start(bool withRelic, string card)
    {
        var blueprint = FightProbe.OneFight(
            FightProbe.Solo(Quiet, QuietIntent, energy: 9), [.. Enumerable.Repeat(card, 24)]);
        if (withRelic)
            blueprint = blueprint with
            {
                Start = blueprint.Start with { StartingRelics = [.. blueprint.Start.StartingRelics, Relic] },
                Characters = [],
            };

        var play = new RunPlayback(() => { });
        play.Start(blueprint, seed: 1, interactive: true);
        Assert.True(play.Error is null, play.Error);
        while (play.Session!.IsAwaitingInterlude)
            play.Session.Continue();
        Assert.True(play.Session.Error is null, play.Session.Error);
        return new Fight(play);
    }

    private sealed class Fight(RunPlayback play) : IDisposable
    {
        private InteractiveCombatDriver Driver => play.CombatDriver!;
        private CombatantCardZones Zones => Driver.Current!.State.GetCardZones(Driver.Current!.HeroId);

        public IReadOnlyList<CardInstanceId> Hand =>
            [.. Zones.GetCardsInZone(CardZone.Hand).Select(card => card.Id)];

        // Every card the relic is currently holding, wherever it sits — read across all the piles so a card
        // that was kept and then moved cannot hide from the count.
        public IReadOnlyList<CardInstanceId> Kept =>
        [
            .. Enum.GetValues<CardZone>()
                .SelectMany(zone => Zones.GetCardsInZone(zone))
                .Where(card => card.HasMark(StandardCombatIds.RetainedCardMark))
                .Select(card => card.Id),
        ];

        public void Play(string cardId)
        {
            var card = Driver.Current!.Hand.First(c => c.DefinitionId.value == cardId);
            Driver.PlayCard(card.Id, Driver.Current!.State.Combatants.First(c => c.Id != Driver.Current!.HeroId).Id);
            // A card that asks the player to pick another card asks here; the first candidate is as good as
            // any, since no fact below turns on WHICH card was copied or taken back.
            for (var guard = 0; Driver.PendingCardChoice is { Count: > 0 } offered && guard < 4; guard++)
                Driver.SupplyCardChoice([offered[0].Id]);
            Assert.True(play.Session!.Error is null, play.Session.Error);
        }

        public void EndTurn()
        {
            Driver.EndTurn();
            Assert.True(play.Session!.Error is null, play.Session.Error);
        }

        public void Dispose() => play.Dispose();
    }
}
