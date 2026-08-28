using BnbContent.Converter;
using RogueDeck.Core.Combat;
using RogueDeck.Run;
using RogueDeck.Sandbox.Composition;
using RogueDeck.Sandbox.Run;
using RogueDeck.Scenario.Authoring;

namespace BnbContent.Tests;

// Act III boss 5 — The Queen Under the Hill. The act's complete examination, and the one fight where the
// player is expected to use the legal system on purpose: gift → Claim → Wergild → Favour → audience.
// Declining the whole cycle stays legal to the end; it is simply a longer road.
public class ActThreeBossQueenTests
{
    private const string Working = "cower_behind_a_desk";

    // Grace indices: 0 decline, then passage, plenty, shelter, recall.
    private const int Decline = 0;
    private const int Passage = 1;
    private const int Shelter = 3;

    private static CombatantState Hero(RunPlayback play) =>
        play.CombatDriver!.Current!.State.GetCombatant(play.CombatDriver.Current!.HeroId);

    private static CombatantState Court(RunPlayback play) =>
        play.CombatDriver!.Current!.State.Combatants.First(c => c.Id != play.CombatDriver.Current!.HeroId);

    private static int Block(CombatantState c) =>
        c.DefensivePools.TryGetValue(StandardCombatIds.BlockDefensivePool, out var pool) ? pool.Current : 0;

    private static int OwedTo(RunPlayback play, CombatantId creditor) =>
        Hero(play).Statuses
            .Where(s => s.DefinitionId == new StatusDefinitionId(ActThree.WergildId)
                && s.SourceCombatantId == creditor)
            .Sum(s => s.Stacks);

    private static void Play(RunPlayback play, InteractiveRunSession session, string cardId, CombatantId target)
    {
        var card = play.CombatDriver!.Current!.Hand.First(c => c.DefinitionId.value == cardId);
        play.CombatDriver.PlayCard(card.Id, target);
        Assert.True(session.Error is null, session.Error);
    }

    // The court settles on the first bell and offers from the next turn on.
    private static (RunPlayback Play, InteractiveRunSession Session, CombatantId Queen) Start(
        string intentId, IReadOnlyList<string> deck, int energy = 9, int? health = null,
        params (string, int)[] statuses)
    {
        var probe = FightProbe.Solo(ActThree.QueenEnemyId, intentId, energy, statuses);
        if (health is { } hp)
            probe = new EncounterDefinition(probe.Id,
                [probe.Enemies[0] with { MaxHealth = hp }],
                probe.HeroResources, probe.HeroStartingStatuses, probe.HeroDisplayName,
                probe.CardsDrawnPerTurn, probe.TriggeredEffects);
        return FightProbe.Start(probe, deck: deck, health: 900);
    }

    private static void NextTurnTaking(RunPlayback play, int grace)
    {
        play.CombatDriver!.EndTurn();
        Assert.NotNull(play.CombatDriver.PendingOptionChoice);
        play.CombatDriver.SupplyOptionChoice([grace]);
    }

    private static void Audience(RunPlayback play, InteractiveRunSession session, int option, CombatantId at)
    {
        var card = play.CombatDriver!.Current!.Hand
            .First(c => c.DefinitionId.value == ActThree.RightOfAudienceCardId);
        play.CombatDriver.PlayCard(card.Id, at);
        if (play.CombatDriver.PendingOptionChoice is not null)
            play.CombatDriver.SupplyOptionChoice([option]);
        Assert.True(session.Error is null, session.Error);
    }

    // ── Do Not Speak Before Addressed ─────────────────────────────────────────────────────────────────────

    // Leave to speak is what a licence is. The first word of a turn takes one; the first word of a turn
    // without one is a Trespass owed to the court.
    [Fact]
    public void Speaking_without_leave_is_a_trespass()
    {
        var (play, session, queen) = Start("count_every_buried_name", [.. Enumerable.Repeat(Working, 5)]);

        Play(play, session, Working, queen); // the fight's opening licence answers for this one
        Assert.Equal(0, FightProbe.StacksOf(Hero(play), ActThree.SafeConductId));
        Assert.Equal(0, Hero(play).Statuses
            .Count(s => s.DefinitionId == new StatusDefinitionId(ActThree.TrespassId)));

        Play(play, session, Working, queen); // …and the court answers one word a turn, not every word
        Assert.Equal(0, Hero(play).Statuses
            .Count(s => s.DefinitionId == new StatusDefinitionId(ActThree.TrespassId)));

        NextTurnTaking(play, Decline);
        Play(play, session, Working, queen); // a new turn, and nothing left to answer for it

        Assert.Equal(1, Hero(play).Statuses
            .Count(s => s.DefinitionId == new StatusDefinitionId(ActThree.TrespassId)));
        play.Dispose();
    }

