using System.Text.Json;
using RogueDeck.Core.Combat;
using RogueDeck.Sandbox.Composition;
using RogueDeck.Scenario.Authoring;

namespace BnbContent.Converter;

// ACT V, the first god — NISABA, KEEPER OF THE FIRST TABLET. She writes, and the room agrees afterwards.
//
// She does not record what happens; what she has written HAPPENS. So her fight is not read off her intent but
// off the tablet standing beside her: three INSCRIPTIONS, each a sentence about your future with a number of
// turns beside it, and when a sentence's count runs out the sentence becomes true.
//
//   The Body Shall Bear Thirty-Six.          2   36 HP, and Block has nothing to say about it.
//   Three Measures Shall Be Withheld.        1   three Energy off the turn that follows.
//   The Hand Shall Hold Two.                 2   you draw three fewer cards.
//   The Guard Shall Be Counted as Nothing.   2   every Block you gain next turn is worth 18 less.
//   Three Wounds Shall Be Entered.           3   three sheets of Red Tape into your draw pile.
//   Two Works Shall Be Broken.               3   two cards out of your draw pile, exhausted.
//
// You cannot dispel a sentence. You EDIT it. A REED MARK — one a turn, four at most, and a fifth for meeting
// her Counted Margin — buys one revision of one line, and a revised line becomes true in smaller words:
//
//   0 → the whole sentence · 1 → two thirds · 2 → a third · 3 → nothing at all · 4 → it turns against her.
//
// The written statement still becomes true. Only the wording changed. That is the whole fight: not "can I
// survive this", but "which of the three do I get to the end of before the count does".
//
// Three phases, each announced by her own blood. THE CLAY REMEMBERS (100–65 %) is the tablet as written above.
// THE LAPIS RECORD (65–30 %) writes on something less forgiving: she IMPRESSES A SEAL on the line closest to
// enactment that will still BE there, and a sealed line cannot be revised for one whole turn — the target is
// shown before the turn it binds, so the question is which line must be corrected FIRST. And at 30 % every ordinary line is closed and
// one sentence is left:
//
//   ON THE FOURTH DAWN, THE NAME OF THE SUPPLICANT SHALL BE ERASED.
//
// Four rounds. Unrevised it kills you outright — not damage, erasure. And while it stands unresolved she is
// INDELIBLE: her HP cannot fall below 1, however hard you hit, because the lethal wording is still on the
// tablet. Revise it four times and it reads THE NAME OF THE SUPPLICANT SHALL REMAIN, the Indelible goes, and
// she can be killed like anything else. Revise it a fifth time and it reads THE NAME OF THE KEEPER SHALL BE
// ERASED, and the tablet is not particular about whose name is on it.
public static partial class ActFive
{
    public const string NisabaEnemyId = "nisaba_keeper_of_the_first_tablet";
    public const string NisabaEncounterId = "act_5_nisaba_keeper_of_the_first_tablet";

    // Her rule, worn from the first round; every trigger below hangs off it.
    public const string FirstTabletId = "the_first_tablet";

    // What the player spends to edit a line, and the count she asks the turn to come to.
    public const string ReedMarksId = "reed_marks";
    public const string CountedMarginId = "counted_margin";
    public const int ReedMarkCeiling = 4;

    // The two later phases, and the announcement that precedes each (announced by damage, taken by the intent
    // it licenses — the Scribe's idiom, and for the same reason: a phase that lands mid-turn lands on a plan
    // that was made without it).
    public const string LapisRecordId = "the_lapis_record";
    public const string LapisAnnouncedId = "the_lapis_tablet_is_opened";
    public const string LastLineId = "the_last_line";
    public const string LastLineRevisedId = "the_last_line_revised";
    public const string LastLineAnnouncedId = "the_last_line_is_written";
    public const string IndelibleId = "indelible";

    // What an enacted sentence leaves on the player, one face per kind of harm. Each is worn for exactly the
    // turn it was written about and takes itself off at the end of it.
    public const string MeasuresWithheldId = "measures_withheld";
    public const string HandShallHoldId = "the_hand_shall_hold";
    public const string OpenHandId = "the_open_hand";
    public const string CountedAsNothingId = "counted_as_nothing";
    public const string GuardShallStandId = "the_guard_shall_stand";

    // The revision sheets carry it so the tablet does not count them as cards played, and so they never
    // become an entry in anybody else's record.
    public const string ReedTag = "nisaba_reed";

    public const int NisabaMaxHealth = 620;
    private const int LapisAt = 403;      // 65 % of 620
    private const int LastLineAt = 186;   // 30 % of 620
    private const int LastLineRounds = 4;

    // The countdown a sentence is written with, and how far each revision walks it back, live in the table.
    private sealed record Line(string Slug, string Sentence, string Short, int Turns, string Ladder);

    private static readonly Line[] Lines =
    [
        new("body_shall_bear", "The Body Shall Bear Thirty-Six", "Thirty-Six", 2,
            "36 HP, ignoring Block. Revised: 24 · 12 · nothing · and at four, Nisaba bears 18 herself."),
        new("measures_withheld", "Three Measures Shall Be Withheld", "Three Measures", 1,
            "3 Energy off your next turn. Revised: 2 · 1 · none · and at four, one card of that turn costs 1 less."),
        new("hand_shall_hold_two", "The Hand Shall Hold Two", "The Hand Holds Two", 2,
            "You draw 3 fewer cards next turn. Revised: 2 · 1 · none · and at four, you draw one more."),
        new("guard_counted_nothing", "The Guard Shall Be Counted as Nothing", "The Guard Counts Nothing", 2,
            "Every Block you gain next turn is worth 18 less. Revised: 12 · 6 · none · and at four, 6 more."),
        new("three_wounds", "Three Wounds Shall Be Entered", "Three Wounds", 3,
            "3 sheets of Red Tape into your draw pile. Revised: 2 · 1 · none · and at four, one sheet is struck out."),
        new("two_works_broken", "Two Works Shall Be Broken", "Two Works", 3,
            "2 cards exhausted out of your draw pile. Revised: 1 · none · and at four, one comes back from the exhaust pile."),
    ];

    // THE THREE SLOTS OF THE TABLET, and the whole of the inscription director (§6.5). Each slot owns two
    // sentences and alternates between them, so no sentence can ever stand twice, no two extreme deadlines
    // can crowd the same slot, and the tablet is refilled to three the moment a line comes off it — without
    // anything having to search six sentences for a free one. The pairs mix cadences on purpose: the
    // one-turn sentence shares a slot with a two-turn one, and the two three-turn ones sit apart.
    private static readonly (int A, int B)[] Slots = [(0, 4), (1, 3), (2, 5)];

    // The faces the tests and the frontend name by hand.
    public static IReadOnlyList<string> LineFaces => [.. Lines.Select(LineId)];
    public static IReadOnlyList<string> SealedFaces => [.. Lines.Select(SealedId)];

