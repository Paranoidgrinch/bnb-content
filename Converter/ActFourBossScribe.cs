using System.Text.Json;
using RogueDeck.Core.Combat;
using RogueDeck.Sandbox.Composition;
using RogueDeck.Scenario.Authoring;

namespace BnbContent.Converter;

// Act IV, boss — The First Scribe of the House of Life. An ancient scribe before an absurdly long scroll. He
// does not record what happens. It happens because he records it.
//
// So the fight has no intent you can read off his face and plan around: YOU write his next turn. The first
// three cards you play are copied into the TABLET as three entries, and at the end of his own window the
// tablet is read back at you, in order:
//
//   a DEED      6 damage.
//   a WORKING   6 Block for him, and a sheet of Paperwork for you.
//   ANYTHING    1 Strength for him — once per tablet — and Inscribed for you.
//   EMPTY       1 Doubt: nothing recorded is its own kind of wrong.
//
// Which makes the whole fight a sequencing puzzle with one escape hatch: SCRAPE AN ENTRY, offered the moment
// the first entry is written, blanks one slot for a sheet of Paperwork, once a turn. You are never stopped
// from acting — you are made to decide what the record of your acting will cost.
//
// Two complete tablets read (or 290) and the scroll is scraped and written over: the PALIMPSEST records the
// LAST three cards instead of the first, and the final entry of each tablet is INHERITED into the next one,
// where it cannot be scraped. Nothing is ever fully erased. Below 100 he announces that THE TEXT IS NOW
// CANON, and for one whole turn there is no scraping at all.
public static partial class ActFour
{
    public const string ScribeEnemyId = "first_scribe_of_the_house_of_life";

    public const string HouseOfLifeId = "the_house_of_life";
    public const string FirstEntryId = "tablet_first_entry";
    public const string SecondEntryId = "tablet_second_entry";
    public const string ThirdEntryId = "tablet_third_entry";
    public const string InheritedEntryId = "tablet_inherited_entry";
    public const string PalimpsestId = "the_palimpsest";
    public const string TextIsCanonId = "the_text_is_now_canon";

    public const string ScrapeFirstCardId = "scrape_the_first_entry";
    public const string ScrapeSecondCardId = "scrape_the_second_entry";
    public const string ScrapeThirdCardId = "scrape_the_third_entry";

    public const string TabletTag = "house_of_life_tablet";

    public const int TabletsForPalimpsest = 2;
    private const int PalimpsestAt = 290;
    private const int CanonAt = 100;
    private const int RecordedAttack = 6;
    private const int RecordedBlock = 6;
    private const int PalimpsestBlock = 16;
    private const int CanonBlow = 24;
    private const int NothingToCorrect = 12;

    // The three slots, in the order they are read.
    private static readonly string[] TabletSlots = [FirstEntryId, SecondEntryId, ThirdEntryId];

    // How much of this tablet is written, how many whole ones he has read back, whether this turn's scraping
    // is spent, and whether the once-per-tablet Strength has been taken. EntryKind is the scratch register
    // the card kind is computed into — a status' stacks can be set from a counter, but not from a branch.
    public static CounterId EntriesWritten => new("tablet_entries_written");
    public static CounterId TabletsRead => new("tablets_read");
    public static CounterId ScrapeSpent => new("scrape_spent");
    public static CounterId ScrapeBanned => new("scrape_banned");
    public static CounterId StrengthFromTablet => new("strength_from_tablet");
    public static CounterId EntryKind => new("entry_kind");
    public static CounterId LastEntry => new("last_entry_kind");
    public static CounterId LastCompleteFinal => new("last_complete_final_entry");
    public static CounterId PalimpsestTaken => new("palimpsest_taken");
    public static CounterId CanonTaken => new("canon_taken");

    public static EffectProgram<EnemyActionContext>? ScribeIntent(string enemyId, string intentId) =>
        $"{enemyId}.{intentId}" switch
        {
            "first_scribe_of_the_house_of_life.dip_the_reed" => ByPalimpsest(
                I: Seq(Debuff(InscribedId, 2)),
                II: Seq(Debuff(InscribedId, 2), Debuff(Cards.Keywords.Paperwork, 1))),
            "first_scribe_of_the_house_of_life.margin_of_arrears" => ByPalimpsest(
                I: Seq(Debuff(Cards.Keywords.Paperwork, 2), Hit(15)),
                II: Seq(Debuff(Cards.Keywords.Paperwork, 2), Hit(18))),
            "first_scribe_of_the_house_of_life.erase_the_honorific" => ByPalimpsest(
                I: Seq(EraseAnHonorific(), Hit(18)),
                II: Seq(EraseAnHonorific(), Hit(21))),
            "first_scribe_of_the_house_of_life.reed_through_flesh" => ByPalimpsest(
                I: Seq(Hit(27)),
                II: Seq(Hit(30))),
            "first_scribe_of_the_house_of_life.seal_the_scroll" => ByPalimpsest(
                I: Seq(Guard(24), Debuff(EmbalmedId, 1)),
                II: Seq(Guard(26), Debuff(EmbalmedId, 1))),
            "first_scribe_of_the_house_of_life.correct_the_margin" => CorrectTheMargin(),
            "first_scribe_of_the_house_of_life.the_text_is_now_canon" => TheTextIsNowCanon(),
            _ => null,
        };

