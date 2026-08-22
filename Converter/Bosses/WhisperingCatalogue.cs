using System.Text.Json;
using RogueDeck.Core.Combat;
using RogueDeck.Sandbox.Composition;
using RogueDeck.Scenario.Authoring;
using BnbContent.Converter.Cards;

namespace BnbContent.Converter.Bosses;

// ── The Whispering Catalogue (Act II boss, 258 HP) ────────────────────────────────────────────────────────
//
// "The Catalogue describes what the player is becoming."
//
// At the end of every player turn it files a Turn Record — how fast you played, what you opened with, whether
// you answered the archive. On your next draw it says out loud what you will do again. Confirm the prediction
// and it gains Authority; contradict it and it gains a Contradiction and bleeds for having been wrong.
//
//   3 Authority      → the confirmed habit becomes an ESTABLISHED ENTRY: a standing rule made out of your own
//                      behaviour, which then taxes you for repeating it.
//   3 Contradictions → the Entry is suspended for a turn and the Catalogue bleeds.
//
// Two Entries established (or 129 HP) and it Speaks in Full: Phase II predicts you TWICE a turn. At 64 HP it
// prepares the Final Entry, where confirming costs Paperwork and contradicting costs it 8 HP a time — the
// player's last choice is between obeying the record of themselves or violently falsifying it.
//
// The ledger lives on the PLAYER, because it is the player's cards and the player's plays it is about; the
// Authority and Contradictions live on the Catalogue as visible stacks, because they are what it has on you.
// Deviations: ADAPTATIONS.md.
public static class WhisperingCatalogue
{
    public const string EnemyId = "whispering_catalogue_boss";

    // On the Catalogue.
    public const string TheCatalogueId = "the_whispering_catalogue";
    public const string AuthorityId = "catalogue_authority";
    public const string ContradictionId = "catalogue_contradiction";
    public const string SpeaksInFullId = "the_catalogue_speaks_in_full";
    public const string CompleteDescriptionId = "the_complete_description";
    public const string FinalEntryId = "final_entry";
    public const string EntrySuspendedId = "entry_suspended";
    public const string DescribedTwiceId = "you_have_been_described";

    // The five Established Entries. At most one stands at a time.
    public const string EntryBusyId = "established_tempo_busy";
    public const string EntrySparseId = "established_tempo_sparse";
    public const string EntryOpeningId = "established_opening";
    public const string EntryComplianceId = "established_compliance";
    public const string EntryDamagedId = "established_damaged_record";

    // The Whispered Predictions.
    public const string PredictHasteId = "you_will_again_act_in_haste";
    public const string PredictSparinglyId = "you_will_again_proceed_sparingly";
    public const string PredictViolenceId = "you_will_open_with_violence";
    public const string PredictProcedureId = "you_will_repeat_the_procedure";
    public const string PredictRecitationId = "you_will_begin_with_ceremony";
    public const string PredictAnswerId = "you_will_again_answer_the_archive";
    public const string PredictDamagedId = "you_will_again_use_the_damaged_record";

    // On the player.
    public const string CatalogueRulesId = "whispering_catalogue_rules";
    public const string CatalogueReferenceId = "whispering_catalogue_reference";
    public const string CatalogueReferenceMark = "referenced_by_the_whispering_catalogue";

    // ── The ledger ────────────────────────────────────────────────────────────────────────────────────────
    //
    // Everything the record is made of is counted on the PLAYER, so the rules that watch a play can write it
    // with `Self` and the Catalogue's intents can read it with `Across`. Both sides name the same numbers.
    private static readonly CounterId OpeningCounter = new("catalogue_opening");          // 0 none 1 Deed 2 Working 3 other
    private static readonly CounterId ReferencesMetCounter = new("catalogue_references_met");
    private static readonly CounterId RedactedPlayedCounter = new("catalogue_redacted_played");

    // The Turn Record itself: last completed turn only.
    private static readonly CounterId RecordTempoCounter = new("catalogue_record_tempo");     // 1 busy 0 sparse
    private static readonly CounterId RecordOpeningCounter = new("catalogue_record_opening");
    private static readonly CounterId RecordReferenceCounter = new("catalogue_record_reference");
    private static readonly CounterId RecordRedactedCounter = new("catalogue_record_redacted");
    private static readonly CounterId HasRecordCounter = new("catalogue_has_record");

    private static readonly CounterId ReferenceDueCounter = new("catalogue_reference_due");
    private static readonly CounterId LastConfirmedCounter = new("catalogue_last_confirmed");
    private static readonly CounterId ConfirmedLastTurnCounter = new("catalogue_confirmed_last_turn");
    private static readonly CounterId ConfirmedThisTurnCounter = new("catalogue_confirmed_this_turn");
    private static readonly CounterId AuthorityGainedCounter = new("catalogue_authority_gained");
    private static readonly CounterId NoAuthorityCounter = new("catalogue_no_authority");
    private static readonly CounterId LastEntryCounter = new("catalogue_last_entry");        // 1..5, see Entries
    private static readonly CounterId EntryUsedCounter = new("catalogue_entry_used");
    private static readonly CounterId BeatCounter = new("catalogue_beat");

    // On the Catalogue: how many Entries it has ever established, which is what calls Phase II.
    private static readonly CounterId HistoryCounter = new("catalogue_history");
    private static readonly CounterId FinalEntrySpentCounter = new("catalogue_final_entry_spent");

    public const int AuthorityMaximum = 3;
    public const int ContradictionMaximum = 3;
    public const int HistoryForPhaseTwo = 2;
    public const int PhaseTwoHealth = 129;
    public const int FinalEntryHealth = 64;
    public const int BusyTempo = 3;

    // In a solo boss fight each side's lowest-health enemy is simply the other side — so ONE selector reads
    // "across the table" from whichever end the program is running on, and the ledger has a single spelling.
    private static readonly ICombatantTargetSelector Self = CombatantTargetSelectors.Source;
    private static readonly ICombatantTargetSelector Across = CombatantTargetSelectors.LowestHealthEnemyOfSource;

    // ── Content ───────────────────────────────────────────────────────────────────────────────────────────

    public static IEnumerable<StatusData> Statuses() =>
    [
        Marker(TheCatalogueId, "The Whispering Catalogue",
            "It is compiling an entry about you."),
        Stacking(AuthorityId, "Authority",
            "What the Catalogue has established about you. At 3 it becomes a standing Entry."),
        Stacking(ContradictionId, "Contradiction",
            "Where the record was wrong. At 3 the standing Entry is suspended and the Catalogue bleeds."),
        Marker(SpeaksInFullId, "The Catalogue Speaks in Full",
            "Its next action is the complete description."),
        Marker(CompleteDescriptionId, "The Complete Description",
            "It now predicts you twice a turn."),
        Marker(FinalEntryId, "Final Entry",
            "Confirm and be described; contradict and tear the page."),
        Marker(EntrySuspendedId, "Entry Suspended",
            "The established Entry does not apply this turn."),
        Marker(DescribedTwiceId, "You Have Been Described",
            "The established Entry applies twice this turn."),

        Marker(EntryBusyId, "Established Tempo — Busy",
            "Your third card each turn is cited."),
        Marker(EntrySparseId, "Established Tempo — Sparse",
            "End a turn on 2 cards or fewer and the Catalogue gains 10 Block."),
        Marker(EntryOpeningId, "Established Opening",
            "Open as the record says and the Catalogue gains 8 Block; the card is redacted afterwards."),
        Marker(EntryComplianceId, "Established Compliance",
            "The first citation you answer each turn gives the Catalogue 8 Block."),
        Marker(EntryDamagedId, "Established Damaged Record",
            "The first redacted card you play each turn gives the Catalogue 1 Authority."),

        Prediction(PredictHasteId, "You Will Again Act in Haste", "You will play at least 3 cards."),
        Prediction(PredictSparinglyId, "You Will Again Proceed Sparingly", "You will play no more than 2 cards."),
        Prediction(PredictViolenceId, "You Will Open With Violence", "Your first card will be a Deed."),
        Prediction(PredictProcedureId, "You Will Repeat the Procedure", "Your first card will be a Working."),
        Prediction(PredictRecitationId, "You Will Begin With Ceremony", "Your first card will be a Rite."),
        Prediction(PredictAnswerId, "You Will Again Answer the Archive", "You will answer a citation."),
        Prediction(PredictDamagedId, "You Will Again Use the Damaged Record", "You will play a redacted card."),

        // The Catalogue's own citation. It is never cited automatically — only "Open a New Entry" and the Busy
        // Entry issue one — so the cite hook spends the pending count instead of marking every draw.
        ActTwo.Reference(CatalogueReferenceId, "Catalogue Reference", CatalogueReferenceMark,
            "The Catalogue has cited this card. Play it and the entry is answered.",
            cite: CiteWhatIsDue(),
            onFulfilled: OnReferenceAnswered()),

        Rules(),
        BossState(),
    ];

