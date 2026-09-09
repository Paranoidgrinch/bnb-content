using RogueDeck.Core.Combat;
using RogueDeck.Sandbox.Composition;
using RogueDeck.Scenario.Authoring;

namespace BnbContent.Converter;

// ACT V, the sixth and last god — ENLIL, VOICE OF THE UNALTERABLE DECREE. He decrees, and it is so.
//
// Every other god in this act does something TO you. Enlil does something to the GAME. His question (§11) is
// the only one in the whole boss pool that is not about damage or resources at all:
//
//     Can your build still function when the underlying rules of combat temporarily become something else?
//
// ── A DECREE IS NOT A PROMISE (§11.1) ────────────────────────────────────────────────────────────────────
// Utu, one room earlier, asks you to swear and then never stops you: the card that breaks the Oath resolves
// in full and is merely SEEN. Enlil is his exact opposite and the pairing is deliberate. Under THE FOURTH
// WORK SHALL BE THE LAST the fifth card is not a choice with a price — it is unavailable. The rule is not a
// promise; it is current reality. That is why this god, and only this god, needed the engine to grow a way
// of saying a RULE rather than a number (Core: CombatDecrees.cs).
//
// ── UNALTERABLE (§11.2) ──────────────────────────────────────────────────────────────────────────────────
// Nothing in this fight removes, dispels, shortens, delays or bargains away a decree. It runs its announced
// count and it goes. The player never gets a card, a status or a choice that touches one, and that absence
// is the mechanic: the only variable is what YOU do under it.
//
// ── HE IS ABSOLUTE, NOT ARBITRARY (§11.3) ────────────────────────────────────────────────────────────────
// Every decree stands on the table as NEXT DECREE for a full player turn before it enacts. The chip carries
// the line and the rule in the same words it will have when it is real. There is exactly one moment in the
// fight when something takes effect the turn it is spoken, and it is the phase change that announces it.
//
// ── NO DECREE PRIVILEGE (§11.11) ─────────────────────────────────────────────────────────────────────────
// A universal decree binds him. He wears every one of them himself, on his own row, next to his name — and
// that is not decoration: the engine reads each rule from the combatant it is about, so his 48-Block Raise
// E-kur really does gain 30 while NO WALL SHALL RISE ABOVE THIRTY stands, and his own blows are capped by
// his own ceiling. He does not complain and he never cancels a word of his because it cost him.
//
// ── THE DIRECTOR IS THE SHAPE OF THE POOL, NOT A SEARCH (§11.6) ──────────────────────────────────────────
// §11.6 wants no contradictions, no impossible states and no hard-locked build. Rather than filter twelve
// decrees every time one expires, the twelve are split into TWO RINGS and each ring has one slot. A ring is
// a cycle: each decree names its successor. Two decrees can only ever be in force together if they are in
// DIFFERENT rings — so every contradictory pair is put in the SAME ring and is then impossible by
// construction. THE FIRST WORD SHALL BE WITHOUT PRICE and NO WORK SHALL BE WITHOUT MEASURE are the pair
// that matters, and they are two steps apart in the ring of Works. Harsh matchups stay legal; the Director
// is not there to make him nice.
//
// ── THE THREE PHASES ─────────────────────────────────────────────────────────────────────────────────────
//     100–70 %   ONE WORD                    one ring turns              you learn to read reality
//      70–35 %   THE ASSEMBLY FALLS SILENT   both rings turn, staggered  the rule set changes gradually
//        ≤ 35 %  THREE WORDS ARE ENOUGH      the rotation stops          three curated words, until death
//
// The Final Order is one of four hand-curated triples (§11.13–§11.16) and it is NOT random: he answers what
// you were doing in the turn you brought him under 35 %. A hand that plays five cards is answered by the
// Narrow Kingdom; a single enormous blow by the Measured Kingdom; energy left standing by the Kingdom of
// Ash; anything else by the Ordered Kingdom. Then no fourth mechanic arrives, ever (§11.17). The only
// question left is whether the build can be reinvented under that reality.
//
// ── WHAT THIS FIGHT DOES NOT HAVE ────────────────────────────────────────────────────────────────────────
// §11.5 lists fifteen candidate decrees and §11.6 asks for "around 12". Two are not here. THE MEASURE SHALL
// NOT EXCEED SEVEN is the master's own "optional pool candidate" and says nothing this game's 3-energy
// economy can feel. THE LAST DRAWN SHALL REMAIN cannot be written: it needs to name the LAST card of a hand
// whose size is only known at run time, and a card is named by a constant index. Both are in ADAPTATIONS.md
// rather than quietly dropped.
public static partial class ActFive
{
    public const string EnlilEnemyId = "enlil_voice_of_the_unalterable_decree";
    public const string EnlilEncounterId = "act_5_enlil_voice_of_the_unalterable_decree";

