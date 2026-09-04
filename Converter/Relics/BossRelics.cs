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

    // Act III. The relic master calls the fourth of these "The Hill That Answers"; the boss master calls it
    // "The Answering Hill", and the encounter is named for the boss master, so the boss master wins.
    public const string OmbudsmanOfRootAndRoad = "The Ombudsman of Root and Road";
    public const string NotaryOfOldGrowth = "The Notary of Old Growth";
    public const string GrandmotherClause = "Grandmother Clause";
    public const string AnsweringHill = "The Answering Hill";
    public const string QueenUnderTheHill = "The Queen Under the Hill";

    // Act IV. The names are the encounter names, because that is what the reward wiring matches on.
    public const string PharaohOfTheSealedName = "The Pharaoh of the Sealed Name";
    public const string WeigherOfTheUnspokenHeart = "The Weigher of the Unspoken Heart";
    public const string ArchitectOfTheImpossiblePyramid = "The Architect of the Impossible Pyramid";
    public const string LadyOfTheBlackGranaries = "The Lady of the Black Granaries";
    public const string FirstScribeOfTheHouseOfLife = "The First Scribe of the House of Life";
    public const string MotherOfNatronAndResin = "The Mother of Natron and Resin";
    public const string VizierOfTheKingsMouth = "The Vizier of the King's Mouth";
    public const string QueenOfTheFloodReckoning = "The Queen of the Flood Reckoning";

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

        // ── Act III ───────────────────────────────────────────────────────────────────────────────────────
        Boss("boundary_tally", "Boundary Tally", OmbudsmanOfRootAndRoad,
            "The road and the root take it in turns: on a road turn your first real card costs 1 less, on a root turn you open with 10 Block.",
            combatRule: BossRelicRules.BoundaryTally),
        Boss("counter_petition_twine", "Counter-Petition Twine", OmbudsmanOfRootAndRoad,
            "Once a turn you may re-argue a card: discard one, draw one, and gain 1 Energy.",
            combatRule: BossRelicRules.CounterPetitionTwine),
        Boss("signed_settlement", "Signed Settlement", OmbudsmanOfRootAndRoad,
            "Come through an enemy turn untouched for 1 Energy and a card; come through it hurt for 8 Block.",
            combatRule: BossRelicRules.SignedSettlement),

        Boss("countersealed_ring_of_passage", "Countersealed Ring of Passage", NotaryOfOldGrowth,
            "Your first real card each turn sets a price; the next card at that price is refunded. A turn with no match ends in 5 Block.",
            combatRule: BossRelicRules.CountersealedRingOfPassage),
        Boss("countersealed_ring_of_restraint", "Countersealed Ring of Restraint", NotaryOfOldGrowth,
            "Play three real cards and the fourth is refunded and draws a card. A turn that never reaches three keeps the ring armed.",
            combatRule: BossRelicRules.CountersealedRingOfRestraint),
        Boss("countersealed_ring_of_keeping", "Countersealed Ring of Keeping", NotaryOfOldGrowth,
            "Empty your hand of real cards and the next turn opens with 1 Energy and two extra cards; keep something back and one card stays, cheaper.",
            combatRule: BossRelicRules.CountersealedRingOfKeeping),

        Boss("honey_spoon", "Honey Spoon", GrandmotherClause,
            "Once a turn you may take 2 Energy. End that turn with at least 1 Energy, or it costs you 6 HP.",
            combatRule: BossRelicRules.HoneySpoon),
        Boss("better_chair_cushion", "Better Chair Cushion", GrandmotherClause,
            "Once a turn you may take 14 Block. End that turn holding a real card, or it costs you 6 HP.",
            combatRule: BossRelicRules.BetterChairCushion),
        Boss("last_slice_tin", "Last-Slice Tin", GrandmotherClause,
            "Once a turn you may draw 2. Play no more than four real cards that turn, or it costs you 6 HP.",
            combatRule: BossRelicRules.LastSliceTin),

        Boss("surveyed_milestone", "Surveyed Milestone", AnsweringHill,
            "The largest thing on the field is a landmark: first bringing it past three quarters, half and a quarter grants 1 Energy and a card.",
            combatRule: BossRelicRules.SurveyedMilestone),
        Boss("survey_cairn", "Survey Cairn", AnsweringHill,
            "End a turn with 12 Block or more and the cairn buries twelve of it; the next turn opens with 1 Energy and a card.",
            combatRule: BossRelicRules.SurveyCairn),
        Boss("loadstone_cairn", "Loadstone Cairn", AnsweringHill,
            "What the enemies take out of you is weight in the stone: next turn it is Block, and it is on your first Deed. Up to 12.",
            combatRule: BossRelicRules.LoadstoneCairn),

        Boss("royal_grace_cup", "Royal Grace Cup", QueenUnderTheHill,
            "Once a turn the cup offers an Energy, a card or 10 Block. Take it and every enemy guards for 6.",
            combatRule: BossRelicRules.RoyalGraceCup),
        Boss("hollow_court_token", "Hollow-Court Token", QueenUnderTheHill,
            "Spending your purse to the bottom is remembered, up to three times; open a turn owed all three and it pays 1 Energy, two cards and 8 Block.",
            combatRule: BossRelicRules.HollowCourtToken),
        Boss("silver_name_tally", "Silver Name-Tally", QueenUnderTheHill,
            "Once a combat: one enemy's guard is gone, you gain 10 Block against what it was about to do, and your next card that turn is refunded.",
            combatRule: BossRelicRules.SilverNameTally),

        // ── Act IV ────────────────────────────────────────────────────────────────────────────────────────
        Boss("crown_of_the_three_names", "Crown of the Three Names", PharaohOfTheSealedName,
            "Every turn is worth one more Energy.",
            combatRule: BossRelicRules.CrownOfTheThreeNames),
        Boss("edict_of_the_open_audience", "Edict of the Open Audience", PharaohOfTheSealedName,
            "Once a combat, every card in your hand is heard for nothing.",
            combatRule: BossRelicRules.EdictOfTheOpenAudience),
        // The one relic in the pool that takes itself off. The fight writes down that the cartouche was read;
        // the run reads that when the fight resolves, and destroys the relic — which is the only door between
        // the two layers (see RunEventValues.CombatCounter).
        Boss("eternal_cartouche", "Eternal Cartouche", PharaohOfTheSealedName,
            "The first blow that would end you does not: you stand again, clean of every affliction, and the "
            + "cartouche is spent for good.",
            runPrograms:
            [
                RunPrograms.When<CombatResolvedRunEvent>(
                    RunExpr.GreaterThan(
                        RunEventValues.CombatCounter(BossRelicRules.CartoucheSpentCounter), RunExpr.Const(0)),
                    [new RemoveRelicRunEffect(new RelicId("eternal_cartouche"))]),
            ],
            combatRule: BossRelicRules.EternalCartouche),

        Boss("feather_of_perfect_measure", "Feather of Perfect Measure", WeigherOfTheUnspokenHeart,
            "Whichever kind you lead with costs 1 less, and the first answer in the other kind draws 1 and "
            + "gains 8 Block.",
            combatRule: BossRelicRules.FeatherOfPerfectMeasure),
        Boss("acquittal_scarab", "Acquittal Scarab", WeigherOfTheUnspokenHeart,
            "Every third turn the court sits: enemy guards fall and you strike 30% harder. You read one "
            + "judgment further ahead than anyone else.",
            combatRule: BossRelicRules.AcquittalScarab),
        Boss("balance_of_the_two_pans", "Balance of the Two Pans", WeigherOfTheUnspokenHeart,
            "End a turn with as many Deeds as Workings, one of each at least, to heal 2 and open the next "
            + "turn with an Energy. An unbalanced turn ends in 12 Block.",
            combatRule: BossRelicRules.BalanceOfTheTwoPans),

        Boss("impossible_capstone", "Impossible Capstone", ArchitectOfTheImpossiblePyramid,
            "Half of whatever Block you still hold at the end of a turn is still there at the start of the next.",
            combatRule: BossRelicRules.ImpossibleCapstone),
        Boss("pyramidion_of_repetition", "Pyramidion of Repetition", ArchitectOfTheImpossiblePyramid,
            "Every sixth real card you play in a fight happens twice, and the second time is free.",
            combatRule: BossRelicRules.PyramidionOfRepetition),
        Boss("crooked_plumb_line", "Crooked Plumb Line", ArchitectOfTheImpossiblePyramid,
            "The first time in a turn you follow a card with one of another kind, up to 2 Energy comes back. "
            + "A turn that never bends ends in 10 Block.",
            combatRule: BossRelicRules.CrookedPlumbLine),

        Boss("black_granary_key", "Black Granary Key", LadyOfTheBlackGranaries,
            "Energy you do not spend is stored, and it comes back the moment you run out.",
            combatRule: BossRelicRules.BlackGranaryKey),
        // The one relic here with nothing to say inside a fight: what it does happens after one.
        Boss("granary_reserve_seal", "Granary Reserve Seal", LadyOfTheBlackGranaries,
            "Every fight you win puts 15 HP back.",
            runPrograms: [AfterEveryVictory(Heal(15))]),
        Boss("ration_seal", "Ration Seal", LadyOfTheBlackGranaries,
            "The fourth real card of a turn is free and draws you another. A turn that never gets there ends "
            + "in 10 Block.",
            combatRule: BossRelicRules.RationSeal),

        Boss("palimpsest_reed", "Palimpsest Reed", FirstScribeOfTheHouseOfLife,
            "The first real card you play each turn is copied down; the copy is in your hand next turn, and "
            + "it is free.",
            combatRule: BossRelicRules.PalimpsestReed),
        Boss("erasure_tablet", "Erasure Tablet", FirstScribeOfTheHouseOfLife,
            "Once a combat you may erase what the enemies were about to do; they guard for 20 instead.",
            combatRule: BossRelicRules.ErasureTablet),
        Boss("correction_reed", "Correction Reed", FirstScribeOfTheHouseOfLife,
            "Once a turn you may correct the record: a card away, a card back out of the discard pile, and "
            + "the one you take back comes cheaper.",
            combatRule: BossRelicRules.CorrectionReed),

        Boss("canopic_cabinet", "Canopic Cabinet", MotherOfNatronAndResin,
            "The fight opens with 12 Block, and the first two afflictions laid on you are refused outright.",
            combatRule: BossRelicRules.CanopicCabinet),
        Boss("resin_shroud", "Resin Shroud", MotherOfNatronAndResin,
            "Once a fight, coming round below half your health strips every affliction and wraps you in 25 "
            + "Block.",
            combatRule: BossRelicRules.ResinShroud),
        Boss("basin_of_black_natron", "Basin of Black Natron", MotherOfNatronAndResin,
            "Each turn the basin washes a stack off one of your afflictions — or gives you 12 Block if you "
            + "have none.",
            combatRule: BossRelicRules.BasinOfBlackNatron),

        Boss("triune_office_seal", "Triune Office Seal", VizierOfTheKingsMouth,
            "All three offices answer to you: an extra card each turn, 8 more on your first Deed, and 8 "
            + "Block on your first Working.",
            combatRule: BossRelicRules.TriuneOfficeSeal),
        Boss("staff_of_the_kings_mouth", "Staff of the King's Mouth", VizierOfTheKingsMouth,
            "The first real card of each turn is paid for out of the King's own purse, up to 2 Energy.",
            combatRule: BossRelicRules.StaffOfTheKingsMouth),
        Boss("vacant_throne_decree", "Vacant-Throne Decree", VizierOfTheKingsMouth,
            "Once a combat, the empty throne pays: 3 Energy, three cards and 20 Block.",
            combatRule: BossRelicRules.VacantThroneDecree),

        Boss("sluice_gate_of_the_two_lands", "Sluice Gate of the Two Lands", QueenOfTheFloodReckoning,
            "Once a turn you may work the gate: 12 Block into an Energy, or an Energy into 12 Block.",
            combatRule: BossRelicRules.SluiceGateOfTheTwoLands),
        Boss("flood_reckoning_crown", "Flood-Reckoning Crown", QueenOfTheFloodReckoning,
            "How you ended decides how you open: dry, and the crown pays an Energy and a card; with "
            + "something left, an Energy and 15 Block. The first turn opens with 10 Block.",
            combatRule: BossRelicRules.FloodReckoningCrown),
        Boss("black_flood_vessel", "Black Flood Vessel", QueenOfTheFloodReckoning,
            "Once a combat you may pour the whole hand away and draw seven fresh ones, with 2 Energy to "
            + "spend on them.",
            combatRule: BossRelicRules.BlackFloodVessel),
    ];

    // A boss's own three, in the order the design lists them.
    public static IReadOnlyList<BnbRelic> For(string boss) =>
        All().Where(relic => relic.Source == boss).ToList();
}
