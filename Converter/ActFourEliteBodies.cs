using System.Text.Json;
using RogueDeck.Core.Combat;
using RogueDeck.Run;
using RogueDeck.Sandbox.Composition;
using RogueDeck.Scenario.Authoring;

namespace BnbContent.Converter;

// Act IV's eleven elites since the rework of 2026-11-04 (user): each its own mechanic, one clear sentence, every
// count a stack you can read. The elite penalty is Entombed (at 5 you lose a turn).
//
//   the Corvée Register       two Haulers drag an obelisk at you; at 4 it falls for 40.
//   Colossus of Memnon        every third round dawn: up to 2 of your Burdened become Entombed.
//   Coils of Apep             a coil a round, 1 card fewer drawn per coil; 20 damage in a turn loosens one.
//   the Sphinx                Deeds or Workings, each round; three answers and she is solved.
//   Ammit                     she devours the most expensive card left in your hand, unless fed a Feather.
//   The Lioness               every HP you lose is Rage; block one of her attacks fully and it is gone.
//   Priest of the Four Sons   all four sons' jars, and every third round a broken one refilled.
//   Brood Mother              three brood jars; every second round each hatches a Scarab.
//   The Embalmer              a card a round wrapped: it costs 0, playing it is 1 Entombed.
//   The Restless King         rises twice, each time with a random jar's power.
//   The Three Robbers         a death makes the others stronger, and one takes up the dead one's tool.
public static partial class ActFour
{
    // ── ids ───────────────────────────────────────────────────────────────────────────────────────────────

    public const string RopeMasterEnemyId = "rope_master_of_the_corvee";
    public const string StoneHaulerSummonEnemyId = "stone_hauler_of_the_corvee";
    public const string ColossusEnemyId = "colossus_of_the_endless_procession";
    public const string DecanKeeperEnemyId = "keeper_of_the_thirty_six_decans";
    public const string SphinxEnemyId = "sphinx_of_the_processional_measure";
    public const string LionessEnemyId = "lioness_of_sekhmet";
    public const string SurveyorEliteEnemyId = "surveyor_of_the_errant_cord";
    public const string ScarabHostEnemyId = "scarab_host_of_the_sealed_granary";
    public const string LinenOverseerEnemyId = "mummified_overseer_of_the_linen_house";
    public const string PryBarVeteranEnemyId = "pry_bar_veteran";
    public const string LampThiefEnemyId = "lamp_thief";
    public const string CurseBearerEnemyId = "curse_bearer";

    public const string LionessEncounterId = "labyrinth_elite_lioness_of_sekhmet";

    // The Taskmaster
    public const string ObeliskRuleId = "raising_the_obelisk";
    public const string ObeliskId = "obelisk";
    public const string WhippedBackId = "whipped_back";
    public const string AtTheRopeId = "at_the_rope";
    public const int ObeliskFalls = 4;
    public const int ObeliskDamage = 40;
    public const int HaulerHealth = 40;
    public const int HaulersAtTheRope = 2;
    public const int HaulerReturnRounds = 2;
    public const int WhipBlock = 10;

    // Colossus of Memnon
    public const string DawnSongRuleId = "the_dawn_song";
    public const string UntilDawnId = "until_dawn";
    public const string DawnBreaksId = "dawn_breaks";
    public const int DawnEvery = 3;
    public const int DawnTakesAtMost = 2;

    // Coils of Apep
    public const string CoilsRuleId = "the_coils";
    public const string CoilId = "coil";
    public const string StrainId = "apep_strain";
    public const string LoosenedId = "apep_loosened";
    public const int MaxCoils = 3;
    public const int LoosenAt = 20;

    // The Sphinx
    public const string RiddleRuleId = "the_riddle";
    public const string SphinxWantsDeedsId = "sphinx_wants_deeds";
    public const string SphinxWantsWorkingsId = "sphinx_wants_workings";
    public const string SignId = "sphinx_sign";
    public const string SolvedId = "sphinx_solved";
    public const int SignsToSolve = 3;
    public const int SolvedRounds = 2;

    // Ammit
    public const string DevouredId = "devoured";
    public const string DevouredMark = "devoured_by_ammit";
    public const int DevourStrength = 2;

    // The Lioness
    public const string LionessRuleId = "sekhmets_rage";
    public const string RageId = "rage";
    public const int RageDamage = 3;

    // Priest of the Four Sons
    public const string FourSonsRuleId = "the_four_sons";
    public const int PriestJarHealth = 24;
    public const int RefillBlock = 12;
    // One jar of each son: Imsety's Liver, Hapi's Lung, Duamutef's Stomach, Qebehsenuef's Ka.
    public static readonly IReadOnlyList<string> FourSonsJars = ["liver", "lung", "stomach", "ka"];

    // Brood Mother
    public const string BroodRuleId = "the_brood";
    public const string BroodJarId = "brood_jar";
    public const string BroodScarabId = "brood_scarab";
    public const string UntilHatchId = "until_hatch";
    public const string BroodJarEnemyId = "brood_jar";
    public const string BroodScarabEnemyId = "brood_scarab";
    public const int BroodJars = 3;
    public const int BroodJarHealth = 20;
    public const int ScarabHealth = 10;
    public const int ScarabBites = 3;
    public const int ScarabBite = 3;
    public const int HatchEvery = 2;

    // The Embalmer
    public const string LinenHouseId = "the_linen_house";
    public const string LinenWrappedMark = "linen_wrapped";
    public const int WrapsPerTurn = 1;

    // The Restless King
    public const string KingRisenId = "king_risen";
    public const string KingRisenLastId = "king_risen_last";
    public const string KingPendingId = "king_power_pending";
    // The King takes any jar's power except the ones that mean nothing on him: the Ka (a third rise) and the Gut
    // (he does not weigh).
    public static readonly IReadOnlyList<string> KingPowers = ["liver", "blood", "lung", "breath", "stomach", "hunger"];

    // The Three Robbers
    public const string TombbreakerId = "tombbreaker";
    public const int RobberRage = 3;
    public sealed record Tool(string Key, string Name, string OwnerId, string StolenIntent);
    public static readonly IReadOnlyList<Tool> Tools =
    [
        new("pry_bar", "Bronze Pry-Bar", PryBarVeteranEnemyId, "stolen_pry"),
        new("knife", "Flint Knife", LampThiefEnemyId, "stolen_knife"),
        new("idol", "Cursed Idol", CurseBearerEnemyId, "stolen_idol"),
    ];
    public static string OwnsToolId(string key) => $"owns_{key}";
    public static string HoldsToolId(string key) => $"holds_{key}";
    public static string ToolReadyId(string key) => $"ready_{key}";
    public const string UsedToolId = "used_a_tool";

    private static CounterId HaulersSet => new("haulers_set");
    private static CounterId JarsLaid => new("elite_jars_laid");
    private static CounterId KingRoll => new("king_roll");
    private static CounterId RefillRoll => new("refill_done");

    // ── the registry ──────────────────────────────────────────────────────────────────────────────────────

    public static readonly IReadOnlySet<string> EliteIdentities = new HashSet<string>(StringComparer.Ordinal)
    {
        RopeMasterEnemyId, StoneHaulerSummonEnemyId, ColossusEnemyId, DecanKeeperEnemyId, SphinxEnemyId,
        AmmitEnemyId, LionessEnemyId, SurveyorEliteEnemyId, ScarabHostEnemyId, LinenOverseerEnemyId, CurseEnemyId,
        PryBarVeteranEnemyId, LampThiefEnemyId, CurseBearerEnemyId,
    };