    // ── The Turn Record ───────────────────────────────────────────────────────────────────────────────────
    //
    // Written at the END of the player's turn, which is the only moment that knows the whole turn. The engine
    // keeps its own per-turn play stats and resets them on the combatant's TURN START, so at the player's
    // TurnEnded the count of cards played this turn is still standing and can simply be read.
    //
    // ADAPTATION: the design's Opening categories are Attack / Skill / Power-Other; this game's card taxonomy
    // is Deed / Working / Rite / Junk, so the Opening is recorded as Deed / Working / Rite-or-other and Junk
    // is skipped exactly as the design says.
    private static StatusData Rules()
    {
        var onDraw = new EffectProgram<CardsDrawnTriggeredEffectContext>(
            new ConditionalEffectNode<CardsDrawnTriggeredEffectContext>(
                IsTheApplicant<CardsDrawnTriggeredEffectContext>(),
                PreparePredictions()));

        var onPlay = new EffectProgram<CardPlayedTriggeredEffectContext>(
            new ConditionalEffectNode<CardPlayedTriggeredEffectContext>(
                IsTheApplicant<CardPlayedTriggeredEffectContext>(),
                new CausalSequenceEffectNode<CardPlayedTriggeredEffectContext>(
                [
                    RecordTheOpening(),
                    NoteRedacted(),
                    EstablishedBusy(),
                    EstablishedDamagedRecord(),
                ])));

        var onTurnEnded = new EffectProgram<TurnEndedTriggeredEffectContext>(
            new ConditionalEffectNode<TurnEndedTriggeredEffectContext>(
                IsTheApplicant<TurnEndedTriggeredEffectContext>(),
                new CausalSequenceEffectNode<TurnEndedTriggeredEffectContext>(
                [
                    // A suspension bought by the LAST turn's contradictions has now been served. Lifting it
                    // before this turn's contradictions are counted is what lets the same turn impose a fresh
                    // one without immediately cancelling it.
                    Across_Remove<TurnEndedTriggeredEffectContext>(EntrySuspendedId),
                    Across_Remove<TurnEndedTriggeredEffectContext>(DescribedTwiceId),
                    EstablishedSparse(),
                    ResolvePredictions(),
                    SettleAuthority(),
                    SettleContradictions(),
                    Across_Remove<TurnEndedTriggeredEffectContext>(FinalEntryId),
                    WriteTheRecord(),
                    ClearTurnLedger(),
                ])));

        return Rule(CatalogueRulesId, "The Compilation",
            "Everything you do is filed. What you do twice becomes a rule about you.",
            [
                Watch("CardsDrawn", onDraw),
                Watch("CardPlayed", onPlay),
                Watch("TurnEnded", onTurnEnded),
            ]);
    }

    // "The type of the first non-Junk card." Only the first one counts, so the write is gated on the slot
    // still being empty; a Junk card never fills it.
    private static IEffectNode<CardPlayedTriggeredEffectContext> RecordTheOpening()
    {
        IEffectNode<CardPlayedTriggeredEffectContext> Note(string tag, int value) =>
            new ConditionalEffectNode<CardPlayedTriggeredEffectContext>(
                PlayedCardHasTag(tag),
                SetOn<CardPlayedTriggeredEffectContext>(Self, OpeningCounter, value));

        return new ConditionalEffectNode<CardPlayedTriggeredEffectContext>(
            new AndExpression<CardPlayedTriggeredEffectContext>(
                IsZero<CardPlayedTriggeredEffectContext>(Self, OpeningCounter),
                new NotExpression<CardPlayedTriggeredEffectContext>(PlayedCardHasTag(CardAuthoring.JunkTag))),
            new CausalSequenceEffectNode<CardPlayedTriggeredEffectContext>(
            [
                // Anything that is neither Deed nor Working is filed as the third category, so a card with no
                // taxonomy tag at all still opens the turn rather than leaving the record blank.
                SetOn<CardPlayedTriggeredEffectContext>(Self, OpeningCounter, 3),
                Note(CardAuthoring.DeedTag, 1),
                Note(CardAuthoring.WorkingTag, 2),
                EstablishedOpening(),
            ]));
    }

    private static IEffectNode<CardPlayedTriggeredEffectContext> NoteRedacted() =>
        new ConditionalEffectNode<CardPlayedTriggeredEffectContext>(
            new CardInstanceHasMarkExpression<CardPlayedTriggeredEffectContext>(
                new TriggerEventCardInstanceExpression<CardPlayedTriggeredEffectContext>(),
                new TagId(ActTwo.RedactedMark)),
            Bump<CardPlayedTriggeredEffectContext>(Self, RedactedPlayedCounter, 1));

    // ── The Established Entries ────────────────────────────────────────────────────────────────────────────
    //
    // Each one is a standing rule made out of a habit the Catalogue proved you have, and each fires at most
    // once a player turn — twice while "You Have Been Described" stands, which is exactly what that intent
    // buys. A suspended Entry does nothing at all.
    private static IEffectNode<TContext> WhileEntryStands<TContext>(
        string entryId, IEffectNode<TContext> effect) where TContext : class =>
        new ConditionalEffectNode<TContext>(
            new AndExpression<TContext>(
                AcrossHas<TContext>(entryId),
                new AndExpression<TContext>(
                    new NotExpression<TContext>(AcrossHas<TContext>(EntrySuspendedId)),
                    StillAllowed<TContext>())),
            new CausalSequenceEffectNode<TContext>(
            [
                effect,
                Bump<TContext>(Self, EntryUsedCounter, 1),
            ]));

    // One use a turn, or two while "You Have Been Described" stands — which is exactly what that intent buys.
    // The latch is a single counter because only one Entry can ever stand, so there is never a second Entry
    // to confuse it with.
    private static ICombatExpression<TContext, bool> StillAllowed<TContext>() where TContext : class =>
        new OrExpression<TContext>(
            UsedFewerThan<TContext>(1),
            new AndExpression<TContext>(AcrossHas<TContext>(DescribedTwiceId), UsedFewerThan<TContext>(2)));

