using System.Text.Json;
using RogueDeck.Core.Combat;
using RogueDeck.Scenario.Authoring;
using BnbContent.Converter.Cards;

namespace BnbContent.Converter.Elites;

// ── The Silence Between Two Words (Act II elite) ──────────────────────────────────────────────────────────
//
// Two written words hang in the Archive; the creature is the silence between them. Each turn it marks two of
// your cards as its Words, and what it gains depends on how many you speak. Speak both and it Echoes twice;
// speak one and it Echoes once and misfiles a card; speak NEITHER and the silence is perfect — it loses ten
// HP, ten Block and an Echo, and you have thrown away two cards to do it.
//
// The bookkeeping is counters on the PLAYER and marks on the player's cards: the pair is about the player's
// hand, and both sides have to read the tally.
public static class SilenceBetweenTwoWords
{
    public const string EnemyId = "silence_between_two_words";

    public const string TheSilenceId = "the_silence";
    public const string SilenceRulesId = "unspoken_pair_rules";
    public const string EchoId = "echo";

    public const string FirstWordMark = "first_word";
    public const string SecondWordMark = "second_word";
    // Set while a card is eligible to become a Word, and on the cards that WERE Words last turn — the design
    // forbids picking the same instance twice running when there is room to choose otherwise.
    public const string EligibleMark = "word_eligible";
    public const string WasWordMark = "was_word";

    private static readonly CounterId SpokenCounter = new("words_spoken");
    private static readonly CounterId PairSizeCounter = new("unspoken_pair_size");

    private const int MaxEcho = 4;
    private const int PerfectSilenceHpLoss = 10;
    private const int PerfectSilenceBlockLoss = 10;

    private static readonly ICombatantTargetSelector Opponent = CombatantTargetSelectors.LowestHealthEnemyOfSource;
    private static readonly ICombatantTargetSelector Self = CombatantTargetSelectors.Source;
    private static readonly ICombatantTargetSelector Silences =
        CombatantTargetSelectors.AllEnemiesOfSourceWithStatus(new StatusDefinitionId(TheSilenceId));

    public static IEnumerable<StatusData> Statuses() => [Marker(TheSilenceId, "The Silence"), Echo(), Rules()];

    // ── 8.5 Echo ──────────────────────────────────────────────────────────────────────────────────────────
    //
    // "+4 damage per Echo on the next direct attack, then Echo → 0." Written as a passive modifier plus the
    // trigger that spends it — which is also why the signature deals a flat 6: the Echo adds its own 16 at
    // Echo 4, for the 22 the design states, instead of the number being counted twice.
    private static StatusData Echo()
    {
        var spend = new EffectProgram<DamageDealtTriggeredEffectContext>(
            new ModifyStatusStacksNode<DamageDealtTriggeredEffectContext>(
                Self, new StatusDefinitionId(EchoId),
                new NegateExpression<DamageDealtTriggeredEffectContext>(
                    new CombatantStatusStacksExpression<DamageDealtTriggeredEffectContext>(
                        Self, new StatusDefinitionId(EchoId)))));

        return new StatusData
        {
            Id = EchoId,
            NameKey = "Echo",
            DescriptionKey = "Words spoken into the silence. The next direct attack hits for 4 more each, and spends them.",
            Polarity = StatusPolarity.Buff,
            StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
            UsesStacks = true,
            Tags = [],
            PassiveModifiers =
            [
                new PassiveModifierData(PassiveModifierPipeline.DamageDealt,
                    PassiveModifierOperation.AddPerStack, 4, RestrictDamageKind: DamageKind.Direct),
            ],
            Triggers =
            [
                new StatusTriggerData("DamageDealt", JsonSerializer.SerializeToElement(
                    spend, CombatJson.CreateOptions<DamageDealtTriggeredEffectContext>())),
            ],
        };
    }