    public static IReadOnlyList<StatusData> EliteStatuses() =>
    [
        .. TaskmasterStatuses(), .. MemnonStatuses(), .. ApepStatuses(), .. SphinxStatuses(), .. AmmitStatuses(),
        .. LionessStatuses(), .. FourSonsStatuses(), .. BroodStatuses(), TheLinenHouse(), .. KingStatuses(),
        .. RobberStatuses(),
    ];

    public static IReadOnlyList<CardData> EliteCards() => [];

    public static EffectProgram<EnemyActionContext>? EliteIntent(string enemyId, string intentId) =>
        $"{enemyId}.{intentId}" switch
        {
            "rope_master_of_the_corvee.crack_the_whip" => CrackTheWhip(),
            "colossus_of_the_endless_procession.dawn_song" => DawnSong(8),
            "keeper_of_the_thirty_six_decans.constrict" => Constrict(15),
            "keeper_of_the_thirty_six_decans.shed_skin" => ShedSkin(24),
            "lioness_of_sekhmet.prowl" => Prowl(20),
            "surveyor_of_the_errant_cord.refill" => Refill(),
            _ when Tools.FirstOrDefault(t => t.StolenIntent == intentId) is { } tool && Robbers.Contains(enemyId) =>
                StolenTool(tool),
            _ when intentId == "rise" && EliteIdentities.Contains(enemyId) => RiseAction(),
            _ => null,
        };

    // The encounter-wide downing rules each elite brings (what falls with what). One body per encounter brings
    // them, so a trio does not hear its own deaths three times.
    public static IReadOnlyList<EncounterTriggerData> EliteTriggers(string enemyId) => enemyId switch
    {
        RopeMasterEnemyId => Downed(TaskmasterDowned()),
        DecanKeeperEnemyId => Downed(ApepDowned()),
        SurveyorEliteEnemyId => LabyrinthTriggers(),
        ScarabHostEnemyId => Downed(BroodDowned()),
        PryBarVeteranEnemyId => Downed(RobberDowned()),
        _ => [],
    };

    private static IReadOnlyList<EncounterTriggerData> Downed(IEffectNode<CombatantDownedTriggeredEffectContext> program) =>
    [
        new EncounterTriggerData("Downed", JsonSerializer.SerializeToElement(
            new EffectProgram<CombatantDownedTriggeredEffectContext>(program),
            CombatJson.CreateOptions<CombatantDownedTriggeredEffectContext>())),
    ];

    private static ICombatantTargetSelector Living(string status) =>
        CombatantTargetSelectors.WithStatus(CombatantTargetSelectors.AllAliveCombatants, new StatusDefinitionId(status));

    private static ICombatExpression<TContext, int> CountOf<TContext>(string status) where TContext : class =>
        new CountTargetsExpression<TContext>(Living(status));

    private static ICombatExpression<TContext, bool> FirstBell<TContext>() where TContext : class =>
        Eq<TContext>(new RoundNumberExpression<TContext>(), N<TContext>(1));

    private static IEffectNode<TContext> Mark<TContext>(ICombatantTargetSelector who, string status, int stacks = 1)
        where TContext : class =>
        new ApplyStatusNode<TContext>(who, new StatusDefinitionId(status), N<TContext>(stacks));

    private static IEffectNode<TContext> Unmark<TContext>(ICombatantTargetSelector who, string status)
        where TContext : class =>
        new RemoveStatusNode<TContext>(who, new StatusDefinitionId(status));

    // A countdown on a body: one fewer, and at the last one `then` and the count gone.
    private static IEffectNode<TContext> CountDown<TContext>(
        ICombatantTargetSelector who, string status, IEffectNode<TContext> then) where TContext : class =>
        new ConditionalEffectNode<TContext>(
            new ComparisonExpression<TContext>(StacksOn<TContext>(who, status), ComparisonOperator.LessOrEqual, N<TContext>(1)),
            new CausalSequenceEffectNode<TContext>([Unmark<TContext>(who, status), then]),
            @else: new ModifyStatusStacksNode<TContext>(who, new StatusDefinitionId(status), N<TContext>(-1)));

    private static IEffectNode<TContext> KillAll<TContext>(string status) where TContext : class =>
        new ForEachTargetEffectNode<TContext>(Living(status),
            new SetHealthNode<TContext>(CombatantTargetSelectors.IterationTarget, N<TContext>(0)));

    private static ICombatExpression<TContext, int> PlayedTagged<TContext>(string tag) where TContext : class =>
        new CardsPlayedThisTurnWithTagExpression<TContext>(Applicant, new TagId(tag));

    // ── The Taskmaster ────────────────────────────────────────────────────────────────────────────────────

    private static IEffectNode<TContext> SummonHauler<TContext>() where TContext : class =>
        new SummonCombatantNode<TContext>(
            StandardCombatIds.EnemyTeam, N<TContext>(HaulerHealth),
            new CombatantDefinitionId(StoneHaulerSummonEnemyId), "Ushabti Hauler",
            startingStatuses: [new StatusGrant(new StatusDefinitionId(AtTheRopeId), 1)]);

    private static IEnumerable<StatusData> TaskmasterStatuses()
    {
        var master = Bearer(ObeliskRuleId);
        var self = CombatantTargetSelectors.Source;
        var haulers = CountOf<TurnStartedTriggeredEffectContext>(AtTheRopeId);

        // The first bell: the gang walks on.
        var gang = new ConditionalEffectNode<TurnStartedTriggeredEffectContext>(
            AllOf(IsPlayersTurn<TurnStartedTriggeredEffectContext>(), Standing<TurnStartedTriggeredEffectContext>(master),
                Eq(new CombatantCounterExpression<TurnStartedTriggeredEffectContext>(master, HaulersSet), N<TurnStartedTriggeredEffectContext>(0))),
            new CausalSequenceEffectNode<TurnStartedTriggeredEffectContext>(
            [
                new SetCombatantCounterNode<TurnStartedTriggeredEffectContext>(master, HaulersSet, N<TurnStartedTriggeredEffectContext>(1), relative: false),
                .. Enumerable.Range(0, HaulersAtTheRope).Select(_ => SummonHauler<TurnStartedTriggeredEffectContext>()),
            ]));

        // Its own turn: the fallen are whipped back, then the obelisk moves while a Hauler stands.
        var returnAll = new RepeatEffectNode<TurnStartedTriggeredEffectContext>(
            new SubtractExpression<TurnStartedTriggeredEffectContext>(N<TurnStartedTriggeredEffectContext>(HaulersAtTheRope), haulers),
            SummonHauler<TurnStartedTriggeredEffectContext>());
        var drag = new ConditionalEffectNode<TurnStartedTriggeredEffectContext>(Above(haulers, 0),
            new CausalSequenceEffectNode<TurnStartedTriggeredEffectContext>(
            [
                Mark<TurnStartedTriggeredEffectContext>(self, ObeliskId),
                new ConditionalEffectNode<TurnStartedTriggeredEffectContext>(
                    new ComparisonExpression<TurnStartedTriggeredEffectContext>(
                        StacksOn<TurnStartedTriggeredEffectContext>(self, ObeliskId), ComparisonOperator.GreaterOrEqual,
                        N<TurnStartedTriggeredEffectContext>(ObeliskFalls)),
                    new CausalSequenceEffectNode<TurnStartedTriggeredEffectContext>(
                    [
                        new DealDamageNode<TurnStartedTriggeredEffectContext>(Applicant, N<TurnStartedTriggeredEffectContext>(ObeliskDamage)),
                        Unmark<TurnStartedTriggeredEffectContext>(self, ObeliskId),
                    ])),
            ]));
        var ownTurn = new CausalSequenceEffectNode<TurnStartedTriggeredEffectContext>(
        [
            new ConditionalEffectNode<TurnStartedTriggeredEffectContext>(Wears<TurnStartedTriggeredEffectContext>(self, WhippedBackId),
                CountDown<TurnStartedTriggeredEffectContext>(self, WhippedBackId, returnAll)),
            drag,
        ]);

        yield return Face(ObeliskRuleId, "Raising the Obelisk",
            "While a Hauler stands the obelisk moves 1 each round; at 4 it falls on you for 40. Haulers return 2 rounds after falling.",
            triggers: [On("TurnStarted", gang), On("TurnStarted", ownTurn, StatusTriggerScope.Bearer)]);
        yield return Face(ObeliskId, "Obelisk", "How far the obelisk has come. At 4 it falls on you for 40.", stacks: true);
        yield return Face(WhippedBackId, "Whipped Back", "Rounds until the fallen Haulers are whipped back in.", stacks: true);
        yield return Face(AtTheRopeId, "At the Rope",
            "A Hauler dragging the obelisk. It does not attack, and it falls with the Overseer.");
    }