    private static ICombatExpression<TContext, bool> UsedFewerThan<TContext>(int uses) where TContext : class =>
        new ComparisonExpression<TContext>(
            new CombatantCounterExpression<TContext>(Self, EntryUsedCounter),
            ComparisonOperator.Less, new ConstantExpression<TContext>(uses));

    // "The first time each player turn the player plays a third card: Reference one remaining valid hand card."
    private static IEffectNode<CardPlayedTriggeredEffectContext> EstablishedBusy() =>
        new ConditionalEffectNode<CardPlayedTriggeredEffectContext>(
            new ComparisonExpression<CardPlayedTriggeredEffectContext>(
                new CardsPlayedThisTurnExpression<CardPlayedTriggeredEffectContext>(Self),
                ComparisonOperator.GreaterOrEqual, new ConstantExpression<CardPlayedTriggeredEffectContext>(BusyTempo)),
            WhileEntryStands(EntryBusyId, CiteOneInHand<CardPlayedTriggeredEffectContext>()));

    // "If the player ends the turn with no more than 2 cards played: Catalogue gains 10 Block."
    private static IEffectNode<TurnEndedTriggeredEffectContext> EstablishedSparse() =>
        new ConditionalEffectNode<TurnEndedTriggeredEffectContext>(
            new ComparisonExpression<TurnEndedTriggeredEffectContext>(
                new CardsPlayedThisTurnExpression<TurnEndedTriggeredEffectContext>(Self),
                ComparisonOperator.LessOrEqual, new ConstantExpression<TurnEndedTriggeredEffectContext>(2)),
            WhileEntryStands(EntrySparseId, BlockAcross<TurnEndedTriggeredEffectContext>(10)));

    // "If the first non-Junk card matches the Recorded Opening type: 8 Block, and the card becomes Redacted."
    // Called from inside RecordTheOpening, where the slot has just been filled with this turn's opening.
    private static IEffectNode<CardPlayedTriggeredEffectContext> EstablishedOpening() =>
        new ConditionalEffectNode<CardPlayedTriggeredEffectContext>(
            new ComparisonExpression<CardPlayedTriggeredEffectContext>(
                new CombatantCounterExpression<CardPlayedTriggeredEffectContext>(Self, OpeningCounter),
                ComparisonOperator.Equal,
                new CombatantCounterExpression<CardPlayedTriggeredEffectContext>(Self, RecordOpeningCounter)),
            WhileEntryStands(EntryOpeningId,
                new CausalSequenceEffectNode<CardPlayedTriggeredEffectContext>(
                [
                    BlockAcross<CardPlayedTriggeredEffectContext>(8),
                    new MarkCardInstanceNode<CardPlayedTriggeredEffectContext>(
                        Self, new TriggerEventCardInstanceExpression<CardPlayedTriggeredEffectContext>(),
                        new TagId(ActTwo.RedactedMark)),
                ])));

    // "The first Redacted card played each turn: Catalogue gains 1 Authority."
    private static IEffectNode<CardPlayedTriggeredEffectContext> EstablishedDamagedRecord() =>
        new ConditionalEffectNode<CardPlayedTriggeredEffectContext>(
            new CardInstanceHasMarkExpression<CardPlayedTriggeredEffectContext>(
                new TriggerEventCardInstanceExpression<CardPlayedTriggeredEffectContext>(),
                new TagId(ActTwo.RedactedMark)),
            WhileEntryStands(EntryDamagedId, GainAuthority<CardPlayedTriggeredEffectContext>(1)));

    // "The first Catalogue Reference fulfilled each player turn: Catalogue gains 8 Block." The answer also
    // feeds the record — this is the Archive Conduct the profile is made of.
    private static IEffectNode<CardPlayedTriggeredEffectContext> OnReferenceAnswered() =>
        new CausalSequenceEffectNode<CardPlayedTriggeredEffectContext>(
        [
            Bump<CardPlayedTriggeredEffectContext>(Self, ReferencesMetCounter, 1),
            WhileEntryStands(EntryComplianceId, BlockAcross<CardPlayedTriggeredEffectContext>(8)),
        ]);

    // ── Whispered Predictions ─────────────────────────────────────────────────────────────────────────────
    //
    // "After the next normal draw, choose one prediction derived from the previous Turn Record." The record
    // decides WHICH predictions are legal; a beat counter decides which of the legal families is spoken, so
    // the Catalogue does not simply say the same thing every turn and "Reclassify the Evidence" has something
    // to replace.
    //
    // ADAPTATION: the design also requires both branches to be currently achievable. Tempo always is; an
    // Opening or Conduct prediction is only prepared when the record HAS that habit, and otherwise the beat
    // falls back to tempo. The finer "is a non-Attack opening still possible from this hand" test has no
    // engine question behind it and is not attempted.
    private static IEffectNode<CardsDrawnTriggeredEffectContext> PreparePredictions() =>
        new ConditionalEffectNode<CardsDrawnTriggeredEffectContext>(
            new ComparisonExpression<CardsDrawnTriggeredEffectContext>(
                new CombatantCounterExpression<CardsDrawnTriggeredEffectContext>(Self, HasRecordCounter),
                ComparisonOperator.Greater, new ConstantExpression<CardsDrawnTriggeredEffectContext>(0)),
            new CausalSequenceEffectNode<CardsDrawnTriggeredEffectContext>(
            [
                SpeakFamily<CardsDrawnTriggeredEffectContext>(Self, Across, 0),
                // "Present 2 compatible Predictions derived from the last Turn Record." The second is chosen
                // to be a DIFFERENT reading of the same record rather than the next beat, which could land on
                // the family the first one already fell back to and merge into a single prediction.
                new ConditionalEffectNode<CardsDrawnTriggeredEffectContext>(
                    AcrossHas<CardsDrawnTriggeredEffectContext>(CompleteDescriptionId),
                    SpeakSecondReading<CardsDrawnTriggeredEffectContext>(Self, Across)),
            ]));

    // The family at `offset` beats from now: 0 tempo, 1 opening, 2 conduct — each falling back to tempo when
    // the record holds no such habit. `ledger` is whoever carries the Turn Record (always the player) and
    // `book` is the Catalogue; naming both is what lets the boss speak this from its own side of the table.
    private static IEffectNode<TContext> SpeakFamily<TContext>(
        ICombatantTargetSelector ledger, ICombatantTargetSelector book, int offset) where TContext : class
    {
        var family = new RemainderExpression<TContext>(
            new AddExpression<TContext>(
                new CombatantCounterExpression<TContext>(ledger, BeatCounter),
                new ConstantExpression<TContext>(offset)),
            new ConstantExpression<TContext>(3));

        return new ConditionalEffectNode<TContext>(
            new ComparisonExpression<TContext>(family, ComparisonOperator.Equal, new ConstantExpression<TContext>(1)),
            OpeningPrediction<TContext>(ledger, book),
            @else: new ConditionalEffectNode<TContext>(
                new ComparisonExpression<TContext>(family, ComparisonOperator.Equal, new ConstantExpression<TContext>(2)),
                ConductPrediction<TContext>(ledger, book),
                @else: TempoPrediction<TContext>(ledger, book)));
    }

