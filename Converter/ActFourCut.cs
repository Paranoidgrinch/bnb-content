using System.Text.Json;
using RogueDeck.Core.Combat;
using RogueDeck.Run;
using RogueDeck.Sandbox.Composition;
using RogueDeck.Scenario.Authoring;
using BnbContent.Converter.Cards;

namespace BnbContent.Converter;

// Act IV after the content cut of 2026-10-30: the new and reworked bodies, written in one place.
//
//   Necropolis of Thieves   the Jackal of the Western Gate guards an ally; the Cursed Loot Bearer pays out in
//                           Gold and a curse; the False-Door Finder hides behind two doors, one of them real.
//   Hall of Two Truths      the Assessor of the Forty-Two names a sin each turn, and a clean turn is a Feather.
//   Elites                  the Curse of the Opened Tomb grows with what you took this act; Ammit counts your
//                           failures and is appeased by Feathers.
//
// FEATHERS are the act's one carried token: a clean turn before the Assessor (or a measure met before Ammit)
// is a Feather on you, a run counter carries them from fight to fight, Ammit eats them, and the Weigher of the
// Unspoken Heart counts up to two of them toward declaring your heart light.
public static partial class ActFour
{
    // ── ids ───────────────────────────────────────────────────────────────────────────────────────────────

    public const string JackalEnemyId = "jackal_of_the_western_gate";
    public const string JackalRuleId = "guards_the_west";
    public const string GuardedId = "guarded_by_the_jackal";

    public const string AssessorEnemyId = "assessor_of_the_forty_two";
    public const string FortyTwoId = "the_forty_two";
    public const string SinDeedsId = "sin_of_deeds";
    public const string SinWorkingsId = "sin_of_workings";
    public const string SinRitesId = "sin_of_rites";
    public const string SinnedThisTurnId = "sinned_this_turn";
    public const string FeatherOfTruthId = "feather_of_truth";

    public const string LootRuleId = "every_object_requires_a_form";
    public const string TombCurseCardId = "tomb_curse";

    public const string FalseDoorsRuleId = "necropolis_passage";
    public const string FalseDoorId = "a_false_door";
    public const string RealDoorId = "the_real_door";
    public const string DoorSlammedId = "door_slammed";
    public const string BehindTheDoorsId = "behind_the_doors";
    public const string FalseDoorEnemyId = "false_door";

    public const string CurseEnemyId = "curse_of_the_opened_tomb";
    public const string TombRemembersId = "the_tomb_remembers";
    public const string TombSpoilsId = "tomb_spoils";
    public const string SpoilsCarriedId = "tomb_spoils_carried";
    public const string ReturnCardId = "return_something_taken";

    public const string AmmitEnemyId = "ammit_devourer_of_the_unworthy";
    public const string AmmitRuleId = "the_devourer_waits";
    public const string UnworthyId = "unworthy";
    public const string AmmitShadowId = "ammits_shadow";

    public const string FeathersFromTheRoadId = "feathers_from_the_road";

    // Counters the fight leaves behind for the run (CombatCounter), and the run's own.
    public static CounterId FeathersEarned => new("feathers_earned");
    public static CounterId FeathersSpent => new("feathers_spent");
    public static CounterId LootClaimed => new("tomb_loot_claimed");
    public static CounterId GoldReturned => new("tomb_gold_returned");
    private static CounterId AmmitFailuresRead => new("ammit_failures_read");
    private static CounterId AmmitMetRead => new("ammit_met_read");
    private static CounterId WeigherFeathersTaken => new("weigher_feathers_taken");

    public static readonly RunCounterId FeathersCarried = new("labyrinth_feathers_carried");
    public static readonly RunCounterId TombGoldTaken = new("labyrinth_tomb_gold_taken");
    public static readonly RunCounterId TombCardsTaken = new("labyrinth_tomb_cards_taken");

    public const int GuardShare = 50;
    public const int LootGold = 45;
    public const int ReturnGold = 30;
    public const int GoldPerSpoil = 40;
    public const int MaxSpoils = 8;
    public const int MaxCarriedFeathers = 6;
    public const int DevourAt = 3;
    public const int DevourDamage = 40;
    public const int FeatherAppeasement = 12;
    public const int WeigherFeathersFromTheRoad = 2;

    private static ICombatantTargetSelector Parties { get; } =
        CombatantTargetSelectors.WithStatus(
            CombatantTargetSelectors.AllAliveCombatants, new StatusDefinitionId(LabyrinthBodyId));

    private static ICombatExpression<TContext, bool> Has<TContext>(ICombatantTargetSelector who, string statusId)
        where TContext : class =>
        new ComparisonExpression<TContext>(
            new CombatantStatusStacksExpression<TContext>(who, new StatusDefinitionId(statusId)),
            ComparisonOperator.Greater, new ConstantExpression<TContext>(0));

    private static ICombatExpression<TContext, bool> Standing<TContext>(ICombatantTargetSelector who)
        where TContext : class =>
        new ComparisonExpression<TContext>(
            new CombatantCurrentHealthExpression<TContext>(who),
            ComparisonOperator.Greater, new ConstantExpression<TContext>(0));

    private static ICombatExpression<TContext, bool> IsPlayersTurn<TContext>() where TContext : class =>
        Has<TContext>(CombatantTargetSelectors.Source, PassiveStatuses.ApplicantId);

