using RogueDeck.Run;
using static BnbContent.Converter.Relics.RelicAuthoring;

namespace BnbContent.Converter.Relics;

// The 38 Elite relics and the mimic's tooth — the pool the master does not have, added on the user's call
// (2026-09-10) so that the game's second-biggest fight stops paying out of the same bag as everything else.
//
// ONE RELIC PER ELITE, AND IT IS ALWAYS THAT ONE. A boss gives a forced 1-of-3 because an act's final
// examination should not be solved twice the same way; an elite is a body with a lesson, and the relic IS
// the lesson, so it is fixed. `Source` is therefore the ELITE ENCOUNTER ID, not prose: the map reads it to
// build that encounter's own victory reward (MapSpecBuilder.EliteRewards), and a typo is a conversion error
// rather than a relic nobody can win.
//
// Each speaks its own act's vocabulary. An Act-III relic that did not mention Safe-Conduct, Trespass, Claim
// or Wergild would be an Act-I relic found in a hedge.
public static class EliteRelics
{
    public static IReadOnlyList<BnbRelic> All() => [.. ActI, .. ActII, .. ActIII, .. ActIV, .. Mimics];

    // ── Act I — The City ──────────────────────────────────────────────────────────────────────────────────

    public static readonly IReadOnlyList<BnbRelic> ActI =
    [
        Elite(EliteRelicRules.ClimbedId, "The Case That Climbed", "city_elite_appeal_01",
            "The first time an enemy falls each fight, gain 1 Energy and draw a card.",
            combatRule: EliteRelicRules.CaseThatClimbed),

        Elite(EliteRelicRules.HalfSignedId, "Half-Signed Page", "city_elite_appeal_02",
            "The first card you play each fight costs nothing.",
            combatRule: EliteRelicRules.HalfSignedPage),

        Elite(EliteRelicRules.RemittiturId, "Remittitur Seal", "city_elite_appeal_03",
            "The first status an enemy puts on you each fight is sent back: 2 Paperwork on whoever filed it.",
            combatRule: EliteRelicRules.RemittiturSeal),

        Elite(EliteRelicRules.ThriceStruckId, "Thrice-Struck Appointment Card", "city_elite_delay_01",
            "Every third turn, draw one more card.",
            combatRule: EliteRelicRules.ThriceStruckAppointment),

        Elite(EliteRelicRules.ShutterKeyId, "Shutter Key of the Late Hour", "city_elite_delay_02",
            "End a turn having played nothing and the shutter pays for it: 12 Block and 1 Energy at your "
            + "next hand.",
            combatRule: EliteRelicRules.ShutterKeyLateHour),

        Elite(EliteRelicRules.ChairId, "Chair of the Ninth Hour", "city_elite_delay_03",
            "Two more cards in your opening hand, and one fewer on the turn after.",
            combatRule: EliteRelicRules.ChairOfTheNinthHour),

        Elite(EliteRelicRules.ContemptNailId, "Contempt Ledger Nail", "city_elite_enforcement_01",
            "Every Censure you take is filed against the weakest enemy: 2 Paperwork.",
            combatRule: EliteRelicRules.ContemptLedgerNail),

        // The Seizure Procession identifies property, removes access to it, and turns that into force. Its
        // relic is the identifying half kept and the rest given away: the lantern strikes things off a list.
        // No combat rule at all — a shop SERVICE is a thing that is simply true while the relic is worn, and
        // the shelf asks what the player is carrying as it is built, so this cannot miss its moment.
        Elite("inventory_lantern_glass", "Inventory Lantern Glass", "city_elite_enforcement_02",
            "Every shop will strike one card from your deck for nothing.",
            shopServices: [ShopService.RemoveCard(StandardRunIds.Gold, 0)]),

        Elite(EliteRelicRules.GateChainId, "Gate-Chain Counterweight", "city_elite_enforcement_03",
            "Deal 20 or more damage in one turn and your first card next turn costs 1 less.",
            combatRule: EliteRelicRules.GateChainCounterweight),

        Elite(EliteRelicRules.SpearheadId, "Sealed Spearhead", "city_elite_enforcement_04",
            "When an enemy falls, every other enemy takes 6 damage.",
            combatRule: EliteRelicRules.SealedSpearhead),
    ];

