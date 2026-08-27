using BnbContent.Converter;
using RogueDeck.Core.Combat;
using RogueDeck.Run;
using RogueDeck.Sandbox.Composition;
using RogueDeck.Sandbox.Run;
using RogueDeck.Scenario.Authoring;

namespace BnbContent.Tests;

// Act III elite 4 — Great Toll Frog. Wergild is the one pressure Act III lets you answer outright, and the
// ford swallows every point of it. The only settlement that does not arm the Frog is the one that costs
// more than it has to.
public class ActThreeEliteFrogTests
{
    private const string OneCost = "paper_cut";
    private const string TwoCost = "permit_a38";

    private static CombatantState Hero(RunPlayback play) =>
        play.CombatDriver!.Current!.State.GetCombatant(play.CombatDriver.Current!.HeroId);

    private static CombatantState Frog(RunPlayback play) =>
        play.CombatDriver!.Current!.State.Combatants.First(c => c.Id != play.CombatDriver.Current!.HeroId);

    private static void Play(RunPlayback play, InteractiveRunSession session, string cardId, CombatantId target)
    {
        var card = play.CombatDriver!.Current!.Hand.First(c => c.DefinitionId.value == cardId);
        play.CombatDriver.PlayCard(card.Id, target);
        Assert.True(session.Error is null, session.Error);
    }

    private static int OwedTo(RunPlayback play, CombatantId creditor) =>
        Hero(play).Statuses
            .Where(s => s.DefinitionId == new StatusDefinitionId(ActThree.WergildId)
                && s.SourceCombatantId == creditor)
            .Sum(s => s.Stacks);

    // Make Amends can now raise a SECOND question — the ford's change — so the answers are supplied in
    // order and the last one is repeated for anything still pending.
    private static void MakeAmends(
        RunPlayback play, InteractiveRunSession session, CombatantId at, params int[] answers)
    {
        var card = play.CombatDriver!.Current!.Hand.First(c => c.DefinitionId.value == ActThree.MakeAmendsCardId);
        play.CombatDriver.PlayCard(card.Id, at);
        var next = 0;
        for (var guard = 0; guard < 6; guard++)
        {
            if (play.CombatDriver.PendingOptionChoice is not null)
                play.CombatDriver.SupplyOptionChoice([answers[Math.Min(next++, answers.Length - 1)]]);
            else if (play.CombatDriver.PendingCardChoice is { } cards)
                play.CombatDriver.SupplyCardChoice([cards[0].Id]);
            else
                break;
        }
        Assert.True(session.Error is null, session.Error);
    }

    // ── Nothing Crosses for Free ──────────────────────────────────────────────────────────────────────────

    // Spending your last Energy is a crossing you did not pay for. Once a turn.
    [Fact]
    public void Spending_your_last_energy_is_a_crossing()
    {
        var (play, session, frog) = FightProbe.Start(
            FightProbe.Solo(ActThree.GreatTollFrogEnemyId, "mud_bank_impact", energy: 2),
            deck: [OneCost, OneCost, OneCost, OneCost, OneCost], health: 400);

        Play(play, session, OneCost, frog);
        Assert.Equal(1, FightProbe.StacksOf(Hero(play), ActThree.SafeConductId)); // 1 Energy left: nothing

        Play(play, session, OneCost, frog);
        Assert.Equal(0, FightProbe.StacksOf(Hero(play), ActThree.SafeConductId)); // the purse is empty
        play.Dispose();
    }

    // A free card played on an empty purse is not a crossing paid for: the card has to have cost something.
    [Fact]
    public void A_card_that_costs_nothing_is_not_a_crossing()
    {
        var (play, session, frog) = FightProbe.Start(
            FightProbe.Solo(ActThree.GreatTollFrogEnemyId, "croak_the_amount_due", energy: 0),
            deck: [OneCost, OneCost, OneCost, OneCost, OneCost], health: 400);

        play.CombatDriver!.EndTurn();               // the ford names a price, and Make Amends is free
        Assert.Equal(2, OwedTo(play, frog));

        MakeAmends(play, session, frog, 0, 0);      // paid in coin — but there is no coin, so nothing happens
        Assert.Equal(1, FightProbe.StacksOf(Hero(play), ActThree.SafeConductId));
        play.Dispose();
    }

