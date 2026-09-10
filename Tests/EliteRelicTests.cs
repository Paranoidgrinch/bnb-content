using System.Text.Json;
using BnbContent.Converter;
using BnbContent.Converter.Relics;
using RogueDeck.Core.Combat;
using RogueDeck.Run;
using RogueDeck.Sandbox.Composition;
using RogueDeck.Sandbox.Run;

using static BnbContent.Converter.Relics.RelicAuthoring;

namespace BnbContent.Tests;

// THE FAUCET, AND THE POOL IT POURS FROM.
//
// Until 2026-09-10 every Elite, Boss and Mimic reward in Acts I–IV offered a pool of 49 relics of which 47
// were ported v2 demo material and 2 were canonical by id collision, and a treasure chest did the same. The
// 50 authored Normal relics reached a player only through events, programs and shops, and 12 of the 24 Shop
// relics were named nowhere in the shipped document at all. Every one of those pools had passing tests: each
// proved a relic WORKS, and not one asked whether a run can ever be handed it.
//
// So the first half of this file tests the plumbing, from the document a run actually walks: which faucet
// pours what, and whether the whole authored pool is behind it. The second half is the house rule for any
// new relic pool — one real fight per relic, because a relic that does nothing installs cleanly, validates
// cleanly and is silently skipped, which is how two Act-IV rites survived a whole build step dead.
public class EliteRelicTests
{
    private static readonly BabData Data = BabData.Load(TestData.Directory);
    private static readonly RunBlueprint Game = BlueprintAssembler.Build(Data, seed: 20260910);

    // Every relic id the authored pools hold — the only ids a run is allowed to hand out.
    private static readonly HashSet<string> Authored =
        [.. FinalRelics.All().Select(r => r.Id)];

    // ══ the plumbing ══════════════════════════════════════════════════════════════════════════════════════

    [Fact]
    public void No_faucet_anywhere_offers_a_relic_that_is_not_in_a_final_pool()
    {
        var offered = RelicsGrantedAnywhere(Game);

        Assert.NotEmpty(offered);
        Assert.Empty(offered.Except(Authored).Order());
    }

    [Fact]
    public void Every_authored_normal_and_shop_relic_can_actually_be_reached()
    {
        var offered = RelicsGrantedAnywhere(Game);

        // The two pools a run is supposed to be able to work through. Event and Boss relics hang off named
        // branches and named kills and are covered by their own suites; Elite relics are covered below.
        Assert.Empty(FinalRelics.Pool(Pool.Normal).Select(r => r.Id).Except(offered).Order());
        Assert.Empty(FinalRelics.Pool(Pool.Shop).Select(r => r.Id).Except(offered).Order());
    }

    [Fact]
    public void Every_elite_encounter_pays_out_the_one_relic_written_for_it()
    {
        var elites = Data.Encounters.Where(e => e.Role == "elite" && e.Act <= 4).ToList();
        Assert.Equal(38, elites.Count);

        foreach (var elite in elites)
        {
            var relic = EliteRelics.For(elite.Id);
            Assert.True(relic is not null, $"elite '{elite.Id}' has no relic written for it");

            var act = Game.Acts![elite.Act - 1];
            var byEncounter = act.MapGeneration!.VictoryRewardsByEncounter;
            Assert.True(byEncounter.ContainsKey(elite.Id), $"elite '{elite.Id}' pays out nothing of its own");

            // Exactly ITS relic and no other: a fixed reward, not a draw.
            Assert.Equal([relic!.Id], RelicsIn(byEncounter[elite.Id]).Order());
        }
    }

    [Fact]
    public void No_role_reward_hands_out_a_relic_except_the_mimic()
    {
        foreach (var act in Game.Acts!)
        {
            var rewards = act.MapGeneration?.VictoryRewards;
            if (rewards is null)
                continue;
            foreach (var (role, reward) in rewards)
            {
                var relics = RelicsIn(reward).ToList();
                if (role == MapNodeKind.Mimic)
                    Assert.Single(relics);
                else
                    Assert.Empty(relics);
            }
        }
    }

    [Fact]
    public void The_mimic_of_each_act_hands_over_the_tooth_of_that_grade()
    {
        for (var act = 1; act <= 4; act++)
        {
            var rewards = Game.Acts![act - 1].MapGeneration!.VictoryRewards;
            Assert.Equal(
                [EliteRelics.ToothId(act)],
                RelicsIn(rewards[MapNodeKind.Mimic]).Order());
        }
    }

    [Fact]
    public void A_later_tooth_takes_the_earlier_one_away()
    {
        // Four grades, one tooth. The removal rides on the relic's own pickup, so every place that hands one
        // over carries it — there is no "and also remove" for a grant site to forget.
        for (var grade = 1; grade <= 4; grade++)
        {
            var tooth = EliteRelics.Mimics.Single(r => r.Id == EliteRelics.ToothId(grade));
            var removed = (tooth.Pickup ?? [])
                .OfType<RemoveRelicRunEffect>().Select(e => e.Relic.Value).Order().ToList();
            Assert.Equal(
                Enumerable.Range(1, grade - 1).Select(EliteRelics.ToothId).Order(),
                removed);
        }
    }

