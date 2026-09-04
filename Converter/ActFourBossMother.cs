using System.Text.Json;
using RogueDeck.Core.Combat;
using RogueDeck.Sandbox.Composition;
using RogueDeck.Scenario.Authoring;

namespace BnbContent.Converter;

// Act IV, boss — The Mother of Natron and Resin. An ancient priestess among open funerary vessels, who
// speaks about the player exclusively in the past tense.
//
// She never stops you shedding an affliction. She KEEPS it. Every negative status that leaves the player —
// faded, cleansed, spent, taken off any way at all — goes into the next empty VESSEL, and four of them full
// is UNSEAL THE VESSELS: everything stored comes back at one stack apiece, with two Embalmed on top.
//
// The counterplay is not to stop the storing. It is to decide what the jars are allowed to hold:
//
//   WASH A VESSEL   once a turn, 1 Energy, empty one jar you choose — and take 1 Embalmed for it.
//
// Which is the whole bargain in one line: preservation later, or preservation now. Washing the jar that
// holds the Entombed and leaving the one that holds a single sheet of Paperwork is the fight.
//
// Fill all four (or reach 305) and THREE JARS ARE ENOUGH: the shelf shortens, the cycle comes round faster,
// and Unseal the Three also takes 12 straight out of you. Below 90 she announces FINISH THE PREPARATION, and
// what that comes to is written on her shelf: 34, and 3 more for every jar still standing.
public static partial class ActFour
{
    public const string MotherEnemyId = "mother_of_natron_and_resin";

    public const string NatronAndResinId = "natron_and_resin";
    public const string VesselsFilledId = "vessels_filled";
    public const string ThreeJarsId = "three_jars_are_enough";
    public const string LastPreparationId = "finish_the_preparation_announced";
    public const string VesselsFullId = "the_vessels_are_full";

    public const string VesselTag = "natron_vessel";

    public const int JarsPhaseOne = 4;
    public const int JarsPhaseTwo = 3;
    private const int ThreeJarsAt = 305;
    private const int PreparationAt = 90;
    private const int UnsealEmbalmed = 2;
    private const int ThreeJarsBlock = 14;
    private const int UnsealThreeLoss = 12;
    private const int PreparationBase = 34;
    private const int PreparationPerJar = 3;
    private const int PreparationCap = 43;

    // Whether this turn's washing is spent, and the two once-per-combat gates.
    public static CounterId WashSpent => new("vessel_wash_spent");
    public static CounterId JarsTaken => new("three_jars_taken");
    public static CounterId PreparationTaken => new("preparation_taken");

    // What the jars can hold. A vessel is a face on HER carrying how many of that affliction are stored, and
    // `vessels_filled` is the shelf's own total — one number a telegraph and an intent rule can both read,
    // which is what the Architect's monument taught: a rule and a face that disagree are a bug the player
    // watches happen.
    private static readonly (string Status, string Vessel, string Card, string Name)[] MotherVessels =
    [
        (WeighedId, "vessel_of_the_weighed", "wash_the_weighed_vessel", "Weighed"),
        (BurdenedId, "vessel_of_the_burdened", "wash_the_burdened_vessel", "Burdened"),
        (EntombedId, "vessel_of_the_entombed", "wash_the_entombed_vessel", "Entombed"),
        (InscribedId, "vessel_of_the_inscribed", "wash_the_inscribed_vessel", "Inscribed"),
        (Cards.Keywords.Paperwork, "vessel_of_the_paperwork", "wash_the_paperwork_vessel", "Paperwork"),
        (Cards.Keywords.Doubt, "vessel_of_the_doubt", "wash_the_doubt_vessel", "Doubt"),
    ];