    private static StatusTriggerData On<TContext>(
        string trigger, IEffectNode<TContext> body, StatusTriggerScope scope = StatusTriggerScope.Anywhere)
        where TContext : class =>
        new(trigger, JsonSerializer.SerializeToElement(
            new EffectProgram<TContext>(body), CombatJson.CreateOptions<TContext>()), scope);

    private static StatusData Face(
        string id, string name, string description, StatusPolarity polarity = StatusPolarity.Neutral,
        bool stacks = false, IReadOnlyList<PassiveModifierData>? modifiers = null,
        IReadOnlyList<StatusTriggerData>? triggers = null) => new()
        {
            Id = id,
            NameKey = name,
            DescriptionKey = description,
            Polarity = polarity,
            StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
            UsesStacks = stacks,
            Tags = [],
            PassiveModifiers = modifiers ?? [],
            Triggers = triggers ?? [],
        };

    private static ConstantExpression<TContext> N<TContext>(int value) where TContext : class => new(value);

    public static IReadOnlyList<StatusData> CutStatuses() =>
    [
        GuardsTheWest(), Guarded(),
        TheFortyTwo(),
        Face(SinDeedsId, "The Sin: Deeds", "Play a Deed this turn and you are Entombed 1."),
        Face(SinWorkingsId, "The Sin: Workings", "Play a Working this turn and you are Entombed 1."),
        Face(SinRitesId, "The Sin: Rites", "Play a Rite this turn and you are Entombed 1."),
        Face(SinnedThisTurnId, "Sin Recorded", "This turn is not clean: no Feather."),
        Face(FeatherOfTruthId, "Feather", "A turn found true, carried through the act. Ammit is appeased by it; "
            + "the Weigher counts up to 2 of them.", StatusPolarity.Buff, stacks: true),
        LootAndCurse(),
        TwoFalseDoors(), FalseDoor(),
        Face(RealDoorId, "The Real Door", "This is the real door this turn."),
        Face(DoorSlammedId, "Door Slammed", "A false door has already slammed on you this turn."),
        Face(BehindTheDoorsId, "Behind the Doors", "While a door stands, it takes half damage.",
            StatusPolarity.Buff, modifiers:
            [new PassiveModifierData(PassiveModifierPipeline.DamageReceived, PassiveModifierOperation.ScalePercent, 50)]),
        TheTombRemembers(),
        Face(TombSpoilsId, "Spoils", "What you took this act. Each one adds 3 to its blows.",
            StatusPolarity.Buff, stacks: true, modifiers:
            [new PassiveModifierData(PassiveModifierPipeline.DamageDealt, PassiveModifierOperation.AddPerStack, 3)]),
        Face(SpoilsCarriedId, "Spoils Carried", "What you took this act, counted at the door of every fight.",
            stacks: true),
        TheDevourerWaits(),
        Face(UnworthyId, "Unworthy", "Your failures this fight. At 3 Ammit devours: 40 damage, 12 less for "
            + "each Feather she eats.", StatusPolarity.Neutral, stacks: true),
        AmmitsShadow(),
        FeathersFromTheRoad(),
    ];

    public static IReadOnlyList<CardData> CutCards() => [ReturnSomethingTaken(), .. TombCurse()];

    // ── the Jackal of the Western Gate ────────────────────────────────────────────────────────────────────
    //
    // "Each turn it visibly guards one ally and intercepts hits on it." The guarded ally wears the chip; a hit
    // on it lands at half, and the Jackal takes the same again.
    private static StatusData GuardsTheWest()
    {
        var jackal = Bearer(JackalRuleId);
        var ward = CombatantTargetSelectors.FirstTarget(
            CombatantTargetSelectors.WithoutStatus(
                CombatantTargetSelectors.WithoutStatus(Parties, new StatusDefinitionId(JackalRuleId)),
                new StatusDefinitionId(GuardedId)));
        var anyWard = CombatantTargetSelectors.FirstTarget(
            CombatantTargetSelectors.WithoutStatus(Parties, new StatusDefinitionId(JackalRuleId)));

        IEffectNode<TurnStartedTriggeredEffectContext> Clear() =>
            new ForEachTargetEffectNode<TurnStartedTriggeredEffectContext>(Parties,
                new RemoveStatusNode<TurnStartedTriggeredEffectContext>(
                    CombatantTargetSelectors.IterationTarget, new StatusDefinitionId(GuardedId)));

        // A different ally each turn when there is one; the same one when it is the only one left.
        var guard = new ConditionalEffectNode<TurnStartedTriggeredEffectContext>(
            new AndExpression<TurnStartedTriggeredEffectContext>(
                IsPlayersTurn<TurnStartedTriggeredEffectContext>(), Standing<TurnStartedTriggeredEffectContext>(jackal)),
            new ConditionalEffectNode<TurnStartedTriggeredEffectContext>(
                Standing<TurnStartedTriggeredEffectContext>(ward),
                new CausalSequenceEffectNode<TurnStartedTriggeredEffectContext>(
                [
                    new ApplyStatusNode<TurnStartedTriggeredEffectContext>(
                        ward, new StatusDefinitionId(BorderNextMark), N<TurnStartedTriggeredEffectContext>(1)),
                    Clear(),
                    new ForEachTargetEffectNode<TurnStartedTriggeredEffectContext>(
                        CombatantTargetSelectors.WithStatus(Parties, new StatusDefinitionId(BorderNextMark)),
                        new CausalSequenceEffectNode<TurnStartedTriggeredEffectContext>(
                        [
                            new RemoveStatusNode<TurnStartedTriggeredEffectContext>(
                                CombatantTargetSelectors.IterationTarget, new StatusDefinitionId(BorderNextMark)),
                            new ApplyStatusNode<TurnStartedTriggeredEffectContext>(
                                CombatantTargetSelectors.IterationTarget, new StatusDefinitionId(GuardedId),
                                N<TurnStartedTriggeredEffectContext>(1)),
                        ])),
                ]),
                new ApplyStatusNode<TurnStartedTriggeredEffectContext>(
                    anyWard, new StatusDefinitionId(GuardedId), N<TurnStartedTriggeredEffectContext>(1))));

        var fallen = new ConditionalEffectNode<CombatantDownedTriggeredEffectContext>(
            new TargetHasStatusExpression<CombatantDownedTriggeredEffectContext>(
                CombatantTargetSelectors.SourceIncludingDowned, new StatusDefinitionId(JackalRuleId)),
            new ForEachTargetEffectNode<CombatantDownedTriggeredEffectContext>(Parties,
                new RemoveStatusNode<CombatantDownedTriggeredEffectContext>(
                    CombatantTargetSelectors.IterationTarget, new StatusDefinitionId(GuardedId))));

        return Face(JackalRuleId, "Guards the West",
            "Each turn the Jackal guards one ally. A hit on the guarded ally lands at half, and the Jackal "
            + "takes the same again.",
            triggers:
            [
                On("TurnStarted", guard),
                On("Downed", fallen, StatusTriggerScope.Bearer),
            ]);
    }

