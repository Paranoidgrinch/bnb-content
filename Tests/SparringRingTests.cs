using RogueDeck.Run;
using BnbContent.Converter;
using BnbContent.Converter.Playtest;
using RogueDeck.Core.Combat;
using RogueDeck.Sandbox.Composition;
using RogueDeck.Sandbox.Run;

namespace BnbContent.Tests;

// THE SPARRING RING, PROVED. A snapshot states a fighter and an opponent; these tests are about the ONE claim
// that makes the instrument worth having — that what the snapshot says is what stands in the ring. An
// instrument that quietly drops half a loadout would answer balance questions with confident nonsense, which
// is worse than answering none.
public class SparringRingTests
{
    private static readonly RunBlueprint Game = FightProbe.Game;

    private const string Deed = "paper_cut";
    private const string Stapler = "suspiciously_helpful_stapler";   // combat opening: gain 8 Block

    // A PLAIN opponent for the tests that are about the LOADOUT rather than about a fight. A boss is the
    // wrong sparring partner for those: Nanna-Sin lays a card in the fighter's hand every morning and
    // restates his own chips at every turn start, so a test that counted the deck or read a status off him
    // would be measuring his rules and calling it the ring's.
    private const string Clerk = "clerkling_apprentice";

    private static SparringMatch.Result FightOnce(SparringRing.Snapshot snapshot) =>
        SparringMatch.Fight(SparringRing.Assemble(Game, snapshot), seed: 1, turnBudget: 40);

    // Start a ring and hand back the live fight, so a test can look at the table rather than at the report.
    private static (RunPlayback Play, InteractiveRunSession Session) Ring(SparringRing.Snapshot snapshot)
    {
        var play = new RunPlayback(() => { });
        play.Start(SparringRing.Assemble(Game, snapshot), seed: 1, interactive: true);
        Assert.True(play.Error is null, play.Error);
        var session = play.Session!;
        while (session.IsAwaitingInterlude)
            session.Continue();
        Assert.True(session.Error is null, session.Error);
        return (play, session);
    }

    private static CombatantState Hero(RunPlayback play) =>
        play.CombatDriver!.Current!.State.GetCombatant(play.CombatDriver.Current!.HeroId);

    // Everything the fighter is standing behind, whatever pool it is held in.
    private static int Guard(RunPlayback play) =>
        Hero(play).DefensivePools.Values.Sum(pool => pool.Current);

    // ── what the snapshot says is what stands in the ring ─────────────────────────────────────────────────

    // THE LOAD-BEARING CLAIM. A relic named in a snapshot is a relic the fighter is actually wearing — not
    // "listed somewhere", but granted, enabled, and with its combat rule installed in the fight.
    [Fact]
    public void A_relic_named_in_a_snapshot_is_worn_in_the_fight()
    {
        var bare = Ring(new SparringRing.Snapshot
        {
            Enemy = ActFive.NannaSinEnemyId, Deck = [.. Enumerable.Repeat(Deed, 12)],
        });
        var armed = Ring(new SparringRing.Snapshot
        {
            Enemy = ActFive.NannaSinEnemyId, Deck = [.. Enumerable.Repeat(Deed, 12)],
            Relics = [Stapler],
        });

        Assert.Empty(bare.Session.Run.Relics);
        var worn = Assert.Single(armed.Session.Run.Relics);
        Assert.Equal(Stapler, worn.Id.Value);
        Assert.True(worn.Enabled);

        // …and it is not decoration: the stapler's whole rule is 8 Block at the first bell, so the fighter
        // who brought it starts the fight behind more of it than the one who did not.
        Assert.True(Guard(armed.Play) > Guard(bare.Play),
            $"the relic granted no Block: {Guard(armed.Play)} vs {Guard(bare.Play)}");

        bare.Play.Dispose();
        armed.Play.Dispose();
    }