    public static EffectProgram<EnemyActionContext>? MotherIntent(string enemyId, string intentId) =>
        $"{enemyId}.{intentId}" switch
        {
            "mother_of_natron_and_resin.pack_with_natron" => ByJars(
                I: Seq(Debuff(EmbalmedId, 2)),
                II: Seq(Debuff(EmbalmedId, 2), Debuff(WeighedId, 2))),
            "mother_of_natron_and_resin.black_resin" => ByJars(
                I: Seq(Debuff("poison", 3)),
                II: Seq(Debuff("poison", 4))),
            "mother_of_natron_and_resin.bind_the_limbs" => ByJars(
                I: Seq(Debuff(BurdenedId, 1), Debuff(EntombedId, 1)),
                II: Seq(Debuff(BurdenedId, 1), Debuff(EntombedId, 2))),
            "mother_of_natron_and_resin.name_the_body" => ByJars(
                I: Seq(Debuff(InscribedId, 1), Hit(14)),
                II: Seq(Debuff(InscribedId, 2), Hit(16))),
            "mother_of_natron_and_resin.funerary_hook" => ByJars(
                I: Seq(Hit(14), Hit(14)),
                II: Seq(Hit(16), Hit(16))),
            "mother_of_natron_and_resin.you_have_already_died" => ByJars(
                I: Seq(Debuff(Cards.Keywords.Doubt, 1), Debuff("panic", 1), Hit(16)),
                II: Seq(Debuff(Cards.Keywords.Doubt, 1), Debuff("panic", 1), Hit(19))),
            "mother_of_natron_and_resin.resin_over_the_mouth" => ByJars(
                I: Seq(Guard(26), Debuff(EmbalmedId, 1)),
                II: Seq(Guard(28), Debuff(EmbalmedId, 1))),
            "mother_of_natron_and_resin.unseal_the_vessels" => UnsealTheVessels(),
            "mother_of_natron_and_resin.finish_the_preparation" => FinishThePreparation(),
            _ => null,
        };

    public static IReadOnlyList<StatusData> MotherStatuses() =>
    [
        TheNatronAndResin(),
        TheShelf(),
        ThreeJarsAreEnough(),
        TheLastPreparation(),
        TheVesselsAreFull(),
        .. MotherVessels.Select(v => Vessel(v.Vessel, v.Name)),
    ];

    public static IReadOnlyList<CardData> MotherWashCards() =>
        [.. MotherVessels.Select(v => WashCard(v.Card, v.Name, v.Vessel))];

    // ── the shelf ─────────────────────────────────────────────────────────────────────────────────────────

    private static StatusData Vessel(string id, string kind) => new()
    {
        Id = id,
        NameKey = $"Vessel: {kind}",
        DescriptionKey =
            $"{kind} taken off you and kept. It comes back one stack at a time when the vessels are "
            + "unsealed — unless you wash the jar first.",
        Polarity = StatusPolarity.Neutral,
        StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
        UsesStacks = true,
        Tags = [],
        PassiveModifiers = [],
        Triggers = [],
    };

    private static StatusData TheShelf() => new()
    {
        Id = VesselsFilledId,
        NameKey = "Vessels",
        DescriptionKey =
            "How much of the shelf is full. Four full and she unseals them; three, once three jars are "
            + "enough.",
        Polarity = StatusPolarity.Neutral,
        StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
        UsesStacks = true,
        Tags = [],
        PassiveModifiers = [],
        Triggers = [],
    };

    private static StatusData ThreeJarsAreEnough() => new()
    {
        Id = ThreeJarsId,
        NameKey = "Three Jars Are Enough",
        DescriptionKey =
            "The shelf is shorter and the cycle is faster: three full and she unseals them, and the unsealing "
            + "takes 12 straight out of you.",
        Polarity = StatusPolarity.Neutral,
        StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
        UsesStacks = false,
        Tags = [],
        PassiveModifiers = [],
        Triggers = [],
    };

    // The queued unsealing, and the whole of the response turn the master promises. The shelf is judged
    // ONCE, at the start of the player's turn, and what the judgment says is written on her where the
    // telegraph is: fill the shelf and you are told, and you have a turn to wash a jar and take it back.
    private static StatusData TheVesselsAreFull() => new()
    {
        Id = VesselsFullId,
        NameKey = "The Vessels Are Full",
        DescriptionKey =
            "She will unseal them at the end of this turn. Wash one out and there is nothing to unseal.",
        Polarity = StatusPolarity.Neutral,
        StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
        UsesStacks = false,
        Tags = [],
        PassiveModifiers = [],
        Triggers = [],
    };