    // The other reading of the same record. Which family the FIRST prediction really came from decides this:
    // a tempo primary is joined by whichever habit the record actually holds, and an Opening or Conduct
    // primary is joined by the tempo, which is the one reading a record always supports. When the record
    // holds nothing but its tempo there is only one honest thing to say, and only one is said.
    private static IEffectNode<TContext> SpeakSecondReading<TContext>(
        ICombatantTargetSelector ledger, ICombatantTargetSelector book) where TContext : class
    {
        var family = new RemainderExpression<TContext>(
            new CombatantCounterExpression<TContext>(ledger, BeatCounter),
            new ConstantExpression<TContext>(3));

        IEffectNode<TContext> IfOpening(IEffectNode<TContext> then, IEffectNode<TContext> otherwise) =>
            new ConditionalEffectNode<TContext>(HasOpening<TContext>(ledger), then, @else: otherwise);

        IEffectNode<TContext> IfConduct(IEffectNode<TContext> then, IEffectNode<TContext> otherwise) =>
            new ConditionalEffectNode<TContext>(HasConduct<TContext>(ledger), then, @else: otherwise);

        var nothing = new NoOpEffectNode<TContext>();
        var tempo = TempoPrediction<TContext>(ledger, book);
        var opening = OpeningPrediction<TContext>(ledger, book);
        var conduct = ConductPrediction<TContext>(ledger, book);

        return new ConditionalEffectNode<TContext>(
            new ComparisonExpression<TContext>(family, ComparisonOperator.Equal, new ConstantExpression<TContext>(1)),
            // The first prediction was the Opening one — unless the record had no opening, in which case it
            // fell back to the tempo and the Conduct is what is left to say.
            IfOpening(tempo, IfConduct(conduct, nothing)),
            @else: new ConditionalEffectNode<TContext>(
                new ComparisonExpression<TContext>(family, ComparisonOperator.Equal, new ConstantExpression<TContext>(2)),
                IfConduct(tempo, IfOpening(opening, nothing)),
                // The first prediction was the tempo, so the second is whatever habit the record holds.
                @else: IfOpening(opening, IfConduct(conduct, nothing))));
    }

    private static ICombatExpression<TContext, bool> HasOpening<TContext>(ICombatantTargetSelector ledger)
        where TContext : class => Positive<TContext>(ledger, RecordOpeningCounter);

    private static ICombatExpression<TContext, bool> HasConduct<TContext>(ICombatantTargetSelector ledger)
        where TContext : class =>
        new OrExpression<TContext>(
            Positive<TContext>(ledger, RecordReferenceCounter),
            Positive<TContext>(ledger, RecordRedactedCounter));

    private static IEffectNode<TContext> TempoPrediction<TContext>(
        ICombatantTargetSelector ledger, ICombatantTargetSelector book) where TContext : class =>
        new ConditionalEffectNode<TContext>(
            Positive<TContext>(ledger, RecordTempoCounter),
            Speak<TContext>(book, PredictHasteId),
            @else: Speak<TContext>(book, PredictSparinglyId));

    private static IEffectNode<TContext> OpeningPrediction<TContext>(
        ICombatantTargetSelector ledger, ICombatantTargetSelector book) where TContext : class =>
        new ConditionalEffectNode<TContext>(
            CounterIs<TContext>(ledger, RecordOpeningCounter, 1), Speak<TContext>(book, PredictViolenceId),
            @else: new ConditionalEffectNode<TContext>(
                CounterIs<TContext>(ledger, RecordOpeningCounter, 2), Speak<TContext>(book, PredictProcedureId),
                @else: new ConditionalEffectNode<TContext>(
                    CounterIs<TContext>(ledger, RecordOpeningCounter, 3), Speak<TContext>(book, PredictRecitationId),
                    @else: TempoPrediction<TContext>(ledger, book))));

    private static IEffectNode<TContext> ConductPrediction<TContext>(
        ICombatantTargetSelector ledger, ICombatantTargetSelector book) where TContext : class =>
        new ConditionalEffectNode<TContext>(
            Positive<TContext>(ledger, RecordReferenceCounter), Speak<TContext>(book, PredictAnswerId),
            @else: new ConditionalEffectNode<TContext>(
                Positive<TContext>(ledger, RecordRedactedCounter), Speak<TContext>(book, PredictDamagedId),
                @else: TempoPrediction<TContext>(ledger, book)));

    private static IEffectNode<TContext> Speak<TContext>(
        ICombatantTargetSelector book, string predictionId) where TContext : class =>
        new ForEachTargetEffectNode<TContext>(book,
            new ApplyStatusNode<TContext>(CombatantTargetSelectors.IterationTarget,
                new StatusDefinitionId(predictionId), new ConstantExpression<TContext>(1)));

    // ── Resolution ────────────────────────────────────────────────────────────────────────────────────────
    //
    // Every prediction that was spoken is answered by the turn that just happened. At most two can stand, but
    // asking all seven costs nothing and keeps each prediction's rule beside its own name.
    private static IEffectNode<TurnEndedTriggeredEffectContext> ResolvePredictions() =>
        new CausalSequenceEffectNode<TurnEndedTriggeredEffectContext>(
        [
            Judge(PredictHasteId, 1, PlayedAtLeast(BusyTempo)),
            Judge(PredictSparinglyId, 2,
                new NotExpression<TurnEndedTriggeredEffectContext>(PlayedAtLeast(BusyTempo))),
            Judge(PredictViolenceId, 3, RecordIs<TurnEndedTriggeredEffectContext>(OpeningCounter, 1)),
            Judge(PredictProcedureId, 3, RecordIs<TurnEndedTriggeredEffectContext>(OpeningCounter, 2)),
            Judge(PredictRecitationId, 3, RecordIs<TurnEndedTriggeredEffectContext>(OpeningCounter, 3)),
            Judge(PredictAnswerId, 4, RecordSays<TurnEndedTriggeredEffectContext>(ReferencesMetCounter)),
            Judge(PredictDamagedId, 5, RecordSays<TurnEndedTriggeredEffectContext>(RedactedPlayedCounter)),
        ]);

    // `entry` is the Established Entry this prediction would become if it were confirmed three times.
    private static IEffectNode<TurnEndedTriggeredEffectContext> Judge(
        string predictionId, int entry, ICombatExpression<TurnEndedTriggeredEffectContext, bool> cameTrue) =>
        new ConditionalEffectNode<TurnEndedTriggeredEffectContext>(
            AcrossHas<TurnEndedTriggeredEffectContext>(predictionId),
            new CausalSequenceEffectNode<TurnEndedTriggeredEffectContext>(
            [
                new ConditionalEffectNode<TurnEndedTriggeredEffectContext>(
                    cameTrue, Confirmed(entry), @else: Contradicted()),
                Across_Remove<TurnEndedTriggeredEffectContext>(predictionId),
            ]));

    // "If the player confirms the prediction: Catalogue gains 1 Authority." Under the Final Entry the payment
    // changes: being described costs Paperwork instead.
    private static IEffectNode<TurnEndedTriggeredEffectContext> Confirmed(int entry) =>
        new CausalSequenceEffectNode<TurnEndedTriggeredEffectContext>(
        [
            SetOn<TurnEndedTriggeredEffectContext>(Self, LastConfirmedCounter, 1),
            SetOn<TurnEndedTriggeredEffectContext>(Self, LastEntryCounter, entry),
            Bump<TurnEndedTriggeredEffectContext>(Self, ConfirmedThisTurnCounter, 1),
            new ConditionalEffectNode<TurnEndedTriggeredEffectContext>(
                AcrossHas<TurnEndedTriggeredEffectContext>(FinalEntryId),
                new ApplyStatusNode<TurnEndedTriggeredEffectContext>(
                    Self, new StatusDefinitionId(Keywords.Paperwork),
                    new ConstantExpression<TurnEndedTriggeredEffectContext>(2)),
                // "At most one confirmed Prediction per player turn may generate Authority", and a prediction
                // the Catalogue reclassified into place generates none at all.
                @else: new ConditionalEffectNode<TurnEndedTriggeredEffectContext>(
                    new AndExpression<TurnEndedTriggeredEffectContext>(
                        IsZero<TurnEndedTriggeredEffectContext>(Self, AuthorityGainedCounter),
                        IsZero<TurnEndedTriggeredEffectContext>(Self, NoAuthorityCounter)),
                    new CausalSequenceEffectNode<TurnEndedTriggeredEffectContext>(
                    [
                        GainAuthority<TurnEndedTriggeredEffectContext>(1),
                        SetOn<TurnEndedTriggeredEffectContext>(Self, AuthorityGainedCounter, 1),
                    ]))),
        ]);