    private static string LineId(Line line) => $"nisaba_line_{line.Slug}";
    private static string RevisedId(Line line) => $"nisaba_revised_{line.Slug}";
    private static string SealedId(Line line) => $"nisaba_sealed_{line.Slug}";
    private static string ReviseCardId(Line line) => $"revise_{line.Slug}";
    public static string ReviseLastLineCardId => "revise_the_last_line";

    // Counters. What the turn has played, whether the margin was met, which way each slot last wrote, and
    // whether a phase announcement has already been made.
    private static CounterId CardsThisTurn => new("nisaba_cards_this_turn");
    private static CounterId MarginMet => new("nisaba_margin_met");
    private static CounterId SlotTurn(int slot) => new($"nisaba_slot_{slot}");
    private static CounterId SealPlaced => new("nisaba_seal_placed");
    private static CounterId LapisTaken => new("nisaba_lapis_taken");
    private static CounterId LastLineTaken => new("nisaba_last_line_taken");

    private static ICombatantTargetSelector Keeper => Bearer(FirstTabletId);

    // ── what the act hands to the rest of the converter ───────────────────────────────────────────────────

    public static IReadOnlyList<StatusData> NisabaStatuses() =>
    [
        TheFirstTablet(),
        ReedMarks(), CountedMargin(),
        .. Lines.Select(TheLine), .. Lines.Select(TheRevision), .. Lines.Select(TheSeal),
        TheLapisRecord(), Announcement(LapisAnnouncedId, "The Lapis Tablet",
            "Wet earth accepts correction. She is reaching for something less forgiving."),
        TheLastLine(), TheLastLineRevised(), Announcement(LastLineAnnouncedId, "The Last Line",
            "Every other sentence is closed. She is writing the only one that is left."),
        Indelible(),
        MeasuresWithheld(), HandShallHold(), OpenHand(), CountedAsNothing(), GuardShallStand(),
    ];

    public static IReadOnlyList<CardData> NisabaReedCards() =>
        [.. Lines.Select(ReviseCard), ReviseTheLastLine()];

    public static EffectProgram<EnemyActionContext>? NisabaIntent(string enemyId, string intentId) =>
        enemyId != NisabaEnemyId ? null : intentId switch
        {
            "set_the_reed" => new EffectProgram<EnemyActionContext>(
                Seq(Hit(16), Debuff(Cards.Keywords.Paperwork, 1))),
            "measure_the_boundary" => new EffectProgram<EnemyActionContext>(
                Seq(Hit(22), Guard(14))),
            "count_the_unrevised" => CountTheUnrevised(),
            // Her one turn that costs the player no HP, and the only one: she is a scribe, not a wall, and the
            // guard on this and on Measure the Boundary is only there so the fight cannot be outraced before
            // the tablet has had its say.
            "dry_the_clay" => new EffectProgram<EnemyActionContext>(
                Seq(Guard(12), Debuff(Cards.Keywords.Doubt, 2))),
            "impress_the_seal" => ImpressTheSeal(),
            "open_the_lapis_tablet" => OpenTheLapisTablet(),
            "write_the_last_line" => WriteTheLastLine(),
            _ => null,
        };

    // ── the tablet, as faces ──────────────────────────────────────────────────────────────────────────────

    // A LINE. Its stacks are the turns left before it becomes true, which is the number the player is
    // planning against, and its name is the sentence itself.
    private static StatusData TheLine(Line line) => Face(
        LineId(line), line.Sentence,
        $"In this many turns it becomes true. {line.Ladder}", stacks: true);

    // A REVISION. Its stacks are how far the wording has been walked back — the second number the player is
    // planning against, and the reason a line and its revisions are two faces rather than one: a status
    // carries one count, and this fight asks the player to watch two.
    private static StatusData TheRevision(Line line) => Face(
        RevisedId(line), $"Revised: {line.Short}",
        $"How far this sentence has been edited. {line.Ladder}", stacks: true);

    // A SEAL. The lapis phase's one addition: this line cannot be revised for the turn it covers, and it is
    // on the tablet before that turn begins.
    private static StatusData TheSeal(Line line) => Face(
        SealedId(line), $"Sealed: {line.Short}",
        "Impressed. This line cannot be revised this turn — correct another, or wait for the seal to lift.",
        stacks: false);

    private static StatusData Face(string id, string name, string description, bool stacks) => new()
    {
        Id = id,
        NameKey = name,
        DescriptionKey = description,
        Polarity = StatusPolarity.Neutral,
        StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
        UsesStacks = stacks,
        Tags = [],
        PassiveModifiers = [],
        Triggers = [],
    };

    private static StatusData Announcement(string id, string name, string description) =>
        Face(id, name, description, stacks: false);

    private static StatusData TheLapisRecord() => Face(
        LapisRecordId, "The Lapis Record",
        "The clay is gone. She impresses a seal on the line closest to becoming true, and a sealed line "
        + "cannot be revised for the turn it covers.", stacks: false);

    private static StatusData TheLastLine() => Face(
        LastLineId, "On the Fourth Dawn, the Name of the Supplicant Shall Be Erased",
        "Dawns remaining. Unrevised it is not damage — it is erasure. Revised: massive · heavy · moderate · "
        + "at four it reads SHALL REMAIN and she becomes killable · at five it reads THE NAME OF THE KEEPER.",
        stacks: true);

    private static StatusData TheLastLineRevised() => Face(
        LastLineRevisedId, "Revised: The Last Line",
        "How far the last sentence has been edited. Four takes the erasure off you; five puts it on her.",
        stacks: true);

    // The one rule the engine had to be taught for this act (see the Core commit): a death prevention that is
    // NOT spent saving its bearer. Indelible is not a charm with charges — while the lethal wording stands
    // unresolved on the tablet she cannot be brought below 1, however many blows land in one action.
    private static StatusData Indelible() => new()
    {
        Id = IndelibleId,
        NameKey = "Indelible",
        DescriptionKey =
            "While the Last Line still says the supplicant's name will be erased, Nisaba's HP cannot fall "
            + "below 1. Revise the sentence to SHALL REMAIN and this goes.",
        // NEUTRAL, and deliberately: a boss rule is not a buff. Marked as one it would come off to any card
        // that strips an enemy's buffs, and the whole of the Last Line — the sentence that has to be
        // REWRITTEN rather than shot through — would have an ordinary cleanse as its back door.
        Polarity = StatusPolarity.Neutral,
        StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
        UsesStacks = false,
        Tags = [],
        PassiveModifiers = [],
        Triggers = [],
        DeathPrevention = new StatusDeathPreventionData(1, [], Repeating: true),
    };

    // ── what the player holds ─────────────────────────────────────────────────────────────────────────────

    private static StatusData ReedMarks() => Face(
        ReedMarksId, "Reed Marks",
        $"One a turn, {ReedMarkCeiling} at most, and one more for meeting her Counted Margin. A mark buys one "
        + "revision of one line.", stacks: true);

