using BnbContent.Converter;
using RogueDeck.Core.Combat;
using RogueDeck.Run;
using RogueDeck.Sandbox.Run;
using RogueDeck.Scenario.Authoring;
using RogueDeck.Sandbox.Composition;

namespace BnbContent.Tests;

// ACT V, the second god — Inanna, Mistress of the Eanna Ledger, proved in live fights.
//
// The tests follow the ledger: what a claim does to a card, what using it costs, what the Procession does
// with what was not paid, and what the third phase collects when the count runs out.
public class ActFiveBossInannaTests
{
    private const string Cut = "paper_cut";              // Deed, 1 Energy: deal 6
    private const string Penalty = "compounded_penalty"; // Attack, 2 Energy
    private const string Junk = "red_tape";              // rubbish, worth nothing to Eanna

    // Her one turn that neither claims nor collects, so a fight about one rule is only about that rule.
    private const string Quiet = "adorn_eanna";

    private static CombatantState Hero(RunPlayback play) =>
        play.CombatDriver!.Current!.State.GetCombatant(play.CombatDriver.Current!.HeroId);

    private static CombatantState Mistress(RunPlayback play) =>
        play.CombatDriver!.Current!.State.Combatants
            .First(c => c.DefinitionId.value.Contains("inanna", StringComparison.Ordinal));

    private static int Stacks(CombatantState body, string status) => FightProbe.StacksOf(body, status);

    private static int Energy(RunPlayback play) =>
        Hero(play).Resources[StandardCombatIds.EnergyResource].Current;

    private static IReadOnlyList<string> InHand(RunPlayback play) =>
        [.. play.CombatDriver!.Current!.Hand.Select(c => c.DefinitionId.value)];

    // Everything the player still owns, wherever the fight has left it — which is the domain a claim reads,
    // because the hand is gone the moment the player's turn ends.
    private static IReadOnlyList<CardInstance> Deck(RunPlayback play)
    {
        var combat = play.CombatDriver!.Current!;
        var zones = combat.State.GetCardZones(combat.HeroId);
        return
        [
            .. new[] { CardZone.Hand, CardZone.DrawPile, CardZone.DiscardPile }
                .SelectMany(zone => zones.GetCardsInZone(zone)),
        ];
    }

    private static IReadOnlyList<CardInstance> Claimed(RunPlayback play) =>
        [.. Deck(play).Where(card => card.HasMark(ActFive.ClaimMark))];

    private static void Play(RunPlayback play, InteractiveRunSession session, string cardId, CombatantId? target)
    {
        var card = play.CombatDriver!.Current!.Hand.First(c => c.DefinitionId.value == cardId);
        play.CombatDriver.PlayCard(card.Id, target);
        Assert.True(session.Error is null, session.Error);
    }

    // A probe that opens with statuses on BOTH sides — her phase on her, the ledger's rows on the player.
    private static EncounterDefinition Ledger(
        string intent,
        (string Status, int Stacks)[]? hers = null,
        (string Status, int Stacks)[]? theirs = null,
        int energy = 9,
        int? drawn = null)
    {
        var probe = FightProbe.Solo(ActFive.InannaEnemyId, intent, energy, hers ?? []);
        return new EncounterDefinition(
            probe.Id, probe.Enemies, probe.HeroResources,
            [.. probe.HeroStartingStatuses ?? [],
             .. (theirs ?? []).Select(s => new StartingStatusSpec(new StatusDefinitionId(s.Status), s.Stacks))],
            probe.HeroDisplayName, drawn ?? probe.CardsDrawnPerTurn, probe.TriggeredEffects);
    }

    // ── the ledger ────────────────────────────────────────────────────────────────────────────────────────

