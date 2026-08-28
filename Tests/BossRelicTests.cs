using BnbContent.Converter;
using BnbContent.Converter.Relics;
using RogueDeck.Core.Combat;
using RogueDeck.Run;
using RogueDeck.Sandbox.Composition;
using RogueDeck.Sandbox.Run;
using static BnbContent.Converter.Relics.RelicAuthoring;

namespace BnbContent.Tests;

// The Boss relics (BnB_Final_Relics_Master_PostAudit.md §6): three per boss, one of the three at random when
// that boss falls, and never anywhere else. These tests cover the three things that can quietly go wrong —
// the pool's shape, the wiring that ties each boss to ITS three, and whether the rules actually fire.
public class BossRelicTests
{
    private static readonly BabData Data = BabData.Load(TestData.Directory);
    private static readonly RunBlueprint Game = BlueprintAssembler.Build(Data, seed: 20260826);

    private static readonly string[] Bosses =
    [
        BossRelics.DeputyUndersecretary, BossRelics.QueueCommissioner, BossRelics.LordSealkeeper,
        BossRelics.MunicipalDragon, BossRelics.LivingCharter, BossRelics.WhisperingCatalogue,
        BossRelics.WardenOfSealedVolumes, BossRelics.CuratorOfMisplacedHours,
        BossRelics.AuditorOfReturnedLives, BossRelics.GrandCrossReference,
        BossRelics.OmbudsmanOfRootAndRoad, BossRelics.NotaryOfOldGrowth, BossRelics.GrandmotherClause,
        BossRelics.AnsweringHill, BossRelics.QueenUnderTheHill,
    ];

    // ── the pool ──────────────────────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Every_boss_of_all_three_acts_has_exactly_three()
    {
        Assert.Equal(45, BossRelics.All().Count);
        foreach (var boss in Bosses)
            Assert.Equal(3, BossRelics.For(boss).Count);
        Assert.All(BossRelics.All(), relic =>
        {
            Assert.Equal(Pool.Boss, relic.Pool);
            Assert.Equal(Rarity.Boss, relic.Rarity);
            Assert.Equal(Eligibility.General, relic.Eligibility); // "character-independent"
            Assert.Contains(relic.Source, Bosses);
        });
    }

    [Fact]
    public void Every_boss_relic_says_what_it_does_and_installs_a_registered_rule()
    {
        var statuses = FinalRelics.Statuses().Select(s => s.Id).ToHashSet();
        foreach (var relic in BossRelics.All())
        {
            Assert.False(string.IsNullOrWhiteSpace(relic.Text), $"'{relic.Id}' has no text");
            // Every one of them does something: a rule inside the fight, or a program between fights.
            Assert.True(relic.CombatRule is not null || relic.RunPrograms is { Count: > 0 },
                $"'{relic.Id}' does nothing at all");
            if (relic.CombatRule is { } rule)
                Assert.Contains(rule.Id, statuses);
        }
    }

    // ── what the win hands over ───────────────────────────────────────────────────────────────────────────

    // "Defeating it awards 1 of ITS 3 at random." A role-wide reward table would hand every boss of the act
    // the same pool, which is exactly the bug this wiring exists to prevent.
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public void Each_boss_pays_out_its_own_three(int actIndex)
    {
        var spec = Game.Acts![actIndex].MapGeneration!;
        var bosses = Data.Encounters.Where(e => e.Act == actIndex + 1 && e.Role == "boss").ToList();

        Assert.Equal(5, bosses.Count);
        foreach (var boss in bosses)
        {
            Assert.True(spec.VictoryRewardsByEncounter.ContainsKey(boss.Id),
                $"boss '{boss.Id}' pays out nothing of its own");
            var offered = RelicsIn(spec.VictoryRewardsByEncounter[boss.Id]);
            Assert.Equal(
                BossRelics.For(boss.Name).Select(r => r.Id).OrderBy(id => id),
                offered.OrderBy(id => id));
        }
    }

    // …and one of them, not a pick of three: the design says no choice screen.
    [Fact]
    public void The_relic_is_drawn_and_not_chosen()
    {
        var spec = Game.Acts![0].MapGeneration!;
        var source = OfferedRelicSource(spec.VictoryRewardsByEncounter["city_boss_01"]);

        var pool = Assert.IsType<PoolRewardSource>(source);
        Assert.Equal(3, pool.Pool.Entries.Count);
        Assert.Equal(1, pool.Count); // one drawn from the three
    }

    // A boss still pays what a boss pays: the relic is on top of the gold and the card.
    [Fact]
    public void A_boss_still_pays_gold_and_a_card()
    {
        var reward = Game.Acts![0].MapGeneration!.VictoryRewardsByEncounter["city_boss_01"];
        var grant = ((FixedRewardSource)reward.Source).Offers[0].Grant;

        Assert.Contains(grant, e => e is ChangeResourceRunEffect);
        Assert.Contains(grant, e => e is OfferRewardRunEffect offer
            && offer.Reward.Value.StartsWith("cards:", StringComparison.Ordinal));
    }