    // The mark that says where the guard goes next: written before the old guard is cleared, so the choice
    // is made while the old one still counts as "already guarded".
    private const string BorderNextMark = "jackal_moves_to";

    private static StatusData Guarded()
    {
        var jackal = Bearer(JackalRuleId);
        var intercept = new ConditionalEffectNode<DamageReceivedTriggeredEffectContext>(
            Standing<DamageReceivedTriggeredEffectContext>(jackal),
            new DealDamageNode<DamageReceivedTriggeredEffectContext>(
                jackal, new EventAmountExpression<DamageReceivedTriggeredEffectContext>(), ignoresBlock: true,
                kind: DamageKind.DamageOverTime));

        return Face(GuardedId, "Guarded by the Jackal",
            "Hits on this party land at half; the Jackal takes the same again.", StatusPolarity.Buff,
            modifiers:
            [new PassiveModifierData(PassiveModifierPipeline.DamageReceived, PassiveModifierOperation.ScalePercent, GuardShare)],
            triggers: [On("DamageTaken", intercept, StatusTriggerScope.Bearer)]);
    }

    public static StatusData JackalMovesTo() => Face(BorderNextMark, "Guard Moving", "The Jackal is moving here.");

    // ── the Assessor of the Forty-Two ─────────────────────────────────────────────────────────────────────
    //
    // "Each turn it names a forbidden card type, the sin. A clean turn gives you a Feather." The sin is a chip
    // on the Assessor and turns Deed → Working → Rite.
    private static StatusData TheFortyTwo()
    {
        var assessor = Bearer(FortyTwoId);

        IEffectNode<TurnStartedTriggeredEffectContext> Turn(string from, string to) =>
            new CausalSequenceEffectNode<TurnStartedTriggeredEffectContext>(
            [
                new RemoveStatusNode<TurnStartedTriggeredEffectContext>(assessor, new StatusDefinitionId(from)),
                new ApplyStatusNode<TurnStartedTriggeredEffectContext>(
                    assessor, new StatusDefinitionId(to), N<TurnStartedTriggeredEffectContext>(1)),
            ]);

        var name = new ConditionalEffectNode<TurnStartedTriggeredEffectContext>(
            new AndExpression<TurnStartedTriggeredEffectContext>(
                IsPlayersTurn<TurnStartedTriggeredEffectContext>(), Standing<TurnStartedTriggeredEffectContext>(assessor)),
            new CausalSequenceEffectNode<TurnStartedTriggeredEffectContext>(
            [
                new RemoveStatusNode<TurnStartedTriggeredEffectContext>(assessor, new StatusDefinitionId(SinnedThisTurnId)),
                new ConditionalEffectNode<TurnStartedTriggeredEffectContext>(
                    Has<TurnStartedTriggeredEffectContext>(assessor, SinDeedsId),
                    Turn(SinDeedsId, SinWorkingsId),
                    new ConditionalEffectNode<TurnStartedTriggeredEffectContext>(
                        Has<TurnStartedTriggeredEffectContext>(assessor, SinWorkingsId),
                        Turn(SinWorkingsId, SinRitesId),
                        Turn(SinRitesId, SinDeedsId))),
            ]));

        ICombatExpression<CardPlayedTriggeredEffectContext, bool> Sin(string marker, string tag) =>
            new AndExpression<CardPlayedTriggeredEffectContext>(
                Has<CardPlayedTriggeredEffectContext>(assessor, marker),
                new TriggerEventSourceCardHasTagExpression<CardPlayedTriggeredEffectContext>(new TagId(tag)));

        var judged = new ConditionalEffectNode<CardPlayedTriggeredEffectContext>(
            new AndExpression<CardPlayedTriggeredEffectContext>(
                Standing<CardPlayedTriggeredEffectContext>(assessor),
                new OrExpression<CardPlayedTriggeredEffectContext>(
                    Sin(SinDeedsId, CardAuthoring.DeedTag),
                    new OrExpression<CardPlayedTriggeredEffectContext>(
                        Sin(SinWorkingsId, CardAuthoring.WorkingTag), Sin(SinRitesId, CardAuthoring.RiteTag)))),
            new CausalSequenceEffectNode<CardPlayedTriggeredEffectContext>(
            [
                new ApplyStatusNode<CardPlayedTriggeredEffectContext>(
                    Applicant, new StatusDefinitionId(EntombedId), N<CardPlayedTriggeredEffectContext>(1),
                    sourceSelector: assessor),
                new ApplyStatusNode<CardPlayedTriggeredEffectContext>(
                    assessor, new StatusDefinitionId(SinnedThisTurnId), N<CardPlayedTriggeredEffectContext>(1)),
            ]));

        var clean = new ConditionalEffectNode<TurnEndedTriggeredEffectContext>(
            new AndExpression<TurnEndedTriggeredEffectContext>(
                new AndExpression<TurnEndedTriggeredEffectContext>(
                    IsPlayersTurn<TurnEndedTriggeredEffectContext>(), Standing<TurnEndedTriggeredEffectContext>(assessor)),
                new NotExpression<TurnEndedTriggeredEffectContext>(
                    Has<TurnEndedTriggeredEffectContext>(assessor, SinnedThisTurnId))),
            GainAFeather<TurnEndedTriggeredEffectContext>());

        return Face(FortyTwoId, "The Forty-Two",
            "Each turn the Assessor names a sin: Deeds, Workings or Rites. Each sinful card is 1 Entombed. A "
            + "clean turn earns a Feather.",
            triggers: [On("TurnStarted", name), On("CardPlayed", judged), On("TurnEnded", clean)]);
    }

