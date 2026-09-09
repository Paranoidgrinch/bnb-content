using BnbContent.Converter;
using RogueDeck.Core.Combat;
using RogueDeck.Sandbox.Composition;
using RogueDeck.Run;
using RogueDeck.Sandbox.Run;
using RogueDeck.Scenario.Authoring;

namespace BnbContent.Tests;

// ACT V, the fourth god — Nanna-Sin, Lord of the Counted Moon, proved in live fights.
//
// The tests follow the ring: where the moon stands, which card the count takes, when that card comes back,
// what it costs when it does, how long it lasts, what he brings back of his own, and what a held night buys.
public class ActFiveBossNannaSinTests
{
    private const string Cut = "paper_cut";   // Deed, 1 Energy: deal 6

    private static CombatantState Hero(RunPlayback play) =>
        play.CombatDriver!.Current!.State.GetCombatant(play.CombatDriver.Current!.HeroId);

    private static CombatantState Lord(RunPlayback play) =>
        play.CombatDriver!.Current!.State.Combatants
            .First(c => c.DefinitionId.value.Contains("nanna_sin", StringComparison.Ordinal));

    private static int Stacks(CombatantState body, string status) => FightProbe.StacksOf(body, status);

    private static IReadOnlyList<CardInstance> Hand(RunPlayback play) => play.CombatDriver!.Current!.Hand;

    private static IReadOnlyList<CardInstance> Zone(RunPlayback play, CardZone zone)
    {
        var combat = play.CombatDriver!.Current!;
        return [.. combat.State.GetCardZones(combat.HeroId).GetCardsInZone(zone)];
    }

    private static bool Marked(CardInstance card, string mark) => card.HasMark(new TagId(mark));

    private static IEnumerable<CardInstance> Everywhere(RunPlayback play) =>
        new[] { CardZone.Hand, CardZone.DrawPile, CardZone.DiscardPile, CardZone.ExhaustPile }
            .SelectMany(z => Zone(play, z));

    private static void Play(RunPlayback play, CardInstance card, CombatantId? target) =>
        play.CombatDriver!.PlayCard(card.Id, target);

    private static void PlayOne(RunPlayback play, CombatantId enemy, string cardId = Cut) =>
        Play(play, Hand(play).First(c => c.DefinitionId.value == cardId), enemy);

    // His whole fight is the ring, and the ring is driven by his own intent rules — so a probe narrowed to
    // one action still walks the eight phases exactly as the real fight does. The health buys the rounds a
    // full orbit needs; this is a mechanism probe, not a balance sample.
    private static (RunPlayback Play, InteractiveRunSession Session, CombatantId EnemyId) Calendar(
        int energy = 4, int deck = 24, int health = 9000)
    {
        var probe = FightProbe.Solo(ActFive.NannaSinEnemyId, "count_the_unseen", energy);
        return FightProbe.Start(probe, deck: [.. Enumerable.Repeat(Cut, deck)], health: health);
    }

    // The calendar with his intent RULES taken off: he plays one move every night whatever the phase says,
    // which is the only way to read what a Returning Move adds without the rest of the ring's debuffs
    // (Paperwork ticks at the bearer's turn end; Doubt eats an attack) sitting on top of the reading.
    private static (RunPlayback Play, CombatantId EnemyId) OneMoveEveryNight(string intentId)
    {
        var probe = FightProbe.Solo(ActFive.NannaSinEnemyId, intentId, 4);
        var unruled = new EncounterDefinition(
            probe.Id, [probe.Enemies[0] with { IntentRules = [] }], probe.HeroResources,
            probe.HeroStartingStatuses, probe.HeroDisplayName, probe.CardsDrawnPerTurn, probe.TriggeredEffects);
        var (play, _, enemy) = FightProbe.Start(
            unruled, deck: [.. Enumerable.Repeat(Cut, 24)], health: 9000);
        return (play, enemy);
    }

    // Walk the calendar to a named phase, leaving the player's turn of that night open.
    private static (RunPlayback Play, CombatantId EnemyId) At(int phase, int energy = 4, int deck = 24)
    {
        var (play, _, enemy) = Calendar(energy, deck);
        while (Stacks(Lord(play), ActFive.LunarPhaseId) != phase)
            play.CombatDriver!.EndTurn();
        return (play, enemy);
    }

    // ── the ring ──────────────────────────────────────────────────────────────────────────────────────────