    private static StatusData TheLastPreparation() => new()
    {
        Id = LastPreparationId,
        NameKey = "Finish the Preparation",
        DescriptionKey = "34, and 3 more for every jar still standing. The jars are not emptied by it.",
        Polarity = StatusPolarity.Neutral,
        StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
        UsesStacks = false,
        Tags = [],
        PassiveModifiers = [],
        Triggers = [],
    };

    // ── the rule ──────────────────────────────────────────────────────────────────────────────────────────

    public static StatusData TheNatronAndResin() => new()
    {
        Id = NatronAndResinId,
        NameKey = "Natron and Resin",
        DescriptionKey =
            "Every affliction that leaves you is kept in the next empty vessel. Fill the shelf and she gives "
            + "them all back. Once a turn, 1 Energy, you may wash one jar out — and be preserved for it.",
        Polarity = StatusPolarity.Neutral,
        StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
        UsesStacks = false,
        Tags = [],
        PassiveModifiers = [],
        Triggers =
        [
            // A stack lost is an EXPIRY and a status taken off outright is a REMOVAL: the engine reports the
            // two through different events, and a shelf that listens for only one of them is a shelf that
            // never fills. Both, then, and the same program behind each.
            Trigger(StoreIt<StatusRemovedTriggeredEffectContext>(),
                nameof(TriggerEvent.StatusRemoved), StatusTriggerScope.Anywhere),
            Trigger(StoreIt<StatusExpiredTriggeredEffectContext>(),
                nameof(TriggerEvent.StatusExpired), StatusTriggerScope.Anywhere),
            Trigger(OpenTheJars(), nameof(TriggerEvent.TurnStarted), StatusTriggerScope.Anywhere),
            Trigger(TheEmbalmingFailsafes(), nameof(TriggerEvent.DamageTaken)),
        ],
    };

    // What leaves the player is kept. Which affliction it was is asked of the EVENT — the only way a rule
    // can tell one status' movement from another's without one counter per status watched — and the shelf
    // takes at most one stack per removal, and nothing at all once it is full.
    private static EffectProgram<TContext> StoreIt<TContext>() where TContext : class
    {
        var mother = Bearer(NatronAndResinId);
        var filled = new CombatantStatusStacksExpression<TContext>(
            mother, new StatusDefinitionId(VesselsFilledId));

        ICombatExpression<TContext, bool> Room(bool shortShelf) =>
            new ComparisonExpression<TContext>(
                filled, ComparisonOperator.Less,
                new ConstantExpression<TContext>(shortShelf ? JarsPhaseTwo : JarsPhaseOne));

        var wearsThreeJars = new TargetHasStatusExpression<TContext>(
            mother, new StatusDefinitionId(ThreeJarsId));

        IEffectNode<TContext> Keep(string vesselId) =>
            new CausalSequenceEffectNode<TContext>(
            [
                new ApplyStatusNode<TContext>(
                    mother, new StatusDefinitionId(vesselId),
                    new ConstantExpression<TContext>(1), sourceSelector: mother),
                new ApplyStatusNode<TContext>(
                    mother, new StatusDefinitionId(VesselsFilledId),
                    new ConstantExpression<TContext>(1), sourceSelector: mother),
            ]);

        // Which jar, decided by which status the event is about. The innermost `otherwise` is nothing at all:
        // an affliction this act does not keep passes straight through, and must not move the shelf's total.
        IEffectNode<TContext> Sort(int index) =>
            index >= MotherVessels.Length
                ? new NoOpEffectNode<TContext>()
                : new ConditionalEffectNode<TContext>(
                    new TriggerEventStatusIsExpression<TContext>(
                        new StatusDefinitionId(MotherVessels[index].Status)),
                    Keep(MotherVessels[index].Vessel),
                    Sort(index + 1));

        return new EffectProgram<TContext>(
            new ConditionalEffectNode<TContext>(
                new AndExpression<TContext>(
                    // It has to have happened to the PLAYER: her own jars empty by removal too.
                    new TargetHasStatusExpression<TContext>(
                        CombatantTargetSelectors.EventTarget,
                        new StatusDefinitionId(PassiveStatuses.ApplicantId)),
                    // "Is there room?" depends on which shelf she is standing at, and a question is not a
                    // branch: the two readings are written out and one of them is true.
                    new OrExpression<TContext>(
                        new AndExpression<TContext>(wearsThreeJars, Room(true)),
                        new AndExpression<TContext>(
                            new NotExpression<TContext>(wearsThreeJars), Room(false)))),
                Sort(0)));
    }

