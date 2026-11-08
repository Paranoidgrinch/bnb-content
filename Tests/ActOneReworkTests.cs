using BnbContent.Converter;
using RogueDeck.Core.Combat;
using RogueDeck.Sandbox.Composition;
using RogueDeck.Sandbox.Run;
using RogueDeck.Scenario.Scripting;

namespace BnbContent.Tests;

// Act I's four reworked bodies (CONTENT_REVIEW.md E1-E4, 2026-11-08): each rule is one decision about the cards in
// hand, and each test plays that decision both ways.
public class ActOneReworkTests
{
    private const string Cut = "paper_cut";              // 1 Energy Deed, 6 damage
    private const string Desk = "cower_behind_a_desk";   // 1 Energy Working, 5 Block
    private const string Judgment = "summary_judgment";  // 2 Energy Deed

    private static InteractiveCombat Fight(RunPlayback play) => play.CombatDriver!.Current!;
    private static CombatantState Hero(RunPlayback play) => Fight(play).State.GetCombatant(Fight(play).HeroId);
    private static CombatantState Body(RunPlayback play, CombatantId id) => Fight(play).State.GetCombatant(id);
    private static int Energy(RunPlayback play) => Hero(play).Resources[StandardCombatIds.EnergyResource].Current;
    private static int Block(RunPlayback play, CombatantId id) =>
        Body(play, id).DefensivePools.TryGetValue(StandardCombatIds.BlockDefensivePool, out var pool) ? pool.Current : 0;
    private static bool Marked(CardInstance card, string mark) => card.HasMark(new TagId(mark));

    private static void Play(RunPlayback play, CardInstance card, CombatantId? target)
    {
        play.CombatDriver!.PlayCard(card.Id, target);
        Assert.Null(play.Session!.Error);
    }

    private static CardInstance InHand(RunPlayback play, string id) => Fight(play).Hand.First(c => c.DefinitionId.value == id);

    private static void EndTurn(RunPlayback play)
    {
        play.CombatDriver!.EndTurn();
        Assert.Null(play.Session!.Error);
    }

    // ── E1 Exception Imp ─────────────────────────────────────────────────────────────────────────────────────

    [Fact]
    public void The_first_deed_pays_the_imps_filing_fee_and_waives_its_next_block()
    {
        var (play, _, imp) = FightProbe.Start(FightProbe.Solo(ActOne.ImpId, "exception_to_the_exception"),
            [.. Enumerable.Repeat(Cut, 5)]);
        Assert.Equal(1, FightProbe.StacksOf(Hero(play), ActOne.FilingFeeId));

        Play(play, InHand(play, Cut), imp);
        Assert.Equal(1, Energy(play));                        // 1 + the fee
        Assert.Equal(0, FightProbe.StacksOf(Hero(play), ActOne.FilingFeeId));
        Assert.Equal(1, FightProbe.StacksOf(Body(play, imp), ActOne.WaiverId));
        Play(play, InHand(play, Cut), imp);
        Assert.Equal(0, Energy(play));                        // the second Deed is at its price

        EndTurn(play);                                        // Exception to the Exception lapses
        Assert.Equal(0, Block(play, imp));
        Assert.Equal(0, FightProbe.StacksOf(Body(play, imp), ActOne.WaiverId));
        Assert.Equal(0, FightProbe.StacksOf(Body(play, imp), "strength"));
        Assert.Equal(1, FightProbe.StacksOf(Hero(play), ActOne.FilingFeeId)); // charged again
    }

    [Fact]
    public void A_turn_without_a_deed_grows_the_imp_and_keeps_its_block()
    {
        var (play, _, imp) = FightProbe.Start(FightProbe.Solo(ActOne.ImpId, "exception_to_the_exception"),
            [.. Enumerable.Repeat(Desk, 5)]);
        Play(play, InHand(play, Desk), null);
        Assert.Equal(2, Energy(play));                        // the fee is on Deeds only
        EndTurn(play);
        Assert.Equal(1, FightProbe.StacksOf(Body(play, imp), "strength"));
        Assert.Equal(7, Block(play, imp));
    }