    // One phase a round, in order, carrying `— I II III IV III II I`, and the sky comes round on the ninth.
    [Fact]
    public void The_ring_turns_one_phase_a_round_and_carries_the_counts()
    {
        int[] counts = [0, 1, 2, 3, 4, 3, 2, 1];
        var (play, _, _) = Calendar();

        for (var night = 0; night < 17; night++)
        {
            var phase = night % 8 + 1;
            Assert.Equal(phase, Stacks(Lord(play), ActFive.LunarPhaseId));
            Assert.Equal(counts[night % 8], Stacks(Lord(play), ActFive.LunarCountId));
            Assert.Equal(night / 8 + 1, Stacks(Lord(play), ActFive.OrbitId));
            play.CombatDriver!.EndTurn();
        }

        play.Dispose();
    }

    // The New Moon has no count, so it takes no card — and nothing at all comes back on it.
    [Fact]
    public void The_new_moon_counts_nothing()
    {
        var (play, enemy) = At(1);

        PlayOne(play, enemy);
        PlayOne(play, enemy);
        Assert.DoesNotContain(Everywhere(play), c => Marked(c, ActFive.CountedMark));
        Assert.DoesNotContain(Hand(play), c => Marked(c, ActFive.EchoMark));
        play.Dispose();
    }

    // §9.2: at `First Quarter — II` the SECOND card played is Counted, and it is the second and no other.
    [Fact]
    public void The_count_takes_the_card_played_on_it()
    {
        var (play, enemy) = At(3);
        Assert.Equal(2, Stacks(Lord(play), ActFive.LunarCountId));

        PlayOne(play, enemy);
        Assert.DoesNotContain(Everywhere(play), c => Marked(c, ActFive.CountedMark));

        PlayOne(play, enemy);
        var counted = Assert.Single(Everywhere(play), c => Marked(c, ActFive.CountedMark));
        Assert.Equal(2, counted.GetMarkCounter(new CounterId("nanna_sin_counted_at")));

        // …and a third play does not take the count a second time.
        PlayOne(play, enemy);
        Assert.Single(Everywhere(play), c => Marked(c, ActFive.CountedMark));
        play.Dispose();
    }

    // ── the count returns ─────────────────────────────────────────────────────────────────────────────────

    // §9.3, the audit's revision, and the whole reason the mechanic bites inside the first orbit: III is
    // recorded at the Waxing Gibbous and returns at the Waning Gibbous, two nights later, in the SAME orbit.
    // §9.4: the copy is free, it is the card it was, and the player is the one who decides to play it.
    [Fact]
    public void A_counted_card_comes_back_free_when_the_count_comes_round_again()
    {
        var (play, enemy) = At(4);
        Assert.Equal(3, Stacks(Lord(play), ActFive.LunarCountId));

        for (var i = 0; i < 3; i++)
            PlayOne(play, enemy);
        Assert.Single(Everywhere(play), c => Marked(c, ActFive.CountedMark));

        // The Full Moon in between takes its own count and returns nothing of the player's: the last IV was
        // never recorded.
        play.CombatDriver!.EndTurn();
        Assert.Equal(5, Stacks(Lord(play), ActFive.LunarPhaseId));
        Assert.DoesNotContain(Hand(play), c => Marked(c, ActFive.EchoMark));

        play.CombatDriver!.EndTurn();
        Assert.Equal(6, Stacks(Lord(play), ActFive.LunarPhaseId));
        Assert.Equal(3, Stacks(Lord(play), ActFive.LunarCountId));

        var echo = Assert.Single(Hand(play), c => Marked(c, ActFive.EchoMark));
        Assert.Equal(Cut, echo.DefinitionId.value);
        // Free: the whole printed cost taken off this one copy.
        Assert.Equal(-1, echo.GetMarkCounter(StandardCombatIds.CardCostDeltaCounter));
        // And the record is spent by being answered — the same count is free to take a new card tonight.
        Assert.DoesNotContain(Everywhere(play), c => Marked(c, ActFive.CountedMark));
        play.Dispose();
    }

    // §9.4: it disappears at turn end. An Echo that survived the night would be a permanent addition to a
    // deck that never bought it — so it is swept from wherever the turn's end left it.
    [Fact]
    public void An_echo_belongs_to_its_own_night()
    {
        var (play, enemy) = At(4);
        for (var i = 0; i < 3; i++)
            PlayOne(play, enemy);
        play.CombatDriver!.EndTurn();
        play.CombatDriver!.EndTurn();
        Assert.Single(Hand(play), c => Marked(c, ActFive.EchoMark));

        var copiesBefore = Everywhere(play).Count(c => c.DefinitionId.value == Cut);
        play.CombatDriver!.EndTurn();
        Assert.DoesNotContain(Everywhere(play), c => Marked(c, ActFive.EchoMark));
        Assert.Equal(copiesBefore - 1, Everywhere(play).Count(c => c.DefinitionId.value == Cut));
        play.Dispose();
    }