    private static StatusData CountedMargin() => Face(
        CountedMarginId, "Counted Margin",
        "The count she has entered for this turn. End the turn having played exactly this many cards — her "
        + "own reed sheets and rubbish not counted — and take an extra Reed Mark. Missing it costs nothing.",
        stacks: true);

    // "Three Measures Shall Be Withheld", once it is true. Taken at the START of the turn it was written
    // about, because a pool that is refilled at that moment cannot be robbed the turn before.
    private static StatusData MeasuresWithheld() => new()
    {
        Id = MeasuresWithheldId,
        NameKey = "Measures Withheld",
        DescriptionKey = "This much Energy is taken from the start of your turn, once.",
        Polarity = StatusPolarity.Debuff,
        StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
        UsesStacks = true,
        Tags = [],
        PassiveModifiers = [],
        Triggers =
        [
            Trigger(new EffectProgram<TurnStartedTriggeredEffectContext>(
                new CausalSequenceEffectNode<TurnStartedTriggeredEffectContext>(
                [
                    new LoseResourceNode<TurnStartedTriggeredEffectContext>(
                        CombatantTargetSelectors.Source, StandardCombatIds.EnergyResource,
                        new CombatantStatusStacksExpression<TurnStartedTriggeredEffectContext>(
                            CombatantTargetSelectors.Source, new StatusDefinitionId(MeasuresWithheldId))),
                    new RemoveStatusNode<TurnStartedTriggeredEffectContext>(
                        CombatantTargetSelectors.Source, new StatusDefinitionId(MeasuresWithheldId)),
                ])), nameof(TriggerEvent.TurnStarted)),
        ],
    };

    private static StatusData HandShallHold() => Worn(
        HandShallHoldId, "The Hand Holds Two",
        "You draw one card fewer for each of these, this turn.",
        StatusPolarity.Debuff,
        new PassiveModifierData(PassiveModifierPipeline.TurnStartDraw, PassiveModifierOperation.AddPerStack, -1,
            RestrictDamageKind: null));

    private static StatusData OpenHand() => Worn(
        OpenHandId, "The Open Hand",
        "The sentence was turned round: you draw one card more this turn.",
        StatusPolarity.Buff,
        new PassiveModifierData(PassiveModifierPipeline.TurnStartDraw, PassiveModifierOperation.AddPerStack, 1,
            RestrictDamageKind: null));

    private static StatusData CountedAsNothing() => Worn(
        CountedAsNothingId, "Counted as Nothing",
        "Every Block you gain this turn is worth 6 less for each of these.",
        StatusPolarity.Debuff,
        new PassiveModifierData(PassiveModifierPipeline.BlockGain, PassiveModifierOperation.AddPerStack, -6,
            RestrictDamageKind: null));

    private static StatusData GuardShallStand() => Worn(
        GuardShallStandId, "The Guard Shall Stand",
        "The sentence was turned round: every Block you gain this turn is worth 6 more.",
        StatusPolarity.Buff,
        new PassiveModifierData(PassiveModifierPipeline.BlockGain, PassiveModifierOperation.AddPerStack, 6,
            RestrictDamageKind: null));

    // A sentence's mark on the turn it was written about, and no longer: it comes off at the end of that
    // turn whether it was felt or not.
    private static StatusData Worn(
        string id, string name, string description, StatusPolarity polarity, PassiveModifierData modifier) => new()
    {
        Id = id,
        NameKey = name,
        DescriptionKey = description,
        Polarity = polarity,
        StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
        UsesStacks = true,
        Tags = [],
        PassiveModifiers = [modifier],
        Triggers =
        [
            Trigger(new EffectProgram<TurnEndedTriggeredEffectContext>(
                new RemoveStatusNode<TurnEndedTriggeredEffectContext>(
                    CombatantTargetSelectors.Source, new StatusDefinitionId(id))),
                nameof(TriggerEvent.TurnEnded)),
        ],
    };

    // ── the rule ──────────────────────────────────────────────────────────────────────────────────────────

    public static StatusData TheFirstTablet() => new()
    {
        Id = FirstTabletId,
        NameKey = "The First Tablet",
        DescriptionKey =
            "Three sentences about your future stand beside her, each with the turns left before it becomes "
            + "true. You cannot dispel one. You edit it: a Reed Mark buys one revision, and a revised "
            + "sentence comes true in smaller words.",
        Polarity = StatusPolarity.Neutral,
        StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
        UsesStacks = false,
        Tags = [],
        PassiveModifiers = [],
        Triggers =
        [
            Trigger(OpenTheTurn(), nameof(TriggerEvent.TurnStarted), StatusTriggerScope.Anywhere),
            Trigger(CountTheTurnsCards(), nameof(TriggerEvent.CardPlayed), StatusTriggerScope.Anywhere),
            Trigger(TurnEnds(), nameof(TriggerEvent.TurnEnded), StatusTriggerScope.Anywhere),
            Trigger(TheAnnouncements(), nameof(TriggerEvent.DamageTaken)),
        ],
    };

    // The player's turn opens: the reed is cut, the margin is entered, the tablet is filled back to three,
    // and a sheet is laid in hand for every line that can still be corrected.
    private static EffectProgram<TurnStartedTriggeredEffectContext> OpenTheTurn()
    {
        var marks = Stacks<TurnStartedTriggeredEffectContext>(Applicant, ReedMarksId);

        IEffectNode<TurnStartedTriggeredEffectContext> AMark() =>
            new ConditionalEffectNode<TurnStartedTriggeredEffectContext>(
                new ComparisonExpression<TurnStartedTriggeredEffectContext>(
                    marks, ComparisonOperator.Less, Const<TurnStartedTriggeredEffectContext>(ReedMarkCeiling)),
                new ApplyStatusNode<TurnStartedTriggeredEffectContext>(
                    Applicant, new StatusDefinitionId(ReedMarksId),
                    Const<TurnStartedTriggeredEffectContext>(1), sourceSelector: Keeper));

        // One a turn, and one more for a turn that came to exactly the count she entered. Never past four.
        var theReed = new CausalSequenceEffectNode<TurnStartedTriggeredEffectContext>(
        [
            AMark(),
            new ConditionalEffectNode<TurnStartedTriggeredEffectContext>(
                new ComparisonExpression<TurnStartedTriggeredEffectContext>(
                    new CombatantCounterExpression<TurnStartedTriggeredEffectContext>(Keeper, MarginMet),
                    ComparisonOperator.Equal, Const<TurnStartedTriggeredEffectContext>(1)),
                AMark()),
            new SetCombatantCounterNode<TurnStartedTriggeredEffectContext>(
                Keeper, MarginMet, Const<TurnStartedTriggeredEffectContext>(0), relative: false),
        ]);

        // The count she has entered for this turn: three, four or five, in a rhythm the player can learn.
        var theMargin = new CausalSequenceEffectNode<TurnStartedTriggeredEffectContext>(
        [
            new RemoveStatusNode<TurnStartedTriggeredEffectContext>(
                Applicant, new StatusDefinitionId(CountedMarginId)),
            new ApplyStatusNode<TurnStartedTriggeredEffectContext>(
                Applicant, new StatusDefinitionId(CountedMarginId),
                new AddExpression<TurnStartedTriggeredEffectContext>(
                    Const<TurnStartedTriggeredEffectContext>(3),
                    new RemainderExpression<TurnStartedTriggeredEffectContext>(
                        new RoundNumberExpression<TurnStartedTriggeredEffectContext>(),
                        Const<TurnStartedTriggeredEffectContext>(3))),
                sourceSelector: Keeper),
        ]);

        return new EffectProgram<TurnStartedTriggeredEffectContext>(
            new ConditionalEffectNode<TurnStartedTriggeredEffectContext>(
                PlayersTurn<TurnStartedTriggeredEffectContext>(),
                new CausalSequenceEffectNode<TurnStartedTriggeredEffectContext>(
                [
                    new SetCombatantCounterNode<TurnStartedTriggeredEffectContext>(
                        Keeper, CardsThisTurn, Const<TurnStartedTriggeredEffectContext>(0), relative: false),
                    theReed,
                    theMargin,
                    FillTheTablet<TurnStartedTriggeredEffectContext>(),
                    OfferTheSheets<TurnStartedTriggeredEffectContext>(),
                ])));
    }

