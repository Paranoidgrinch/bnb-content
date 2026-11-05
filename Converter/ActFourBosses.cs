using RogueDeck.Core.Combat;
using RogueDeck.Run;
using RogueDeck.Sandbox.Composition;
using RogueDeck.Scenario.Authoring;
using BnbContent.Converter.Cards;

namespace BnbContent.Converter;

// Act IV's seven bosses since the rework of 2026-11-04 (user): one rule each, a second half from 50 %, never more
// than 1 Entombed at once and little Burdened. Every count is a stack on a status you can read.
//
//   The Pharaoh            a command each round; three obeyed break a name; three names, then his Ward is gone.
//   The Weigher            one scale for the whole fight: Deeds to the heart, Workings to the feather.
//   The Architect          two courses a round; a hit of 12 tears one down; at 6 the Capstone.
//   Renenutet              her bounty each turn: 7 HP for cards; at 7 Grain she harvests you for 49.
//   The First Scribe       your first three cards are written down and read back at you.
//   Selket                 four vessels pour on you in turn; from half (or with them broken) she rises once.
//   Anuket of the Cataract the Nile rises when you end on 0 Energy and falls when you keep some.
//
// Damage a boss deals from its own turn is dealt from a rule on its OWN turn (a mark written on your turn, paid on
// its turn), because a rule woken by the end of YOUR turn acts in your name: your Strength would swell it and a
// Feather would be spent on it.
public static partial class ActFour
{
    public const string PharaohEnemyId = "pharaoh_of_the_sealed_name";
    public const string WeigherEnemyId = "weigher_of_the_unspoken_heart";
    public const string ArchitectEnemyId = "architect_of_the_impossible_pyramid";
    public const string LadyEnemyId = "lady_of_the_black_granaries";
    public const string ScribeEnemyId = "first_scribe_of_the_house_of_life";
    public const string MotherEnemyId = "mother_of_natron_and_resin";
    public const string QueenEnemyId = "queen_of_the_flood_reckoning";

    // ── shared shapes ─────────────────────────────────────────────────────────────────────────────────────

    private static IEffectNode<TContext> Do<TContext>(params IEffectNode<TContext>[] steps) where TContext : class =>
        new CausalSequenceEffectNode<TContext>(steps);

    private static IEffectNode<TContext> If<TContext>(
        ICombatExpression<TContext, bool> condition, IEffectNode<TContext> then, IEffectNode<TContext>? @else = null)
        where TContext : class =>
        new ConditionalEffectNode<TContext>(condition, then, @else: @else);

    private static ICombatExpression<TContext, bool> Not<TContext>(ICombatExpression<TContext, bool> term) where TContext : class =>
        new NotExpression<TContext>(term);

    private static ICombatExpression<TContext, bool> Ge<TContext>(ICombatExpression<TContext, int> a, int b) where TContext : class =>
        new ComparisonExpression<TContext>(a, ComparisonOperator.GreaterOrEqual, N<TContext>(b));

    private static ICombatExpression<TContext, bool> Lt<TContext>(ICombatExpression<TContext, int> a, int b) where TContext : class =>
        new ComparisonExpression<TContext>(a, ComparisonOperator.Less, N<TContext>(b));

    private static IEffectNode<TContext> Hit<TContext>(ICombatExpression<TContext, int> amount) where TContext : class =>
        new DealDamageNode<TContext>(Applicant, amount);

    private static IEffectNode<TContext> Afflict<TContext>(string status, int stacks, ICombatantTargetSelector source)
        where TContext : class =>
        new ApplyStatusNode<TContext>(Applicant, new StatusDefinitionId(status), N<TContext>(stacks), sourceSelector: source);

    private static IEffectNode<TContext> Give<TContext>(string cardId, CardZone zone) where TContext : class =>
        new CreateCardInstanceNode<TContext>(Applicant, new CardDefinitionId(cardId), zone, N<TContext>(1));

    private static ICombatExpression<TContext, bool> AtHalf<TContext>(ICombatantTargetSelector body) where TContext : class =>
        new ComparisonExpression<TContext>(
            new MultiplyExpression<TContext>(new CombatantCurrentHealthExpression<TContext>(body), N<TContext>(100)),
            ComparisonOperator.LessOrEqual,
            new MultiplyExpression<TContext>(new CombatantMaxHealthExpression<TContext>(body), N<TContext>(50)));

    // The second half: the moment a blow takes the body to half its health, once.
    private static StatusTriggerData FromHalf(string phaseId, IEffectNode<DamageReceivedTriggeredEffectContext>? also = null)
    {
        var body = CombatantTargetSelectors.EventTarget;
        return On("DamageTaken", If(
            AllOf(Not(Wears<DamageReceivedTriggeredEffectContext>(body, phaseId)), AtHalf<DamageReceivedTriggeredEffectContext>(body)),
            Do(Mark<DamageReceivedTriggeredEffectContext>(body, phaseId), also ?? new NoOpEffectNode<DamageReceivedTriggeredEffectContext>())),
            StatusTriggerScope.Bearer);
    }

    // The size of a single hit: what reached health and what the Block took.
    private static ICombatExpression<DamageReceivedTriggeredEffectContext, int> HitSize =>
        new AddExpression<DamageReceivedTriggeredEffectContext>(
            new EventAmountExpression<DamageReceivedTriggeredEffectContext>(),
            new EventBlockedAmountExpression<DamageReceivedTriggeredEffectContext>());

    private static ICombatExpression<CardPlayedTriggeredEffectContext, bool> PlayedKind(string tag) =>
        new TriggerEventSourceCardHasTagExpression<CardPlayedTriggeredEffectContext>(new TagId(tag));

    // A turn of the player's that the boss is alive for.
    private static ICombatExpression<TContext, bool> PlayersTurnAgainst<TContext>(ICombatantTargetSelector boss) where TContext : class =>
        AllOf(IsPlayersTurn<TContext>(), Standing<TContext>(boss));

    // ── the registry ──────────────────────────────────────────────────────────────────────────────────────

    public static readonly IReadOnlySet<string> BossIdentities = new HashSet<string>(StringComparer.Ordinal)
    {
        PharaohEnemyId, WeigherEnemyId, ArchitectEnemyId, LadyEnemyId, ScribeEnemyId, MotherEnemyId, QueenEnemyId,
    };

    public static IReadOnlyList<StatusData> BossStatuses() =>
    [
        .. PharaohStatuses(), .. WeigherStatuses(), .. ArchitectStatuses(), .. RenenutetStatuses(),
        .. ScribeStatuses(), .. SelketStatuses(), .. AnuketStatuses(),
    ];

    public static IReadOnlyList<CardData> BossCards() => [.. PharaohCards(), .. RenenutetCards()];