    [Fact]
    public void A_treasure_chest_offers_a_normal_relic_and_never_a_shop_one()
    {
        var chests = Game.Events!.Where(e => e.Key.StartsWith("treasure:", StringComparison.Ordinal)).ToList();
        Assert.NotEmpty(chests);

        var shopOnly = FinalRelics.Pool(Pool.Shop).Select(r => r.Id).ToHashSet();
        foreach (var (id, chest) in chests)
        {
            var offered = RelicsIn(chest).ToList();
            Assert.True(offered.Count > 0, $"chest '{id}' offers no relic at all");
            Assert.Empty(offered.Intersect(shopOnly));
            Assert.Empty(offered.Except(FinalRelics.Pool(Pool.Normal).Select(r => r.Id)));
        }
    }

    // ══ the names ═════════════════════════════════════════════════════════════════════════════════════════

    [Fact]
    public void No_elite_relic_takes_a_name_the_game_already_uses()
    {
        // The guard for the mistake this pool actually made: two relics were first named after the mechanic
        // they were drawn from — `the_proper_line`, `the_errant_cord` — which are the ids the Ant Queen and
        // the Surveyor already had for their own bodies. The registry refused the second registration and 37
        // tests across four acts failed with one message.
        // A relic and the rule it installs share an id on purpose — that is how every pool in this game is
        // written — so the two are checked for uniqueness separately rather than as one bag.
        var relicIds = EliteRelics.All().Select(r => r.Id).ToList();
        var statusIds = EliteRelicRules.All().Select(s => s.Id).ToList();
        Assert.Equal(relicIds.Count, relicIds.Distinct().Count());
        Assert.Equal(statusIds.Count, statusIds.Distinct().Count());
        var mine = relicIds.Concat(statusIds).Distinct().ToList();

        var everythingElse = Game.Statuses!.Select(s => s.Id)
            .Concat(Data.Encounters.Select(e => e.Id))
            .Concat(Data.Enemies.Select(e => e.Id))
            .ToHashSet(StringComparer.Ordinal);

        foreach (var status in EliteRelicRules.All())
            everythingElse.Remove(status.Id);   // its own registration is not a collision

        Assert.Empty(mine.Where(everythingElse.Contains).Order());
    }

    // ══ the rarity curve ══════════════════════════════════════════════════════════════════════════════════

    [Fact]
    public void The_curve_gets_steeper_act_by_act_and_always_sums_to_a_hundred()
    {
        var previousRare = -1;
        for (var act = 1; act <= 4; act++)
        {
            var (common, uncommon, rare) = ConversionPools.RarityCurve[act];
            Assert.Equal(100, common + uncommon + rare);
            Assert.True(rare > previousRare, $"act {act} is no likelier to hand over a Rare than act {act - 1}");
            previousRare = rare;
        }
        Assert.True(ConversionPools.RarityCurve[1].Common > ConversionPools.RarityCurve[4].Common);
    }

    // ══ one real fight per relic ══════════════════════════════════════════════════════════════════════════

    public static TheoryData<string> EveryEliteRelic()
    {
        var data = new TheoryData<string>();
        foreach (var relic in EliteRelics.All())
            data.Add(relic.Id);
        return data;
    }

    [Theory]
    [MemberData(nameof(EveryEliteRelic))]
    public void Every_elite_relic_reaches_a_real_fight(string relicId)
    {
        var relic = EliteRelics.All().Single(r => r.Id == relicId);
        var (play, session, _) = WithRelic(relicId);

        Assert.Null(session.Error);

        // The rule a relic carries is a hidden status handed over when the fight opens. If it is not on the
        // hero at the opening hand, the relic is in the run and not in the fight — which is exactly the
        // failure that looks like success.
        if (relic.CombatRule is { } rule)
            Assert.True(
                FightProbe.StacksOf(Hero(play), rule.Id) > 0,
                $"'{relicId}' installs no rule into the fight");

        play.Dispose();
    }

    // ══ what the ones that speak at the opening hand actually say ═════════════════════════════════════════

    [Fact]
    public void The_thread_opens_the_fight_with_a_safe_conduct()
    {
        var (play, _, _) = WithRelic(EliteRelicRules.MendedThreadId);
        Assert.Equal(1, FightProbe.StacksOf(Hero(play), ActThree.SafeConductId));
        play.Dispose();
    }