    // ── Royal Grace ───────────────────────────────────────────────────────────────────────────────────────

    // Every gift is real, and every one of them hands her standing she did not have. Declining is free.
    [Fact]
    public void Grace_is_a_real_gift_that_hands_her_standing()
    {
        var (play, _, _) = Start("count_every_buried_name", [.. Enumerable.Repeat(Working, 5)]);

        NextTurnTaking(play, Decline);
        Assert.Equal(0, FightProbe.StacksOf(Court(play), ActThree.ClaimId));

        NextTurnTaking(play, Shelter);
        Assert.True(Block(Hero(play)) >= 12);
        Assert.Equal(1, FightProbe.StacksOf(Court(play), ActThree.ClaimId));
        Assert.Equal(1, FightProbe.StacksOf(Court(play), ActThree.ClaimCreatedId)); // granted, not moved
        play.Dispose();
    }

    // At the ceiling there is nothing left to give in return, so no grace is offered at all.
    [Fact]
    public void No_grace_is_offered_at_the_ceiling()
    {
        var (play, _, _) = Start("count_every_buried_name", [.. Enumerable.Repeat(Working, 5)],
            energy: 9, health: null, (ActThree.ClaimId, 3));

        play.CombatDriver!.EndTurn();

        Assert.Null(play.CombatDriver.PendingOptionChoice);
        play.Dispose();
    }

    // ── Favour ────────────────────────────────────────────────────────────────────────────────────────────

    // Settling a royal demand in full is what the court owes you for, and it hands you the audience too.
    [Fact]
    public void Settling_in_full_earns_favour_and_an_audience()
    {
        var (play, session, queen) = Start("call_in_the_gift", [.. Enumerable.Repeat(Working, 5)],
            energy: 9, health: null, (ActThree.ClaimId, 1));

        play.CombatDriver!.EndTurn(); // the Claim is cashed for a demand of 2
        if (play.CombatDriver.PendingOptionChoice is not null)
            play.CombatDriver.SupplyOptionChoice([Decline]);
        Assert.Equal(2, OwedTo(play, queen));

        for (var i = 0; i < 2; i++)
        {
            var card = play.CombatDriver.Current!.Hand
                .First(c => c.DefinitionId.value == ActThree.MakeAmendsCardId);
            play.CombatDriver.PlayCard(card.Id, queen);
            if (play.CombatDriver.PendingOptionChoice is not null)
                play.CombatDriver.SupplyOptionChoice([0]);
        }
        Assert.True(session.Error is null, session.Error);

        play.CombatDriver.EndTurn();
        if (play.CombatDriver.PendingOptionChoice is not null)
            play.CombatDriver.SupplyOptionChoice([Decline]);

        Assert.Equal(1, FightProbe.StacksOf(Hero(play), ActThree.FavorId));
        Assert.Contains(play.CombatDriver.Current!.Hand,
            c => c.DefinitionId.value == ActThree.RightOfAudienceCardId);
        play.Dispose();
    }

    // ── Right of Audience ─────────────────────────────────────────────────────────────────────────────────

    // One favour strikes a Claim off.
    [Fact]
    public void One_favour_strikes_a_claim_off()
    {
        var probe = FightProbe.Solo(ActThree.QueenEnemyId, "count_every_buried_name", 9,
            (ActThree.ClaimId, 2));
        var withFavour = new EncounterDefinition(probe.Id, probe.Enemies, probe.HeroResources,
            [.. probe.HeroStartingStatuses ?? [],
             new StartingStatusSpec(new StatusDefinitionId(ActThree.FavorId), 3)],
            probe.HeroDisplayName, probe.CardsDrawnPerTurn, probe.TriggeredEffects);

        var (play, session, queen) = FightProbe.Start(withFavour,
            deck: [.. Enumerable.Repeat(Working, 5)], health: 900);

        Audience(play, session, option: 0, at: queen);

        Assert.Equal(1, FightProbe.StacksOf(Court(play), ActThree.ClaimId));
        Assert.Equal(2, FightProbe.StacksOf(Hero(play), ActThree.FavorId));
        play.Dispose();
    }