    private static IEffectNode<TContext> GainAFeather<TContext>() where TContext : class =>
        new CausalSequenceEffectNode<TContext>(
        [
            new ApplyStatusNode<TContext>(Applicant, new StatusDefinitionId(FeatherOfTruthId), N<TContext>(1)),
            new SetCombatantCounterNode<TContext>(Applicant, FeathersEarned, N<TContext>(1), relative: true),
        ]);

    // ── the Cursed Loot Bearer ────────────────────────────────────────────────────────────────────────────
    //
    // REWORKED: "killing it gives Gold plus a curse card". The fight writes the claim down, and a run program
    // pays it after the fight (combat cannot reach run Gold or the deck).
    private static StatusData LootAndCurse()
    {
        var claimed = new ConditionalEffectNode<CombatantDownedTriggeredEffectContext>(
            new TargetHasStatusExpression<CombatantDownedTriggeredEffectContext>(
                CombatantTargetSelectors.SourceIncludingDowned, new StatusDefinitionId(LootRuleId)),
            new ForEachTargetEffectNode<CombatantDownedTriggeredEffectContext>(
                CombatantTargetSelectors.AllEnemiesOfSourceWithStatus(new StatusDefinitionId(PassiveStatuses.ApplicantId)),
                new SetCombatantCounterNode<CombatantDownedTriggeredEffectContext>(
                    CombatantTargetSelectors.IterationTarget, LootClaimed,
                    N<CombatantDownedTriggeredEffectContext>(1), relative: true)));

        return Face(LootRuleId, "Cursed Loot",
            $"Kill it and the loot is yours: {LootGold} Gold after the fight — and a Tomb Curse joins your deck.",
            triggers: [On("Downed", claimed, StatusTriggerScope.Bearer)]);
    }

    // The curse the loot carries: unplayable, and it buries you a little whenever it is still in hand.
    private static readonly CardAuthoring.BnbCard TombCurseCard = new(
        TombCurseCardId, "Tomb Curse", CardAuthoring.JunkTag, 0,
        "Unplayable. Still in hand at the end of your turn: 1 Entombed.",
        new CombatNodeModel("gainBlock", "source", CombatAmountSpec.FromConst(0)),
        Rarity: "junk", Tags: [CardAuthoring.UnplayableTag, "curse", TombCurseCardId]);

    private static IReadOnlyList<CardData> TombCurse() =>
    [
        TombCurseCard.Compile() with
        {
            LifecyclePrograms = new Dictionary<CardLifecycleTrigger, EffectProgram<CardLifecycleContext>>
            {
                [CardLifecycleTrigger.TurnEndInHand] = new(new ApplyStatusNode<CardLifecycleContext>(
                    CombatantTargetSelectors.Source, new StatusDefinitionId(EntombedId),
                    new ConstantExpression<CardLifecycleContext>(1))),
            },
        },
    ];