    public static IReadOnlyList<StatusData> ScribeStatuses() =>
    [
        TheHouseOfLife(),
        Entry(FirstEntryId, "First Entry"),
        Entry(SecondEntryId, "Second Entry"),
        Entry(ThirdEntryId, "Third Entry"),
        Entry(InheritedEntryId, "Inherited Entry"),
        ThePalimpsest(),
        TheTextIsCanon(),
    ];

    public static IReadOnlyList<CardData> ScribeScrapeCards() =>
    [
        ScrapeCard(ScrapeFirstCardId, "Scrape the First Entry", FirstEntryId),
        ScrapeCard(ScrapeSecondCardId, "Scrape the Second Entry", SecondEntryId),
        ScrapeCard(ScrapeThirdCardId, "Scrape the Third Entry", ThirdEntryId),
    ];

    // ── the tablet, as four faces ─────────────────────────────────────────────────────────────────────────

    // A slot is a face on HIM carrying the KIND of what was recorded: 1 a Deed, 2 a Working, 3 anything else.
    // Absent is Empty, which is an entry like any other and reads back as Doubt.
    private static StatusData Entry(string id, string name) => new()
    {
        Id = id,
        NameKey = name,
        DescriptionKey =
            "What the scroll says you did: 1 a Deed (6 damage), 2 a Working (6 Block for him, a sheet for "
            + "you), 3 anything else (Strength for him, Inscribed for you). Nothing written is 1 Doubt.",
        Polarity = StatusPolarity.Neutral,
        StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
        UsesStacks = true,
        Tags = [],
        PassiveModifiers = [],
        Triggers = [],
    };

    private static StatusData ThePalimpsest() => new()
    {
        Id = PalimpsestId,
        NameKey = "The Palimpsest",
        DescriptionKey =
            "The scroll has been scraped and written over. He records the LAST three cards of your turn "
            + "instead of the first, and each tablet's final entry is inherited into the next — where you "
            + "cannot scrape it.",
        Polarity = StatusPolarity.Neutral,
        StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
        UsesStacks = false,
        Tags = [],
        PassiveModifiers = [],
        Triggers = [],
    };

    private static StatusData TheTextIsCanon() => new()
    {
        Id = TextIsCanonId,
        NameKey = "The Text Is Now Canon",
        DescriptionKey = "For one whole turn nothing may be scraped. Then the record is read, and read out.",
        Polarity = StatusPolarity.Neutral,
        StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
        UsesStacks = false,
        Tags = [],
        PassiveModifiers = [],
        Triggers = [],
    };

    // ── the rule ──────────────────────────────────────────────────────────────────────────────────────────

    public static StatusData TheHouseOfLife() => new()
    {
        Id = HouseOfLifeId,
        NameKey = "The House of Life",
        DescriptionKey =
            "He writes down the first three cards of your turn, and at the end of his own he reads them back "
            + "at you. One entry a turn may be scraped for a sheet of Paperwork. Two whole tablets read and "
            + "the scroll is written over.",
        Polarity = StatusPolarity.Neutral,
        StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
        UsesStacks = false,
        Tags = [],
        PassiveModifiers =
        [
        ],
        Triggers =
        [
            Trigger(OpenTheScroll(), nameof(TriggerEvent.TurnStarted), StatusTriggerScope.Anywhere),
            Trigger(WriteTheEntry(), nameof(TriggerEvent.CardPlayed), StatusTriggerScope.Anywhere),
            Trigger(ReadTheTablet(), nameof(TriggerEvent.TurnEnded), StatusTriggerScope.Anywhere),
            Trigger(TheAnnouncements(), nameof(TriggerEvent.DamageTaken)),
        ],
    };

