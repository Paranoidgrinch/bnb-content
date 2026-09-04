using BnbContent.Converter;
using BnbContent.Converter.Relics;
using RogueDeck.Core.Combat;
using RogueDeck.Run;
using RogueDeck.Sandbox.Composition;
using RogueDeck.Sandbox.Run;

namespace BnbContent.Tests;

// The Act-IV boss relics in a real fight: the Licensing Labyrinth's eight offices handed to the player.
// One test per relic, because a relic that does nothing looks exactly like one that works — it installs, it
// validates, and the fight is quietly played without it (which is how two Act-IV Rites survived a whole
// build step dead, see ADAPTATIONS).
public class ActFourBossRelicTests
{
    private const string Quiet = "ordinance_tablet";
    private const string QuietIntent = "stone_precedent";
    private const string StrikingIntent = "etched_subsection"; // 7 damage and a Doubt
    private const string Deed = "paper_cut";                   // Deed, 1 Energy, 6 damage
    private const string Working = "cower_behind_a_desk";      // Working, 1 Energy, 5 Block

    // ── The Pharaoh of the Sealed Name ────────────────────────────────────────────────────────────────────

    [Fact]
    public void The_crown_holds_an_energy_for_every_turn()
    {
        var (play, _, _) = WithRelic("crown_of_the_three_names");

        // The pool is already full when a turn's triggers run, so the extra point waits and arrives the
        // moment the holder runs dry — which is the same card, one moment later.
        Assert.Equal(1, FightProbe.StacksOf(Hero(play), "held_energy"));
        play.Dispose();
    }

    [Fact]
    public void The_open_audience_hears_the_whole_hand_for_nothing()
    {
        var (play, session, target) = WithRelic("edict_of_the_open_audience");
        var energy = Energy(Hero(play));

        PlayAction(play, session, ActFourBossRelicCards.AudienceId, target);
        for (var i = 0; i < 4; i++)
            Play(play, session, Deed, target);

        Assert.Equal(energy, Energy(Hero(play))); // four cards, nothing paid
        play.Dispose();
    }

    [Fact]
    public void The_eternal_cartouche_stands_you_up_once()
    {
        var (play, session, _) = WithRelic(
            "eternal_cartouche", intent: StrikingIntent, startingHealth: 5, maxHealth: 100);

        play.CombatDriver!.EndTurn();   // 7 damage onto 5 HP
        Assert.Null(session.Error);

        Assert.Equal(BossRelicRules.CartoucheHealth, Hero(play).Health.Current);
        Assert.Equal(0, FightProbe.StacksOf(Hero(play), "eternal_cartouche"));
        Assert.Equal(1, FightProbe.StacksOf(Hero(play), BossRelicRules.CartoucheSpentId));
        play.Dispose();
    }

    // The other half of the cartouche, and the only half a fight cannot show: the RUN destroys the relic
    // afterwards, reading the counter the fight left behind. The Reserve Seal is here for the same reason —
    // it has nothing at all to say inside a fight.
    [Fact]
    public void The_two_relics_that_speak_outside_a_fight_say_so()
    {
        var cartouche = BossRelics.All().Single(r => r.Id == "eternal_cartouche");
        var seal = BossRelics.All().Single(r => r.Id == "granary_reserve_seal");

        Assert.NotEmpty(cartouche.RunPrograms!);
        Assert.NotNull(cartouche.CombatRule);
        Assert.NotEmpty(seal.RunPrograms!);
        Assert.Null(seal.CombatRule);
    }

    // ── The Weigher of the Unspoken Heart ─────────────────────────────────────────────────────────────────

    [Fact]
    public void The_feather_cheapens_the_lead_and_pays_for_the_answer()
    {
        var (play, session, target) = WithRelic("feather_of_perfect_measure", deck: Both());

        Play(play, session, Deed, target);
        Assert.Equal(1, FightProbe.StacksOf(Hero(play), "held_energy")); // the lead, refunded

        var hand = play.CombatDriver!.Current!.Hand.Count;
        Play(play, session, Working, target);

        Assert.Equal(5 + 8, Block(Hero(play)));       // the Working's own 5, and the feather's 8
        Assert.Equal(hand, play.CombatDriver.Current!.Hand.Count); // one played, one drawn
        play.Dispose();
    }

    [Fact]
    public void The_scarab_sits_in_court_every_third_turn()
    {
        var (play, session, target) = WithRelic("acquittal_scarab");

        var health = Enemy(play, target).Health.Current;
        Play(play, session, Deed, target);
        var ordinary = health - Enemy(play, target).Health.Current;

        play.CombatDriver!.EndTurn();
        play.CombatDriver.EndTurn();
        Assert.Null(session.Error);

        health = Enemy(play, target).Health.Current;
        Play(play, session, Deed, target);

        Assert.Equal(6, ordinary);
        Assert.Equal(7, health - Enemy(play, target).Health.Current); // 6 × 130 %
        play.Dispose();
    }

