using BnbContent.Converter;
using BnbContent.Converter.Relics;
using RogueDeck.Core.Combat;
using RogueDeck.Run;
using static BnbContent.Tests.RunBench;

namespace BnbContent.Tests;

// THE NORMAL RELICS THAT WORK BETWEEN FIGHTS, each held to its own text on a short road of real rooms (relic audit of
// 2026-11-20). The road is the authored game's own shop, campfire and events; the fight is a probe ring that pays no
// spoils of its own, so every coin and every hit point that changes is the relic's.
public class RunRelicBehaviourTests
{
    private static readonly EncounterDefinition Tablet =
        FightProbe.Solo(LieutenantTests.Quiet, LieutenantTests.QuietIntent, energy: 3);

    // Answer a road: fights are won, a shop buys what `buy` names (in order, while affordable) and then leaves, a
    // campfire does `fire`, an event takes its first choice, and every pick of cards takes the first offered.
    private static bool Walker(RunBench b, Queue<string>? buy = null, string fire = "rest")
    {
        if (b.Play.CombatDriver?.Current is not null) { b.WinFight(); return true; }
        if (b.Session.IsAwaitingEntities) { b.Session.PickEntities([0]); return true; }
        var choices = b.Choices;
        if (choices.Count == 0) return false;
        if (choices.Contains("leave") && choices.Any(c => c.StartsWith("buy-", StringComparison.Ordinal)
                                                         || c == "remove-card" || c == "reroll"))
        {
            // "|" in the list closes this shop: what follows is for the next one.
            while (buy is { Count: > 0 })
            {
                var want = buy.Peek();
                if (want == "|") { buy.Dequeue(); break; }
                var hit = choices.FirstOrDefault(c => want == "*" ? c.StartsWith("buy-", StringComparison.Ordinal) : c == want);
                if (hit is null) break;
                buy.Dequeue();
                b.Pick(hit);
                return true;
            }
            b.Pick("leave");
            return true;
        }
        if (choices.Contains("rest") && choices.Contains("amend")) { b.Pick(fire); return true; }
        b.Pick(choices[0]);
        return true;
    }

    private static Func<RunBench, bool> Walk(Queue<string>? buy = null, string fire = "rest") =>
        b => Walker(b, buy, fire);

    // ── the shop ─────────────────────────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Alms_basin_heals_once_in_each_shop()
    {
        using var r = Start([Shop, Shop], relics: ["alms_basin"], health: 20, gold: 600);
        r.Walk(Walk(new(["*", "*", "*", "|", "*", "*"])));
        // Two shops: 8 each, however much was bought in either.
        Assert.Equal(36, r.Health);
    }

    [Fact]
    public void Counterfeit_toll_writ_refunds_ten_for_every_purchase()
    {
        int Spent(string[] relics)
        {
            using var r = Start([Shop], relics: relics, gold: 600);
            r.Walk(Walk(new(["*", "*"])));
            return 600 - r.Gold;
        }
        Assert.Equal(Spent([]) - 20, Spent(["counterfeit_toll_writ"]));
    }

    // ── fights ───────────────────────────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Levy_stamp_adds_four_gold_to_every_victory()
    {
        using var r = Start([Fight, Fight], relics: ["levy_stamp"]);
        r.Walk(Walk());
        Assert.Equal(8, r.Gold);
    }

    [Fact]
    public void Grave_coin_pays_only_for_an_enemy_that_dies_under_a_debuff()
    {
        using (var r = Start([Fight], relics: ["grave_coin"], deck: [.. Enumerable.Repeat("paper_cut", 10)]))
        {
            r.Walk(Walk());
            Assert.Equal(0, r.Gold);
        }
        using var d = Start([Fight], relics: ["grave_coin"], deck: [.. Enumerable.Repeat("thrown_inkpot", 10)]);
        d.Walk(Walk());
        Assert.Equal(4, d.Gold);
    }

    [Fact]
    public void Executioners_measure_pays_eight_for_an_exact_kill()
    {
        var six = FightProbe.Roster("six", 3, (LieutenantTests.Biter, LieutenantTests.BiteIntent, 6));
        using (var r = Start([Fight], relics: ["executioners_measure"], fight: six))
        {
            r.Walk(Walk());
            Assert.Equal(8, r.Gold);
        }
        var five = FightProbe.Roster("five", 3, (LieutenantTests.Biter, LieutenantTests.BiteIntent, 5));
        using var o = Start([Fight], relics: ["executioners_measure"], fight: five);
        o.Walk(Walk());
        Assert.Equal(0, o.Gold);
    }

    // ── the road ─────────────────────────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Road_claim_token_heals_and_upgrades_in_an_event_room_only()
    {
        using (var r = Start(["event:misfiling_cabinet"], relics: ["road_claim_token"], health: 20))
        {
            // The room is entered as the road begins: the token has paid before anything is asked.
            Assert.Equal(25, r.Health);
            Assert.Equal(1, r.Upgraded);
        }
        using var s = Start([Shop, Rest], relics: ["road_claim_token"], health: 20);
        s.Walk(Walk(fire: "amend"));
        Assert.Equal(20, s.Health);
    }