    // Health, the pool and the deck are the other three halves of a loadout, and each of them has a place in
    // the blueprint where a careless write would be silently overridden.
    [Fact]
    public void The_fighter_stands_on_the_health_the_pool_and_the_deck_it_was_given()
    {
        var (play, _) = Ring(new SparringRing.Snapshot
        {
            Enemy = Clerk,
            MaxHealth = 90, Health = 37, Energy = 5,
            Deck = [.. Enumerable.Repeat(Deed, 11)],
        });

        var hero = Hero(play);
        Assert.Equal(90, hero.Health.Max);
        Assert.Equal(37, hero.Health.Current);
        Assert.Equal(5, hero.Resources[StandardCombatIds.EnergyResource].Max);

        var zones = play.CombatDriver!.Current!.State.GetCardZones(play.CombatDriver.Current!.HeroId);
        var deck = new[] { CardZone.Hand, CardZone.DrawPile, CardZone.DiscardPile }
            .SelectMany(zones.GetCardsInZone).ToList();
        Assert.Equal(11, deck.Count);
        Assert.All(deck, card => Assert.Equal(Deed, card.DefinitionId.value));
        // ⚠ Counted against a PLAIN opponent on purpose. A fight that deals its own paper — Nanna-Sin lays a
        // Hold the Moon in hand every morning — puts a twelfth card in a deck of eleven, and it belongs to
        // the fight rather than to the loadout.
        play.Dispose();
    }

    // A status the snapshot puts on the fighter is ADDED to the room's own openers rather than replacing
    // them — the applicant marker is how every passive in this game tells "this happened to the player", and
    // a ring that dropped it would run a fight with half its rules silently inert.
    [Fact]
    public void A_stated_status_joins_the_rooms_own_openers_instead_of_replacing_them()
    {
        var (play, _) = Ring(new SparringRing.Snapshot
        {
            Enemy = Clerk, Deck = [.. Enumerable.Repeat(Deed, 12)],
            Statuses = new Dictionary<string, int> { ["ward_wax"] = 3 },
        });

        var hero = Hero(play);
        Assert.Equal(3, FightProbe.StacksOf(hero, "ward_wax"));
        Assert.Equal(1, FightProbe.StacksOf(hero, PassiveStatuses.ApplicantId));
        play.Dispose();
    }

    // Pinning an intent is the sharpest tool in the file: it turns "what does this boss cost on average" into
    // "what does THIS move cost", which is the question a designer actually has.
    [Fact]
    public void A_pinned_intent_is_the_only_move_the_enemy_is_left_holding()
    {
        var ring = SparringRing.Assemble(Game, new SparringRing.Snapshot
        {
            Enemy = ActFive.NannaSinEnemyId, Intent = "crown_of_ur",
        });

        var body = Assert.Single(Assert.Single(ring.Encounters).Enemies);
        var action = Assert.Single(body.Actions);
        Assert.Equal($"{ActFive.NannaSinEnemyId}.crown_of_ur", action.value);
        // …and it keeps everything else the game gives it: its real health and its own opening statuses.
        Assert.Equal(ActFive.NannaSinMaxHealth, body.MaxHealth);
        Assert.Contains(body.StartingStatuses ?? [],
            s => s.Status.value == ActFive.LunarCalendarId);
    }

    // An enemy standing on its own still carries the statuses the snapshot opens it with — how a late-phase
    // fight is measured without playing the early one first.
    [Fact]
    public void An_enemy_can_be_stood_up_already_carrying_something()
    {
        var (play, _) = Ring(new SparringRing.Snapshot
        {
            Enemy = Clerk, Deck = [.. Enumerable.Repeat(Deed, 12)],
            EnemyStatuses = new Dictionary<string, int> { ["paperwork"] = 4 },
        });

        var body = play.CombatDriver!.Current!.State.Combatants
            .First(c => c.DefinitionId.value == Clerk);
        Assert.Equal(4, FightProbe.StacksOf(body, "paperwork"));
        play.Dispose();
    }

