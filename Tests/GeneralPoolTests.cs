using BnbContent.Converter;
using BnbContent.Converter.Cards;
using RogueDeck.Core.Combat;
using RogueDeck.Sandbox.Composition;
using RogueDeck.Sandbox.Run;

namespace BnbContent.Tests;

// The general pool rebuilt on 2026-11-08 (GENERAL_POOL_PLAN.md), in live fights. One sweep plays every card and
// its upgrade; the rest pin the rules the cards are made of — Unread, Pledge, Liable, Held Energy overflow, and
// the cards that watch the piles.
public class GeneralPoolTests
{
    private const string Quiet = "ordinance_tablet";
    private const string QuietIntent = "stone_precedent";   // guards itself, does nothing to the hero
    private const string Filler = "paper_cut";

    private static InteractiveCombatDriver Driver(RunPlayback play) => play.CombatDriver!;

    private static CombatantState Hero(RunPlayback play) =>
        Driver(play).Current!.State.GetCombatant(Driver(play).Current!.HeroId);

    private static CombatantState Enemy(RunPlayback play, CombatantId id) => Driver(play).Current!.State.GetCombatant(id);

    private static int Block(CombatantState combatant) =>
        combatant.DefensivePools.TryGetValue(StandardCombatIds.BlockDefensivePool, out var pool) ? pool.Current : 0;

    private static int Energy(RunPlayback play) => Hero(play).Resources[StandardCombatIds.EnergyResource].Current;

    private static IReadOnlyList<CardInstance> Zone(RunPlayback play, CardZone zone) =>
        Driver(play).Current!.State.CardZonesByCombatant[Hero(play).Id].GetCardsInZone(zone);

    private static int Count(RunPlayback play, CardZone zone, string? id = null) =>
        Zone(play, zone).Count(c => id is null || c.DefinitionId.value == id);

    private static (RunPlayback Play, InteractiveRunSession Session, CombatantId Enemy) Fight(int energy, params string[] deck) =>
        FightProbe.Start(FightProbe.Solo(Quiet, QuietIntent, energy: energy), deck.ToList());

    private static void Play(RunPlayback play, InteractiveRunSession session, string cardId, CombatantId target)
    {
        var card = Driver(play).Current!.Hand.First(c => c.DefinitionId.value == cardId);
        Driver(play).PlayCard(card.Id, target);
        Assert.Null(session.Error);
    }

    private static void Pick(RunPlayback play, InteractiveRunSession session, string cardId)
    {
        var offered = Assert.IsAssignableFrom<IReadOnlyList<CardInstance>>(Driver(play).PendingCardChoice);
        Driver(play).SupplyCardChoice([offered.First(c => c.DefinitionId.value == cardId).Id]);
        Assert.Null(session.Error);
    }

    private static void Choose(RunPlayback play, InteractiveRunSession session, int option)
    {
        Assert.NotNull(Driver(play).PendingOptionChoice);
        Driver(play).SupplyOptionChoice([option]);
        Assert.Null(session.Error);
    }

    private static void EndTurn(RunPlayback play, InteractiveRunSession session)
    {
        Driver(play).EndTurn();
        Assert.Null(session.Error);
    }

    // ── the sweep ─────────────────────────────────────────────────────────────────────────────────────────

    public static TheoryData<string> Cards()
    {
        var data = new TheoryData<string>();
        foreach (var card in FinalCards.General())
            data.Add(card.Id);
        return data;
    }

    [Fact]
    public void The_pool_is_seventy_cards_each_with_its_upgrade()
    {
        var cards = FinalCards.General();
        Assert.Equal(140, cards.Count);
        Assert.Equal(70, FinalCards.GeneralPool(5).Count);
        Assert.Equal(26, FinalCards.GeneralPool(5).Count(c => c.Rarity == "common"));
        Assert.Equal(29, FinalCards.GeneralPool(5).Count(c => c.Rarity == "uncommon"));
        Assert.Equal(15, FinalCards.GeneralPool(5).Count(c => c.Rarity == "rare"));
        Assert.All(cards.Where(c => !c.Id.EndsWith('+')), c => Assert.Contains(cards, u => u.Id == c.Id + "+"));
    }

    // Every card, and every upgrade, played into a live fight and through the turn's end: whatever it asks is
    // answered with its first offer, and nothing may fail.
    [Theory]
    [MemberData(nameof(Cards))]
    public void Every_card_plays_and_its_turn_ends_cleanly(string id)
    {
        var (play, session, enemy) = Fight(9, id, Filler, Filler, Filler, Filler);
        Play(play, session, id, enemy);

        for (var i = 0; i < 20; i++)
        {
            if (Driver(play).PendingOptionChoice is not null)
                Driver(play).SupplyOptionChoice([0]);
            else if (Driver(play).PendingCardChoice is { Count: > 0 } offered)
                Driver(play).SupplyCardChoice([offered[0].Id]);
            else
                break;
            Assert.Null(session.Error);
        }

        EndTurn(play, session);
        EndTurn(play, session);
        play.Dispose();
    }