    // ── the False-Door Finder ─────────────────────────────────────────────────────────────────────────────
    //
    // REWORKED: "two false doors, only one real, and you choose which to attack". The Finder raises two doors
    // at the start of the fight and stands behind them (half damage). Each turn one door is the real one — you
    // cannot tell which. A blow on the real door passes through to the Finder; a blow on a false one slams it
    // on you: 1 Entombed, once a turn.
    private static StatusData TwoFalseDoors()
    {
        var finder = Bearer(FalseDoorsRuleId);
        var doors = CombatantTargetSelectors.WithStatus(
            CombatantTargetSelectors.AllAliveCombatants, new StatusDefinitionId(FalseDoorId));
        var anyDoor = new ComparisonExpression<TurnStartedTriggeredEffectContext>(
            new CountTargetsExpression<TurnStartedTriggeredEffectContext>(doors),
            ComparisonOperator.Greater, N<TurnStartedTriggeredEffectContext>(0));

        IEffectNode<TurnStartedTriggeredEffectContext> Door() =>
            new SummonCombatantNode<TurnStartedTriggeredEffectContext>(
                StandardCombatIds.EnemyTeam, N<TurnStartedTriggeredEffectContext>(DoorHealth),
                new CombatantDefinitionId(FalseDoorEnemyId), "False Door",
                startingStatuses: [new StatusGrant(new StatusDefinitionId(FalseDoorId), 1)]);

        var turn = new ConditionalEffectNode<TurnStartedTriggeredEffectContext>(
            new AndExpression<TurnStartedTriggeredEffectContext>(
                IsPlayersTurn<TurnStartedTriggeredEffectContext>(), Standing<TurnStartedTriggeredEffectContext>(finder)),
            new CausalSequenceEffectNode<TurnStartedTriggeredEffectContext>(
            [
                // The first turn: the doors go up.
                new ConditionalEffectNode<TurnStartedTriggeredEffectContext>(
                    new ComparisonExpression<TurnStartedTriggeredEffectContext>(
                        new RoundNumberExpression<TurnStartedTriggeredEffectContext>(),
                        ComparisonOperator.Equal, N<TurnStartedTriggeredEffectContext>(1)),
                    new CausalSequenceEffectNode<TurnStartedTriggeredEffectContext>(
                    [
                        Door(), Door(),
                        new ApplyStatusNode<TurnStartedTriggeredEffectContext>(
                            finder, new StatusDefinitionId(BehindTheDoorsId), N<TurnStartedTriggeredEffectContext>(1)),
                    ])),
                new RemoveStatusNode<TurnStartedTriggeredEffectContext>(finder, new StatusDefinitionId(DoorSlammedId)),
                new ForEachTargetEffectNode<TurnStartedTriggeredEffectContext>(doors,
                    new RemoveStatusNode<TurnStartedTriggeredEffectContext>(
                        CombatantTargetSelectors.IterationTarget, new StatusDefinitionId(RealDoorId))),
                new RandomTargetSelectionNode<TurnStartedTriggeredEffectContext>(
                    doors, N<TurnStartedTriggeredEffectContext>(1),
                    new ApplyStatusNode<TurnStartedTriggeredEffectContext>(
                        CombatantTargetSelectors.IterationTarget, new StatusDefinitionId(RealDoorId),
                        N<TurnStartedTriggeredEffectContext>(1))),
                // With both doors broken the Finder has nothing to stand behind.
                new ConditionalEffectNode<TurnStartedTriggeredEffectContext>(
                    new NotExpression<TurnStartedTriggeredEffectContext>(anyDoor),
                    new RemoveStatusNode<TurnStartedTriggeredEffectContext>(
                        finder, new StatusDefinitionId(BehindTheDoorsId))),
            ]));

        // When the Finder falls, its doors fall with it.
        var fallen = new ConditionalEffectNode<CombatantDownedTriggeredEffectContext>(
            new TargetHasStatusExpression<CombatantDownedTriggeredEffectContext>(
                CombatantTargetSelectors.SourceIncludingDowned, new StatusDefinitionId(FalseDoorsRuleId)),
            new ForEachTargetEffectNode<CombatantDownedTriggeredEffectContext>(
                CombatantTargetSelectors.WithStatus(
                    CombatantTargetSelectors.AllAliveCombatants, new StatusDefinitionId(FalseDoorId)),
                new SetHealthNode<CombatantDownedTriggeredEffectContext>(
                    CombatantTargetSelectors.IterationTarget, N<CombatantDownedTriggeredEffectContext>(0))));

        return Face(FalseDoorsRuleId, "Two False Doors",
            "It hides behind two doors, half damage while one stands. Each turn one door is real: hit it and "
            + "the Finder takes the blow. Hit the other: 1 Entombed.",
            triggers: [On("TurnStarted", turn), On("Downed", fallen, StatusTriggerScope.Bearer)]);
    }

    private const int DoorHealth = 30;

    private static StatusData FalseDoor()
    {
        var finder = Bearer(FalseDoorsRuleId);
        var me = CombatantTargetSelectors.EventTarget;
        var struck = new ConditionalEffectNode<DamageReceivedTriggeredEffectContext>(
            Standing<DamageReceivedTriggeredEffectContext>(finder),
            new ConditionalEffectNode<DamageReceivedTriggeredEffectContext>(
                Has<DamageReceivedTriggeredEffectContext>(me, RealDoorId),
                new DealDamageNode<DamageReceivedTriggeredEffectContext>(
                    finder, new EventAmountExpression<DamageReceivedTriggeredEffectContext>(), ignoresBlock: true,
                    kind: DamageKind.DamageOverTime),
                new ConditionalEffectNode<DamageReceivedTriggeredEffectContext>(
                    new NotExpression<DamageReceivedTriggeredEffectContext>(
                        Has<DamageReceivedTriggeredEffectContext>(finder, DoorSlammedId)),
                    new CausalSequenceEffectNode<DamageReceivedTriggeredEffectContext>(
                    [
                        new ApplyStatusNode<DamageReceivedTriggeredEffectContext>(
                            Applicant, new StatusDefinitionId(EntombedId), N<DamageReceivedTriggeredEffectContext>(1)),
                        new ApplyStatusNode<DamageReceivedTriggeredEffectContext>(
                            finder, new StatusDefinitionId(DoorSlammedId), N<DamageReceivedTriggeredEffectContext>(1)),
                    ]))));

        return Face(FalseDoorId, "False Door",
            "One of the two doors is real, and it changes every turn. Real: the Finder takes your blow. "
            + "False: it slams on you, 1 Entombed.",
            triggers: [On("DamageTaken", struck, StatusTriggerScope.Bearer)]);
    }