    // THE INSCRIPTION DIRECTOR, and it is three lines of arithmetic rather than a search: each slot owns two
    // sentences and writes whichever of them it did not write last, so the tablet cannot hold a duplicate,
    // cannot crowd two extreme deadlines together, and cannot arrive at a board nobody could have answered.
    // Nothing is written at all once the Last Line has been announced: from there the fight is one sentence.
    private static IEffectNode<TContext> FillTheTablet<TContext>() where TContext : class
    {
        IEffectNode<TContext> Write(Line line) =>
            new CausalSequenceEffectNode<TContext>(
            [
                new RemoveStatusNode<TContext>(Keeper, new StatusDefinitionId(RevisedId(line))),
                new RemoveStatusNode<TContext>(Keeper, new StatusDefinitionId(SealedId(line))),
                new ApplyStatusNode<TContext>(
                    Keeper, new StatusDefinitionId(LineId(line)), Const<TContext>(line.Turns),
                    sourceSelector: Keeper),
            ]);

        var slots = Slots.Select((pair, index) =>
        {
            var a = Lines[pair.A];
            var b = Lines[pair.B];
            return (IEffectNode<TContext>)new ConditionalEffectNode<TContext>(
                new NotExpression<TContext>(
                    new OrExpression<TContext>(Has<TContext>(Keeper, LineId(a)), Has<TContext>(Keeper, LineId(b)))),
                new CausalSequenceEffectNode<TContext>(
                [
                    new ConditionalEffectNode<TContext>(
                        new ComparisonExpression<TContext>(
                            new RemainderExpression<TContext>(
                                new CombatantCounterExpression<TContext>(Keeper, SlotTurn(index)),
                                Const<TContext>(2)),
                            ComparisonOperator.Equal, Const<TContext>(0)),
                        Write(a), Write(b)),
                    new SetCombatantCounterNode<TContext>(
                        Keeper, SlotTurn(index), Const<TContext>(1), relative: true),
                ]));
        });

        return new ConditionalEffectNode<TContext>(
            new ComparisonExpression<TContext>(
                new CombatantCounterExpression<TContext>(Keeper, LastLineTaken),
                ComparisonOperator.Equal, Const<TContext>(0)),
            new CausalSequenceEffectNode<TContext>([.. slots]));
    }

    // A sheet in hand for every sentence that can still be corrected — and one for the Last Line while it
    // stands. A sealed line gets none, which is what "cannot be revised" looks like from the hand.
    private static IEffectNode<TContext> OfferTheSheets<TContext>() where TContext : class
    {
        var ordinary = Lines.Select(line => (IEffectNode<TContext>)new ConditionalEffectNode<TContext>(
            Correctable<TContext>(line),
            new CreateCardInstanceNode<TContext>(
                Applicant, new CardDefinitionId(ReviseCardId(line)), CardZone.Hand, Const<TContext>(1))));

        return new SequenceEffectNode<TContext>(
        [
            .. ordinary,
            new ConditionalEffectNode<TContext>(
                LastLineCorrectable<TContext>(),
                new CreateCardInstanceNode<TContext>(
                    Applicant, new CardDefinitionId(ReviseLastLineCardId), CardZone.Hand, Const<TContext>(1))),
        ]);
    }

    private static ICombatExpression<TContext, bool> Correctable<TContext>(Line line) where TContext : class =>
        new AndExpression<TContext>(
            Has<TContext>(Keeper, LineId(line)),
            new AndExpression<TContext>(
                new NotExpression<TContext>(Has<TContext>(Keeper, SealedId(line))),
                new ComparisonExpression<TContext>(
                    Stacks<TContext>(Keeper, RevisedId(line)), ComparisonOperator.Less, Const<TContext>(4))));

    private static ICombatExpression<TContext, bool> LastLineCorrectable<TContext>() where TContext : class =>
        new AndExpression<TContext>(
            Has<TContext>(Keeper, LastLineId),
            new ComparisonExpression<TContext>(
                Stacks<TContext>(Keeper, LastLineRevisedId), ComparisonOperator.Less, Const<TContext>(5)));

    // What the turn came to. Her own reed sheets do not count, and neither does rubbish — the margin is a
    // count of things you MEANT to do.
    private static EffectProgram<CardPlayedTriggeredEffectContext> CountTheTurnsCards() =>
        new(new ConditionalEffectNode<CardPlayedTriggeredEffectContext>(
            new AndExpression<CardPlayedTriggeredEffectContext>(
                new TargetHasStatusExpression<CardPlayedTriggeredEffectContext>(
                    CombatantTargetSelectors.Source, new StatusDefinitionId(PassiveStatuses.ApplicantId)),
                new NotExpression<CardPlayedTriggeredEffectContext>(
                    new OrExpression<CardPlayedTriggeredEffectContext>(
                        new TriggerEventSourceCardHasTagExpression<CardPlayedTriggeredEffectContext>(
                            new TagId(Cards.CardAuthoring.JunkTag)),
                        new TriggerEventSourceCardHasTagExpression<CardPlayedTriggeredEffectContext>(
                            new TagId(ReedTag))))),
            new SetCombatantCounterNode<CardPlayedTriggeredEffectContext>(
                Keeper, CardsThisTurn, Const<CardPlayedTriggeredEffectContext>(1), relative: true)));

    // ── the count runs out ────────────────────────────────────────────────────────────────────────────────