    // A Hauler falls: the whip comes in 2 rounds. The Taskmaster falls: the rope goes slack.
    private static IEffectNode<CombatantDownedTriggeredEffectContext> TaskmasterDowned()
    {
        var downed = CombatantTargetSelectors.SourceIncludingDowned;
        var master = Bearer(ObeliskRuleId);
        return new CausalSequenceEffectNode<CombatantDownedTriggeredEffectContext>(
        [
            new ConditionalEffectNode<CombatantDownedTriggeredEffectContext>(
                AllOf(Wears<CombatantDownedTriggeredEffectContext>(downed, AtTheRopeId),
                    Standing<CombatantDownedTriggeredEffectContext>(master),
                    new NotExpression<CombatantDownedTriggeredEffectContext>(Wears<CombatantDownedTriggeredEffectContext>(master, WhippedBackId))),
                Mark<CombatantDownedTriggeredEffectContext>(master, WhippedBackId, HaulerReturnRounds)),
            new ConditionalEffectNode<CombatantDownedTriggeredEffectContext>(
                Wears<CombatantDownedTriggeredEffectContext>(downed, ObeliskRuleId),
                KillAll<CombatantDownedTriggeredEffectContext>(AtTheRopeId)),
        ]);
    }

    private static EffectProgram<EnemyActionContext> CrackTheWhip() =>
        new(new ForEachTargetEffectNode<EnemyActionContext>(Living(AtTheRopeId),
            new GainBlockNode<EnemyActionContext>(CombatantTargetSelectors.IterationTarget, Const(WhipBlock))));

    // ── Colossus of Memnon ────────────────────────────────────────────────────────────────────────────────

    private static IEnumerable<StatusData> MemnonStatuses()
    {
        var self = CombatantTargetSelectors.Source;
        var dusk = new ConditionalEffectNode<TurnEndedTriggeredEffectContext>(
            Wears<TurnEndedTriggeredEffectContext>(self, UntilDawnId),
            CountDown<TurnEndedTriggeredEffectContext>(self, UntilDawnId, Mark<TurnEndedTriggeredEffectContext>(self, DawnBreaksId)));
        yield return Face(DawnSongRuleId, "The Dawn Song",
            "Every third round is dawn: it sings, and up to 2 of your Burdened become Entombed.",
            triggers: [On("TurnEnded", dusk, StatusTriggerScope.Bearer)]);
        yield return Face(UntilDawnId, "Until Dawn", "Rounds until it sings at dawn.", stacks: true);
        yield return Face(DawnBreaksId, "Dawn", "This round it sings: up to 2 of your Burdened become Entombed.");
    }

    // The song: your Burdened turned to Entombed, a blow, and the night begins again.
    private static EffectProgram<EnemyActionContext> DawnSong(int damage)
    {
        var self = CombatantTargetSelectors.Source;
        // At most 2 of your Burdened become Entombed (user, 2026-11-04); the rest stays Burdened.
        var burden = new MinExpression<EnemyActionContext>(StacksOn<EnemyActionContext>(Applicant, BurdenedId), Const(DawnTakesAtMost));
        return new(new CausalSequenceEffectNode<EnemyActionContext>(
        [
            new ConditionalEffectNode<EnemyActionContext>(Above(burden, 0),
                new CausalSequenceEffectNode<EnemyActionContext>(
                [
                    new ApplyStatusNode<EnemyActionContext>(Applicant, new StatusDefinitionId(EntombedId), burden, sourceSelector: self),
                    new ModifyStatusStacksNode<EnemyActionContext>(Applicant, new StatusDefinitionId(BurdenedId),
                        new NegateExpression<EnemyActionContext>(burden)),
                ])),
            new DealDamageNode<EnemyActionContext>(Applicant, Const(damage)),
            Unmark<EnemyActionContext>(self, DawnBreaksId),
            Mark<EnemyActionContext>(self, UntilDawnId, DawnEvery),
        ]));
    }

    // ── Coils of Apep ─────────────────────────────────────────────────────────────────────────────────────

    private static IEffectNode<TContext> Tighten<TContext>(ICombatantTargetSelector apep) where TContext : class =>
        new ConditionalEffectNode<TContext>(
            new ComparisonExpression<TContext>(StacksOn<TContext>(Applicant, CoilId), ComparisonOperator.Less, N<TContext>(MaxCoils)),
            new ApplyStatusNode<TContext>(Applicant, new StatusDefinitionId(CoilId), N<TContext>(1), sourceSelector: apep));

    private static IEnumerable<StatusData> ApepStatuses()
    {
        var apep = Bearer(CoilsRuleId);
        var self = CombatantTargetSelectors.Source;

        var round = new ConditionalEffectNode<TurnEndedTriggeredEffectContext>(Standing<TurnEndedTriggeredEffectContext>(self),
            Tighten<TurnEndedTriggeredEffectContext>(self));

        // Your blows this turn strain the coils; 20 of them in one turn and one coil gives, once a turn.
        var struck = CombatantTargetSelectors.EventTarget;
        var strain = new ConditionalEffectNode<DamageReceivedTriggeredEffectContext>(
            Above(new EventAmountExpression<DamageReceivedTriggeredEffectContext>(), 0),
            new CausalSequenceEffectNode<DamageReceivedTriggeredEffectContext>(
            [
                new ApplyStatusNode<DamageReceivedTriggeredEffectContext>(struck, new StatusDefinitionId(StrainId),
                    new EventAmountExpression<DamageReceivedTriggeredEffectContext>()),
                new ConditionalEffectNode<DamageReceivedTriggeredEffectContext>(
                    AllOf(
                        new ComparisonExpression<DamageReceivedTriggeredEffectContext>(
                            StacksOn<DamageReceivedTriggeredEffectContext>(struck, StrainId), ComparisonOperator.GreaterOrEqual,
                            N<DamageReceivedTriggeredEffectContext>(LoosenAt)),
                        new NotExpression<DamageReceivedTriggeredEffectContext>(Wears<DamageReceivedTriggeredEffectContext>(struck, LoosenedId))),
                    new CausalSequenceEffectNode<DamageReceivedTriggeredEffectContext>(
                    [
                        Mark<DamageReceivedTriggeredEffectContext>(struck, LoosenedId),
                        new ConditionalEffectNode<DamageReceivedTriggeredEffectContext>(
                            Above(StacksOn<DamageReceivedTriggeredEffectContext>(Applicant, CoilId), 0),
                            CountDown<DamageReceivedTriggeredEffectContext>(Applicant, CoilId,
                                new NoOpEffectNode<DamageReceivedTriggeredEffectContext>())),
                    ])),
            ]));
        var morning = new ConditionalEffectNode<TurnStartedTriggeredEffectContext>(IsPlayersTurn<TurnStartedTriggeredEffectContext>(),
            new CausalSequenceEffectNode<TurnStartedTriggeredEffectContext>(
            [
                Unmark<TurnStartedTriggeredEffectContext>(apep, StrainId),
                Unmark<TurnStartedTriggeredEffectContext>(apep, LoosenedId),
            ]));

        yield return Face(CoilsRuleId, "The Coils",
            "Each round it coils tighter: 1 card fewer drawn per Coil (at most 3). Deal it 20 in one turn to loosen one.",
            triggers:
            [
                On("TurnEnded", round, StatusTriggerScope.Bearer),
                On("DamageTaken", strain, StatusTriggerScope.Bearer),
                On("TurnStarted", morning),
            ]);
        yield return new StatusData
        {
            Id = CoilId,
            NameKey = "Coil",
            DescriptionKey = "Apep is wrapped around you: draw 1 card fewer per Coil.",
            Polarity = StatusPolarity.Debuff,
            StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
            UsesStacks = true,
            Tags = [],
            PassiveModifiers =
            [
                new PassiveModifierData(PassiveModifierPipeline.TurnStartDraw, PassiveModifierOperation.AddPerStack, -1,
                    RestrictDamageKind: null),
            ],
            Triggers = [],
        };
        yield return Face(StrainId, "Strain", "Damage you dealt it this turn. At 20 one Coil loosens.", stacks: true);
        yield return Face(LoosenedId, "Loosened", "A coil gave this turn; the next gives next turn.");
    }