    // ── Act II — The Endless Archives ─────────────────────────────────────────────────────────────────────

    public static readonly IReadOnlyList<BnbRelic> ActII =
    [
        Elite(EliteRelicRules.BellLipId, "Cracked Bell-Lip", "archives_elite_after_hours_return_bell",
            "The first Overdue filed against you each fight: 10 Block and a card.",
            combatRule: EliteRelicRules.CrackedBellLip),

        Elite(EliteRelicRules.RollerPinId, "Roller Pin of the Stacks", "archives_elite_rolling_stacks_colossus",
            "The first card misfiled against you each fight is replaced.",
            combatRule: EliteRelicRules.RollerPinOfTheStacks),

        // ADAPTATION. The design wanted a card ENTERED in the book — written down once and thereafter always
        // in the opening hand. That is a per-card permanent mark applied at reward time, and the reward layer
        // marks offers, not the cards a run ends up holding. What the black book has instead is what a
        // catalogue actually offers: one more line. `DrawMoreOffersRule` asks the reward's OWN source for the
        // extra draw, so what appears is only ever what that reward could have offered anyway.
        Elite("blank_line_black_book", "Blank Line in the Black Book",
            "archives_elite_catalogue_of_unwise_names",
            "Every card reward shows one more card to choose from.",
            rewardRules: [new DrawMoreOffersRule(new RewardMatch(Kind: RewardKinds.Card), 1)]),

        Elite(EliteRelicRules.UnspokenId, "The Unspoken Word", "archives_elite_silence_between_two_words",
            "End a turn still holding two cards or more: 4 Block at your next hand.",
            combatRule: EliteRelicRules.TheUnspokenWord),

        Elite(EliteRelicRules.CensoringInkId, "Strip of Censoring Ink", "archives_elite_black_ink_oracle",
            "Every fight opens with 8 Block: the first blow is blacked out.",
            combatRule: EliteRelicRules.StripOfCensoringInk),

        Elite(EliteRelicRules.ConcordanceId, "The Line Between the Volumes",
            "archives_elite_volumes_of_cause_and_consequence",
            "The second card you play each turn strikes 4 harder.",
            combatRule: EliteRelicRules.ConcordanceThread),

        Elite(EliteRelicRules.DrawerId, "Drawer Within a Drawer", "archives_elite_drawer_of_infinite_returns",
            "The first card you exhaust each fight comes back to your discard pile.",
            combatRule: EliteRelicRules.DrawerWithinADrawer),

        Elite(EliteRelicRules.PresentHandId, "The Missing Present Hand", "archives_elite_presentless_clock",
            "Your opening turn is filed to the past: 6 Block and a card at your second hand.",
            combatRule: EliteRelicRules.MissingPresentHand),

        // ⚠ THE STRONGEST RELIC IN THIS POOL, and deliberately so — it is what an obituary that refuses to
        // end badly should hand over. The design said "once per RUN"; a run-layer death prevention is a
        // different machine from the one the engine has, and what it has is a per-STATUS one (bought at V-1
        // for Nisaba). Per fight is therefore what this is, which is more than the slate promised — the
        // first balance pass should look here before it looks anywhere else in this pool.
        Elite("the_third_ending", "The Third Ending", "archives_elite_obituary_with_three_endings",
            "Once each fight, the blow that would end you is struck through instead: you stand at 8.",
            combatRule: EliteRelicRules.TheThirdEnding),
    ];

    // ── Act III — The Green Docket ────────────────────────────────────────────────────────────────────────

