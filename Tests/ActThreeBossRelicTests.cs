using BnbContent.Converter;
using BnbContent.Converter.Relics;
using RogueDeck.Core.Combat;
using RogueDeck.Run;
using RogueDeck.Sandbox.Composition;
using RogueDeck.Sandbox.Run;
using RogueDeck.Scenario.Authoring;

namespace BnbContent.Tests;

// The Act-III boss relics in a real fight: the Green Docket's five courts handed to the player. Each one is
// a piece of its boss's own machinery — the Ombudsman's two Grounds, the Notary's rings, Grandmother's
// courtesies (whose clause is now enforced against the HOLDER), the Hill's stored weight, the Queen's
// reciprocity.
public class ActThreeBossRelicTests
{
    private const string Quiet = "ordinance_tablet";
    private const string QuietIntent = "stone_precedent";
    private const string StrikingIntent = "etched_subsection"; // 7 damage and a Doubt
    private const string Deed = "paper_cut";
    private const string Working = "cower_behind_a_desk";

    private static CombatantState Hero(RunPlayback play) =>
        play.CombatDriver!.Current!.State.GetCombatant(play.CombatDriver.Current!.HeroId);

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

    private static (RunPlayback Play, InteractiveRunSession Session, CombatantId EnemyId) WithRelic(
        string relicId, string card = Deed, int energy = 3, string intent = QuietIntent)
    {
        var probe = FightProbe.Solo(Quiet, intent, energy);
        var blueprint = FightProbe.OneFight(probe, [.. Enumerable.Repeat(card, 12)]);
        blueprint = blueprint with
        {
            Start = blueprint.Start with
            {
                StartingRelics = [.. blueprint.Start.StartingRelics, relicId],
                MaxHealth = 400,
                StartingHealth = 400,
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

    private static void PlayAction(
        RunPlayback play, InteractiveRunSession session, string cardId, CombatantId target,
        params int[] answers)
    {
        var card = play.CombatDriver!.Current!.Hand.First(c => c.DefinitionId.value == cardId);
        play.CombatDriver.PlayCard(card.Id, target);
        var next = 0;
        for (var guard = 0; guard < 4; guard++)
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

    // ── the Ombudsman ─────────────────────────────────────────────────────────────────────────────────────

    // "Road: your first real card costs 1 less. Root: open with 10 Block." They take it in turns.
    [Fact]
    public void The_boundary_tally_alternates_road_and_root()
    {
        var (play, _, _) = WithRelic("boundary_tally");

        // The first turn is a road turn: the discount is on a card, not on the board.
        Assert.Equal(0, Block(Hero(play)));

        play.CombatDriver!.EndTurn();
        Assert.Equal(10, Block(Hero(play))); // …and the second is a root turn
        play.Dispose();
    }

    // The twine is a card, because a combat here has no free actions.
    [Fact]
    public void The_counter_petition_twine_re_argues_a_card_once_a_turn()
    {
        var (play, session, target) = WithRelic("counter_petition_twine");

        Assert.Contains(play.CombatDriver!.Current!.Hand,
            c => c.DefinitionId.value == ActThreeBossRelicCards.TwineId);

        PlayAction(play, session, ActThreeBossRelicCards.TwineId, target);
        Assert.Equal(1, FightProbe.StacksOf(Hero(play), "held_energy"));

        // Once a turn, and the twine says so by not being there: it exhausts, and the relic offers another
        // at the next bell.
        Assert.DoesNotContain(play.CombatDriver.Current!.Hand,
            c => c.DefinitionId.value == ActThreeBossRelicCards.TwineId);
        play.CombatDriver.EndTurn();
        Assert.Contains(play.CombatDriver.Current!.Hand,
            c => c.DefinitionId.value == ActThreeBossRelicCards.TwineId);
        play.Dispose();
    }

    // "Come through an enemy turn untouched for 1 Energy and a card; come through it hurt for 8 Block." And
    // the first turn is nobody's settlement.
    [Fact]
    public void The_signed_settlement_pays_for_a_quiet_night()
    {
        var (quiet, _, _) = WithRelic("signed_settlement");

        Assert.Equal(0, Block(Hero(quiet))); // no turn-1 effect
        quiet.CombatDriver!.EndTurn();       // the tablet only guards, so the night was untouched

        Assert.Equal(1, FightProbe.StacksOf(Hero(quiet), "held_energy"));
        quiet.Dispose();

        var (hurt, _, _) = WithRelic("signed_settlement", intent: StrikingIntent);
        hurt.CombatDriver!.EndTurn();

        Assert.Equal(8, Block(Hero(hurt)));
        hurt.Dispose();
    }

    // ── the Notary ────────────────────────────────────────────────────────────────────────────────────────

    // "Your first real card sets a price; the next card at that price is refunded."
    [Fact]
    public void The_ring_of_passage_refunds_a_matched_price()
    {
        var (play, session, target) = WithRelic("countersealed_ring_of_passage");

        Play(play, session, Deed, target);   // 1 Energy: the price of the turn
        Assert.Equal(2, Energy(Hero(play)));

        Play(play, session, Deed, target);   // the match, and its price comes back
        Assert.Equal(1, FightProbe.StacksOf(Hero(play), "held_energy"));
        play.Dispose();
    }

    // …and a turn that never matches ends in 5 Block instead — which is Block for the enemy turn, and gone
    // by the time the player sees the board again, so it is read off what the blow actually cost.
    [Fact]
    public void An_unmatched_turn_ends_in_block()
    {
        var (bare, _, _) = WithRelic("boundary_tally", intent: StrikingIntent); // a relic that guards later
        bare.CombatDriver!.EndTurn();
        var unguarded = Hero(bare).Health.Current;
        bare.Dispose();

        var (play, _, _) = WithRelic("countersealed_ring_of_passage", intent: StrikingIntent);
        play.CombatDriver!.EndTurn(); // nothing played at all: nothing matched, so 5 Block met the blow

        Assert.True(Hero(play).Health.Current > unguarded,
            $"the ring's consolation met the blow: {Hero(play).Health.Current} vs {unguarded}");
        play.Dispose();
    }

    // "Play three real cards and the fourth is refunded and draws a card."
    [Fact]
    public void The_ring_of_restraint_pays_for_the_fourth_card()
    {
        var (play, session, target) = WithRelic("countersealed_ring_of_restraint", energy: 9);

        for (var i = 0; i < 3; i++)
            Play(play, session, Deed, target);
        Assert.Equal(0, FightProbe.StacksOf(Hero(play), "held_energy"));

        Play(play, session, Deed, target); // the fourth
        Assert.Equal(1, FightProbe.StacksOf(Hero(play), "held_energy"));
        play.Dispose();
    }

    // "Empty your hand of real cards and the next turn opens with 1 Energy and two extra cards."
    [Fact]
    public void The_ring_of_keeping_pays_for_an_empty_hand()
    {
        var (play, session, target) = WithRelic("countersealed_ring_of_keeping", energy: 9);

        while (play.CombatDriver!.Current!.Hand.Any(c => c.DefinitionId.value == Deed))
            Play(play, session, Deed, target);

        var hand = play.CombatDriver.Current!.Hand.Count;
        play.CombatDriver.EndTurn();

        Assert.Equal(1, FightProbe.StacksOf(Hero(play), "held_energy"));
        Assert.True(play.CombatDriver.Current!.Hand.Count > hand, "two extra cards came with it");
        play.Dispose();
    }

    // ── Grandmother ───────────────────────────────────────────────────────────────────────────────────────

    // A gift with a clause on it, and the clause is now the HOLDER's to keep. Kept, it costs nothing.
    [Fact]
    public void The_honey_spoon_is_free_to_a_holder_who_keeps_the_promise()
    {
        var (play, session, target) = WithRelic("honey_spoon", card: Working, energy: 3);

        PlayAction(play, session, ActThreeBossRelicCards.HoneyId, target);
        Assert.Equal(2, FightProbe.StacksOf(Hero(play), "held_energy")); // two, held until there is room

        var health = Hero(play).Health.Current;
        play.CombatDriver!.EndTurn(); // ends with a full purse: the promise is kept

        // Only the tablet's blow, and nothing for a broken promise.
        Assert.True(Hero(play).Health.Current >= health - 13, "the spoon cost nothing");
        play.Dispose();
    }

    // …and broken, it costs 6 HP that no Block sees.
    [Fact]
    public void The_honey_spoon_costs_six_to_a_holder_who_breaks_it()
    {
        var (play, session, target) = WithRelic("honey_spoon", card: Working, energy: 3);

        PlayAction(play, session, ActThreeBossRelicCards.HoneyId, target);
        while (Energy(Hero(play)) > 0
            && play.CombatDriver!.Current!.Hand.Any(c => c.DefinitionId.value == Working))
            Play(play, session, Working, target);

        var health = Hero(play).Health.Current;
        play.CombatDriver!.EndTurn();

        Assert.True(Hero(play).Health.Current <= health - 6, "the promise was broken and paid for");
        play.Dispose();
    }

    // The tin's promise is about how much you play, not what you hold.
    [Fact]
    public void The_last_slice_tin_draws_two_and_asks_for_restraint()
    {
        var (play, session, target) = WithRelic("last_slice_tin", energy: 9);

        var hand = play.CombatDriver!.Current!.Hand.Count;
        PlayAction(play, session, ActThreeBossRelicCards.TinId, target);
        Assert.True(play.CombatDriver.Current!.Hand.Count >= hand + 1, "two cards came out of the tin");
        play.Dispose();
    }

    // ── the Hill ──────────────────────────────────────────────────────────────────────────────────────────

    // "What the enemies take out of you is weight in the stone: next turn it is Block."
    [Fact]
    public void The_loadstone_cairn_turns_what_you_lost_into_block()
    {
        var (play, _, _) = WithRelic("loadstone_cairn", intent: StrikingIntent);

        play.CombatDriver!.EndTurn(); // the tablet takes 7, and the stone remembers it

        Assert.Equal(7, Block(Hero(play)));
        play.Dispose();
    }

    // "End a turn with 12 Block or more and the cairn buries twelve; the next turn opens with 1 Energy and
    // a card."
    [Fact]
    public void The_survey_cairn_buries_block_and_pays_next_turn()
    {
        var (play, session, target) = WithRelic("survey_cairn", card: Working, energy: 9);

        for (var i = 0; i < 3; i++)
            Play(play, session, Working, target); // 5 Block a card
        Assert.True(Block(Hero(play)) >= 12);

        play.CombatDriver!.EndTurn();

        Assert.Equal(1, FightProbe.StacksOf(Hero(play), "held_energy"));
        play.Dispose();
    }

    // ── the Queen ─────────────────────────────────────────────────────────────────────────────────────────

    // "The cup offers an Energy, a card or 10 Block. Take it and every enemy guards for 6."
    [Fact]
    public void The_royal_grace_cup_gives_and_the_court_guards()
    {
        var (play, session, target) = WithRelic("royal_grace_cup", card: Working, energy: 3);

        PlayAction(play, session, ActThreeBossRelicCards.GraceId, target, answers: 2); // 10 Block

        Assert.True(Block(Hero(play)) >= 10);
        var enemy = play.CombatDriver!.Current!.State.Combatants.First(c => c.Id == target);
        Assert.Equal(6, Block(enemy));
        play.Dispose();
    }

    // "Spending your purse to the bottom is remembered, up to three times; open a turn owed all three and it
    // pays 1 Energy, two cards and 8 Block."
    [Fact]
    public void The_hollow_court_token_remembers_an_empty_purse()
    {
        var (play, session, target) = WithRelic("hollow_court_token", energy: 1);

        for (var turn = 0; turn < 3; turn++)
        {
            Play(play, session, Deed, target); // the last Energy, every turn
            play.CombatDriver!.EndTurn();
        }

        Assert.True(FightProbe.StacksOf(Hero(play), "held_energy") >= 1);
        Assert.True(Block(Hero(play)) >= 8);
        play.Dispose();
    }

    // "Once a combat: one enemy's guard is gone, you gain 10 Block, and your next card that turn is
    // refunded."
    [Fact]
    public void The_silver_name_tally_is_spoken_once_a_combat()
    {
        var (play, session, target) = WithRelic("silver_name_tally", card: Working, energy: 3);

        PlayAction(play, session, ActThreeBossRelicCards.TallyId, target);
        Assert.True(Block(Hero(play)) >= 10);

        play.CombatDriver!.EndTurn();

        Assert.DoesNotContain(play.CombatDriver.Current!.Hand,
            c => c.DefinitionId.value == ActThreeBossRelicCards.TallyId); // and never again this fight
        play.Dispose();
    }
}