    // ── Liable, Held Energy, Pledge ───────────────────────────────────────────────────────────────────────

    [Fact]
    public void Liable_adds_a_quarter_to_attacks_and_fades_at_the_end_of_its_turn()
    {
        var (play, session, enemy) = Fight(9, "named_in_the_roll", "beadles_cudgel", Filler, Filler, Filler);
        Play(play, session, "named_in_the_roll", enemy);
        Assert.Equal(2, FightProbe.StacksOf(Enemy(play, enemy), Keywords.Liable));

        var health = Enemy(play, enemy).Health.Current;
        Play(play, session, "beadles_cudgel", enemy);
        Assert.Equal(health - 11, Enemy(play, enemy).Health.Current); // 9 × 1.25

        EndTurn(play, session);
        Assert.Equal(1, FightProbe.StacksOf(Enemy(play, enemy), Keywords.Liable));
        play.Dispose();
    }

    // Energy gained arrives in full: in the pool, or — where a pool has a ceiling — as Held Energy. Never both.
    [Fact]
    public void Energy_gained_arrives_once()
    {
        var (play, session, enemy) = Fight(3, "stolen_hour", Filler, Filler, Filler, Filler);
        var before = Energy(play) + FightProbe.StacksOf(Hero(play), HeldEnergy.Id);
        Play(play, session, "stolen_hour", enemy);
        Assert.Equal(before + 2, Energy(play) + FightProbe.StacksOf(Hero(play), HeldEnergy.Id));
        play.Dispose();
    }

    [Fact]
    public void A_pledge_is_played_short_and_the_shortfall_is_next_turns_fatigue()
    {
        var (play, session, enemy) = Fight(3, "beadles_cudgel", "beadles_cudgel", "beadles_cudgel", "coin_under_the_tongue", Filler);
        Play(play, session, "beadles_cudgel", enemy);
        Play(play, session, "beadles_cudgel", enemy);
        Play(play, session, "beadles_cudgel", enemy);
        Assert.Equal(0, Energy(play));

        Play(play, session, "coin_under_the_tongue", enemy);
        Assert.Equal(1, FightProbe.StacksOf(Hero(play), "fatigue"));
        EndTurn(play, session);
        Assert.Equal(2, Energy(play));
        play.Dispose();
    }

    // ── Unread ────────────────────────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Unread_fires_when_a_card_discards_it_and_not_at_the_end_of_the_turn()
    {
        var (play, session, enemy) = Fight(9, "scattered_notes", "spiteful_draft", "spiteful_draft", Filler, Filler);
        var health = Enemy(play, enemy).Health.Current;

        Play(play, session, "scattered_notes", enemy);
        Pick(play, session, "spiteful_draft");
        Assert.Equal(health - 7, Enemy(play, enemy).Health.Current);

        // The other Draft goes out with the hand at the turn's end — that is no Unread.
        health = Enemy(play, enemy).Health.Current;
        EndTurn(play, session);
        Assert.Equal(health, Enemy(play, enemy).Health.Current);
        play.Dispose();
    }

    // A copy played in an earlier round is no longer "played": thrown away later, its Unread still fires.
    [Fact]
    public void Unread_fires_for_a_card_that_was_played_in_an_earlier_round()
    {
        var (play, session, enemy) = Fight(9, "spiteful_draft", "scattered_notes", Filler, Filler, Filler);
        Play(play, session, "spiteful_draft", enemy);
        EndTurn(play, session);   // five cards, so the next hand is all of them again

        // The Tablet has raised 10 Block by now, so what it can take is HP and Block together.
        var standing = Enemy(play, enemy).Health.Current + Block(Enemy(play, enemy));
        Play(play, session, "scattered_notes", enemy);
        Pick(play, session, "spiteful_draft");
        Assert.Equal(standing - 7, Enemy(play, enemy).Health.Current + Block(Enemy(play, enemy)));
        play.Dispose();
    }

    [Fact]
    public void Sieve_and_shears_sends_a_card_from_the_top_to_the_bottom()
    {
        var deck = new[] { "sieve_and_shears", Filler, Filler, Filler, Filler, "beadles_cudgel", Filler, Filler };
        var (play, session, enemy) = Fight(9, deck);
        var top = Zone(play, CardZone.DrawPile)[0];
        Play(play, session, "sieve_and_shears", enemy);
        Choose(play, session, 0);
        Driver(play).SupplyCardChoice([top.Id]);
        Assert.Null(session.Error);
        Choose(play, session, 1);
        Assert.Equal(top.Id, Zone(play, CardZone.DrawPile)[^1].Id);
        play.Dispose();
    }

