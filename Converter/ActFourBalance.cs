using System.Text.Json;
using RogueDeck.Core.Combat;
using RogueDeck.Sandbox.Composition;
using RogueDeck.Scenario.Authoring;

namespace BnbContent.Converter;

// Act IV, Stages 16 and 17 — The Hall of the Balance and The Sealed Court Before Eternity. Five bodies the
// player already knows, in the offices the labyrinth was always going to promote them into.
//
//   The Crooked Rod Bearer, whose standard was wrong on purpose, now carries the feather of final measure —
//   and it is exact. Meet it and the balance opens; miss it and the answer is the DISTANCE, which is the
//   Reed-Cord Surveyor's lesson delivered by the body that spent the whole act cheating it.
//   The Crocodile of the Short Measure waits under the scale instead of in the grain. Its jaws open on a
//   failed weighing or on a player already deep in burial, and the next bite is the one that counts.
//   The Stone-Hauler Ushabti is a Golden Captain now: the same Stone, quarried the same way out of what the
//   bureaucracy made you overpay, but spent on the Court instead of carried in its own fists.
//   The Palette-Bearing Apprentice is the Court's permanent writer. What it enters does not close: the first
//   affliction this side lays on you each round is preserved, once.
//   The Cornerstone Oath-Stone is part of the final door, keeping the same two tokens it always kept — and
//   arriving with what it remembers of the route already carved into it.
//
// NO NEW VOCABULARY, which is the whole point of both stages: every word here — the measure and its distance,
// Stone, preservation, Kept and Broken Oaths — is one the player has already been taught by the body now
// using it. What changes is the office, not the language.
public static partial class ActFour
{
    public const string FeatherBearerEnemyId = "feather_bearer";
    public const string BalanceCrocodileEnemyId = "crocodile_beneath_the_balance";
    public const string GoldenCaptainEnemyId = "golden_ushabti_captain";
    public const string EternalScribeEnemyId = "eternal_reed_scribe";
    public const string OathboundGateEnemyId = "oathbound_gate";

    public const string FeatherOfFinalMeasureId = "feather_of_final_measure";
    public const string BalanceOpenId = "balance_open";
    public const string WaitsBeneathTheScaleId = "waits_beneath_the_scale";
    public const string JawsOpenId = "jaws_open";
    public const string EntryDoesNotCloseId = "entry_does_not_close";

    // What the feather asks for: the whole turn, exactly. The same three the Crocodile of the Short Measure
    // asked for in Stage 3 — its own final form is standing next to this one, and the two demands are meant
    // to rhyme.
    private const int FinalMeasure = 3;

    // The feather's answer to a miss: 16, and five more for every point of distance, up to 31 in all.
    private const int FeatherAnswer = 16;
    private const int FeatherPerPoint = 5;
    private const int FeatherAnswerCap = 15;

    // How much more the open balance lets through per hit, and how deep in burial the player has to be for
    // the jaws to open on their own.
    private const int BalanceOpening = 8;
    private const int JawsEntombedThreshold = 3;

    // The Captain's brace: what every body of the Court gets, and what a Stone adds to it.
    private const int CourtBrace = 12;
    private const int BracePerStone = 4;
    private const int BraceStoneCap = 12;

    public static CounterId BalanceRead => new("balance_read");
    public static CounterId JawsRead => new("jaws_read");
    public static CounterId EntryMadeThisRound => new("entry_made_this_round");

    public static EffectProgram<EnemyActionContext>? BalanceIntent(string enemyId, string intentId) =>
        $"{enemyId}.{intentId}" switch
        {
            "feather_bearer.true_balance" => SetTheMeasure(0, Const(FinalMeasure)),
            "crocodile_beneath_the_balance.jaws_of_misjudgment" => JawsOfMisjudgment(29),
            "golden_ushabti_captain.command_brace" => CommandBrace(),
            "oathbound_gate.read_the_oath" => SetTheMeasure(16, Const(2)),
            _ => null,
        };

    // ── the Feather-Bearer ────────────────────────────────────────────────────────────────────────────────