    public static EffectProgram<EnemyActionContext>? BossIntent(string enemyId, string intentId) =>
        $"{enemyId}.{intentId}" switch
        {
            "pharaoh_of_the_sealed_name.reel" => Reel(),
            "pharaoh_of_the_sealed_name.all_names_are_mine" => AllNamesAreMine(32),
            "weigher_of_the_unspoken_heart.tip_the_scale" => TipTheScale(16),
            "architect_of_the_impossible_pyramid.capstone" => CapstoneDescends(36),
            "lady_of_the_black_granaries.harvest" => Harvest(39),
            "lady_of_the_black_granaries.feed" => Feed(),
            "queen_of_the_flood_reckoning.the_river_takes" => TheRiverTakes(32),
            _ when intentId == "rise" && BossIdentities.Contains(enemyId) => RiseAction(),
            _ => null,
        };

    // What falls with what, and Selket's second half when her last vessel breaks.
    public static IReadOnlyList<EncounterTriggerData> BossTriggers(string enemyId) => enemyId switch
    {
        PharaohEnemyId => Downed(If(
            Wears<CombatantDownedTriggeredEffectContext>(CombatantTargetSelectors.SourceIncludingDowned, SealedNameId),
            KillAll<CombatantDownedTriggeredEffectContext>(TraitorId))),
        MotherEnemyId => Downed(SelketDowned()),
        _ => [],
    };

    // ── The Pharaoh ───────────────────────────────────────────────────────────────────────────────────────

    public const string SealedNameId = "the_sealed_name";
    public const string CartoucheWardId = "cartouche_ward";
    public const string ThroneNameId = "the_throne_name";
    public const string TwoLandsNameId = "the_name_of_the_two_lands";
    public const string EternalNameId = "the_eternal_name";
    public const string NameExposedId = "the_nameless_pharaoh";
    public const string NameCracksId = "name_cracks";
    public const string NameBrokenId = "name_broken";
    public const string NamesGatheringId = "names_gathering";
    public const string AllNamesDueId = "all_names_due";
    public const string TraitorId = "traitor";
    public const string TraitorCondemnedId = "traitor_condemned";
    public const string TraitorEnemyId = "pharaohs_traitor";
    public const string OneBlowLandedId = "one_blow_landed";
    public const string KneelBrokenId = "kneel_broken";
    public const string KneelRefusedId = "kneel_refused";
    public const string StruckBackId = "struck_back";
    public const string TributePaidId = "tribute_paid";
    public const string TributeCardId = "pharaohs_tribute";
    public const string TributeTag = "pharaohs_tribute";
    public const string HisNameCardId = "his_name";

    public const int CracksToBreak = 3;
    public const int WardPercent = 80;
    public const int TraitorHealth = 25;
    public const int TraitorStrike = 10;
    public const int OneBlowNeeded = 15;
    public const int StrikeBack = 15;
    public const int SilenceCards = 2;
    public const int TributeHeal = 20;
    public const int KneelBlock = 25;
    public const int HisNameBlock = 6;

    public sealed record Command(string Key, string Name, string Text);

    public static readonly IReadOnlyList<Command> Commands =
    [
        new("slay", "Slay the Traitor", "Kill the Traitor this turn, or it stays and strikes you for 10 each round."),
        new("blow", "One Blow", "Hit him for 15 or more in a single blow this turn, or he strikes back for 15."),
        new("silence", "Silence", "Play at most 2 cards this turn, or 1 Entombed."),
        new("tribute", "Tribute", "Play the Tribute in your hand this turn, or he heals 20."),
        new("kneel", "Kneel", "Gain no Block this turn, or he gains 25 Block."),
        new("bear", "Bear My Name", "His Name is shuffled into your draw pile for this fight. Nothing else is asked."),
    ];

    public static string CommandId(string key) => $"command_{key}";

    // Harder with every name: what each of the three asks.
    private static readonly string[] ThroneCommands = ["blow", "silence", "tribute"];
    private static readonly string[] TwoLandsCommands = ["slay", "kneel", "blow", "tribute"];
    private static readonly string[] EternalCommands = ["slay", "kneel", "silence", "bear"];

    private static CounterId CommandRoll => new("pharaoh_command_roll");

    private static ICombatantTargetSelector Pharaoh => Bearer(SealedNameId);