    // His rule, worn from the first bell; every program below hangs off it.
    public const string TheWordId = "the_word_of_enlil";

    // The three phases.
    public const string OneWordId = "one_word";
    public const string AssemblySilentId = "the_assembly_falls_silent";
    public const string ThreeWordsId = "three_words_are_enough";

    // The two rings' clocks — "II rounds remain", and the only countdown in the fight.
    public const string WordStandsId = "the_word_stands";
    public const string SecondWordStandsId = "the_second_word_stands";

    // The four Final Orders.
    public const string NarrowKingdomId = "the_narrow_kingdom";
    public const string AshKingdomId = "the_kingdom_of_ash";
    public const string MeasuredKingdomId = "the_measured_kingdom";
    public const string OrderedKingdomId = "the_ordered_kingdom";

    // ── the twelve decrees ────────────────────────────────────────────────────────────────────────────────
    public const string FourthWorkId = "the_fourth_work_shall_be_the_last";
    public const string FirstWithoutPriceId = "the_first_word_shall_be_without_price";
    public const string NoLikenessId = "no_work_shall_follow_its_likeness";
    public const string WithoutMeasureId = "no_work_shall_be_without_measure";
    public const string ThirdWithoutPriceId = "the_third_work_shall_be_without_price";
    public const string UnspentTomorrowId = "what_is_unspent_shall_pass_into_tomorrow";

    public const string HandOfSevenId = "seven_shall_be_the_hands_measure";
    public const string BlowOfTwentyId = "no_single_blow_shall_exceed_twenty";
    public const string NoWorkReturnsId = "no_work_shall_return";
    public const string CastOutReturnsId = "what_has_been_cast_out_shall_return";
    public const string WallOfThirtyId = "no_wall_shall_rise_above_thirty";
    public const string FirstBlowNothingId = "the_first_blow_shall_fall_upon_nothing";

    // 700. The same as Nanna-Sin, above Utu and Nisaba, below Inanna. He does not scale off anything the
    // player does, so his length is bought outright — and it has to be bought, because a decree that runs
    // two rounds is worth nothing in a fight that ends in six.
    public const int EnlilMaxHealth = 700;

    private const int SilenceAt = 70;   // §11.10
    private const int LastWordsAt = 35; // §11.12

    // The two moves the phase changes queue, and the flags that make each threshold cross exactly once.
    private static CounterId SilenceMove => new("enlil_silence_move");
    private static CounterId FinalMove => new("enlil_final_move");
    private static CounterId SilenceTaken => new("enlil_silence_taken");
    private static CounterId LastWordsTaken => new("enlil_last_words_taken");

    // The preparation turn (§11.12): set to 2 when the last three are announced, walked down at each player
    // turn end, and the three enact when it reaches zero — so one whole player turn passes under the old
    // reality with the new one already written on the table.
    private static CounterId FinalDue => new("enlil_final_due");

    // Which round WHAT HAS BEEN CAST OUT SHALL RETURN has already given a card back in. ⚠ CardsDrawn is not
    // "the turn began" — a card that draws cards fires it again mid-turn, and a decree that returns one
    // exhausted card a turn must not return four because the hand was refilled.
    private static CounterId Returned => new("enlil_returned");

    private static ICombatantTargetSelector Voice => Bearer(TheWordId);

    // Which ring a decree turns in. The whole Director is this one field: two decrees are only ever in force
    // together if they are in different rings, so a contradictory pair goes in the same ring and can never
    // meet. Everything else §11.6 asks for follows from there.
    private enum Order { Works, Force }

    // One decree. `Rounds` is §11.7's severity class made concrete: 3 or 2 for a structural rule, 1 for a
    // severe one, because a rule whose consequences outlive it (a card exhausted is exhausted for good)
    // must not run long enough to empty a deck.
    private sealed record Decree(string Id, string Line, string Rule, int Rounds, Order Turns);