    [Fact]
    public void The_ledger_opens_with_a_procession_and_nothing_claimed()
    {
        var (play, _, _) = FightProbe.Start(
            FightProbe.Solo(ActFive.InannaEnemyId, Quiet, 9),
            deck: [.. Enumerable.Repeat(Cut, 10)], health: 900);

        Assert.Equal(3, Stacks(Mistress(play), ActFive.ProcessionId));
        Assert.Equal(0, Stacks(Hero(play), ActFive.TempleDueId));
        Assert.Equal(0, Stacks(Hero(play), ActFive.TemplePropertyId));
        Assert.Empty(Claimed(play));
        // Nothing is owed, so neither way of paying is in hand: an offer with nothing to settle is noise.
        Assert.DoesNotContain(ActFive.OfferSurplusCardId, InHand(play));
        play.Dispose();
    }

    // "Claims the unclaimed card with the highest base cost." She reads the whole deck, because the hand is
    // discarded before her turn begins and a claim on an empty table is no claim at all.
    [Fact]
    public void She_claims_the_finest_work_in_the_deck()
    {
        var (play, _, _) = FightProbe.Start(
            FightProbe.Solo(ActFive.InannaEnemyId, "claim_the_finest_work", 9),
            deck: [.. Enumerable.Repeat(Cut, 6), .. Enumerable.Repeat(Penalty, 4)], health: 900);

        play.CombatDriver!.EndTurn();

        var claimed = Claimed(play);
        Assert.Single(claimed);
        Assert.Equal(Penalty, claimed[0].DefinitionId.value);
        Assert.Equal(1, Stacks(Hero(play), ActFive.TemplePropertyId));
        play.Dispose();
    }

    // "Claims the card the player has played most often." She does not search a history — she writes a count
    // in the margin of every card as it is used, and claims the biggest number she wrote.
    [Fact]
    public void The_favored_work_is_the_one_the_player_has_used_most()
    {
        var (play, session, enemy) = FightProbe.Start(
            FightProbe.Solo(ActFive.InannaEnemyId, "claim_the_favored_work", 9),
            deck: [.. Enumerable.Repeat(Cut, 8)], health: 900);

        var used = play.CombatDriver!.Current!.Hand.First(c => c.DefinitionId.value == Cut).Id;
        play.CombatDriver.PlayCard(used, enemy);
        Assert.True(session.Error is null, session.Error);

        play.CombatDriver.EndTurn();

        var claimed = Claimed(play);
        Assert.Single(claimed);
        Assert.Equal(used, claimed[0].Id);
        play.Dispose();
    }

    // Two seals to begin with (§7.8), however many turns she is given to place them.
    [Fact]
    public void She_holds_no_more_than_two_claims_before_the_storehouse()
    {
        var (play, _, _) = FightProbe.Start(
            FightProbe.Solo(ActFive.InannaEnemyId, "claim_the_finest_work", 9),
            deck: [.. Enumerable.Repeat(Cut, 10)], health: 900);

        for (var round = 0; round < 5; round++)
            play.CombatDriver!.EndTurn();

        Assert.Equal(2, Claimed(play).Count);
        Assert.Equal(2, Stacks(Hero(play), ActFive.TemplePropertyId));
        play.Dispose();
    }

    // ── what a claim is worth, and what it costs ──────────────────────────────────────────────────────────

    // The temptation, stated exactly: the claimed copy is CHEAPER, once a turn, and every use of it writes a
    // Due. A four-card deck is drawn whole, so the claim she placed last turn is certainly in hand.
    [Fact]
    public void A_claimed_card_is_a_energy_cheaper_and_every_use_writes_a_due()
    {
        var (play, session, enemy) = FightProbe.Start(
            FightProbe.Solo(ActFive.InannaEnemyId, "claim_the_finest_work", 9),
            deck: [.. Enumerable.Repeat(Penalty, 4)], health: 900);

        play.CombatDriver!.EndTurn();
        var claimed = Claimed(play).Single().Id;
        Assert.Contains(play.CombatDriver.Current!.Hand, c => c.Id == claimed);

        // Two Energy on the sheet, one at the till.
        var before = Energy(play);
        play.CombatDriver.PlayCard(claimed, enemy);
        Assert.True(session.Error is null, session.Error);
        Assert.Equal(before - 1, Energy(play));
        Assert.Equal(1, Stacks(Hero(play), ActFive.TempleDueId));

        // And the copy beside it, which she never entered, is priced as written and writes nothing.
        var plain = play.CombatDriver.Current!.Hand.First(c => c.Id != claimed);
        var paid = Energy(play);
        play.CombatDriver.PlayCard(plain.Id, enemy);
        Assert.Equal(paid - 2, Energy(play));
        Assert.Equal(1, Stacks(Hero(play), ActFive.TempleDueId));
        play.Dispose();
    }