    // The player's turn opens with a clean tablet and a scraping unspent. The scrape sheets themselves are
    // NOT laid here: there is nothing to scrape until something is written, and a hand of three dead sheets
    // every turn is three cards of clutter. They arrive the moment the first entry does.
    private static EffectProgram<TurnStartedTriggeredEffectContext> OpenTheScroll()
    {
        var scribe = Bearer(HouseOfLifeId);

        return new EffectProgram<TurnStartedTriggeredEffectContext>(
            new ConditionalEffectNode<TurnStartedTriggeredEffectContext>(
                PlayersTurn<TurnStartedTriggeredEffectContext>(),
                new CausalSequenceEffectNode<TurnStartedTriggeredEffectContext>(
                [
                    new SetCombatantCounterNode<TurnStartedTriggeredEffectContext>(
                        scribe, EntriesWritten,
                        new ConstantExpression<TurnStartedTriggeredEffectContext>(0), relative: false),
                    new SetCombatantCounterNode<TurnStartedTriggeredEffectContext>(
                        scribe, ScrapeSpent,
                        new ConstantExpression<TurnStartedTriggeredEffectContext>(0), relative: false),
                    new SetCombatantCounterNode<TurnStartedTriggeredEffectContext>(
                        scribe, LastEntry,
                        new ConstantExpression<TurnStartedTriggeredEffectContext>(0), relative: false),

                    // The canon is ANNOUNCED when his blood says so and lands on the turn after — the sheets
                    // for the turn it was announced in are already in the player's hand, and a ban that
                    // reaches back into a turn already being played is a ban nobody was told about.
                    new ConditionalEffectNode<TurnStartedTriggeredEffectContext>(
                        new ComparisonExpression<TurnStartedTriggeredEffectContext>(
                            new CombatantCounterExpression<TurnStartedTriggeredEffectContext>(
                                scribe, CanonTaken),
                            ComparisonOperator.Equal,
                            new ConstantExpression<TurnStartedTriggeredEffectContext>(1)),
                        new CausalSequenceEffectNode<TurnStartedTriggeredEffectContext>(
                        [
                            new SetCombatantCounterNode<TurnStartedTriggeredEffectContext>(
                                scribe, CanonTaken,
                                new ConstantExpression<TurnStartedTriggeredEffectContext>(2), relative: false),
                            new ApplyStatusNode<TurnStartedTriggeredEffectContext>(
                                scribe, new StatusDefinitionId(TextIsCanonId),
                                new ConstantExpression<TurnStartedTriggeredEffectContext>(1),
                                sourceSelector: scribe),
                        ])),

                    // Whether THIS turn may scrape, settled once so a mid-turn announcement cannot close a
                    // window the player was already standing in.
                    new ConditionalEffectNode<TurnStartedTriggeredEffectContext>(
                        new TargetHasStatusExpression<TurnStartedTriggeredEffectContext>(
                            scribe, new StatusDefinitionId(TextIsCanonId)),
                        new SetCombatantCounterNode<TurnStartedTriggeredEffectContext>(
                            scribe, ScrapeBanned,
                            new ConstantExpression<TurnStartedTriggeredEffectContext>(1), relative: false),
                        new SetCombatantCounterNode<TurnStartedTriggeredEffectContext>(
                            scribe, ScrapeBanned,
                            new ConstantExpression<TurnStartedTriggeredEffectContext>(0), relative: false)),
                ])));
    }