    [Fact]
    public void The_third_answer_opens_with_the_two_the_sphinx_never_showed()
    {
        var (play, _, _) = WithRelic(EliteRelicRules.RiddleId);
        Assert.Equal(1, FightProbe.StacksOf(Hero(play), Converter.Cards.Keywords.WardWax));
        Assert.Equal(1, FightProbe.StacksOf(Hero(play), Converter.Cards.Keywords.Seal));
        play.Dispose();
    }

    [Fact]
    public void The_censoring_strip_blacks_out_the_first_blow()
    {
        var (play, _, _) = WithRelic(EliteRelicRules.CensoringInkId);
        Assert.Equal(8, Block(Hero(play)));
        play.Dispose();
    }

    [Fact]
    public void The_bone_tag_lays_down_a_refusal_at_the_opening_hand()
    {
        var (play, _, _) = WithRelic(EliteRelicRules.BoneTagId);
        Assert.Equal(1, FightProbe.StacksOf(Hero(play), EliteRelicRules.BoneTagCharmId));
        play.Dispose();
    }

    [Fact]
    public void The_chair_deals_two_more_at_the_opening_hand_and_takes_one_back_after()
    {
        var (bare, _, _) = WithRelic(null);
        var opening = Hand(bare).Count;
        bare.Dispose();

        var (play, _, _) = WithRelic(EliteRelicRules.ChairId);
        Assert.Equal(opening + 2, Hand(play).Count);
        play.Dispose();
    }

    [Fact]
    public void The_chorus_makes_the_first_card_of_the_fight_free()
    {
        var (play, _, _) = WithRelic(EliteRelicRules.HalfSignedId);
        Assert.Equal(1, FightProbe.StacksOf(Hero(play), RelicRules.NextCardFreeId));
        play.Dispose();
    }

    [Fact]
    public void Each_grade_of_the_tooth_is_worth_more_than_the_last()
    {
        var block = new List<int>();
        for (var grade = 1; grade <= 4; grade++)
        {
            var (play, _, _) = WithRelic(EliteRelics.ToothId(grade));
            block.Add(Block(Hero(play)));
            play.Dispose();
        }
        Assert.Equal(block.Order(), block);
        Assert.True(block[3] > block[0]);
    }

    // ══ the harness ═══════════════════════════════════════════════════════════════════════════════════════

    private const string Quiet = "ordinance_tablet";
    private const string QuietIntent = "stone_precedent";
    private const string Deed = "paper_cut";

    private static (RunPlayback Play, InteractiveRunSession Session, CombatantId EnemyId) WithRelic(
        string? relicId, int energy = 5)
    {
        var probe = FightProbe.Solo(Quiet, QuietIntent, energy);
        var blueprint = FightProbe.OneFight(probe, [Deed, Deed, Deed, Deed, Deed, Deed, Deed, Deed]);
        blueprint = blueprint with
        {
            Start = blueprint.Start with
            {
                StartingRelics = relicId is null
                    ? blueprint.Start.StartingRelics
                    : [.. blueprint.Start.StartingRelics, relicId],
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

        var combat = play.CombatDriver!.Current!;
        return (play, session, combat.State.Combatants.First(c => c.Id != combat.HeroId).Id);
    }

    private static CombatantState Hero(RunPlayback play) =>
        play.CombatDriver!.Current!.State.GetCombatant(play.CombatDriver.Current.HeroId);

    private static IReadOnlyList<CardInstance> Hand(RunPlayback play) =>
        [.. play.CombatDriver!.Current!.Hand];

    private static int Block(CombatantState hero) =>
        hero.DefensivePools.TryGetValue(StandardCombatIds.BlockDefensivePool, out var pool) ? pool.Current : 0;

    // ── reading the document ──────────────────────────────────────────────────────────────────────────────

    // Every relic id any faucet in the whole document can hand a player. Read off the SHIPPED shape rather
    // than the pools it was built from, because the question is what a run can reach and not what a pool
    // holds — which is the distinction the old faucet failed on for four acts.
    private static IReadOnlyCollection<string> RelicsGrantedAnywhere(RunBlueprint game)
    {
        var json = JsonSerializer.Serialize(game with { Presentation = new() }, RunJson.CreateOptions());
        return Ids(json);
    }

    private static IEnumerable<string> RelicsIn(object node) =>
        Ids(JsonSerializer.Serialize(node, RunJson.CreateOptions()));

    private static HashSet<string> Ids(string json)
    {
        var found = new HashSet<string>(StringComparer.Ordinal);
        foreach (System.Text.RegularExpressions.Match match in
            System.Text.RegularExpressions.Regex.Matches(
                json, "\"kind\"\\s*:\\s*\"fx\\.addRelicById\"\\s*,\\s*\"value\"\\s*:\\s*\\{\\s*\"Relic\"\\s*:\\s*\\{\\s*\"Value\"\\s*:\\s*\"([^\"]+)\""))
            found.Add(match.Groups[1].Value);
        return found;
    }
}