    [Fact]
    public void The_two_pans_balance_or_they_do_not()
    {
        var (play, session, target) = WithRelic(
            "balance_of_the_two_pans", deck: Both(), startingHealth: 300, maxHealth: 400);
        var health = Hero(play).Health.Current;

        // An unbalanced turn pays nothing forward — the 12 Block it pays instead is swept away by the next
        // turn's own clear, so what it can be asked for here is the Energy it did NOT hold.
        Play(play, session, Deed, target);
        play.CombatDriver!.EndTurn();
        Assert.Null(session.Error);
        Assert.Equal(0, FightProbe.StacksOf(Hero(play), "held_energy"));
        Assert.Equal(health, Hero(play).Health.Current);

        Play(play, session, Deed, target);
        Play(play, session, Working, target);
        play.CombatDriver.EndTurn();
        Assert.Null(session.Error);

        Assert.Equal(health + 2, Hero(play).Health.Current);
        Assert.Equal(1, FightProbe.StacksOf(Hero(play), "held_energy"));
        play.Dispose();
    }

    // ── The Architect of the Impossible Pyramid ───────────────────────────────────────────────────────────

    [Fact]
    public void The_capstone_keeps_half_the_block()
    {
        var (play, session, target) = WithRelic("impossible_capstone", deck: Deck(Working));

        Play(play, session, Working, target);
        Play(play, session, Working, target);
        Assert.Equal(10, Block(Hero(play)));

        play.CombatDriver!.EndTurn();
        Assert.Null(session.Error);
        Assert.Equal(5, Block(Hero(play)));  // the clear takes it all; half comes straight back
        play.Dispose();
    }

    [Fact]
    public void The_pyramidion_repeats_the_sixth_card()
    {
        // Read against the same six plays made without the relic on: the difference is one whole extra card.
        Assert.Equal(6, SixCards(withRelic: true) - SixCards(withRelic: false));
    }

    [Fact]
    public void The_plumb_line_pays_for_the_bend()
    {
        var (play, session, target) = WithRelic("crooked_plumb_line", deck: Both());

        Play(play, session, Deed, target);
        Assert.Equal(0, FightProbe.StacksOf(Hero(play), "held_energy")); // nothing to follow yet
        Play(play, session, Working, target);

        Assert.Equal(1, FightProbe.StacksOf(Hero(play), "held_energy")); // what the second card cost
        play.Dispose();
    }

    // ── The Lady of the Black Granaries ───────────────────────────────────────────────────────────────────

    [Fact]
    public void The_granary_key_stores_what_you_did_not_spend()
    {
        var (play, session, target) = WithRelic("black_granary_key");

        Play(play, session, Deed, target);   // 3 → 2
        play.CombatDriver!.EndTurn();
        Assert.Null(session.Error);

        Assert.Equal(2, FightProbe.StacksOf(Hero(play), "held_energy"));
        play.Dispose();
    }

    [Fact]
    public void The_ration_seal_pays_for_the_fourth_card()
    {
        var (play, session, target) = WithRelic("ration_seal", energy: 5);

        for (var i = 0; i < 3; i++)
            Play(play, session, Deed, target);
        var hand = play.CombatDriver!.Current!.Hand.Count;

        Play(play, session, Deed, target);   // the fourth
        Assert.Equal(1, FightProbe.StacksOf(Hero(play), "held_energy"));
        Assert.Equal(hand, play.CombatDriver.Current!.Hand.Count); // one played, one drawn
        play.Dispose();
    }

    // ── The First Scribe of the House of Life ─────────────────────────────────────────────────────────────

    [Fact]
    public void The_palimpsest_reed_hands_the_copy_back_next_turn()
    {
        var (play, session, target) = WithRelic("palimpsest_reed");

        var opening = play.CombatDriver!.Current!.Hand.Count;
        Play(play, session, Deed, target);
        play.CombatDriver.EndTurn();
        Assert.Null(session.Error);

        Assert.Equal(opening + 1, play.CombatDriver.Current!.Hand.Count); // the fresh hand, and the copy
        play.Dispose();
    }

    [Fact]
    public void The_erasure_tablet_rubs_out_what_was_coming()
    {
        var (play, session, target) = WithRelic("erasure_tablet", intent: StrikingIntent);

        PlayAction(play, session, ActFourBossRelicCards.ErasureId, target);
        var health = Hero(play).Health.Current;

        play.CombatDriver!.EndTurn();
        Assert.Null(session.Error);

        Assert.Equal(health, Hero(play).Health.Current);      // the line landed for nothing
        Assert.Equal(20, Block(Enemy(play, target)));          // and it guarded instead
        play.Dispose();
    }