    // ── the Curse of the Opened Tomb ──────────────────────────────────────────────────────────────────────
    //
    // NEW: "it grows with the Gold and cards you took this act. Returning something weakens it." What you took
    // is counted by run programs and walked into every fight as Spoils Carried; the Curse takes them over as
    // its own Spoils (3 more on each blow apiece). Each turn it hands you the means to give something back.
    private static StatusData TheTombRemembers()
    {
        var curse = Bearer(TombRemembersId);
        var carried = new CombatantStatusStacksExpression<CardsDrawnTriggeredEffectContext>(
            Applicant, new StatusDefinitionId(SpoilsCarriedId));

        var take = new ConditionalEffectNode<CardsDrawnTriggeredEffectContext>(
            new AndExpression<CardsDrawnTriggeredEffectContext>(
                Standing<CardsDrawnTriggeredEffectContext>(curse),
                Has<CardsDrawnTriggeredEffectContext>(Applicant, SpoilsCarriedId)),
            new CausalSequenceEffectNode<CardsDrawnTriggeredEffectContext>(
            [
                new ApplyStatusNode<CardsDrawnTriggeredEffectContext>(
                    curse, new StatusDefinitionId(TombSpoilsId), carried),
                new RemoveStatusNode<CardsDrawnTriggeredEffectContext>(Applicant, new StatusDefinitionId(SpoilsCarriedId)),
            ]));

        var offer = new ConditionalEffectNode<CardsDrawnTriggeredEffectContext>(
            new AndExpression<CardsDrawnTriggeredEffectContext>(
                Standing<CardsDrawnTriggeredEffectContext>(curse),
                new ComparisonExpression<CardsDrawnTriggeredEffectContext>(
                    new CombatantZoneCardCountExpression<CardsDrawnTriggeredEffectContext>(
                        Applicant, CardZone.Hand, new TagId(ReturnCardId)),
                    ComparisonOperator.Equal, N<CardsDrawnTriggeredEffectContext>(0))),
            new CreateCardInstanceNode<CardsDrawnTriggeredEffectContext>(
                Applicant, new CardDefinitionId(ReturnCardId), CardZone.Hand, N<CardsDrawnTriggeredEffectContext>(1)));

        return Face(TombRemembersId, "The Tomb Remembers",
            "It grows with what you took this act: 1 Spoil per card and per 40 Gold. Each turn you may give "
            + "something back.",
            triggers: [On("CardsDrawn", new CausalSequenceEffectNode<CardsDrawnTriggeredEffectContext>([take, offer]))]);
    }

    private static CardData ReturnSomethingTaken()
    {
        var curse = Bearer(TombRemembersId);
        IEffectNode<CardPlayContext> Weaken(int spoils, int health) =>
            new CausalSequenceEffectNode<CardPlayContext>(
            [
                new ModifyStatusStacksNode<CardPlayContext>(curse, new StatusDefinitionId(TombSpoilsId), N<CardPlayContext>(-spoils)),
                new SetHealthNode<CardPlayContext>(curse,
                    new SubtractExpression<CardPlayContext>(
                        new CombatantCurrentHealthExpression<CardPlayContext>(curse), N<CardPlayContext>(health))),
            ]);

        return new CardData
        {
            Id = ReturnCardId,
            NameKey = "Return Something Taken",
            DescriptionKey = $"Choose: give back {ReturnGold} Gold (paid after the fight) — it loses 2 Spoils and "
                + "15 HP. Or sacrifice a card from your hand — it loses 1 Spoil and 10 HP.",
            Costs = [],
            Tags = [new TagId(ReturnCardId), new TagId("form")],
            Program = new EffectProgram<CardPlayContext>(
                new ChooseOptionsNode<CardPlayContext>(
                    [
                        new CausalSequenceEffectNode<CardPlayContext>(
                        [
                            new SetCombatantCounterNode<CardPlayContext>(
                                CombatantTargetSelectors.Source, GoldReturned, N<CardPlayContext>(1), relative: true),
                            Weaken(2, 15),
                        ]),
                        new ConditionalEffectNode<CardPlayContext>(
                            new ComparisonExpression<CardPlayContext>(
                                new CombatantZoneCardCountExpression<CardPlayContext>(
                                    CombatantTargetSelectors.Source, CardZone.Hand),
                                ComparisonOperator.Greater, N<CardPlayContext>(1)),
                            new CausalSequenceEffectNode<CardPlayContext>(
                            [
                                new MoveCardToZoneNode<CardPlayContext>(
                                    CombatantTargetSelectors.Source,
                                    new ChosenCardInZoneExpression<CardPlayContext>(
                                        CardZone.Hand, "sacrifice a card", excludeTag: new TagId(ReturnCardId)),
                                    CardZone.ExhaustPile),
                                Weaken(1, 10),
                            ])),
                    ],
                    [$"give back {ReturnGold} gold", "sacrifice a card"],
                    count: 1, purpose: "return something taken")),
            PlayedCardDestinationZone = CardZone.ExhaustPile,
            TurnEndHandDestinationZone = CardZone.ExhaustPile,
        };
    }