    // The player's turn opens with the washing unspent and a sheet in hand for every jar that has something
    // in it — which is how a choice among four (or three) is put in front of them.
    private static EffectProgram<TurnStartedTriggeredEffectContext> OpenTheJars()
    {
        var mother = Bearer(NatronAndResinId);

        IEffectNode<TurnStartedTriggeredEffectContext> Offer(string vesselId, string cardId) =>
            new ConditionalEffectNode<TurnStartedTriggeredEffectContext>(
                new ComparisonExpression<TurnStartedTriggeredEffectContext>(
                    new CombatantStatusStacksExpression<TurnStartedTriggeredEffectContext>(
                        mother, new StatusDefinitionId(vesselId)),
                    ComparisonOperator.Greater,
                    new ConstantExpression<TurnStartedTriggeredEffectContext>(0)),
                new CreateCardInstanceNode<TurnStartedTriggeredEffectContext>(
                    Applicant, new CardDefinitionId(cardId), CardZone.Hand,
                    new ConstantExpression<TurnStartedTriggeredEffectContext>(1)));

        return new EffectProgram<TurnStartedTriggeredEffectContext>(
            new ConditionalEffectNode<TurnStartedTriggeredEffectContext>(
                PlayersTurn<TurnStartedTriggeredEffectContext>(),
                new CausalSequenceEffectNode<TurnStartedTriggeredEffectContext>(
                [
                    new SetCombatantCounterNode<TurnStartedTriggeredEffectContext>(
                        mother, WashSpent,
                        new ConstantExpression<TurnStartedTriggeredEffectContext>(0), relative: false),

                    // Whether the shelf is full is settled here and nowhere else. A shelf that filled while
                    // the player's turn was ending would otherwise be answered by her before they had a turn
                    // to answer it, and the wash they were promised would arrive after the unsealing.
                    new RemoveStatusNode<TurnStartedTriggeredEffectContext>(
                        mother, new StatusDefinitionId(VesselsFullId)),
                    new ConditionalEffectNode<TurnStartedTriggeredEffectContext>(
                        FullShelf<TurnStartedTriggeredEffectContext>(mother),
                        new ApplyStatusNode<TurnStartedTriggeredEffectContext>(
                            mother, new StatusDefinitionId(VesselsFullId),
                            new ConstantExpression<TurnStartedTriggeredEffectContext>(1),
                            sourceSelector: mother)),

                    .. MotherVessels.Select(v => Offer(v.Vessel, v.Card)),
                ])));
    }