    private static readonly Decree[] Decrees =
    [
        // ── the ring of Works and Measure (§11.5, Order of Works + Order of Measure) ──────────────────────
        new(FourthWorkId, "THE FOURTH WORK SHALL BE THE LAST",
            "No more than 4 cards may be played each turn. The fifth is not a choice with a price — it is "
            + "unavailable.", 3, Order.Works),
        new(FirstWithoutPriceId, "THE FIRST WORD SHALL BE WITHOUT PRICE",
            "The first card you play each turn costs nothing.", 2, Order.Works),
        new(NoLikenessId, "NO WORK SHALL FOLLOW ITS LIKENESS",
            "The same kind of card may not be played twice in a row. Deed after Deed, Working after "
            + "Working, Rite after Rite — the second one cannot be played at all.", 3, Order.Works),
        new(WithoutMeasureId, "NO WORK SHALL BE WITHOUT MEASURE",
            "No card may cost nothing. Anything that would cost 0 costs 1 instead — including a card made "
            + "free by anything else.", 1, Order.Works),
        new(ThirdWithoutPriceId, "THE THIRD WORK SHALL BE WITHOUT PRICE",
            "The third card you play each turn costs nothing.", 2, Order.Works),
        new(UnspentTomorrowId, "WHAT IS UNSPENT SHALL PASS INTO TOMORROW",
            "Energy you do not spend is still there next turn, on top of the refill.", 3, Order.Works),

        // ── the ring of Force, Returning and the Hand (§11.5, Order of Force + Returning + the Hand) ──────
        new(HandOfSevenId, "SEVEN SHALL BE THE HAND'S MEASURE",
            "A hand holds no more than 7. Cards past it are not drawn at all — they stay on the pile "
            + "rather than being drawn and thrown away.", 3, Order.Force),
        new(BlowOfTwentyId, "NO SINGLE BLOW SHALL EXCEED TWENTY",
            "No single instance of damage may exceed 20. This binds both sides, and it binds Enlil.", 2,
            Order.Force),
        new(NoWorkReturnsId, "NO WORK SHALL RETURN",
            "A card you play is exhausted instead of discarded. It does not come back this fight.", 1,
            Order.Force),
        new(CastOutReturnsId, "WHAT HAS BEEN CAST OUT SHALL RETURN",
            "At the start of your turn, the oldest exhausted card returns to your hand.", 2, Order.Force),
        new(WallOfThirtyId, "NO WALL SHALL RISE ABOVE THIRTY",
            "No single gain of Block may exceed 30. This binds both sides, and it binds Enlil.", 2,
            Order.Force),
        new(FirstBlowNothingId, "THE FIRST BLOW SHALL FALL UPON NOTHING",
            "The first damage each side deals in a turn is 0. This binds both sides, and it binds Enlil.",
            1, Order.Force),
    ];

    private static Decree Named(string id) => Decrees.First(d => d.Id == id);

    private static Decree[] RingOf(Order ring) => [.. Decrees.Where(d => d.Turns == ring)];

    // A ring is a CYCLE: each decree names the one that follows it, and the last names the first. There is
    // no list to search and no pool to filter — the order is the Director.
    private static Decree Follows(Decree decree)
    {
        var ring = RingOf(decree.Turns);
        return ring[(Array.FindIndex(ring, d => d.Id == decree.Id) + 1) % ring.Length];
    }

    // The chip that stands on the table for a full player turn before the rule is real (§11.3).
    private static string NextId(Decree decree) => "next_" + decree.Id;

    // The same, by id — what a test (and anything else outside this file) has to be able to name.
    public static string NextIdFor(string decreeId) => NextId(Named(decreeId));

    public static IReadOnlyList<string> EnlilKingdoms() => [.. Kingdoms.Select(k => k.Id)];

    public static IReadOnlyList<string> EnlilWordsOf(string kingdomId) =>
        Kingdoms.First(k => k.Id == kingdomId).Words;

    private static string ClockOf(Order ring) => ring == Order.Works ? WordStandsId : SecondWordStandsId;

    // ── the four Final Orders (§11.13 – §11.16) ───────────────────────────────────────────────────────────
    private sealed record Kingdom(string Id, string Name, string Text, string[] Words);