    // One trigger for both sides of the round, because both ends of it belong to the tablet: the player's
    // turn ends with the margin settled and the seals lifted, and HERS ends with every count one lower and
    // whatever reached nothing becoming true.
    private static EffectProgram<TurnEndedTriggeredEffectContext> TurnEnds()
    {
        var margin = new CausalSequenceEffectNode<TurnEndedTriggeredEffectContext>(
        [
            new ConditionalEffectNode<TurnEndedTriggeredEffectContext>(
                new AndExpression<TurnEndedTriggeredEffectContext>(
                    Has<TurnEndedTriggeredEffectContext>(Applicant, CountedMarginId),
                    new ComparisonExpression<TurnEndedTriggeredEffectContext>(
                        new CombatantCounterExpression<TurnEndedTriggeredEffectContext>(Keeper, CardsThisTurn),
                        ComparisonOperator.Equal,
                        Stacks<TurnEndedTriggeredEffectContext>(Applicant, CountedMarginId))),
                new SetCombatantCounterNode<TurnEndedTriggeredEffectContext>(
                    Keeper, MarginMet, Const<TurnEndedTriggeredEffectContext>(1), relative: false)),
            new RemoveStatusNode<TurnEndedTriggeredEffectContext>(
                Applicant, new StatusDefinitionId(CountedMarginId)),
            // The seal covered exactly the turn that has just ended.
            .. Lines.Select(line => (IEffectNode<TurnEndedTriggeredEffectContext>)
                new RemoveStatusNode<TurnEndedTriggeredEffectContext>(
                    Keeper, new StatusDefinitionId(SealedId(line)))),
        ]);

        IEffectNode<TurnEndedTriggeredEffectContext> Count(Line line) =>
            new ConditionalEffectNode<TurnEndedTriggeredEffectContext>(
                new ComparisonExpression<TurnEndedTriggeredEffectContext>(
                    Stacks<TurnEndedTriggeredEffectContext>(Keeper, LineId(line)),
                    ComparisonOperator.GreaterOrEqual, Const<TurnEndedTriggeredEffectContext>(2)),
                new ModifyStatusStacksNode<TurnEndedTriggeredEffectContext>(
                    Keeper, new StatusDefinitionId(LineId(line)), Const<TurnEndedTriggeredEffectContext>(-1)),
                new ConditionalEffectNode<TurnEndedTriggeredEffectContext>(
                    Has<TurnEndedTriggeredEffectContext>(Keeper, LineId(line)),
                    new CausalSequenceEffectNode<TurnEndedTriggeredEffectContext>(
                    [
                        Enact<TurnEndedTriggeredEffectContext>(line),
                        new RemoveStatusNode<TurnEndedTriggeredEffectContext>(
                            Keeper, new StatusDefinitionId(LineId(line))),
                        new RemoveStatusNode<TurnEndedTriggeredEffectContext>(
                            Keeper, new StatusDefinitionId(RevisedId(line))),
                        new RemoveStatusNode<TurnEndedTriggeredEffectContext>(
                            Keeper, new StatusDefinitionId(SealedId(line))),
                    ])));

        var hers = new CausalSequenceEffectNode<TurnEndedTriggeredEffectContext>(
        [
            .. Lines.Select(Count),
            TheFourthDawn(),
        ]);

        return new EffectProgram<TurnEndedTriggeredEffectContext>(
            new ConditionalEffectNode<TurnEndedTriggeredEffectContext>(
                PlayersTurn<TurnEndedTriggeredEffectContext>(), margin, hers));
    }

    // A SENTENCE BECOMING TRUE. Every one of them is the same shape — the whole sentence at revision 0, two
    // thirds at 1, a third at 2, nothing at 3, and at 4 the wording has been turned round far enough that it
    // is no longer about the supplicant at all.
    private static IEffectNode<TContext> Enact<TContext>(Line line) where TContext : class
    {
        var revisions = new MinExpression<TContext>(
            Stacks<TContext>(Keeper, RevisedId(line)), Const<TContext>(3));
        var reversed = new ComparisonExpression<TContext>(
            Stacks<TContext>(Keeper, RevisedId(line)),
            ComparisonOperator.GreaterOrEqual, Const<TContext>(4));

        // "three, less one for each revision" — the shape four of the six sentences share.
        ICombatExpression<TContext, int> Down(int from, int step) =>
            new MaxExpression<TContext>(Const<TContext>(0),
                new SubtractExpression<TContext>(
                    Const<TContext>(from),
                    new MultiplyExpression<TContext>(Const<TContext>(step), revisions)));

        IEffectNode<TContext> Written(ICombatExpression<TContext, int> amount, IEffectNode<TContext> body) =>
            new ConditionalEffectNode<TContext>(
                new ComparisonExpression<TContext>(amount, ComparisonOperator.Greater, Const<TContext>(0)), body);

        IEffectNode<TContext> Give(string statusId, ICombatExpression<TContext, int> stacks) =>
            new ApplyStatusNode<TContext>(
                Applicant, new StatusDefinitionId(statusId), stacks, sourceSelector: Keeper);

        IEffectNode<TContext> Turned(IEffectNode<TContext> body) =>
            new ConditionalEffectNode<TContext>(reversed, body);

        IEffectNode<TContext> Exhaust(CardZone from, CardZone to, int take, TagId? tag = null) =>
            new ForEachCardInZoneNode<TContext>(
                Applicant, from,
                new MoveCardToZoneNode<TContext>(Applicant, new IteratedCardExpression<TContext>(), to),
                tagFilter: tag, takeFirst: take);

        switch (line.Slug)
        {
            case "body_shall_bear":
            {
                var loss = Down(36, 12);
                return new CausalSequenceEffectNode<TContext>(
                [
                    Written(loss, new DealDamageNode<TContext>(
                        Applicant, loss, ignoresBlock: true, kind: DamageKind.DamageOverTime)),
                    // "The final revision may reverse subject or meaning." It is still written down; it is
                    // simply no longer written about you.
                    Turned(new DealDamageNode<TContext>(
                        Keeper, Const<TContext>(18), ignoresBlock: true, kind: DamageKind.DamageOverTime)),
                ]);
            }
            case "measures_withheld":
            {
                var withheld = Down(3, 1);
                return new CausalSequenceEffectNode<TContext>(
                [
                    Written(withheld, Give(MeasuresWithheldId, withheld)),
                    // ADAPTATION: the master's "+1 Energy" is given INTO A REFILLED POOL and buys nothing at
                    // all (the shared Act-IV lesson). What it means is one more card out of the turn, so it
                    // is given in the grammar this game already owns for that.
                    Turned(Give(Relics.ActFourEventRelicRules.SpareId, Const<TContext>(1))),
                ]);
            }
            case "hand_shall_hold_two":
            {
                var fewer = Down(3, 1);
                return new CausalSequenceEffectNode<TContext>(
                [
                    Written(fewer, Give(HandShallHoldId, fewer)),
                    Turned(Give(OpenHandId, Const<TContext>(1))),
                ]);
            }
            case "guard_counted_nothing":
            {
                var counted = Down(3, 1);
                return new CausalSequenceEffectNode<TContext>(
                [
                    Written(counted, Give(CountedAsNothingId, counted)),
                    Turned(Give(GuardShallStandId, Const<TContext>(1))),
                ]);
            }
            case "three_wounds":
            {
                var sheets = Down(3, 1);
                return new CausalSequenceEffectNode<TContext>(
                [
                    Written(sheets, new CausalSequenceEffectNode<TContext>(
                    [
                        new CreateCardInstanceNode<TContext>(
                            Applicant, new CardDefinitionId("red_tape"), CardZone.DrawPile, sheets),
                        Give(Cards.Keywords.JunkFiled, sheets),
                    ])),
                    Turned(Exhaust(CardZone.DrawPile, CardZone.ExhaustPile, 1,
                        new TagId(Cards.CardAuthoring.JunkTag))),
                ]);
            }
            default:
            {
                // Two Works Shall Be Broken. A count that has to be a LITERAL (how many cards the walk takes)
                // rather than an expression, so the two severities are two branches instead of one sum.
                var revised = Stacks<TContext>(Keeper, RevisedId(line));
                return new CausalSequenceEffectNode<TContext>(
                [
                    new ConditionalEffectNode<TContext>(
                        new ComparisonExpression<TContext>(revised, ComparisonOperator.Equal, Const<TContext>(0)),
                        Exhaust(CardZone.DrawPile, CardZone.ExhaustPile, 2),
                        new ConditionalEffectNode<TContext>(
                            new ComparisonExpression<TContext>(
                                revised, ComparisonOperator.Equal, Const<TContext>(1)),
                            Exhaust(CardZone.DrawPile, CardZone.ExhaustPile, 1))),
                    Turned(Exhaust(CardZone.ExhaustPile, CardZone.DrawPile, 1)),
                ]);
            }
        }
    }