    // 305 is the failsafe on the short shelf, and 90 announces the last preparation. Neither is an attack:
    // the fight pauses, the shelf is trimmed to what the new phase can hold, and the player is handed a turn
    // to stand in.
    private static EffectProgram<DamageReceivedTriggeredEffectContext> TheEmbalmingFailsafes()
    {
        var mother = Bearer(NatronAndResinId);
        var health = new CombatantCurrentHealthExpression<DamageReceivedTriggeredEffectContext>(mother);

        ICombatExpression<DamageReceivedTriggeredEffectContext, bool> At(int band) =>
            new ComparisonExpression<DamageReceivedTriggeredEffectContext>(
                health, ComparisonOperator.LessOrEqual,
                new ConstantExpression<DamageReceivedTriggeredEffectContext>(band));

        ICombatExpression<DamageReceivedTriggeredEffectContext, bool> NotYet(CounterId taken) =>
            new ComparisonExpression<DamageReceivedTriggeredEffectContext>(
                new CombatantCounterExpression<DamageReceivedTriggeredEffectContext>(mother, taken),
                ComparisonOperator.Equal,
                new ConstantExpression<DamageReceivedTriggeredEffectContext>(0));

        // A shelf of four walking into a phase that holds three loses one jar, and it is a GIFT, not a
        // judgment: the master lets the player pick which one goes. The order is fixed here because the
        // engine has no way to ask mid-trigger, and because nothing about it can hurt.
        IEffectNode<DamageReceivedTriggeredEffectContext> Spill(int index) =>
            index >= MotherVessels.Length
                ? new NoOpEffectNode<DamageReceivedTriggeredEffectContext>()
                : new ConditionalEffectNode<DamageReceivedTriggeredEffectContext>(
                    new ComparisonExpression<DamageReceivedTriggeredEffectContext>(
                        new CombatantStatusStacksExpression<DamageReceivedTriggeredEffectContext>(
                            mother, new StatusDefinitionId(MotherVessels[index].Vessel)),
                        ComparisonOperator.Greater,
                        new ConstantExpression<DamageReceivedTriggeredEffectContext>(0)),
                    new CausalSequenceEffectNode<DamageReceivedTriggeredEffectContext>(
                    [
                        new ModifyStatusStacksNode<DamageReceivedTriggeredEffectContext>(
                            mother, new StatusDefinitionId(MotherVessels[index].Vessel),
                            new ConstantExpression<DamageReceivedTriggeredEffectContext>(-1)),
                        new ModifyStatusStacksNode<DamageReceivedTriggeredEffectContext>(
                            mother, new StatusDefinitionId(VesselsFilledId),
                            new ConstantExpression<DamageReceivedTriggeredEffectContext>(-1)),
                    ]),
                    Spill(index + 1));

        return new EffectProgram<DamageReceivedTriggeredEffectContext>(
            new CausalSequenceEffectNode<DamageReceivedTriggeredEffectContext>(
            [
                new ConditionalEffectNode<DamageReceivedTriggeredEffectContext>(
                    new AndExpression<DamageReceivedTriggeredEffectContext>(
                        At(ThreeJarsAt), NotYet(JarsTaken)),
                    new CausalSequenceEffectNode<DamageReceivedTriggeredEffectContext>(
                    [
                        ThreeJarsNow<DamageReceivedTriggeredEffectContext>(mother),
                        new ConditionalEffectNode<DamageReceivedTriggeredEffectContext>(
                            new ComparisonExpression<DamageReceivedTriggeredEffectContext>(
                                new CombatantStatusStacksExpression<DamageReceivedTriggeredEffectContext>(
                                    mother, new StatusDefinitionId(VesselsFilledId)),
                                ComparisonOperator.Greater,
                                new ConstantExpression<DamageReceivedTriggeredEffectContext>(JarsPhaseTwo)),
                            Spill(0)),
                    ])),

                // The last preparation is announced, never sprung: it is written on her before the turn it
                // answers, and the shelf the player leaves standing is the size of it.
                new ConditionalEffectNode<DamageReceivedTriggeredEffectContext>(
                    new AndExpression<DamageReceivedTriggeredEffectContext>(
                        At(PreparationAt), NotYet(PreparationTaken)),
                    new CausalSequenceEffectNode<DamageReceivedTriggeredEffectContext>(
                    [
                        new SetCombatantCounterNode<DamageReceivedTriggeredEffectContext>(
                            mother, PreparationTaken,
                            new ConstantExpression<DamageReceivedTriggeredEffectContext>(1), relative: false),
                        new ApplyStatusNode<DamageReceivedTriggeredEffectContext>(
                            mother, new StatusDefinitionId(LastPreparationId),
                            new ConstantExpression<DamageReceivedTriggeredEffectContext>(1),
                            sourceSelector: mother),
                    ])),
            ]));
    }

    // "Is the shelf full?" — the same two readings the storing side asks in the negative.
    private static ICombatExpression<TContext, bool> FullShelf<TContext>(ICombatantTargetSelector mother)
        where TContext : class
    {
        var filled = new CombatantStatusStacksExpression<TContext>(
            mother, new StatusDefinitionId(VesselsFilledId));
        var threeJars = new TargetHasStatusExpression<TContext>(
            mother, new StatusDefinitionId(ThreeJarsId));

        ICombatExpression<TContext, bool> AtLeast(int jars) =>
            new ComparisonExpression<TContext>(
                filled, ComparisonOperator.GreaterOrEqual, new ConstantExpression<TContext>(jars));

        return new OrExpression<TContext>(
            new AndExpression<TContext>(threeJars, AtLeast(JarsPhaseTwo)),
            new AndExpression<TContext>(
                new NotExpression<TContext>(threeJars), AtLeast(JarsPhaseOne)));
    }