    private static readonly Kingdom[] Kingdoms =
    [
        new(NarrowKingdomId, "The Narrow Kingdom",
            "Few actions, and every one of them has to count. Four cards a turn, the first of them free, "
            + "and nothing you leave unspent is lost.",
            [FourthWorkId, FirstWithoutPriceId, UnspentTomorrowId]),
        new(AshKingdomId, "The Kingdom of Ash",
            "Nothing you play comes back — and one thing that has already gone returns to you each turn. "
            + "A new economy, not a slower death.",
            [NoWorkReturnsId, CastOutReturnsId, ThirdWithoutPriceId]),
        new(MeasuredKingdomId, "The Measured Kingdom",
            "Damage and Block are rewritten at the root. Twenty is the largest blow there is, thirty the "
            + "highest wall, and the first blow of every turn falls upon nothing.",
            [BlowOfTwentyId, WallOfThirtyId, FirstBlowNothingId]),
        new(OrderedKingdomId, "The Ordered Kingdom",
            "Combat becomes a sequencing puzzle. Never the same kind twice in a row, the third work free, "
            + "and seven cards is the whole of the hand.",
            [NoLikenessId, ThirdWithoutPriceId, HandOfSevenId]),
    ];

    // ── what the act hands to the rest of the converter ───────────────────────────────────────────────────

    public static IReadOnlyList<StatusData> EnlilStatuses() =>
    [
        TheWordOfEnlil(),
        Face(OneWordId, "One Word",
            "The first reality. One decree governs at a time, and the next one is already on the table.",
            stacks: false),
        Face(AssemblySilentId, "The Assembly Falls Silent",
            "The second reality. Two decrees govern at once, and their counts do not run out together — "
            + "the rules change gradually rather than all at once.", stacks: false),
        Face(ThreeWordsId, "Three Words Are Enough",
            "The last reality. The rotation is over. Three words were spoken and they stand until one of "
            + "you is finished; no fourth ever comes.", stacks: false),
        Face(WordStandsId, "The Word Stands",
            "Rounds the standing decree still has to run. It cannot be removed, dispelled, shortened or "
            + "bargained away — it expires, and not before.", stacks: true),
        Face(SecondWordStandsId, "The Second Word Stands",
            "Rounds the second standing decree still has to run. The two counts are staggered on purpose.",
            stacks: true),
        .. Kingdoms.Select(k => Face(k.Id, k.Name, k.Text, stacks: false)),
        .. Decrees.Select(TheDecree),
        .. Decrees.Select(TheAnnouncement),
    ];

    // Every decree, every announcement and every Final Order, for the phase-marker list: in this fight the
    // chip row is not commentary on the rules, it is where the rules are written down.
    public static IReadOnlyList<string> EnlilDecreeMarkers() =>
    [
        .. Decrees.Select(d => d.Id),
        .. Decrees.Select(NextId),
        .. Kingdoms.Select(k => k.Id),
    ];

    public static EffectProgram<EnemyActionContext>? EnlilIntent(string enemyId, string intentId) =>
        enemyId != EnlilEnemyId ? null : intentId switch
        {
            // §11.9: his direct moves stay comparatively readable. The decrees are the complexity, and a
            // move that also needed reading would be a second fight on top of the first.
            "the_command_goes_forth" => Word(Hit(30)),
            "wind_over_the_plain" => Word(Seq(Hit(9), Hit(9), Hit(9), Hit(9))),
            // THE MOVE THAT PROVES §11.11. 48 Block — and 30 of it whenever his own wall decree stands.
            "raise_e_kur" => Word(Seq(Guard(48), Hit(14))),
            "weight_of_kingship" => Word(Seq(Hit(26), Debuff(Cards.Keywords.Doubt, 2))),
            "the_assembly_falls_silent" => Word(Seq(
                Silence(SilenceMove), Hit(34), Debuff(Cards.Keywords.Paperwork, 3))),
            "three_words_are_enough" => Word(Seq(Silence(FinalMove), Hit(44))),
            _ => null,
        };

    private static EffectProgram<EnemyActionContext> Word(IEffectNode<EnemyActionContext> move) => new(move);