    // "The player controls which card receives the claim through sequencing." The one claim she aims with the
    // player's own hand, and it is charged for the moment it is used, because using it is what claims it.
    [Fact]
    public void The_first_gift_lands_on_the_card_the_player_chooses()
    {
        var (play, session, enemy) = FightProbe.Start(
            FightProbe.Solo(ActFive.InannaEnemyId, "claim_the_first_gift", 9),
            deck: [.. Enumerable.Repeat(Cut, 8)], health: 900);

        play.CombatDriver!.EndTurn();
        Assert.Equal(1, Stacks(Hero(play), ActFive.FirstGiftId));

        var chosen = play.CombatDriver.Current!.Hand.First(c => c.DefinitionId.value == Cut).Id;
        play.CombatDriver.PlayCard(chosen, enemy);
        Assert.True(session.Error is null, session.Error);

        Assert.Equal(chosen, Claimed(play).Single().Id);
        Assert.Equal(0, Stacks(Hero(play), ActFive.FirstGiftId));
        Assert.Equal(1, Stacks(Hero(play), ActFive.TempleDueId));
        play.Dispose();
    }

    // ── paying ────────────────────────────────────────────────────────────────────────────────────────────

    // The Energy Offering (§7.4). One for one, and the cap the master asks for is the pool itself: what pays
    // the temple is the attack that was not made.
    [Fact]
    public void Offering_the_surplus_settles_one_due_for_one_energy()
    {
        var (play, session, _) = FightProbe.Start(
            Ledger(Quiet, theirs: [(ActFive.TempleDueId, 3)]),
            deck: [.. Enumerable.Repeat(Cut, 8)], health: 900);

        Assert.Contains(ActFive.OfferSurplusCardId, InHand(play));
        Assert.Contains(ActFive.DedicateWorkCardId, InHand(play));

        var before = Energy(play);
        Play(play, session, ActFive.OfferSurplusCardId, null);

        Assert.Equal(before - 1, Energy(play));
        Assert.Equal(2, Stacks(Hero(play), ActFive.TempleDueId));
        // Still owed, so the offer stands again.
        Assert.Contains(ActFive.OfferSurplusCardId, InHand(play));
        play.Dispose();
    }

    // Dedicating a work (§7.4): the card is gone for the rest of the fight, and Eanna allows for it exactly
    // what the master says — nothing for rubbish, one for a work, four for something already hers.
    [Fact]
    public void A_dedicated_work_leaves_the_fight_and_rubbish_settles_nothing()
    {
        var (play, session, _) = FightProbe.Start(
            Ledger(Quiet, theirs: [(ActFive.TempleDueId, 6)]),
            deck: [.. Enumerable.Repeat(Cut, 4), .. Enumerable.Repeat(Junk, 4)], health: 900);

        var work = play.CombatDriver!.Current!.Hand.First(c => c.DefinitionId.value == Cut).Id;
        Play(play, session, ActFive.DedicateWorkCardId, null);
        Assert.NotNull(play.CombatDriver.PendingCardChoice);
        play.CombatDriver.SupplyCardChoice([work]);
        Assert.True(session.Error is null, session.Error);

        Assert.Equal(5, Stacks(Hero(play), ActFive.TempleDueId));
        Assert.DoesNotContain(Deck(play), c => c.Id == work);

        // And the same sheet given a sheet of Red Tape settles nothing at all.
        var rubbish = play.CombatDriver.Current!.Hand.First(c => c.DefinitionId.value == Junk).Id;
        Play(play, session, ActFive.DedicateWorkCardId, null);
        play.CombatDriver.SupplyCardChoice([rubbish]);
        Assert.True(session.Error is null, session.Error);

        Assert.Equal(5, Stacks(Hero(play), ActFive.TempleDueId));
        Assert.DoesNotContain(Deck(play), c => c.Id == rubbish);
        play.Dispose();
    }

