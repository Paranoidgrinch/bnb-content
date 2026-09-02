using System.Text.Json;
using RogueDeck.Core.Combat;
using RogueDeck.Sandbox.Composition;
using RogueDeck.Scenario.Authoring;

namespace BnbContent.Converter;

// Act IV, Stage 5 — The Tribute Causeway. Stage 4 asked what a missed measure costs. This one asks what a
// MET one costs, and the answer is the act's whole joke: "The tribute was correct. Processing was not
// included."
//
//   The Foreign Tribute Shade charges for the processing: the first measure you meet each round is filed,
//   and filing is a sheet of Paperwork. Correctness is not a discount.
//   The Donkey of the Third Tally is not carrying three loads — it was ENTERED three times. Every measure
//   that resolves, met or missed, is another entry against the same animal, and the third one is what it
//   feels. A third entry that was correct weighs less; it still weighs.
//   The Empty-Handed Envoy reads the one thing the measure itself cannot say: what was left in your hand
//   when the turn ended. Empty hands mean everything was presented — or that nothing was.
//
// Which is why this stage needed the player's hand counted somewhere the hand still EXISTS. A rule at turn
// end cannot read it: the hand is discarded before turn-end triggers run. So the count is taken as the turn
// happens — when cards are drawn, and after each action the player finishes — and the Envoy reads that number
// afterwards.
public static partial class ActFour
{
    public const string TributeShadeEnemyId = "foreign_tribute_shade";
    public const string DonkeyEnemyId = "donkey_of_the_third_tally";
    public const string EnvoyEnemyId = "empty_handed_envoy";

    public const string AdministrativeCostId = "administrative_cost_of_tribute";
    // The id is the Donkey's own: Act III's Court already owns a status called `tally`, and two acts
    // may share a WORD without sharing a rule.
    public const string TallyId = "donkey_tally";
    public const string ThirdTallyId = "third_tally";
    public const string NothingWasPresentedId = "nothing_was_presented";
    public const string PresentedInFullId = "presented_in_full";

    // How many entries the same animal takes before it feels them (master §Stage 5).
    public const int TalliesPerLoad = 3;

    // What a third entry costs, and what a CORRECT third entry costs instead (appendix: 1–2 Burdened).
    private const int LoadBurden = 2;
    private const int CorrectLoadBurden = 1;

    // How much more an Envoy that has been presented in full takes while it stands there empty-handed.
    private const int PresentedInFullPercent = 150;

    // Each body's bookmark, and the count of what the player still held when the turn ended.
    public static CounterId TributesRead => new("tributes_read");
    public static CounterId TalliesRead => new("tallies_read");
    public static CounterId PresentationsRead => new("presentations_read");
    public static CounterId CardsLeftInHand => new("cards_left_in_hand");

    public static EffectProgram<EnemyActionContext>? CausewayIntent(string enemyId, string intentId) =>
        $"{enemyId}.{intentId}" switch
        {
            "foreign_tribute_shade.assess_tribute" => SetTheMeasure(13, Const(2)),
            _ => null,
        };

    // ── the Foreign Tribute Shade ─────────────────────────────────────────────────────────────────────────

    // The processing fee. It reads the tally of measures MET — the twin the resolution has kept since the
    // Floodmark Basins — against its own bookmark, so a tribute is charged for once and once only, whoever
    // demanded it.
    public static StatusData AdministrativeCostOfTribute() => new()
    {
        Id = AdministrativeCostId,
        NameKey = "Administrative Cost of Tribute",
        DescriptionKey =
            "Correctness is not a discount: the first measure you MEET each round is filed, and filing costs "
            + "you 1 Paperwork.",
        Polarity = StatusPolarity.Neutral,
        StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
        UsesStacks = false,
        Tags = [],
        PassiveModifiers = [],
        Triggers = [Trigger(ChargeForProcessing(), nameof(TriggerEvent.TurnStarted))],
    };