    public static readonly IReadOnlyList<BnbRelic> ActIII =
    [
        Elite(EliteRelicRules.AntlerTineId, "Pre-Approved Antler Tine",
            "green_docket_elite_stag_of_pre_approved_violence",
            "The first Safe-Conduct you spend each fight is not spent.",
            combatRule: EliteRelicRules.PreApprovedAntlerTine),

        Elite(EliteRelicRules.MendedThreadId, "Mended Thread of the Grandmother",
            "green_docket_elite_grandmother_web",
            "Every fight opens with 1 Safe-Conduct, and spending one gives 3 Block.",
            combatRule: EliteRelicRules.MendedThread),

        Elite(EliteRelicRules.TollStoneId, "Toll-Stone from the Wrong Bank",
            "green_docket_elite_the_wrong_bridge_in_person",
            "The first Claim laid on you each fight also gives 6 Block.",
            combatRule: EliteRelicRules.TollStoneWrongBank),

        // The frog's lesson is that every payment becomes ammunition unless change is left behind. Its relic
        // is the change: Block inside the fight, and a purse outside it — because Gold is a RUN resource and
        // no combat effect can reach it.
        Elite(EliteRelicRules.ThroatCoinId, "Coin Left in the Throat", "green_docket_elite_great_toll_frog",
            "Every Claim laid on you leaves 2 Block behind, and every victory leaves 10 Gold.",
            runPrograms: [AfterEveryVictory(Gold(10))],
            combatRule: EliteRelicRules.CoinLeftInTheThroat),

        Elite(EliteRelicRules.ProperLineId, "The Head of the Line",
            "green_docket_elite_ant_queen_of_the_proper_line",
            "Play three cards in a turn and the line is kept: draw 2.",
            combatRule: EliteRelicRules.TheProperLine),

        Elite(EliteRelicRules.BoneTagId, "Bone Tag from the Juniper", "green_docket_elite_juniper_injunction",
            "The first affliction laid on you each fight is refused.",
            combatRule: EliteRelicRules.BoneTagFromTheJuniper),

        Elite(EliteRelicRules.BoundaryCordId, "Obsolete Boundary Cord",
            "green_docket_elite_surveyor_of_forgotten_paths",
            "Every turn in which something on you lapsed of its own accord: 3 Block.",
            combatRule: EliteRelicRules.ObsoleteBoundaryCord),

        Elite(EliteRelicRules.ReedId, "Reed Cut at the Hearing", "green_docket_elite_three_reeds_of_appeal",
            "The first Claim you lay each fight is laid twice.",
            combatRule: EliteRelicRules.ReedCutAtTheHearing),

        Elite(EliteRelicRules.ThornId, "Thorn Chosen From Three", "green_docket_elite_magistrate_of_thorns",
            "At every opening hand, choose: 10 Block, a card and 1 Energy, or 6 damage to every enemy.",
            combatRule: EliteRelicRules.ThornChosenFromThree),
    ];

    // ── Act IV — The Licensing Labyrinth ──────────────────────────────────────────────────────────────────