    // ── E2 Inverted Hourglass and Fading Number Token ────────────────────────────────────────────────────────

    [Fact]
    public void Two_unspent_energy_spill_the_glass_and_empty_its_turn()
    {
        var (play, _, glass) = FightProbe.Start(FightProbe.Solo(ActOne.HourglassId, "turn_the_glass"),
            [.. Enumerable.Repeat(Cut, 10)], health: 200);

        EndTurn(play);                                        // 3 Energy kept: the glass falls empty
        Assert.Equal(200, Hero(play).Health.Current);
        Assert.Equal(0, FightProbe.StacksOf(Body(play, glass), ActOne.SpiltSandId));

        Play(play, InHand(play, Cut), glass);
        Play(play, InHand(play, Cut), glass);                 // 1 left: not enough to spill
        EndTurn(play);
        Assert.Equal(200 - ActOne.GlassBase, Hero(play).Health.Current);
    }

    [Fact]
    public void Spilt_sand_loses_the_banked_grains()
    {
        var probe = FightProbe.Roster("spill",
            (ActOne.HourglassId, "glass_stillness", null), (ActOne.TokenId, "unreadable_digit", null));
        var (play, _, _) = FightProbe.Start(probe, [.. Enumerable.Repeat(Cut, 10)], health: 200);
        var glass = Fight(play).State.Combatants.First(c => c.Id.value.StartsWith(ActOne.HourglassId)).Id;

        // Round 1: everything spent; the Token files Fatigue, which takes an Energy as round 2 opens — a grain.
        Play(play, InHand(play, Cut), glass);
        Play(play, InHand(play, Cut), glass);
        Play(play, InHand(play, Cut), glass);
        EndTurn(play);
        Assert.Equal(1, Body(play, glass).GetCounter(PassiveStatuses.StolenSandCounter));

        EndTurn(play);                                        // 2 Energy kept: the grain is lost…
        // …and round 3's Fatigue banks a fresh one, so the glass holds 1 rather than 2.
        Assert.Equal(1, Body(play, glass).GetCounter(PassiveStatuses.StolenSandCounter));
    }

    [Fact]
    public void The_token_fades_when_nothing_hits_it_and_a_hit_restores_it()
    {
        var (play, _, token) = FightProbe.Start(FightProbe.Solo(ActOne.TokenId, "sharp_edge_token"),
            [.. Enumerable.Repeat(Cut, 10)], health: 200);
        var full = Body(play, token).Health.Current;

        EndTurn(play);                                        // a round without a hit
        Assert.Equal(1, FightProbe.StacksOf(Body(play, token), ActOne.FadedNumeralId));

        Play(play, InHand(play, Cut), token);                 // softened, and restores it
        var first = full - Body(play, token).Health.Current;
        Assert.Equal(6 * ActOne.FadedPercent / 100, first);
        Assert.Equal(0, FightProbe.StacksOf(Body(play, token), ActOne.FadedNumeralId));
        Play(play, InHand(play, Cut), token);
        Assert.Equal(first + 6, full - Body(play, token).Health.Current);

        EndTurn(play);                                        // hit this round: it does not fade
        Assert.Equal(0, FightProbe.StacksOf(Body(play, token), ActOne.FadedNumeralId));
    }

    // ── E3 Red Tape Golem ────────────────────────────────────────────────────────────────────────────────────

    private static IReadOnlyList<CardInstance> Wrapped(RunPlayback play) =>
        [.. Fight(play).Hand.Where(c => Marked(c, ActOne.WrappedMark))];

    [Fact]
    public void The_golem_wraps_a_card_that_then_costs_one_more()
    {
        var (play, _, golem) = FightProbe.Start(FightProbe.Solo(ActOne.GolemId, "procedural_mass"),
            [.. Enumerable.Repeat(Cut, 5)]);
        var wrapped = Assert.Single(Wrapped(play));
        Play(play, wrapped, golem);
        Assert.Equal(1, Energy(play));
        Assert.Empty(Wrapped(play));
        Play(play, InHand(play, Cut), golem);
        Assert.Equal(0, Energy(play));                        // the others are at their price

        EndTurn(play);
        Assert.Single(Wrapped(play));                         // a fresh wrap each turn
    }

