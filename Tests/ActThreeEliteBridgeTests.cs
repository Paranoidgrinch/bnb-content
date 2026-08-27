using BnbContent.Converter;
using RogueDeck.Core.Combat;
using RogueDeck.Run;
using RogueDeck.Sandbox.Composition;
using RogueDeck.Sandbox.Run;
using RogueDeck.Scenario.Authoring;

namespace BnbContent.Tests;

// Act III elite 3 — The Wrong Bridge in Person. One fight with two banks, and one joke told twice:
// settling in full is good for you on This Bank and good for the BRIDGE on the Other.
public class ActThreeEliteBridgeTests
{
    private const string Deed = "paper_cut";
    private const string Working = "cower_behind_a_desk";

    private static CombatantState Hero(RunPlayback play) =>
        play.CombatDriver!.Current!.State.GetCombatant(play.CombatDriver.Current!.HeroId);

    private static CombatantState Bridge(RunPlayback play) =>
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

    private static void MakeAmends(RunPlayback play, InteractiveRunSession session, int option, CombatantId at)
    {
        var card = play.CombatDriver!.Current!.Hand.First(c => c.DefinitionId.value == ActThree.MakeAmendsCardId);
        play.CombatDriver.PlayCard(card.Id, at);
        for (var guard = 0; guard < 4; guard++)
        {
            if (play.CombatDriver.PendingOptionChoice is not null)
                play.CombatDriver.SupplyOptionChoice([option]);
            else if (play.CombatDriver.PendingCardChoice is { } cards)
                play.CombatDriver.SupplyCardChoice([cards[0].Id]);
            else
                break;
        }
        Assert.True(session.Error is null, session.Error);
    }

    // Everything below happens on the far bank once the Bridge has been driven under 104, so most probes
    // start it there outright.
    private static EncounterDefinition FarBank(string intentId, params (string, int)[] extra) =>
        FightProbe.Solo(ActThree.WrongBridgeEnemyId, intentId, energy: 9,
            [(ActThree.TheOtherBankId, 1), .. extra]);

    // Return Standing is the PLAYER's, so a probe that wants some has to open the hero with it.
    private static EncounterDefinition WithHero(EncounterDefinition probe, params (string, int)[] statuses) =>
        new(probe.Id, probe.Enemies, probe.HeroResources,
            [.. probe.HeroStartingStatuses ?? [],
             .. statuses.Select(s => new StartingStatusSpec(new StatusDefinitionId(s.Item1), s.Item2))],
            probe.HeroDisplayName, probe.CardsDrawnPerTurn, probe.TriggeredEffects);

    // ── Debt Before Passage ───────────────────────────────────────────────────────────────────────────────

    // With nothing owed, hitting the Bridge is just hitting a bridge.
    [Fact]
    public void Hitting_a_bridge_you_owe_nothing_is_no_trespass()
    {
        var (play, session, bridge) = FightProbe.Start(
            FightProbe.Solo(ActThree.WrongBridgeEnemyId, "approaching_abutment", energy: 9),
            deck: [Deed, Deed, Deed, Deed, Deed], health: 400);

        Play(play, session, Deed, bridge);

        Assert.Equal(1, FightProbe.StacksOf(Hero(play), ActThree.SafeConductId)); // nothing was refused
        play.Dispose();
    }

    // With a demand open, the first blow of the turn is a Trespass on top of the damage — and only the first.
    [Fact]
    public void While_a_demand_is_open_the_first_blow_of_a_turn_is_a_trespass()
    {
        var (play, session, bridge) = FightProbe.Start(
            FightProbe.Solo(ActThree.WrongBridgeEnemyId, "future_toll", energy: 9),
            deck: [Deed, Deed, Deed, Deed, Deed], health: 400);

        play.CombatDriver!.EndTurn(); // Future Toll: 2 owed
        Assert.Equal(2, OwedTo(play, bridge));

        var before = Bridge(play).Health.Current;
        Play(play, session, Deed, bridge);
        Assert.True(Bridge(play).Health.Current < before, "the damage still resolves");
        Assert.Equal(0, FightProbe.StacksOf(Hero(play), ActThree.SafeConductId)); // the licence went

        Play(play, session, Deed, bridge);
        Play(play, session, Deed, bridge);
        Assert.Equal(0, Hero(play).Statuses
            .Count(s => s.DefinitionId == new StatusDefinitionId(ActThree.TrespassId))); // once a turn
        play.Dispose();
    }

    // ── Toll Before Passage ───────────────────────────────────────────────────────────────────────────────