    private static IEnumerable<StatusData> PharaohStatuses()
    {
        var pharaoh = Pharaoh;
        var self = CombatantTargetSelectors.Source;

        // Your turn opens: the command, chosen from what this name asks.
        IEffectNode<TurnStartedTriggeredEffectContext> Issue(string key) => Do(
            Mark<TurnStartedTriggeredEffectContext>(pharaoh, CommandId(key)),
            key switch
            {
                // One Traitor at most (user, 2026-11-04): one still standing is condemned again, no second one comes.
                "slay" => If(
                    Above(CountOf<TurnStartedTriggeredEffectContext>(TraitorId), 0),
                    new ForEachTargetEffectNode<TurnStartedTriggeredEffectContext>(Living(TraitorId),
                        Mark<TurnStartedTriggeredEffectContext>(CombatantTargetSelectors.IterationTarget, TraitorCondemnedId)),
                    new SummonCombatantNode<TurnStartedTriggeredEffectContext>(
                        StandardCombatIds.EnemyTeam, N<TurnStartedTriggeredEffectContext>(TraitorHealth),
                        new CombatantDefinitionId(TraitorEnemyId), "Traitor",
                        startingStatuses:
                        [
                            new StatusGrant(new StatusDefinitionId(TraitorId), 1),
                            new StatusGrant(new StatusDefinitionId(TraitorCondemnedId), 1),
                        ])),
                "tribute" => Give<TurnStartedTriggeredEffectContext>(TributeCardId, CardZone.Hand),
                "bear" => Give<TurnStartedTriggeredEffectContext>(HisNameCardId, CardZone.DrawPile),
                _ => new NoOpEffectNode<TurnStartedTriggeredEffectContext>(),
            });
        IEffectNode<TurnStartedTriggeredEffectContext> Pick(string[] pool) => Do(
        [
            new SetCombatantCounterNode<TurnStartedTriggeredEffectContext>(pharaoh, CommandRoll,
                new RandomBelowExpression<TurnStartedTriggeredEffectContext>(pool.Length), relative: false),
            .. pool.Select((key, i) => If(
                Eq(new CombatantCounterExpression<TurnStartedTriggeredEffectContext>(pharaoh, CommandRoll), N<TurnStartedTriggeredEffectContext>(i)),
                Issue(key))),
        ]);
        var command = If(
            AllOf(PlayersTurnAgainst<TurnStartedTriggeredEffectContext>(pharaoh),
                Not(Wears<TurnStartedTriggeredEffectContext>(pharaoh, NameExposedId))),
            Do(
                Unmark<TurnStartedTriggeredEffectContext>(pharaoh, OneBlowLandedId),
                Unmark<TurnStartedTriggeredEffectContext>(pharaoh, KneelBrokenId),
                Unmark<TurnStartedTriggeredEffectContext>(pharaoh, TributePaidId),
                If(Wears<TurnStartedTriggeredEffectContext>(pharaoh, EternalNameId), Pick(EternalCommands),
                    If(Wears<TurnStartedTriggeredEffectContext>(pharaoh, TwoLandsNameId), Pick(TwoLandsCommands),
                        Pick(ThroneCommands)))));

        // Your turn ends: the command judged.
        var crack = Do(
            Mark<TurnEndedTriggeredEffectContext>(pharaoh, NameCracksId),
            If(Ge(StacksOn<TurnEndedTriggeredEffectContext>(pharaoh, NameCracksId), CracksToBreak),
                Do(
                    Unmark<TurnEndedTriggeredEffectContext>(pharaoh, NameCracksId),
                    Mark<TurnEndedTriggeredEffectContext>(pharaoh, NameBrokenId),
                    If(Wears<TurnEndedTriggeredEffectContext>(pharaoh, EternalNameId),
                        Do(
                            Unmark<TurnEndedTriggeredEffectContext>(pharaoh, EternalNameId),
                            Unmark<TurnEndedTriggeredEffectContext>(pharaoh, CartoucheWardId),
                            Unmark<TurnEndedTriggeredEffectContext>(pharaoh, AllNamesDueId),
                            Unmark<TurnEndedTriggeredEffectContext>(pharaoh, NamesGatheringId),
                            Mark<TurnEndedTriggeredEffectContext>(pharaoh, NameExposedId)),
                        If(Wears<TurnEndedTriggeredEffectContext>(pharaoh, TwoLandsNameId),
                            Do(
                                Unmark<TurnEndedTriggeredEffectContext>(pharaoh, TwoLandsNameId),
                                Mark<TurnEndedTriggeredEffectContext>(pharaoh, EternalNameId)),
                            Do(
                                Unmark<TurnEndedTriggeredEffectContext>(pharaoh, ThroneNameId),
                                Mark<TurnEndedTriggeredEffectContext>(pharaoh, TwoLandsNameId)))))));
        ICombatExpression<TurnEndedTriggeredEffectContext, bool> Obeyed(string key) => key switch
        {
            "slay" => Eq(CountOf<TurnEndedTriggeredEffectContext>(TraitorCondemnedId), N<TurnEndedTriggeredEffectContext>(0)),
            "blow" => Wears<TurnEndedTriggeredEffectContext>(pharaoh, OneBlowLandedId),
            "silence" => new ComparisonExpression<TurnEndedTriggeredEffectContext>(
                ActThree.RealCardsPlayed<TurnEndedTriggeredEffectContext>(), ComparisonOperator.LessOrEqual,
                N<TurnEndedTriggeredEffectContext>(SilenceCards)),
            "tribute" => Wears<TurnEndedTriggeredEffectContext>(pharaoh, TributePaidId),
            "kneel" => Not(Wears<TurnEndedTriggeredEffectContext>(pharaoh, KneelBrokenId)),
            _ => Eq(N<TurnEndedTriggeredEffectContext>(0), N<TurnEndedTriggeredEffectContext>(0)),
        };
        IEffectNode<TurnEndedTriggeredEffectContext> Refused(string key) => key switch
        {
            "slay" => new ForEachTargetEffectNode<TurnEndedTriggeredEffectContext>(Living(TraitorCondemnedId),
                Unmark<TurnEndedTriggeredEffectContext>(CombatantTargetSelectors.IterationTarget, TraitorCondemnedId)),
            "blow" => Mark<TurnEndedTriggeredEffectContext>(pharaoh, StruckBackId),
            "silence" => Afflict<TurnEndedTriggeredEffectContext>(EntombedId, 1, pharaoh),
            "tribute" => new HealNode<TurnEndedTriggeredEffectContext>(pharaoh, N<TurnEndedTriggeredEffectContext>(TributeHeal)),
            "kneel" => Mark<TurnEndedTriggeredEffectContext>(pharaoh, KneelRefusedId),
            _ => new NoOpEffectNode<TurnEndedTriggeredEffectContext>(),
        };
        var judge = If(PlayersTurnAgainst<TurnEndedTriggeredEffectContext>(pharaoh), Do(
        [
            .. Commands.Select(c => If(Wears<TurnEndedTriggeredEffectContext>(pharaoh, CommandId(c.Key)),
                Do(
                    Unmark<TurnEndedTriggeredEffectContext>(pharaoh, CommandId(c.Key)),
                    If(Obeyed(c.Key), crack, Refused(c.Key))))),
        ]));

        // A single blow of 15 or more while One Blow stands.
        var blow = If(
            AllOf(Wears<DamageReceivedTriggeredEffectContext>(CombatantTargetSelectors.EventTarget, CommandId("blow")),
                Ge(HitSize, OneBlowNeeded)),
            Mark<DamageReceivedTriggeredEffectContext>(CombatantTargetSelectors.EventTarget, OneBlowLandedId));

        // Block gained by the player while he demands that they kneel.
        var stood = If(
            AllOf(Wears<BlockGainedTriggeredEffectContext>(CombatantTargetSelectors.EventTarget, PassiveStatuses.ApplicantId),
                Wears<BlockGainedTriggeredEffectContext>(pharaoh, CommandId("kneel")),
                Above(new EventAmountExpression<BlockGainedTriggeredEffectContext>(), 0)),
            Mark<BlockGainedTriggeredEffectContext>(pharaoh, KneelBrokenId));

        // His own turn: the traitors that stayed strike, a refused blow is answered; and as it ends, the Block a
        // refused kneel bought him, and in his Eternal Name the gathering of all three.
        var ownTurn = Do(
            new ForEachTargetEffectNode<TurnStartedTriggeredEffectContext>(Living(TraitorId),
                If(Not(Wears<TurnStartedTriggeredEffectContext>(CombatantTargetSelectors.IterationTarget, TraitorCondemnedId)),
                    Hit(N<TurnStartedTriggeredEffectContext>(TraitorStrike)))),
            If(Wears<TurnStartedTriggeredEffectContext>(self, StruckBackId),
                Do(Hit(N<TurnStartedTriggeredEffectContext>(StrikeBack)), Unmark<TurnStartedTriggeredEffectContext>(self, StruckBackId))));
        var ownTurnEnds = Do(
            If(Wears<TurnEndedTriggeredEffectContext>(self, KneelRefusedId),
                Do(new GainBlockNode<TurnEndedTriggeredEffectContext>(self, N<TurnEndedTriggeredEffectContext>(KneelBlock)),
                    Unmark<TurnEndedTriggeredEffectContext>(self, KneelRefusedId))),
            If(Wears<TurnEndedTriggeredEffectContext>(self, EternalNameId),
                Do(
                    Mark<TurnEndedTriggeredEffectContext>(self, NamesGatheringId),
                    If(Ge(StacksOn<TurnEndedTriggeredEffectContext>(self, NamesGatheringId), 2),
                        Do(Unmark<TurnEndedTriggeredEffectContext>(self, NamesGatheringId),
                            Mark<TurnEndedTriggeredEffectContext>(self, AllNamesDueId))))));

        yield return Face(SealedNameId, "The Sealed Name",
            "Each round he gives a command. Obey 3 and a name breaks: he loses a turn. Break all 3 names and his Ward falls.",
            triggers:
            [
                On("TurnStarted", command),
                On("TurnEnded", judge),
                On("DamageTaken", blow, StatusTriggerScope.Bearer),
                On("BlockGained", stood),
                On("TurnStarted", ownTurn, StatusTriggerScope.Bearer),
                On("TurnEnded", ownTurnEnds, StatusTriggerScope.Bearer),
            ]);
        yield return Face(CartoucheWardId, "Cartouche Ward", "While one of his names stands he takes 20 % less damage.",
            StatusPolarity.Buff, modifiers:
            [
                new PassiveModifierData(PassiveModifierPipeline.DamageReceived, PassiveModifierOperation.ScalePercent, WardPercent,
                    RestrictDamageKind: null),
            ]);
        yield return Face(ThroneNameId, "The Throne Name", "His first name. Its commands: One Blow, Silence, Tribute.");
        yield return Face(TwoLandsNameId, "The Name of the Two Lands",
            "His second name. Its commands: Slay the Traitor, Kneel, One Blow, Tribute.");
        yield return Face(EternalNameId, "The Eternal Name",
            "His last name. Its commands: Slay the Traitor, Kneel, Silence, Bear My Name. All Names Are Mine comes every other turn.");
        yield return Face(NameExposedId, "Nameless", "All three names are broken: no more commands, and no Ward.", StatusPolarity.Debuff);
        yield return Face(NameCracksId, "Cracks in the Name", "Commands you obeyed. At 3 the name breaks and he loses a turn.", stacks: true);
        yield return Face(NameBrokenId, "A Name Breaks", "His name broke: he loses his next turn.", StatusPolarity.Debuff);
        yield return Face(NamesGatheringId, "Gathering the Names", "At 2 his next action is All Names Are Mine.", stacks: true);
        yield return Face(AllNamesDueId, "All Names Are Mine", "His next action is All Names Are Mine.");
        foreach (var c in Commands)
            yield return Face(CommandId(c.Key), $"Command: {c.Name}", c.Text);
        yield return Face(TraitorId, "Traitor", "At the start of the Pharaoh's turn it strikes you for 10. It falls with the Pharaoh.");
        yield return Face(TraitorCondemnedId, "Condemned", "Kill it this turn, or it stays and strikes you every round.");
        yield return Face(OneBlowLandedId, "One Blow Landed", "You struck him for 15 or more in a single blow this turn.");
        yield return Face(KneelBrokenId, "You Stood", "You gained Block while he demanded you kneel.");
        yield return Face(KneelRefusedId, "Not Knelt", "At the end of his turn he gains 25 Block.");
        yield return Face(StruckBackId, "He Strikes Back", "At the start of his turn he strikes you for 15.");
        yield return Face(TributePaidId, "Tribute Paid", "You paid the tribute this turn.");
    }