    private static EffectProgram<TurnStartedTriggeredEffectContext> ChargeForProcessing()
    {
        var shade = CombatantTargetSelectors.Source;
        var unread = SinceLastLooked<TurnStartedTriggeredEffectContext>(shade, MeasuresMet, TributesRead);

        return new EffectProgram<TurnStartedTriggeredEffectContext>(
            new ConditionalEffectNode<TurnStartedTriggeredEffectContext>(
                new ComparisonExpression<TurnStartedTriggeredEffectContext>(
                    unread, ComparisonOperator.Greater,
                    new ConstantExpression<TurnStartedTriggeredEffectContext>(0)),
                new CausalSequenceEffectNode<TurnStartedTriggeredEffectContext>(
                [
                    // One sheet however many tributes were correct: the fee is for the round's processing,
                    // not per item.
                    new ApplyStatusNode<TurnStartedTriggeredEffectContext>(
                        Applicant, new StatusDefinitionId(Cards.Keywords.Paperwork),
                        new ConstantExpression<TurnStartedTriggeredEffectContext>(1), sourceSelector: shade),

                    MoveTheBookmark<TurnStartedTriggeredEffectContext>(shade, MeasuresMet, TributesRead),
                ])));
    }

    // ── the Donkey of the Third Tally ─────────────────────────────────────────────────────────────────────

    // The entries against one animal. Visible, because the third one is the one that lands and the player is
    // meant to see it coming.
    public static StatusData Tally() => new()
    {
        Id = TallyId,
        NameKey = "Tally",
        DescriptionKey =
            "This animal has been entered in the register this many times. At 3 the entries are settled: the "
            + "load goes on you, and the tally starts again.",
        Polarity = StatusPolarity.Neutral,
        StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
        UsesStacks = true,
        Tags = [],
        PassiveModifiers = [],
        Triggers = [],
    };

    // Every resolution is an entry, met or missed — the Donkey does not care whether you were right, only
    // that you were counted. What being right changes is what the third entry weighs.
    public static StatusData ThirdTally() => new()
    {
        Id = ThirdTallyId,
        NameKey = "Counted Again",
        DescriptionKey =
            "Every measure that resolves, right or wrong, enters this animal once more. The third entry is "
            + "the one you carry: 2 Burdened, or 1 if that third measure was met.",
        Polarity = StatusPolarity.Neutral,
        StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
        UsesStacks = false,
        Tags = [],
        PassiveModifiers = [],
        Triggers = [Trigger(CountTheDonkeyAgain(), nameof(TriggerEvent.TurnStarted))],
    };

