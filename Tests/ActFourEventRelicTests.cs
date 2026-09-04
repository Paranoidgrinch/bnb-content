using BnbContent.Converter;
using BnbContent.Converter.Cards;
using BnbContent.Converter.Relics;
using RogueDeck.Core.Combat;
using RogueDeck.Run;
using RogueDeck.Sandbox.Composition;
using RogueDeck.Sandbox.Run;

namespace BnbContent.Tests;

// The nine Act-IV Event relics in real fights — one live fight each, plus the object two of them lean on.
//
// One live fight per relic, for the same reason the boss relics get one: a relic that does nothing installs,
// validates and is quietly played without. Each is measured against the SAME fight without it wherever the
// answer is a number a fight could have produced anyway (a card in hand, a point of Block).
public class ActFourEventRelicTests
{
    private const string Quiet = "ordinance_tablet";
    private const string QuietIntent = "stone_precedent";   // guards for 10, hits for nothing
    private const string Scribe = "fourfold_vessel_guardian";
    private const string NameOffice = "name_office";        // 11 damage and an Inscribed
    private const string Deed = "paper_cut";                // Deed, 1 Energy, 6 damage
    private const string Marking = "etched_subsection";     // 7 damage and a Doubt: an action that MARKS you
    private const string Wax = "waxen_surety";              // Working, 1 Energy, "gain 4 Ward Wax"
    private const string Index = "smudged_index";           // Working, 1 Energy, archives off the draw pile
    private const string Docket = "night_docket";           // Working, 0 Energy, exhausts itself on play
    private const int WaxStacks = 4;

    // ── Cup of the Lowest Mark ────────────────────────────────────────────────────────────────────────────

    [Fact]
    public void The_cup_fills_on_the_turn_you_very_nearly_spent()
    {
        var (play, session, target) = WithRelic(ActFourEventRelicRules.CupId, startingHealth: 100);

        Play(play, session, Deed, target);
        Play(play, session, Deed, target);      // two of three spent: the lowest mark
        Assert.Equal(1, Energy(Hero(play)));

        play.CombatDriver!.EndTurn();
        Assert.Null(session.Error);
        Assert.Equal(104, Hero(play).Health.Current);

        // …and the card it promised, at the next hand rather than into a hand about to be discarded.
        var withCup = play.CombatDriver!.Current!.Hand.Count;
        play.Dispose();
        Assert.Equal(withCup - 1, SecondHand(relicId: null));
    }

    [Fact]
    public void The_cup_stays_dry_on_a_turn_that_spent_everything()
    {
        var (play, session, target) = WithRelic(ActFourEventRelicRules.CupId, startingHealth: 100);

        for (var i = 0; i < 3; i++)
            Play(play, session, Deed, target);
        play.CombatDriver!.EndTurn();
        Assert.Null(session.Error);

        Assert.Equal(100, Hero(play).Health.Current);
        play.Dispose();
    }

    // ── Red Linen Knot ────────────────────────────────────────────────────────────────────────────────────

    [Fact]
    public void The_knot_opens_the_fight_wrapped_and_wraps_again_when_the_linen_holds()
    {
        var (play, session, _) = WithRelic(
            ActFourEventRelicRules.KnotId, heroStatuses: [("panic", 1)]);

        Assert.Equal(ActFourEventRelicRules.KnotBlock, Block(Hero(play)));
        Assert.Equal(1, FightProbe.StacksOf(Hero(play), ActFour.EmbalmedId));

        // Panic fades at the bearer's turn end — and does not, because the linen is holding it.
        play.CombatDriver!.EndTurn();
        Assert.Null(session.Error);

        Assert.Equal(1, FightProbe.StacksOf(Hero(play), "panic"));
        Assert.Equal(0, FightProbe.StacksOf(Hero(play), ActFour.EmbalmedId));
        Assert.Equal(ActFourEventRelicRules.KnotBlock, Block(Hero(play)));
        play.Dispose();
    }

    // ── Blank Cartouche ───────────────────────────────────────────────────────────────────────────────────