    private static IEnumerable<CardData> PharaohCards()
    {
        yield return ActThree.Gift(TributeCardId, "Tribute", "Pay the Pharaoh his tribute. It does nothing else.", TributeTag,
            new ApplyStatusNode<CardPlayContext>(Pharaoh, new StatusDefinitionId(TributePaidId), new ConstantExpression<CardPlayContext>(1)),
            cost: 2);
        yield return new CardData
        {
            Id = HisNameCardId,
            NameKey = "His Name",
            DescriptionKey = "Gain 6 Block. The Pharaoh's name, borne for this fight.",
            Costs = [new ResourceCost(StandardCombatIds.EnergyResource, 1)],
            Tags = [new TagId("form"), new TagId(HisNameCardId)],
            Program = new EffectProgram<CardPlayContext>(
                new GainBlockNode<CardPlayContext>(CombatantTargetSelectors.Source, new ConstantExpression<CardPlayContext>(HisNameBlock))),
        };
    }

    private static EffectProgram<EnemyActionContext> Reel() =>
        new(Unmark<EnemyActionContext>(CombatantTargetSelectors.Source, NameBrokenId));

    private static EffectProgram<EnemyActionContext> AllNamesAreMine(int damage) =>
        new(Do(Hit(Const(damage)), Unmark<EnemyActionContext>(CombatantTargetSelectors.Source, AllNamesDueId)));

    // ── The Weigher ───────────────────────────────────────────────────────────────────────────────────────

    public const string GreatScaleId = "the_great_scale";
    public const string TowardHeartId = "toward_the_heart";
    public const string TowardFeatherId = "toward_the_feather";
    public const string CrocodileWaitsId = "the_crocodile_waits";
    public const string FeatherPanRestsId = "the_feather_pan_rests";
    public const string HeartRemembersId = "the_heart_weighs_double";

    public const int ScaleLimit = 5;
    public const int BitePerStep = 6;
    public const int BlockPerStep = 8;

    private static ICombatantTargetSelector Weigher => Bearer(GreatScaleId);

    private static IEffectNode<TContext> StepTo<TContext>(ICombatantTargetSelector weigher, string toward, string away)
        where TContext : class =>
        If(Above(StacksOn<TContext>(weigher, away), 0),
            CountDown<TContext>(weigher, away, new NoOpEffectNode<TContext>()),
            If(Lt(StacksOn<TContext>(weigher, toward), ScaleLimit), Mark<TContext>(weigher, toward)));