    // "Eanna wants value. Not garbage." A card she has already entered is worth four of an ordinary one —
    // which is the fight's sharpest decision, because it is the card the discount was making magnificent.
    [Fact]
    public void A_dedicated_claim_is_worth_four_ordinary_works()
    {
        var (play, session, _) = FightProbe.Start(
            Ledger("claim_the_finest_work", theirs: [(ActFive.TempleDueId, 6)]),
            deck: [.. Enumerable.Repeat(Penalty, 4)], health: 900);

        play.CombatDriver!.EndTurn();
        var claimed = Claimed(play).Single().Id;
        Assert.Contains(play.CombatDriver.Current!.Hand, c => c.Id == claimed);

        var owed = Stacks(Hero(play), ActFive.TempleDueId);
        Play(play, session, ActFive.DedicateWorkCardId, null);
        play.CombatDriver.SupplyCardChoice([claimed]);
        Assert.True(session.Error is null, session.Error);

        Assert.Equal(owed - 4, Stacks(Hero(play), ActFive.TempleDueId));
        Assert.DoesNotContain(Deck(play), c => c.Id == claimed);
        Assert.Empty(Claimed(play));
        play.Dispose();
    }

    // ── the Procession ────────────────────────────────────────────────────────────────────────────────────

    // Called a whole turn before it collects — which is the turn the paying is meant to happen in — and what
    // was not paid does not go away.
    [Fact]
    public void The_procession_is_called_before_it_collects_and_what_is_unpaid_becomes_arrears()
    {
        var (play, _, _) = FightProbe.Start(
            Ledger(Quiet, theirs: [(ActFive.TempleDueId, 5)]),
            deck: [.. Enumerable.Repeat(Cut, 10)], health: 900);

        // Three of her windows walk the count down; the third one calls it.
        for (var round = 0; round < 3; round++)
            play.CombatDriver!.EndTurn();

        Assert.Equal(0, Stacks(Mistress(play), ActFive.ProcessionId));
        Assert.Equal(1, Stacks(Mistress(play), ActFive.ProcessionCalledId));
        Assert.Equal(5, Stacks(Hero(play), ActFive.TempleDueId));

        // …and her next act reads the ledger aloud.
        play.CombatDriver!.EndTurn();

        Assert.Equal(0, Stacks(Hero(play), ActFive.TempleDueId));
        Assert.Equal(5, Stacks(Hero(play), ActFive.ArrearsId));
        Assert.Equal(3, Stacks(Mistress(play), ActFive.ProcessionId));
        Assert.Equal(0, Stacks(Mistress(play), ActFive.ProcessionCalledId));
        play.Dispose();
    }

    // "Then keep it. And keep what follows." Refusal is allowed, and it is asked for again with interest.
    [Fact]
    public void Demand_the_arrears_grows_with_what_was_refused()
    {
        static int Taken((string, int)[] arrears)
        {
            var (play, _, _) = FightProbe.Start(
                Ledger("demand_the_arrears", theirs: arrears),
                deck: [.. Enumerable.Repeat(Cut, 10)], health: 900);
            var before = Hero(play).Health.Current;
            play.CombatDriver!.EndTurn();
            var taken = before - Hero(play).Health.Current;
            play.Dispose();
            return taken;
        }

        Assert.Equal(10, Taken([]));
        Assert.Equal(10 + (2 * 5), Taken([(ActFive.ArrearsId, 5)]));
    }

    // ── the storehouse ────────────────────────────────────────────────────────────────────────────────────