    private static IEffectNode<EnemyActionContext> Silence(CounterId flag) =>
        new SetCombatantCounterNode<EnemyActionContext>(
            Voice, flag, Const<EnemyActionContext>(0), relative: false);

    // ── his rule ──────────────────────────────────────────────────────────────────────────────────────────

    private static StatusData TheWordOfEnlil() => new()
    {
        Id = TheWordId,
        NameKey = "The Word of Enlil",
        DescriptionKey =
            "He speaks, and the rule is the rule. A decree stands on the table for a full turn as NEXT "
            + "DECREE before it is real, runs its announced count, and goes. Nothing removes one early, and "
            + "where the wording is universal it binds him too.",
        Polarity = StatusPolarity.Neutral,
        StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
        UsesStacks = false,
        Tags = [],
        PassiveModifiers = [],
        Triggers =
        [
            Trigger(TheWordTurns(), nameof(TriggerEvent.TurnEnded), StatusTriggerScope.Anywhere),
            Trigger(TheKingshipChanges(), nameof(TriggerEvent.DamageTaken)),
        ],
    };

    // ── the rings turn, at the end of the player's turn ───────────────────────────────────────────────────
    //
    // Everything a decree does to time happens here and nowhere else: the standing count walks down by one,
    // and a count that reaches zero takes its decree off the table, makes the announced one real, and puts
    // the next announcement up. Reading it at the PLAYER's turn end is what gives §11.3 its guarantee — the
    // announcement made at the end of one turn cannot enact before the end of the next, so there is always
    // a whole player turn in between.
    private static EffectProgram<TurnEndedTriggeredEffectContext> TheWordTurns() =>
        new(Steps<TurnEndedTriggeredEffectContext>(
        [
            new ConditionalEffectNode<TurnEndedTriggeredEffectContext>(
                new AndExpression<TurnEndedTriggeredEffectContext>(
                    PlayersTurn<TurnEndedTriggeredEffectContext>(),
                    new NotExpression<TurnEndedTriggeredEffectContext>(
                        Has<TurnEndedTriggeredEffectContext>(Voice, ThreeWordsId))),
                Steps<TurnEndedTriggeredEffectContext>(
                [
                    TheRingTurns<TurnEndedTriggeredEffectContext>(Order.Works, gated: false),
                    TheRingTurns<TurnEndedTriggeredEffectContext>(Order.Force, gated: true),
                ])),
            // §11.12's preparation turn, walked down here so that it is measured in player turns.
            new ConditionalEffectNode<TurnEndedTriggeredEffectContext>(
                new AndExpression<TurnEndedTriggeredEffectContext>(
                    PlayersTurn<TurnEndedTriggeredEffectContext>(),
                    At(Due<TurnEndedTriggeredEffectContext>(), ComparisonOperator.GreaterOrEqual, 1)),
                Steps<TurnEndedTriggeredEffectContext>(
                [
                    new SetCombatantCounterNode<TurnEndedTriggeredEffectContext>(
                        Voice, FinalDue, Const<TurnEndedTriggeredEffectContext>(-1), relative: true),
                    new ConditionalEffectNode<TurnEndedTriggeredEffectContext>(
                        At(Due<TurnEndedTriggeredEffectContext>(), ComparisonOperator.LessOrEqual, 0),
                        TheLastWordsStand<TurnEndedTriggeredEffectContext>()),
                ])),
        ]));

    private static ICombatExpression<TContext, int> Due<TContext>() where TContext : class =>
        new CombatantCounterExpression<TContext>(Voice, FinalDue);

    // One ring's turn. `gated` is the second ring, which does not exist at all until the assembly falls
    // silent — before that its clock is absent and its announcement has never been made, so a turn of it
    // would put the first decree of the Force ring on the table a whole phase early.
    private static IEffectNode<TContext> TheRingTurns<TContext>(Order ring, bool gated) where TContext : class
    {
        var turn = Steps<TContext>(
        [
            new ModifyStatusStacksNode<TContext>(
                Voice, new StatusDefinitionId(ClockOf(ring)), Const<TContext>(-1)),
            new ConditionalEffectNode<TContext>(
                new NotExpression<TContext>(Has<TContext>(Voice, ClockOf(ring))),
                Steps<TContext>(
                [
                    // The old word goes, and which one it was does not have to be discovered: taking a
                    // status nobody wears off is free, so the whole ring is swept and exactly the one that
                    // was standing comes off.
                    .. RingOf(ring).SelectMany(d => new[]
                    {
                        Drop<TContext>(Voice, d.Id), Drop<TContext>(Applicant, d.Id),
                    }),
                    Enacted<TContext>(RingOf(ring), 0),
                ])),
        ]);

        return gated
            ? new ConditionalEffectNode<TContext>(Has<TContext>(Voice, AssemblySilentId), turn)
            : turn;
    }