    // The exact measure, and the two things it can come to. Both of them belong to the feather and not to the
    // measure — §3.2 — so this rule answers a resolution the same ordering-free way every body in this act
    // does: at its own turn start, once per resolution, through a bookmark.
    public static StatusData FeatherOfFinalMeasure() => new()
    {
        Id = FeatherOfFinalMeasureId,
        NameKey = "The Feather of Final Measure",
        DescriptionKey =
            "This measure is exact and this scale is honest. Meet it and the balance opens — every blow you "
            + "land goes 8 deeper until the feather is raised again. Miss it and the answer is the distance: "
            + "16 damage, and 5 more for every point you were out, up to 31.",
        Polarity = StatusPolarity.Neutral,
        StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
        UsesStacks = false,
        Tags = [],
        PassiveModifiers = [],
        Triggers = [Trigger(AnswerTheBalance(), nameof(TriggerEvent.TurnStarted))],
    };

    // The window a met measure opens: on the FEATHER-BEARER, because what an honest scale gives you for being
    // exact is a look at what is holding it.
    public static StatusData BalanceOpen() => new()
    {
        Id = BalanceOpenId,
        NameKey = "The Balance Is Open",
        DescriptionKey = "Weighed true. Every blow that lands on this body goes 8 deeper.",
        Polarity = StatusPolarity.Debuff,
        StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
        UsesStacks = false,
        Tags = [],
        PassiveModifiers =
        [
            new PassiveModifierData(PassiveModifierPipeline.DamageReceived,
                PassiveModifierOperation.AddFlat, BalanceOpening, RestrictDamageKind: null),
        ],
        Triggers = [],
    };

    private static EffectProgram<TurnStartedTriggeredEffectContext> AnswerTheBalance()
    {
        var bearer = CombatantTargetSelectors.Source;
        var unread = ResolutionsSinceLastLooked<TurnStartedTriggeredEffectContext>(bearer, BalanceRead);

        var wasExact = new ComparisonExpression<TurnStartedTriggeredEffectContext>(
            new CombatantCounterExpression<TurnStartedTriggeredEffectContext>(Applicant, MeasureResult),
            ComparisonOperator.Equal,
            new ConstantExpression<TurnStartedTriggeredEffectContext>(1));

        // 16 + 5 per point of distance, capped. The record is 1 + the distance, so the distance is the record
        // less one — and a fight in which nothing has resolved never reaches this branch at all.
        var byDistance = new AddExpression<TurnStartedTriggeredEffectContext>(
            new ConstantExpression<TurnStartedTriggeredEffectContext>(FeatherAnswer),
            new MinExpression<TurnStartedTriggeredEffectContext>(
                new MultiplyExpression<TurnStartedTriggeredEffectContext>(
                    new ConstantExpression<TurnStartedTriggeredEffectContext>(FeatherPerPoint),
                    new SubtractExpression<TurnStartedTriggeredEffectContext>(
                        new CombatantCounterExpression<TurnStartedTriggeredEffectContext>(
                            Applicant, MeasureResult),
                        new ConstantExpression<TurnStartedTriggeredEffectContext>(1))),
                new ConstantExpression<TurnStartedTriggeredEffectContext>(FeatherAnswerCap)));

        return new EffectProgram<TurnStartedTriggeredEffectContext>(
            new CausalSequenceEffectNode<TurnStartedTriggeredEffectContext>(
            [
                // Whatever the last weighing gave you, you had one turn of it. The window closes before the
                // next one is answered, so it is never two turns wide and never stacks.
                new RemoveStatusNode<TurnStartedTriggeredEffectContext>(
                    bearer, new StatusDefinitionId(BalanceOpenId)),

                new ConditionalEffectNode<TurnStartedTriggeredEffectContext>(
                    new ComparisonExpression<TurnStartedTriggeredEffectContext>(
                        unread, ComparisonOperator.Greater,
                        new ConstantExpression<TurnStartedTriggeredEffectContext>(0)),
                    new CausalSequenceEffectNode<TurnStartedTriggeredEffectContext>(
                    [
                        MoveTheResolutionBookmark<TurnStartedTriggeredEffectContext>(bearer, BalanceRead),

                        new ConditionalEffectNode<TurnStartedTriggeredEffectContext>(
                            wasExact,
                            new ApplyStatusNode<TurnStartedTriggeredEffectContext>(
                                bearer, new StatusDefinitionId(BalanceOpenId),
                                new ConstantExpression<TurnStartedTriggeredEffectContext>(1),
                                sourceSelector: bearer),
                            new DealDamageNode<TurnStartedTriggeredEffectContext>(Applicant, byDistance)),
                    ])),
            ]));
    }