    // ── 8.2 The Unspoken Pair ─────────────────────────────────────────────────────────────────────────────
    //
    // After the player's normal draw, two different non-Junk cards in hand become the Words. The selection is
    // the Silence's, not the player's, so it is deterministic: the first two eligible cards.
    //
    // "The same card instance cannot be selected on two consecutive player turns if at least three
    // alternatives exist" is built as a three-pass sieve: mark every non-Junk card eligible, strike the
    // eligibility of last turn's Words, and — only if fewer than two survive — put them back. That last step
    // is the design's "if at least three alternatives exist" read from the other side: when there is not
    // enough hand to avoid a repeat, the repeat is allowed.
    private static StatusData Rules()
    {
        var pair = new EffectProgram<CardsDrawnTriggeredEffectContext>(
            new CausalSequenceEffectNode<CardsDrawnTriggeredEffectContext>(
            [
                ClearMark<CardsDrawnTriggeredEffectContext>(CardZone.Hand, EligibleMark),
                MarkEligible(),
                StrikeLastTurnsWords(),
                RestoreIfTooFew(),
                new SetCombatantCounterNode<CardsDrawnTriggeredEffectContext>(
                    Self, SpokenCounter, new ConstantExpression<CardsDrawnTriggeredEffectContext>(0), relative: false),
                new SetCombatantCounterNode<CardsDrawnTriggeredEffectContext>(
                    Self, PairSizeCounter, new ConstantExpression<CardsDrawnTriggeredEffectContext>(0), relative: false),
                // "If fewer than two eligible cards exist, no Pair is created that turn."
                new ConditionalEffectNode<CardsDrawnTriggeredEffectContext>(
                    new ComparisonExpression<CardsDrawnTriggeredEffectContext>(
                        EligibleCount<CardsDrawnTriggeredEffectContext>(),
                        ComparisonOperator.GreaterOrEqual,
                        new ConstantExpression<CardsDrawnTriggeredEffectContext>(2)),
                    new CausalSequenceEffectNode<CardsDrawnTriggeredEffectContext>(
                    [
                        SpeakWord(FirstWordMark),
                        SpeakWord(SecondWordMark),
                        new SetCombatantCounterNode<CardsDrawnTriggeredEffectContext>(
                            Self, PairSizeCounter,
                            new ConstantExpression<CardsDrawnTriggeredEffectContext>(2), relative: false),
                    ])),
            ]));

        // 8.3: a Word is spoken only if the card is actually PLAYED. Every other way it can leave the hand —
        // discarded, exhausted, transformed, put back — leaves it unspoken, which is what makes the tally a
        // count of plays rather than a count of departures.
        var speak = new EffectProgram<CardPlayedTriggeredEffectContext>(
            new ConditionalEffectNode<CardPlayedTriggeredEffectContext>(
                new OrExpression<CardPlayedTriggeredEffectContext>(
                    IsWord<CardPlayedTriggeredEffectContext>(FirstWordMark),
                    IsWord<CardPlayedTriggeredEffectContext>(SecondWordMark)),
                new SetCombatantCounterNode<CardPlayedTriggeredEffectContext>(
                    Self, SpokenCounter,
                    new ConstantExpression<CardPlayedTriggeredEffectContext>(1), relative: true)));

        return Rule(SilenceRulesId, "The Unspoken Pair",
            "Two of your cards are the Silence's Words. Speaking them feeds its Echo; speaking neither is a "
            + "Perfect Silence, and costs it.",
            [
                new StatusTriggerData("CardsDrawn", JsonSerializer.SerializeToElement(
                    pair, CombatJson.CreateOptions<CardsDrawnTriggeredEffectContext>())),
                new StatusTriggerData("CardPlayed", JsonSerializer.SerializeToElement(
                    speak, CombatJson.CreateOptions<CardPlayedTriggeredEffectContext>())),
                new StatusTriggerData("TurnEnded", JsonSerializer.SerializeToElement(
                    Resolve(), CombatJson.CreateOptions<TurnEndedTriggeredEffectContext>())),
            ]);
    }

    // Every non-Junk card in hand is a candidate. Junk is marked first and then struck, because a loop can
    // select BY a tag but not select around one.
    private static IEffectNode<CardsDrawnTriggeredEffectContext> MarkEligible() =>
        new CausalSequenceEffectNode<CardsDrawnTriggeredEffectContext>(
        [
            new ForEachCardInZoneNode<CardsDrawnTriggeredEffectContext>(
                Self, CardZone.Hand,
                new MarkCardInstanceNode<CardsDrawnTriggeredEffectContext>(
                    Self, new IteratedCardExpression<CardsDrawnTriggeredEffectContext>(),
                    new TagId(EligibleMark))),
            new ForEachCardInZoneNode<CardsDrawnTriggeredEffectContext>(
                Self, CardZone.Hand,
                new MarkCardInstanceNode<CardsDrawnTriggeredEffectContext>(
                    Self, new IteratedCardExpression<CardsDrawnTriggeredEffectContext>(),
                    new TagId(EligibleMark), remove: true),
                tagFilter: new TagId(CardAuthoring.JunkTag)),
        ]);