    // Every Claim granted on This Bank is another demand for 2, and the Claim stays: a right and a price are
    // two different things.
    [Fact]
    public void A_new_claim_on_this_bank_is_another_demand()
    {
        var (play, _, bridge) = FightProbe.Start(
            FightProbe.Solo(ActThree.WrongBridgeEnemyId, "future_toll", energy: 9),
            deck: [Working, Working, Working, Working, Working], health: 500);

        play.CombatDriver!.EndTurn(); // 2 owed
        play.CombatDriver.EndTurn();  // left owing: 4 damage, and the demand becomes standing

        Assert.Equal(1, FightProbe.StacksOf(Bridge(play), ActThree.ClaimId));  // the Claim remains
        Assert.True(OwedTo(play, bridge) >= 2, "the new standing charged a new toll");
        play.Dispose();
    }

    // …and on the far bank it does not, because there a settled demand is what makes standing.
    [Fact]
    public void A_new_claim_on_the_far_bank_charges_nothing()
    {
        var (play, _, bridge) = FightProbe.Start(
            FarBank("future_toll"), deck: [Working, Working, Working, Working, Working], health: 500);

        play.CombatDriver!.EndTurn(); // Charge the Return Toll — no Claim to cash, so nothing is demanded
        Assert.Equal(0, OwedTo(play, bridge));
        play.Dispose();
    }

    // ── the crossing ──────────────────────────────────────────────────────────────────────────────────────

    // Ninety-six damage and the Bridge turns around. Its Claims and the demand it has raised are exactly
    // where they were, because nothing moved.
    [Fact]
    public void The_bridge_turns_around_at_the_far_bank()
    {
        var (play, session, bridge) = FightProbe.Start(
            // Standing it merely holds, and an intent that only hits: nothing here grants a Claim, so what
            // the Bridge carries across is exactly what it carried in.
            FightProbe.Solo(ActThree.WrongBridgeEnemyId, "approaching_abutment", energy: 40,
                (ActThree.ClaimId, 2)),
            deck: [.. Enumerable.Repeat(Deed, 5)], health: 900);

        Assert.Equal(0, FightProbe.StacksOf(Bridge(play), ActThree.TheOtherBankId));

        // Drive it under 104 across several turns of the whole hand.
        for (var turn = 0; turn < 12 && FightProbe.StacksOf(Bridge(play), ActThree.TheOtherBankId) == 0; turn++)
        {
            while (play.CombatDriver!.Current!.Hand.Any(c => c.DefinitionId.value == Deed))
                Play(play, session, Deed, bridge);
            play.CombatDriver.EndTurn();
        }

        Assert.Equal(1, FightProbe.StacksOf(Bridge(play), ActThree.TheOtherBankId));
        Assert.True(Bridge(play).Health.Current <= ActThree.OtherBankHealth);
        Assert.Equal(2, FightProbe.StacksOf(Bridge(play), ActThree.ClaimId)); // carried across …
        Assert.Equal(0, FightProbe.StacksOf(Bridge(play), ActThree.ClaimCreatedId)); // …and never re-granted
        play.Dispose();
    }

    // ── settlement, from each bank ────────────────────────────────────────────────────────────────────────

    // This Bank: paying in full strikes a Claim off and costs the Bridge 5 HP that no Block can see.
    [Fact]
    public void Settling_on_this_bank_strikes_a_claim_and_five_health()
    {
        var (play, session, bridge) = FightProbe.Start(
            FightProbe.Solo(ActThree.WrongBridgeEnemyId, "future_toll", energy: 9,
                (ActThree.ClaimId, 1)),
            deck: [Working, Working, Working, Working, Working], health: 500);

        play.CombatDriver!.EndTurn(); // 2 owed
        MakeAmends(play, session, option: 0, at: bridge);
        MakeAmends(play, session, option: 0, at: bridge);
        Assert.Equal(0, OwedTo(play, bridge));

        var health = Bridge(play).Health.Current;
        play.CombatDriver.EndTurn();

        Assert.Equal(0, FightProbe.StacksOf(Bridge(play), ActThree.ClaimId));
        Assert.Equal(health - 5, Bridge(play).Health.Current);
        play.Dispose();
    }

    // The Other Bank: the same payment GRANTS the Bridge standing and spends the Return Standing you had.
    [Fact]
    public void Settling_on_the_far_bank_grants_the_bridge_standing()
    {
        var (play, session, bridge) = FightProbe.Start(
            FarBank("future_toll", (ActThree.ClaimId, 1), (ActThree.ClaimCreatedId, 1)),
            deck: [Working, Working, Working, Working, Working], health: 500);

        play.CombatDriver!.EndTurn(); // Charge the Return Toll: the Claim is cashed for a demand of 2
        Assert.Equal(2, OwedTo(play, bridge));
        Assert.Equal(0, FightProbe.StacksOf(Bridge(play), ActThree.ClaimId));

        MakeAmends(play, session, option: 0, at: bridge);
        MakeAmends(play, session, option: 0, at: bridge);
        play.CombatDriver.EndTurn();

        // Freshly GRANTED — which is what the announcement records, and what the Bridge immediately cashes
        // again on the far bank's own toll.
        Assert.Equal(2, FightProbe.StacksOf(Bridge(play), ActThree.ClaimCreatedId));
        play.Dispose();
    }

