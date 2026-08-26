using RogueDeck.Run;
using static BnbContent.Converter.Relics.RelicAuthoring;

namespace BnbContent.Converter.Relics;

// The Boss relics (BnB_Final_Relics_Master_PostAudit.md §6): every Act-I and Act-II boss has exactly three,
// and beating it hands over ONE of its three at random — no choice screen. They are character-independent and
// never appear in a shop, a treasure, an event or a normal reward.
//
// `Source` is the boss's name as the design writes it, and it is load-bearing: the map builder reads it to give
// each boss encounter its own three (see MapSpecBuilder.BossRewards).
public static class BossRelics
{
    // The bosses, by the name the design gives them.
    public const string DeputyUndersecretary = "The Deputy Undersecretary";
    public const string QueueCommissioner = "The Queue Commissioner";
    public const string LordSealkeeper = "The Lord Sealkeeper";
    public const string MunicipalDragon = "The Municipal Dragon";
    public const string LivingCharter = "The Living Charter";
    public const string WhisperingCatalogue = "The Whispering Catalogue";
    public const string WardenOfSealedVolumes = "The Warden of Sealed Volumes";
    public const string CuratorOfMisplacedHours = "The Curator of Misplaced Hours";
    public const string AuditorOfReturnedLives = "The Auditor of Returned Lives";
    public const string GrandCrossReference = "The Grand Cross-Reference";