    private static IEnumerable<StatusData> WeigherStatuses()
    {
        var weigher = Weigher;
        var self = CombatantTargetSelectors.Source;
        IEffectNode<CardPlayedTriggeredEffectContext> Move(string toward, string away) =>
            If(Wears<CardPlayedTriggeredEffectContext>(weigher, HeartRemembersId),
                Do(StepTo<CardPlayedTriggeredEffectContext>(weigher, toward, away), StepTo<CardPlayedTriggeredEffectContext>(weigher, toward, away)),
                StepTo<CardPlayedTriggeredEffectContext>(weigher, toward, away));
        var play = If(PlayersTurnAgainst<CardPlayedTriggeredEffectContext>(weigher), Do(
            If(PlayedKind(CardAuthoring.DeedTag), Move(TowardHeartId, TowardFeatherId)),
            If(PlayedKind(CardAuthoring.WorkingTag), Move(TowardFeatherId, TowardHeartId))));

        var heart = StacksOn<TurnEndedTriggeredEffectContext>(weigher, TowardHeartId);
        var feather = StacksOn<TurnEndedTriggeredEffectContext>(weigher, TowardFeatherId);
        var weigh = If(PlayersTurnAgainst<TurnEndedTriggeredEffectContext>(weigher),
            If(AllOf(Eq(heart, N<TurnEndedTriggeredEffectContext>(0)), Eq(feather, N<TurnEndedTriggeredEffectContext>(0))),
                new ApplyStatusNode<TurnEndedTriggeredEffectContext>(Applicant, new StatusDefinitionId(ScaleFeatherId), N<TurnEndedTriggeredEffectContext>(1)),
                Do(
                    If(Above(heart, 0), new ApplyStatusNode<TurnEndedTriggeredEffectContext>(weigher, new StatusDefinitionId(CrocodileWaitsId),
                        new MultiplyExpression<TurnEndedTriggeredEffectContext>(heart, N<TurnEndedTriggeredEffectContext>(BitePerStep)))),
                    If(Above(feather, 0), new ApplyStatusNode<TurnEndedTriggeredEffectContext>(weigher, new StatusDefinitionId(FeatherPanRestsId),
                        new MultiplyExpression<TurnEndedTriggeredEffectContext>(feather, N<TurnEndedTriggeredEffectContext>(BlockPerStep)))))));

        var bites = StacksOn<TurnStartedTriggeredEffectContext>(self, CrocodileWaitsId);
        var bite = If(Above(bites, 0), Do(Hit(bites), Unmark<TurnStartedTriggeredEffectContext>(self, CrocodileWaitsId)));
        var rests = StacksOn<TurnEndedTriggeredEffectContext>(self, FeatherPanRestsId);
        var rest = If(Above(rests, 0),
            Do(new GainBlockNode<TurnEndedTriggeredEffectContext>(self, rests), Unmark<TurnEndedTriggeredEffectContext>(self, FeatherPanRestsId)));

        yield return Face(GreatScaleId, "The Great Scale",
            "Deeds tip it to the heart, Workings to the feather; it never resets. Level at turn end: a Feather; tipped, he bites or guards.",
            triggers:
            [
                On("CardPlayed", play),
                On("TurnEnded", weigh),
                On("TurnStarted", bite, StatusTriggerScope.Bearer),
                On("TurnEnded", rest, StatusTriggerScope.Bearer),
                FromHalf(HeartRemembersId),
            ]);
        yield return Face(TowardHeartId, "Toward the Heart",
            "The scale leans to the heart. At the end of your turn the crocodile is owed 6 per step.", stacks: true);
        yield return Face(TowardFeatherId, "Toward the Feather",
            "The scale leans to the feather. At the end of your turn he is owed 8 Block per step.", stacks: true);
        yield return Face(CrocodileWaitsId, "The Crocodile Waits", "At the start of his turn the crocodile bites you for this much.", stacks: true);
        yield return Face(FeatherPanRestsId, "The Feather Pan Rests", "At the end of his turn he gains this much Block.", stacks: true);
        yield return Face(HeartRemembersId, "The Heart Weighs Double", "From half his health every card moves the scale 2 steps.");
    }

    // Tip the scale: one step further from level (to the heart when it stands level), and a guard.
    private static EffectProgram<EnemyActionContext> TipTheScale(int block)
    {
        var self = CombatantTargetSelectors.Source;
        return new(Do(
            If(Above(StacksOn<EnemyActionContext>(self, TowardFeatherId), 0),
                StepTo<EnemyActionContext>(self, TowardFeatherId, TowardHeartId),
                StepTo<EnemyActionContext>(self, TowardHeartId, TowardFeatherId)),
            new GainBlockNode<EnemyActionContext>(self, Const(block))));
    }

    // ── The Architect ─────────────────────────────────────────────────────────────────────────────────────

    public const string ImpossiblePyramidId = "the_impossible_pyramid";
    public const string CourseId = "pyramid_course";
    public const string PlanAlwaysCorrectId = "the_plan_was_always_correct";

    public const int CoursesPerRound = 2;
    public const int CapstoneAt = 6;
    public const int CoursesAfterCapstone = 3;
    public const int TearAt = 12;
    public const int TearAtLater = 15;

    private static IEnumerable<StatusData> ArchitectStatuses()
    {
        var self = CombatantTargetSelectors.Source;
        var courses = StacksOn<TurnEndedTriggeredEffectContext>(self, CourseId);
        var build = new RepeatEffectNode<TurnEndedTriggeredEffectContext>(N<TurnEndedTriggeredEffectContext>(CoursesPerRound),
            If(Lt(StacksOn<TurnEndedTriggeredEffectContext>(self, CourseId), CapstoneAt), Mark<TurnEndedTriggeredEffectContext>(self, CourseId)));

        var body = CombatantTargetSelectors.EventTarget;
        var later = Wears<DamageReceivedTriggeredEffectContext>(body, PlanAlwaysCorrectId);
        var tears = new OrExpression<DamageReceivedTriggeredEffectContext>(
            AllOf(later, Ge(HitSize, TearAtLater)),
            AllOf(Not(later), Ge(HitSize, TearAt)));
        var tear = If(AllOf(Above(StacksOn<DamageReceivedTriggeredEffectContext>(body, CourseId), 0), tears),
            CountDown<DamageReceivedTriggeredEffectContext>(body, CourseId, new NoOpEffectNode<DamageReceivedTriggeredEffectContext>()));

        yield return Face(ImpossiblePyramidId, "The Impossible Pyramid",
            "He builds 2 courses a round. Any single hit of 12 or more tears one down. At 6 his next action is the Capstone.",
            triggers:
            [
                On("TurnEnded", build, StatusTriggerScope.Bearer),
                On("DamageTaken", tear, StatusTriggerScope.Bearer),
                FromHalf(PlanAlwaysCorrectId),
            ]);
        yield return Face(CourseId, "Course", "Courses of the pyramid. At 6 the Capstone comes down: 45 and 1 Entombed.", stacks: true);
        yield return Face(PlanAlwaysCorrectId, "The Plan Was Always Correct",
            "From half his health a course only comes down to a single hit of 15 or more.");
    }

    // The capstone, and three courses left standing to begin again.
    private static EffectProgram<EnemyActionContext> CapstoneDescends(int damage)
    {
        var self = CombatantTargetSelectors.Source;
        return new(Do(
            Hit(Const(damage)),
            Afflict<EnemyActionContext>(EntombedId, 1, self),
            Unmark<EnemyActionContext>(self, CourseId),
            Mark<EnemyActionContext>(self, CourseId, CoursesAfterCapstone)));
    }

    // ── Renenutet ─────────────────────────────────────────────────────────────────────────────────────────

    public const string BountyRuleId = "her_bounty";
    public const string GrainId = "renenutet_grain";
    public const string SeventhYearId = "the_seventh_year";
    public const string BountyCardId = "renenutets_bounty";
    public const string SeventhBountyCardId = "renenutets_bounty_seventh_year";
    public const string BountyTag = "renenutets_bounty";

    public const int HarvestAt = 7;
    public const int BountyPrice = 7;
    public const int FeedPerGrain = 7;

    private static ICombatantTargetSelector Renenutet => Bearer(BountyRuleId);

    private static IEnumerable<StatusData> RenenutetStatuses()
    {
        var ren = Renenutet;
        var offer = If(PlayersTurnAgainst<TurnStartedTriggeredEffectContext>(ren),
            If(Wears<TurnStartedTriggeredEffectContext>(ren, SeventhYearId),
                Give<TurnStartedTriggeredEffectContext>(SeventhBountyCardId, CardZone.Hand),
                Give<TurnStartedTriggeredEffectContext>(BountyCardId, CardZone.Hand)));
        yield return Face(BountyRuleId, "Her Bounty",
            "Her Bounty is in your hand each turn: 7 HP for cards, and Grain for her. At 7 Grain she harvests you for 49.",
            triggers: [On("TurnStarted", offer), FromHalf(SeventhYearId)]);
        yield return Face(GrainId, "Grain", "Grain in her stores. At 7 her next action is the Harvest: 49.", stacks: true);
        yield return Face(SeventhYearId, "The Seventh Year",
            "From half her health her Bounty is 7 HP for 7 cards, and each one is 2 Grain.");
    }