    // ── the Crocodile Beneath the Balance ─────────────────────────────────────────────────────────────────

    // What opens the jaws, and the two known conditions that do it — a weighing that failed, or a player
    // already three deep in burial. Both are visible to the player before they act, which is the difference
    // between a predator and an ambush.
    public static StatusData WaitsBeneathTheScale() => new()
    {
        Id = WaitsBeneathTheScaleId,
        NameKey = "Waits Beneath the Scale",
        DescriptionKey =
            "This one has been under the scale the whole time. A measure you miss opens its jaws — so does "
            + "carrying 3 Entombed — and the next bite is the one that counts.",
        Polarity = StatusPolarity.Neutral,
        StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
        UsesStacks = false,
        Tags = [],
        PassiveModifiers = [],
        Triggers = [Trigger(OpenTheJaws(), nameof(TriggerEvent.TurnStarted))],
    };

    public static StatusData JawsOpen() => new()
    {
        Id = JawsOpenId,
        NameKey = "Jaws Open",
        DescriptionKey = "The next bite is the Jaws of Misjudgment — and then they close again.",
        Polarity = StatusPolarity.Neutral,
        StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
        UsesStacks = false,
        Tags = [],
        PassiveModifiers = [],
        Triggers = [],
    };

    private static EffectProgram<TurnStartedTriggeredEffectContext> OpenTheJaws()
    {
        var crocodile = CombatantTargetSelectors.Source;
        var missed = SinceLastLooked<TurnStartedTriggeredEffectContext>(crocodile, MeasuresFailed, JawsRead);

        return new EffectProgram<TurnStartedTriggeredEffectContext>(
            new CausalSequenceEffectNode<TurnStartedTriggeredEffectContext>(
            [
                new ConditionalEffectNode<TurnStartedTriggeredEffectContext>(
                    new OrExpression<TurnStartedTriggeredEffectContext>(
                        new ComparisonExpression<TurnStartedTriggeredEffectContext>(
                            missed, ComparisonOperator.Greater,
                            new ConstantExpression<TurnStartedTriggeredEffectContext>(0)),
                        new ComparisonExpression<TurnStartedTriggeredEffectContext>(
                            new CombatantStatusStacksExpression<TurnStartedTriggeredEffectContext>(
                                Applicant, new StatusDefinitionId(EntombedId)),
                            ComparisonOperator.GreaterOrEqual,
                            new ConstantExpression<TurnStartedTriggeredEffectContext>(JawsEntombedThreshold))),
                    new ApplyStatusNode<TurnStartedTriggeredEffectContext>(
                        crocodile, new StatusDefinitionId(JawsOpenId),
                        new ConstantExpression<TurnStartedTriggeredEffectContext>(1),
                        sourceSelector: crocodile)),

                MoveTheBookmark<TurnStartedTriggeredEffectContext>(crocodile, MeasuresFailed, JawsRead),
            ]));
    }

    // The bite that counts: heavier the deeper the player is already buried, and then the jaws close. A
    // predator that stayed open would not be waiting for anything.
    private static EffectProgram<EnemyActionContext> JawsOfMisjudgment(int damage) =>
        new(new CausalSequenceEffectNode<EnemyActionContext>(
        [
            new DealDamageNode<EnemyActionContext>(
                Applicant,
                new AddExpression<EnemyActionContext>(
                    Const(damage),
                    new MinExpression<EnemyActionContext>(
                        new MultiplyExpression<EnemyActionContext>(
                            Const(3),
                            new CombatantStatusStacksExpression<EnemyActionContext>(
                                Applicant, new StatusDefinitionId(EntombedId))),
                        Const(9)))),

            new RemoveStatusNode<EnemyActionContext>(
                CombatantTargetSelectors.Source, new StatusDefinitionId(JawsOpenId)),
        ]));