    // Every card you play is copied down. Rubbish is not writing, and neither is a scrape sheet — a sheet
    // that erased an entry must not become one.
    //
    // Phase I keeps the FIRST three. The Palimpsest keeps the last three, which is the same tablet read
    // through a window that slides: each new entry pushes the older ones up a slot and the oldest off the
    // end. Both are causal — the slot is chosen from the count this very card just made.
    private static EffectProgram<CardPlayedTriggeredEffectContext> WriteTheEntry()
    {
        var scribe = Bearer(HouseOfLifeId);
        var written = new CombatantCounterExpression<CardPlayedTriggeredEffectContext>(scribe, EntriesWritten);
        var kind = new CombatantCounterExpression<CardPlayedTriggeredEffectContext>(scribe, EntryKind);

        ICombatExpression<CardPlayedTriggeredEffectContext, bool> Is(string tag) =>
            new TriggerEventSourceCardHasTagExpression<CardPlayedTriggeredEffectContext>(new TagId(tag));

        ICombatExpression<CardPlayedTriggeredEffectContext, int> Stacks(string slotId) =>
            new CombatantStatusStacksExpression<CardPlayedTriggeredEffectContext>(
                scribe, new StatusDefinitionId(slotId));

        // A slot is rewritten, never added to: the face carries a kind, not a tally.
        IEffectNode<CardPlayedTriggeredEffectContext> Set(
            string slotId, ICombatExpression<CardPlayedTriggeredEffectContext, int> value) =>
            new CausalSequenceEffectNode<CardPlayedTriggeredEffectContext>(
            [
                new RemoveStatusNode<CardPlayedTriggeredEffectContext>(
                    scribe, new StatusDefinitionId(slotId)),
                new ConditionalEffectNode<CardPlayedTriggeredEffectContext>(
                    new ComparisonExpression<CardPlayedTriggeredEffectContext>(
                        value, ComparisonOperator.Greater,
                        new ConstantExpression<CardPlayedTriggeredEffectContext>(0)),
                    new ApplyStatusNode<CardPlayedTriggeredEffectContext>(
                        scribe, new StatusDefinitionId(slotId), value, sourceSelector: scribe)),
            ]);

        IEffectNode<CardPlayedTriggeredEffectContext> At(int count, string slotId) =>
            new ConditionalEffectNode<CardPlayedTriggeredEffectContext>(
                new ComparisonExpression<CardPlayedTriggeredEffectContext>(
                    written, ComparisonOperator.Equal,
                    new ConstantExpression<CardPlayedTriggeredEffectContext>(count)),
                Set(slotId, kind));

        // The scrape sheets arrive with the first entry — the master offers them "after at least one slot has
        // been written" — unless the text has been declared canon, in which case there is no scraping.
        //
        // The question asked is a STATE, not a moment: "something stands on the tablet, this turn may still
        // scrape, and there are no sheets out". A count that has to be exactly one at exactly the right node
        // is a bet on the order two triggers resolve in, and the failsafe that fires on the same card play
        // wins that bet often enough to eat the offer.
        var anyEntry = TabletSlots
            .Select(slot => (ICombatExpression<CardPlayedTriggeredEffectContext, bool>)
                new ComparisonExpression<CardPlayedTriggeredEffectContext>(
                    Stacks(slot), ComparisonOperator.Greater,
                    new ConstantExpression<CardPlayedTriggeredEffectContext>(0)))
            .Aggregate((a, b) => new OrExpression<CardPlayedTriggeredEffectContext>(a, b));

        var offerTheSheets = new ConditionalEffectNode<CardPlayedTriggeredEffectContext>(
            new AndExpression<CardPlayedTriggeredEffectContext>(
                anyEntry,
                new AndExpression<CardPlayedTriggeredEffectContext>(
                    new ComparisonExpression<CardPlayedTriggeredEffectContext>(
                        new CombatantZoneCardCountExpression<CardPlayedTriggeredEffectContext>(
                            Applicant, CardZone.Hand, new TagId(TabletTag)),
                        ComparisonOperator.Equal,
                        new ConstantExpression<CardPlayedTriggeredEffectContext>(0)),
                    new AndExpression<CardPlayedTriggeredEffectContext>(
                        new ComparisonExpression<CardPlayedTriggeredEffectContext>(
                            new CombatantCounterExpression<CardPlayedTriggeredEffectContext>(
                                scribe, ScrapeSpent),
                            ComparisonOperator.Equal,
                            new ConstantExpression<CardPlayedTriggeredEffectContext>(0)),
                        new ComparisonExpression<CardPlayedTriggeredEffectContext>(
                            new CombatantCounterExpression<CardPlayedTriggeredEffectContext>(
                                scribe, ScrapeBanned),
                            ComparisonOperator.Equal,
                            new ConstantExpression<CardPlayedTriggeredEffectContext>(0))))),
            new SequenceEffectNode<CardPlayedTriggeredEffectContext>(
            [
                .. new[] { ScrapeFirstCardId, ScrapeSecondCardId, ScrapeThirdCardId }.Select(id =>
                    (IEffectNode<CardPlayedTriggeredEffectContext>)
                    new CreateCardInstanceNode<CardPlayedTriggeredEffectContext>(
                        Applicant, new CardDefinitionId(id), CardZone.Hand,
                        new ConstantExpression<CardPlayedTriggeredEffectContext>(1))),
            ]));

        var phaseOne = new SequenceEffectNode<CardPlayedTriggeredEffectContext>(
        [
            At(1, FirstEntryId),
            At(2, SecondEntryId),
            At(3, ThirdEntryId),
        ]);

        // The window slides: the first slot takes what the second held, the second what the third held, and
        // the third takes this card. Causal, because each read has to happen before the next write.
        var palimpsest = new CausalSequenceEffectNode<CardPlayedTriggeredEffectContext>(
        [
            Set(FirstEntryId, Stacks(SecondEntryId)),
            Set(SecondEntryId, Stacks(ThirdEntryId)),
            Set(ThirdEntryId, kind),
        ]);

        return new EffectProgram<CardPlayedTriggeredEffectContext>(
            new ConditionalEffectNode<CardPlayedTriggeredEffectContext>(
                new AndExpression<CardPlayedTriggeredEffectContext>(
                    new TargetHasStatusExpression<CardPlayedTriggeredEffectContext>(
                        CombatantTargetSelectors.Source, new StatusDefinitionId(PassiveStatuses.ApplicantId)),
                    new NotExpression<CardPlayedTriggeredEffectContext>(
                        new OrExpression<CardPlayedTriggeredEffectContext>(
                            Is(Cards.CardAuthoring.JunkTag), Is(TabletTag)))),
                new CausalSequenceEffectNode<CardPlayedTriggeredEffectContext>(
                [
                    // What kind of thing this was, as a number a face can carry.
                    new ConditionalEffectNode<CardPlayedTriggeredEffectContext>(
                        Is(Cards.CardAuthoring.DeedTag),
                        new SetCombatantCounterNode<CardPlayedTriggeredEffectContext>(
                            scribe, EntryKind,
                            new ConstantExpression<CardPlayedTriggeredEffectContext>(1), relative: false),
                        new ConditionalEffectNode<CardPlayedTriggeredEffectContext>(
                            Is(Cards.CardAuthoring.WorkingTag),
                            new SetCombatantCounterNode<CardPlayedTriggeredEffectContext>(
                                scribe, EntryKind,
                                new ConstantExpression<CardPlayedTriggeredEffectContext>(2), relative: false),
                            new SetCombatantCounterNode<CardPlayedTriggeredEffectContext>(
                                scribe, EntryKind,
                                new ConstantExpression<CardPlayedTriggeredEffectContext>(3), relative: false))),

                    new SetCombatantCounterNode<CardPlayedTriggeredEffectContext>(
                        scribe, EntriesWritten,
                        new ConstantExpression<CardPlayedTriggeredEffectContext>(1), relative: true),
                    new SetCombatantCounterNode<CardPlayedTriggeredEffectContext>(
                        scribe, LastEntry, kind, relative: false),

                    new ConditionalEffectNode<CardPlayedTriggeredEffectContext>(
                        new TargetHasStatusExpression<CardPlayedTriggeredEffectContext>(
                            scribe, new StatusDefinitionId(PalimpsestId)),
                        palimpsest,
                        phaseOne),

                    offerTheSheets,
                ])));
    }