    // Two suspend her law for the rest of the turn; three strike her guard away and prepare the name.
    [Fact]
    public void Three_favours_speak_the_granted_name()
    {
        var probe = FightProbe.Solo(ActThree.QueenEnemyId, "count_every_buried_name", 9);
        var withFavour = new EncounterDefinition(probe.Id, probe.Enemies, probe.HeroResources,
            [.. probe.HeroStartingStatuses ?? [],
             new StartingStatusSpec(new StatusDefinitionId(ActThree.FavorId), 3)],
            probe.HeroDisplayName, probe.CardsDrawnPerTurn, probe.TriggeredEffects);

        var (play, session, queen) = FightProbe.Start(withFavour,
            deck: [.. Enumerable.Repeat(Working, 5)], health: 900);

        play.CombatDriver!.EndTurn(); // she guards
        if (play.CombatDriver.PendingOptionChoice is not null)
            play.CombatDriver.SupplyOptionChoice([Decline]);
        Assert.True(Block(Court(play)) >= 14);

        Audience(play, session, option: 2, at: queen);

        Assert.Equal(0, FightProbe.StacksOf(Hero(play), ActThree.FavorId));
        Assert.Equal(0, Block(Court(play)));
        Assert.Equal(1, FightProbe.StacksOf(Court(play), ActThree.GrantedNamePreparedId));
        Assert.Equal(1, FightProbe.StacksOf(Court(play), ActThree.GrantedNamePendingId)); // and it convenes
        play.Dispose();
    }

    // Once a turn: a second audience is refused and spends nothing.
    [Fact]
    public void The_court_hears_you_once_a_turn()
    {
        var probe = FightProbe.Solo(ActThree.QueenEnemyId, "count_every_buried_name", 9,
            (ActThree.ClaimId, 3));
        var withFavour = new EncounterDefinition(probe.Id, probe.Enemies, probe.HeroResources,
            [.. probe.HeroStartingStatuses ?? [],
             new StartingStatusSpec(new StatusDefinitionId(ActThree.FavorId), 3)],
            probe.HeroDisplayName, probe.CardsDrawnPerTurn, probe.TriggeredEffects);

        var (play, session, queen) = FightProbe.Start(withFavour,
            deck: [.. Enumerable.Repeat(Working, 5)], health: 900);

        Audience(play, session, option: 0, at: queen);
        Assert.Equal(2, FightProbe.StacksOf(Hero(play), ActThree.FavorId));

        Audience(play, session, option: 0, at: queen);
        Assert.Equal(2, FightProbe.StacksOf(Hero(play), ActThree.FavorId));
        play.Dispose();
    }

    // ── the transitions ───────────────────────────────────────────────────────────────────────────────────

    // The court convenes rather than striking, and everything is preserved across it.
    [Fact]
    public void The_court_convenes_without_a_blow()
    {
        var (play, _, _) = Start("open_the_hill_registry", [.. Enumerable.Repeat(Working, 5)],
            energy: 9, health: 250, statuses: [(ActThree.ClaimId, 1)]);

        var before = Hero(play).Health.Current;
        play.CombatDriver!.EndTurn();
        if (play.CombatDriver.PendingOptionChoice is not null)
            play.CombatDriver.SupplyOptionChoice([Decline]);

        Assert.Equal(before, Hero(play).Health.Current);
        Assert.Equal(1, FightProbe.StacksOf(Court(play), ActThree.CourtInSessionId));
        Assert.Equal(14, Block(Court(play)));
        Assert.Equal(1, FightProbe.StacksOf(Court(play), ActThree.ClaimId)); // preserved
        play.Dispose();
    }

    // "Court Standing": standing she holds while you hold no favour is worth guarding.
    [Fact]
    public void Court_standing_guards_a_queen_you_owe_nothing_to()
    {
        var (play, _, _) = Start("open_the_hill_registry", [.. Enumerable.Repeat(Working, 5)],
            energy: 9, health: null,
            statuses: [(ActThree.CourtInSessionId, 1), (ActThree.ClaimId, 1)]);

        play.CombatDriver!.EndTurn();
        if (play.CombatDriver.PendingOptionChoice is not null)
            play.CombatDriver.SupplyOptionChoice([Decline]);

        Assert.Equal(8, Block(Court(play)));
        play.Dispose();
    }