    // The other half of §9.3: a card counted on the WANING side points at the next orbit, so I recorded at
    // the Waning Crescent comes back at the Waxing Crescent — six nights on and one sky later.
    [Fact]
    public void A_count_taken_on_the_waning_side_returns_in_the_next_orbit()
    {
        var (play, enemy) = At(8);
        Assert.Equal(1, Stacks(Lord(play), ActFive.LunarCountId));
        PlayOne(play, enemy);
        Assert.Single(Everywhere(play), c => Marked(c, ActFive.CountedMark));

        play.CombatDriver!.EndTurn();
        Assert.Equal(1, Stacks(Lord(play), ActFive.LunarPhaseId));
        Assert.Equal(2, Stacks(Lord(play), ActFive.OrbitId));
        Assert.DoesNotContain(Hand(play), c => Marked(c, ActFive.EchoMark));

        play.CombatDriver!.EndTurn();
        Assert.Equal(2, Stacks(Lord(play), ActFive.LunarPhaseId));
        Assert.Single(Hand(play), c => Marked(c, ActFive.EchoMark));
        play.Dispose();
    }

    // ── his own recurrence ────────────────────────────────────────────────────────────────────────────────

    // §9.5. The ring is fixed, so the move that returns is always the move of the phase opposite this one —
    // and it is on a chip BEFORE it lands, because the telegraph can only name tonight's intent and a boss
    // that deals half its damage off the telegraph is the hidden rule this act forbids.
    [Fact]
    public void What_he_brings_back_is_on_the_table_before_it_lands()
    {
        var (play, _, _) = Calendar();

        // Orbit I, waxing: the nights those counts point at have not happened yet, so nothing returns.
        foreach (var phase in new[] { 1, 2, 3, 4, 5 })
        {
            Assert.Equal(phase, Stacks(Lord(play), ActFive.LunarPhaseId));
            Assert.Equal(0, Stacks(Lord(play), ActFive.ReturningMoveId));
            play.CombatDriver!.EndTurn();
        }

        // Waning Gibbous returns Fill the Measure (26) at 55 %.
        Assert.Equal(6, Stacks(Lord(play), ActFive.LunarPhaseId));
        Assert.Equal(26 * 55 / 100, Stacks(Lord(play), ActFive.ReturningMoveId));
        play.CombatDriver!.EndTurn();
        // Last Quarter returns Divide the Night (24).
        Assert.Equal(24 * 55 / 100, Stacks(Lord(play), ActFive.ReturningMoveId));
        play.CombatDriver!.EndTurn();
        // Waning Crescent returns Raise the Horns (20).
        Assert.Equal(20 * 55 / 100, Stacks(Lord(play), ActFive.ReturningMoveId));

        // Orbit II: the waxing half has a night behind it now, so everything returns.
        play.CombatDriver!.EndTurn();
        play.CombatDriver!.EndTurn();
        Assert.Equal(2, Stacks(Lord(play), ActFive.LunarPhaseId));
        Assert.Equal(2, Stacks(Lord(play), ActFive.OrbitId));
        Assert.Equal(18 * 55 / 100, Stacks(Lord(play), ActFive.ReturningMoveId));
        play.Dispose();
    }

    // The chip is not decoration: what it says is what he adds. Read the blow on a night that returns
    // nothing against the same blow one orbit later, when the same phase has a night behind it.
    [Fact]
    public void The_move_that_returns_is_damage_the_chip_already_named()
    {
        var (play, _) = OneMoveEveryNight("fill_the_measure");

        int Struck()
        {
            var before = Hero(play).Health.Current;
            play.CombatDriver!.EndTurn();
            return before - Hero(play).Health.Current;
        }

        while (Stacks(Lord(play), ActFive.LunarPhaseId) != 4)
            play.CombatDriver!.EndTurn();
        Assert.Equal(0, Stacks(Lord(play), ActFive.ReturningMoveId));
        var young = Struck();          // Fill the Measure, and nothing behind it yet

        while (!(Stacks(Lord(play), ActFive.LunarPhaseId) == 4 && Stacks(Lord(play), ActFive.OrbitId) == 2))
            play.CombatDriver!.EndTurn();
        var promised = Stacks(Lord(play), ActFive.ReturningMoveId);
        Assert.True(promised > 0, "the second orbit's Waxing Gibbous has a night behind it");
        Assert.Equal(young + promised, Struck());
        play.Dispose();
    }