    public static readonly IReadOnlyList<BnbRelic> ActIV =
    [
        Elite(EliteRelicRules.ErrantCordId, "The Shorter Ferrule", "labyrinth_elite_surveyor_of_the_errant_cord",
            "The first Weighed you take each fight is one lighter.",
            combatRule: EliteRelicRules.TheErrantCord),

        Elite(EliteRelicRules.GranarySealId, "Broken Granary Seal",
            "labyrinth_elite_scarab_host_of_the_sealed_granary",
            "The first time each fight you strike an enemy that is behind Block: 8 more damage.",
            combatRule: EliteRelicRules.BrokenGranarySeal),

        Elite(EliteRelicRules.CorveeRopeId, "Cut Corvée Rope", "labyrinth_elite_rope_master_of_the_corvee",
            "Every third Burdened surcharge you pay gives 1 Energy back.",
            combatRule: EliteRelicRules.CutCorveeRope),

        Elite(EliteRelicRules.GlyphId, "Glyph Struck From the Name",
            "labyrinth_elite_keeper_of_the_living_cartouche",
            "The first time the register enlarges something against you each fight, it does not.",
            combatRule: EliteRelicRules.GlyphStruckFromTheName),

        Elite(EliteRelicRules.ShearsId, "Overseer's Linen Shears",
            "labyrinth_elite_mummified_overseer_of_the_linen_house",
            "The first thing that fades from you each fight: heal 3 and gain 3 Block.",
            combatRule: EliteRelicRules.OverseersLinenShears),

        // Both pans answer for themselves: the lighter one pays in coin. A fight cannot hand over Gold, so
        // the weight is a run program and says so.
        Elite("weight_from_the_lighter_pan", "Weight From the Lighter Pan",
            "labyrinth_elite_treasury_of_the_two_pans",
            "Every victory is weighed out in coin: 25 Gold.",
            runPrograms: [AfterEveryVictory(Gold(25))]),

        Elite(EliteRelicRules.RiddleId, "The Riddle's Third Answer",
            "labyrinth_elite_sphinx_of_the_processional_measure",
            "Every fight opens with 1 Ward Wax and 1 Seal.",
            combatRule: EliteRelicRules.RiddlesThirdAnswer),

        Elite(EliteRelicRules.WickId, "The Lamp Thief's Wick", "labyrinth_elite_the_tombbreakers_three",
            "When an enemy falls, gain 2 Block for every enemy still standing.",
            combatRule: EliteRelicRules.LampThiefsWick),

        Elite(EliteRelicRules.DecanChipId, "Decan Star-Table Chip",
            "labyrinth_elite_keeper_of_the_thirty_six_decans",
            "Every sixth turn, one affliction leaves you.",
            combatRule: EliteRelicRules.DecanStarTableChip),

        Elite(EliteRelicRules.StepStoneId, "Processional Step-Stone",
            "labyrinth_elite_colossus_of_the_endless_procession",
            "Every third turn, your first card costs nothing.",
            combatRule: EliteRelicRules.ProcessionalStepStone),
    ];

    // ── the mimic, one relic in four grades ───────────────────────────────────────────────────────────────

    // A mimic is rare enough that its prize should be worth the surprise, and rare enough that the same prize
    // twice would be a waste. So there is ONE tooth and four grades of it: a later act's mimic hands over a
    // bigger one and the smaller is removed as it does, which is why `Pickup` carries the removal of every
    // lesser grade rather than the grant carrying it. The player never holds two.
    public static readonly IReadOnlyList<BnbRelic> Mimics =
    [
        Tooth(1, "a splinter of the oak it grew through"),
        Tooth(2, "a brass hinge, still screwed to it"),
        Tooth(3, "the lock-plate, its keyhole ringed with tooth-marks"),
        Tooth(4, "the whole false lid, folded shut around it like a jaw at rest"),
    ];

    public static string ToothId(int grade) => EliteRelicRules.ToothId(grade);

    private static BnbRelic Tooth(int grade, string what) =>
        Mimic(ToothId(grade), $"Tooth From the False Lid {Roman(grade)}", grade.ToString(),
            $"The first blow of every fight glances off the tooth. It comes with {what}.",
            pickup: [.. Enumerable.Range(1, grade - 1).Select(lesser =>
                (IRunEffectRequest)new RemoveRelicRunEffect(new RelicId(ToothId(lesser))))],
            combatRule: grade switch
            {
                1 => EliteRelicRules.FalseLidToothI,
                2 => EliteRelicRules.FalseLidToothII,
                3 => EliteRelicRules.FalseLidToothIII,
                _ => EliteRelicRules.FalseLidToothIV,
            });

    private static string Roman(int n) => n switch { 1 => "I", 2 => "II", 3 => "III", _ => "IV" };

    // ── what the map asks ─────────────────────────────────────────────────────────────────────────────────

    // The relic that belongs to one elite encounter, or null if that encounter has none. Null is a conversion
    // error at the call site rather than a silent skip: an elite with no relic is a reward that pays nothing.
    public static BnbRelic? For(string eliteEncounterId) =>
        All().FirstOrDefault(r => r.Pool == Pool.Elite && r.Source == eliteEncounterId);

    // The tooth an act's mimic hands over.
    public static BnbRelic ForMimic(int act) =>
        Mimics.First(r => r.Source == Math.Clamp(act, 1, 4).ToString());
}