    private static EffectProgram<TurnStartedTriggeredEffectContext> CountTheDonkeyAgain()
    {
        var donkey = CombatantTargetSelectors.Source;
        var unread = ResolutionsSinceLastLooked<TurnStartedTriggeredEffectContext>(donkey, TalliesRead);

        var settle = new CausalSequenceEffectNode<TurnStartedTriggeredEffectContext>(
        [
            // A correct third entry weighs one instead of two. "Correct" is the record the measure leaves:
            // 1 is exact, anything above it is a miss.
            new ConditionalEffectNode<TurnStartedTriggeredEffectContext>(
                new ComparisonExpression<TurnStartedTriggeredEffectContext>(
                    new CombatantCounterExpression<TurnStartedTriggeredEffectContext>(Applicant, MeasureResult),
                    ComparisonOperator.Equal,
                    new ConstantExpression<TurnStartedTriggeredEffectContext>(1)),
                new ApplyStatusNode<TurnStartedTriggeredEffectContext>(
                    Applicant, new StatusDefinitionId(BurdenedId),
                    new ConstantExpression<TurnStartedTriggeredEffectContext>(CorrectLoadBurden),
                    sourceSelector: donkey),
                new ApplyStatusNode<TurnStartedTriggeredEffectContext>(
                    Applicant, new StatusDefinitionId(BurdenedId),
                    new ConstantExpression<TurnStartedTriggeredEffectContext>(LoadBurden),
                    sourceSelector: donkey)),

            new RemoveStatusNode<TurnStartedTriggeredEffectContext>(donkey, new StatusDefinitionId(TallyId)),
        ]);

        return new EffectProgram<TurnStartedTriggeredEffectContext>(
            new ConditionalEffectNode<TurnStartedTriggeredEffectContext>(
                new ComparisonExpression<TurnStartedTriggeredEffectContext>(
                    unread, ComparisonOperator.Greater,
                    new ConstantExpression<TurnStartedTriggeredEffectContext>(0)),
                new CausalSequenceEffectNode<TurnStartedTriggeredEffectContext>(
                [
                    MoveTheResolutionBookmark<TurnStartedTriggeredEffectContext>(donkey, TalliesRead),

                    new ApplyStatusNode<TurnStartedTriggeredEffectContext>(
                        donkey, new StatusDefinitionId(TallyId),
                        new ConstantExpression<TurnStartedTriggeredEffectContext>(1), sourceSelector: donkey),

                    new ConditionalEffectNode<TurnStartedTriggeredEffectContext>(
                        new ComparisonExpression<TurnStartedTriggeredEffectContext>(
                            new CombatantStatusStacksExpression<TurnStartedTriggeredEffectContext>(
                                donkey, new StatusDefinitionId(TallyId)),
                            ComparisonOperator.GreaterOrEqual,
                            new ConstantExpression<TurnStartedTriggeredEffectContext>(TalliesPerLoad)),
                        settle),
                ])));
    }

    // ── the Empty-Handed Envoy ────────────────────────────────────────────────────────────────────────────

    // What the Envoy is open to while it stands there with nothing in its hands: everything was presented,
    // and there is nothing left of it to hide behind.
    public static StatusData PresentedInFull() => new()
    {
        Id = PresentedInFullId,
        NameKey = "Presented in Full",
        DescriptionKey =
            "Everything was presented and everything was correct: this envoy has nothing left to hold up, "
            + "and takes 50% more damage until its next turn.",
        Polarity = StatusPolarity.Debuff,
        StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
        UsesStacks = false,
        Tags = [],
        PassiveModifiers =
        [
            new PassiveModifierData(PassiveModifierPipeline.DamageReceived,
                PassiveModifierOperation.ScalePercent, PresentedInFullPercent),
        ],
        Triggers = [],
    };

    // "When a Weighed requirement resolves while the player ends that turn with nothing left in hand." Empty
    // hands are the ambiguity the identity is built on: presented in full, or nothing presented at all — and
    // the measure is what tells the two apart.
    public static StatusData NothingWasPresented() => new()
    {
        Id = NothingWasPresentedId,
        NameKey = "Nothing Was Presented",
        DescriptionKey =
            "This envoy reads what was left in your hand. End a turn empty-handed and a measure resolves: if "
            + "you met it, the envoy is exposed until its next turn; if you missed it, you are Inscribed.",
        Polarity = StatusPolarity.Neutral,
        StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
        UsesStacks = false,
        Tags = [],
        PassiveModifiers = [],
        Triggers =
        [
            Trigger(ReadTheHand(), nameof(TriggerEvent.TurnStarted)),
            // The hand has to be counted while it still exists: a rule at turn end cannot see it, because the
            // hand is discarded before turn-end triggers run. So the count is taken as the turn happens.
            Trigger(CountTheHand<CardsDrawnTriggeredEffectContext>(), nameof(TriggerEvent.CardsDrawn),
                StatusTriggerScope.Anywhere),
            Trigger(CountTheHand<ActionResolvedTriggeredEffectContext>(), nameof(TriggerEvent.ActionResolved),
                StatusTriggerScope.Anywhere),
        ],
    };

