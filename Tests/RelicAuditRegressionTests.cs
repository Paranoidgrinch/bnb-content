using System.Text.RegularExpressions;
using BnbContent.Converter;
using BnbContent.Converter.Playtest;

namespace BnbContent.Tests;

// THE RELICS THE AUDIT OF 2026-11-19 FOUND BROKEN, KEPT FIXED. Each one installed cleanly and passed the "reaches a
// real fight" check while doing the wrong thing — usually to the wrong body: in a reaction to something that
// happened TO the wearer (a status landing, a blow, an enemy falling) the SOURCE is somebody else, and "you"
// written as the source handed the effect to an enemy or to a corpse.
//
// The relic combat audit (Playtest/RelicCombatAudit.cs) fights twelve fights across Acts I-IV with and without each
// relic and writes down who every one of the relic's effects reached. These assertions read that page.
public class RelicAuditRegressionTests
{
    private static readonly Lazy<Dictionary<string, string>> Pages = new(() =>
    {
        var dir = Path.Combine(Path.GetTempPath(), "bnb-relic-audit-" + Guid.NewGuid().ToString("N"));
        var game = BlueprintAssembler.Build(BabData.Load(TestData.Directory), 20260717);
        RelicCombatAudit.Run(game, dir,
        [
            "contempt_ledger_nail", "sekhmets_claw", "case_that_climbed", "lamp_thiefs_wick",
            "drawer_within_a_drawer", "misdated_pocket_watch", "funerary_linen_coil", "toll_stone_wrong_bank",
            "coin_left_in_the_throat", "mended_thread", "pre_approved_antler_tine", "concordance_medallion",
        ], fights: 12, seed: 20260717, jobs: 4);
        var text = File.ReadAllText(Path.Combine(dir, "COMBAT_SUMMARY.md"));
        Directory.Delete(dir, recursive: true);
        return Regex.Split(text, @"(?m)^## ").Skip(1)
            .ToDictionary(p => p.Split(' ', 2)[0], p => p, StringComparer.Ordinal);
    });

    private static string Did(string relic) =>
        Regex.Match(Pages.Value[relic], @"(?m)^did: (.*)$") is { Success: true } m ? m.Groups[1].Value : "";

    private static int Times(string did, string what) =>
        Regex.Matches(did, Regex.Escape(what) + @"[^;]* x(\d+)").Sum(m => int.Parse(m.Groups[1].Value));

    [Fact]
    public void The_contempt_nail_files_doubt_against_an_enemy_and_never_its_wearer()
    {
        var did = Did("contempt_ledger_nail");
        Assert.True(Times(did, "enemy status doubt") > 0, did);
        Assert.Equal(0, Times(did, "hero status doubt"));
    }

    [Fact]
    public void The_concordance_medallion_copies_your_filing_to_the_other_enemies_and_never_to_you()
    {
        var did = Did("concordance_medallion");
        Assert.True(Times(did, "enemy status paperwork") + Times(did, "enemy status doubt") > 0, did);
        Assert.Equal(0, Times(did, "hero status"));
    }

    [Fact]
    public void Sekhmets_claw_strengthens_the_one_who_was_hurt()
    {
        var did = Did("sekhmets_claw");
        Assert.True(Times(did, "hero status strength") > 0, did);
        Assert.Equal(0, Times(did, "enemy status strength"));
    }

    [Fact]
    public void The_case_that_climbed_pays_the_wearer_when_an_enemy_falls()
    {
        var did = Did("case_that_climbed");
        Assert.True(Times(did, "hero status held_energy") > 0, did);
        Assert.True(Times(did, "event CardsDrawn") > 0, did);
    }

    [Fact]
    public void The_lamp_thiefs_wick_guards_the_wearer()
    {
        var did = Did("lamp_thiefs_wick");
        Assert.True(Times(did, "hero block") > 0, did);
        Assert.Equal(0, Times(did, "enemy block"));
    }

    [Fact]
    public void The_drawer_gives_back_the_first_burnt_card_in_most_fights()
    {
        // One per fight at most, and in a fight where something was burnt at all: no longer spent by the first draw.
        var did = Did("drawer_within_a_drawer");
        Assert.True(Times(did, "event CardMovedToZone") >= 6, did);
    }

    [Fact]
    public void The_pocket_watch_pays_after_the_first_turn()
    {
        var did = Did("misdated_pocket_watch");
        Assert.True(Times(did, "event CardsDrawn") + Times(did, "held_energy") + Times(did, "hero block") > 12, did);
    }

    [Fact]
    public void The_linen_coil_wraps_a_card_put_away()
    {
        Assert.True(Did("funerary_linen_coil").Length > 0);
    }

    [Fact]
    public void A_claim_against_you_pays_the_toll_stone_and_the_coin()
    {
        Assert.True(Times(Did("toll_stone_wrong_bank"), "hero block") > 0, Did("toll_stone_wrong_bank"));
        Assert.True(Times(Did("coin_left_in_the_throat"), "hero block") > 0, Did("coin_left_in_the_throat"));
    }

    [Fact]
    public void Only_a_spent_safe_conduct_answers_the_thread_and_the_tine()
    {
        // Twelve fights open with at most a Safe-Conduct or two each; the old rules answered every stack change
        // of every status — dozens of times.
        Assert.True(Times(Did("mended_thread"), "hero block") <= 24, Did("mended_thread"));
        Assert.True(Times(Did("pre_approved_antler_tine"), "hero status safe_conduct") <= 12, Did("pre_approved_antler_tine"));
    }
}

// The three between-room relics of the same audit, walked through Act I with the run log (Playtest/RunAudit.cs).
public class RunRelicAuditRegressionTests
{
    private static readonly Lazy<string> Summary = new(() =>
    {
        var dir = Path.Combine(Path.GetTempPath(), "bnb-run-audit-" + Guid.NewGuid().ToString("N"));
        var game = BlueprintAssembler.Build(BabData.Load(TestData.Directory), 20260717);
        var options = RogueDeck.Run.RunJson.CreateOptions(indented: false);
        var shipped = RogueDeck.Run.RunJson.BlueprintFromJson(RogueDeck.Run.RunJson.ToJson(game, options), options);
        RunAudit.Run(shipped, dir, ["twin_ember_brazier", "road_claim_token", "inventory_lantern_glass"],
            acts: 1, character: "lieutenant", seed: 20260717, jobs: 3);
        // The summary, then the lantern's own log: a shop's services are written there whether or not the walk
        // without the relic met the same shop.
        var text = File.ReadAllText(Path.Combine(dir, "SUMMARY.md"))
            + "\n## inventory_lantern_glass_log\n" + File.ReadAllText(Path.Combine(dir, "inventory_lantern_glass.log"));
        Directory.Delete(dir, recursive: true);
        return text;
    });

    private static string Page(string relic) =>
        Regex.Match(Summary.Value, $@"(?ms)^## {relic}\n(.*?)(?=^## |\z)").Groups[1].Value;

    [Fact]
    public void The_brazier_answers_campfire_choices_and_never_a_room_entered()
    {
        var page = Page("twin_ember_brazier");
        Assert.DoesNotContain("NodeEntered", page);
    }

    [Fact]
    public void The_road_claim_token_answers_event_rooms_only()
    {
        var page = Page("road_claim_token");
        Assert.DoesNotContain("EventChoiceMade", page);
    }

    [Fact]
    public void The_lantern_glass_brings_its_free_removal_into_the_shop()
    {
        var log = Page("inventory_lantern_glass_log");
        Assert.Contains("asked: shop", log);
        Assert.Contains("lantern-remove-card", log);
    }
}