    private static IEffectNode<CardsDrawnTriggeredEffectContext> StrikeLastTurnsWords() =>
        new ForEachCardInZoneNode<CardsDrawnTriggeredEffectContext>(
            Self, CardZone.Hand,
            new MarkCardInstanceNode<CardsDrawnTriggeredEffectContext>(
                Self, new IteratedCardExpression<CardsDrawnTriggeredEffectContext>(),
                new TagId(EligibleMark), remove: true),
            markFilter: new TagId(WasWordMark));

    private static IEffectNode<CardsDrawnTriggeredEffectContext> RestoreIfTooFew() =>
        new ConditionalEffectNode<CardsDrawnTriggeredEffectContext>(
            new ComparisonExpression<CardsDrawnTriggeredEffectContext>(
                EligibleCount<CardsDrawnTriggeredEffectContext>(),
                ComparisonOperator.Less,
                new ConstantExpression<CardsDrawnTriggeredEffectContext>(2)),
            new ForEachCardInZoneNode<CardsDrawnTriggeredEffectContext>(
                Self, CardZone.Hand,
                new MarkCardInstanceNode<CardsDrawnTriggeredEffectContext>(
                    Self, new IteratedCardExpression<CardsDrawnTriggeredEffectContext>(),
                    new TagId(EligibleMark)),
                markFilter: new TagId(WasWordMark)));

    // Take the first still-eligible card and make it a Word: it stops being eligible so the second pick is a
    // different instance, and it remembers that it was a Word for next turn's sieve.
    private static IEffectNode<CardsDrawnTriggeredEffectContext> SpeakWord(string word) =>
        new ForEachCardInZoneNode<CardsDrawnTriggeredEffectContext>(
            Self, CardZone.Hand,
            new CausalSequenceEffectNode<CardsDrawnTriggeredEffectContext>(
            [
                new MarkCardInstanceNode<CardsDrawnTriggeredEffectContext>(
                    Self, new IteratedCardExpression<CardsDrawnTriggeredEffectContext>(), new TagId(word)),
                new MarkCardInstanceNode<CardsDrawnTriggeredEffectContext>(
                    Self, new IteratedCardExpression<CardsDrawnTriggeredEffectContext>(),
                    new TagId(EligibleMark), remove: true),
            ]),
            markFilter: new TagId(EligibleMark), takeFirst: 1);

    // ── 8.4 Turn-end resolution ───────────────────────────────────────────────────────────────────────────
    //
    // Resolved at the PLAYER's turn end, which is where the design puts it and the only moment that works:
    // the Silence's own Block is wiped when its turn begins, so a resolution a beat later could never take
    // "up to 10 current Block" off anything. The rule runs on the player and reaches across to the Silence.
    private static EffectProgram<TurnEndedTriggeredEffectContext> Resolve() =>
        new(new ConditionalEffectNode<TurnEndedTriggeredEffectContext>(
            new ComparisonExpression<TurnEndedTriggeredEffectContext>(
                new CombatantCounterExpression<TurnEndedTriggeredEffectContext>(Self, PairSizeCounter),
                ComparisonOperator.Equal, new ConstantExpression<TurnEndedTriggeredEffectContext>(2)),
            new CausalSequenceEffectNode<TurnEndedTriggeredEffectContext>(
            [
                new ForEachTargetEffectNode<TurnEndedTriggeredEffectContext>(Silences,
                    new CausalSequenceEffectNode<TurnEndedTriggeredEffectContext>(
                    [
                        Spoken(2, GainEcho(2)),
                        Spoken(1, GainEcho(1)),
                        Spoken(0, PerfectSilence()),
                    ])),
                // A half-spoken sentence misfiles a card. Outside the per-Silence loop, because one card is
                // misfiled however many Silences are listening.
                Spoken(1, MisfileOne()),
                // The pair is settled: the Words are struck, and the cards remember they were Words.
                RememberAndClear(FirstWordMark),
                RememberAndClear(SecondWordMark),
                new SetCombatantCounterNode<TurnEndedTriggeredEffectContext>(
                    Self, PairSizeCounter,
                    new ConstantExpression<TurnEndedTriggeredEffectContext>(0), relative: false),
            ])));

    private static IEffectNode<TurnEndedTriggeredEffectContext> Spoken(
        int count, IEffectNode<TurnEndedTriggeredEffectContext> then) =>
        new ConditionalEffectNode<TurnEndedTriggeredEffectContext>(
            new ComparisonExpression<TurnEndedTriggeredEffectContext>(
                new CombatantCounterExpression<TurnEndedTriggeredEffectContext>(Self, SpokenCounter),
                ComparisonOperator.Equal, new ConstantExpression<TurnEndedTriggeredEffectContext>(count)),
            then);

