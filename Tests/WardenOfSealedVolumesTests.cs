using BnbContent.Converter;
using BnbContent.Converter.Bosses;
using RogueDeck.Run;
using RogueDeck.Core.Combat;
using RogueDeck.Sandbox.Composition;
using RogueDeck.Sandbox.Run;
using RogueDeck.Scenario.Authoring;

namespace BnbContent.Tests;

// "The card is still yours. The right to use it is not." The Warden takes a card out of every normal zone and
// gives it back only on that Seal's own terms — and when it does, it comes back free and stays in hand. These
// tests seal a volume three ways, buy it back three ways, and check what the keys cost the Warden.
public class WardenOfSealedVolumesTests
{
    private const string Deed = "paper_cut";
    private const string Working = "strong_binder";

    private static CombatantState Warden(RunPlayback play, CombatantId id) =>
        play.CombatDriver!.Current!.State.GetCombatant(id);

    private static bool Has(RunPlayback play, CombatantId id, string status) =>
        Warden(play, id).Statuses.Any(s => s.DefinitionId == new StatusDefinitionId(status));

    private static int Custody(RunPlayback play, CombatantId id) =>
        FightProbe.StacksOf(Warden(play, id), WardenOfSealedVolumes.CustodyId);

    private static int Guard(CombatantState c) =>
        c.DefensivePools.TryGetValue(StandardCombatIds.BlockDefensivePool, out var pool) ? pool.Current : 0;

    private static IReadOnlyList<CardInstance> Hand(RunPlayback play) => play.CombatDriver!.Current!.Hand;

    // A sealed volume is in the Banished pile: out of every normal zone, still there to look at.
    private static int SealedAway(RunPlayback play, CombatantId _) =>
        play.CombatDriver!.Current!.State.GetCardZones(play.CombatDriver.Current.HeroId)
            .GetCardsInZone(CardZone.BanishedPile).Count;

    private static (RunPlayback Play, InteractiveRunSession Session, CombatantId Id) Fight(
        string intent, int? bossHealth = null)
    {
        var probe = bossHealth is { } hp
            ? FightProbe.Roster("warden_of_sealed_volumes", energy: 9,
                (WardenOfSealedVolumes.EnemyId, intent, hp))
            : FightProbe.Solo(WardenOfSealedVolumes.EnemyId, intent, 9);

        return FightProbe.Start(probe,
            deck: [.. Enumerable.Repeat(Deed, 12), .. Enumerable.Repeat(Working, 12)],
            health: 600);
    }

    private static (RunPlayback Play, InteractiveRunSession Session, CombatantId Id) Cycle(
        int? bossHealth, params string[] intents)
    {
        var probe = FightProbe.Solo(WardenOfSealedVolumes.EnemyId, intents[0], 9);
        var body = probe.Enemies.Single() with
        {
            Actions = [.. intents.Select(i => new EnemyActionDefinitionId($"{WardenOfSealedVolumes.EnemyId}.{i}"))],
            MaxHealth = bossHealth ?? probe.Enemies.Single().MaxHealth,
        };

        return FightProbe.Start(
            new EncounterDefinition(probe.Id, [body], probe.HeroResources, probe.HeroStartingStatuses,
                probe.HeroDisplayName, probe.CardsDrawnPerTurn, probe.TriggeredEffects),
            deck: [.. Enumerable.Repeat(Deed, 12), .. Enumerable.Repeat(Working, 12)],
            health: 600);
    }

    private static void Play(RunPlayback play, CombatantId at, string definitionId)
    {
        var card = Hand(play).FirstOrDefault(c => c.DefinitionId.value == definitionId);
        Assert.True(card is not null, $"the probe hand held no {definitionId}");
        play.CombatDriver!.PlayCard(card!.Id, at);
    }