    [Fact]
    public void Two_vigil_lamps_burn_twice_as_bright()
    {
        var (play, session, enemy) = Fight(9, "vigil_lamp", "vigil_lamp", Filler, Filler, Filler);
        Play(play, session, "vigil_lamp", enemy);
        Play(play, session, "vigil_lamp", enemy);
        var log = Driver(play).Current!.State.CombatLog.Count;
        EndTurn(play, session);
        Assert.Contains(Driver(play).Current!.State.CombatLog.Skip(log),
            e => e.Message == "Gained 144 block on 'hero'.");
        play.Dispose();
    }

    [Fact]
    public void A_played_unread_card_does_only_what_it_says()
    {
        var (play, session, enemy) = Fight(9, "spiteful_draft", Filler, Filler, Filler, Filler);
        var health = Enemy(play, enemy).Health.Current;
        Play(play, session, "spiteful_draft", enemy);
        Assert.Equal(health - 7, Enemy(play, enemy).Health.Current);
        play.Dispose();
    }

    [Fact]
    public void Burnt_offering_reads_an_exhausted_tallow_stub()
    {
        var (play, session, enemy) = Fight(9, "burnt_offering", "tallow_stub", Filler, Filler, Filler);
        Play(play, session, "burnt_offering", enemy);
        Pick(play, session, "tallow_stub");
        Assert.Equal(8, Block(Hero(play)));
        Assert.Equal(1, FightProbe.StacksOf(Hero(play), HeldEnergy.Id));
        play.Dispose();
    }

    // ── the draw engine ───────────────────────────────────────────────────────────────────────────────────

    [Fact]
    public void A_dog_eared_almanac_kept_over_the_turn_draws_two_more()
    {
        var deck = new[] { "dog_eared_almanac", Filler, Filler, Filler, Filler, Filler, Filler, Filler, Filler, Filler, Filler, Filler };
        var (play, session, enemy) = Fight(9, deck);
        EndTurn(play, session);
        Assert.Equal(1, Driver(play).Current!.Hand.Count(c => c.DefinitionId.value == "dog_eared_almanac"));

        var hand = Driver(play).Current!.Hand.Count;
        Play(play, session, "dog_eared_almanac", enemy);
        Assert.Equal(hand - 1 + 3, Driver(play).Current!.Hand.Count);
        play.Dispose();
    }

    [Fact]
    public void Gathered_gossip_draws_one_more_than_there_are_enemies()
    {
        var deck = new[] { "gathered_gossip", "gathered_gossip", "gathered_gossip", "gathered_gossip", "gathered_gossip", Filler, Filler, Filler, Filler };
        var (play, session, enemy) = Fight(9, deck);
        var hand = Driver(play).Current!.Hand.Count;
        Play(play, session, "gathered_gossip", enemy);
        Assert.Equal(hand - 1 + 2, Driver(play).Current!.Hand.Count);
        play.Dispose();
    }

    [Fact]
    public void A_moonlit_counterfeit_copy_is_free_and_the_original_stays()
    {
        var (play, session, enemy) = Fight(9, "moonlit_counterfeit", "beadles_cudgel", Filler, Filler, Filler);
        Play(play, session, "moonlit_counterfeit", enemy);
        Pick(play, session, "beadles_cudgel");
        Assert.Equal(2, Driver(play).Current!.Hand.Count(c => c.DefinitionId.value == "beadles_cudgel"));

        var energy = Energy(play);
        var copy = Driver(play).Current!.Hand.Last(c => c.DefinitionId.value == "beadles_cudgel");
        Driver(play).PlayCard(copy.Id, enemy);
        Assert.Null(session.Error);
        Assert.Equal(energy, Energy(play));
        play.Dispose();
    }

    // ── debuffs ───────────────────────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Compounded_grievance_doubles_what_the_enemy_carries()
    {
        var (play, session, enemy) = Fight(9, "seed_of_misgiving", "named_in_the_roll", "compounded_grievance", Filler, Filler);
        Play(play, session, "seed_of_misgiving", enemy);
        Play(play, session, "named_in_the_roll", enemy);
        Play(play, session, "compounded_grievance", enemy);
        Assert.Equal(4, FightProbe.StacksOf(Enemy(play, enemy), Keywords.Doubt));
        Assert.Equal(4, FightProbe.StacksOf(Enemy(play, enemy), Keywords.Liable));
        play.Dispose();
    }

    [Fact]
    public void Bill_of_attainder_adds_one_to_each_doubt_and_liable()
    {
        var (play, session, enemy) = Fight(9, "bill_of_attainder", "overlooking", Filler, Filler, Filler);
        Play(play, session, "bill_of_attainder", enemy);
        Play(play, session, "overlooking", enemy);
        Assert.Equal(2, FightProbe.StacksOf(Enemy(play, enemy), Keywords.Doubt));
        Assert.Equal(2, FightProbe.StacksOf(Enemy(play, enemy), Keywords.Liable));
        play.Dispose();
    }