    // Echo is capped at 4, not refused at 4: over the ceiling it takes what fits.
    private static IEffectNode<TurnEndedTriggeredEffectContext> GainEcho(int amount)
    {
        var target = CombatantTargetSelectors.IterationTarget;
        var stacks = new CombatantStatusStacksExpression<TurnEndedTriggeredEffectContext>(
            target, new StatusDefinitionId(EchoId));

        return new ConditionalEffectNode<TurnEndedTriggeredEffectContext>(
            new ComparisonExpression<TurnEndedTriggeredEffectContext>(
                stacks, ComparisonOperator.LessOrEqual,
                new ConstantExpression<TurnEndedTriggeredEffectContext>(MaxEcho - amount)),
            new ApplyStatusNode<TurnEndedTriggeredEffectContext>(
                target, new StatusDefinitionId(EchoId),
                new ConstantExpression<TurnEndedTriggeredEffectContext>(amount)),
            @else: new ApplyStatusNode<TurnEndedTriggeredEffectContext>(
                target, new StatusDefinitionId(EchoId),
                new SubtractExpression<TurnEndedTriggeredEffectContext>(
                    new ConstantExpression<TurnEndedTriggeredEffectContext>(MaxEcho),
                    new CombatantStatusStacksExpression<TurnEndedTriggeredEffectContext>(
                        target, new StatusDefinitionId(EchoId)))));
    }

    // "Silence loses 10 HP and up to 10 current Block. If Echo exists, Echo -1. The HP Loss is not a Damage
    // event" — so it is a health SET, which no Block and no damage reaction can see.
    private static IEffectNode<TurnEndedTriggeredEffectContext> PerfectSilence()
    {
        var target = CombatantTargetSelectors.IterationTarget;

        return new CausalSequenceEffectNode<TurnEndedTriggeredEffectContext>(
        [
            new SetHealthNode<TurnEndedTriggeredEffectContext>(target,
                new SubtractExpression<TurnEndedTriggeredEffectContext>(
                    new CombatantCurrentHealthExpression<TurnEndedTriggeredEffectContext>(target),
                    new ConstantExpression<TurnEndedTriggeredEffectContext>(PerfectSilenceHpLoss))),
            new ModifyDefensivePoolNode<TurnEndedTriggeredEffectContext>(
                target, StandardCombatIds.BlockDefensivePool,
                new NegateExpression<TurnEndedTriggeredEffectContext>(
                    new MinExpression<TurnEndedTriggeredEffectContext>(
                        new ConstantExpression<TurnEndedTriggeredEffectContext>(PerfectSilenceBlockLoss),
                        new CombatantDefensivePoolExpression<TurnEndedTriggeredEffectContext>(
                            target, StandardCombatIds.BlockDefensivePool)))),
            new ConditionalEffectNode<TurnEndedTriggeredEffectContext>(
                new ComparisonExpression<TurnEndedTriggeredEffectContext>(
                    new CombatantStatusStacksExpression<TurnEndedTriggeredEffectContext>(
                        target, new StatusDefinitionId(EchoId)),
                    ComparisonOperator.Greater, new ConstantExpression<TurnEndedTriggeredEffectContext>(0)),
                new ModifyStatusStacksNode<TurnEndedTriggeredEffectContext>(
                    target, new StatusDefinitionId(EchoId),
                    new ConstantExpression<TurnEndedTriggeredEffectContext>(-1))),
        ]);
    }

    private static IEffectNode<TurnEndedTriggeredEffectContext> RememberAndClear(string word) =>
        new ForEachCardInZoneNode<TurnEndedTriggeredEffectContext>(
            Self, CardZone.Hand,
            new CausalSequenceEffectNode<TurnEndedTriggeredEffectContext>(
            [
                new MarkCardInstanceNode<TurnEndedTriggeredEffectContext>(
                    Self, new IteratedCardExpression<TurnEndedTriggeredEffectContext>(), new TagId(WasWordMark)),
                new MarkCardInstanceNode<TurnEndedTriggeredEffectContext>(
                    Self, new IteratedCardExpression<TurnEndedTriggeredEffectContext>(),
                    new TagId(word), remove: true),
            ]),
            markFilter: new TagId(word));

    private static IEffectNode<TurnEndedTriggeredEffectContext> MisfileOne() =>
        new ForEachCardInZoneNode<TurnEndedTriggeredEffectContext>(
            Self, CardZone.DrawPile,
            new MarkCardInstanceNode<TurnEndedTriggeredEffectContext>(
                Self, new IteratedCardExpression<TurnEndedTriggeredEffectContext>(),
                new TagId(ActTwo.MisfiledMark)),
            takeFirst: 1);