    // Apep falls: the coils fall away.
    private static IEffectNode<CombatantDownedTriggeredEffectContext> ApepDowned() =>
        new ConditionalEffectNode<CombatantDownedTriggeredEffectContext>(
            Wears<CombatantDownedTriggeredEffectContext>(CombatantTargetSelectors.SourceIncludingDowned, CoilsRuleId),
            Unmark<CombatantDownedTriggeredEffectContext>(Applicant, CoilId));

    private static EffectProgram<EnemyActionContext> Constrict(int damage) =>
        new(new CausalSequenceEffectNode<EnemyActionContext>(
        [
            new DealDamageNode<EnemyActionContext>(Applicant, Const(damage)),
            Tighten<EnemyActionContext>(CombatantTargetSelectors.Source),
        ]));

    private static EffectProgram<EnemyActionContext> ShedSkin(int block) =>
        new(new CausalSequenceEffectNode<EnemyActionContext>(
        [
            new GainBlockNode<EnemyActionContext>(CombatantTargetSelectors.Source, Const(block)),
            new RemoveStatusesByPolarityNode<EnemyActionContext>(CombatantTargetSelectors.Source, StatusPolarity.Debuff),
        ]));

    // ── The Sphinx ────────────────────────────────────────────────────────────────────────────────────────

    private static IEnumerable<StatusData> SphinxStatuses()
    {
        var sphinx = Bearer(RiddleRuleId);
        var self = CombatantTargetSelectors.Source;
        var solved = Wears<TurnStartedTriggeredEffectContext>(sphinx, SolvedId);

        // Your turn begins: she names what she wants to hear (not while she is solved).
        var ask = new ConditionalEffectNode<TurnStartedTriggeredEffectContext>(
            AllOf(IsPlayersTurn<TurnStartedTriggeredEffectContext>(), Standing<TurnStartedTriggeredEffectContext>(sphinx)),
            new CausalSequenceEffectNode<TurnStartedTriggeredEffectContext>(
            [
                Unmark<TurnStartedTriggeredEffectContext>(sphinx, SphinxWantsDeedsId),
                Unmark<TurnStartedTriggeredEffectContext>(sphinx, SphinxWantsWorkingsId),
                new ConditionalEffectNode<TurnStartedTriggeredEffectContext>(
                    new NotExpression<TurnStartedTriggeredEffectContext>(solved),
                    new ConditionalEffectNode<TurnStartedTriggeredEffectContext>(
                        Eq(new RandomBelowExpression<TurnStartedTriggeredEffectContext>(2), N<TurnStartedTriggeredEffectContext>(0)),
                        Mark<TurnStartedTriggeredEffectContext>(sphinx, SphinxWantsDeedsId),
                        @else: Mark<TurnStartedTriggeredEffectContext>(sphinx, SphinxWantsWorkingsId))),
            ]));

        // Your turn ends: the answer.
        var deeds = PlayedTagged<TurnEndedTriggeredEffectContext>(Cards.CardAuthoring.DeedTag);
        var workings = PlayedTagged<TurnEndedTriggeredEffectContext>(Cards.CardAuthoring.WorkingTag);
        var asked = new OrExpression<TurnEndedTriggeredEffectContext>(
            Wears<TurnEndedTriggeredEffectContext>(sphinx, SphinxWantsDeedsId),
            Wears<TurnEndedTriggeredEffectContext>(sphinx, SphinxWantsWorkingsId));
        var right = new OrExpression<TurnEndedTriggeredEffectContext>(
            AllOf(Wears<TurnEndedTriggeredEffectContext>(sphinx, SphinxWantsDeedsId),
                new ComparisonExpression<TurnEndedTriggeredEffectContext>(deeds, ComparisonOperator.Greater, workings)),
            AllOf(Wears<TurnEndedTriggeredEffectContext>(sphinx, SphinxWantsWorkingsId),
                new ComparisonExpression<TurnEndedTriggeredEffectContext>(workings, ComparisonOperator.Greater, deeds)));
        var answer = new ConditionalEffectNode<TurnEndedTriggeredEffectContext>(
            AllOf(IsPlayersTurn<TurnEndedTriggeredEffectContext>(), Standing<TurnEndedTriggeredEffectContext>(sphinx), asked),
            new ConditionalEffectNode<TurnEndedTriggeredEffectContext>(right,
                new CausalSequenceEffectNode<TurnEndedTriggeredEffectContext>(
                [
                    Mark<TurnEndedTriggeredEffectContext>(sphinx, SignId),
                    new ConditionalEffectNode<TurnEndedTriggeredEffectContext>(
                        new ComparisonExpression<TurnEndedTriggeredEffectContext>(
                            StacksOn<TurnEndedTriggeredEffectContext>(sphinx, SignId), ComparisonOperator.GreaterOrEqual,
                            N<TurnEndedTriggeredEffectContext>(SignsToSolve)),
                        new CausalSequenceEffectNode<TurnEndedTriggeredEffectContext>(
                        [
                            Unmark<TurnEndedTriggeredEffectContext>(sphinx, SignId),
                            Mark<TurnEndedTriggeredEffectContext>(sphinx, SolvedId, SolvedRounds),
                            new ModifyDefensivePoolNode<TurnEndedTriggeredEffectContext>(sphinx, StandardCombatIds.BlockDefensivePool,
                                N<TurnEndedTriggeredEffectContext>(-999)),
                        ])),
                ]),
                @else: new ApplyStatusNode<TurnEndedTriggeredEffectContext>(Applicant, new StatusDefinitionId(EntombedId),
                    N<TurnEndedTriggeredEffectContext>(1), sourceSelector: sphinx)));

        // Her own turn ends: a round of being solved has passed.
        var solvedRound = new ConditionalEffectNode<TurnEndedTriggeredEffectContext>(Wears<TurnEndedTriggeredEffectContext>(self, SolvedId),
            CountDown<TurnEndedTriggeredEffectContext>(self, SolvedId, new NoOpEffectNode<TurnEndedTriggeredEffectContext>()));

        yield return Face(RiddleRuleId, "The Riddle",
            "Each round she names Deeds or Workings: play more of those than the other for a Sign, else 1 Entombed. At 3 she is solved.",
            triggers:
            [
                On("TurnStarted", ask),
                On("TurnEnded", answer),
                On("TurnEnded", solvedRound, StatusTriggerScope.Bearer),
            ]);
        yield return Face(SphinxWantsDeedsId, "She Wants Deeds", "This turn play more Deeds than Workings, or 1 Entombed.");
        yield return Face(SphinxWantsWorkingsId, "She Wants Workings", "This turn play more Workings than Deeds, or 1 Entombed.");
        yield return Face(SignId, "Sign", "Riddles you answered. At 3 she is solved.", stacks: true);
        yield return Face(SolvedId, "Solved", "Solved for 2 rounds: she gains no Block and takes 25 % more damage.",
            StatusPolarity.Debuff, stacks: true, modifiers:
            [
                new PassiveModifierData(PassiveModifierPipeline.DamageReceived, PassiveModifierOperation.ScalePercent, 125),
                new PassiveModifierData(PassiveModifierPipeline.BlockGain, PassiveModifierOperation.ScalePercent, 0, RestrictDamageKind: null),
            ]);
    }