    // ── Ammit, Devourer of the Unworthy ───────────────────────────────────────────────────────────────────
    //
    // NEW: "counts your failures this fight (missed measures). At 3 she devours, a huge hit. Feathers appease
    // her." Her own pressure intent raises the measure; at her turn start she reads the act's tallies since
    // she last looked: each miss is Unworthy, each measure met is a Feather. At 3 Unworthy she devours.
    private static StatusData TheDevourerWaits()
    {
        var ammit = CombatantTargetSelectors.Source;
        var failed = SinceLastLooked<TurnStartedTriggeredEffectContext>(ammit, MeasuresFailed, AmmitFailuresRead);
        var met = SinceLastLooked<TurnStartedTriggeredEffectContext>(ammit, MeasuresMet, AmmitMetRead);
        var feathers = new CombatantStatusStacksExpression<TurnStartedTriggeredEffectContext>(
            Applicant, new StatusDefinitionId(FeatherOfTruthId));
        var eaten = new MinExpression<TurnStartedTriggeredEffectContext>(feathers, N<TurnStartedTriggeredEffectContext>(3));

        var read = new CausalSequenceEffectNode<TurnStartedTriggeredEffectContext>(
        [
            new ConditionalEffectNode<TurnStartedTriggeredEffectContext>(
                new ComparisonExpression<TurnStartedTriggeredEffectContext>(
                    failed, ComparisonOperator.Greater, N<TurnStartedTriggeredEffectContext>(0)),
                new ApplyStatusNode<TurnStartedTriggeredEffectContext>(ammit, new StatusDefinitionId(UnworthyId), failed)),
            new ConditionalEffectNode<TurnStartedTriggeredEffectContext>(
                new ComparisonExpression<TurnStartedTriggeredEffectContext>(
                    met, ComparisonOperator.Greater, N<TurnStartedTriggeredEffectContext>(0)),
                GainAFeather<TurnStartedTriggeredEffectContext>()),
            MoveTheBookmark<TurnStartedTriggeredEffectContext>(ammit, MeasuresFailed, AmmitFailuresRead),
            MoveTheBookmark<TurnStartedTriggeredEffectContext>(ammit, MeasuresMet, AmmitMetRead),
            new ConditionalEffectNode<TurnStartedTriggeredEffectContext>(
                new ComparisonExpression<TurnStartedTriggeredEffectContext>(
                    new CombatantStatusStacksExpression<TurnStartedTriggeredEffectContext>(ammit, new StatusDefinitionId(UnworthyId)),
                    ComparisonOperator.GreaterOrEqual, N<TurnStartedTriggeredEffectContext>(DevourAt)),
                new CausalSequenceEffectNode<TurnStartedTriggeredEffectContext>(
                [
                    new DealDamageNode<TurnStartedTriggeredEffectContext>(
                        Applicant,
                        new MaxExpression<TurnStartedTriggeredEffectContext>(
                            N<TurnStartedTriggeredEffectContext>(4),
                            new SubtractExpression<TurnStartedTriggeredEffectContext>(
                                N<TurnStartedTriggeredEffectContext>(DevourDamage),
                                new MultiplyExpression<TurnStartedTriggeredEffectContext>(
                                    eaten, N<TurnStartedTriggeredEffectContext>(FeatherAppeasement))))),
                    new SetCombatantCounterNode<TurnStartedTriggeredEffectContext>(
                        Applicant, FeathersSpent, eaten, relative: true),
                    new ModifyStatusStacksNode<TurnStartedTriggeredEffectContext>(
                        Applicant, new StatusDefinitionId(FeatherOfTruthId),
                        new NegateExpression<TurnStartedTriggeredEffectContext>(eaten)),
                    new RemoveStatusNode<TurnStartedTriggeredEffectContext>(ammit, new StatusDefinitionId(UnworthyId)),
                ])),
        ]);

        return Face(AmmitRuleId, "The Devourer Waits",
            "Each measure you miss is Unworthy; each one met is a Feather. At 3 Unworthy she devours: 40, and "
            + "12 less for each Feather she eats (up to 3).",
            triggers: [On("TurnStarted", read, StatusTriggerScope.Bearer)]);
    }

    // Ammit's Shadow, the event fight: a body with no actions of its own, which bites at the start of each of
    // its turns and is counted like her.
    private static StatusData AmmitsShadow()
    {
        var bite = new DealDamageNode<TurnStartedTriggeredEffectContext>(
            Applicant, N<TurnStartedTriggeredEffectContext>(9));
        return Face(AmmitShadowId, "Ammit's Shadow",
            "It bites for 9 at the start of each of its turns.",
            triggers: [On("TurnStarted", bite, StatusTriggerScope.Bearer)]);
    }