    private static void PlayAny(RunPlayback play, CombatantId at)
    {
        var card = Hand(play).FirstOrDefault();
        Assert.True(card is not null, "the probe hand was empty");
        play.CombatDriver!.PlayCard(card!.Id, at);
    }

    private static void EndTurn(RunPlayback play, int option = 0)
    {
        play.CombatDriver!.EndTurn();
        if (play.CombatDriver.PendingOptionChoice is not null)
            play.CombatDriver.SupplyOptionChoice([option]);
    }

    // §6.2/§6.3: the Seal intent calls for the sealing, and it resolves after the player's next normal draw —
    // when there is a hand to reach into. The volume leaves the hand entirely and Custody goes up.
    [Fact]
    public void It_takes_one_volume_out_of_every_normal_zone()
    {
        var (play, _, warden) = Fight("seal_the_principal_instrument");

        Assert.Equal(0, Custody(play, warden));
        Assert.Equal(0, SealedAway(play, warden));

        // Its first action calls for the sealing; the reaching happens on the next draw.
        EndTurn(play);

        Assert.Equal(1, Custody(play, warden));
        Assert.Equal(1, SealedAway(play, warden));
        Assert.True(Has(play, warden, WardenOfSealedVolumes.SealOfRestraintId),
            "the first key it reaches for is the Seal of Restraint");
    }

    // §6.3: the player chooses WHICH volume is surrendered — the whole point of offering two candidates
    // rather than sniping one deterministically.
    [Fact]
    public void Which_volume_is_surrendered_is_the_players_choice()
    {
        var (playA, _, wardenA) = Fight("seal_the_principal_instrument");
        EndTurn(playA, option: 0);
        var first = playA.CombatDriver!.Current!.State
            .GetCardZones(playA.CombatDriver.Current.HeroId)
            .GetCardsInZone(CardZone.BanishedPile).Single().Id;

        var (playB, _, wardenB) = Fight("seal_the_principal_instrument");
        EndTurn(playB, option: 1);
        var second = playB.CombatDriver!.Current!.State
            .GetCardZones(playB.CombatDriver.Current.HeroId)
            .GetCardsInZone(CardZone.BanishedPile).Single().Id;

        Assert.Equal(1, Custody(playA, wardenA));
        Assert.Equal(1, Custody(playB, wardenB));
        // The two runs are the same fight with the same seed, and the two candidates are often two copies of
        // the same card — so it is the INSTANCE that proves the answer took effect, not the name.
        Assert.NotEqual(first, second);
    }

    // §6.4: the Seal of Restraint opens on a quiet turn — no more than 2 cards played — and gives its volume
    // back at the START of the next player turn, which is what it costs over the other two keys.
    [Fact]
    public void Restraint_gives_the_volume_back_after_a_quiet_turn()
    {
        var (play, _, warden) = Fight("seal_the_principal_instrument");

        EndTurn(play);
        Assert.True(Has(play, warden, WardenOfSealedVolumes.SealOfRestraintId));
        Assert.Equal(1, SealedAway(play, warden));

        // A quiet turn: two cards and no more.
        PlayAny(play, warden);
        PlayAny(play, warden);
        EndTurn(play);

        Assert.Equal(0, Custody(play, warden));
        Assert.Equal(0, SealedAway(play, warden));
        Assert.False(Has(play, warden, WardenOfSealedVolumes.SealOfRestraintId));
    }

    // …and a busy turn does not open it. The condition is a real condition.
    [Fact]
    public void A_busy_turn_leaves_the_volume_where_it_is()
    {
        var (play, _, warden) = Fight("seal_the_principal_instrument");

        EndTurn(play);
        Assert.Equal(1, SealedAway(play, warden));

        for (var i = 0; i < 4 && Hand(play).Count > 0; i++)
            PlayAny(play, warden);
        EndTurn(play);

        Assert.Equal(1, Custody(play, warden));
        Assert.Equal(1, SealedAway(play, warden));
    }