    // ── Ammit ─────────────────────────────────────────────────────────────────────────────────────────────

    // Ammit does not weigh (content review E19, 2026-11-08: six normals and the Weigher already do). She eats.
    // She keeps her eye on the most expensive card in your hand (printed cost; she has no taste for the
    // unplayable). Play that card and her eye moves on to the next most expensive one you hold; whatever she is
    // eyeing when your turn ends, she devours: gone for the fight, and she gains 2 Strength. Hold a Feather of
    // Truth and you are asked first: feed her the Feather instead, or let her have the card.
    //
    // ⚠ Why an eye and not a look at the hand as the turn ends: the engine discards the hand as the turn ends, and a
    // rule hearing the turn end cannot be sure whether the hand is still there. So the choice is made while the hand
    // is held — at the draw and after every play — and the discard only asks whether the eyed card went with it.
    //
    // The Feathers are the act's own: carried from its events into every fight, and she lays one down herself as
    // the fight opens (HeroOpening). Hers is spent first, so the carried count only falls for Feathers of yours.
    private static CounterId CostliestInHand => new("ammit_costliest");
    private static CounterId ChosenForAmmit => new("ammit_chosen");
    private static CounterId AmmitsFeatherSpent => new("ammit_feather_spent");
    public const string SpatOutMark = "spat_out_by_ammit";
    public const string AmmitsGazeId = "ammits_gaze";

    // Put her eye on the most expensive edible card in hand that was not just played (the first of them on a tie).
    private static IEffectNode<TContext> Eye<TContext>(ICombatantTargetSelector player) where TContext : class
    {
        var card = new IteratedCardExpression<TContext>();
        var cost = new CardInstanceBaseCostExpression<TContext>(card, StandardCombatIds.EnergyResource);
        var costliest = new CombatantCounterExpression<TContext>(player, CostliestInHand);
        var edible = AllOf(
            new NotExpression<TContext>(new CardInstanceHasTagExpression<TContext>(card, StandardCombatIds.UnplayableTag)),
            new NotExpression<TContext>(new CardInstanceHasMarkExpression<TContext>(card, new TagId(SpatOutMark))));
        IEffectNode<TContext> Set(CounterId counter, ICombatExpression<TContext, int> value) =>
            new SetCombatantCounterNode<TContext>(player, counter, value, relative: false);
        return new CausalSequenceEffectNode<TContext>(
        [
            Set(CostliestInHand, N<TContext>(-1)),
            new ForEachCardInZoneNode<TContext>(player, CardZone.Hand,
                new ConditionalEffectNode<TContext>(
                    AllOf(edible, new ComparisonExpression<TContext>(cost, ComparisonOperator.Greater, costliest)),
                    Set(CostliestInHand, cost))),
            Set(ChosenForAmmit, N<TContext>(0)),
            new ForEachCardInZoneNode<TContext>(player, CardZone.Hand,
                new ConditionalEffectNode<TContext>(
                    AllOf(edible, Eq(cost, costliest),
                        Eq(new CombatantCounterExpression<TContext>(player, ChosenForAmmit), N<TContext>(0))),
                    new CausalSequenceEffectNode<TContext>(
                    [
                        new MarkCardInstanceNode<TContext>(player, card, new TagId(DevouredMark)),
                        Set(ChosenForAmmit, N<TContext>(1)),
                    ]))),
            new ConditionalEffectNode<TContext>(
                Eq(new CombatantCounterExpression<TContext>(player, ChosenForAmmit), N<TContext>(1)),
                new ConditionalEffectNode<TContext>(
                    new NotExpression<TContext>(Wears<TContext>(player, AmmitsGazeId)), Mark<TContext>(player, AmmitsGazeId)),
                @else: Unmark<TContext>(player, AmmitsGazeId)),
        ]);
    }