    // The transition, written once because two things reach it: the first unsealing, and 305.
    private static IEffectNode<TContext> ThreeJarsNow<TContext>(ICombatantTargetSelector mother)
        where TContext : class =>
        new CausalSequenceEffectNode<TContext>(
        [
            new SetCombatantCounterNode<TContext>(
                mother, JarsTaken, new ConstantExpression<TContext>(1), relative: false),
            new ApplyStatusNode<TContext>(
                mother, new StatusDefinitionId(ThreeJarsId),
                new ConstantExpression<TContext>(1), sourceSelector: mother),
            new GainBlockNode<TContext>(mother, new ConstantExpression<TContext>(ThreeJarsBlock)),
        ]);

    // ── the intents ───────────────────────────────────────────────────────────────────────────────────────

    private static EffectProgram<EnemyActionContext> ByJars(
        IEffectNode<EnemyActionContext> I, IEffectNode<EnemyActionContext> II) =>
        new(new ConditionalEffectNode<EnemyActionContext>(
            new TargetHasStatusExpression<EnemyActionContext>(
                CombatantTargetSelectors.Source, new StatusDefinitionId(ThreeJarsId)),
            II, I));

    // One slot, two unsealings. Everything the shelf holds comes back at one stack apiece, two Embalmed on
    // top of it, and the shelf is emptied — and on the short shelf it also takes 12 straight out, which is
    // an HP loss and not a blow: no Block stands in front of it and no Strength makes it bigger.
    private static EffectProgram<EnemyActionContext> UnsealTheVessels()
    {
        var mother = CombatantTargetSelectors.Source;

        IEffectNode<EnemyActionContext> GiveBack(string vesselId, string statusId) =>
            new ConditionalEffectNode<EnemyActionContext>(
                new ComparisonExpression<EnemyActionContext>(
                    new CombatantStatusStacksExpression<EnemyActionContext>(
                        mother, new StatusDefinitionId(vesselId)),
                    ComparisonOperator.Greater, Const(0)),
                // One stack per JAR, and a jar that holds three of a thing is three jars holding it.
                new ApplyStatusNode<EnemyActionContext>(
                    Applicant, new StatusDefinitionId(statusId),
                    new CombatantStatusStacksExpression<EnemyActionContext>(
                        mother, new StatusDefinitionId(vesselId)),
                    sourceSelector: mother));

        var shortShelf = new TargetHasStatusExpression<EnemyActionContext>(
            mother, new StatusDefinitionId(ThreeJarsId));

        return new EffectProgram<EnemyActionContext>(
            new CausalSequenceEffectNode<EnemyActionContext>(
            [
                .. MotherVessels.Select(v => GiveBack(v.Vessel, v.Status)),

                new ApplyStatusNode<EnemyActionContext>(
                    Applicant, new StatusDefinitionId(EmbalmedId), Const(UnsealEmbalmed),
                    sourceSelector: mother),

                new ConditionalEffectNode<EnemyActionContext>(
                    shortShelf,
                    new DealDamageNode<EnemyActionContext>(
                        Applicant, Const(UnsealThreeLoss),
                        ignoresBlock: true, kind: DamageKind.DamageOverTime)),

                .. MotherVessels.Select(v => (IEffectNode<EnemyActionContext>)
                    new RemoveStatusNode<EnemyActionContext>(
                        mother, new StatusDefinitionId(v.Vessel))),
                new RemoveStatusNode<EnemyActionContext>(
                    mother, new StatusDefinitionId(VesselsFilledId)),
                new RemoveStatusNode<EnemyActionContext>(
                    mother, new StatusDefinitionId(VesselsFullId)),

                // The first unsealing that actually resolves is what shortens the shelf.
                new ConditionalEffectNode<EnemyActionContext>(
                    new AndExpression<EnemyActionContext>(
                        new NotExpression<EnemyActionContext>(shortShelf),
                        new ComparisonExpression<EnemyActionContext>(
                            new CombatantCounterExpression<EnemyActionContext>(mother, JarsTaken),
                            ComparisonOperator.Equal, Const(0))),
                    ThreeJarsNow<EnemyActionContext>(mother)),
            ]));
    }