    // ── the Weigher counts the road ───────────────────────────────────────────────────────────────────────
    //
    // "Feathers should matter for the Weigher": up to two of the Feathers you carry in are laid on his scale
    // at the first hand, toward declaring your heart light.
    private static StatusData FeathersFromTheRoad()
    {
        var weigher = Bearer(FeathersFromTheRoadId);
        var carried = new MinExpression<CardsDrawnTriggeredEffectContext>(
            new CombatantStatusStacksExpression<CardsDrawnTriggeredEffectContext>(
                Applicant, new StatusDefinitionId(FeatherOfTruthId)),
            N<CardsDrawnTriggeredEffectContext>(WeigherFeathersFromTheRoad));

        var lay = new ConditionalEffectNode<CardsDrawnTriggeredEffectContext>(
            new AndExpression<CardsDrawnTriggeredEffectContext>(
                new ComparisonExpression<CardsDrawnTriggeredEffectContext>(
                    new CombatantCounterExpression<CardsDrawnTriggeredEffectContext>(weigher, WeigherFeathersTaken),
                    ComparisonOperator.Equal, N<CardsDrawnTriggeredEffectContext>(0)),
                Has<CardsDrawnTriggeredEffectContext>(Applicant, FeatherOfTruthId)),
            new CausalSequenceEffectNode<CardsDrawnTriggeredEffectContext>(
            [
                new SetCombatantCounterNode<CardsDrawnTriggeredEffectContext>(
                    weigher, WeigherFeathersTaken, N<CardsDrawnTriggeredEffectContext>(1), relative: false),
                new ApplyStatusNode<CardsDrawnTriggeredEffectContext>(
                    weigher, new StatusDefinitionId(FeatherId), carried, sourceSelector: weigher),
                new SetCombatantCounterNode<CardsDrawnTriggeredEffectContext>(Applicant, FeathersSpent, carried, relative: true),
                new ModifyStatusStacksNode<CardsDrawnTriggeredEffectContext>(
                    Applicant, new StatusDefinitionId(FeatherOfTruthId),
                    new NegateExpression<CardsDrawnTriggeredEffectContext>(carried)),
            ]));

        return Face(FeathersFromTheRoadId, "Feathers From the Road",
            "At your first hand, up to 2 Feathers you carry are laid on his scale.",
            triggers: [On("CardsDrawn", lay)]);
    }

    // ── the run's side ────────────────────────────────────────────────────────────────────────────────────

    public const string FeatherLedgerProgramId = "labyrinth_feather_ledger";
    public const string TombLedgerProgramId = "labyrinth_tomb_ledger";

    // Installed with the act: what the fights wrote down is carried, and every combat door opens with it.
    public static IReadOnlyDictionary<string, ITriggeredRunEffectDefinition> CutPrograms() =>
        new Dictionary<string, ITriggeredRunEffectDefinition>
        {
            [FeatherLedgerProgramId] = RunPrograms.On<CombatResolvedRunEvent>(
            [
                RunEffectTemplates.ChangeCounter(FeathersCarried, RunExpr.Subtract(
                    RunEventValues.CombatCounter(FeathersEarned.ToString()),
                    RunEventValues.CombatCounter(FeathersSpent.ToString()))),
                RunEffectTemplates.GainResource(StandardRunIds.Gold, RunExpr.Subtract(
                    RunExpr.Multiply(RunEventValues.CombatCounter(LootClaimed.ToString()), RunExpr.Const(LootGold)),
                    RunExpr.Multiply(RunEventValues.CombatCounter(GoldReturned.ToString()), RunExpr.Const(ReturnGold)))),
            ]),
            [TombLedgerProgramId + "_gold"] = RunPrograms.When<ResourceChangedRunEvent>(
                RunExpr.And(
                    RunEventValues.ResourceIs(StandardRunIds.Gold),
                    new RunComparisonExpression(RunEventValues.ResourceDelta, RunComparisonOperator.GreaterThan, RunExpr.Const(0))),
                RunEffectTemplates.ChangeCounter(TombGoldTaken, RunEventValues.ResourceDelta)),
            [TombLedgerProgramId + "_cards"] = RunPrograms.On<CardAddedToDeckRunEvent>(
                [RunEffectTemplates.ChangeCounter(TombCardsTaken, RunExpr.Const(1))]),
            [TombLedgerProgramId + "_curse"] = RunPrograms.When<CombatResolvedRunEvent>(
                new RunComparisonExpression(
                    RunEventValues.CombatCounter(LootClaimed.ToString()), RunComparisonOperator.GreaterThan, RunExpr.Const(0)),
                RunEffectTemplates.Literal(new AddCardToDeckRunEffect(new CardDefinitionId(TombCurseCardId)))),
            [TombLedgerProgramId + "_doors"] = DoorsCarry(),
        };

    public static IEnumerable<string> CutProgramIds() =>
    [
        FeatherLedgerProgramId, TombLedgerProgramId + "_gold", TombLedgerProgramId + "_cards",
        TombLedgerProgramId + "_curse", TombLedgerProgramId + "_doors",
    ];

    // Every combat door opens with what you carry: your Feathers, and what the tomb would count as taken.
    private static ITriggeredRunEffectDefinition DoorsCarry()
    {
        var spoils = RunExpr.Min(
            RunExpr.Add(RunExpr.Divide(RunExpr.Counter(TombGoldTaken), RunExpr.Const(GoldPerSpoil)),
                RunExpr.Counter(TombCardsTaken)),
            RunExpr.Const(MaxSpoils));
        var feathers = RunExpr.Min(RunExpr.Counter(FeathersCarried), RunExpr.Const(MaxCarriedFeathers));

        IRunEffectTemplate Carry(IRunExpression<int> amount, string status, int k) =>
            RunEffectTemplates.Literal(new ConditionalRunEffect(
                RunExpr.Equal(amount, RunExpr.Const(k)),
                [Openings.NextCombat(new CombatNodeModel("applyStatus", "source", CombatAmountSpec.FromConst(k),
                    StatusId: status))],
                []));

        return RunPrograms.When<NodeEnteredRunEvent>(
            new EventBoolValueExpression(RunEventFields.NodeIsCombat),
            [
                .. Enumerable.Range(1, MaxCarriedFeathers).Select(k => Carry(feathers, FeatherOfTruthId, k)),
                .. Enumerable.Range(1, MaxSpoils).Select(k => Carry(spoils, SpoilsCarriedId, k)),
            ]);
    }
}