    private static IEnumerable<StatusData> AmmitStatuses()
    {
        var ammit = Bearer(AmmitRuleId);
        var eye = new TagId(DevouredMark);
        var spat = new TagId(SpatOutMark);

        // The draw: last turn's marks are wiped from the hand, and her eye falls on its most expensive card.
        IEffectNode<CardsDrawnTriggeredEffectContext> Wipe(TagId mark) =>
            new ForEachCardInZoneNode<CardsDrawnTriggeredEffectContext>(Applicant, CardZone.Hand,
                new MarkCardInstanceNode<CardsDrawnTriggeredEffectContext>(Applicant,
                    new IteratedCardExpression<CardsDrawnTriggeredEffectContext>(), mark, remove: true),
                markFilter: mark);
        var draw = new ConditionalEffectNode<CardsDrawnTriggeredEffectContext>(
            AllOf(IsPlayersTurn<CardsDrawnTriggeredEffectContext>(), Standing<CardsDrawnTriggeredEffectContext>(ammit)),
            new CausalSequenceEffectNode<CardsDrawnTriggeredEffectContext>(
            [Wipe(eye), Wipe(spat), Eye<CardsDrawnTriggeredEffectContext>(Applicant)]));

        // The eyed card is played: it got away, and her eye moves to the next most expensive card you hold.
        var played = new TriggerEventCardInstanceExpression<CardPlayedTriggeredEffectContext>();
        var escape = new ConditionalEffectNode<CardPlayedTriggeredEffectContext>(
            AllOf(Standing<CardPlayedTriggeredEffectContext>(ammit),
                new CardInstanceHasMarkExpression<CardPlayedTriggeredEffectContext>(played, eye)),
            new CausalSequenceEffectNode<CardPlayedTriggeredEffectContext>(
            [
                new MarkCardInstanceNode<CardPlayedTriggeredEffectContext>(Applicant, played, eye, remove: true),
                new MarkCardInstanceNode<CardPlayedTriggeredEffectContext>(Applicant, played, spat),
                Eye<CardPlayedTriggeredEffectContext>(Applicant),
            ]));

        // The turn ends and the hand is discarded: an eyed card that went with it was held to the end. Heard as the
        // discard itself, the one moment the hand has certainly gone to the pile.
        var card = new FirstMarkedCardInOwnerZoneExpression<HandDiscardedTriggeredEffectContext>(Applicant, CardZone.DiscardPile, eye);
        var heldToTheEnd = Above(new CombatantZoneCardCountExpression<HandDiscardedTriggeredEffectContext>(Applicant, CardZone.DiscardPile, mark: eye), 0);
        var devour = new CausalSequenceEffectNode<HandDiscardedTriggeredEffectContext>(
        [
            new MoveCardToZoneNode<HandDiscardedTriggeredEffectContext>(Applicant, card, CardZone.BanishedPile),
            Mark<HandDiscardedTriggeredEffectContext>(ammit, "strength", DevourStrength),
            Mark<HandDiscardedTriggeredEffectContext>(ammit, DevouredId),
        ]);
        var feed = new CausalSequenceEffectNode<HandDiscardedTriggeredEffectContext>(
        [
            new MarkCardInstanceNode<HandDiscardedTriggeredEffectContext>(Applicant, card, eye, remove: true),
            new ModifyStatusStacksNode<HandDiscardedTriggeredEffectContext>(Applicant, new StatusDefinitionId(FeatherOfTruthId), N<HandDiscardedTriggeredEffectContext>(-1)),
            new ConditionalEffectNode<HandDiscardedTriggeredEffectContext>(
                Eq(new CombatantCounterExpression<HandDiscardedTriggeredEffectContext>(ammit, AmmitsFeatherSpent), N<HandDiscardedTriggeredEffectContext>(0)),
                new SetCombatantCounterNode<HandDiscardedTriggeredEffectContext>(ammit, AmmitsFeatherSpent, N<HandDiscardedTriggeredEffectContext>(1), relative: false),
                @else: new SetCombatantCounterNode<HandDiscardedTriggeredEffectContext>(Applicant, FeathersSpent, N<HandDiscardedTriggeredEffectContext>(1), relative: true)),
        ]);
        var feast = new ConditionalEffectNode<HandDiscardedTriggeredEffectContext>(
            AllOf(IsPlayersTurn<HandDiscardedTriggeredEffectContext>(), Standing<HandDiscardedTriggeredEffectContext>(ammit)),
            new CausalSequenceEffectNode<HandDiscardedTriggeredEffectContext>(
            [
                new ConditionalEffectNode<HandDiscardedTriggeredEffectContext>(heldToTheEnd,
                    new ConditionalEffectNode<HandDiscardedTriggeredEffectContext>(
                        Above(StacksOn<HandDiscardedTriggeredEffectContext>(Applicant, FeatherOfTruthId), 0),
                        new ChooseOptionsNode<HandDiscardedTriggeredEffectContext>(
                            [feed, devour],
                            ["Feed Ammit a Feather of Truth", "Let Ammit devour the card she is eyeing"],
                            count: 1, purpose: "Ammit devours the most expensive card you held to the end of your turn"),
                        @else: devour)),
                Unmark<HandDiscardedTriggeredEffectContext>(Applicant, AmmitsGazeId),
            ]));

        yield return Face(AmmitRuleId, "Hunger of the Devourer",
            $"At the end of your turn she devours the most expensive card still in your hand: it is gone for this fight, "
            + $"and she gains {DevourStrength} Strength. Feed her a Feather of Truth and she spares the card.",
            triggers: [On("CardsDrawn", draw), On("CardPlayed", escape), On("HandDiscarded", feast)]);
        yield return Face(DevouredId, "Devoured", "Cards she has eaten from your hand this fight.", stacks: true);
        yield return Face(AmmitsGazeId, "Ammit's Gaze",
            "Ammit is eyeing the most expensive card in your hand. Play it and she eyes the next; whatever she eyes "
            + "when your turn ends, she devours, unless you feed her a Feather of Truth.");
    }

    // ── The Lioness ───────────────────────────────────────────────────────────────────────────────────────

    private static IEnumerable<StatusData> LionessStatuses()
    {
        var lioness = Bearer(LionessRuleId);
        var self = CombatantTargetSelectors.Source;
        var hurt = new ConditionalEffectNode<DamageReceivedTriggeredEffectContext>(
            AllOf(Wears<DamageReceivedTriggeredEffectContext>(CombatantTargetSelectors.EventTarget, PassiveStatuses.ApplicantId),
                Above(new EventAmountExpression<DamageReceivedTriggeredEffectContext>(), 0),
                Standing<DamageReceivedTriggeredEffectContext>(lioness)),
            Mark<DamageReceivedTriggeredEffectContext>(lioness, RageId));
        var blocked = new ConditionalEffectNode<DamageDealtTriggeredEffectContext>(
            Eq(new EventAmountExpression<DamageDealtTriggeredEffectContext>(), N<DamageDealtTriggeredEffectContext>(0)),
            Unmark<DamageDealtTriggeredEffectContext>(self, RageId));

        yield return Face(LionessRuleId, "Sekhmet's Rage",
            "Every time you lose HP she gains 1 Rage. Block one of her attacks completely and she loses all Rage.",
            triggers: [On("DamageTaken", hurt), On("DamageDealt", blocked, StatusTriggerScope.Bearer)]);
        yield return Face(RageId, "Rage", "Her attacks deal 3 more per Rage. Block one of them completely and it is gone.",
            StatusPolarity.Buff, stacks: true, modifiers:
            [new PassiveModifierData(PassiveModifierPipeline.DamageDealt, PassiveModifierOperation.AddPerStack, RageDamage)]);
    }

    private static EffectProgram<EnemyActionContext> Prowl(int block) =>
        new(new CausalSequenceEffectNode<EnemyActionContext>(
        [
            new GainBlockNode<EnemyActionContext>(CombatantTargetSelectors.Source, Const(block)),
            Mark<EnemyActionContext>(CombatantTargetSelectors.Source, RageId),
        ]));

    // ── Priest of the Four Sons ───────────────────────────────────────────────────────────────────────────

    // A son's jar set beside the priest, and its power on him.
    private static IEffectNode<TContext> LayJar<TContext>(ICombatantTargetSelector priest, string key) where TContext : class
    {
        var jar = Jars.First(j => j.Key == key);
        return new CausalSequenceEffectNode<TContext>(
        [
            new SummonCombatantNode<TContext>(
                StandardCombatIds.EnemyTeam, N<TContext>(PriestJarHealth),
                new CombatantDefinitionId(CanopicJarEnemyId), jar.Name,
                startingStatuses:
                [
                    new StatusGrant(new StatusDefinitionId(CanopicJarId), 1),
                    new StatusGrant(new StatusDefinitionId(JarBodyId(key)), 1),
                ]),
            new ApplyStatusNode<TContext>(priest, new StatusDefinitionId(JarPowerId(key)), N<TContext>(1)),
        ]);
    }

    private static IEnumerable<StatusData> FourSonsStatuses()
    {
        var priest = Bearer(FourSonsRuleId);
        var lay = new ConditionalEffectNode<TurnStartedTriggeredEffectContext>(
            AllOf(IsPlayersTurn<TurnStartedTriggeredEffectContext>(), Standing<TurnStartedTriggeredEffectContext>(priest),
                Eq(new CombatantCounterExpression<TurnStartedTriggeredEffectContext>(priest, JarsLaid), N<TurnStartedTriggeredEffectContext>(0))),
            new CausalSequenceEffectNode<TurnStartedTriggeredEffectContext>(
            [
                new SetCombatantCounterNode<TurnStartedTriggeredEffectContext>(priest, JarsLaid, N<TurnStartedTriggeredEffectContext>(1), relative: false),
                .. FourSonsJars.Select(key => LayJar<TurnStartedTriggeredEffectContext>(priest, key)),
            ]));
        yield return Face(FourSonsRuleId, "The Four Sons",
            "A jar of each son of Horus stands beside him, each a power. Every third round he refills a broken one.",
            triggers: [On("TurnStarted", lay)]);
    }

    // The refill: the first broken jar in the sons' order comes back, and he braces.
    private static EffectProgram<EnemyActionContext> Refill()
    {
        var self = CombatantTargetSelectors.Source;
        IEffectNode<EnemyActionContext> Next(int i) => i >= FourSonsJars.Count
            ? new NoOpEffectNode<EnemyActionContext>()
            : new ConditionalEffectNode<EnemyActionContext>(
                Eq(CountOf<EnemyActionContext>(JarBodyId(FourSonsJars[i])), Const(0)),
                LayJar<EnemyActionContext>(self, FourSonsJars[i]),
                @else: Next(i + 1));
        return new(new CausalSequenceEffectNode<EnemyActionContext>(
        [
            Next(0),
            new GainBlockNode<EnemyActionContext>(self, Const(RefillBlock)),
        ]));
    }