    // ── defence ───────────────────────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Standing_stones_keep_the_block_through_two_turn_starts()
    {
        var (play, session, enemy) = Fight(9, "standing_stones", "barred_door", Filler, Filler, Filler);
        Play(play, session, "barred_door", enemy);
        Play(play, session, "standing_stones", enemy);
        EndTurn(play, session);
        Assert.Equal(14, Block(Hero(play)));
        EndTurn(play, session);
        Assert.Equal(14, Block(Hero(play)));
        EndTurn(play, session);
        Assert.Equal(0, Block(Hero(play)));
        play.Dispose();
    }

    [Fact]
    public void Rowan_over_the_door_gives_doubt_to_whoever_attacks_and_lapses()
    {
        var (play, session, enemy) = FightProbe.Start(
            FightProbe.Solo("embossed_seal", "heavy_impression", energy: 9),
            ["rowan_over_the_door", Filler, Filler, Filler, Filler]);
        Play(play, session, "rowan_over_the_door", enemy);
        EndTurn(play, session);
        Assert.Equal(1, FightProbe.StacksOf(Enemy(play, enemy), Keywords.Doubt));
        Assert.Equal(0, FightProbe.StacksOf(Hero(play), GeneralPoolRules.Rowan));
        play.Dispose();
    }

    // ── the Exhausted pile ────────────────────────────────────────────────────────────────────────────────

    [Fact]
    public void A_corpse_candle_comes_back_when_another_card_is_exhausted()
    {
        var (play, session, enemy) = Fight(9, "corpse_candle", "burnt_offering", Filler, Filler, Filler);
        Play(play, session, "corpse_candle", enemy);
        Assert.Equal(1, Count(play, CardZone.ExhaustPile, "corpse_candle"));

        Play(play, session, "burnt_offering", enemy);
        Pick(play, session, Filler);
        Assert.Equal(0, Count(play, CardZone.ExhaustPile, "corpse_candle"));
        Assert.Equal(1, Driver(play).Current!.Hand.Count(c => c.DefinitionId.value == "corpse_candle"));
        play.Dispose();
    }

    [Fact]
    public void Last_ember_replays_the_newest_exhausted_card()
    {
        var (play, session, enemy) = Fight(3, "beadles_cudgel", "stolen_hour", "last_ember", Filler, Filler);
        Play(play, session, "beadles_cudgel", enemy);   // 3 → 2
        Play(play, session, "stolen_hour", enemy);   // +2
        Play(play, session, "last_ember", enemy);    // −1, then Stolen Hour again: +2 — 5 in all
        Assert.Equal(5, Energy(play) + FightProbe.StacksOf(Hero(play), HeldEnergy.Id));
        play.Dispose();
    }

    [Fact]
    public void Read_by_firelight_brings_two_cards_and_exhausts_what_is_left()
    {
        var deck = new[] { "read_by_firelight", "read_by_firelight", "read_by_firelight", "read_by_firelight", "read_by_firelight", Filler, Filler, Filler, Filler };
        var (play, session, enemy) = Fight(9, deck);
        var hand = Driver(play).Current!.Hand.Count;
        Play(play, session, "read_by_firelight", enemy);
        Assert.Equal(hand - 1 + 2, Driver(play).Current!.Hand.Count);
        var exhausted = Count(play, CardZone.ExhaustPile);
        EndTurn(play, session);
        Assert.Equal(exhausted + 2, Count(play, CardZone.ExhaustPile));
        play.Dispose();
    }

    [Fact]
    public void An_ashen_effigy_costs_one_less_for_each_two_exhausted_cards()
    {
        var (play, session, enemy) = Fight(9, "ashen_effigy", "stolen_hour", "sanctioned_charm", Filler, Filler);
        Play(play, session, "stolen_hour", enemy);
        Play(play, session, "sanctioned_charm", enemy);
        Assert.Equal(2, Count(play, CardZone.ExhaustPile));

        var energy = Energy(play);
        Play(play, session, "ashen_effigy", enemy);
        Assert.Equal(energy - 3, Energy(play));
        play.Dispose();
    }

    [Fact]
    public void Unburied_returns_the_exhausted_cards_except_junk_to_the_draw_pile()
    {
        var (play, session, enemy) = Fight(9, "stolen_hour", "unburied", Filler, Filler, Filler);
        Play(play, session, "stolen_hour", enemy);
        var draw = Count(play, CardZone.DrawPile);
        Play(play, session, "unburied", enemy);
        Assert.Equal(draw + 1, Count(play, CardZone.DrawPile));
        Assert.Equal(3, Block(Hero(play)));
        play.Dispose();
    }
}