    // ── Sovereign Reciprocity ─────────────────────────────────────────────────────────────────────────────

    // In the last phase the economy cuts both ways: favour spent costs her 6 HP, once a turn.
    [Fact]
    public void Favour_spent_costs_the_sovereign()
    {
        var probe = FightProbe.Solo(ActThree.QueenEnemyId, "count_every_buried_name", 9,
            (ActThree.ClaimId, 2), (ActThree.SovereignReciprocityId, 1), (ActThree.CourtInSessionId, 1));
        var withFavour = new EncounterDefinition(probe.Id, probe.Enemies, probe.HeroResources,
            [.. probe.HeroStartingStatuses ?? [],
             new StartingStatusSpec(new StatusDefinitionId(ActThree.FavorId), 3)],
            probe.HeroDisplayName, probe.CardsDrawnPerTurn, probe.TriggeredEffects);

        var (play, session, queen) = FightProbe.Start(withFavour,
            deck: [.. Enumerable.Repeat(Working, 5)], health: 900);

        var health = Court(play).Health.Current;
        Audience(play, session, option: 0, at: queen);

        Assert.Equal(health - 6, Court(play).Health.Current);
        play.Dispose();
    }

    // And standing she spends guards YOU.
    [Fact]
    public void Standing_she_spends_guards_you()
    {
        var (play, _, _) = Start("call_in_the_gift", [.. Enumerable.Repeat(Working, 5)],
            energy: 9, health: null,
            statuses: [(ActThree.ClaimId, 1), (ActThree.SovereignReciprocityId, 1),
                       (ActThree.CourtInSessionId, 1)]);

        play.CombatDriver!.EndTurn();
        if (play.CombatDriver.PendingOptionChoice is not null)
            play.CombatDriver.SupplyOptionChoice([Decline]);

        // The guard is given as her action resolves, which is inside her own turn — so it is read off the
        // record rather than off the player's board, where the next bell has already swept it away.
        var guarded = play.CombatDriver.Current!.State.CombatLog
            .Where(e => e.Type == StandardCombatLogTypes.BlockGained)
            .Select(e => e.Message).ToList();
        Assert.Contains(guarded, m => m.Contains("4 block on 'hero'", StringComparison.Ordinal));
        Assert.Equal(0, FightProbe.StacksOf(Court(play), ActThree.ClaimId));
        play.Dispose();
    }

    // ── Hill Court Final Order ────────────────────────────────────────────────────────────────────────────

    // "22 +4 per Claim +2 per open Wergild point, to a maximum of 34 — and 8 less once her name has been
    // spoken."
    [Fact]
    public void The_final_order_is_lighter_for_a_name_already_spoken()
    {
        var (plain, _, _) = Start("hill_court_final_order", [.. Enumerable.Repeat(Working, 5)],
            energy: 0, health: 55,
            statuses: [(ActThree.ClaimId, 2), (ActThree.SovereignReciprocityId, 1),
                       (ActThree.CourtInSessionId, 1)]);
        var before = Hero(plain).Health.Current;
        plain.CombatDriver!.EndTurn();
        Assert.Equal(before - 30, Hero(plain).Health.Current);
        Assert.Equal(0, FightProbe.StacksOf(Court(plain), ActThree.ClaimId));
        plain.Dispose();

        var (named, _, _) = Start("hill_court_final_order", [.. Enumerable.Repeat(Working, 5)],
            energy: 0, health: 55,
            statuses: [(ActThree.ClaimId, 2), (ActThree.SovereignReciprocityId, 1),
                       (ActThree.CourtInSessionId, 1), (ActThree.GrantedNamePreparedId, 1)]);
        var start = Hero(named).Health.Current;
        named.CombatDriver!.EndTurn();
        Assert.Equal(start - 22, Hero(named).Health.Current);
        Assert.Equal(0, FightProbe.StacksOf(Court(named), ActThree.GrantedNamePreparedId));
        named.Dispose();
    }

    // Until she is nearly spent, the slot is an ordinary blow.
    [Fact]
    public void The_final_order_waits_for_the_court_to_be_nearly_spent()
    {
        var (play, _, _) = Start("hill_court_final_order", [.. Enumerable.Repeat(Working, 5)], energy: 0);

        var before = Hero(play).Health.Current;
        play.CombatDriver!.EndTurn();

        Assert.Equal(before - 18, Hero(play).Health.Current);
        play.Dispose();
    }
}