    // §9.6. The Full Moon amplifies both sides: an Echo played tonight happens a second time at half, and
    // the chip beside him says he is returning MORE than the ring's ordinary 55 %.
    [Fact]
    public void The_full_moon_reflects_what_it_returns()
    {
        // Off the ring's own rules, because Pour Away the Light leaves Doubt on the player and Doubt eats an
        // attack — which would be measuring the reflection through a debuff instead of measuring it.
        var (play, enemy) = OneMoveEveryNight("crown_of_ur");
        while (Stacks(Lord(play), ActFive.LunarPhaseId) != 5)
            play.CombatDriver!.EndTurn();

        Assert.Equal(1, Stacks(Hero(play), ActFive.PerfectReflectionId));
        // Crown of Ur returns at 70 rather than the ring's 55 — but only from the second sky.
        Assert.Equal(0, Stacks(Lord(play), ActFive.ReturningMoveId));

        // Record a IV, and come back to the Full Moon a whole orbit later.
        for (var i = 0; i < 4; i++)
            PlayOne(play, enemy);
        while (!(Stacks(Lord(play), ActFive.LunarPhaseId) == 5 && Stacks(Lord(play), ActFive.OrbitId) == 2))
            play.CombatDriver!.EndTurn();

        Assert.Equal(34 * 70 / 100, Stacks(Lord(play), ActFive.ReturningMoveId));
        var echo = Assert.Single(Hand(play), c => Marked(c, ActFive.EchoMark));

        // An ordinary Paper Cut is 6. Under Perfect Reflection the Echo resolves again at a half.
        var before = Lord(play).Health.Current;
        Play(play, echo, enemy);
        Assert.Equal(6 + 3, before - Lord(play).Health.Current);
        play.Dispose();
    }

    // Orbit III raises the whole ring at once (§9.9), which is the only thing that changes about his numbers
    // until the Unending Month.
    [Fact]
    public void The_old_moon_returns_harder()
    {
        var (play, _, _) = Calendar();
        while (!(Stacks(Lord(play), ActFive.LunarPhaseId) == 6 && Stacks(Lord(play), ActFive.OrbitId) == 3))
            play.CombatDriver!.EndTurn();

        Assert.Equal(1, Stacks(Lord(play), ActFive.OldMoonId));
        Assert.Equal(26 * 75 / 100, Stacks(Lord(play), ActFive.ReturningMoveId));
        play.Dispose();
    }

    // §9.9's soft enrage: from the fourth orbit each new sky leaves one Radiance on him, and nothing takes
    // it off. It is one per ORBIT and not one per night.
    [Fact]
    public void The_unending_month_leaves_light_on_him()
    {
        var (play, _, _) = Calendar();
        while (Stacks(Lord(play), ActFive.OrbitId) < 4)
            play.CombatDriver!.EndTurn();

        Assert.Equal(1, Stacks(Lord(play), ActFive.UnendingMonthId));
        Assert.Equal(1, Stacks(Lord(play), ActFive.RadianceId));
        for (var night = 0; night < 8; night++)
            play.CombatDriver!.EndTurn();
        Assert.Equal(5, Stacks(Lord(play), ActFive.OrbitId));
        Assert.Equal(2, Stacks(Lord(play), ActFive.RadianceId));
        play.Dispose();
    }

    // ── the intercalary seal ──────────────────────────────────────────────────────────────────────────────

    // §9.8. One seal an orbit, offered every morning it is unspent, and it buys the same night again — for
    // the player and for him.
    [Fact]
    public void Holding_the_moon_buys_the_same_night_again()
    {
        var (play, enemy) = At(3);
        Assert.Equal(1, Stacks(Hero(play), ActFive.IntercalarySealId));
        Assert.Contains(ActFive.HoldTheMoonCardId, Hand(play).Select(c => c.DefinitionId.value));

        PlayOne(play, enemy, ActFive.HoldTheMoonCardId);
        Assert.Equal(0, Stacks(Hero(play), ActFive.IntercalarySealId));
        Assert.Equal(1, Stacks(Lord(play), ActFive.MoonHeldId));

        play.CombatDriver!.EndTurn();
        Assert.Equal(3, Stacks(Lord(play), ActFive.LunarPhaseId));
        Assert.Equal(0, Stacks(Lord(play), ActFive.MoonHeldId));
        // The seal is spent, so the sheet is not laid out again this orbit.
        Assert.DoesNotContain(ActFive.HoldTheMoonCardId, Hand(play).Select(c => c.DefinitionId.value));

        // …and the calendar goes on from the night it was held on, one night behind the round.
        play.CombatDriver!.EndTurn();
        Assert.Equal(4, Stacks(Lord(play), ActFive.LunarPhaseId));
        play.Dispose();
    }