    // ── reading it back ───────────────────────────────────────────────────────────────────────────────────

    // At the end of HIS window the record is read, in order, and then the scroll is clean again.
    //
    // The master reads the tablet before his ordinary intent. It is read after it here, and for one reason:
    // Correct the Margin is an intent that EDITS a standing entry, and an entry read before the intent that
    // edits it is an entry no correction can reach. One of the two orders had to give; this one keeps both
    // the correction and the whole of his answer inside the window the player is planning against.
    private static EffectProgram<TurnEndedTriggeredEffectContext> ReadTheTablet()
    {
        var scribe = Bearer(HouseOfLifeId);

        ICombatExpression<TurnEndedTriggeredEffectContext, int> Stacks(string slotId) =>
            new CombatantStatusStacksExpression<TurnEndedTriggeredEffectContext>(
                scribe, new StatusDefinitionId(slotId));

        ICombatExpression<TurnEndedTriggeredEffectContext, bool> KindIs(string slotId, int kind) =>
            new ComparisonExpression<TurnEndedTriggeredEffectContext>(
                Stacks(slotId), ComparisonOperator.Equal,
                new ConstantExpression<TurnEndedTriggeredEffectContext>(kind));

        IEffectNode<TurnEndedTriggeredEffectContext> Give(string statusId, int stacks) =>
            new ApplyStatusNode<TurnEndedTriggeredEffectContext>(
                Applicant, new StatusDefinitionId(statusId),
                new ConstantExpression<TurnEndedTriggeredEffectContext>(stacks), sourceSelector: scribe);

        // A recorded Power is worth a Strength ONCE per tablet; every one of them still writes you up.
        var recordedPower = new CausalSequenceEffectNode<TurnEndedTriggeredEffectContext>(
        [
            new ConditionalEffectNode<TurnEndedTriggeredEffectContext>(
                new ComparisonExpression<TurnEndedTriggeredEffectContext>(
                    new CombatantCounterExpression<TurnEndedTriggeredEffectContext>(
                        scribe, StrengthFromTablet),
                    ComparisonOperator.Equal,
                    new ConstantExpression<TurnEndedTriggeredEffectContext>(0)),
                new CausalSequenceEffectNode<TurnEndedTriggeredEffectContext>(
                [
                    new ApplyStatusNode<TurnEndedTriggeredEffectContext>(
                        scribe, new StatusDefinitionId("strength"),
                        new ConstantExpression<TurnEndedTriggeredEffectContext>(1), sourceSelector: scribe),
                    new SetCombatantCounterNode<TurnEndedTriggeredEffectContext>(
                        scribe, StrengthFromTablet,
                        new ConstantExpression<TurnEndedTriggeredEffectContext>(1), relative: false),
                ])),
            Give(InscribedId, 1),
        ]);

        // `emptyReads` says whether a blank slot is an Empty entry (the three current ones) or simply a slot
        // that is not there at all (the inherited one, in a phase that has no inheritance).
        IEffectNode<TurnEndedTriggeredEffectContext> Resolve(string slotId, bool emptyReads) =>
            new ConditionalEffectNode<TurnEndedTriggeredEffectContext>(
                KindIs(slotId, 1),
                new DealDamageNode<TurnEndedTriggeredEffectContext>(
                    Applicant,
                    new ConstantExpression<TurnEndedTriggeredEffectContext>(RecordedAttack)),
                new ConditionalEffectNode<TurnEndedTriggeredEffectContext>(
                    KindIs(slotId, 2),
                    new CausalSequenceEffectNode<TurnEndedTriggeredEffectContext>(
                    [
                        new GainBlockNode<TurnEndedTriggeredEffectContext>(
                            scribe,
                            new ConstantExpression<TurnEndedTriggeredEffectContext>(RecordedBlock)),
                        Give(Cards.Keywords.Paperwork, 1),
                    ]),
                    new ConditionalEffectNode<TurnEndedTriggeredEffectContext>(
                        new ComparisonExpression<TurnEndedTriggeredEffectContext>(
                            Stacks(slotId), ComparisonOperator.GreaterOrEqual,
                            new ConstantExpression<TurnEndedTriggeredEffectContext>(3)),
                        recordedPower,
                        emptyReads
                            ? Give(Cards.Keywords.Doubt, 1)
                            : new NoOpEffectNode<TurnEndedTriggeredEffectContext>())));

        var complete = TabletSlots
            .Select(slot => (ICombatExpression<TurnEndedTriggeredEffectContext, bool>)
                new ComparisonExpression<TurnEndedTriggeredEffectContext>(
                    Stacks(slot), ComparisonOperator.Greater,
                    new ConstantExpression<TurnEndedTriggeredEffectContext>(0)))
            .Aggregate((a, b) => new AndExpression<TurnEndedTriggeredEffectContext>(a, b));

        var wearsPalimpsest = new TargetHasStatusExpression<TurnEndedTriggeredEffectContext>(
            scribe, new StatusDefinitionId(PalimpsestId));

        // The inheritance: the final CURRENT entry of the tablet just read is carried into the next one.
        IEffectNode<TurnEndedTriggeredEffectContext> InheritFrom(string slotId, IEffectNode<TurnEndedTriggeredEffectContext> otherwise) =>
            new ConditionalEffectNode<TurnEndedTriggeredEffectContext>(
                new ComparisonExpression<TurnEndedTriggeredEffectContext>(
                    Stacks(slotId), ComparisonOperator.Greater,
                    new ConstantExpression<TurnEndedTriggeredEffectContext>(0)),
                new ApplyStatusNode<TurnEndedTriggeredEffectContext>(
                    scribe, new StatusDefinitionId(InheritedEntryId), Stacks(slotId), sourceSelector: scribe),
                otherwise);

        var inherit = new CausalSequenceEffectNode<TurnEndedTriggeredEffectContext>(
        [
            new RemoveStatusNode<TurnEndedTriggeredEffectContext>(
                scribe, new StatusDefinitionId(InheritedEntryId)),
            InheritFrom(ThirdEntryId,
                InheritFrom(SecondEntryId,
                    InheritFrom(FirstEntryId, new NoOpEffectNode<TurnEndedTriggeredEffectContext>()))),
        ]);

        return new EffectProgram<TurnEndedTriggeredEffectContext>(
            new ConditionalEffectNode<TurnEndedTriggeredEffectContext>(
                new NotExpression<TurnEndedTriggeredEffectContext>(
                    PlayersTurn<TurnEndedTriggeredEffectContext>()),
                new CausalSequenceEffectNode<TurnEndedTriggeredEffectContext>(
                [
                    // The inherited entry is read first, and only where there is one to read.
                    new ConditionalEffectNode<TurnEndedTriggeredEffectContext>(
                        wearsPalimpsest, Resolve(InheritedEntryId, emptyReads: false)),
                    Resolve(FirstEntryId, emptyReads: true),
                    Resolve(SecondEntryId, emptyReads: true),
                    Resolve(ThirdEntryId, emptyReads: true),

                    // A whole tablet is three entries written, and the transition counts those.
                    new ConditionalEffectNode<TurnEndedTriggeredEffectContext>(
                        complete,
                        new CausalSequenceEffectNode<TurnEndedTriggeredEffectContext>(
                        [
                            new SetCombatantCounterNode<TurnEndedTriggeredEffectContext>(
                                scribe, TabletsRead,
                                new ConstantExpression<TurnEndedTriggeredEffectContext>(1), relative: true),
                            new SetCombatantCounterNode<TurnEndedTriggeredEffectContext>(
                                scribe, LastCompleteFinal, Stacks(ThirdEntryId), relative: false),
                        ])),

                    // Two whole tablets read and the scroll is scraped down and written over.
                    new ConditionalEffectNode<TurnEndedTriggeredEffectContext>(
                        new AndExpression<TurnEndedTriggeredEffectContext>(
                            new NotExpression<TurnEndedTriggeredEffectContext>(wearsPalimpsest),
                            new ComparisonExpression<TurnEndedTriggeredEffectContext>(
                                new CombatantCounterExpression<TurnEndedTriggeredEffectContext>(
                                    scribe, TabletsRead),
                                ComparisonOperator.GreaterOrEqual,
                                new ConstantExpression<TurnEndedTriggeredEffectContext>(
                                    TabletsForPalimpsest))),
                        TheScrollIsWrittenOver<TurnEndedTriggeredEffectContext>(scribe)),

                    // …and once it is worn, every tablet leaves its last entry behind.
                    new ConditionalEffectNode<TurnEndedTriggeredEffectContext>(wearsPalimpsest, inherit),

                    .. TabletSlots.Select(slot =>
                        (IEffectNode<TurnEndedTriggeredEffectContext>)
                        new RemoveStatusNode<TurnEndedTriggeredEffectContext>(
                            scribe, new StatusDefinitionId(slot))),

                    new SetCombatantCounterNode<TurnEndedTriggeredEffectContext>(
                        scribe, EntriesWritten,
                        new ConstantExpression<TurnEndedTriggeredEffectContext>(0), relative: false),
                    new SetCombatantCounterNode<TurnEndedTriggeredEffectContext>(
                        scribe, StrengthFromTablet,
                        new ConstantExpression<TurnEndedTriggeredEffectContext>(0), relative: false),
                ])));
    }