    private static IEnumerable<CardData> RenenutetCards()
    {
        var player = CombatantTargetSelectors.Source;
        IEffectNode<CardPlayContext> Price() => new SetHealthNode<CardPlayContext>(player,
            new SubtractExpression<CardPlayContext>(new CombatantCurrentHealthExpression<CardPlayContext>(player),
                new ConstantExpression<CardPlayContext>(BountyPrice)));
        IEffectNode<CardPlayContext> Draw(int cards) => new DrawCardsNode<CardPlayContext>(player, new ConstantExpression<CardPlayContext>(cards));
        IEffectNode<CardPlayContext> Store(int grain) => new ApplyStatusNode<CardPlayContext>(Renenutet,
            new StatusDefinitionId(GrainId), new ConstantExpression<CardPlayContext>(grain));

        yield return ActThree.Gift(BountyCardId, "Her Bounty", "Lose 7 HP. Draw 4 cards. Gain 1 Energy. She stores 1 Grain.", BountyTag,
            Do(Price(), Draw(4),
                new GainResourceNode<CardPlayContext>(player, StandardCombatIds.EnergyResource, new ConstantExpression<CardPlayContext>(1)),
                Store(1)));
        yield return ActThree.Gift(SeventhBountyCardId, "Her Bounty", "Lose 7 HP. Draw 7 cards. She stores 2 Grain.", BountyTag,
            Do(Price(), Draw(7), Store(2)));
    }

    private static EffectProgram<EnemyActionContext> Harvest(int damage) =>
        new(Do(Hit(Const(damage)), Unmark<EnemyActionContext>(CombatantTargetSelectors.Source, GrainId)));

    private static EffectProgram<EnemyActionContext> Feed()
    {
        var self = CombatantTargetSelectors.Source;
        return new(new HealNode<EnemyActionContext>(self,
            new MultiplyExpression<EnemyActionContext>(StacksOn<EnemyActionContext>(self, GrainId), Const(FeedPerGrain))));
    }

    // ── The First Scribe ──────────────────────────────────────────────────────────────────────────────────

    public const string HouseOfLifeId = "the_house_of_life";
    public const string PalimpsestId = "the_palimpsest";

    public const int ScribeEntries = 3;
    public const int ReadDeed = 6;
    public const int ReadWorking = 6;

    private static readonly string[] Numerals = ["I", "II", "III"];

    public static string EntryId(int slot, string kind) => $"scroll_{slot}_{kind}";

    private static CounterId ScribeWritten => new("scribe_written_this_turn");

    private static ICombatantTargetSelector Scribe => Bearer(HouseOfLifeId);

    private static IEnumerable<StatusData> ScribeStatuses()
    {
        var scribe = Scribe;
        var self = CombatantTargetSelectors.Source;
        string[] kinds = ["deed", "working"];

        ICombatExpression<CardPlayedTriggeredEffectContext, bool> Filled(int slot) =>
            new OrExpression<CardPlayedTriggeredEffectContext>(
                Wears<CardPlayedTriggeredEffectContext>(scribe, EntryId(slot, "deed")),
                Wears<CardPlayedTriggeredEffectContext>(scribe, EntryId(slot, "working")));
        IEffectNode<CardPlayedTriggeredEffectContext> Clear(int slot) => Do(
            Unmark<CardPlayedTriggeredEffectContext>(scribe, EntryId(slot, "deed")),
            Unmark<CardPlayedTriggeredEffectContext>(scribe, EntryId(slot, "working")));
        IEffectNode<CardPlayedTriggeredEffectContext> Copy(int from, int to) => Do(
            Clear(to),
            If(Wears<CardPlayedTriggeredEffectContext>(scribe, EntryId(from, "deed")),
                Mark<CardPlayedTriggeredEffectContext>(scribe, EntryId(to, "deed")),
                If(Wears<CardPlayedTriggeredEffectContext>(scribe, EntryId(from, "working")),
                    Mark<CardPlayedTriggeredEffectContext>(scribe, EntryId(to, "working")))));
        IEffectNode<CardPlayedTriggeredEffectContext> WriteFirstEmpty(string kind) =>
            If(Not(Filled(1)), Mark<CardPlayedTriggeredEffectContext>(scribe, EntryId(1, kind)),
                If(Not(Filled(2)), Mark<CardPlayedTriggeredEffectContext>(scribe, EntryId(2, kind)),
                    If(Not(Filled(3)), Mark<CardPlayedTriggeredEffectContext>(scribe, EntryId(3, kind)))));
        var written = new CombatantCounterExpression<CardPlayedTriggeredEffectContext>(scribe, ScribeWritten);
        IEffectNode<CardPlayedTriggeredEffectContext> Write(string kind) =>
            If(Wears<CardPlayedTriggeredEffectContext>(scribe, PalimpsestId),
                // the last three: a full scroll moves up and the newest is written at the bottom
                If(Filled(3), Do(Copy(2, 1), Copy(3, 2), Clear(3), Mark<CardPlayedTriggeredEffectContext>(scribe, EntryId(3, kind))),
                    WriteFirstEmpty(kind)),
                If(Lt(written, ScribeEntries),
                    Do(WriteFirstEmpty(kind),
                        new SetCombatantCounterNode<CardPlayedTriggeredEffectContext>(scribe, ScribeWritten,
                            N<CardPlayedTriggeredEffectContext>(1), relative: true))));
        var erase = If(Filled(3), Clear(3), If(Filled(2), Clear(2), If(Filled(1), Clear(1))));
        var play = If(PlayersTurnAgainst<CardPlayedTriggeredEffectContext>(scribe), Do(
            If(PlayedKind(CardAuthoring.DeedTag), Write("deed")),
            If(PlayedKind(CardAuthoring.WorkingTag), Write("working")),
            If(PlayedKind(CardAuthoring.RiteTag), erase)));

        var morning = If(PlayersTurnAgainst<TurnStartedTriggeredEffectContext>(scribe),
            new SetCombatantCounterNode<TurnStartedTriggeredEffectContext>(scribe, ScribeWritten, N<TurnStartedTriggeredEffectContext>(0), relative: false));

        // His turn ends: the scroll is read back, and wiped.
        var read = Do(
        [
            .. Enumerable.Range(1, ScribeEntries).SelectMany(slot => new[]
            {
                If(Wears<TurnEndedTriggeredEffectContext>(self, EntryId(slot, "deed")), Hit(N<TurnEndedTriggeredEffectContext>(ReadDeed))),
                If(Wears<TurnEndedTriggeredEffectContext>(self, EntryId(slot, "working")),
                    new GainBlockNode<TurnEndedTriggeredEffectContext>(self, N<TurnEndedTriggeredEffectContext>(ReadWorking))),
            }),
            .. Enumerable.Range(1, ScribeEntries).SelectMany(slot =>
                kinds.Select(kind => Unmark<TurnEndedTriggeredEffectContext>(self, EntryId(slot, kind)))),
        ]);

        yield return Face(HouseOfLifeId, "The House of Life",
            "He writes your first 3 Deeds and Workings, reads them back: Deeds hit you for 6, Workings give him 6 Block. Rites erase one.",
            triggers:
            [
                On("CardPlayed", play),
                On("TurnStarted", morning),
                On("TurnEnded", read, StatusTriggerScope.Bearer),
                FromHalf(PalimpsestId),
            ]);
        yield return Face(PalimpsestId, "The Palimpsest", "From half his health he writes your LAST 3 Deeds and Workings instead.");
        for (var slot = 1; slot <= ScribeEntries; slot++)
        {
            yield return Face(EntryId(slot, "deed"), $"Entry {Numerals[slot - 1]}: a Deed",
                "Written on his scroll. As his turn ends it is read back: 6 damage to you.");
            yield return Face(EntryId(slot, "working"), $"Entry {Numerals[slot - 1]}: a Working",
                "Written on his scroll. As his turn ends it is read back: 6 Block for him.");
        }
    }