    // The announcement chain, as an if / else-if ladder rather than a run of independent tests — the branch
    // that fires puts the NEXT announcement up, and a flat list would then let the later test for that same
    // announcement fire in the very same breath and skip a decree every time.
    private static IEffectNode<TContext> Enacted<TContext>(Decree[] ring, int index) where TContext : class
    {
        if (index >= ring.Length)
            return new NoOpEffectNode<TContext>();

        var decree = ring[index];
        var next = Follows(decree);

        return new ConditionalEffectNode<TContext>(
            Has<TContext>(Voice, NextId(decree)),
            Steps<TContext>(
            [
                Drop<TContext>(Voice, NextId(decree)),
                // Worn by BOTH, which is the whole of §11.4 and §11.11: each rule is read about whoever
                // wears it, so his word binds him by the same machinery that binds the player.
                Say<TContext>(Voice, decree.Id, 1),
                Say<TContext>(Applicant, decree.Id, 1),
                Say<TContext>(Voice, ClockOf(decree.Turns), decree.Rounds),
                Say<TContext>(Voice, NextId(next), 1),
            ]),
            @else: Enacted<TContext>(ring, index + 1));
    }

    // ── the phases, read off his own blood ────────────────────────────────────────────────────────────────

    private static EffectProgram<DamageReceivedTriggeredEffectContext> TheKingshipChanges() =>
        new(Steps<DamageReceivedTriggeredEffectContext>(
        [
            // §11.10 — "One word has governed you. There may be more."
            new ConditionalEffectNode<DamageReceivedTriggeredEffectContext>(
                Crossed<DamageReceivedTriggeredEffectContext>(SilenceAt, SilenceTaken),
                Steps<DamageReceivedTriggeredEffectContext>(
                [
                    Mark<DamageReceivedTriggeredEffectContext>(SilenceTaken),
                    Mark<DamageReceivedTriggeredEffectContext>(SilenceMove),
                    Drop<DamageReceivedTriggeredEffectContext>(Voice, OneWordId),
                    Say<DamageReceivedTriggeredEffectContext>(Voice, AssemblySilentId, 1),
                    // The second ring's first announcement. Its clock is absent, so the very next player
                    // turn end enacts it — announced now, real one player turn later, exactly as §11.3 says.
                    Say<DamageReceivedTriggeredEffectContext>(Voice, NextId(RingOf(Order.Force)[0]), 1),
                ])),
            // §11.12 — "You have lived beneath changing words. Then let the last ones remain."
            new ConditionalEffectNode<DamageReceivedTriggeredEffectContext>(
                Crossed<DamageReceivedTriggeredEffectContext>(LastWordsAt, LastWordsTaken),
                Steps<DamageReceivedTriggeredEffectContext>(
                [
                    Mark<DamageReceivedTriggeredEffectContext>(LastWordsTaken),
                    Mark<DamageReceivedTriggeredEffectContext>(FinalMove),
                    Drop<DamageReceivedTriggeredEffectContext>(Voice, AssemblySilentId),
                    Say<DamageReceivedTriggeredEffectContext>(Voice, ThreeWordsId, 1),
                    // The rotation stops: every standing word and every pending one is taken off the table,
                    // both clocks with them, so what is announced next is the whole of the rest of the fight.
                    .. Decrees.SelectMany(d => new IEffectNode<DamageReceivedTriggeredEffectContext>[]
                    {
                        Drop<DamageReceivedTriggeredEffectContext>(Voice, d.Id),
                        Drop<DamageReceivedTriggeredEffectContext>(Applicant, d.Id),
                        Drop<DamageReceivedTriggeredEffectContext>(Voice, NextId(d)),
                    }),
                    Drop<DamageReceivedTriggeredEffectContext>(Voice, WordStandsId),
                    Drop<DamageReceivedTriggeredEffectContext>(Voice, SecondWordStandsId),
                    new SetCombatantCounterNode<DamageReceivedTriggeredEffectContext>(
                        Voice, FinalDue, Const<DamageReceivedTriggeredEffectContext>(2), relative: false),
                    TheLastOrder<DamageReceivedTriggeredEffectContext>(),
                ])),
        ]));