    // The transition, written once because two things reach it: the second whole tablet, and 290.
    private static IEffectNode<TContext> TheScrollIsWrittenOver<TContext>(ICombatantTargetSelector scribe)
        where TContext : class =>
        new CausalSequenceEffectNode<TContext>(
        [
            new SetCombatantCounterNode<TContext>(
                scribe, PalimpsestTaken, new ConstantExpression<TContext>(1), relative: false),
            new ApplyStatusNode<TContext>(
                scribe, new StatusDefinitionId(PalimpsestId),
                new ConstantExpression<TContext>(1), sourceSelector: scribe),
            new GainBlockNode<TContext>(scribe, new ConstantExpression<TContext>(PalimpsestBlock)),
        ]);

    // ── what his own blood announces ──────────────────────────────────────────────────────────────────────

    // 290 is the failsafe on the palimpsest: a fight that never let two whole tablets be written still gets
    // the second half of the boss. What it carries over is the last entry of the last WHOLE tablet, which is
    // the only thing "the most recently completed tablet" can mean when the current one is half-written.
    //
    // 100 announces the canon, and the announcement is the whole of it: the ban lands on the turn AFTER this
    // one, because this turn's scrape sheets are already in hand.
    private static EffectProgram<DamageReceivedTriggeredEffectContext> TheAnnouncements()
    {
        var scribe = Bearer(HouseOfLifeId);
        var health = new CombatantCurrentHealthExpression<DamageReceivedTriggeredEffectContext>(scribe);

        ICombatExpression<DamageReceivedTriggeredEffectContext, bool> NotYet(CounterId taken) =>
            new ComparisonExpression<DamageReceivedTriggeredEffectContext>(
                new CombatantCounterExpression<DamageReceivedTriggeredEffectContext>(scribe, taken),
                ComparisonOperator.Equal,
                new ConstantExpression<DamageReceivedTriggeredEffectContext>(0));

        ICombatExpression<DamageReceivedTriggeredEffectContext, bool> At(int band) =>
            new ComparisonExpression<DamageReceivedTriggeredEffectContext>(
                health, ComparisonOperator.LessOrEqual,
                new ConstantExpression<DamageReceivedTriggeredEffectContext>(band));

        var inherited = new CombatantCounterExpression<DamageReceivedTriggeredEffectContext>(
            scribe, LastCompleteFinal);

        return new EffectProgram<DamageReceivedTriggeredEffectContext>(
            new CausalSequenceEffectNode<DamageReceivedTriggeredEffectContext>(
            [
                new ConditionalEffectNode<DamageReceivedTriggeredEffectContext>(
                    new AndExpression<DamageReceivedTriggeredEffectContext>(
                        At(PalimpsestAt), NotYet(PalimpsestTaken)),
                    new CausalSequenceEffectNode<DamageReceivedTriggeredEffectContext>(
                    [
                        TheScrollIsWrittenOver<DamageReceivedTriggeredEffectContext>(scribe),

                        // The half-written tablet is scraped off — the slots, and not the count beside them:
                        // a counter reset landing between another trigger's increment and its own read is
                        // exactly how a turn's scraping went missing.
                        .. TabletSlots.Select(slot =>
                            (IEffectNode<DamageReceivedTriggeredEffectContext>)
                            new RemoveStatusNode<DamageReceivedTriggeredEffectContext>(
                                scribe, new StatusDefinitionId(slot))),
                        // …and the last whole tablet's final entry is what survives it.
                        new ConditionalEffectNode<DamageReceivedTriggeredEffectContext>(
                            new ComparisonExpression<DamageReceivedTriggeredEffectContext>(
                                inherited, ComparisonOperator.Greater,
                                new ConstantExpression<DamageReceivedTriggeredEffectContext>(0)),
                            new ApplyStatusNode<DamageReceivedTriggeredEffectContext>(
                                scribe, new StatusDefinitionId(InheritedEntryId), inherited,
                                sourceSelector: scribe)),
                    ])),

                new ConditionalEffectNode<DamageReceivedTriggeredEffectContext>(
                    new AndExpression<DamageReceivedTriggeredEffectContext>(
                        At(CanonAt), NotYet(CanonTaken)),
                    new CausalSequenceEffectNode<DamageReceivedTriggeredEffectContext>(
                    [
                        new SetCombatantCounterNode<DamageReceivedTriggeredEffectContext>(
                            scribe, CanonTaken,
                            new ConstantExpression<DamageReceivedTriggeredEffectContext>(1), relative: false),
                        new ApplyStatusNode<DamageReceivedTriggeredEffectContext>(
                            scribe, new StatusDefinitionId(TextIsCanonId),
                            new ConstantExpression<DamageReceivedTriggeredEffectContext>(1),
                            sourceSelector: scribe),
                    ])),
            ]));
    }