    // ── Selket ────────────────────────────────────────────────────────────────────────────────────────────

    public const string SelketRuleId = "selkets_vessels";
    public const string SelketVesselId = "selket_vessel";
    public const string SelketVesselEnemyId = "selket_vessel";
    public const string PoursNextId = "pours_next";
    public const string SelketWrappedId = "selket_wrapped";
    public const string SelketRisenId = "selket_risen";

    public const int VesselHealth = 40;

    public sealed record Vessel(string Key, string Name, string Text, string Status, int Stacks);

    public static readonly IReadOnlyList<Vessel> Vessels =
    [
        new("weight", "Vessel of Weight", "When it pours: 2 Burdened on you.", BurdenedId, 2),
        new("venom", "Vessel of Venom", "When it pours: 4 Poison on you.", "poison", 4),
        new("dread", "Vessel of Dread", "When it pours: 1 Panic on you.", "panic", 1),
        new("tomb", "Vessel of the Tomb", "When it pours: 1 Entombed on you.", EntombedId, 1),
    ];

    public static string VesselId(string key) => $"selket_vessel_{key}";

    private static CounterId SelketPour => new("selket_pour");
    private static CounterId SelketLaid => new("selket_vessels_laid");

    private static ICombatantTargetSelector Selket => Bearer(SelketRuleId);

    // Her second half: she wraps herself in linen and will rise once.
    private static IEffectNode<TContext> Wrap<TContext>(ICombatantTargetSelector selket) where TContext : class =>
        If(AllOf(Standing<TContext>(selket), Not(Wears<TContext>(selket, SelketWrappedId))),
            Do(Mark<TContext>(selket, SelketWrappedId), Mark<TContext>(selket, SelketRisenId)));

    private static IEnumerable<StatusData> SelketStatuses()
    {
        var selket = Selket;
        var self = CombatantTargetSelectors.Source;

        var lay = If(
            AllOf(PlayersTurnAgainst<TurnStartedTriggeredEffectContext>(selket),
                Eq(new CombatantCounterExpression<TurnStartedTriggeredEffectContext>(selket, SelketLaid), N<TurnStartedTriggeredEffectContext>(0))),
            Do(
            [
                new SetCombatantCounterNode<TurnStartedTriggeredEffectContext>(selket, SelketLaid, N<TurnStartedTriggeredEffectContext>(1), relative: false),
                .. Vessels.Select((v, i) => (IEffectNode<TurnStartedTriggeredEffectContext>)
                    new SummonCombatantNode<TurnStartedTriggeredEffectContext>(
                        StandardCombatIds.EnemyTeam, N<TurnStartedTriggeredEffectContext>(VesselHealth),
                        new CombatantDefinitionId(SelketVesselEnemyId), v.Name,
                        startingStatuses:
                        [
                            new StatusGrant(new StatusDefinitionId(SelketVesselId), 1),
                            new StatusGrant(new StatusDefinitionId(VesselId(v.Key)), 1),
                            .. i == 0 ? [new StatusGrant(new StatusDefinitionId(PoursNextId), 1)] : Array.Empty<StatusGrant>(),
                        ])),
            ]));

        // Her turn opens: the vessel whose turn it is pours (a broken one pours nothing), and the next is shown.
        var index = new CombatantCounterExpression<TurnStartedTriggeredEffectContext>(self, SelketPour);
        var pour = Do(
        [
            .. Vessels.Select((v, i) => If(
                AllOf(Eq(index, N<TurnStartedTriggeredEffectContext>(i)), Above(CountOf<TurnStartedTriggeredEffectContext>(VesselId(v.Key)), 0)),
                Afflict<TurnStartedTriggeredEffectContext>(v.Status, v.Stacks, self))),
            new SetCombatantCounterNode<TurnStartedTriggeredEffectContext>(self, SelketPour,
                new RemainderExpression<TurnStartedTriggeredEffectContext>(
                    new AddExpression<TurnStartedTriggeredEffectContext>(index, N<TurnStartedTriggeredEffectContext>(1)),
                    N<TurnStartedTriggeredEffectContext>(Vessels.Count)), relative: false),
            new ForEachTargetEffectNode<TurnStartedTriggeredEffectContext>(Living(SelketVesselId),
                Unmark<TurnStartedTriggeredEffectContext>(CombatantTargetSelectors.IterationTarget, PoursNextId)),
            .. Vessels.Select((v, i) => If(Eq(index, N<TurnStartedTriggeredEffectContext>(i)),
                new ForEachTargetEffectNode<TurnStartedTriggeredEffectContext>(Living(VesselId(v.Key)),
                    Mark<TurnStartedTriggeredEffectContext>(CombatantTargetSelectors.IterationTarget, PoursNextId)))),
        ]);

        var body = CombatantTargetSelectors.EventTarget;
        var half = If(AtHalf<DamageReceivedTriggeredEffectContext>(body), Wrap<DamageReceivedTriggeredEffectContext>(body));

        yield return Face(SelketRuleId, "Selket's Vessels",
            "Her 4 vessels pour on you in turn, one each turn of hers; broken ones pour nothing. From half, or all broken, she rises once.",
            triggers:
            [
                On("TurnStarted", lay),
                On("TurnStarted", pour, StatusTriggerScope.Bearer),
                On("DamageTaken", half, StatusTriggerScope.Bearer),
            ]);
        yield return Face(SelketVesselId, "Selket's Vessel", "One of her four vessels. It does not act; it falls with Selket.");
        foreach (var v in Vessels)
            yield return Face(VesselId(v.Key), v.Name, v.Text);
        yield return Face(PoursNextId, "Pours Next", "At the start of Selket's turn this vessel pours on you.");
        yield return Face(SelketWrappedId, "Wrapped in Linen", "She has wrapped herself: her next death only Downs her.");
        yield return RisenStatus(SelketRisenId, "Rises Once",
            "At her next death she is Downed; on her turn she rises with 40 % of her HP, free of debuffs.", Fall(RisePercent));
    }