    // ── Brood Mother ──────────────────────────────────────────────────────────────────────────────────────

    private static IEffectNode<TContext> Summon<TContext>(string definition, string name, int health, string status)
        where TContext : class =>
        new SummonCombatantNode<TContext>(
            StandardCombatIds.EnemyTeam, N<TContext>(health), new CombatantDefinitionId(definition), name,
            startingStatuses: [new StatusGrant(new StatusDefinitionId(status), 1)]);

    private static IEnumerable<StatusData> BroodStatuses()
    {
        var mother = Bearer(BroodRuleId);
        var self = CombatantTargetSelectors.Source;
        var lay = new ConditionalEffectNode<TurnStartedTriggeredEffectContext>(
            AllOf(IsPlayersTurn<TurnStartedTriggeredEffectContext>(), Standing<TurnStartedTriggeredEffectContext>(mother),
                Eq(new CombatantCounterExpression<TurnStartedTriggeredEffectContext>(mother, JarsLaid), N<TurnStartedTriggeredEffectContext>(0))),
            new CausalSequenceEffectNode<TurnStartedTriggeredEffectContext>(
            [
                new SetCombatantCounterNode<TurnStartedTriggeredEffectContext>(mother, JarsLaid, N<TurnStartedTriggeredEffectContext>(1), relative: false),
                .. Enumerable.Range(0, BroodJars).Select(_ =>
                    Summon<TurnStartedTriggeredEffectContext>(BroodJarEnemyId, "Brood Jar", BroodJarHealth, BroodJarId)),
            ]));

        // Her turn begins: every Scarab bites.
        var bite = new ForEachTargetEffectNode<TurnStartedTriggeredEffectContext>(Living(BroodScarabId),
            new RepeatEffectNode<TurnStartedTriggeredEffectContext>(N<TurnStartedTriggeredEffectContext>(ScarabBites),
                new DealDamageNode<TurnStartedTriggeredEffectContext>(Applicant, N<TurnStartedTriggeredEffectContext>(ScarabBite))));

        // Her turn ends: every second one, each intact jar hatches.
        var hatch = new ForEachTargetEffectNode<TurnEndedTriggeredEffectContext>(Living(BroodJarId),
            Summon<TurnEndedTriggeredEffectContext>(BroodScarabEnemyId, "Brood Scarab", ScarabHealth, BroodScarabId));
        var clutch = new ConditionalEffectNode<TurnEndedTriggeredEffectContext>(Wears<TurnEndedTriggeredEffectContext>(self, UntilHatchId),
            CountDown<TurnEndedTriggeredEffectContext>(self, UntilHatchId,
                new CausalSequenceEffectNode<TurnEndedTriggeredEffectContext>([hatch, Mark<TurnEndedTriggeredEffectContext>(self, UntilHatchId, HatchEvery)])));

        yield return Face(BroodRuleId, "The Brood",
            "Three brood jars stand beside her. Every second round each intact jar hatches a Scarab that bites you 3 times for 3.",
            triggers:
            [
                On("TurnStarted", lay),
                On("TurnStarted", bite, StatusTriggerScope.Bearer),
                On("TurnEnded", clutch, StatusTriggerScope.Bearer),
            ]);
        yield return Face(UntilHatchId, "Until the Hatching", "Rounds until each intact brood jar hatches a Scarab.", stacks: true);
        yield return Face(BroodJarId, "Brood Jar", "Every second round a Scarab hatches from it. It falls with the Brood Mother.");
        yield return Face(BroodScarabId, "Brood Scarab", "At the start of the Brood Mother's turn it bites you 3 times for 3.");
    }

    // The Brood Mother falls: her jars and her brood fall with her.
    private static IEffectNode<CombatantDownedTriggeredEffectContext> BroodDowned() =>
        new ConditionalEffectNode<CombatantDownedTriggeredEffectContext>(
            Wears<CombatantDownedTriggeredEffectContext>(CombatantTargetSelectors.SourceIncludingDowned, BroodRuleId),
            new CausalSequenceEffectNode<CombatantDownedTriggeredEffectContext>(
            [
                KillAll<CombatantDownedTriggeredEffectContext>(BroodJarId),
                KillAll<CombatantDownedTriggeredEffectContext>(BroodScarabId),
            ]));

    // ── The Embalmer ──────────────────────────────────────────────────────────────────────────────────────

    // A discount larger than any printed cost: the per-copy price clamps at 0 (CardInstanceCostModifier).
    private const int FreeOfCharge = -9;

    private static CounterId WrappedThisTurn => new("linen_wrapped_this_turn");

    // Each turn, once the hand is drawn, it wraps one card of it (cards already wrapped, and cards that cannot be
    // played, are passed over). A wrapped card costs 0 until it is played, and playing it is 1 Entombed.
    public static StatusData TheLinenHouse()
    {
        var embalmer = Bearer(LinenHouseId);
        var mark = new TagId(LinenWrappedMark);
        var card = new IteratedCardExpression<CardsDrawnTriggeredEffectContext>();

        var wrapOne = new ConditionalEffectNode<CardsDrawnTriggeredEffectContext>(
            AllOf(
                new ComparisonExpression<CardsDrawnTriggeredEffectContext>(
                    new CombatantCounterExpression<CardsDrawnTriggeredEffectContext>(embalmer, WrappedThisTurn),
                    ComparisonOperator.Less, N<CardsDrawnTriggeredEffectContext>(WrapsPerTurn)),
                new NotExpression<CardsDrawnTriggeredEffectContext>(new CardInstanceHasMarkExpression<CardsDrawnTriggeredEffectContext>(card, mark)),
                new NotExpression<CardsDrawnTriggeredEffectContext>(
                    new CardInstanceHasTagExpression<CardsDrawnTriggeredEffectContext>(card, StandardCombatIds.UnplayableTag))),
            new CausalSequenceEffectNode<CardsDrawnTriggeredEffectContext>(
            [
                new MarkCardInstanceNode<CardsDrawnTriggeredEffectContext>(Applicant, card, mark, sourceSelector: embalmer),
                new SetCardInstanceMarkCounterNode<CardsDrawnTriggeredEffectContext>(
                    Applicant, card, StandardCombatIds.CardCostDeltaCounter, N<CardsDrawnTriggeredEffectContext>(FreeOfCharge), relative: false),
                new SetCombatantCounterNode<CardsDrawnTriggeredEffectContext>(embalmer, WrappedThisTurn, N<CardsDrawnTriggeredEffectContext>(1), relative: true),
            ]));

        var wrap = new ConditionalEffectNode<CardsDrawnTriggeredEffectContext>(
            AllOf(IsPlayersTurn<CardsDrawnTriggeredEffectContext>(), Standing<CardsDrawnTriggeredEffectContext>(embalmer)),
            new CausalSequenceEffectNode<CardsDrawnTriggeredEffectContext>(
            [
                new SetCombatantCounterNode<CardsDrawnTriggeredEffectContext>(embalmer, WrappedThisTurn, N<CardsDrawnTriggeredEffectContext>(0), relative: false),
                new ForEachCardInZoneNode<CardsDrawnTriggeredEffectContext>(Applicant, CardZone.Hand, wrapOne),
            ]));

        var played = new TriggerEventCardInstanceExpression<CardPlayedTriggeredEffectContext>();
        var unwrap = new ConditionalEffectNode<CardPlayedTriggeredEffectContext>(
            new CardInstanceHasMarkExpression<CardPlayedTriggeredEffectContext>(played, mark),
            new CausalSequenceEffectNode<CardPlayedTriggeredEffectContext>(
            [
                new MarkCardInstanceNode<CardPlayedTriggeredEffectContext>(Applicant, played, mark, remove: true),
                new ApplyStatusNode<CardPlayedTriggeredEffectContext>(Applicant, new StatusDefinitionId(EntombedId),
                    N<CardPlayedTriggeredEffectContext>(1), sourceSelector: embalmer),
            ]));

        return Face(LinenHouseId, "The Linen House",
            "Each turn it wraps a card in your hand. A wrapped card costs 0, but playing it gives you 1 Entombed.",
            triggers: [On("CardsDrawn", wrap), On("CardPlayed", unwrap)]);
    }