    // ── the intents ───────────────────────────────────────────────────────────────────────────────────────

    private static EffectProgram<EnemyActionContext> ByPalimpsest(
        IEffectNode<EnemyActionContext> I, IEffectNode<EnemyActionContext> II) =>
        new(new ConditionalEffectNode<EnemyActionContext>(
            new TargetHasStatusExpression<EnemyActionContext>(
                CombatantTargetSelectors.Source, new StatusDefinitionId(PalimpsestId)),
            II, I));

    // One honorific off the applicant's name — the first thing standing to their credit, whatever it is.
    private static IEffectNode<EnemyActionContext> EraseAnHonorific() =>
        new ModifySelectedStatusStacksNode<EnemyActionContext>(
            Applicant,
            new StatusSelectionSpec(StatusPolarityFilter.Buff, StatusPick.First),
            Const(-1));

    // He changes the earliest entry he can into what the LAST card of your turn was — "another currently
    // represented type", which is the only kind of correction a record admits. A scroll with nothing on it
    // to correct, or nothing else represented on it, gets the reed instead: an intent the engine has already
    // reached cannot step aside.
    private static EffectProgram<EnemyActionContext> CorrectTheMargin()
    {
        var scribe = CombatantTargetSelectors.Source;
        var last = new CombatantCounterExpression<EnemyActionContext>(scribe, LastEntry);

        ICombatExpression<EnemyActionContext, int> Stacks(string slotId) =>
            new CombatantStatusStacksExpression<EnemyActionContext>(scribe, new StatusDefinitionId(slotId));

        ICombatExpression<EnemyActionContext, bool> Correctable(string slotId) =>
            new AndExpression<EnemyActionContext>(
                new ComparisonExpression<EnemyActionContext>(Stacks(slotId), ComparisonOperator.Greater, Const(0)),
                new AndExpression<EnemyActionContext>(
                    new ComparisonExpression<EnemyActionContext>(last, ComparisonOperator.Greater, Const(0)),
                    new ComparisonExpression<EnemyActionContext>(
                        Stacks(slotId), ComparisonOperator.NotEqual, last)));

        IEffectNode<EnemyActionContext> Correct(string slotId) =>
            new CausalSequenceEffectNode<EnemyActionContext>(
            [
                new RemoveStatusNode<EnemyActionContext>(scribe, new StatusDefinitionId(slotId)),
                new ApplyStatusNode<EnemyActionContext>(
                    scribe, new StatusDefinitionId(slotId), last, sourceSelector: scribe),
            ]);

        return new EffectProgram<EnemyActionContext>(
            new ConditionalEffectNode<EnemyActionContext>(
                Correctable(FirstEntryId), Correct(FirstEntryId),
                new ConditionalEffectNode<EnemyActionContext>(
                    Correctable(SecondEntryId), Correct(SecondEntryId),
                    new ConditionalEffectNode<EnemyActionContext>(
                        Correctable(ThirdEntryId), Correct(ThirdEntryId),
                        Hit(NothingToCorrect)))));
    }

