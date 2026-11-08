using BnbContent.Converter;
using BnbContent.Converter.Cards;
using RogueDeck.Core.Combat;
using RogueDeck.Sandbox.Composition;
using RogueDeck.Sandbox.Run;

namespace BnbContent.Tests;

// The relics the relic audit of 2026-11-08 (RELIC_AUDIT_PLAN.md) added or rewrote, in live fights.
public class RelicAuditTests
{
    private const string Quiet = "ordinance_tablet";
    private const string QuietIntent = "stone_precedent";
    private const string Filler = "paper_cut";

    private static InteractiveCombatDriver Driver(RunPlayback play) => play.CombatDriver!;

    private static CombatantState Hero(RunPlayback play) =>
        Driver(play).Current!.State.GetCombatant(Driver(play).Current!.HeroId);

    private static CombatantState Enemy(RunPlayback play, CombatantId id) => Driver(play).Current!.State.GetCombatant(id);

    private static int Block(CombatantState combatant) =>
        combatant.DefensivePools.TryGetValue(StandardCombatIds.BlockDefensivePool, out var pool) ? pool.Current : 0;

    private static int Energy(RunPlayback play) => Hero(play).Resources[StandardCombatIds.EnergyResource].Current;

    private static (RunPlayback Play, InteractiveRunSession Session, CombatantId Enemy) WithRelic(
        string relicId, int energy, params string[] deck)
    {
        var blueprint = FightProbe.OneFight(FightProbe.Solo(Quiet, QuietIntent, energy: energy), deck.ToList());
        blueprint = blueprint with
        {
            Start = blueprint.Start with { StartingRelics = [.. blueprint.Start.StartingRelics, relicId] },
            Characters = [],
        };
        var play = new RunPlayback(() => { });
        play.Start(blueprint, seed: 1, interactive: true);
        Assert.True(play.Error is null, play.Error);
        var session = play.Session!;
        while (session.IsAwaitingInterlude)
            session.Continue();
        Assert.True(session.Error is null, session.Error);
        var combat = Driver(play).Current!;
        return (play, session, combat.State.Combatants.First(c => c.Id != combat.HeroId).Id);
    }

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

    [Fact]
    public void Witness_slate_adds_one_to_the_first_liable_each_turn()
    {
        var (play, session, enemy) = WithRelic("witness_slate", 9, "named_in_the_roll", "named_in_the_roll", Filler, Filler, Filler);
        Play(play, session, "named_in_the_roll", enemy);
        Assert.Equal(3, FightProbe.StacksOf(Enemy(play, enemy), Keywords.Liable));
        Play(play, session, "named_in_the_roll", enemy);
        Assert.Equal(5, FightProbe.StacksOf(Enemy(play, enemy), Keywords.Liable));
        play.Dispose();
    }

    [Fact]
    public void Ember_box_draws_when_held_energy_arrives()
    {
        var (play, session, enemy) = WithRelic("ember_box", 3,
            "banked_embers", "beadles_cudgel", "beadles_cudgel", Filler, Filler, Filler, Filler, Filler);
        Play(play, session, "banked_embers", enemy);   // 2 left, 2 held
        Play(play, session, "beadles_cudgel", enemy);  // 1 left
        var hand = Driver(play).Current!.Hand.Count;
        Play(play, session, "beadles_cudgel", enemy);  // 0: the held 2 arrive, and a card with them
        Assert.Equal(2, Energy(play));
        Assert.Equal(hand - 1 + 1, Driver(play).Current!.Hand.Count);
        play.Dispose();
    }

    [Fact]
    public void Charnel_ledger_makes_unread_happen_twice()
    {
        var (play, session, enemy) = WithRelic("charnel_ledger", 9, "scattered_notes", "spiteful_draft", Filler, Filler, Filler);
        var health = Enemy(play, enemy).Health.Current;
        Play(play, session, "scattered_notes", enemy);
        Pick(play, session, "spiteful_draft");
        Assert.Equal(health - 14, Enemy(play, enemy).Health.Current);
        play.Dispose();
    }

    [Fact]
    public void Lead_counterweight_keeps_a_heavy_card_in_hand()
    {
        var (play, session, _) = WithRelic("lead_counterweight", 9, "barred_door", "dawn_summons", Filler, Filler, Filler);
        Driver(play).EndTurn();
        Assert.Null(session.Error);
        Assert.Contains(Driver(play).Current!.Hand, c => c.DefinitionId.value == "dawn_summons");
        play.Dispose();
    }

    [Fact]
    public void Oilcloth_wrap_keeps_the_hand_and_deals_two_fewer()
    {
        var deck = Enumerable.Repeat(Filler, 12).ToArray();
        var (play, session, _) = WithRelic("oilcloth_wrap", 9, deck);
        var hand = Driver(play).Current!.Hand.Count;
        Driver(play).EndTurn();
        Assert.Null(session.Error);
        Assert.Equal(hand + 3, Driver(play).Current!.Hand.Count);   // five kept, three drawn
        play.Dispose();
    }

    [Fact]
    public void Rushlight_makes_the_first_card_free()
    {
        var (play, session, enemy) = WithRelic("rushlight", 3, "dawn_summons", Filler, Filler, Filler, Filler, Filler);
        Play(play, session, "dawn_summons", enemy);
        Assert.Equal(3, Energy(play));
        play.Dispose();
    }

    [Fact]
    public void Tinder_pouch_and_ashen_wax_knife_answer_an_exhausted_card()
    {
        var (play, session, enemy) = WithRelic("tinder_pouch", 9, "stolen_hour", Filler, Filler, Filler, Filler);
        Play(play, session, "stolen_hour", enemy);
        Assert.Equal(5, Block(Hero(play)));
        play.Dispose();

        (play, session, enemy) = WithRelic("ashen_wax_knife", 9, "stolen_hour", Filler, Filler, Filler, Filler, Filler, Filler);
        var hand = Driver(play).Current!.Hand.Count;
        Play(play, session, "stolen_hour", enemy);
        Assert.Equal(hand - 1 + 1, Driver(play).Current!.Hand.Count);
        play.Dispose();
    }

    [Fact]
    public void Ash_pan_pays_once_when_five_cards_are_exhausted()
    {
        var deck = new[] { "stolen_hour", "stolen_hour", "stolen_hour", "stolen_hour", "stolen_hour" };
        var (play, session, enemy) = WithRelic("ash_pan", 9, deck);
        for (var i = 0; i < 4; i++)
            Play(play, session, "stolen_hour", enemy);
        // The probe's pool may be full, so what arrives is counted with what is held.
        int Total() => Energy(play) + FightProbe.StacksOf(Hero(play), HeldEnergy.Id);
        var energy = Total();
        Play(play, session, "stolen_hour", enemy);
        Assert.Equal(energy + 2 + 1, Total());
        play.Dispose();
    }
}