    // ⚠ AND A BOSS MAY TAKE IT STRAIGHT BACK OFF, which is not the ring failing but the fight working. Every
    // marker on Nanna-Sin's ring is struck off and written again at each turn start, so standing him up in
    // the Old Moon lasts exactly until his calendar turns. A snapshot reaches a late phase of a fight like
    // his by giving it the ROUNDS it counts, not by pinning the chip that counts them.
    [Fact]
    public void A_boss_that_restates_its_own_chips_overwrites_what_the_snapshot_pinned()
    {
        var (play, _) = Ring(new SparringRing.Snapshot
        {
            Enemy = ActFive.NannaSinEnemyId, Deck = [.. Enumerable.Repeat(Deed, 12)],
            EnemyStatuses = new Dictionary<string, int> { [ActFive.OldMoonId] = 1 },
        });

        var lord = play.CombatDriver!.Current!.State.Combatants
            .First(c => c.DefinitionId.value.Contains("nanna_sin", StringComparison.Ordinal));
        Assert.Equal(0, FightProbe.StacksOf(lord, ActFive.OldMoonId));
        Assert.Equal(1, FightProbe.StacksOf(lord, ActFive.OrbitId));
        play.Dispose();
    }

    // ── the report ────────────────────────────────────────────────────────────────────────────────────────

    // A fight the hero walks out of is a fight the hero WON, and this is not the tautology it looks like: a
    // ring is a one-room run, so winning also ends the RUN, and the run's own result answers a different
    // question. Reading it here once said "LOST" about a fight that finished 21 HP up against three corpses.
    [Fact]
    public void A_fight_the_fighter_walks_out_of_is_reported_as_won()
    {
        var result = FightOnce(new SparringRing.Snapshot
        {
            Enemy = Clerk,
            Deck = [.. Enumerable.Repeat(Deed, 12)],
            MaxHealth = 400,
        });

        Assert.Null(result.Error);
        Assert.True(result.Ended, "the fight did not end inside the budget");
        Assert.True(result.Won, result.Line);
        Assert.True(result.HeroHealth > 0);
        Assert.Equal(0, result.EnemyHealthLeft);
    }

    // ── saying no usefully ────────────────────────────────────────────────────────────────────────────────

    // The ids in this game are long and the one in a designer's head is a fragment. An instrument that
    // answers a near miss with "no" and nothing else sends its user grepping the source data, which is the
    // errand it exists to save.
    [Fact]
    public void A_near_miss_says_what_was_probably_meant()
    {
        var thrown = Assert.Throws<InvalidOperationException>(() =>
            SparringRing.Assemble(Game, new SparringRing.Snapshot { Encounter = "tombbreakers" }));
        Assert.Contains("Did you mean", thrown.Message, StringComparison.Ordinal);
        Assert.Contains("tombbreakers", thrown.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void A_snapshot_fields_an_encounter_or_an_enemy_and_says_so_when_it_fields_neither()
    {
        Assert.Contains("must name", Assert.Throws<InvalidOperationException>(() =>
            SparringRing.Assemble(Game, new SparringRing.Snapshot())).Message, StringComparison.Ordinal);
        Assert.Contains("one or the other", Assert.Throws<InvalidOperationException>(() =>
            SparringRing.Assemble(Game, new SparringRing.Snapshot
            {
                Encounter = "labyrinth_elite_the_tombbreakers_three", Enemy = ActFive.NannaSinEnemyId,
            })).Message, StringComparison.Ordinal);
    }

    // ── the checked-in snapshots ──────────────────────────────────────────────────────────────────────────

    // A snapshot beside the content is a regression test that reads like a bug report — and it is only that
    // while every id in it is still an id. This is the test that fails the day somebody renames a card.
    [Theory]
    [InlineData("tombbreakers.json")]
    public void A_checked_in_snapshot_still_names_things_that_exist(string file)
    {
        var path = Path.Combine(TestData.Directory, "..", "snapshots", file);
        var snapshot = SparringRing.Read(path);
        var ring = SparringRing.Assemble(Game, snapshot);

        Assert.Single(ring.Encounters);
        var known = Game.Cards.Select(c => c.Id).ToHashSet(StringComparer.Ordinal);
        Assert.All(snapshot.Deck, card => Assert.Contains(card, known));
        var relics = Game.Relics.Select(r => r.Id).ToHashSet(StringComparer.Ordinal);
        Assert.All(snapshot.Relics, relic => Assert.Contains(relic, relics));
    }
}