    // The signature. The ban was laid at the start of the turn this answers; here it is read out, and the
    // scraping comes back with the announcement spent.
    private static EffectProgram<EnemyActionContext> TheTextIsNowCanon() =>
        new(new CausalSequenceEffectNode<EnemyActionContext>(
        [
            Hit(CanonBlow),
            new RemoveStatusNode<EnemyActionContext>(
                CombatantTargetSelectors.Source, new StatusDefinitionId(TextIsCanonId)),
        ]));

    // ── scraping, as cards ────────────────────────────────────────────────────────────────────────────────

    // An entry is erased by PLAYING the sheet that erases it, which is how a choice among three slots is put
    // in front of the player. The sheet costs no Energy and one sheet of Paperwork, it is only ever in hand
    // once something has been written, and it does nothing at all once the turn's scraping is spent.
    private static CardData ScrapeCard(string id, string name, string slotId)
    {
        var scribe = Bearer(HouseOfLifeId);

        return new CardData
        {
            Id = id,
            NameKey = name,
            DescriptionKey =
                "Blank this entry on the scroll. One entry a turn, and the correction is written up: "
                + "1 Paperwork.",
            Costs = [],
            Tags = [new TagId(TabletTag), new TagId(Cards.CardAuthoring.TemporaryTag)],
            Program = new EffectProgram<CardPlayContext>(
                new ConditionalEffectNode<CardPlayContext>(
                    new AndExpression<CardPlayContext>(
                        new ComparisonExpression<CardPlayContext>(
                            new CombatantCounterExpression<CardPlayContext>(scribe, ScrapeSpent),
                            ComparisonOperator.Equal, new ConstantExpression<CardPlayContext>(0)),
                        new ComparisonExpression<CardPlayContext>(
                            new CombatantStatusStacksExpression<CardPlayContext>(
                                scribe, new StatusDefinitionId(slotId)),
                            ComparisonOperator.Greater, new ConstantExpression<CardPlayContext>(0))),
                    new CausalSequenceEffectNode<CardPlayContext>(
                    [
                        new RemoveStatusNode<CardPlayContext>(scribe, new StatusDefinitionId(slotId)),
                        new SetCombatantCounterNode<CardPlayContext>(
                            scribe, ScrapeSpent, new ConstantExpression<CardPlayContext>(1), relative: false),
                        new ApplyStatusNode<CardPlayContext>(
                            CombatantTargetSelectors.Source,
                            new StatusDefinitionId(Cards.Keywords.Paperwork),
                            new ConstantExpression<CardPlayContext>(1), sourceSelector: scribe),
                    ]))),
            PlayedCardDestinationZone = CardZone.ExhaustPile,
            TurnEndHandDestinationZone = CardZone.ExhaustPile,
        };
    }
}