    // "1 Contradiction, and lose up to 6 current Block; if no Block remains, lose 4 HP instead." Under the
    // Final Entry a falsified page costs it 8 HP outright.
    private static IEffectNode<TurnEndedTriggeredEffectContext> Contradicted() =>
        new CausalSequenceEffectNode<TurnEndedTriggeredEffectContext>(
        [
            SetOn<TurnEndedTriggeredEffectContext>(Self, LastConfirmedCounter, 0),
            new ConditionalEffectNode<TurnEndedTriggeredEffectContext>(
                AcrossHas<TurnEndedTriggeredEffectContext>(FinalEntryId),
                LoseHealthAcross<TurnEndedTriggeredEffectContext>(8),
                @else: new CausalSequenceEffectNode<TurnEndedTriggeredEffectContext>(
                [
                    new ForEachTargetEffectNode<TurnEndedTriggeredEffectContext>(Across,
                        new ApplyStatusNode<TurnEndedTriggeredEffectContext>(
                            CombatantTargetSelectors.IterationTarget, new StatusDefinitionId(ContradictionId),
                            new ConstantExpression<TurnEndedTriggeredEffectContext>(1))),
                    StripBlockOrBleed<TurnEndedTriggeredEffectContext>(6, 4),
                ])),
        ]);

    // "At 3 Authority: the most recently confirmed Prediction becomes an Established Entry; Authority → 0;
    // Established History +1." Only one Entry ever stands, so the new one clears the field first.
    private static IEffectNode<TurnEndedTriggeredEffectContext> SettleAuthority() =>
        new ConditionalEffectNode<TurnEndedTriggeredEffectContext>(
            new ComparisonExpression<TurnEndedTriggeredEffectContext>(
                AcrossStacks<TurnEndedTriggeredEffectContext>(AuthorityId),
                ComparisonOperator.GreaterOrEqual,
                new ConstantExpression<TurnEndedTriggeredEffectContext>(AuthorityMaximum)),
            new CausalSequenceEffectNode<TurnEndedTriggeredEffectContext>(
            [
                Across_Remove<TurnEndedTriggeredEffectContext>(EntryBusyId),
                Across_Remove<TurnEndedTriggeredEffectContext>(EntrySparseId),
                Across_Remove<TurnEndedTriggeredEffectContext>(EntryOpeningId),
                Across_Remove<TurnEndedTriggeredEffectContext>(EntryComplianceId),
                Across_Remove<TurnEndedTriggeredEffectContext>(EntryDamagedId),
                Establish(1, EntryBusyId),
                Establish(2, EntrySparseId),
                Establish(3, EntryOpeningId),
                Establish(4, EntryComplianceId),
                Establish(5, EntryDamagedId),
                Across_Remove<TurnEndedTriggeredEffectContext>(AuthorityId),
                new ForEachTargetEffectNode<TurnEndedTriggeredEffectContext>(Across,
                    Bump<TurnEndedTriggeredEffectContext>(
                        CombatantTargetSelectors.IterationTarget, HistoryCounter, 1)),
            ]));

    private static IEffectNode<TurnEndedTriggeredEffectContext> Establish(int slot, string entryId) =>
        new ConditionalEffectNode<TurnEndedTriggeredEffectContext>(
            RecordIs<TurnEndedTriggeredEffectContext>(LastEntryCounter, slot),
            new ForEachTargetEffectNode<TurnEndedTriggeredEffectContext>(Across,
                new ApplyStatusNode<TurnEndedTriggeredEffectContext>(
                    CombatantTargetSelectors.IterationTarget, new StatusDefinitionId(entryId),
                    new ConstantExpression<TurnEndedTriggeredEffectContext>(1))));

    // "At 3 Contradictions: reset; disable the Established Entry for the next player turn; lose 8 Block, or
    // 6 HP if none." This is the deliberate anti-pattern counterplay — being unreadable hurts it.
    private static IEffectNode<TurnEndedTriggeredEffectContext> SettleContradictions() =>
        new ConditionalEffectNode<TurnEndedTriggeredEffectContext>(
            new ComparisonExpression<TurnEndedTriggeredEffectContext>(
                AcrossStacks<TurnEndedTriggeredEffectContext>(ContradictionId),
                ComparisonOperator.GreaterOrEqual,
                new ConstantExpression<TurnEndedTriggeredEffectContext>(ContradictionMaximum)),
            new CausalSequenceEffectNode<TurnEndedTriggeredEffectContext>(
            [
                Across_Remove<TurnEndedTriggeredEffectContext>(ContradictionId),
                new ForEachTargetEffectNode<TurnEndedTriggeredEffectContext>(Across,
                    new ApplyStatusNode<TurnEndedTriggeredEffectContext>(
                        CombatantTargetSelectors.IterationTarget, new StatusDefinitionId(EntrySuspendedId),
                        new ConstantExpression<TurnEndedTriggeredEffectContext>(1))),
                StripBlockOrBleed<TurnEndedTriggeredEffectContext>(8, 6),
            ]));

    // "The Catalogue stores only the most recent completed player turn."
    private static IEffectNode<TurnEndedTriggeredEffectContext> WriteTheRecord() =>
        new CausalSequenceEffectNode<TurnEndedTriggeredEffectContext>(
        [
            new ConditionalEffectNode<TurnEndedTriggeredEffectContext>(
                PlayedAtLeast(BusyTempo),
                SetOn<TurnEndedTriggeredEffectContext>(Self, RecordTempoCounter, 1),
                @else: SetOn<TurnEndedTriggeredEffectContext>(Self, RecordTempoCounter, 0)),
            Copy(RecordOpeningCounter, OpeningCounter),
            Copy(RecordReferenceCounter, ReferencesMetCounter),
            Copy(RecordRedactedCounter, RedactedPlayedCounter),
            SetOn<TurnEndedTriggeredEffectContext>(Self, HasRecordCounter, 1),
        ]);

    private static IEffectNode<TurnEndedTriggeredEffectContext> ClearTurnLedger() =>
        new CausalSequenceEffectNode<TurnEndedTriggeredEffectContext>(
        [
            SetOn<TurnEndedTriggeredEffectContext>(Self, OpeningCounter, 0),
            SetOn<TurnEndedTriggeredEffectContext>(Self, ReferencesMetCounter, 0),
            SetOn<TurnEndedTriggeredEffectContext>(Self, RedactedPlayedCounter, 0),
            SetOn<TurnEndedTriggeredEffectContext>(Self, EntryUsedCounter, 0),
            SetOn<TurnEndedTriggeredEffectContext>(Self, AuthorityGainedCounter, 0),
            SetOn<TurnEndedTriggeredEffectContext>(Self, NoAuthorityCounter, 0),
            // What "Speak Both Entries" scales on is what the PREVIOUS turn confirmed, so this turn's tally
            // becomes last turn's on the way out.
            Copy(ConfirmedLastTurnCounter, ConfirmedThisTurnCounter),
            SetOn<TurnEndedTriggeredEffectContext>(Self, ConfirmedThisTurnCounter, 0),
            Bump<TurnEndedTriggeredEffectContext>(Self, BeatCounter, 1),
        ]);