    // Phase II: one surplus claim at a time, worn by the side that produces the surplus so the chip that
    // charges you is on your own row — and the first property card of the turn is answered with Splendor.
    [Fact]
    public void The_open_storehouse_posts_a_surplus_claim_and_pays_the_first_property_card()
    {
        var (play, session, enemy) = FightProbe.Start(
            Ledger(Quiet,
                hers: [(ActFive.StorehouseId, 1)],
                theirs: [(ActFive.FirstGiftId, 1)]),
            deck: [.. Enumerable.Repeat(Cut, 8)], health: 900);

        Assert.Equal(1, Stacks(Hero(play), ActFive.ClaimOfGrainId));
        Assert.Equal(0, Stacks(Hero(play), ActFive.ClaimOfHandsId));

        var gift = play.CombatDriver!.Current!.Hand.First(c => c.DefinitionId.value == Cut).Id;
        play.CombatDriver.PlayCard(gift, enemy);
        Assert.True(session.Error is null, session.Error);

        Assert.Equal(1, Stacks(Hero(play), ActFive.SplendorId));
        Assert.Equal(1, Stacks(Hero(play), ActFive.TempleDueId));

        // Only the FIRST each turn: the second use of temple property is Due and nothing more.
        var second = play.CombatDriver.Current!.Hand.First(c => c.DefinitionId.value == Cut).Id;
        play.CombatDriver.PlayCard(second, enemy);
        Assert.Equal(1, Stacks(Hero(play), ActFive.SplendorId));

        // …and the claim of the turn rotates, so what she is watching next is knowable rather than random.
        play.CombatDriver.EndTurn();
        Assert.Equal(0, Stacks(Hero(play), ActFive.ClaimOfGrainId));
        Assert.Equal(1, Stacks(Hero(play), ActFive.ClaimOfHandsId));
        play.Dispose();
    }

    // A surplus claim is a line, not a ban: an ordinary hand is free, and only what is above it is Due.
    [Fact]
    public void A_surplus_claim_charges_only_for_what_is_above_its_line()
    {
        static int Owed(int drawn)
        {
            var (play, _, _) = FightProbe.Start(
                Ledger(Quiet, hers: [(ActFive.StorehouseId, 1)],
                    theirs: [(ActFive.ClaimOfHandsId, 1)], drawn: drawn),
                deck: [.. Enumerable.Repeat(Cut, 20)], health: 900);
            var owed = Stacks(Hero(play), ActFive.TempleDueId);
            play.Dispose();
            return owed;
        }

        // Five cards is under the line and free; eleven is four above it, and every two of those is a Due.
        Assert.Equal(0, Owed(5));
        Assert.Equal(2, Owed(11));
    }

    // ── all things enter Eanna ────────────────────────────────────────────────────────────────────────────

    // "You still divide the world. Yours. Mine. A provincial distinction." Three turns in which everything is
    // hers, and then the whole ledger at once — no Block, and the seals come off because she has been paid.
    [Fact]
    public void All_things_enter_eanna_claims_the_whole_deck_and_collects_when_the_count_runs_out()
    {
        var (play, session, enemy) = FightProbe.Start(
            Ledger(Quiet, hers: [(ActFive.AllThingsAnnouncedId, 1)]),
            deck: [.. Enumerable.Repeat(Cut, 8)], health: 900);

        play.CombatDriver!.EndTurn();

        // Everything the player owns, entered — and the count the player opens their turn looking at.
        Assert.Equal(8, Claimed(play).Count);
        Assert.Equal(3, Stacks(Mistress(play), ActFive.AllThingsId));
        Assert.Equal(8, Stacks(Hero(play), ActFive.TemplePropertyId));

        // Three turns of using what is no longer yours.
        for (var turn = 0; turn < 3; turn++)
        {
            while (play.CombatDriver!.Current!.Hand.Any(c => c.DefinitionId.value == Cut)
                   && Energy(play) >= 1)
                Play(play, session, Cut, enemy);
            play.CombatDriver!.EndTurn();
        }

        Assert.Equal(0, Stacks(Mistress(play), ActFive.AllThingsId));
        Assert.Equal(0, Stacks(Hero(play), ActFive.TempleDueId));
        Assert.True(Stacks(Hero(play), ActFive.ArrearsId) > 0, "the ledger collected nothing");
        Assert.Empty(Claimed(play));
        play.Dispose();
    }
}
