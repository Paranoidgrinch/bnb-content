using System.Text.Json;

namespace BnbContent.Converter;

// ── THE CARD PROGRESSION, AS THE GAME DOCUMENT CARRIES IT (META_PROGRESSION_PLAN.md §1, user 2026-11-17/18) ──
// The Lieutenant starts with 30 of her 80 cards and 10 of her 19 relics, and earns the rest over twelve levels:
// every level from 2 to 13 is a booster (a sealed folder in the Safe Deposit Box) that deepens one mechanic, or
// bridges two. Variety over power (user, 2026-11-18): the start set was bot-checked against the full pool
// (bnb-balance/bots/startset-20261117) and wins Act I as well, so the boosters bring ways to play, not strength.
//
// The relics the start does not hold ride in the boosters of their mechanic (the plan's relic chests were never
// specified; one folder is simpler than a second faucet). The frontend (bnb-godot CardProgression.cs) reads this
// from Extra["progression:<character>"], counts XP and locks what is not owned yet for normal runs only.
public static class Progression
{
    public const string ExtraPrefix = "progression:";

    public sealed record Booster(int Level, string Name, IReadOnlyList<string> Cards, IReadOnlyList<string> Relics);

    public sealed record Xp(int Room, int Elite, int Boss, int Win);

    public sealed record Definition(
        string Character, IReadOnlyList<string> StartCards, IReadOnlyList<string> StartRelics,
        IReadOnlyList<Booster> Boosters, Xp Xp, IReadOnlyList<int> LevelCosts);

    private static string[] Lt(string ids) => [.. ids.Split(' ', StringSplitOptions.RemoveEmptyEntries).Select(id => "lt_" + id)];
    private static string[] LtRelics(string ids) => [.. ids.Split(' ', StringSplitOptions.RemoveEmptyEntries).Select(id => "lt_relic_" + id)];

    // XP: a room 1, an elite 3, a boss 10, the win 15. Level 2 after about one Act I run; level n → n+1 costs
    // 30 + 15·(n−1), 1350 in all — about 25 runs.
    public static readonly Xp Pay = new(1, 3, 10, 15);
    public static readonly IReadOnlyList<int> LevelCosts = [.. Enumerable.Range(1, 12).Select(n => 30 + 15 * (n - 1))];

    public static Definition Lieutenant { get; } = new(
        "lieutenant",
        Lt("""
           pike_butt cut_the_strap clear_the_square split_lip heel_turn clenched_jaw whetstone armguard arrest marked_man roll_call second_rank
           heave shield_punch broken_pike scar front_rank bloody_cobbles gauntlet pursuit against_the_wall no_excuses flank whistle
           armsmaster last_ditch black_gauntlet drum_major rising_star splintered_shield
           """.Replace('\n', ' ')),
        LtRelics("chipped_buckler split_chinstrap tin_fife dented_gorget parade_spur pay_chest sword_knot warrant_cord regimental_drum brandy_flask"),
        [
            new(2, "Orders I", Lt("gatehouse cadence double_file parade_ground"), LtRelics("worn_epaulette")),
            new(3, "Orders II", Lt("last_bell scabbard no_surrender chain_of_command"), []),
            new(4, "Challenges I", Lt("braced_pike no_running second_blade service_record"), LtRelics("badge_chain")),
            new(5, "Challenges II", Lt("witness dare on_your_feet redress"), []),
            new(6, "Open Guard I", Lt("headlong recoil ruckus full_tilt"), LtRelics("lead_knuckles")),
            new(7, "Orders and Challenges", Lt("testimony watchword field_pardon watch_surgeon"), []),
            new(8, "Orders, Strength and Open Guard", Lt("mind_the_gap under_the_colours reprimand insubordination"), LtRelics("scarlet_epaulette")),
            new(9, "Challenges, Strength and Authority", Lt("remand riposte succession countercharge"), LtRelics("iron_shackle")),
            new(10, "Open Guard II", Lt("buckle overreach broken_nose open_visor"), []),
            new(11, "Authority", Lt("signal dispatch poise brevet"), LtRelics("commission")),
            new(12, "Strength", Lt("arms_drill grip overhand hilt_knot pike_square"), LtRelics("full_tang spur_buckle")),
            new(13, "The final folder", Lt("make_an_example ultimatum stand_down blood_debt forced_march"), LtRelics("torn_standard")),
        ],
        Pay,
        LevelCosts);

    public static IReadOnlyList<Definition> All => [Lieutenant];

    public static IEnumerable<KeyValuePair<string, string>> Extra() =>
        All.Select(d => new KeyValuePair<string, string>(ExtraPrefix + d.Character, JsonSerializer.Serialize(d)));
}