    // ── The boss's own state ──────────────────────────────────────────────────────────────────────────────
    //
    // The transition and the Final Entry are the Catalogue's business, so they are checked at ITS turn start —
    // the one moment it acts on its own behalf.
    private static StatusData BossState()
    {
        var onTurnStarted = new EffectProgram<TurnStartedTriggeredEffectContext>(
            new CausalSequenceEffectNode<TurnStartedTriggeredEffectContext>(
            [
                // "Trigger when Established History reaches 2, or the Catalogue reaches 129 HP or less."
                new ConditionalEffectNode<TurnStartedTriggeredEffectContext>(
                    new AndExpression<TurnStartedTriggeredEffectContext>(
                        new NotExpression<TurnStartedTriggeredEffectContext>(
                            SelfHas<TurnStartedTriggeredEffectContext>(CompleteDescriptionId)),
                        new AndExpression<TurnStartedTriggeredEffectContext>(
                            new NotExpression<TurnStartedTriggeredEffectContext>(
                                SelfHas<TurnStartedTriggeredEffectContext>(SpeaksInFullId)),
                            new OrExpression<TurnStartedTriggeredEffectContext>(
                                new ComparisonExpression<TurnStartedTriggeredEffectContext>(
                                    new CombatantCounterExpression<TurnStartedTriggeredEffectContext>(Self, HistoryCounter),
                                    ComparisonOperator.GreaterOrEqual,
                                    new ConstantExpression<TurnStartedTriggeredEffectContext>(HistoryForPhaseTwo)),
                                new ComparisonExpression<TurnStartedTriggeredEffectContext>(
                                    new CombatantCurrentHealthExpression<TurnStartedTriggeredEffectContext>(Self),
                                    ComparisonOperator.LessOrEqual,
                                    new ConstantExpression<TurnStartedTriggeredEffectContext>(PhaseTwoHealth))))),
                    new ApplyStatusNode<TurnStartedTriggeredEffectContext>(
                        Self, new StatusDefinitionId(SpeaksInFullId),
                        new ConstantExpression<TurnStartedTriggeredEffectContext>(1))),
                // "At 64 HP or less, once per combat: prepare FINAL ENTRY."
                new ConditionalEffectNode<TurnStartedTriggeredEffectContext>(
                    new AndExpression<TurnStartedTriggeredEffectContext>(
                        IsZero<TurnStartedTriggeredEffectContext>(Self, FinalEntrySpentCounter),
                        new ComparisonExpression<TurnStartedTriggeredEffectContext>(
                            new CombatantCurrentHealthExpression<TurnStartedTriggeredEffectContext>(Self),
                            ComparisonOperator.LessOrEqual,
                            new ConstantExpression<TurnStartedTriggeredEffectContext>(FinalEntryHealth))),
                    new CausalSequenceEffectNode<TurnStartedTriggeredEffectContext>(
                    [
                        new ApplyStatusNode<TurnStartedTriggeredEffectContext>(
                            Self, new StatusDefinitionId(FinalEntryId),
                            new ConstantExpression<TurnStartedTriggeredEffectContext>(1)),
                        SetOn<TurnStartedTriggeredEffectContext>(Self, FinalEntrySpentCounter, 1),
                    ])),
            ]));

        return Rule(TheCatalogueId + "_state", "The Compiler",
            "It stops describing you one entry at a time when it has enough of them.",
            [Watch("TurnStarted", onTurnStarted)]);
    }

    // ── Intents ───────────────────────────────────────────────────────────────────────────────────────────
    //
    // Five slots, each of which reads as its Phase-I move, its Phase-II move, or — for exactly one action —
    // the transition. The design's cooldowns of 2 and 3 intents are satisfied by the cycle itself: a
    // five-slot rotation brings any single slot round again only every fifth action.
    public static EffectProgram<EnemyActionContext>? Intent(string intentId) => intentId switch
    {
        "open_a_new_entry" => Phases(
            // Open a New Entry: a citation on the next hand, and 12 Block.
            new CausalSequenceEffectNode<EnemyActionContext>([CiteLater(1), Block(12)]),
            // Bind Conduct to Record: up to 2 citations, and 8 Block.
            new CausalSequenceEffectNode<EnemyActionContext>([CiteLater(2), Block(8)])),

        "compare_with_prior_conduct" => Phases(
            // Compare With Prior Conduct: 14, or 18 if the last prediction came true.
            new ConditionalEffectNode<EnemyActionContext>(
                Positive<EnemyActionContext>(Across, LastConfirmedCounter),
                Damage(18), @else: Damage(14)),
            // Speak Both Entries: 8 twice, and one hit gains +3 per prediction confirmed last turn — 22 at
            // the ceiling, which is what two confirmations reach.
            new CausalSequenceEffectNode<EnemyActionContext>(
            [
                new DealDamageNode<EnemyActionContext>(Across,
                    new MinExpression<EnemyActionContext>(
                        new AddExpression<EnemyActionContext>(
                            new ConstantExpression<EnemyActionContext>(8),
                            new MultiplyExpression<EnemyActionContext>(
                                new ConstantExpression<EnemyActionContext>(3),
                                new CombatantCounterExpression<EnemyActionContext>(Across, ConfirmedLastTurnCounter))),
                        new ConstantExpression<EnemyActionContext>(14))),
                Damage(8),
            ])),

        "index_the_deviation" => Phases(
            // Index the Deviation: 12 and 1 Doubt.
            new CausalSequenceEffectNode<EnemyActionContext>([Damage(12), ApplyToPlayer(Keywords.Doubt, 1)]),
            // Correct the Contradiction: it buys back up to 2 of its own errors, one Authority each. With no
            // Contradiction to correct the move is ineligible, and a cycled intent with nothing to do does
            // nothing — the same answer a card played into an empty board gets everywhere in this act.
            CorrectTheContradiction()),

        "publish_the_preliminary_finding" => Phases(
            // Publish the Preliminary Finding: 14 + 3 per Authority, capped at 23.
            new DealDamageNode<EnemyActionContext>(Across, ScaledByAuthority(14, 3, 23)),
            // The Record Is Now Complete: 16 + 4 per Authority, capped at 28, and it spends the lot.
            new CausalSequenceEffectNode<EnemyActionContext>(
            [
                new DealDamageNode<EnemyActionContext>(Across, ScaledByAuthority(16, 4, 28)),
                new RemoveStatusNode<EnemyActionContext>(Self, new StatusDefinitionId(AuthorityId)),
            ])),

        "reclassify_the_evidence" => Phases(
            // Reclassify the Evidence: the standing prediction is withdrawn and another one derived from the
            // same record is put in its place — which cannot earn Authority this turn.
            new CausalSequenceEffectNode<EnemyActionContext>([Reclassify(), Block(10)]),
            // You Have Been Described: the Entry applies twice next turn.
            new CausalSequenceEffectNode<EnemyActionContext>(
            [
                new ApplyStatusNode<EnemyActionContext>(Self, new StatusDefinitionId(DescribedTwiceId),
                    new ConstantExpression<EnemyActionContext>(1)),
                Block(12),
            ])),

        _ => null,
    };