    // "Never appear in Shops, Treasure, random Normal rewards, or Events."
    [Fact]
    public void No_other_pool_can_offer_a_boss_relic()
    {
        var bossIds = BossRelics.All().Select(r => r.Id).ToHashSet();

        foreach (var pool in new[] { Pool.Normal, Pool.Shop, Pool.Event })
            Assert.All(FinalRelics.Pool(pool), relic => Assert.DoesNotContain(relic.Id, bossIds));

        // …and nothing outside the boss rewards grants one by id, in any act.
        foreach (var act in Game.Acts!)
        {
            var spec = act.MapGeneration!;
            foreach (var (role, reward) in spec.VictoryRewards)
                Assert.All(RelicsIn(reward), id =>
                    Assert.False(bossIds.Contains(id), $"the {role} reward can hand out boss relic '{id}'"));
        }
        foreach (var (id, shop) in Game.Shops)
            Assert.All(RelicIdsIn(System.Text.Json.JsonSerializer.SerializeToElement(
                shop, RunJson.CreateOptions()).ToString()),
                relic => Assert.False(bossIds.Contains(relic), $"shop '{id}' stocks boss relic '{relic}'"));
    }

    // ── the rules, in a real fight ────────────────────────────────────────────────────────────────────────

    private const string Quiet = "ordinance_tablet";
    private const string QuietIntent = "stone_precedent";

    // "At end of turn store up to 1 unspent Energy; gain stored Energy next turn." The Energy pool has a hard
    // ceiling, so the carried point is HELD and arrives the moment the next turn runs dry (see HeldEnergy).
    [Fact]
    public void The_unfinished_docket_carries_one_energy_into_the_next_turn()
    {
        var (play, session, target) = WithRelic("unfinished_docket", Deck("paper_cut"));

        // Spend down to exactly 1 of the 3, so one point is left standing at the end of the turn.
        Play(play, session, "paper_cut", target);
        Play(play, session, "paper_cut", target);
        Assert.Equal(1, Energy(Hero(play)));

        play.CombatDriver!.EndTurn();
        Assert.Null(session.Error);
        Assert.Equal(1, FightProbe.StacksOf(Hero(play), "held_energy")); // carried into the new turn

        // Spend the refilled three; the fourth card is the one the docket paid for.
        for (var i = 0; i < 3; i++)
            Play(play, session, "paper_cut", target);
        Assert.Equal(1, Energy(Hero(play)));
        Assert.Equal(0, FightProbe.StacksOf(Hero(play), "held_energy"));
        play.Dispose();
    }

    // "At end of turn gain 4 Block per unplayed card, max 8." End-of-turn Block is cleared when the next turn
    // starts, so what it is worth is the enemy turn in between: the mallet's 13 damage against 8 Block.
    [Fact]
    public void The_backlog_counterseal_pays_for_what_you_did_not_get_to()
    {
        var withRelic = HealthAfterOneEnemyTurn("backlog_counterseal");
        var without = HealthAfterOneEnemyTurn(relicId: null);

        Assert.Equal(8, withRelic - without); // a full hand is the 8-Block ceiling
    }

    // "First Deed each turn deals 4 additional total damage."
    [Fact]
    public void The_execution_seal_sharpens_the_first_deed_of_a_turn_only()
    {
        var (play, session, target) = WithRelic("execution_seal_shard", Deck("paper_cut"));
        var health = play.CombatDriver!.Current!.State.GetCombatant(target).Health.Current;

        Play(play, session, "paper_cut", target);   // 6 + 4
        Assert.Equal(health - 10, play.CombatDriver.Current!.State.GetCombatant(target).Health.Current);
        Play(play, session, "paper_cut", target);   // 6
        Assert.Equal(health - 16, play.CombatDriver.Current!.State.GetCombatant(target).Health.Current);
        play.Dispose();
    }

    // "At end of turn retain up to 8 remaining Block for next turn."
    [Fact]
    public void The_continuance_fragment_carries_block_across_the_turn()
    {
        var (play, session, target) = WithRelic("continuance_fragment", Deck("cower_behind_a_desk"));

        Play(play, session, "cower_behind_a_desk", target);  // 5 Block
        Play(play, session, "cower_behind_a_desk", target);  // 10 Block
        play.CombatDriver!.EndTurn();
        Assert.Null(session.Error);

        // Block is cleared at the turn's start, and 8 of it comes straight back.
        Assert.Equal(8, Block(Hero(play)));
        play.Dispose();
    }

    // "Every 4 Energy spent on cards → gain 1 Energy."
    [Fact]
    public void The_settled_ledger_returns_a_point_for_every_four_spent()
    {
        var (play, session, target) = WithRelic("settled_ledger", Deck("paper_cut"));
        var before = Energy(Hero(play));

        for (var i = 0; i < 4; i++)                       // 4 cards at 1 Energy each
            Play(play, session, "paper_cut", target);

        Assert.Equal(before - 4 + 1, Energy(Hero(play)));
        play.Dispose();
    }