    [Fact]
    public void The_blank_cartouche_deals_one_more_and_holds_no_name()
    {
        var (play, session, _) = WithRelic(
            ActFourEventRelicRules.CartoucheId, enemy: Scribe, intent: NameOffice);

        var opening = play.CombatDriver!.Current!.Hand.Count;

        play.CombatDriver.EndTurn();            // the office writes: 11 damage and an Inscribed
        Assert.Null(session.Error);
        Assert.Equal(0, FightProbe.StacksOf(Hero(play), ActFour.InscribedId));
        play.Dispose();

        var (bare, bareSession, _) = WithRelic(null, enemy: Scribe, intent: NameOffice);
        Assert.Equal(opening - 1, bare.CombatDriver!.Current!.Hand.Count);
        bare.CombatDriver.EndTurn();
        Assert.Null(bareSession.Error);
        Assert.Equal(1, FightProbe.StacksOf(Hero(bare), ActFour.InscribedId));
        bare.Dispose();
    }

    // ── Jar of Borrowed Breath ────────────────────────────────────────────────────────────────────────────

    [Fact]
    public void The_jar_gives_the_breath_back_when_an_affliction_leaves()
    {
        var (play, session, _) = WithRelic(
            ActFourEventRelicRules.JarId, heroStatuses: [("panic", 1)], startingHealth: 100);

        play.CombatDriver!.EndTurn();           // the last stack of Panic goes, and with it the Panic
        Assert.Null(session.Error);

        Assert.Equal(0, FightProbe.StacksOf(Hero(play), "panic"));
        Assert.Equal(103, Hero(play).Health.Current);
        var withJar = play.CombatDriver!.Current!.Hand.Count;
        play.Dispose();

        var (bare, bareSession, _) = WithRelic(null, heroStatuses: [("panic", 1)], startingHealth: 100);
        bare.CombatDriver!.EndTurn();
        Assert.Null(bareSession.Error);
        Assert.Equal(100, Hero(bare).Health.Current);
        Assert.Equal(withJar - 1, bare.CombatDriver!.Current!.Hand.Count);
        bare.Dispose();
    }

    // ── Broken Royal Weight ───────────────────────────────────────────────────────────────────────────────

    [Fact]
    public void The_broken_weight_takes_the_first_missed_measure_and_is_heavier_for_it()
    {
        var (play, session, _) = WithRelic(
            ActFourEventRelicRules.WeightId, heroStatuses: [(ActFour.WeighedId, 3)]);

        Assert.Equal(ActFourEventRelicRules.WeightBlock, Block(Hero(play)));

        play.CombatDriver!.EndTurn();           // nothing spent against a measure of three
        Assert.Null(session.Error);

        Assert.Equal(1, Hero(play).Counters[ActFour.MeasuresFailed]);
        Assert.Equal(ActFourEventRelicRules.WeightBlock, Block(Hero(play)));
        Assert.Equal(1, FightProbe.StacksOf(Hero(play), ActFour.BurdenedId));
        play.Dispose();
    }

    // ── Petition Chisel ───────────────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Three_marked_actions_file_the_wall_all_at_once()
    {
        var (play, session, _) = WithRelic(ActFourEventRelicRules.ChiselId, intent: Marking);

        // Three enemy actions, each of which marks you: three grievances.
        for (var i = 0; i < 3; i++)
        {
            play.CombatDriver!.EndTurn();
            Assert.Null(session.Error);
        }

        // …and the turn after the third is the filing: two more cards, a spare hand, and a stack struck off.
        Assert.Equal(1, FightProbe.StacksOf(Hero(play), ActFourEventRelicRules.SpareId));
        var filed = (Hand: play.CombatDriver!.Current!.Hand.Count,
                     Doubt: FightProbe.StacksOf(Hero(play), Keywords.Doubt));
        play.Dispose();

        var unfiled = AfterThreeMarkedTurns(relicId: null);
        Assert.Equal(unfiled.Hand + 2, filed.Hand);
        Assert.Equal(unfiled.Doubt - 1, filed.Doubt);
    }

    // Grievances are about being MARKED, not about being hit: an office that only guards leaves no complaint
    // on the wall, however long it goes on doing it.
    [Fact]
    public void A_quiet_office_leaves_nothing_on_the_wall()
    {
        var (play, session, _) = WithRelic(ActFourEventRelicRules.ChiselId);

        for (var i = 0; i < 3; i++)
        {
            play.CombatDriver!.EndTurn();
            Assert.Null(session.Error);
        }

        Assert.Equal(0, FightProbe.StacksOf(Hero(play), ActFourEventRelicRules.SpareId));
        play.Dispose();
    }