    private static ICombatExpression<TContext, bool> Crossed<TContext>(int percent, CounterId taken)
        where TContext : class =>
        new AndExpression<TContext>(
            At(new CombatantHealthPercentageExpression<TContext>(Voice),
                ComparisonOperator.LessOrEqual, percent),
            At(new CombatantCounterExpression<TContext>(Voice, taken), ComparisonOperator.Equal, 0));

    private static IEffectNode<TContext> Mark<TContext>(CounterId flag) where TContext : class =>
        new SetCombatantCounterNode<TContext>(Voice, flag, Const<TContext>(1), relative: false);

    // THE DIRECTOR'S ONE REAL DECISION (§11.12: "These are not random"). He answers the turn that brought
    // him here — the shape of the very play that took him under 35 % — so the Final Order reads as a reply
    // rather than a roll. Four branches, tested in the order of how loudly the player said the thing.
    private static IEffectNode<TContext> TheLastOrder<TContext>() where TContext : class
    {
        var busy = At(new CardsPlayedThisTurnExpression<TContext>(Applicant),
            ComparisonOperator.GreaterOrEqual, 4);
        var heavy = At(new DamageDealtThisTurnExpression<TContext>(Applicant),
            ComparisonOperator.GreaterOrEqual, 30);
        var holding = At(
            new CombatantCurrentResourceExpression<TContext>(Applicant, StandardCombatIds.EnergyResource),
            ComparisonOperator.GreaterOrEqual, 2);

        return new ConditionalEffectNode<TContext>(busy, Announced<TContext>(NarrowKingdomId),
            @else: new ConditionalEffectNode<TContext>(heavy, Announced<TContext>(MeasuredKingdomId),
                @else: new ConditionalEffectNode<TContext>(holding, Announced<TContext>(AshKingdomId),
                    @else: Announced<TContext>(OrderedKingdomId))));
    }

    private static IEffectNode<TContext> Announced<TContext>(string kingdomId) where TContext : class
    {
        var kingdom = Kingdoms.First(k => k.Id == kingdomId);
        return Steps<TContext>(
        [
            Say<TContext>(Voice, kingdom.Id, 1),
            .. kingdom.Words.Select(w => Say<TContext>(Voice, NextId(Named(w)), 1)),
        ]);
    }

    // The preparation turn is over. Whichever kingdom was named makes its three words real, on both sides,
    // and there is no clock to give them: §11.17's whole point is that nothing more happens after this.
    private static IEffectNode<TContext> TheLastWordsStand<TContext>() where TContext : class =>
        Steps<TContext>(
        [
            .. Kingdoms.Select(k => (IEffectNode<TContext>)new ConditionalEffectNode<TContext>(
                Has<TContext>(Voice, k.Id),
                Steps<TContext>(
                [
                    .. k.Words.SelectMany(w => new IEffectNode<TContext>[]
                    {
                        Drop<TContext>(Voice, NextId(Named(w))),
                        Say<TContext>(Voice, w, 1),
                        Say<TContext>(Applicant, w, 1),
                    }),
                ]))),
        ]);

    // ── the decrees themselves ────────────────────────────────────────────────────────────────────────────

    private static StatusData TheDecree(Decree decree) =>
        Face(decree.Id, decree.Line, decree.Rule, stacks: false) with
        {
            CombatRules = RulesOf(decree),
            PassiveModifiers = ArithmeticOf(decree),
            Triggers = TriggersOf(decree),
        };

    private static StatusData TheAnnouncement(Decree decree) => Face(
        NextId(decree), "NEXT DECREE: " + decree.Line,
        decree.Rule + " Not yet in force — it enacts at the end of your next turn, and you may prepare.",
        stacks: false);