    // One slot, two phases — plus the single action that turns one into the other. "Remove current Block;
    // gain 14; preserve the Established Entry; Authority → 1; Contradictions remain; no additional attack."
    private static EffectProgram<EnemyActionContext> Phases(
        IEffectNode<EnemyActionContext> compilation, IEffectNode<EnemyActionContext> completeDescription) =>
        new(new ConditionalEffectNode<EnemyActionContext>(
            SelfHas<EnemyActionContext>(SpeaksInFullId),
            new CausalSequenceEffectNode<EnemyActionContext>(
            [
                new ModifyDefensivePoolNode<EnemyActionContext>(Self, StandardCombatIds.BlockDefensivePool,
                    new SubtractExpression<EnemyActionContext>(
                        new ConstantExpression<EnemyActionContext>(0), SelfBlock<EnemyActionContext>())),
                Block(14),
                new RemoveStatusNode<EnemyActionContext>(Self, new StatusDefinitionId(AuthorityId)),
                new ApplyStatusNode<EnemyActionContext>(Self, new StatusDefinitionId(AuthorityId),
                    new ConstantExpression<EnemyActionContext>(1)),
                new RemoveStatusNode<EnemyActionContext>(Self, new StatusDefinitionId(SpeaksInFullId)),
                new ApplyStatusNode<EnemyActionContext>(Self, new StatusDefinitionId(CompleteDescriptionId),
                    new ConstantExpression<EnemyActionContext>(1)),
            ]),
            @else: new ConditionalEffectNode<EnemyActionContext>(
                SelfHas<EnemyActionContext>(CompleteDescriptionId), completeDescription, @else: compilation)));

    private static ICombatExpression<EnemyActionContext, int> ScaledByAuthority(int flat, int per, int ceiling) =>
        new MinExpression<EnemyActionContext>(
            new AddExpression<EnemyActionContext>(
                new ConstantExpression<EnemyActionContext>(flat),
                new MultiplyExpression<EnemyActionContext>(
                    new ConstantExpression<EnemyActionContext>(per),
                    new CombatantStatusStacksExpression<EnemyActionContext>(
                        Self, new StatusDefinitionId(AuthorityId)))),
            new ConstantExpression<EnemyActionContext>(ceiling));

    private static IEffectNode<EnemyActionContext> CorrectTheContradiction() =>
        new ConditionalEffectNode<EnemyActionContext>(
            new ComparisonExpression<EnemyActionContext>(
                new CombatantStatusStacksExpression<EnemyActionContext>(Self, new StatusDefinitionId(ContradictionId)),
                ComparisonOperator.Greater, new ConstantExpression<EnemyActionContext>(0)),
            new CausalSequenceEffectNode<EnemyActionContext>(
            [
                new ModifyStatusStacksNode<EnemyActionContext>(Self, new StatusDefinitionId(ContradictionId),
                    new SubtractExpression<EnemyActionContext>(
                        new ConstantExpression<EnemyActionContext>(0), Corrected())),
                new ApplyStatusNode<EnemyActionContext>(Self, new StatusDefinitionId(AuthorityId), Corrected()),
            ]));

    // "Remove up to 2 Contradictions. For each removed: gain 1 Authority."
    private static ICombatExpression<EnemyActionContext, int> Corrected() =>
        new MinExpression<EnemyActionContext>(
            new ConstantExpression<EnemyActionContext>(2),
            new CombatantStatusStacksExpression<EnemyActionContext>(Self, new StatusDefinitionId(ContradictionId)));

    // The withdrawn prediction goes and the next family speaks instead — and the replacement is barred from
    // earning Authority this turn, which is the price of the Catalogue changing its mind.
    private static IEffectNode<EnemyActionContext> Reclassify() =>
        new CausalSequenceEffectNode<EnemyActionContext>(
        [
            .. Predictions.Select(id =>
                (IEffectNode<EnemyActionContext>)new RemoveStatusNode<EnemyActionContext>(
                    Self, new StatusDefinitionId(id))),
            Bump<EnemyActionContext>(Across, BeatCounter, 1),
            SetOn<EnemyActionContext>(Across, NoAuthorityCounter, 1),
            // The Catalogue speaks from its own side of the table, so the ledger it reads is across from it —
            // which is the same ledger the player's rules write.
            SpeakFamily<EnemyActionContext>(Across, Self, 0),
        ]);

    public static readonly string[] Predictions =
    [
        PredictHasteId, PredictSparinglyId, PredictViolenceId, PredictProcedureId,
        PredictRecitationId, PredictAnswerId, PredictDamagedId,
    ];

    // ── Citations ─────────────────────────────────────────────────────────────────────────────────────────
    //
    // "After next normal draw, issue N Catalogue References." The intent only records the debt; the citing
    // happens on the player's next draw, which is the beat every Act-II citation uses — a card cited during
    // the enemy's turn is a card about to be discarded.
    private static IEffectNode<EnemyActionContext> CiteLater(int count) =>
        Bump<EnemyActionContext>(Across, ReferenceDueCounter, count);

    private static IEffectNode<CardsDrawnTriggeredEffectContext> CiteWhatIsDue() =>
        new ConditionalEffectNode<CardsDrawnTriggeredEffectContext>(
            new AndExpression<CardsDrawnTriggeredEffectContext>(
                IsTheApplicant<CardsDrawnTriggeredEffectContext>(),
                new ComparisonExpression<CardsDrawnTriggeredEffectContext>(
                    new CombatantCounterExpression<CardsDrawnTriggeredEffectContext>(Self, ReferenceDueCounter),
                    ComparisonOperator.Greater, new ConstantExpression<CardsDrawnTriggeredEffectContext>(0))),
            new CausalSequenceEffectNode<CardsDrawnTriggeredEffectContext>(
            [
                new ForEachCardInZoneNode<CardsDrawnTriggeredEffectContext>(
                    Self, CardZone.Hand,
                    new MarkCardInstanceNode<CardsDrawnTriggeredEffectContext>(
                        Self, new IteratedCardExpression<CardsDrawnTriggeredEffectContext>(),
                        new TagId(CatalogueReferenceMark)),
                    takeFirst: 2),
                SetOn<CardsDrawnTriggeredEffectContext>(Self, ReferenceDueCounter, 0),
            ]));

    private static IEffectNode<TContext> CiteOneInHand<TContext>() where TContext : class =>
        new ForEachCardInZoneNode<TContext>(
            Self, CardZone.Hand,
            new MarkCardInstanceNode<TContext>(
                Self, new IteratedCardExpression<TContext>(), new TagId(CatalogueReferenceMark)),
            takeFirst: 1);

    // ── Small shared pieces ───────────────────────────────────────────────────────────────────────────────

    // "Lose up to N Block; if no Block remains, lose M HP instead."
    private static IEffectNode<TContext> StripBlockOrBleed<TContext>(int block, int health) where TContext : class =>
        new ConditionalEffectNode<TContext>(
            new ComparisonExpression<TContext>(
                AcrossBlock<TContext>(), ComparisonOperator.Greater, new ConstantExpression<TContext>(0)),
            new ForEachTargetEffectNode<TContext>(Across,
                new ModifyDefensivePoolNode<TContext>(
                    CombatantTargetSelectors.IterationTarget, StandardCombatIds.BlockDefensivePool,
                    new SubtractExpression<TContext>(
                        new ConstantExpression<TContext>(0),
                        new MinExpression<TContext>(
                            new ConstantExpression<TContext>(block), AcrossBlock<TContext>())))),
            @else: LoseHealthAcross<TContext>(health));