    // THE FOURTH DAWN. One dawn off the count, and when there are none left the sentence is read as it now
    // stands — erasure at nothing revised, and less and less of it after that.
    private static IEffectNode<TurnEndedTriggeredEffectContext> TheFourthDawn()
    {
        var revised = Stacks<TurnEndedTriggeredEffectContext>(Keeper, LastLineRevisedId);

        ICombatExpression<TurnEndedTriggeredEffectContext, bool> At(int level) =>
            new ComparisonExpression<TurnEndedTriggeredEffectContext>(
                revised, ComparisonOperator.Equal, Const<TurnEndedTriggeredEffectContext>(level));

        IEffectNode<TurnEndedTriggeredEffectContext> Loss(int amount) =>
            new DealDamageNode<TurnEndedTriggeredEffectContext>(
                Applicant, Const<TurnEndedTriggeredEffectContext>(amount),
                ignoresBlock: true, kind: DamageKind.DamageOverTime);

        // Revision 0 is not damage. It is the name coming off the record, which no Block and no ward answers.
        var erased = new DealDamageNode<TurnEndedTriggeredEffectContext>(
            Applicant, new CombatantCurrentHealthExpression<TurnEndedTriggeredEffectContext>(Applicant),
            ignoresBlock: true, kind: DamageKind.DamageOverTime);

        var read = new ConditionalEffectNode<TurnEndedTriggeredEffectContext>(
            At(0), erased,
            new ConditionalEffectNode<TurnEndedTriggeredEffectContext>(
                At(1), Loss(45),
                new ConditionalEffectNode<TurnEndedTriggeredEffectContext>(
                    At(2), Loss(30),
                    new ConditionalEffectNode<TurnEndedTriggeredEffectContext>(
                        At(3), Loss(18)))));

        return new ConditionalEffectNode<TurnEndedTriggeredEffectContext>(
            new ComparisonExpression<TurnEndedTriggeredEffectContext>(
                Stacks<TurnEndedTriggeredEffectContext>(Keeper, LastLineId),
                ComparisonOperator.GreaterOrEqual, Const<TurnEndedTriggeredEffectContext>(2)),
            new ModifyStatusStacksNode<TurnEndedTriggeredEffectContext>(
                Keeper, new StatusDefinitionId(LastLineId), Const<TurnEndedTriggeredEffectContext>(-1)),
            new ConditionalEffectNode<TurnEndedTriggeredEffectContext>(
                Has<TurnEndedTriggeredEffectContext>(Keeper, LastLineId),
                new CausalSequenceEffectNode<TurnEndedTriggeredEffectContext>(
                [
                    read,
                    new RemoveStatusNode<TurnEndedTriggeredEffectContext>(
                        Keeper, new StatusDefinitionId(LastLineId)),
                    new RemoveStatusNode<TurnEndedTriggeredEffectContext>(
                        Keeper, new StatusDefinitionId(IndelibleId)),
                ])));
    }

    // Her own blood announces both later phases, one turn before the intent that carries them out.
    private static EffectProgram<DamageReceivedTriggeredEffectContext> TheAnnouncements()
    {
        var health = new CombatantCurrentHealthExpression<DamageReceivedTriggeredEffectContext>(Keeper);

        IEffectNode<DamageReceivedTriggeredEffectContext> Announce(int band, CounterId taken, string marker) =>
            new ConditionalEffectNode<DamageReceivedTriggeredEffectContext>(
                new AndExpression<DamageReceivedTriggeredEffectContext>(
                    new ComparisonExpression<DamageReceivedTriggeredEffectContext>(
                        health, ComparisonOperator.LessOrEqual,
                        Const<DamageReceivedTriggeredEffectContext>(band)),
                    new ComparisonExpression<DamageReceivedTriggeredEffectContext>(
                        new CombatantCounterExpression<DamageReceivedTriggeredEffectContext>(Keeper, taken),
                        ComparisonOperator.Equal, Const<DamageReceivedTriggeredEffectContext>(0))),
                new CausalSequenceEffectNode<DamageReceivedTriggeredEffectContext>(
                [
                    new SetCombatantCounterNode<DamageReceivedTriggeredEffectContext>(
                        Keeper, taken, Const<DamageReceivedTriggeredEffectContext>(1), relative: false),
                    new ApplyStatusNode<DamageReceivedTriggeredEffectContext>(
                        Keeper, new StatusDefinitionId(marker),
                        Const<DamageReceivedTriggeredEffectContext>(1), sourceSelector: Keeper),
                ]));

        return new EffectProgram<DamageReceivedTriggeredEffectContext>(
            new CausalSequenceEffectNode<DamageReceivedTriggeredEffectContext>(
            [
                Announce(LapisAt, LapisTaken, LapisAnnouncedId),
                Announce(LastLineAt, LastLineTaken, LastLineAnnouncedId),
            ]));
    }

    // ── her own hand ──────────────────────────────────────────────────────────────────────────────────────