    // "You buy time now by creating an additional future recurrence" — and with the next occurrence of the
    // count now being the very next night, what was counted comes straight back.
    [Fact]
    public void A_held_night_brings_the_count_straight_back()
    {
        var (play, enemy) = At(3);
        PlayOne(play, enemy);
        PlayOne(play, enemy);
        Assert.Single(Everywhere(play), c => Marked(c, ActFive.CountedMark));
        PlayOne(play, enemy, ActFive.HoldTheMoonCardId);

        play.CombatDriver!.EndTurn();
        Assert.Equal(3, Stacks(Lord(play), ActFive.LunarPhaseId));
        Assert.Single(Hand(play), c => Marked(c, ActFive.EchoMark));
        play.Dispose();
    }

    // A new orbit is a new seal — and a held night inside one orbit does not buy a second.
    [Fact]
    public void The_seal_is_one_an_orbit()
    {
        var (play, enemy) = At(2);
        PlayOne(play, enemy, ActFive.HoldTheMoonCardId);
        for (var night = 0; night < 7; night++)
        {
            play.CombatDriver!.EndTurn();
            Assert.Equal(0, Stacks(Hero(play), ActFive.IntercalarySealId));
        }

        // …and the next end of turn is the one that opens the second sky.
        play.CombatDriver!.EndTurn();
        Assert.Equal(1, Stacks(Lord(play), ActFive.LunarPhaseId));
        Assert.Equal(2, Stacks(Lord(play), ActFive.OrbitId));
        Assert.Equal(1, Stacks(Hero(play), ActFive.IntercalarySealId));
        play.Dispose();
    }

    // ── what cannot be counted ────────────────────────────────────────────────────────────────────────────

    // An Echo is banished at the end of the night it came back on, so a COUNTED stamp on one is a promise the
    // calendar cannot keep. It is passed over — and it does not take the count from the card that can be kept.
    [Fact]
    public void An_echo_cannot_be_counted_and_does_not_take_the_count()
    {
        var (play, enemy) = At(4);
        for (var i = 0; i < 3; i++)
            PlayOne(play, enemy);
        play.CombatDriver!.EndTurn();
        play.CombatDriver!.EndTurn();

        // Waning Gibbous: the Echo is back, and the count is III again.
        Assert.Equal(6, Stacks(Lord(play), ActFive.LunarPhaseId));
        Assert.Equal(3, Stacks(Lord(play), ActFive.LunarCountId));
        var echo = Assert.Single(Hand(play), c => Marked(c, ActFive.EchoMark));

        Play(play, echo, enemy);
        PlayOne(play, enemy);
        PlayOne(play, enemy);
        // Three cards played, but the Echo was not one that could be kept — so nothing is Counted yet.
        Assert.DoesNotContain(Everywhere(play), c => Marked(c, ActFive.CountedMark));

        PlayOne(play, enemy);
        var counted = Assert.Single(Everywhere(play), c => Marked(c, ActFive.CountedMark));
        Assert.False(Marked(counted, ActFive.EchoMark));
        play.Dispose();
    }

    // The sheet the fight lays in hand is not one of the player's plays either: a lever offered every morning
    // must not be able to take the count away from the card the player chose for it.
    [Fact]
    public void The_sheet_the_fight_deals_never_takes_the_count()
    {
        var (play, enemy) = At(2);
        Assert.Equal(1, Stacks(Lord(play), ActFive.LunarCountId));

        PlayOne(play, enemy, ActFive.HoldTheMoonCardId);
        Assert.DoesNotContain(Everywhere(play), c => Marked(c, ActFive.CountedMark));

        PlayOne(play, enemy);
        var counted = Assert.Single(Everywhere(play), c => Marked(c, ActFive.CountedMark));
        Assert.Equal(Cut, counted.DefinitionId.value);
        play.Dispose();
    }
}