    // ── Return Standing ───────────────────────────────────────────────────────────────────────────────────

    // Refusing the Bridge on the far bank is worth something to you — once a turn, and at most twice.
    [Fact]
    public void Refusing_on_the_far_bank_earns_return_standing()
    {
        var (play, session, bridge) = FightProbe.Start(
            FarBank("future_toll", (ActThree.ClaimId, 1)),
            deck: [Deed, Deed, Deed, Deed, Deed], health: 500);

        play.CombatDriver!.EndTurn(); // the Claim is cashed for a demand, so the Bridge's law is live
        Assert.Equal(2, OwedTo(play, bridge));

        Play(play, session, Deed, bridge); // the first blow of the turn — refused by the licence
        Assert.Equal(0, FightProbe.StacksOf(Hero(play), ActThree.SafeConductId));
        Assert.Equal(1, FightProbe.StacksOf(Hero(play), ActThree.ReturnStandingId));

        Play(play, session, Deed, bridge); // the law answers one blow a turn, so nothing more is earned
        Assert.Equal(1, FightProbe.StacksOf(Hero(play), ActThree.ReturnStandingId));
        play.Dispose();
    }

    // "Move the Gap — remove 1 player Safe-Conduct if possible; otherwise attempt 1 Trespass; then 10
    // damage." Taking a licence is not refusing one, so it earns the player nothing.
    [Fact]
    public void Moving_the_gap_takes_the_licence_or_files_without_one()
    {
        var (play, _, _) = FightProbe.Start(
            FarBank("raise_the_tollgate"),
            deck: [Working, Working, Working, Working, Working], health: 500);

        Assert.Equal(1, FightProbe.StacksOf(Hero(play), ActThree.SafeConductId));
        play.CombatDriver!.EndTurn();
        Assert.Equal(0, FightProbe.StacksOf(Hero(play), ActThree.SafeConductId));
        Assert.Equal(0, FightProbe.StacksOf(Hero(play), ActThree.ReturnStandingId));

        play.CombatDriver.EndTurn(); // with no licence left to take, it files instead
        Assert.Equal(1, Hero(play).Statuses
            .Count(s => s.DefinitionId == new StatusDefinitionId(ActThree.TrespassId)));
        play.Dispose();
    }

    // "Stonework Remembers the Crossing — 14 +4 per Return Standing, max 22; then Standing → 0."
    [Fact]
    public void The_stonework_cashes_the_standing_it_gave_you()
    {
        var (bare, _, _) = FightProbe.Start(
            FarBank("charge_for_the_crossing"),
            deck: [Working, Working, Working, Working, Working], health: 500);
        var flat = Hero(bare).Health.Current;
        bare.CombatDriver!.EndTurn();
        Assert.Equal(flat - 14, Hero(bare).Health.Current);
        bare.Dispose();

        var (play, _, _) = FightProbe.Start(
            WithHero(FarBank("charge_for_the_crossing"), (ActThree.ReturnStandingId, 2)),
            deck: [Working, Working, Working, Working, Working], health: 500);

        var before = Hero(play).Health.Current;
        play.CombatDriver!.EndTurn();

        Assert.Equal(before - 22, Hero(play).Health.Current);                    // the stated maximum
        Assert.Equal(0, FightProbe.StacksOf(Hero(play), ActThree.ReturnStandingId)); // and it is spent
        play.Dispose();
    }

    // The far bank's signature clears every last thing the crossing was worth to either side.
    [Fact]
    public void The_collapse_clears_both_sides_ledgers()
    {
        var (play, _, _) = FightProbe.Start(
            FarBank("collapse_before_completion", (ActThree.ClaimId, 2), (ActThree.ClaimCreatedId, 2)),
            deck: [Working, Working, Working, Working, Working], health: 500);

        var before = Hero(play).Health.Current;
        play.CombatDriver!.EndTurn();

        Assert.Equal(before - 28, Hero(play).Health.Current);
        Assert.Equal(0, FightProbe.StacksOf(Bridge(play), ActThree.ClaimId));
        Assert.Equal(0, FightProbe.StacksOf(Hero(play), ActThree.ReturnStandingId));
        play.Dispose();
    }
}