    // §6.4: "For that turn: Retain; Cost 0." The volume comes back free and is still in hand at the end of
    // the turn it came back on — which is exactly what the per-instance retain mark buys, and what neither
    // the definition flag nor the retain-hand status could have said about one card.
    [Fact]
    public void The_volume_comes_back_free_and_stays_for_the_turn()
    {
        // One sealing action and then two quiet ones, so the turn the volume comes back on AND the turn after
        // it are turns the Warden is not reaching into the hand again. Without that the retention is real but
        // invisible: the volume simply goes straight back into custody.
        var (play, _, warden) = Cycle(null,
            "seal_the_principal_instrument", "deny_immediate_access", "deny_immediate_access");

        EndTurn(play);
        var surrendered = play.CombatDriver!.Current!.State
            .GetCardZones(play.CombatDriver.Current.HeroId)
            .GetCardsInZone(CardZone.BanishedPile).Single().Id;

        // A quiet turn opens the Restraint seal at the start of the next one.
        PlayAny(play, warden);
        PlayAny(play, warden);
        EndTurn(play);

        var returned = Hand(play).FirstOrDefault(c => c.Id == surrendered);
        Assert.True(returned is not null, "the volume never came back to hand");
        Assert.True(returned!.Marks.Contains(StandardCombatIds.RetainedCardMark),
            "the returned volume was not retained");

        // Cost 0 for the turn: the whole printed cost taken off this one copy.
        Assert.Equal(-1, returned.MarkCounters.TryGetValue(StandardCombatIds.CardCostDeltaCounter, out var d)
            ? d : 0);

        // End the turn without playing it — retained means retained.
        EndTurn(play);
        Assert.Contains(Hand(play), c => c.Id == surrendered);
    }

    // §6.5: the Seal of Procedure opens the moment a second KIND of card is played, and returns its volume at
    // once rather than next turn.
    [Fact]
    public void Procedure_opens_on_the_second_kind_of_card()
    {
        var (play, _, warden) = Fight("seal_the_principal_instrument");

        // Let the Restraint key be used up first, so the Warden reaches for Procedure next.
        EndTurn(play);
        PlayAny(play, warden);
        PlayAny(play, warden);
        EndTurn(play);
        Assert.Equal(0, Custody(play, warden));

        // Round the cycle until it seals again — this time under Procedure.
        for (var turn = 0; turn < 6 && !Has(play, warden, WardenOfSealedVolumes.SealOfProcedureId); turn++)
        {
            for (var i = 0; i < 3 && Hand(play).Count > 0; i++)
                PlayAny(play, warden);
            EndTurn(play);
        }

        Assert.True(Has(play, warden, WardenOfSealedVolumes.SealOfProcedureId),
            "the Warden never reached for the Seal of Procedure");
        Assert.Equal(1, SealedAway(play, warden));

        // One kind is not enough; the second kind is the key.
        Play(play, warden, Deed);
        Assert.Equal(1, SealedAway(play, warden));

        Play(play, warden, Working);
        Assert.Equal(0, SealedAway(play, warden));
        Assert.Equal(0, Custody(play, warden));
    }