    // The signature. What the shelf still holds is what it comes to, and the jars are not emptied by it —
    // washing before it lands is the only thing that makes it smaller.
    private static EffectProgram<EnemyActionContext> FinishThePreparation()
    {
        var mother = CombatantTargetSelectors.Source;

        return new EffectProgram<EnemyActionContext>(
            new CausalSequenceEffectNode<EnemyActionContext>(
            [
                new DealDamageNode<EnemyActionContext>(
                    Applicant,
                    new MinExpression<EnemyActionContext>(
                        Const(PreparationCap),
                        new AddExpression<EnemyActionContext>(
                            Const(PreparationBase),
                            new MultiplyExpression<EnemyActionContext>(
                                Const(PreparationPerJar),
                                new CombatantStatusStacksExpression<EnemyActionContext>(
                                    mother, new StatusDefinitionId(VesselsFilledId)))))),

                new RemoveStatusNode<EnemyActionContext>(
                    mother, new StatusDefinitionId(LastPreparationId)),
            ]));
    }

    // ── washing, as cards ─────────────────────────────────────────────────────────────────────────────────

    // A jar is emptied by PLAYING the sheet that empties it. The sheet is free to hold and costs an Energy to
    // use — spent inside the program rather than as a card cost, so a second sheet in a turn where the
    // washing is already spent is a dead sheet and not a tax.
    private static CardData WashCard(string id, string kind, string vesselId)
    {
        var mother = Bearer(NatronAndResinId);

        var energy = new CombatantCurrentResourceExpression<CardPlayContext>(
            CombatantTargetSelectors.Source, StandardCombatIds.EnergyResource);

        return new CardData
        {
            Id = id,
            NameKey = $"Wash the {kind} Vessel",
            DescriptionKey =
                $"Pay 1 Energy: the jar holding {kind} is emptied and cannot come back. You are Embalmed 1 "
                + "for handling it. One jar a turn.",
            Costs = [],
            Tags = [new TagId(VesselTag), new TagId(Cards.CardAuthoring.TemporaryTag)],
            Program = new EffectProgram<CardPlayContext>(
                new ConditionalEffectNode<CardPlayContext>(
                    new AndExpression<CardPlayContext>(
                        new ComparisonExpression<CardPlayContext>(
                            new CombatantCounterExpression<CardPlayContext>(mother, WashSpent),
                            ComparisonOperator.Equal, new ConstantExpression<CardPlayContext>(0)),
                        new AndExpression<CardPlayContext>(
                            new ComparisonExpression<CardPlayContext>(
                                new CombatantStatusStacksExpression<CardPlayContext>(
                                    mother, new StatusDefinitionId(vesselId)),
                                ComparisonOperator.Greater, new ConstantExpression<CardPlayContext>(0)),
                            new ComparisonExpression<CardPlayContext>(
                                energy, ComparisonOperator.GreaterOrEqual,
                                new ConstantExpression<CardPlayContext>(1)))),
                    new CausalSequenceEffectNode<CardPlayContext>(
                    [
                        new ModifyResourceNode<CardPlayContext>(
                            CombatantTargetSelectors.Source, StandardCombatIds.EnergyResource,
                            new ConstantExpression<CardPlayContext>(-1)),
                        new ModifyStatusStacksNode<CardPlayContext>(
                            mother, new StatusDefinitionId(vesselId),
                            new ConstantExpression<CardPlayContext>(-1)),
                        new ModifyStatusStacksNode<CardPlayContext>(
                            mother, new StatusDefinitionId(VesselsFilledId),
                            new ConstantExpression<CardPlayContext>(-1)),
                        new SetCombatantCounterNode<CardPlayContext>(
                            mother, WashSpent, new ConstantExpression<CardPlayContext>(1), relative: false),
                        new ApplyStatusNode<CardPlayContext>(
                            CombatantTargetSelectors.Source, new StatusDefinitionId(EmbalmedId),
                            new ConstantExpression<CardPlayContext>(1), sourceSelector: mother),
                    ]))),
            PlayedCardDestinationZone = CardZone.ExhaustPile,
            TurnEndHandDestinationZone = CardZone.ExhaustPile,
        };
    }
}