    // ── the Golden Ushabti Captain ────────────────────────────────────────────────────────────────────────

    // The same Stone, quarried the same way — the Captain still wears `Compulsory Labour` — but spent on the
    // Court rather than swung. Every body of the labyrinth standing here is braced, and the quarry is emptied
    // doing it: an officer who hoarded would be a hauler again.
    private static EffectProgram<EnemyActionContext> CommandBrace()
    {
        var captain = CombatantTargetSelectors.Source;

        return new EffectProgram<EnemyActionContext>(
            new CausalSequenceEffectNode<EnemyActionContext>(
            [
                new ForEachTargetEffectNode<EnemyActionContext>(
                    CombatantTargetSelectors.WithStatus(
                        CombatantTargetSelectors.AllAliveCombatants,
                        new StatusDefinitionId(LabyrinthBodyId)),
                    new GainBlockNode<EnemyActionContext>(
                        CombatantTargetSelectors.IterationTarget,
                        new AddExpression<EnemyActionContext>(
                            Const(CourtBrace),
                            new MinExpression<EnemyActionContext>(
                                new MultiplyExpression<EnemyActionContext>(
                                    Const(BracePerStone),
                                    new CombatantStatusStacksExpression<EnemyActionContext>(
                                        captain, new StatusDefinitionId(StoneId))),
                                Const(BraceStoneCap))))),

                new RemoveStatusNode<EnemyActionContext>(captain, new StatusDefinitionId(StoneId)),
            ]));
    }

    // ── the Eternal Reed Scribe ───────────────────────────────────────────────────────────────────────────

    // "The first important negative status application by the enemy side each round receives Preserved Entry:
    // its next natural decay is prevented once."
    //
    // Which is Embalmed, spelled with the act's own preservation language rather than a second one — one
    // stack, so exactly one fade is held and there is no permanent no-decay state. It is preservation of the
    // PERSON rather than of one entry, because that is what this game's fading point knows how to answer; at
    // the table it comes to the same thing, since the entry the Court just made is the one due to fade next.
    public static StatusData EntryDoesNotClose() => new()
    {
        Id = EntryDoesNotCloseId,
        NameKey = "The Entry Does Not Close",
        DescriptionKey =
            "The Court's permanent writer. The first affliction this side lays on you each round is entered "
            + "for good: 1 Embalmed, which holds the next thing that would fade.",
        Polarity = StatusPolarity.Neutral,
        StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
        UsesStacks = false,
        Tags = [],
        PassiveModifiers = [],
        Triggers =
        [
            Trigger(MakeTheEntry<StatusAppliedTriggeredEffectContext>(),
                nameof(TriggerEvent.StatusApplied), StatusTriggerScope.Anywhere),
            Trigger(MakeTheEntry<StatusMergedTriggeredEffectContext>(),
                nameof(TriggerEvent.StatusMerged), StatusTriggerScope.Anywhere),
            Trigger(ClearLatch<RoundStartedTriggeredEffectContext>(EntryDoesNotCloseId, EntryMadeThisRound),
                nameof(TriggerEvent.RoundStarted), StatusTriggerScope.Anywhere),
        ],
    };

    private static EffectProgram<TContext> MakeTheEntry<TContext>() where TContext : class
    {
        var scribe = Bearer(EntryDoesNotCloseId);

        return new EffectProgram<TContext>(
            new ConditionalEffectNode<TContext>(
                new AndExpression<TContext>(
                    new AndExpression<TContext>(
                        OriginalAfflictionOnThePlayer<TContext>(),
                        new TargetHasStatusExpression<TContext>(
                            CombatantTargetSelectors.Source, new StatusDefinitionId(LabyrinthBodyId))),
                    NotYetThisRound<TContext>(scribe, EntryMadeThisRound)),
                new CausalSequenceEffectNode<TContext>(
                [
                    new SetCombatantCounterNode<TContext>(
                        scribe, EntryMadeThisRound, new ConstantExpression<TContext>(1), relative: false),

                    new ApplyStatusNode<TContext>(
                        Applicant, new StatusDefinitionId(EmbalmedId),
                        new ConstantExpression<TContext>(1), sourceSelector: scribe),
                ])));
    }
}