    // §6.6: the Seal of Evidence hangs on a citation. The Warden marks a card in the hand it did NOT take —
    // a sealed card cannot be cited, because it is not in the hand to be cited — and playing that card is the
    // key. This is the third and last lock, and the fight only reaches it by opening the first two.
    [Fact]
    public void Evidence_opens_by_answering_the_citation()
    {
        // The real cycle: it inspects, it seals, it strikes. Inspecting is what moves the announcement on, so
        // a probe without it would stay on the first key forever.
        var (play, _, warden) = Cycle(null,
            "inspect_the_claim", "seal_the_principal_instrument", "deny_immediate_access");

        // Open each lock as it comes, which is the only way the rotation reaches the third.
        for (var turn = 0; turn < 20 && !Has(play, warden, WardenOfSealedVolumes.SealOfEvidenceId); turn++)
        {
            if (Has(play, warden, WardenOfSealedVolumes.SealOfProcedureId))
            {
                // Two different kinds in one turn.
                Play(play, warden, Deed);
                Play(play, warden, Working);
            }
            else if (Has(play, warden, WardenOfSealedVolumes.SealOfRestraintId))
            {
                // A quiet turn: no more than two cards.
                PlayAny(play, warden);
            }
            else
            {
                PlayAny(play, warden);
                PlayAny(play, warden);
            }

            EndTurn(play);
        }

        Assert.True(Has(play, warden, WardenOfSealedVolumes.SealOfEvidenceId),
            "the Warden never reached for the Seal of Evidence");
        Assert.Equal(1, SealedAway(play, warden));

        // The citation lands on a card still in hand — never on the volume it is holding.
        var cited = Hand(play).FirstOrDefault(c =>
            c.Marks.Contains(new TagId(WardenOfSealedVolumes.WardenReferenceMark)));
        Assert.True(cited is not null, "the Warden sealed by Evidence but cited nothing");

        play.CombatDriver!.PlayCard(cited!.Id, warden);

        Assert.Equal(0, SealedAway(play, warden));
        Assert.False(Has(play, warden, WardenOfSealedVolumes.SealOfEvidenceId));
    }

    // §Transition: at 135 HP Total Lockdown opens the second slot, takes 16 Block and does not attack.
    [Fact]
    public void Total_lockdown_opens_the_second_slot_without_attacking()
    {
        var (play, _, warden) = Fight("deny_immediate_access", bossHealth: 130);

        Assert.False(Has(play, warden, WardenOfSealedVolumes.TotalLockdownId));
        var mine = play.CombatDriver!.Current!.State.GetCombatant(play.CombatDriver.Current.HeroId)
            .Health.Current;

        EndTurn(play);

        Assert.True(Has(play, warden, WardenOfSealedVolumes.TotalLockdownId));
        Assert.Equal(mine, play.CombatDriver!.Current!.State
            .GetCombatant(play.CombatDriver.Current.HeroId).Health.Current);
    }

    // §Phase II passive — Keys Turn Against the Lock: once the Lockdown is on, a correct release costs the
    // Warden 8 HP outright. This is the counterplay the design points at: release deliberately, repeatedly.
    [Fact]
    public void Under_lockdown_every_key_turned_costs_the_warden()
    {
        var (play, _, warden) = Fight("seal_the_principal_instrument", bossHealth: 130);

        // The first action is the Lockdown itself; the second calls for a sealing.
        EndTurn(play);
        Assert.True(Has(play, warden, WardenOfSealedVolumes.TotalLockdownId));

        EndTurn(play);
        Assert.Equal(1, Custody(play, warden));

        var health = Warden(play, warden).Health.Current;

        // A quiet turn opens the Restraint seal at the start of the next one.
        PlayAny(play, warden);
        EndTurn(play);

        // The single-intent probe reaches for another volume on the very next draw, so Custody says nothing
        // here — what the key cost is written on the Warden's body, where no Block can hide it.
        Assert.True(Warden(play, warden).Health.Current <= health - 8,
            "turning the key cost the Warden nothing");
    }

    // §6.7: no third slot exists. Two volumes is everything it can hold.
    [Fact]
    public void It_never_holds_more_than_two_volumes()
    {
        var (play, _, warden) = Fight("seal_the_principal_instrument", bossHealth: 130);

        // Play busily throughout so no Release Condition is ever met, and let the cycle keep sealing.
        for (var turn = 0; turn < 10; turn++)
        {
            for (var i = 0; i < 4 && Hand(play).Count > 0; i++)
                PlayAny(play, warden);
            EndTurn(play);
            Assert.True(Custody(play, warden) <= WardenOfSealedVolumes.CustodyMaximum,
                $"the Warden held {Custody(play, warden)} volumes");
        }
    }
}