    private static IReadOnlyList<CombatRuleData>? RulesOf(Decree decree) => decree.Id switch
    {
        FourthWorkId => [new CombatRuleData(CombatRule.MaxCardsPerTurn, 4)],
        NoLikenessId => [new CombatRuleData(CombatRule.NoLikenessInSuccession, Tags:
            [Cards.CardAuthoring.DeedTag, Cards.CardAuthoring.WorkingTag,
             Cards.CardAuthoring.RiteTag, Cards.CardAuthoring.JunkTag])],
        FirstWithoutPriceId => [new CombatRuleData(CombatRule.NthCardOfTurnIsFree, 1)],
        ThirdWithoutPriceId => [new CombatRuleData(CombatRule.NthCardOfTurnIsFree, 3)],
        WithoutMeasureId => [new CombatRuleData(CombatRule.MinimumCardCost, 1)],
        UnspentTomorrowId => [new CombatRuleData(CombatRule.UnspentResourceCarries)],
        NoWorkReturnsId => [new CombatRuleData(CombatRule.PlayedCardsExhaust)],
        HandOfSevenId => [new CombatRuleData(CombatRule.MaxHandSize, 7)],
        _ => null,
    };

    // The three universal decrees are ARITHMETIC, not rules of play, so they are written where this engine
    // keeps arithmetic — as passive modifiers with a ceiling. Read from the combatant the amount is about,
    // which is exactly why wearing them on both sides is all §11.4 needs.
    private static IReadOnlyList<PassiveModifierData> ArithmeticOf(Decree decree) => decree.Id switch
    {
        BlowOfTwentyId =>
        [
            new PassiveModifierData(
                PassiveModifierPipeline.DamageDealt, PassiveModifierOperation.ClampMax, 20,
                RestrictDamageKind: null),
        ],
        WallOfThirtyId =>
        [
            new PassiveModifierData(
                PassiveModifierPipeline.BlockGain, PassiveModifierOperation.ClampMax, 30,
                RestrictDamageKind: null),
        ],
        FirstBlowNothingId =>
        [
            new PassiveModifierData(
                PassiveModifierPipeline.DamageDealt, PassiveModifierOperation.ClampMax, 0,
                RestrictDamageKind: null, OncePerTurn: true),
        ],
        _ => [],
    };

    private static IReadOnlyList<StatusTriggerData> TriggersOf(Decree decree) => decree.Id switch
    {
        CastOutReturnsId => [Trigger(TheCastOutReturn(), nameof(TriggerEvent.CardsDrawn))],
        _ => [],
    };

    // One card back, at the hand, once a round. It rides on CardsDrawn because a turn-start trigger runs
    // before there is a hand to put anything into — and it is bolted to the round number because CardsDrawn
    // fires again for every card any other effect draws.
    private static EffectProgram<CardsDrawnTriggeredEffectContext> TheCastOutReturn() =>
        new(new ConditionalEffectNode<CardsDrawnTriggeredEffectContext>(
            new AndExpression<CardsDrawnTriggeredEffectContext>(
                PlayersTurn<CardsDrawnTriggeredEffectContext>(),
                new ComparisonExpression<CardsDrawnTriggeredEffectContext>(
                    new CombatantCounterExpression<CardsDrawnTriggeredEffectContext>(Applicant, Returned),
                    ComparisonOperator.NotEqual,
                    new RoundNumberExpression<CardsDrawnTriggeredEffectContext>())),
            Steps<CardsDrawnTriggeredEffectContext>(
            [
                new SetCombatantCounterNode<CardsDrawnTriggeredEffectContext>(
                    Applicant, Returned,
                    new RoundNumberExpression<CardsDrawnTriggeredEffectContext>(), relative: false),
                new MoveCardToZoneNode<CardsDrawnTriggeredEffectContext>(
                    Applicant,
                    new CardInZoneExpression<CardsDrawnTriggeredEffectContext>(CardZone.ExhaustPile, 0),
                    CardZone.Hand),
            ])));

    // ── shared idiom ──────────────────────────────────────────────────────────────────────────────────────

    // His own way of putting a face on somebody. Utu's `Wear` names the sun as the source and there is no
    // sun in this room.
    private static IEffectNode<TContext> Say<TContext>(
        ICombatantTargetSelector body, string statusId, int stacks) where TContext : class =>
        new ApplyStatusNode<TContext>(
            body, new StatusDefinitionId(statusId), Const<TContext>(stacks), sourceSelector: Voice);
}