    [Fact]
    public void The_correction_reed_trades_a_card_for_a_card()
    {
        var (play, session, target) = WithRelic("correction_reed");

        Play(play, session, Deed, target);   // something in the discard pile to take back
        var hand = play.CombatDriver!.Current!.Hand.Count;

        PlayAction(play, session, ActFourBossRelicCards.CorrectionId, target);

        // The reed itself exhausts; one card goes away and one comes back, so the hand is one lighter.
        Assert.Equal(hand - 1, play.CombatDriver.Current!.Hand.Count);
        Assert.Equal(1, play.CombatDriver.Current!.State.CardZonesByCombatant[Hero(play).Id].GetCardsInZone(CardZone.DiscardPile).Count);
        play.Dispose();
    }

    // ── The Mother of Natron and Resin ────────────────────────────────────────────────────────────────────

    [Fact]
    public void The_canopic_cabinet_refuses_the_first_two_afflictions()
    {
        var (play, session, _) = WithRelic("canopic_cabinet", intent: StrikingIntent);

        Assert.Equal(12, Block(Hero(play)));

        play.CombatDriver!.EndTurn();
        play.CombatDriver.EndTurn();
        Assert.Null(session.Error);
        Assert.Equal(0, FightProbe.StacksOf(Hero(play), "doubt"));   // both refused outright

        play.CombatDriver.EndTurn();
        Assert.True(FightProbe.StacksOf(Hero(play), "doubt") > 0);   // the third one lands
        play.Dispose();
    }

    [Fact]
    public void The_resin_shroud_wraps_a_holder_below_half()
    {
        var (play, _, _) = WithRelic("resin_shroud", startingHealth: 30, maxHealth: 100);

        Assert.Equal(25, Block(Hero(play)));
        play.Dispose();
    }

    [Fact]
    public void The_basin_washes_a_stack_off_or_pays_block()
    {
        var (play, session, _) = WithRelic("basin_of_black_natron", intent: StrikingIntent);

        Assert.Equal(12, Block(Hero(play)));   // nothing to wash off yet

        play.CombatDriver!.EndTurn();          // the enemy leaves a Doubt behind
        Assert.Null(session.Error);
        Assert.Equal(0, Block(Hero(play)));    // so the basin washes instead of paying
        play.Dispose();
    }

    // ── The Vizier of the King's Mouth ────────────────────────────────────────────────────────────────────

    [Fact]
    public void The_triune_seal_opens_all_three_offices()
    {
        var (play, session, target) = WithRelic("triune_office_seal", deck: Both());
        var plain = WithRelic(relicId: null, deck: Both());

        Assert.Equal(plain.Play.CombatDriver!.Current!.Hand.Count + 1,
            play.CombatDriver!.Current!.Hand.Count);
        plain.Play.Dispose();

        var health = Enemy(play, target).Health.Current;
        Play(play, session, Deed, target);
        Assert.Equal(6 + 8, health - Enemy(play, target).Health.Current);

        Play(play, session, Working, target);
        Assert.Equal(5 + 8, Block(Hero(play)));
        play.Dispose();
    }

    [Fact]
    public void The_staff_pays_for_the_first_card_of_the_turn()
    {
        var (play, session, target) = WithRelic("staff_of_the_kings_mouth");

        Play(play, session, Deed, target);
        Assert.Equal(1, FightProbe.StacksOf(Hero(play), "held_energy"));
        Play(play, session, Deed, target);
        Assert.Equal(1, FightProbe.StacksOf(Hero(play), "held_energy")); // and only the first
        play.Dispose();
    }

    [Fact]
    public void The_vacant_throne_pays_all_three_ways()
    {
        var (play, session, target) = WithRelic("vacant_throne_decree");
        var hand = play.CombatDriver!.Current!.Hand.Count;

        PlayAction(play, session, ActFourBossRelicCards.ThroneId, target);

        Assert.Equal(20, Block(Hero(play)));
        Assert.Equal(3, FightProbe.StacksOf(Hero(play), "held_energy"));
        Assert.Equal(hand + 2, play.CombatDriver.Current!.Hand.Count); // three drawn, the decree spent
        play.Dispose();
    }

    // ── The Queen of the Flood Reckoning ──────────────────────────────────────────────────────────────────

    [Fact]
    public void The_sluice_gate_trades_energy_for_block()
    {
        var (play, session, target) = WithRelic("sluice_gate_of_the_two_lands");
        var energy = Energy(Hero(play));

        PlayAction(play, session, ActFourBossRelicCards.SluiceId, target, 1); // close the gate

        Assert.Equal(energy - 1, Energy(Hero(play)));
        Assert.Equal(12, Block(Hero(play)));
        play.Dispose();
    }