    // ── Swallowed Payment ─────────────────────────────────────────────────────────────────────────────────

    // Every point of the ford's demand you actually pay is swallowed as a Toll.
    [Fact]
    public void Every_point_paid_to_the_ford_is_swallowed()
    {
        var (play, session, frog) = FightProbe.Start(
            FightProbe.Solo(ActThree.GreatTollFrogEnemyId, "croak_the_amount_due", energy: 9),
            deck: [OneCost, OneCost, OneCost, OneCost, OneCost], health: 400);

        play.CombatDriver!.EndTurn();
        Assert.Equal(2, OwedTo(play, frog));
        Assert.Equal(0, FightProbe.StacksOf(Frog(play), ActThree.TollId));

        MakeAmends(play, session, frog, 0, 0);
        Assert.Equal(1, FightProbe.StacksOf(Frog(play), ActThree.TollId));

        MakeAmends(play, session, frog, 0, 0); // the last point, and the receipt is taken
        Assert.Equal(0, OwedTo(play, frog));
        Assert.Equal(2, FightProbe.StacksOf(Frog(play), ActThree.TollId));
        play.Dispose();
    }

    // LEAVE THE CHANGE: one more card out of hand takes a Toll back out of the ford's throat.
    [Fact]
    public void Leaving_the_change_takes_a_toll_back_out()
    {
        var (play, session, frog) = FightProbe.Start(
            FightProbe.Solo(ActThree.GreatTollFrogEnemyId, "croak_the_amount_due", energy: 9),
            deck: [OneCost, OneCost, OneCost, OneCost, OneCost], health: 400);

        play.CombatDriver!.EndTurn();
        MakeAmends(play, session, frog, 0, 0);      // one point paid, receipt taken
        Assert.Equal(1, FightProbe.StacksOf(Frog(play), ActThree.TollId));

        // Make Amends comes and goes on its own, so the change is counted in real cards.
        var hand = play.CombatDriver.Current!.Hand
            .Count(c => c.DefinitionId.value != ActThree.MakeAmendsCardId);
        MakeAmends(play, session, frog, 0, 1);      // the last point, and the change is left behind

        Assert.Equal(0, OwedTo(play, frog));
        Assert.Equal(1, FightProbe.StacksOf(Frog(play), ActThree.TollId)); // swallowed one, gave one back
        Assert.Equal(hand - 1, play.CombatDriver.Current!.Hand
            .Count(c => c.DefinitionId.value != ActThree.MakeAmendsCardId)); // and it cost a card
        play.Dispose();
    }

    // ── The Toll Is Never Gone ────────────────────────────────────────────────────────────────────────────

    // Every Claim the ford is granted is another demand for 2 — which is another two points to swallow.
    [Fact]
    public void A_new_claim_is_another_demand()
    {
        var (play, _, frog) = FightProbe.Start(
            FightProbe.Solo(ActThree.GreatTollFrogEnemyId, "mud_bank_impact", energy: 9,
                (ActThree.ClaimId, 1)),
            deck: [OneCost, OneCost, OneCost, OneCost, OneCost], health: 500);

        Assert.Equal(0, OwedTo(play, frog));

        // A demand left owing is the act's other route to standing, and the ford bills for that too.
        var (billed, _, ford) = FightProbe.Start(
            FightProbe.Solo(ActThree.GreatTollFrogEnemyId, "croak_the_amount_due", energy: 9),
            deck: [OneCost, OneCost, OneCost, OneCost, OneCost], health: 500);

        billed.CombatDriver!.EndTurn(); // 2 owed
        billed.CombatDriver.EndTurn();  // left owing: 4 damage, a Claim, and a fresh demand for it

        Assert.Equal(1, FightProbe.StacksOf(Frog(billed), ActThree.ClaimId));
        Assert.True(OwedTo(billed, ford) >= 2, "the new standing was billed for");
        play.Dispose();
        billed.Dispose();
    }