    // ── 8.6 Intents ───────────────────────────────────────────────────────────────────────────────────────
    //
    // Every intent settles the pair first. The offensive ones also ask whether the Echo has filled: at Echo 4
    // the next eligible offensive intent IS the Unspoken Verdict — the same conditional the Colossus uses,
    // and the same limit (the telegraph carries the ordinary intent's name; the Echo counter is the warning).
    public static EffectProgram<EnemyActionContext>? Intent(string intentId) => intentId switch
    {
        "leave_space_between_the_words" => Settled(
            new GainBlockNode<EnemyActionContext>(Self, new ConstantExpression<EnemyActionContext>(19))),
        "remove_the_unnecessary_word" => Offensive(new CausalSequenceEffectNode<EnemyActionContext>(
        [
            Damage(13),
            // The oldest card in hand goes under the pile, and one is drawn in its place.
            new MoveCardToZoneNode<EnemyActionContext>(
                Opponent, new CardInOwnerZoneExpression<EnemyActionContext>(Opponent, CardZone.Hand, 0),
                CardZone.DrawPile),
            new DrawCardsNode<EnemyActionContext>(Opponent, new ConstantExpression<EnemyActionContext>(1)),
        ])),
        "hold_the_sentence_open" => Offensive(new CausalSequenceEffectNode<EnemyActionContext>(
        [
            Damage(11),
            new ApplyStatusNode<EnemyActionContext>(
                Opponent, new StatusDefinitionId(Keywords.Doubt),
                new ConstantExpression<EnemyActionContext>(1)),
        ])),
        "a_word_nearly_spoken" => Offensive(Damage(16)),
        "the_unsaid_sentence" => Offensive(Damage(16)),
        _ => null,
    };

    private static EffectProgram<EnemyActionContext> Settled(IEffectNode<EnemyActionContext> body) => new(body);

    // Signature — Unspoken Verdict: a flat 6, to which the Echo adds its own 4 per stack. At Echo 4 that is
    // the 22 the design states, and the Echo trigger clears itself as it is spent.
    private static EffectProgram<EnemyActionContext> Offensive(IEffectNode<EnemyActionContext> body) =>
        new(new CausalSequenceEffectNode<EnemyActionContext>(
        [
            new ConditionalEffectNode<EnemyActionContext>(
                new ComparisonExpression<EnemyActionContext>(
                    new CombatantStatusStacksExpression<EnemyActionContext>(Self, new StatusDefinitionId(EchoId)),
                    ComparisonOperator.GreaterOrEqual, new ConstantExpression<EnemyActionContext>(MaxEcho)),
                Damage(6),
                @else: body),
        ]));

    private static IEffectNode<EnemyActionContext> Damage(int amount) =>
        new DealDamageNode<EnemyActionContext>(Opponent, new ConstantExpression<EnemyActionContext>(amount));

    // ── shared shapes ─────────────────────────────────────────────────────────────────────────────────────

    private static ICombatExpression<TContext, int> EligibleCount<TContext>() where TContext : class =>
        new CombatantZoneCardCountExpression<TContext>(
            CombatantTargetSelectors.Source, CardZone.Hand, mark: new TagId(EligibleMark));

    private static ICombatExpression<TContext, bool> IsWord<TContext>(string mark) where TContext : class =>
        new CardInstanceHasMarkExpression<TContext>(
            new TriggerEventCardInstanceExpression<TContext>(), new TagId(mark));

    private static IEffectNode<TContext> ClearMark<TContext>(CardZone zone, string mark) where TContext : class =>
        new ForEachCardInZoneNode<TContext>(
            CombatantTargetSelectors.Source, zone,
            new MarkCardInstanceNode<TContext>(
                CombatantTargetSelectors.Source, new IteratedCardExpression<TContext>(),
                new TagId(mark), remove: true),
            markFilter: new TagId(mark));

    private static StatusData Marker(string id, string name) => Rule(id, name, name, []);

    private static StatusData Rule(
        string id, string name, string description, IReadOnlyList<StatusTriggerData> triggers) => new()
        {
            Id = id,
            NameKey = name,
            DescriptionKey = description,
            Polarity = StatusPolarity.Neutral,
            StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
            UsesStacks = false,
            Tags = [],
            PassiveModifiers = [],
            Triggers = triggers,
        };
}