    [Fact]
    public void A_working_cuts_the_tape_and_takes_a_strength()
    {
        var (play, _, golem) = FightProbe.Start(
            FightProbe.Solo(ActOne.GolemId, "procedural_mass", ("strength", 2)),
            [Cut, Cut, Cut, Desk, Desk]);
        var wrapped = Assert.Single(Wrapped(play));
        var working = Fight(play).Hand.First(c => c.DefinitionId.value == Desk && c.Id != wrapped.Id);

        Play(play, working, null);
        Assert.Empty(Wrapped(play));
        Assert.Equal(1, FightProbe.StacksOf(Body(play, golem), "strength"));
        Play(play, Fight(play).Hand.First(c => c.Id == wrapped.Id), wrapped.DefinitionId.value == Cut ? golem : null);
        Assert.Equal(1, Energy(play));                        // back at its printed 1
    }

    [Fact]
    public void An_unplayed_wrap_comes_off_with_the_turn()
    {
        var (play, _, _) = FightProbe.Start(FightProbe.Solo(ActOne.GolemId, "procedural_mass"),
            [.. Enumerable.Repeat(Cut, 10)]);
        Assert.Single(Wrapped(play));
        EndTurn(play);
        var zones = Fight(play).State.GetCardZones(Fight(play).HeroId);
        Assert.Equal(1, new[] { CardZone.Hand, CardZone.DiscardPile, CardZone.DrawPile }
            .Sum(z => zones.GetCardsInZone(z).Count(c => Marked(c, ActOne.WrappedMark))));
        Assert.All(zones.GetCardsInZone(CardZone.DiscardPile),
            c => Assert.Equal(0, c.GetMarkCounter(StandardCombatIds.CardCostDeltaCounter)));
    }

    // ── E4 Queue-Crier Homunculus ────────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Leading_with_the_called_cost_plays_it_for_free()
    {
        var (play, _, crier) = FightProbe.Start(FightProbe.Solo(ActOne.CrierId, "move_everyone_back_one_place"),
            [.. Enumerable.Repeat(Cut, 5)]);
        Assert.Equal(1, Hero(play).GetCounter(ActOne.CalledCostCounter));
        Assert.Equal(1, FightProbe.StacksOf(Body(play, crier), ActOne.CalledNumberId(1)));

        Play(play, InHand(play, Cut), crier);
        Assert.Equal(3, Energy(play));
        Assert.Equal(0, FightProbe.StacksOf(Hero(play), "panic"));
        Assert.Equal(0, FightProbe.StacksOf(Body(play, crier), ActOne.CalledNumberId(1))); // the call is over
        Play(play, InHand(play, Cut), crier);
        Assert.Equal(2, Energy(play));                        // only the first card answers it
    }

    [Fact]
    public void Leading_with_another_cost_earns_panic()
    {
        var (play, _, crier) = FightProbe.Start(FightProbe.Solo(ActOne.CrierId, "move_everyone_back_one_place"),
            [Cut, Cut, Cut, Cut, Judgment]);
        var called = Hero(play).GetCounter(ActOne.CalledCostCounter);
        var wrong = called == 1 ? InHand(play, Judgment) : InHand(play, Cut);
        var price = called == 1 ? 2 : 1;

        Play(play, wrong, crier);
        Assert.Equal(1, FightProbe.StacksOf(Hero(play), "panic"));
        Assert.Equal(3 - price, Energy(play));
        Assert.DoesNotContain(Fight(play).Hand, c => Marked(c, ActOne.CalledMark));
        Assert.All(Fight(play).Hand, c => Assert.Equal(0, c.GetMarkCounter(StandardCombatIds.CardCostDeltaCounter)));
    }
}