    // "Count the Unrevised" — ten for every sentence on the tablet the player has not touched. Written as
    // arithmetic rather than as branches: a line's presence is min(stacks, 1), and "untouched" is the same
    // reading of its revisions, subtracted from one. A tablet answered in full still costs the ten she would
    // have taken for one line, because the reed is in her hand either way.
    private static EffectProgram<EnemyActionContext> CountTheUnrevised()
    {
        ICombatExpression<EnemyActionContext, int> Unrevised(Line line) =>
            new MultiplyExpression<EnemyActionContext>(
                new MinExpression<EnemyActionContext>(
                    Stacks<EnemyActionContext>(Keeper, LineId(line)), Const<EnemyActionContext>(1)),
                new SubtractExpression<EnemyActionContext>(
                    Const<EnemyActionContext>(1),
                    new MinExpression<EnemyActionContext>(
                        Stacks<EnemyActionContext>(Keeper, RevisedId(line)), Const<EnemyActionContext>(1))));

        var standing = Lines.Select(Unrevised)
            .Aggregate((a, b) => new AddExpression<EnemyActionContext>(a, b));

        return new EffectProgram<EnemyActionContext>(
            new DealDamageNode<EnemyActionContext>(
                Applicant,
                new MaxExpression<EnemyActionContext>(
                    Const<EnemyActionContext>(10),
                    new MultiplyExpression<EnemyActionContext>(Const<EnemyActionContext>(10), standing))));
    }

    // "Impress the Seal". In the clay phase it is only the reed's blunt end. Once the record is lapis it also
    // closes the line CLOSEST to becoming true — the one the player most needed this turn for — and says so
    // before the turn it covers, which is the whole of the phase's question.
    private static EffectProgram<EnemyActionContext> ImpressTheSeal()
    {
        IEffectNode<EnemyActionContext> SealIf(Line line, int countdown) =>
            new ConditionalEffectNode<EnemyActionContext>(
                new AndExpression<EnemyActionContext>(
                    new ComparisonExpression<EnemyActionContext>(
                        new CombatantCounterExpression<EnemyActionContext>(Keeper, SealPlaced),
                        ComparisonOperator.Equal, Const<EnemyActionContext>(0)),
                    new ComparisonExpression<EnemyActionContext>(
                        Stacks<EnemyActionContext>(Keeper, LineId(line)),
                        ComparisonOperator.Equal, Const<EnemyActionContext>(countdown))),
                new CausalSequenceEffectNode<EnemyActionContext>(
                [
                    new ApplyStatusNode<EnemyActionContext>(
                        Keeper, new StatusDefinitionId(SealedId(line)), Const<EnemyActionContext>(1),
                        sourceSelector: Keeper),
                    new SetCombatantCounterNode<EnemyActionContext>(
                        Keeper, SealPlaced, Const<EnemyActionContext>(1), relative: false),
                ]));

        var passes = new List<IEffectNode<EnemyActionContext>>
        {
            new SetCombatantCounterNode<EnemyActionContext>(
                Keeper, SealPlaced, Const<EnemyActionContext>(0), relative: false),
        };
        // From TWO, not from one. A line whose count is already at one becomes true at the end of this very
        // window, so sealing it binds nothing: what the seal is for is the line the player would otherwise
        // have corrected NEXT turn, which is the smallest count that survives the turn it is placed in.
        for (var countdown = 2; countdown <= 3; countdown++)
            passes.AddRange(Lines.Select(line => SealIf(line, countdown)));

        return new EffectProgram<EnemyActionContext>(
            new CausalSequenceEffectNode<EnemyActionContext>(
            [
                Hit(18),
                new ConditionalEffectNode<EnemyActionContext>(
                    Has<EnemyActionContext>(Keeper, LapisRecordId),
                    new CausalSequenceEffectNode<EnemyActionContext>([.. passes])),
            ]));
    }

    // "Wet earth accepts correction. Then let us write upon something less forgiving."
    private static EffectProgram<EnemyActionContext> OpenTheLapisTablet() =>
        new(new CausalSequenceEffectNode<EnemyActionContext>(
        [
            Hit(24),
            new ApplyStatusNode<EnemyActionContext>(
                Keeper, new StatusDefinitionId(LapisRecordId), Const<EnemyActionContext>(1),
                sourceSelector: Keeper),
            new RemoveStatusNode<EnemyActionContext>(Keeper, new StatusDefinitionId(LapisAnnouncedId)),
        ]));

    // "Write the Last Line". Every ordinary sentence is closed — deliberately, so the finale is about ONE —
    // and what replaces them cannot be survived by outlasting it, only by rewriting it.
    private static EffectProgram<EnemyActionContext> WriteTheLastLine() =>
        new(new CausalSequenceEffectNode<EnemyActionContext>(
        [
            Hit(20),
            .. Lines.SelectMany(line => new IEffectNode<EnemyActionContext>[]
            {
                new RemoveStatusNode<EnemyActionContext>(Keeper, new StatusDefinitionId(LineId(line))),
                new RemoveStatusNode<EnemyActionContext>(Keeper, new StatusDefinitionId(RevisedId(line))),
                new RemoveStatusNode<EnemyActionContext>(Keeper, new StatusDefinitionId(SealedId(line))),
            }),
            // One MORE than the four dawns, because the count is read down at the end of the very window it
            // was written in: the player must open their next turn looking at four.
            new ApplyStatusNode<EnemyActionContext>(
                Keeper, new StatusDefinitionId(LastLineId), Const<EnemyActionContext>(LastLineRounds + 1),
                sourceSelector: Keeper),
            new ApplyStatusNode<EnemyActionContext>(
                Keeper, new StatusDefinitionId(IndelibleId), Const<EnemyActionContext>(1),
                sourceSelector: Keeper),
            new RemoveStatusNode<EnemyActionContext>(Keeper, new StatusDefinitionId(LastLineAnnouncedId)),
        ]));

    // ── the reed, as cards ────────────────────────────────────────────────────────────────────────────────

    // One sheet per correctable line, laid in hand at the turn's start. Playing it spends a Reed Mark and
    // walks the sentence back one step — and lays the next sheet, so a turn with marks to spare can correct
    // the same line twice. It costs no Energy: the fight's currency is the reed, not the pool.
    private static CardData ReviseCard(Line line) => new()
    {
        Id = ReviseCardId(line),
        NameKey = $"Revise: {line.Short}",
        DescriptionKey =
            $"Spend a Reed Mark to edit this sentence one step. {line.Ladder}",
        Costs = [],
        Tags = [new TagId(ReedTag), new TagId(Cards.CardAuthoring.TemporaryTag)],
        Program = new EffectProgram<CardPlayContext>(
            new ConditionalEffectNode<CardPlayContext>(
                new AndExpression<CardPlayContext>(HasAMark<CardPlayContext>(), Correctable<CardPlayContext>(line)),
                new CausalSequenceEffectNode<CardPlayContext>(
                [
                    SpendAMark<CardPlayContext>(),
                    new ApplyStatusNode<CardPlayContext>(
                        Keeper, new StatusDefinitionId(RevisedId(line)), Const<CardPlayContext>(1),
                        sourceSelector: Keeper),
                    new ConditionalEffectNode<CardPlayContext>(
                        new AndExpression<CardPlayContext>(
                            HasAMark<CardPlayContext>(), Correctable<CardPlayContext>(line)),
                        new CreateCardInstanceNode<CardPlayContext>(
                            Applicant, new CardDefinitionId(ReviseCardId(line)), CardZone.Hand,
                            Const<CardPlayContext>(1))),
                ]))),
        PlayedCardDestinationZone = CardZone.ExhaustPile,
        TurnEndHandDestinationZone = CardZone.ExhaustPile,
    };