    // Whatever the player is holding right now, written down.
    //
    // Two things are load-bearing here. ActionResolved rather than CardPlayed, because a card is still in the
    // hand while its own play is resolving and the count would be one too high. And the count is taken only
    // when the PLAYER is the one acting: an enemy's action resolves during the enemy's turn, by which time
    // the player's hand has been discarded, and a recount there would report every turn as empty-handed.
    private static EffectProgram<TContext> CountTheHand<TContext>() where TContext : class =>
        new(new ConditionalEffectNode<TContext>(
            new TargetHasStatusExpression<TContext>(
                CombatantTargetSelectors.Source, new StatusDefinitionId(PassiveStatuses.ApplicantId)),
            new SetCombatantCounterNode<TContext>(
                Applicant, CardsLeftInHand,
                new CombatantZoneCardCountExpression<TContext>(Applicant, CardZone.Hand, null), relative: false)));

    private static EffectProgram<TurnStartedTriggeredEffectContext> ReadTheHand()
    {
        var envoy = CombatantTargetSelectors.Source;
        var unread = ResolutionsSinceLastLooked<TurnStartedTriggeredEffectContext>(envoy, PresentationsRead);

        var emptyHanded = new ComparisonExpression<TurnStartedTriggeredEffectContext>(
            new CombatantCounterExpression<TurnStartedTriggeredEffectContext>(Applicant, CardsLeftInHand),
            ComparisonOperator.Equal,
            new ConstantExpression<TurnStartedTriggeredEffectContext>(0));

        var measureWasMet = new ComparisonExpression<TurnStartedTriggeredEffectContext>(
            new CombatantCounterExpression<TurnStartedTriggeredEffectContext>(Applicant, MeasureResult),
            ComparisonOperator.Equal,
            new ConstantExpression<TurnStartedTriggeredEffectContext>(1));

        return new EffectProgram<TurnStartedTriggeredEffectContext>(
            new CausalSequenceEffectNode<TurnStartedTriggeredEffectContext>(
            [
                // Yesterday's exposure is over: it lasted exactly as long as the player's turn.
                new RemoveStatusNode<TurnStartedTriggeredEffectContext>(
                    envoy, new StatusDefinitionId(PresentedInFullId)),

                new ConditionalEffectNode<TurnStartedTriggeredEffectContext>(
                    new AndExpression<TurnStartedTriggeredEffectContext>(
                        new ComparisonExpression<TurnStartedTriggeredEffectContext>(
                            unread, ComparisonOperator.Greater,
                            new ConstantExpression<TurnStartedTriggeredEffectContext>(0)),
                        emptyHanded),
                    new CausalSequenceEffectNode<TurnStartedTriggeredEffectContext>(
                    [
                        new ConditionalEffectNode<TurnStartedTriggeredEffectContext>(
                            measureWasMet,
                            // Everything was presented: there is nothing left to hide behind.
                            new ApplyStatusNode<TurnStartedTriggeredEffectContext>(
                                envoy, new StatusDefinitionId(PresentedInFullId),
                                new ConstantExpression<TurnStartedTriggeredEffectContext>(1),
                                sourceSelector: envoy),
                            // …or nothing was presented at all, and the register says so.
                            new ApplyStatusNode<TurnStartedTriggeredEffectContext>(
                                Applicant, new StatusDefinitionId(InscribedId),
                                new ConstantExpression<TurnStartedTriggeredEffectContext>(1),
                                sourceSelector: envoy)),
                    ])),

                // The bookmark moves whether the hand was empty or not: a resolution is read once, and a
                // full hand is an answer too.
                new ConditionalEffectNode<TurnStartedTriggeredEffectContext>(
                    new ComparisonExpression<TurnStartedTriggeredEffectContext>(
                        unread, ComparisonOperator.Greater,
                        new ConstantExpression<TurnStartedTriggeredEffectContext>(0)),
                    MoveTheResolutionBookmark<TurnStartedTriggeredEffectContext>(envoy, PresentationsRead)),
            ]));
    }
}