    [Fact]
    public void Twin_ember_brazier_upgrades_on_a_rest_and_heals_on_a_smith()
    {
        int Upgrades(string fire, string[] relics, out int health)
        {
            using var r = Start([Rest], relics: relics, health: 20);
            r.Walk(Walk(fire: fire));
            health = r.Health;
            return r.Upgraded;
        }
        Assert.Equal(Upgrades("rest", [], out var restedHealth) + 1, Upgrades("rest", ["twin_ember_brazier"], out var restedWith));
        Assert.Equal(restedHealth, restedWith);
        Assert.Equal(Upgrades("amend", [], out var smithHealth), Upgrades("amend", ["twin_ember_brazier"], out var smithWith));
        Assert.Equal(smithHealth + 7, smithWith);
    }

    [Fact]
    public void Rootbound_walking_staff_rewards_the_fight_after_a_quiet_room_only()
    {
        (int Block, int Held) Opening(string[] road)
        {
            using var r = Start(road, relics: ["rootbound_walking_staff"]);
            var fights = 0;
            r.Walk(b =>
            {
                if (b.Play.CombatDriver?.Current is not null && ++fights == road.Count(s => s == Fight))
                    return false;
                return Walker(b);
            });
            var combat = r.Play.CombatDriver!.Current!;
            var hero = combat.State.GetCombatant(combat.HeroId);
            return (LieutenantTests.Block(hero), FightProbe.StacksOf(hero, "held_energy"));
        }
        Assert.Equal((6, 1), Opening([Shop, Fight]));
        Assert.Equal((0, 0), Opening([Fight]));            // the road's first room: nothing before it
        Assert.Equal((0, 0), Opening([Fight, Fight]));     // a fight straight after a fight
        Assert.Equal((6, 1), Opening([Fight, Rest, Fight]));
    }

    // ── what a relic does as it is taken ─────────────────────────────────────────────────────────────────────

    [Theory]
    [InlineData("levy_stamp")]
    [InlineData("counterfeit_toll_writ")]
    [InlineData("carved_bone_buckle")]
    [InlineData("blank_folio")]
    public void A_pickup_rides_with_every_grant_of_the_relic(string id)
    {
        var relic = FinalRelics.All().Single(r => r.Id == id);
        var grant = ConversionPools.Grant(relic);
        Assert.Equal(1 + relic.Pickup!.Count, grant.Count);
        Assert.IsType<AddRelicByIdRunEffect>(grant[0]);
        switch (id)
        {
            case "levy_stamp" or "counterfeit_toll_writ":
                Assert.Contains(grant, e => e is ChangeResourceRunEffect { Delta: 30 } g && g.Resource == StandardRunIds.Gold);
                break;
            case "carved_bone_buckle":
                Assert.Contains(grant, e => e is ChangeMaxHealthRunEffect { Delta: 4 });
                Assert.Contains(grant, e => e is HealRunEffect { Amount: 4 });
                break;
            case "blank_folio":
                Assert.Contains(grant, e => e is RemoveCardsRunEffect);
                break;
        }
    }

    // ── the Lieutenant's relics between fights ──────────────────────────────────────────────────────────────

    [Fact]
    public void Commission_carries_up_to_three_authority_into_the_next_fight()
    {
        int Opening(string[] relics)
        {
            var fight = FightProbe.SoloAgainstHero(LieutenantTests.Quiet, LieutenantTests.QuietIntent, 3,
                (Converter.Lieutenant.LieutenantKeywords.Authority, 5));
            using var r = Start([Fight, Fight], relics: relics, character: Converter.Lieutenant.LieutenantCharacter.Id,
                deck: [.. Enumerable.Repeat("lt_watch_cut", 10)], fight: fight);
            var fights = 0;
            r.Walk(b => b.Play.CombatDriver?.Current is not null && ++fights == 2 ? false : Walker(b));
            var combat = r.Play.CombatDriver!.Current!;
            return FightProbe.StacksOf(combat.State.GetCombatant(combat.HeroId), Converter.Lieutenant.LieutenantKeywords.Authority);
        }
        Assert.Equal(5, Opening([]));
        Assert.Equal(8, Opening(["lt_relic_commission"]));   // 5 of the fight's own and 3 kept, at the cap of 8
    }

    [Fact]
    public void Brandy_flask_heals_half_of_what_open_guard_cost_after_a_win()
    {
        int Health(string[] relics)
        {
            var fight = FightProbe.Roster("brandy", 3, (LieutenantTests.Biter, LieutenantTests.BiteIntent, 30));
            using var r = Start([Fight], relics: relics, character: Converter.Lieutenant.LieutenantCharacter.Id, health: 60,
                deck: [.. Enumerable.Repeat("lt_new_stripes", 3), .. Enumerable.Repeat("lt_watch_cut", 7)], fight: fight);
            r.Walk(Walk());
            return r.Health;
        }
        var without = Health([]);
        var with = Health(["lt_relic_brandy_flask"]);
        Assert.True(with > without, $"with {with}, without {without}");
        Assert.True(with - without <= 6, $"with {with}, without {without}");
    }
}