    // The same sheet for the only sentence left, and it is the one that reaches past four. The fourth
    // revision takes the erasure off the supplicant AND the Indelible off her, immediately, so a player who
    // has already solved the sentence is not made to stand through the rest of the countdown. The fifth
    // turns the sentence round, and the tablet is not particular about whose name is on it.
    private static CardData ReviseTheLastLine() => new()
    {
        Id = ReviseLastLineCardId,
        NameKey = "Revise: The Last Line",
        DescriptionKey =
            "Spend a Reed Mark to edit the last sentence one step. At four it reads THE NAME OF THE "
            + "SUPPLICANT SHALL REMAIN and she can be killed; at five it reads THE NAME OF THE KEEPER SHALL "
            + "BE ERASED, and it is read at once.",
        Costs = [],
        Tags = [new TagId(ReedTag), new TagId(Cards.CardAuthoring.TemporaryTag)],
        Program = new EffectProgram<CardPlayContext>(
            new ConditionalEffectNode<CardPlayContext>(
                new AndExpression<CardPlayContext>(HasAMark<CardPlayContext>(), LastLineCorrectable<CardPlayContext>()),
                new CausalSequenceEffectNode<CardPlayContext>(
                [
                    SpendAMark<CardPlayContext>(),
                    new ApplyStatusNode<CardPlayContext>(
                        Keeper, new StatusDefinitionId(LastLineRevisedId), Const<CardPlayContext>(1),
                        sourceSelector: Keeper),
                    new ConditionalEffectNode<CardPlayContext>(
                        new ComparisonExpression<CardPlayContext>(
                            Stacks<CardPlayContext>(Keeper, LastLineRevisedId),
                            ComparisonOperator.GreaterOrEqual, Const<CardPlayContext>(4)),
                        new RemoveStatusNode<CardPlayContext>(Keeper, new StatusDefinitionId(IndelibleId))),
                    new ConditionalEffectNode<CardPlayContext>(
                        new ComparisonExpression<CardPlayContext>(
                            Stacks<CardPlayContext>(Keeper, LastLineRevisedId),
                            ComparisonOperator.GreaterOrEqual, Const<CardPlayContext>(5)),
                        new CausalSequenceEffectNode<CardPlayContext>(
                        [
                            new RemoveStatusNode<CardPlayContext>(Keeper, new StatusDefinitionId(LastLineId)),
                            new DealDamageNode<CardPlayContext>(
                                Keeper, new CombatantCurrentHealthExpression<CardPlayContext>(Keeper),
                                ignoresBlock: true, kind: DamageKind.DamageOverTime),
                        ])),
                    new ConditionalEffectNode<CardPlayContext>(
                        new AndExpression<CardPlayContext>(
                            HasAMark<CardPlayContext>(), LastLineCorrectable<CardPlayContext>()),
                        new CreateCardInstanceNode<CardPlayContext>(
                            Applicant, new CardDefinitionId(ReviseLastLineCardId), CardZone.Hand,
                            Const<CardPlayContext>(1))),
                ]))),
        PlayedCardDestinationZone = CardZone.ExhaustPile,
        TurnEndHandDestinationZone = CardZone.ExhaustPile,
    };

    private static ICombatExpression<TContext, bool> HasAMark<TContext>() where TContext : class =>
        new ComparisonExpression<TContext>(
            Stacks<TContext>(Applicant, ReedMarksId), ComparisonOperator.GreaterOrEqual, Const<TContext>(1));

    private static IEffectNode<TContext> SpendAMark<TContext>() where TContext : class =>
        new ModifyStatusStacksNode<TContext>(
            Applicant, new StatusDefinitionId(ReedMarksId), Const<TContext>(-1));

    // ── shared idioms ─────────────────────────────────────────────────────────────────────────────────────

    private static ConstantExpression<TContext> Const<TContext>(int value) where TContext : class => new(value);

    private static ICombatExpression<TContext, int> Stacks<TContext>(
        ICombatantTargetSelector body, string statusId) where TContext : class =>
        new CombatantStatusStacksExpression<TContext>(body, new StatusDefinitionId(statusId));

    private static ICombatExpression<TContext, bool> Has<TContext>(
        ICombatantTargetSelector body, string statusId) where TContext : class =>
        new TargetHasStatusExpression<TContext>(body, new StatusDefinitionId(statusId));

    // "The body whose rule this is" — the one combatant wearing the tablet.
    private static ICombatantTargetSelector Bearer(string ruleId) =>
        CombatantTargetSelectors.FirstTarget(
            CombatantTargetSelectors.WithStatus(
                CombatantTargetSelectors.AllAliveCombatants, new StatusDefinitionId(ruleId)));

    private static ICombatantTargetSelector Applicant { get; } =
        CombatantTargetSelectors.FirstTarget(
            CombatantTargetSelectors.WithStatus(
                CombatantTargetSelectors.AllAliveCombatants,
                new StatusDefinitionId(PassiveStatuses.ApplicantId)));

    private static ICombatExpression<TContext, bool> PlayersTurn<TContext>() where TContext : class =>
        new TargetHasStatusExpression<TContext>(
            CombatantTargetSelectors.Source, new StatusDefinitionId(PassiveStatuses.ApplicantId));

    private static IEffectNode<EnemyActionContext> Seq(params IEffectNode<EnemyActionContext>[] steps) =>
        new CausalSequenceEffectNode<EnemyActionContext>(steps);

    private static IEffectNode<EnemyActionContext> Hit(int damage) =>
        new DealDamageNode<EnemyActionContext>(Applicant, Const<EnemyActionContext>(damage));

    private static IEffectNode<EnemyActionContext> Guard(int block) =>
        new GainBlockNode<EnemyActionContext>(
            CombatantTargetSelectors.Source, Const<EnemyActionContext>(block));

    private static IEffectNode<EnemyActionContext> Debuff(string statusId, int stacks) =>
        new ApplyStatusNode<EnemyActionContext>(
            Applicant, new StatusDefinitionId(statusId), Const<EnemyActionContext>(stacks));

    private static StatusTriggerData Trigger<TContext>(
        EffectProgram<TContext> program, string trigger,
        StatusTriggerScope scope = StatusTriggerScope.Bearer) where TContext : class =>
        new(trigger, JsonSerializer.SerializeToElement(program, CombatJson.CreateOptions<TContext>()), scope);
}