    // ── Regurgitate the Toll ──────────────────────────────────────────────────────────────────────────────

    // "12 +4 per Toll, maximum 32. Then Toll → 0 and the player gains 1 Safe-Conduct."
    [Fact]
    public void The_ford_gives_the_whole_throat_back_at_once()
    {
        var (play, _, _) = FightProbe.Start(
            FightProbe.Solo(ActThree.GreatTollFrogEnemyId, "regurgitate_the_toll", energy: 9,
                (ActThree.TollId, 5)),
            deck: [TwoCost, TwoCost, TwoCost, TwoCost, TwoCost], health: 500);

        var before = Hero(play).Health.Current;
        play.CombatDriver!.EndTurn();

        Assert.Equal(before - 32, Hero(play).Health.Current);
        Assert.Equal(0, FightProbe.StacksOf(Frog(play), ActThree.TollId));
        Assert.Equal(2, FightProbe.StacksOf(Hero(play), ActThree.SafeConductId)); // the ford's apology
        play.Dispose();
    }

    // At a full throat the ford's other attacks become the Regurgitation — but its Block and its billing
    // are not offensive intents and go on as they were.
    [Fact]
    public void A_full_throat_replaces_the_fords_next_attack()
    {
        var (play, _, _) = FightProbe.Start(
            FightProbe.Solo(ActThree.GreatTollFrogEnemyId, "mud_bank_impact", energy: 9,
                (ActThree.TollId, 5)),
            deck: [TwoCost, TwoCost, TwoCost, TwoCost, TwoCost], health: 500);

        var before = Hero(play).Health.Current;
        play.CombatDriver!.EndTurn(); // Mud-Bank Impact would be 19

        Assert.Equal(before - 32, Hero(play).Health.Current);
        play.Dispose();

        var (billing, _, frog) = FightProbe.Start(
            FightProbe.Solo(ActThree.GreatTollFrogEnemyId, "croak_the_amount_due", energy: 9,
                (ActThree.TollId, 5)),
            deck: [TwoCost, TwoCost, TwoCost, TwoCost, TwoCost], health: 500);

        var health = Hero(billing).Health.Current;
        billing.CombatDriver!.EndTurn();

        Assert.Equal(health, Hero(billing).Health.Current);   // a croak is not an attack
        Assert.Equal(2, OwedTo(billing, frog));
        Assert.Equal(5, FightProbe.StacksOf(Frog(billing), ActThree.TollId));
        billing.Dispose();
    }

    // "Swallow the Offering — consume 1 Claim; gain 10 +3 per Toll Block, max 25."
    [Fact]
    public void Swallowing_an_offering_blocks_by_the_throat()
    {
        var (play, _, _) = FightProbe.Start(
            FightProbe.Solo(ActThree.GreatTollFrogEnemyId, "swallow_the_offering", energy: 9,
                (ActThree.ClaimId, 1), (ActThree.TollId, 5)),
            deck: [TwoCost, TwoCost, TwoCost, TwoCost, TwoCost], health: 500);

        play.CombatDriver!.EndTurn();

        Assert.Equal(25, Frog(play).DefensivePools[StandardCombatIds.BlockDefensivePool].Current);
        Assert.Equal(0, FightProbe.StacksOf(Frog(play), ActThree.ClaimId));
        Assert.Equal(1, FightProbe.StacksOf(Frog(play), ActThree.ClaimConsumedId));
        play.Dispose();
    }

    // "Croak the Amount Due — Wergild 2; at 2+ Claims Wergild 3 instead."
    [Fact]
    public void The_croak_names_a_bigger_price_with_standing_behind_it()
    {
        var (play, _, frog) = FightProbe.Start(
            FightProbe.Solo(ActThree.GreatTollFrogEnemyId, "croak_the_amount_due", energy: 9,
                (ActThree.ClaimId, 2)),
            deck: [TwoCost, TwoCost, TwoCost, TwoCost, TwoCost], health: 500);

        play.CombatDriver!.EndTurn();

        Assert.Equal(3, OwedTo(play, frog));
        play.Dispose();
    }
}