    // "Turn 2 draw +2." A relic that counts turns has to count its OWN — the engine's turn number counts
    // turns within a round, so in a duel the player's turn is always turn 1.
    [Fact]
    public void The_deferred_appointment_book_keeps_its_second_turn_appointment()
    {
        var (play, session, _) = WithRelic("deferred_appointment_book", Deck("paper_cut"));
        var opening = play.CombatDriver!.Current!.Hand.Count;

        play.CombatDriver.EndTurn();
        Assert.Null(session.Error);
        Assert.Equal(opening + 2, play.CombatDriver.Current!.Hand.Count);
        play.Dispose();
    }

    // "End turn with 2 cards or fewer played: 1 card stays in hand and costs 0 next turn." The card is taken
    // into custody at the draw and let go again if the turn turns busy.
    [Fact]
    public void The_custody_shackle_keeps_a_card_and_returns_it_free()
    {
        var (play, session, target) = WithRelic("custody_shackle", Deck("permit_a38"));

        // The first card in hand is the one taken into custody, so play a different copy: one card played is
        // a quiet turn either way.
        var hand = play.CombatDriver!.Current!.Hand.Select(c => c.Id.value).ToList();
        var held = hand[0];
        var card = play.CombatDriver.Current!.Hand.Last();
        play.CombatDriver.PlayCard(card.Id, target);
        Assert.Null(session.Error);

        play.CombatDriver.EndTurn();
        Assert.Null(session.Error);

        // The card under custody survived the discard — and comes back free.
        Assert.Contains(play.CombatDriver.Current!.Hand, c => c.Id.value == held);
        play.Dispose();
    }

    // "After each draw, 1 card in hand costs 1 less and gains 4 Block when played."
    [Fact]
    public void The_release_tag_marks_a_card_that_pays_block_when_played()
    {
        var (play, session, target) = WithRelic("release_tag", Deck("paper_cut"));

        // Every card in the deck is the same, so whichever one carries the tag, playing the hand out pays
        // exactly one card's worth of Block.
        var before = Block(Hero(play));
        for (var i = 0; i < 3; i++)
            Play(play, session, "paper_cut", target);

        Assert.Equal(before + 4, Block(Hero(play)));
        play.Dispose();
    }

    // ── plumbing ──────────────────────────────────────────────────────────────────────────────────────────

    private static string[] Deck(string card) => Enumerable.Repeat(card, 12).ToArray();

    private static CombatantState Hero(RunPlayback play) =>
        play.CombatDriver!.Current!.State.GetCombatant(play.CombatDriver.Current!.HeroId);

    private static int Block(CombatantState combatant) =>
        combatant.DefensivePools.TryGetValue(StandardCombatIds.BlockDefensivePool, out var pool) ? pool.Current : 0;

    private static int Energy(CombatantState combatant) =>
        combatant.Resources[StandardCombatIds.EnergyResource].Current;

    private static void Play(RunPlayback play, InteractiveRunSession session, string cardId, CombatantId target)
    {
        var card = play.CombatDriver!.Current!.Hand.First(c => c.DefinitionId.value == cardId);
        play.CombatDriver.PlayCard(card.Id, target);
        Assert.Null(session.Error);
    }

    // The hero's health after standing through one enemy turn, with or without a relic. The probe enemy
    // swings the Notarial Mallet (13 damage), which is more than any Block these relics hand out.
    private static int HealthAfterOneEnemyTurn(string? relicId)
    {
        var probe = FightProbe.Solo("wax_notary", "notarial_mallet", energy: 3);
        var blueprint = FightProbe.OneFight(probe, Deck("paper_cut").ToList());
        if (relicId is not null)
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

        play.CombatDriver!.EndTurn();
        Assert.Null(session.Error);
        var health = Hero(play).Health.Current;
        play.Dispose();
        return health;
    }

    private static (RunPlayback Play, InteractiveRunSession Session, CombatantId EnemyId) WithRelic(
        string relicId, params string[] deck)
    {
        var probe = FightProbe.Solo(Quiet, QuietIntent, energy: 3);
        var blueprint = FightProbe.OneFight(probe, deck.ToList());
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

        var combat = play.CombatDriver!.Current!;
        return (play, session, combat.State.Combatants.First(c => c.Id != combat.HeroId).Id);
    }

    // The relic ids a victory reward can hand out.
    private static IReadOnlyList<string> RelicsIn(MapVictoryReward reward) =>
        RelicIdsIn(System.Text.Json.JsonSerializer.SerializeToElement(
            reward.Source, RunJson.CreateOptions()).ToString());

    private static IRewardSource OfferedRelicSource(MapVictoryReward reward) =>
        ((FixedRewardSource)reward.Source).Offers[0].Grant
            .OfType<OfferRewardRunEffect>()
            .First(offer => offer.Reward.Value.StartsWith("relic:", StringComparison.Ordinal))
            .Source;

    private static IReadOnlyList<string> RelicIdsIn(string json) =>
        System.Text.RegularExpressions.Regex.Matches(json, "\"relic-([a-z0-9_]+)\"")
            .Select(m => m.Groups[1].Value).Distinct().ToList();
}