    // ── Tablet of the Missing Name ────────────────────────────────────────────────────────────────────────

    [Fact]
    public void The_missing_name_makes_the_first_blessing_one_larger_and_only_the_first()
    {
        var (play, session, target) = WithRelic(
            ActFourEventRelicRules.TabletId, deck: [.. Enumerable.Repeat(Wax, 12)]);

        Assert.Equal(1, FightProbe.StacksOf(Hero(play), ActFourEventRelicRules.AuthorityId));

        Play(play, session, Wax, target);
        Assert.Equal(WaxStacks + 1, FightProbe.StacksOf(Hero(play), Keywords.WardWax));
        Assert.Equal(0, FightProbe.StacksOf(Hero(play), ActFourEventRelicRules.AuthorityId));

        Play(play, session, Wax, target);
        Assert.Equal(WaxStacks + 1 + WaxStacks, FightProbe.StacksOf(Hero(play), Keywords.WardWax));
        play.Dispose();
    }

    // "If you have Inscribed afterward, remove 1 Inscribed." The throne will not have you registered twice,
    // and the invariant holds whichever of the two registers spends itself on the blessing: exactly ONE
    // enlargement reaches it, and the register is clear afterwards.
    [Fact]
    public void A_name_restored_is_not_entered_in_the_register_as_well()
    {
        var (play, session, target) = WithRelic(
            ActFourEventRelicRules.TabletId, deck: [.. Enumerable.Repeat(Wax, 12)],
            heroStatuses: [(ActFour.InscribedId, 1)]);

        Play(play, session, Wax, target);

        Assert.Equal(WaxStacks + 1, FightProbe.StacksOf(Hero(play), Keywords.WardWax));
        Assert.Equal(0, FightProbe.StacksOf(Hero(play), ActFour.InscribedId));
        play.Dispose();
    }

    // ── Funerary Linen Coil ───────────────────────────────────────────────────────────────────────────────

    [Fact]
    public void The_linen_wraps_the_first_card_put_out_of_the_fight_on_purpose()
    {
        var (play, session, target) = WithRelic(
            ActFourEventRelicRules.CoilId, deck: [Index, .. Enumerable.Repeat(Deed, 11)],
            startingHealth: 100);

        Play(play, session, Index, target);
        ArchiveTheFirstOffered(play, session);

        Assert.Equal(104, Hero(play).Health.Current);
        var wrapped = play.CombatDriver!.Current!.Hand.Count;
        play.Dispose();

        // The same play without the coil: the archive still happens, the card and the healing do not.
        var (bare, bareSession, bareTarget) = WithRelic(
            relicId: null, deck: [Index, .. Enumerable.Repeat(Deed, 11)], startingHealth: 100);
        Play(bare, bareSession, Index, bareTarget);
        ArchiveTheFirstOffered(bare, bareSession);

        Assert.Equal(100, Hero(bare).Health.Current);
        Assert.Equal(wrapped - 1, bare.CombatDriver!.Current!.Hand.Count);
        bare.Dispose();
    }

    // The clause the whole rule turns on: a card that exhausts ITSELF on being played made the same move out
    // of the same hand, and is not what the linen is for.
    [Fact]
    public void A_card_that_merely_exhausts_itself_is_not_worth_the_linen()
    {
        var (play, session, target) = WithRelic(
            ActFourEventRelicRules.CoilId, deck: [Docket, .. Enumerable.Repeat(Deed, 11)],
            startingHealth: 100);

        Play(play, session, Docket, target);

        Assert.Equal(100, Hero(play).Health.Current);
        play.Dispose();
    }

    // ── Mercy Counterweight ───────────────────────────────────────────────────────────────────────────────