    public static IReadOnlyList<BnbRelic> All() =>
    [
        // ── Act I ─────────────────────────────────────────────────────────────────────────────────────────
        Boss("unfinished_docket", "Unfinished Docket", DeputyUndersecretary,
            "At the end of your turn, store up to 1 unspent Energy. You gain it next turn.",
            combatRule: BossRelicRules.UnfinishedDocket),
        Boss("red_ribboned_matter", "Red-Ribboned Matter", DeputyUndersecretary,
            "At the end of your turn, keep 1 card in hand. It costs 1 less next turn.",
            combatRule: BossRelicRules.RedRibbonedMatter),
        Boss("backlog_counterseal", "Backlog Counterseal", DeputyUndersecretary,
            "At the end of your turn, gain 4 Block for each card left in your hand, up to 8.",
            combatRule: BossRelicRules.BacklogCounterseal),

        Boss("brass_service_bell", "Brass Service Bell", QueueCommissioner,
            "At the start of every third turn, gain 1 Energy and draw 1 card.",
            combatRule: BossRelicRules.BrassServiceBell),
        Boss("priority_sash", "Priority Sash", QueueCommissioner,
            "The first time each turn you deal 15 or more damage, gain 8 Block.",
            combatRule: BossRelicRules.PrioritySash),
        Boss("ivory_number_disc", "Ivory Number Disc", QueueCommissioner,
            "Two enemy turns in a row without losing HP: gain 1 Energy and draw 1 card.",
            combatRule: BossRelicRules.IvoryNumberDisc),

        Boss("access_seal_shard", "Access Seal-Shard", LordSealkeeper,
            "At the start of each combat, gain 1 Energy and draw 1 extra card.",
            combatRule: BossRelicRules.AccessSealShard),
        Boss("testimony_seal_shard", "Testimony Seal-Shard", LordSealkeeper,
            "At the start of each combat, gain 8 Block and refuse the first negative status applied to you.",
            combatRule: BossRelicRules.TestimonySealShard),
        Boss("execution_seal_shard", "Execution Seal-Shard", LordSealkeeper,
            "Your first Deed each turn deals 4 additional damage.",
            combatRule: BossRelicRules.ExecutionSealShard),

        Boss("stamped_expedition_writ", "Stamped Expedition Writ", MunicipalDragon,
            "Once per combat, when you run out of Energy with cards in hand, gain 2 Energy.",
            combatRule: BossRelicRules.StampedExpeditionWrit),
        Boss("civic_entry_warrant", "Civic Entry Warrant", MunicipalDragon,
            "Once per combat, when you run out of Energy with cards in hand, gain 1 Energy and strip every enemy's Block.",
            combatRule: BossRelicRules.CivicEntryWarrant),
        Boss("inspectors_brass_charter", "Inspector's Brass Charter", MunicipalDragon,
            "At the start of each combat, gain 8 Block.",
            combatRule: BossRelicRules.InspectorsBrassCharter),

        Boss("continuance_fragment", "Continuance Fragment", LivingCharter,
            "At the end of your turn, up to 8 Block carries into your next turn.",
            combatRule: BossRelicRules.ContinuanceFragment),
        Boss("right_of_redress", "Right of Redress", LivingCharter,
            "The first time you have lost 12 HP in a combat, your next turn opens with 15 Block and 2 cards.",
            combatRule: BossRelicRules.RightOfRedress),
        Boss("margin_of_appeal", "Margin of Appeal", LivingCharter,
            "Once per combat, the enemies' next turn is appealed: they deal half damage.",
            combatRule: BossRelicRules.MarginOfAppeal),

        // ── Act II ────────────────────────────────────────────────────────────────────────────────────────
        Boss("errata_ribbon", "Errata Ribbon", WhisperingCatalogue,
            "Play 3+ cards or fewer than 3 — change from last turn for 1 Energy, repeat it for 6 Block.",
            combatRule: BossRelicRules.ErrataRibbon),
        Boss("index_of_contradictions", "Index of Contradictions", WhisperingCatalogue,
            "The first time each turn you play a card of a different type than the one before, draw 1 and gain 3 Block.",
            combatRule: BossRelicRules.IndexOfContradictions),
        Boss("registry_tab", "Registry Tab", WhisperingCatalogue,
            "The type you played most on turn 1 is registered: its first card each turn costs 1 less.",
            combatRule: BossRelicRules.RegistryTab),

        Boss("custody_shackle", "Custody Shackle", WardenOfSealedVolumes,
            "End a turn having played 2 cards or fewer and 1 card stays in hand, free next turn.",
            combatRule: BossRelicRules.CustodyShackle),
        Boss("master_release_key", "Master Release Key", WardenOfSealedVolumes,
            "On turn 1, seal a card of your choice in hand. Next turn it costs 0.",
            combatRule: BossRelicRules.MasterReleaseKey),
        Boss("release_tag", "Release Tag", WardenOfSealedVolumes,
            "After each draw, 1 card in hand costs 1 less and gains you 4 Block when played.",
            combatRule: BossRelicRules.ReleaseTag),

        Boss("misdated_pocket_watch", "Misdated Pocket Watch", CuratorOfMisplacedHours,
            "Your last turn pays this one: no cards → 8 Block, 1–2 → 1 Energy, 3+ → draw 1.",
            combatRule: BossRelicRules.MisdatedPocketWatch),
        Boss("borrowed_minute", "Borrowed Minute", CuratorOfMisplacedHours,
            "Borrow 1 Energy at the start of a turn; the next turn repays it and gains 4 Block.",
            combatRule: BossRelicRules.BorrowedMinute),
        Boss("deferred_appointment_book", "Deferred Appointment Book", CuratorOfMisplacedHours,
            "Turn 2: draw 2. Turn 3: gain 2 Energy. Turn 4: gain 15 Block.",
            combatRule: BossRelicRules.DeferredAppointmentBook),

        Boss("identity_writ", "Identity Writ", AuditorOfReturnedLives,
            "The first time each turn you play a second card of the same type, draw 1. If you never do, gain 5 Block at the end of the turn.",
            combatRule: BossRelicRules.IdentityWrit),
        Boss("settled_ledger", "Settled Ledger", AuditorOfReturnedLives,
            "Every 4 Energy you spend on cards returns 1 Energy.",
            combatRule: BossRelicRules.SettledLedger),
        // The only one of the thirty with no rule inside the fight: it is paid out after it.
        Boss("closure_writ", "Closure Writ", AuditorOfReturnedLives,
            "After winning a combat, heal a quarter of the health you are missing, up to 10.",
            runPrograms:
            [
                AfterEveryVictory(new ComputedHealRunEffect(
                    RunExpr.Min(
                        RunExpr.Const(10),
                        RunExpr.Divide(RunExpr.MissingHealth, RunExpr.Const(4))))),
            ]),

        Boss("premise_slip", "Premise Slip", GrandCrossReference,
            "Your first card each turn is a premise: cards of other types cost 1 less until you follow it, and following it in kind gains 6 Block.",
            combatRule: BossRelicRules.PremiseSlip),
        Boss("concordance_thread", "Concordance Thread", GrandCrossReference,
            "After each draw, 1 card in hand is threaded: playing it draws a card that costs 1 less this turn.",
            combatRule: BossRelicRules.ConcordanceThread),
        Boss("conclusion_leaf", "Conclusion Leaf", GrandCrossReference,
            "The last card type you played decides your next turn: Deed → your first Deed deals 8 more, Working → 8 Block, Rite → draw 1.",
            combatRule: BossRelicRules.ConclusionLeaf),
    ];

    // A boss's own three, in the order the design lists them.
    public static IReadOnlyList<BnbRelic> For(string boss) =>
        All().Where(relic => relic.Source == boss).ToList();
}