    // ── The Restless King ─────────────────────────────────────────────────────────────────────────────────

    private static IEnumerable<StatusData> KingStatuses() =>
    [
        RisenStatus(KingRisenId, "Rises Twice",
            "Twice it is Downed and rises on its turn with 40 % of its HP, each time taking a random jar's power.",
            [.. Fall(RisePercent, KingPendingId), Put(KingRisenLastId)]),
        RisenStatus(KingRisenLastId, "Rises Once More",
            "Downed once more, it rises one last time on its turn with 40 % of its HP and another jar's power.",
            Fall(RisePercent, KingPendingId)),
        Face(KingPendingId, "A Jar Waiting", "It rises with a random jar's power."),
    ];

    // What the King rises with: one of six jar powers, drawn at random (it may draw the same one twice).
    private static IEffectNode<EnemyActionContext>[] KingTakesAJar()
    {
        var self = CombatantTargetSelectors.Source;
        var roll = new CombatantCounterExpression<EnemyActionContext>(self, KingRoll);
        return
        [
            new SetCombatantCounterNode<EnemyActionContext>(self, KingRoll,
                new RandomBelowExpression<EnemyActionContext>(KingPowers.Count), relative: false),
            .. KingPowers.Select((key, i) => (IEffectNode<EnemyActionContext>)
                new ConditionalEffectNode<EnemyActionContext>(Eq(roll, Const(i)),
                    new ApplyStatusNode<EnemyActionContext>(self, new StatusDefinitionId(JarPowerId(key)), Const(1)))),
            MoveTheBookmark<EnemyActionContext>(self, BurdenPaid, HungerRead),
        ];
    }

    // ── The Three Robbers ─────────────────────────────────────────────────────────────────────────────────

    public static readonly IReadOnlySet<string> Robbers =
        new HashSet<string>(StringComparer.Ordinal) { PryBarVeteranEnemyId, LampThiefEnemyId, CurseBearerEnemyId };

    private static IEnumerable<StatusData> RobberStatuses()
    {
        var self = CombatantTargetSelectors.Source;
        // Its own turn ends: a tool it holds and did not just swing is ready again.
        var rest = new ConditionalEffectNode<TurnEndedTriggeredEffectContext>(Wears<TurnEndedTriggeredEffectContext>(self, UsedToolId),
            Unmark<TurnEndedTriggeredEffectContext>(self, UsedToolId),
            @else: new CausalSequenceEffectNode<TurnEndedTriggeredEffectContext>(
            [
                .. Tools.Select(t => (IEffectNode<TurnEndedTriggeredEffectContext>)
                    new ConditionalEffectNode<TurnEndedTriggeredEffectContext>(Wears<TurnEndedTriggeredEffectContext>(self, HoldsToolId(t.Key)),
                        Mark<TurnEndedTriggeredEffectContext>(self, ToolReadyId(t.Key)))),
            ]));

        yield return Face(TombbreakerId, "The Three Robbers",
            "When one dies, the others gain 3 Strength and one takes up its tool. Kill all three and Tomb Gold joins your deck.",
            triggers: [On("TurnEnded", rest, StatusTriggerScope.Bearer)]);
        yield return Face(UsedToolId, "Swung a Tool", "It used a taken tool this turn.");
        foreach (var tool in Tools)
        {
            yield return Face(OwnsToolId(tool.Key), $"Owns the {tool.Name}", $"The {tool.Name} is its own tool.");
            yield return Face(HoldsToolId(tool.Key), $"Took the {tool.Name}",
                $"It took up a dead robber's {tool.Name}: every other turn it uses it.", StatusPolarity.Buff);
            yield return Face(ToolReadyId(tool.Key), $"{tool.Name} Ready", $"Its next action is the {tool.Name}.");
        }
    }

    // A robber falls: the others take it badly, the first of them takes up its tools, and the last to fall leaves
    // its loot.
    private static IEffectNode<CombatantDownedTriggeredEffectContext> RobberDowned()
    {
        var downed = CombatantTargetSelectors.SourceIncludingDowned;
        var heir = CombatantTargetSelectors.FirstTarget(Living(TombbreakerId));
        IEffectNode<CombatantDownedTriggeredEffectContext> Hand(Tool tool) =>
            new ConditionalEffectNode<CombatantDownedTriggeredEffectContext>(
                new OrExpression<CombatantDownedTriggeredEffectContext>(
                    Wears<CombatantDownedTriggeredEffectContext>(downed, OwnsToolId(tool.Key)),
                    Wears<CombatantDownedTriggeredEffectContext>(downed, HoldsToolId(tool.Key))),
                new CausalSequenceEffectNode<CombatantDownedTriggeredEffectContext>(
                [
                    Mark<CombatantDownedTriggeredEffectContext>(heir, HoldsToolId(tool.Key)),
                    Mark<CombatantDownedTriggeredEffectContext>(heir, ToolReadyId(tool.Key)),
                ]));
        return new ConditionalEffectNode<CombatantDownedTriggeredEffectContext>(
            Wears<CombatantDownedTriggeredEffectContext>(downed, TombbreakerId),
            new ConditionalEffectNode<CombatantDownedTriggeredEffectContext>(
                Above(CountOf<CombatantDownedTriggeredEffectContext>(TombbreakerId), 0),
                new CausalSequenceEffectNode<CombatantDownedTriggeredEffectContext>(
                [
                    new ForEachTargetEffectNode<CombatantDownedTriggeredEffectContext>(Living(TombbreakerId),
                        new ApplyStatusNode<CombatantDownedTriggeredEffectContext>(CombatantTargetSelectors.IterationTarget,
                            new StatusDefinitionId("strength"), N<CombatantDownedTriggeredEffectContext>(RobberRage))),
                    .. Tools.Select(Hand),
                ]),
                @else: new SetCombatantCounterNode<CombatantDownedTriggeredEffectContext>(Applicant, LootClaimed,
                    N<CombatantDownedTriggeredEffectContext>(1), relative: true)));
    }

    // A taken tool swung: the dead robber's own blow.
    private static EffectProgram<EnemyActionContext> StolenTool(Tool tool)
    {
        var self = CombatantTargetSelectors.Source;
        IEffectNode<EnemyActionContext> Hit(int damage) => new DealDamageNode<EnemyActionContext>(Applicant, Const(damage));
        IEffectNode<EnemyActionContext>[] blow = tool.Key switch
        {
            "pry_bar" => [Hit(20)],
            "knife" => [Hit(10), Hit(10)],
            _ => [Hit(21)],
        };
        return new(new CausalSequenceEffectNode<EnemyActionContext>(
        [
            .. blow,
            Unmark<EnemyActionContext>(self, ToolReadyId(tool.Key)),
            Mark<EnemyActionContext>(self, UsedToolId),
        ]));
    }
}