    [Fact]
    public void The_flood_crown_reads_how_the_last_turn_ended()
    {
        var (play, session, _) = WithRelic("flood_reckoning_crown");

        Assert.Equal(10, Block(Hero(play)));   // a turn with nothing behind it

        play.CombatDriver!.EndTurn();          // ended with all three still in hand
        Assert.Null(session.Error);
        Assert.Equal(15, Block(Hero(play)));
        Assert.Equal(1, FightProbe.StacksOf(Hero(play), "held_energy"));
        play.Dispose();
    }

    [Fact]
    public void The_black_flood_vessel_pours_the_hand_away()
    {
        var (play, session, target) = WithRelic("black_flood_vessel");

        PlayAction(play, session, ActFourBossRelicCards.VesselId, target);

        Assert.Equal(7, play.CombatDriver!.Current!.Hand.Count);
        Assert.Equal(2, FightProbe.StacksOf(Hero(play), "held_energy"));
        play.Dispose();
    }

    // ── plumbing ──────────────────────────────────────────────────────────────────────────────────────────

    // Six Deeds over two turns, and what they took off the probe. The striking intent is the one the probe
    // has that does NOT guard: against the quiet one the sixth card's extra six would land in Block and the
    // reading would be of the tablet's guard rather than of the relic.
    private static int SixCards(bool withRelic)
    {
        var (play, session, target) =
            WithRelic(withRelic ? "pyramidion_of_repetition" : null, energy: 9, intent: StrikingIntent);
        var health = Enemy(play, target).Health.Current;

        for (var i = 0; i < 5; i++)
            Play(play, session, Deed, target);
        play.CombatDriver!.EndTurn();
        Assert.Null(session.Error);
        Play(play, session, Deed, target);   // the sixth of the fight

        var dealt = health - Enemy(play, target).Health.Current;
        play.Dispose();
        return dealt;
    }

    private static string[] Deck(string card) => [.. Enumerable.Repeat(card, 12)];

    // A deck that answers both halves of a relic that cares which kind you played.
    private static string[] Both() => [.. Enumerable.Repeat(Deed, 6), .. Enumerable.Repeat(Working, 6)];

    private static CombatantState Hero(RunPlayback play) =>
        play.CombatDriver!.Current!.State.GetCombatant(play.CombatDriver.Current!.HeroId);

    private static CombatantState Enemy(RunPlayback play, CombatantId id) =>
        play.CombatDriver!.Current!.State.GetCombatant(id);

    private static int Block(CombatantState c) =>
        c.DefensivePools.TryGetValue(StandardCombatIds.BlockDefensivePool, out var pool) ? pool.Current : 0;

    private static int Energy(CombatantState c) =>
        c.Resources[StandardCombatIds.EnergyResource].Current;

    private static void Play(RunPlayback play, InteractiveRunSession session, string cardId, CombatantId target)
    {
        var card = play.CombatDriver!.Current!.Hand.First(c => c.DefinitionId.value == cardId);
        play.CombatDriver.PlayCard(card.Id, target);
        Assert.Null(session.Error);
    }

    // A relic's own card, and whatever it asks the holder while it resolves.
    private static void PlayAction(
        RunPlayback play, InteractiveRunSession session, string cardId, CombatantId target,
        params int[] answers)
    {
        var card = play.CombatDriver!.Current!.Hand.First(c => c.DefinitionId.value == cardId);
        play.CombatDriver.PlayCard(card.Id, target);
        var next = 0;
        for (var guard = 0; guard < 6; guard++)
        {
            if (play.CombatDriver.PendingOptionChoice is not null && answers.Length > 0)
                play.CombatDriver.SupplyOptionChoice([answers[Math.Min(next++, answers.Length - 1)]]);
            else if (play.CombatDriver.PendingCardChoice is { } cards)
                play.CombatDriver.SupplyCardChoice([cards[0].Id]);
            else
                break;
        }
        Assert.Null(session.Error);
    }

    private static (RunPlayback Play, InteractiveRunSession Session, CombatantId EnemyId) WithRelic(
        string? relicId, IReadOnlyList<string>? deck = null, int energy = 3, string intent = QuietIntent,
        int startingHealth = 400, int maxHealth = 400)
    {
        var probe = FightProbe.Solo(Quiet, intent, energy);
        var blueprint = FightProbe.OneFight(probe, deck ?? Deck(Deed));
        blueprint = blueprint with
        {
            Start = blueprint.Start with
            {
                StartingRelics = relicId is null
                    ? blueprint.Start.StartingRelics
                    : [.. blueprint.Start.StartingRelics, relicId],
                MaxHealth = maxHealth,
                StartingHealth = startingHealth,
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

        var combat = play.CombatDriver!.Current!;
        return (play, session, combat.State.Combatants.First(c => c.Id != combat.HeroId).Id);
    }
}