    // A direct loss of health, which no Block, damage modifier or damage-taken reaction can see — the same
    // spelling the Bell's Toll uses for its own 5 HP.
    private static IEffectNode<TContext> LoseHealthAcross<TContext>(int amount) where TContext : class =>
        new ForEachTargetEffectNode<TContext>(Across,
            new SetHealthNode<TContext>(
                CombatantTargetSelectors.IterationTarget,
                new SubtractExpression<TContext>(
                    new CombatantCurrentHealthExpression<TContext>(CombatantTargetSelectors.IterationTarget),
                    new ConstantExpression<TContext>(amount))));

    private static IEffectNode<TContext> GainAuthority<TContext>(int stacks) where TContext : class =>
        new ForEachTargetEffectNode<TContext>(Across,
            new ApplyStatusNode<TContext>(
                CombatantTargetSelectors.IterationTarget, new StatusDefinitionId(AuthorityId),
                new MinExpression<TContext>(
                    new ConstantExpression<TContext>(stacks),
                    new SubtractExpression<TContext>(
                        new ConstantExpression<TContext>(AuthorityMaximum),
                        new IterationTargetStatusStacksExpression<TContext>(new StatusDefinitionId(AuthorityId))))));

    private static IEffectNode<TContext> BlockAcross<TContext>(int amount) where TContext : class =>
        new ForEachTargetEffectNode<TContext>(Across,
            new GainBlockNode<TContext>(
                CombatantTargetSelectors.IterationTarget, new ConstantExpression<TContext>(amount)));

    private static IEffectNode<EnemyActionContext> Block(int amount) =>
        new GainBlockNode<EnemyActionContext>(Self, new ConstantExpression<EnemyActionContext>(amount));

    private static IEffectNode<EnemyActionContext> Damage(int amount) =>
        new DealDamageNode<EnemyActionContext>(Across, new ConstantExpression<EnemyActionContext>(amount));

    private static IEffectNode<EnemyActionContext> ApplyToPlayer(string status, int stacks) =>
        new ApplyStatusNode<EnemyActionContext>(Across, new StatusDefinitionId(status),
            new ConstantExpression<EnemyActionContext>(stacks));

    private static ICombatExpression<TContext, int> AcrossBlock<TContext>() where TContext : class =>
        new CombatantDefensivePoolExpression<TContext>(Across, StandardCombatIds.BlockDefensivePool);

    private static ICombatExpression<TContext, int> SelfBlock<TContext>() where TContext : class =>
        new CombatantDefensivePoolExpression<TContext>(Self, StandardCombatIds.BlockDefensivePool);

    private static ICombatExpression<TContext, int> AcrossStacks<TContext>(string statusId) where TContext : class =>
        new CombatantStatusStacksExpression<TContext>(Across, new StatusDefinitionId(statusId));

    private static ICombatExpression<TContext, bool> AcrossHas<TContext>(string statusId) where TContext : class =>
        new TargetHasStatusExpression<TContext>(Across, new StatusDefinitionId(statusId));

    private static ICombatExpression<TContext, bool> SelfHas<TContext>(string statusId) where TContext : class =>
        new TargetHasStatusExpression<TContext>(Self, new StatusDefinitionId(statusId));

    private static IEffectNode<TContext> Across_Remove<TContext>(string statusId) where TContext : class =>
        new ForEachTargetEffectNode<TContext>(Across,
            new RemoveStatusNode<TContext>(
                CombatantTargetSelectors.IterationTarget, new StatusDefinitionId(statusId)));

    private static ICombatExpression<TurnEndedTriggeredEffectContext, bool> PlayedAtLeast(int cards) =>
        new ComparisonExpression<TurnEndedTriggeredEffectContext>(
            new CardsPlayedThisTurnExpression<TurnEndedTriggeredEffectContext>(Self),
            ComparisonOperator.GreaterOrEqual, new ConstantExpression<TurnEndedTriggeredEffectContext>(cards));

    private static ICombatExpression<CardPlayedTriggeredEffectContext, bool> PlayedCardHasTag(string tag) =>
        new CardInstanceHasTagExpression<CardPlayedTriggeredEffectContext>(
            new TriggerEventCardInstanceExpression<CardPlayedTriggeredEffectContext>(), new TagId(tag));

    private static ICombatExpression<TContext, bool> RecordSays<TContext>(CounterId counter)
        where TContext : class => Positive<TContext>(Self, counter);

    private static ICombatExpression<TContext, bool> RecordIs<TContext>(CounterId counter, int value)
        where TContext : class => CounterIs<TContext>(Self, counter, value);

    private static ICombatExpression<TContext, bool> Positive<TContext>(
        ICombatantTargetSelector on, CounterId counter) where TContext : class =>
        new ComparisonExpression<TContext>(
            new CombatantCounterExpression<TContext>(on, counter),
            ComparisonOperator.Greater, new ConstantExpression<TContext>(0));

    private static ICombatExpression<TContext, bool> CounterIs<TContext>(
        ICombatantTargetSelector on, CounterId counter, int value) where TContext : class =>
        new ComparisonExpression<TContext>(
            new CombatantCounterExpression<TContext>(on, counter),
            ComparisonOperator.Equal, new ConstantExpression<TContext>(value));

    private static ICombatExpression<TContext, bool> IsZero<TContext>(
        ICombatantTargetSelector on, CounterId counter) where TContext : class =>
        new ComparisonExpression<TContext>(
            new CombatantCounterExpression<TContext>(on, counter),
            ComparisonOperator.Equal, new ConstantExpression<TContext>(0));

    private static IEffectNode<TContext> SetOn<TContext>(
        ICombatantTargetSelector on, CounterId counter, int value) where TContext : class =>
        new SetCombatantCounterNode<TContext>(on, counter, new ConstantExpression<TContext>(value), relative: false);

    private static IEffectNode<TContext> Bump<TContext>(
        ICombatantTargetSelector on, CounterId counter, int delta) where TContext : class =>
        new SetCombatantCounterNode<TContext>(on, counter, new ConstantExpression<TContext>(delta), relative: true);

    private static IEffectNode<TurnEndedTriggeredEffectContext> Copy(CounterId into, CounterId from) =>
        new SetCombatantCounterNode<TurnEndedTriggeredEffectContext>(
            Self, into, new CombatantCounterExpression<TurnEndedTriggeredEffectContext>(Self, from),
            relative: false);

    private static ICombatExpression<TContext, bool> IsTheApplicant<TContext>() where TContext : class =>
        new TargetHasStatusExpression<TContext>(Self, new StatusDefinitionId(PassiveStatuses.ApplicantId));

    // Bearer scope throughout: the ledger sits on the PLAYER and watches the player's own draws, plays and
    // turn end, and the boss state sits on the CATALOGUE and watches its own turn start. Anywhere would fire
    // each program on both turns, and every one of them reads `Self` — it would file the player's habits
    // against the Catalogue's body and the Catalogue's phase against the player's.
    private static StatusTriggerData Watch<TContext>(string trigger, EffectProgram<TContext> program)
        where TContext : class =>
        new(trigger, JsonSerializer.SerializeToElement(program, CombatJson.CreateOptions<TContext>()));

    private static StatusData Prediction(string id, string name, string description) =>
        Rule(id, name, description, []);

    private static StatusData Marker(string id, string name, string description) =>
        Rule(id, name, description, []);

    private static StatusData Stacking(string id, string name, string description) => new()
    {
        Id = id,
        NameKey = name,
        DescriptionKey = description,
        Polarity = StatusPolarity.Neutral,
        StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
        UsesStacks = true,
        Tags = [],
        PassiveModifiers = [],
        Triggers = [],
    };

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