    // A vessel breaks: the last one wraps her. Selket falls: her vessels fall with her.
    private static IEffectNode<CombatantDownedTriggeredEffectContext> SelketDowned()
    {
        var downed = CombatantTargetSelectors.SourceIncludingDowned;
        return Do(
            If(AllOf(Wears<CombatantDownedTriggeredEffectContext>(downed, SelketVesselId),
                    Eq(CountOf<CombatantDownedTriggeredEffectContext>(SelketVesselId), N<CombatantDownedTriggeredEffectContext>(0))),
                Wrap<CombatantDownedTriggeredEffectContext>(Selket)),
            If(Wears<CombatantDownedTriggeredEffectContext>(downed, SelketRuleId),
                KillAll<CombatantDownedTriggeredEffectContext>(SelketVesselId)));
    }

    // ── Anuket of the Cataract ────────────────────────────────────────────────────────────────────────────

    public const string NilometerId = "the_nilometer";
    public const string NileLevelId = "nile_level";
    public const string FloodDriftsId = "the_flood_drifts";

    public const int NileStart = 3;
    public const int NileTop = 5;
    public const int LowWaterBlock = 12;

    public sealed record NileLevel(int Level, string Id, string Name, string Text);

    public static readonly IReadOnlyList<NileLevel> NileLevels =
    [
        new(1, "nile_drought", "Drought", "The river is gone: her attacks deal 3 more."),
        new(2, "nile_low", "Low Water", "At the start of your turn she loses 12 Block."),
        new(3, "nile_ordered", "Ordered Flood", "At the start of your turn she loses 12 Block, and you hold 1 Energy."),
        new(4, "nile_high", "High Water", "At the start of your turn you gain 1 Burdened."),
        new(5, "nile_black_flood", "Black Flood", "Her next action is The River Takes: 40 and 1 Entombed."),
    ];

    public static string WaterBlackId => NileLevels[4].Id;

    private static ICombatantTargetSelector Anuket => Bearer(NilometerId);

    private static IEffectNode<TContext> NameTheLevel<TContext>(ICombatantTargetSelector anuket) where TContext : class => Do(
    [
        .. NileLevels.Select(l => Unmark<TContext>(anuket, l.Id)),
        .. NileLevels.Select(l => If(Eq(StacksOn<TContext>(anuket, NileLevelId), N<TContext>(l.Level)), Mark<TContext>(anuket, l.Id))),
    ]);

    private static IEffectNode<TContext> Flow<TContext>(ICombatantTargetSelector anuket) where TContext : class =>
        If(Lt(StacksOn<TContext>(anuket, NileLevelId), NileTop), Mark<TContext>(anuket, NileLevelId));

    private static IEffectNode<TContext> Ebb<TContext>(ICombatantTargetSelector anuket) where TContext : class =>
        If(Above(StacksOn<TContext>(anuket, NileLevelId), 1),
            new ModifyStatusStacksNode<TContext>(anuket, new StatusDefinitionId(NileLevelId), N<TContext>(-1)));

    private static IEnumerable<StatusData> AnuketStatuses()
    {
        var anuket = Anuket;
        var self = CombatantTargetSelectors.Source;

        // Your turn ends: nothing left in hand to spend and the river rises; Energy kept and it falls.
        var energy = new CombatantCurrentResourceExpression<TurnEndedTriggeredEffectContext>(Applicant, StandardCombatIds.EnergyResource);
        var gauge = If(PlayersTurnAgainst<TurnEndedTriggeredEffectContext>(anuket), Do(
            If(Eq(energy, N<TurnEndedTriggeredEffectContext>(0)), Flow<TurnEndedTriggeredEffectContext>(anuket), Ebb<TurnEndedTriggeredEffectContext>(anuket)),
            NameTheLevel<TurnEndedTriggeredEffectContext>(anuket)));

        // Your turn opens: what the water does.
        var low = new OrExpression<TurnStartedTriggeredEffectContext>(
            Wears<TurnStartedTriggeredEffectContext>(anuket, "nile_low"), Wears<TurnStartedTriggeredEffectContext>(anuket, "nile_ordered"));
        var water = If(PlayersTurnAgainst<TurnStartedTriggeredEffectContext>(anuket), Do(
            If(low, new ModifyDefensivePoolNode<TurnStartedTriggeredEffectContext>(anuket, StandardCombatIds.BlockDefensivePool,
                N<TurnStartedTriggeredEffectContext>(-LowWaterBlock))),
            If(Wears<TurnStartedTriggeredEffectContext>(anuket, "nile_ordered"),
                new ApplyStatusNode<TurnStartedTriggeredEffectContext>(Applicant, new StatusDefinitionId(HeldEnergy.Id), N<TurnStartedTriggeredEffectContext>(1))),
            If(Wears<TurnStartedTriggeredEffectContext>(anuket, "nile_high"), Afflict<TurnStartedTriggeredEffectContext>(BurdenedId, 1, anuket))));

        // Her own turn ends: from half her health the river drifts away from the middle.
        var level = StacksOn<TurnEndedTriggeredEffectContext>(self, NileLevelId);
        var drift = If(Wears<TurnEndedTriggeredEffectContext>(self, FloodDriftsId), Do(
            If(Lt(level, NileStart), Ebb<TurnEndedTriggeredEffectContext>(self), Flow<TurnEndedTriggeredEffectContext>(self)),
            NameTheLevel<TurnEndedTriggeredEffectContext>(self)));

        yield return Face(NilometerId, "The Nilometer",
            "End your turn with 0 Energy and the Nile rises a step; with Energy left it falls. Each of the 5 levels does something.",
            triggers:
            [
                On("TurnEnded", gauge),
                On("TurnStarted", water),
                On("TurnEnded", drift, StatusTriggerScope.Bearer),
                FromHalf(FloodDriftsId),
            ]);
        yield return Face(NileLevelId, "Nile Level", "1 Drought, 2 Low, 3 Ordered, 4 High, 5 Black Flood.", stacks: true);
        foreach (var l in NileLevels)
        {
            IReadOnlyList<PassiveModifierData> drought =
                [new PassiveModifierData(PassiveModifierPipeline.DamageDealt, PassiveModifierOperation.AddFlat, 3, RestrictDamageKind: DamageKind.Direct)];
            yield return l.Level == 1
                ? Face(l.Id, l.Name, l.Text, StatusPolarity.Buff, modifiers: drought)
                : Face(l.Id, l.Name, l.Text);
        }
        yield return Face(FloodDriftsId, "The Flood Drifts",
            "From half her health the Nile drifts a step away from the middle after each of her turns.");
    }

    // The black flood: the river takes, and falls back to its ordered middle.
    private static EffectProgram<EnemyActionContext> TheRiverTakes(int damage)
    {
        var self = CombatantTargetSelectors.Source;
        return new(Do(
            Hit(Const(damage)),
            Afflict<EnemyActionContext>(EntombedId, 1, self),
            Unmark<EnemyActionContext>(self, NileLevelId),
            Mark<EnemyActionContext>(self, NileLevelId, NileStart),
            NameTheLevel<EnemyActionContext>(self)));
    }
}