    [Fact]
    public void The_merciful_pan_takes_a_stack_off_the_first_affliction()
    {
        var (play, session, _) = WithRelic(
            ActFourEventRelicRules.MercyId, intent: Marking, option: 0);

        play.CombatDriver!.EndTurn();          // the office marks you, and the weight takes it
        Assert.Null(session.Error);

        Assert.Equal(0, FightProbe.StacksOf(Hero(play), Keywords.Doubt));
        Assert.Equal(0, FightProbe.StacksOf(Hero(play), ActFourEventRelicRules.WardId));
        play.Dispose();
    }

    [Fact]
    public void The_empty_pan_takes_the_affliction_whole_and_is_paid_for_it()
    {
        var (play, session, _) = WithRelic(
            ActFourEventRelicRules.MercyId, intent: Marking, option: 1);

        play.CombatDriver!.EndTurn();
        Assert.Null(session.Error);

        Assert.Equal(1, FightProbe.StacksOf(Hero(play), Keywords.Doubt));
        Assert.Equal(1, FightProbe.StacksOf(Hero(play), ActFourEventRelicRules.SpareId));
        var paid = play.CombatDriver!.Current!.Hand.Count;
        play.Dispose();

        Assert.Equal(paid - 1, SecondHandAgainst(Marking, relicId: null));
    }

    // ── the shared shapes ─────────────────────────────────────────────────────────────────────────────────

    // The hand the second turn deals, with the relic or without it — the control every "draw one more" is
    // read against.
    private static int SecondHand(string? relicId)
    {
        var (play, session, target) = WithRelic(relicId, startingHealth: 100);
        Play(play, session, Deed, target);
        Play(play, session, Deed, target);
        play.CombatDriver!.EndTurn();
        Assert.Null(session.Error);
        var hand = play.CombatDriver!.Current!.Hand.Count;
        play.Dispose();
        return hand;
    }

    // Three turns of an office that marks you, without the chisel: the control both halves of the filing are
    // measured against, since Doubt is a debuff a fight can shed on its own.
    private static (int Hand, int Doubt) AfterThreeMarkedTurns(string? relicId)
    {
        var (play, session, _) = WithRelic(relicId, intent: Marking);
        for (var i = 0; i < 3; i++)
        {
            play.CombatDriver!.EndTurn();
            Assert.Null(session.Error);
        }

        var reading = (play.CombatDriver!.Current!.Hand.Count,
                       FightProbe.StacksOf(Hero(play), Keywords.Doubt));
        play.Dispose();
        return reading;
    }

    // The hand the second turn deals against a named intent — the control for "and a card at your next hand".
    private static int SecondHandAgainst(string intent, string? relicId)
    {
        var (play, session, _) = WithRelic(relicId, intent: intent, option: 1);
        play.CombatDriver!.EndTurn();
        Assert.Null(session.Error);
        var hand = play.CombatDriver!.Current!.Hand.Count;
        play.Dispose();
        return hand;
    }

    // A card that asks which card to archive: take whatever it offers first.
    private static void ArchiveTheFirstOffered(RunPlayback play, InteractiveRunSession session)
    {
        Assert.NotNull(play.CombatDriver!.PendingCardChoice);
        play.CombatDriver.SupplyCardChoice([play.CombatDriver.PendingCardChoice![0].Id]);
        Assert.Null(session.Error);
    }

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
        string? relicId, IReadOnlyList<string>? deck = null, int energy = 3, string enemy = Quiet,
        string intent = QuietIntent, int startingHealth = 400, int maxHealth = 400,
        int option = 0,
        params (string Status, int Stacks)[] heroStatuses)
    {
        var probe = heroStatuses.Length == 0
            ? FightProbe.Solo(enemy, intent, energy)
            : FightProbe.SoloAgainstHero(enemy, intent, energy, heroStatuses);
        var blueprint = FightProbe.OneFight(probe, deck ?? [.. Enumerable.Repeat(Deed, 12)]);
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

        // A rule may raise a prompt of its own at the first hand — the Mercy Counterweight asks which pan it
        // sits on — and under interactive replay an unanswered prompt PARKS the fight.
        if (play.CombatDriver!.PendingOptionChoice is not null)
        {
            play.CombatDriver.SupplyOptionChoice([option]);
            Assert.True(session.Error is null, session.Error);
        }

        var combat = play.CombatDriver!.Current!;
        return (play, session, combat.State.Combatants.First(c => c.Id != combat.HeroId).Id);
    }
}
