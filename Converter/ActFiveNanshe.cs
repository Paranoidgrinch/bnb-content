using System.Text.Json;
using RogueDeck.Core.Combat;
using RogueDeck.Sandbox.Composition;
using RogueDeck.Scenario.Authoring;

namespace BnbContent.Converter;

// ACT V, the third god — NANSHE, KEEPER OF THE JUST RATION. She allocates, and she is not cruel about it.
//
// She is perfectly fair, which is the whole of the problem. Combat is cut into DISTRIBUTIONS of three days,
// and before the first day of each she shows the player the entire tablet: the three portions and the three
// things she will do with them. Nothing about her is hidden — she does not need it to be.
//
// Your natural share is YOURS. The Energy your build refills to and the cards your build draws are what the
// ration is measured from (§8.2: "she recognises what the player has legitimately acquired"), and inside that
// share she takes nothing at all. What she counts is everything BEYOND it:
//
//   Take Ahead        one point of Energy now, out of a later day
//   Draw Ahead        one card now, out of a later day
//   Borrowed Measure  what has been taken from later days and not yet given back
//   Return the Portion  an unspent point, and an unplayed card, pay a borrowed one back at the day's end
//
// A day never falls below one natural Energy and one natural card while any Distribution but the last is
// running (§8.7) — debt is DELAYED, never forgiven. And a Distribution that closes with nothing owed is
// answered: THE MEASURE HOLDS, and she gives back a portion of Block.
//
// Three phases, and only the ration changes — her attacks never do, because she is transparent and never
// vindictive. THE MEASURED PORTION (100–65 %) counts only her own allotment; card-made Energy and card-made
// draw are free. COUNT EVERY MEASURE (65–30 %) counts them too — "three measures entered; they were not
// allotted; that does not make them uncounted" — and from here one day of every Distribution has its basket
// SEALED, announced on Day I: on that day nothing may be moved between days at all. And at 30 %:
//
//   THE FINAL DISTRIBUTION — "There will be four more. There is no fifth."
//
// Everything left is poured into one store of 16 Energy and 20 cards, the minimum ration is switched off, and
// the player decides how much of it to spend on each of four days. On the fifth there is no portion.
public static partial class ActFive
{
    public const string NansheEnemyId = "nanshe_keeper_of_the_just_ration";
    public const string NansheEncounterId = "act_5_nanshe_keeper_of_the_just_ration";

    // Her rule, worn from the first round. Every trigger below hangs off it.
    public const string JustRationId = "the_just_ration";

    // What the player wears: the tablet itself (which is also the SIGHT that lets them read all three of her
    // days), the day they are on, what has been taken from later days, and what today's share came to.
    public const string RationTabletId = "the_ration_tablet";
    public const string DayOfDistributionId = "the_day_of_the_distribution";
    public const string BorrowedEnergyId = "borrowed_measure_energy";
    public const string BorrowedDrawId = "borrowed_measure_draw";
    public const string WithheldEnergyId = "withheld_energy";
    public const string WithheldDrawId = "withheld_draw";
    public const string BasketSealedId = "the_basket_is_sealed";
    public const string MeasureHoldsId = "the_measure_holds";

    // The Final Distribution's one store, and the four days it has to last.
    public const string FinalEnergyId = "final_ration_energy";
    public const string FinalDrawId = "final_ration_draw";
    public const string DaysRemainId = "the_days_that_remain";

    // Her two later phases and the announcement that precedes each.
    public const string CountEveryMeasureId = "count_every_measure";
    public const string MeasureCountedNextId = "every_measure_is_counted_next";
    public const string FinalDistributionId = "the_final_distribution";
    public const string FinalCalledId = "the_final_distribution_is_called";

    // The four patterns. Each is a chip on HER, and each says what all three of her days will be — which is
    // §8.1's "the full pattern is visible before Day I" written where the player already reads her.
    public const string ShelterId = "distribution_of_shelter";
    public const string LabourId = "distribution_of_labour";
    public const string RestId = "distribution_of_rest";
    public const string NeedId = "distribution_of_need";

    // Her own sheets, so the ration never rations its own paperwork.
    public const string RationSheetTag = "ration_sheet";
    public const string TakeAheadCardId = "take_ahead";
    public const string DrawAheadCardId = "draw_ahead";

    // 600, and BELOW both gods before her for the reason that is hers: Inanna's mechanic makes the player
    // faster and had to buy its own length back; Nanshe's makes the player SLOWER, and a fight that starves
    // the deck attacking it does not also need the hit points to be long.
    public const int NansheMaxHealth = 600;
    private const int CountEveryMeasureAt = 390;   // 65 % of 600
    private const int FinalDistributionAt = 180;   // 30 % of 600

    private const int DaysToADistribution = 3;
    private const int FinalDays = 4;
    private const int FinalEnergyStore = 16;       // the master's own example numbers (§8.13)
    private const int FinalDrawStore = 20;
    private const int MinimumEnergy = 1;           // §8.7, and switched off in the Final Distribution
    private const int MinimumDraw = 1;
    private const int MeasureHeldBlock = 12;

    // Counters, all on her. The natural draw she learnt on the first day, and the two running credits that
    // stop her counting her own hand-outs as though the player had conjured them.
    private static CounterId DrawBase => new("nanshe_draw_base");
    private static CounterId AllottedEnergy => new("nanshe_allotted_energy");
    private static CounterId AllottedDraw => new("nanshe_allotted_draw");
    private static CounterId MeasureTaken => new("nanshe_measure_taken");
    private static CounterId FinalTaken => new("nanshe_final_taken");

    private static ICombatantTargetSelector Nanshe => Bearer(JustRationId);

    // ── what the act hands to the rest of the converter ───────────────────────────────────────────────────

    public static IReadOnlyList<StatusData> NansheStatuses() =>
    [
        TheJustRation(), TheRationTablet(),
        DayOfDistribution(), BorrowedEnergy(), BorrowedDraw(),
        WithheldEnergy(), WithheldDraw(),
        BasketSealed(), MeasureHolds(),
        FinalEnergy(), FinalDraw(), DaysRemain(),
        CountEveryMeasure(), Announcement(MeasureCountedNextId, "Every Measure Is Counted Next",
            "She is about to stop distinguishing what she allotted from what you made yourself."),
        TheFinalDistribution(), Announcement(FinalCalledId, "The Final Distribution Is Called",
            "She is about to pour everything that is left into one store, and say how many days it has."),
        Pattern(ShelterId, "Distribution of Shelter",
            "Day I she strikes · Day II she shelters · Day III she strikes."),
        Pattern(LabourId, "Distribution of Labour",
            "Day I many small blows · Day II a day's wage · Day III the whole labour at once."),
        Pattern(RestId, "Distribution of Rest",
            "Day I the great portion · Day II she rests · Day III she rests."),
        Pattern(NeedId, "Distribution of Need",
            "Day I need · Day II need · Day III everything that was needed."),
    ];

    public static IReadOnlyList<CardData> NansheRationCards() => [TakeAhead(), DrawAhead()];

    public static EffectProgram<EnemyActionContext>? NansheIntent(string enemyId, string intentId) =>
        enemyId != NansheEnemyId ? null : intentId switch
        {
            // Distribution of Shelter.
            "the_shelter_given" => new EffectProgram<EnemyActionContext>(Hit(30)),
            "the_roof_held" => new EffectProgram<EnemyActionContext>(
                Seq(Guard(30), Debuff(Cards.Keywords.Doubt, 2))),
            "the_shelter_taken" => new EffectProgram<EnemyActionContext>(Hit(30)),
            // Distribution of Labour.
            "hands_at_work" => new EffectProgram<EnemyActionContext>(Seq(Hit(9), Hit(9), Hit(9))),
            "the_day_wage" => new EffectProgram<EnemyActionContext>(Hit(20)),
            "the_full_labour" => new EffectProgram<EnemyActionContext>(Hit(40)),
            // Distribution of Rest.
            "the_great_portion" => new EffectProgram<EnemyActionContext>(Hit(44)),
            "the_quiet_measure" => new EffectProgram<EnemyActionContext>(Guard(32)),
            "the_still_water" => new EffectProgram<EnemyActionContext>(
                Seq(Guard(28), Debuff(Cards.Keywords.Paperwork, 3))),
            // Distribution of Need.
            "the_lesser_need" => new EffectProgram<EnemyActionContext>(Hit(20)),
            "the_greater_need" => new EffectProgram<EnemyActionContext>(Hit(22)),
            "all_that_is_needed" => new EffectProgram<EnemyActionContext>(Hit(52)),
            // The two transitions.
            "three_measures_entered" => ThreeMeasuresEntered(),
            "there_will_be_four_more" => ThereWillBeFourMore(),
            _ => null,
        };

    // ── the tablet, as faces ──────────────────────────────────────────────────────────────────────────────

    public static StatusData TheJustRation() => new()
    {
        Id = JustRationId,
        NameKey = "The Just Ration",
        DescriptionKey =
            "Three days to a Distribution. Your natural Energy and your natural draw are your share and she "
            + "takes none of it — but anything you take beyond it comes out of a later day, and no day but "
            + "the last falls below 1 Energy and 1 card.",
        Polarity = StatusPolarity.Neutral,
        StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
        UsesStacks = false,
        Tags = [],
        PassiveModifiers = [],
        Triggers =
        [
            Trigger(OpenTheDay(), nameof(TriggerEvent.TurnStarted), StatusTriggerScope.Anywhere),
            Trigger(CloseTheDay(), nameof(TriggerEvent.TurnEnded), StatusTriggerScope.Anywhere),
            Trigger(CountTheDraw(), nameof(TriggerEvent.CardsDrawn), StatusTriggerScope.Anywhere),
            Trigger(CountTheGain(), nameof(TriggerEvent.ResourceGained), StatusTriggerScope.Anywhere),
            Trigger(TheRationAnnouncements(), nameof(TriggerEvent.DamageTaken)),
        ],
    };

    // THE TABLET IS ALSO THE SIGHT. §8.1 says she does not hide her future intents within a Distribution, and
    // the engine already has the exact faculty: a disclosure that reads past the ordinary telegraph. Two days
    // past today is all three days of the Distribution, which is the tablet.
    private static StatusData TheRationTablet() => Face(
        RationTabletId, "The Ration Tablet",
        "Her whole Distribution is on the table: all three days of it, before the first one begins. She "
        + "hides nothing here.", stacks: false) with
    { Disclosure = new DisclosureData(0, DaysToADistribution - 1) };

    private static StatusData DayOfDistribution() => Face(
        DayOfDistributionId, "Day of the Distribution",
        "Which of her three days this is. The pattern beside her says what she does with each.", stacks: true);

    private static StatusData BorrowedEnergy() => Face(
        BorrowedEnergyId, "Borrowed Measure: Energy",
        "Energy taken out of later days. It comes off the start of each day until it is worked off — never "
        + "below 1 — and an unspent point at the day's end pays one back.", stacks: true);

    private static StatusData BorrowedDraw() => Face(
        BorrowedDrawId, "Borrowed Measure: Draw",
        "Cards taken out of later days. Each costs you a card at the start of a later day — never below 1 — "
        + "and a card still in hand at the day's end pays one back.", stacks: true);

    private static StatusData WithheldEnergy() => Face(
        WithheldEnergyId, "Withheld: Energy",
        "What today's portion is short by, because it was already taken. Nothing is being punished — this "
        + "is yesterday's Energy, spent.", stacks: true);

    private static StatusData WithheldDraw() => Face(
        WithheldDrawId, "Withheld: Draw",
        "How many cards today's portion is short by. She is not stopping your draw; she has already given "
        + "you these cards.", stacks: true) with
    {
        PassiveModifiers =
        [
            new PassiveModifierData(
                PassiveModifierPipeline.TurnStartDraw, PassiveModifierOperation.AddPerStack, -1,
                RestrictDamageKind: null),
        ],
    };

    private static StatusData BasketSealed() => Face(
        BasketSealedId, "The Basket Is Sealed",
        "Which day of this Distribution is fixed. On that day nothing may be moved between days: no Take "
        + "Ahead, no Draw Ahead. It is little. It is still the portion.", stacks: true);

    private static StatusData MeasureHolds() => Face(
        MeasureHoldsId, "The Measure Holds",
        $"You closed a Distribution owing nothing, and she answered with {MeasureHeldBlock} Block.",
        stacks: false);

    private static StatusData FinalEnergy() => Face(
        FinalEnergyId, "Final Ration: Energy",
        "All the Energy left in this combat, in one store. Each day takes your natural share out of it, and "
        + "every point you make yourself comes out of it too.", stacks: true);

    private static StatusData FinalDraw() => Face(
        FinalDrawId, "Final Ration: Draw",
        "All the cards left in this combat, in one store. Each day takes your natural draw out of it, and "
        + "every card you draw yourself comes out of it too.", stacks: true);

    private static StatusData DaysRemain() => Face(
        DaysRemainId, "The Days That Remain",
        "Days that still receive a portion. After them there is no fifth: no Energy, no draw, and no "
        + "minimum. What you have kept is what you have.", stacks: true);

    private static StatusData CountEveryMeasure() => Face(
        CountEveryMeasureId, "Count Every Measure",
        "Energy and cards you make for yourself are counted against later days exactly as her own portions "
        + "are. Cost reduction, retention and cards already in hand are not draw, and are not counted.",
        stacks: false);

    private static StatusData TheFinalDistribution() => Face(
        FinalDistributionId, "The Final Distribution",
        "One store, four days, no minimum ration. Everything you take, she takes out of what is left.",
        stacks: false);

    private static StatusData Pattern(string id, string name, string description) =>
        Face(id, name, description, stacks: false);

    // ── the day opens ─────────────────────────────────────────────────────────────────────────────────────

    // Everything the ration does happens here, in the one window where it can: the Energy pool has just been
    // refilled to the build's own maximum (so THAT is the natural share, read live), and the day's draw has
    // not happened yet (so a modifier written now is a modifier the draw obeys).
    private static EffectProgram<TurnStartedTriggeredEffectContext> OpenTheDay()
    {
        var day = Day<TurnStartedTriggeredEffectContext>();
        var final = Has<TurnStartedTriggeredEffectContext>(Nanshe, FinalDistributionId);
        var daysLeft = Stacks<TurnStartedTriggeredEffectContext>(Applicant, DaysRemainId);

        // Her share of Energy: the pool she has just filled. Her share of draw: what the first day of the
        // fight showed her the build draws.
        var energyShare = new CombatantMaxResourceExpression<TurnStartedTriggeredEffectContext>(
            Applicant, StandardCombatIds.EnergyResource);
        var drawShare = new CombatantCounterExpression<TurnStartedTriggeredEffectContext>(Nanshe, DrawBase);

        return new EffectProgram<TurnStartedTriggeredEffectContext>(
            new ConditionalEffectNode<TurnStartedTriggeredEffectContext>(
                PlayersTurn<TurnStartedTriggeredEffectContext>(),
                new CausalSequenceEffectNode<TurnStartedTriggeredEffectContext>(
                [
                    Rewrite(DayOfDistributionId, day),
                    new ConditionalEffectNode<TurnStartedTriggeredEffectContext>(
                        new ComparisonExpression<TurnStartedTriggeredEffectContext>(
                            day, ComparisonOperator.Equal, Const<TurnStartedTriggeredEffectContext>(1)),
                        OpenTheDistribution()),
                    new RemoveStatusNode<TurnStartedTriggeredEffectContext>(
                        Applicant, new StatusDefinitionId(MeasureHoldsId)),
                    Ration(final, daysLeft, energyShare, FinalEnergyId, BorrowedEnergyId, WithheldEnergyId,
                        MinimumEnergy, energy: true),
                    Ration(final, daysLeft, drawShare, FinalDrawId, BorrowedDrawId, WithheldDrawId,
                        MinimumDraw, energy: false),
                    DealTheSheets(day),
                ])));
    }

    // Day I: the next pattern goes up where her telegraph is read, and — once every measure is counted — the
    // day whose basket is sealed is named before any of the three has been lived through (§8.11).
    private static IEffectNode<TurnStartedTriggeredEffectContext> OpenTheDistribution()
    {
        string[] patterns = [ShelterId, LabourId, RestId, NeedId];
        var which = new RemainderExpression<TurnStartedTriggeredEffectContext>(
            Distribution<TurnStartedTriggeredEffectContext>(),
            Const<TurnStartedTriggeredEffectContext>(patterns.Length));

        // Day II or Day III, alternating, so the seal is never the same day twice running.
        var sealedDay = new AddExpression<TurnStartedTriggeredEffectContext>(
            Const<TurnStartedTriggeredEffectContext>(2),
            new RemainderExpression<TurnStartedTriggeredEffectContext>(
                Distribution<TurnStartedTriggeredEffectContext>(),
                Const<TurnStartedTriggeredEffectContext>(2)));

        return new CausalSequenceEffectNode<TurnStartedTriggeredEffectContext>(
        [
            .. patterns.Select(id => (IEffectNode<TurnStartedTriggeredEffectContext>)
                new RemoveStatusNode<TurnStartedTriggeredEffectContext>(
                    Nanshe, new StatusDefinitionId(id))),
            .. patterns.Select((id, index) => (IEffectNode<TurnStartedTriggeredEffectContext>)
                new ConditionalEffectNode<TurnStartedTriggeredEffectContext>(
                    new ComparisonExpression<TurnStartedTriggeredEffectContext>(
                        which, ComparisonOperator.Equal, Const<TurnStartedTriggeredEffectContext>(index)),
                    new ApplyStatusNode<TurnStartedTriggeredEffectContext>(
                        Nanshe, new StatusDefinitionId(id), Const<TurnStartedTriggeredEffectContext>(1),
                        sourceSelector: Nanshe))),
            new RemoveStatusNode<TurnStartedTriggeredEffectContext>(
                Applicant, new StatusDefinitionId(BasketSealedId)),
            new ConditionalEffectNode<TurnStartedTriggeredEffectContext>(
                new AndExpression<TurnStartedTriggeredEffectContext>(
                    Has<TurnStartedTriggeredEffectContext>(Nanshe, CountEveryMeasureId),
                    new NotExpression<TurnStartedTriggeredEffectContext>(
                        Has<TurnStartedTriggeredEffectContext>(Nanshe, FinalDistributionId))),
                new ApplyStatusNode<TurnStartedTriggeredEffectContext>(
                    Applicant, new StatusDefinitionId(BasketSealedId), sealedDay, sourceSelector: Nanshe)),
        ]);
    }

    // ONE ARITHMETIC FOR BOTH COLUMNS, because Energy and draw are the same promise measured in different
    // units — and a player who has understood one row of the tablet has understood the other.
    //
    // Outside the Final Distribution the day's share is the build's own, less whatever was taken out of it,
    // and never less than the minimum: what the minimum protects is not forgiven, it waits (§8.7). Inside it
    // there is no minimum and no waiting — the day takes what the one store still holds, and when the four
    // days are spent it takes nothing at all.
    private static IEffectNode<TurnStartedTriggeredEffectContext> Ration(
        ICombatExpression<TurnStartedTriggeredEffectContext, bool> final,
        ICombatExpression<TurnStartedTriggeredEffectContext, int> daysLeft,
        ICombatExpression<TurnStartedTriggeredEffectContext, int> share,
        string storeId, string borrowedId, string withheldId, int minimum, bool energy)
    {
        var store = Stacks<TurnStartedTriggeredEffectContext>(Applicant, storeId);
        var borrowed = Stacks<TurnStartedTriggeredEffectContext>(Applicant, borrowedId);

        // The Final Distribution: everything the store can still cover, and nothing at all on the fifth day.
        // The fifth day is written as a SIGN rather than as a branch, because this is a number inside another
        // number's arithmetic and an effect program branches on nodes, not on values: with no day left the
        // sign is zero and the whole portion multiplies out.
        var fromStore = new MultiplyExpression<TurnStartedTriggeredEffectContext>(
            new SignExpression<TurnStartedTriggeredEffectContext>(daysLeft),
            new MinExpression<TurnStartedTriggeredEffectContext>(share, store));

        // Everywhere else: what is owed, as far as the minimum allows.
        var fromDebt = new MaxExpression<TurnStartedTriggeredEffectContext>(
            Const<TurnStartedTriggeredEffectContext>(0),
            new MinExpression<TurnStartedTriggeredEffectContext>(
                borrowed,
                new SubtractExpression<TurnStartedTriggeredEffectContext>(
                    share, Const<TurnStartedTriggeredEffectContext>(minimum))));

        var given = new SubtractExpression<TurnStartedTriggeredEffectContext>(share, fromDebt);
        var withheldNow = fromDebt;

        // ORDER MATTERS AND IT IS NOT OBVIOUS: every one of these numbers is an EXPRESSION over the live
        // state, evaluated where it stands. Both of them read the very row the last step writes down, so the
        // row that explains the day has to be written BEFORE the account it explains is settled — otherwise
        // it is computed against a debt that has already been worked off and reads zero on the one day it
        // had something to say.
        return new ConditionalEffectNode<TurnStartedTriggeredEffectContext>(
            final,
            new CausalSequenceEffectNode<TurnStartedTriggeredEffectContext>(
            [
                Give(energy, fromStore, share),
                Rewrite(withheldId,
                    new SubtractExpression<TurnStartedTriggeredEffectContext>(share, fromStore)),
                new ModifyStatusStacksNode<TurnStartedTriggeredEffectContext>(
                    Applicant, new StatusDefinitionId(storeId),
                    new NegateExpression<TurnStartedTriggeredEffectContext>(fromStore)),
            ]),
            new CausalSequenceEffectNode<TurnStartedTriggeredEffectContext>(
            [
                Give(energy, given, share),
                Rewrite(withheldId, withheldNow),
                new ModifyStatusStacksNode<TurnStartedTriggeredEffectContext>(
                    Applicant, new StatusDefinitionId(borrowedId),
                    new NegateExpression<TurnStartedTriggeredEffectContext>(withheldNow)),
            ]));
    }

    // Handing over the day's portion. Energy is already in the pool, so what happens to it is a SUBTRACTION
    // (the refill runs before this trigger, which is the only reason any of this is possible); draw has not
    // happened yet, so what is written is the credit the coming draw is measured against.
    private static IEffectNode<TurnStartedTriggeredEffectContext> Give(
        bool energy,
        ICombatExpression<TurnStartedTriggeredEffectContext, int> given,
        ICombatExpression<TurnStartedTriggeredEffectContext, int> share) =>
        energy
            ? new CausalSequenceEffectNode<TurnStartedTriggeredEffectContext>(
            [
                new LoseResourceNode<TurnStartedTriggeredEffectContext>(
                    Applicant, StandardCombatIds.EnergyResource,
                    new SubtractExpression<TurnStartedTriggeredEffectContext>(share, given)),
                new SetCombatantCounterNode<TurnStartedTriggeredEffectContext>(
                    Nanshe, AllottedEnergy, Const<TurnStartedTriggeredEffectContext>(0), relative: false),
            ])
            : new SetCombatantCounterNode<TurnStartedTriggeredEffectContext>(
                Nanshe, AllottedDraw, given, relative: false);

    // The two ways of moving a portion between days, laid in hand each morning — unless this is the day whose
    // basket she sealed, or there is nothing ahead left to take from.
    private static IEffectNode<TurnStartedTriggeredEffectContext> DealTheSheets(
        ICombatExpression<TurnStartedTriggeredEffectContext, int> day)
    {
        var unsealed = new NotExpression<TurnStartedTriggeredEffectContext>(
            new ComparisonExpression<TurnStartedTriggeredEffectContext>(
                Stacks<TurnStartedTriggeredEffectContext>(Applicant, BasketSealedId),
                ComparisonOperator.Equal, day));

        IEffectNode<TurnStartedTriggeredEffectContext> Sheet(
            string cardId, ICombatExpression<TurnStartedTriggeredEffectContext, bool> available) =>
            new ConditionalEffectNode<TurnStartedTriggeredEffectContext>(
                new AndExpression<TurnStartedTriggeredEffectContext>(unsealed, available),
                new CreateCardInstanceNode<TurnStartedTriggeredEffectContext>(
                    Applicant, new CardDefinitionId(cardId), CardZone.Hand,
                    Const<TurnStartedTriggeredEffectContext>(1)));

        return new CausalSequenceEffectNode<TurnStartedTriggeredEffectContext>(
        [
            Sheet(TakeAheadCardId, CanTakeEnergy<TurnStartedTriggeredEffectContext>()),
            Sheet(DrawAheadCardId, CanTakeDraw<TurnStartedTriggeredEffectContext>()),
        ]);
    }

    // ── the day closes ────────────────────────────────────────────────────────────────────────────────────

    // RETURN THE PORTION (§8.5), then the account for the Distribution, then one of the four days. She does
    // not confiscate what was not used: an unspent point of Energy and a card still in hand each pay back one
    // borrowed measure. In the Final Distribution nothing is returned — there is no later day to return it to.
    private static EffectProgram<TurnEndedTriggeredEffectContext> CloseTheDay()
    {
        var final = Has<TurnEndedTriggeredEffectContext>(Nanshe, FinalDistributionId);

        IEffectNode<TurnEndedTriggeredEffectContext> Return(
            string borrowedId, ICombatExpression<TurnEndedTriggeredEffectContext, int> unused) =>
            new ModifyStatusStacksNode<TurnEndedTriggeredEffectContext>(
                Applicant, new StatusDefinitionId(borrowedId),
                new NegateExpression<TurnEndedTriggeredEffectContext>(
                    new MinExpression<TurnEndedTriggeredEffectContext>(
                        unused, Stacks<TurnEndedTriggeredEffectContext>(Applicant, borrowedId))));

        var owesNothing = new AndExpression<TurnEndedTriggeredEffectContext>(
            new ComparisonExpression<TurnEndedTriggeredEffectContext>(
                Stacks<TurnEndedTriggeredEffectContext>(Applicant, BorrowedEnergyId),
                ComparisonOperator.Equal, Const<TurnEndedTriggeredEffectContext>(0)),
            new ComparisonExpression<TurnEndedTriggeredEffectContext>(
                Stacks<TurnEndedTriggeredEffectContext>(Applicant, BorrowedDrawId),
                ComparisonOperator.Equal, Const<TurnEndedTriggeredEffectContext>(0)));

        // The account, taken on the last day of a Distribution (§8.12). Modest on purpose, and it arrives as
        // Block at the player's own turn end, which is the one moment Block granted to them survives to be
        // worth anything.
        var theAccount = new ConditionalEffectNode<TurnEndedTriggeredEffectContext>(
            new AndExpression<TurnEndedTriggeredEffectContext>(
                new ComparisonExpression<TurnEndedTriggeredEffectContext>(
                    Day<TurnEndedTriggeredEffectContext>(), ComparisonOperator.Equal,
                    Const<TurnEndedTriggeredEffectContext>(DaysToADistribution)),
                owesNothing),
            new CausalSequenceEffectNode<TurnEndedTriggeredEffectContext>(
            [
                new GainBlockNode<TurnEndedTriggeredEffectContext>(
                    Applicant, Const<TurnEndedTriggeredEffectContext>(MeasureHeldBlock)),
                new ApplyStatusNode<TurnEndedTriggeredEffectContext>(
                    Applicant, new StatusDefinitionId(MeasureHoldsId),
                    Const<TurnEndedTriggeredEffectContext>(1), sourceSelector: Nanshe),
            ]));

        return new EffectProgram<TurnEndedTriggeredEffectContext>(
            new ConditionalEffectNode<TurnEndedTriggeredEffectContext>(
                PlayersTurn<TurnEndedTriggeredEffectContext>(),
                new ConditionalEffectNode<TurnEndedTriggeredEffectContext>(
                    final,
                    new ModifyStatusStacksNode<TurnEndedTriggeredEffectContext>(
                        Applicant, new StatusDefinitionId(DaysRemainId),
                        Const<TurnEndedTriggeredEffectContext>(-1)),
                    new CausalSequenceEffectNode<TurnEndedTriggeredEffectContext>(
                    [
                        Return(BorrowedEnergyId,
                            new CombatantCurrentResourceExpression<TurnEndedTriggeredEffectContext>(
                                Applicant, StandardCombatIds.EnergyResource)),
                        Return(BorrowedDrawId,
                            new CombatantZoneCardCountExpression<TurnEndedTriggeredEffectContext>(
                                Applicant, CardZone.Hand)),
                        theAccount,
                    ]))));
    }

    // ── what the player makes for themselves ──────────────────────────────────────────────────────────────

    // Her own hand-out is not the player's doing, so it is absorbed first — a running credit rather than a
    // per-turn flag, because Take Ahead can promise a point that only arrives several plays later.
    private static EffectProgram<ResourceGainedTriggeredEffectContext> CountTheGain()
    {
        var amount = new EventAmountExpression<ResourceGainedTriggeredEffectContext>();
        var credit = new CombatantCounterExpression<ResourceGainedTriggeredEffectContext>(
            Nanshe, AllottedEnergy);
        var absorbed = new MinExpression<ResourceGainedTriggeredEffectContext>(credit, amount);
        var rest = new SubtractExpression<ResourceGainedTriggeredEffectContext>(amount, absorbed);

        return new EffectProgram<ResourceGainedTriggeredEffectContext>(
            new ConditionalEffectNode<ResourceGainedTriggeredEffectContext>(
                PlayersTurn<ResourceGainedTriggeredEffectContext>(),
                // The charge is read BEFORE the credit is spent, for the same reason the row above is: what
                // is left over is a difference against a credit that the next step is about to change.
                new CausalSequenceEffectNode<ResourceGainedTriggeredEffectContext>(
                [
                    Count<ResourceGainedTriggeredEffectContext>(rest, FinalEnergyId, BorrowedEnergyId),
                    new SetCombatantCounterNode<ResourceGainedTriggeredEffectContext>(
                        Nanshe, AllottedEnergy,
                        new NegateExpression<ResourceGainedTriggeredEffectContext>(absorbed), relative: true),
                ])));
    }

    // The same for cards, and the first draw of the day is the one she allotted. The natural draw is learnt
    // here too, on the first day of the fight — the one day nothing has been taken out of yet, which is why
    // it is the day that says what the build's own portion is.
    private static EffectProgram<CardsDrawnTriggeredEffectContext> CountTheDraw()
    {
        var amount = new EventAmountExpression<CardsDrawnTriggeredEffectContext>();
        var credit = new CombatantCounterExpression<CardsDrawnTriggeredEffectContext>(Nanshe, AllottedDraw);
        var absorbed = new MinExpression<CardsDrawnTriggeredEffectContext>(credit, amount);
        var rest = new SubtractExpression<CardsDrawnTriggeredEffectContext>(amount, absorbed);

        var learn = new ConditionalEffectNode<CardsDrawnTriggeredEffectContext>(
            new AndExpression<CardsDrawnTriggeredEffectContext>(
                new ComparisonExpression<CardsDrawnTriggeredEffectContext>(
                    new RoundNumberExpression<CardsDrawnTriggeredEffectContext>(),
                    ComparisonOperator.Equal, Const<CardsDrawnTriggeredEffectContext>(1)),
                new ComparisonExpression<CardsDrawnTriggeredEffectContext>(
                    new CombatantCounterExpression<CardsDrawnTriggeredEffectContext>(Nanshe, DrawBase),
                    ComparisonOperator.Equal, Const<CardsDrawnTriggeredEffectContext>(0))),
            new CausalSequenceEffectNode<CardsDrawnTriggeredEffectContext>(
            [
                new SetCombatantCounterNode<CardsDrawnTriggeredEffectContext>(
                    Nanshe, DrawBase, amount, relative: false),
                // What she has just learnt she also allotted: the first hand of the fight is a portion.
                new SetCombatantCounterNode<CardsDrawnTriggeredEffectContext>(
                    Nanshe, AllottedDraw, amount, relative: false),
            ]));

        return new EffectProgram<CardsDrawnTriggeredEffectContext>(
            new ConditionalEffectNode<CardsDrawnTriggeredEffectContext>(
                PlayersTurn<CardsDrawnTriggeredEffectContext>(),
                new CausalSequenceEffectNode<CardsDrawnTriggeredEffectContext>(
                [
                    learn,
                    Count<CardsDrawnTriggeredEffectContext>(rest, FinalDrawId, BorrowedDrawId),
                    new SetCombatantCounterNode<CardsDrawnTriggeredEffectContext>(
                        Nanshe, AllottedDraw,
                        new NegateExpression<CardsDrawnTriggeredEffectContext>(absorbed), relative: true),
                ])));
    }

    // What she does with a measure that entered without being allotted. In the first phase: nothing at all —
    // that is what §8.8 means by the player learning the system. After that it is counted, and where it is
    // counted depends only on whether there is still a later day or only the one store.
    private static IEffectNode<TContext> Count<TContext>(
        ICombatExpression<TContext, int> amount, string storeId, string borrowedId) where TContext : class =>
        new ConditionalEffectNode<TContext>(
            new AndExpression<TContext>(
                new ComparisonExpression<TContext>(amount, ComparisonOperator.GreaterOrEqual, Const<TContext>(1)),
                Has<TContext>(Nanshe, CountEveryMeasureId)),
            Charge<TContext>(amount, storeId, borrowedId));

    // Writing a measure down. One store while the Final Distribution runs, a later day everywhere else.
    private static IEffectNode<TContext> Charge<TContext>(
        ICombatExpression<TContext, int> amount, string storeId, string borrowedId) where TContext : class =>
        new ConditionalEffectNode<TContext>(
            Has<TContext>(Nanshe, FinalDistributionId),
            new ModifyStatusStacksNode<TContext>(
                Applicant, new StatusDefinitionId(storeId),
                new NegateExpression<TContext>(amount)),
            new ApplyStatusNode<TContext>(
                Applicant, new StatusDefinitionId(borrowedId), amount, sourceSelector: Nanshe));

    // ── the two transitions ───────────────────────────────────────────────────────────────────────────────

    private static EffectProgram<DamageReceivedTriggeredEffectContext> TheRationAnnouncements()
    {
        var health = new CombatantCurrentHealthExpression<DamageReceivedTriggeredEffectContext>(Nanshe);

        IEffectNode<DamageReceivedTriggeredEffectContext> Announce(int band, CounterId taken, string marker) =>
            new ConditionalEffectNode<DamageReceivedTriggeredEffectContext>(
                new AndExpression<DamageReceivedTriggeredEffectContext>(
                    new ComparisonExpression<DamageReceivedTriggeredEffectContext>(
                        health, ComparisonOperator.LessOrEqual,
                        Const<DamageReceivedTriggeredEffectContext>(band)),
                    new ComparisonExpression<DamageReceivedTriggeredEffectContext>(
                        new CombatantCounterExpression<DamageReceivedTriggeredEffectContext>(Nanshe, taken),
                        ComparisonOperator.Equal, Const<DamageReceivedTriggeredEffectContext>(0))),
                new CausalSequenceEffectNode<DamageReceivedTriggeredEffectContext>(
                [
                    new SetCombatantCounterNode<DamageReceivedTriggeredEffectContext>(
                        Nanshe, taken, Const<DamageReceivedTriggeredEffectContext>(1), relative: false),
                    new ApplyStatusNode<DamageReceivedTriggeredEffectContext>(
                        Nanshe, new StatusDefinitionId(marker),
                        Const<DamageReceivedTriggeredEffectContext>(1), sourceSelector: Nanshe),
                ]));

        return new EffectProgram<DamageReceivedTriggeredEffectContext>(
            new CausalSequenceEffectNode<DamageReceivedTriggeredEffectContext>(
            [
                Announce(CountEveryMeasureAt, MeasureTaken, MeasureCountedNextId),
                Announce(FinalDistributionAt, FinalTaken, FinalCalledId),
            ]));
    }

    // "Three measures entered. They were not allotted. That does not make them uncounted."
    private static EffectProgram<EnemyActionContext> ThreeMeasuresEntered() =>
        new(Seq(
            Hit(22),
            new ApplyStatusNode<EnemyActionContext>(
                Nanshe, new StatusDefinitionId(CountEveryMeasureId), Const<EnemyActionContext>(1),
                sourceSelector: Nanshe),
            new RemoveStatusNode<EnemyActionContext>(
                Nanshe, new StatusDefinitionId(MeasureCountedNextId))));

    // "There will be four more. There is no fifth." Every account is closed and everything that is left goes
    // into one store — which is why the Borrowed Measures go: there is no later day for them to come out of.
    private static EffectProgram<EnemyActionContext> ThereWillBeFourMore() =>
        new(Seq(
            Hit(26),
            new RemoveStatusNode<EnemyActionContext>(Applicant, new StatusDefinitionId(BorrowedEnergyId)),
            new RemoveStatusNode<EnemyActionContext>(Applicant, new StatusDefinitionId(BorrowedDrawId)),
            new RemoveStatusNode<EnemyActionContext>(Applicant, new StatusDefinitionId(BasketSealedId)),
            new ApplyStatusNode<EnemyActionContext>(
                Applicant, new StatusDefinitionId(FinalEnergyId),
                Const<EnemyActionContext>(FinalEnergyStore), sourceSelector: Nanshe),
            new ApplyStatusNode<EnemyActionContext>(
                Applicant, new StatusDefinitionId(FinalDrawId),
                Const<EnemyActionContext>(FinalDrawStore), sourceSelector: Nanshe),
            // Exactly four, and no window is spent writing it: she speaks AFTER the player's turn has ended,
            // so the first day that reads this store is the next one.
            new ApplyStatusNode<EnemyActionContext>(
                Applicant, new StatusDefinitionId(DaysRemainId),
                Const<EnemyActionContext>(FinalDays), sourceSelector: Nanshe),
            new ApplyStatusNode<EnemyActionContext>(
                Nanshe, new StatusDefinitionId(FinalDistributionId), Const<EnemyActionContext>(1),
                sourceSelector: Nanshe),
            new RemoveStatusNode<EnemyActionContext>(
                Nanshe, new StatusDefinitionId(FinalCalledId))));

    // ── the player's two levers ───────────────────────────────────────────────────────────────────────────

    // TAKE AHEAD (§8.3). The point is HELD rather than gained, which is the only way a promise of Energy can
    // survive a pool that is already full — a build allotted three and taking three ahead is the master's
    // 6 / 2 / 4 day, and the sixth point waits for the moment the third is spent.
    private static CardData TakeAhead() => new()
    {
        Id = TakeAheadCardId,
        NameKey = "Take Ahead",
        DescriptionKey =
            "Take 1 Energy out of a later day. It arrives the moment you run out. Tomorrow is simply smaller.",
        Costs = [],
        Tags = [new TagId(RationSheetTag), new TagId(Cards.CardAuthoring.TemporaryTag)],
        Program = new EffectProgram<CardPlayContext>(
            new ConditionalEffectNode<CardPlayContext>(
                CanTakeEnergy<CardPlayContext>(),
                new CausalSequenceEffectNode<CardPlayContext>(
                [
                    new SetCombatantCounterNode<CardPlayContext>(
                        Nanshe, AllottedEnergy, Const<CardPlayContext>(1), relative: true),
                    HeldEnergy.Hold<CardPlayContext>(1),
                    Charge<CardPlayContext>(Const<CardPlayContext>(1), FinalEnergyId, BorrowedEnergyId),
                    Reoffer(TakeAheadCardId, CanTakeEnergy<CardPlayContext>()),
                ]))),
        PlayedCardDestinationZone = CardZone.ExhaustPile,
        TurnEndHandDestinationZone = CardZone.ExhaustPile,
    };

    // DRAW AHEAD (§8.4). The system moves draw QUANTITY, never particular future cards — so this is a draw,
    // and what it costs is a card off a later day.
    private static CardData DrawAhead() => new()
    {
        Id = DrawAheadCardId,
        NameKey = "Draw Ahead",
        DescriptionKey = "Draw 1 card out of a later day. The quantity moves; the cards are still yours.",
        Costs = [],
        Tags = [new TagId(RationSheetTag), new TagId(Cards.CardAuthoring.TemporaryTag)],
        Program = new EffectProgram<CardPlayContext>(
            new ConditionalEffectNode<CardPlayContext>(
                CanTakeDraw<CardPlayContext>(),
                new CausalSequenceEffectNode<CardPlayContext>(
                [
                    new SetCombatantCounterNode<CardPlayContext>(
                        Nanshe, AllottedDraw, Const<CardPlayContext>(1), relative: true),
                    new DrawCardsNode<CardPlayContext>(Applicant, Const<CardPlayContext>(1)),
                    Charge<CardPlayContext>(Const<CardPlayContext>(1), FinalDrawId, BorrowedDrawId),
                    Reoffer(DrawAheadCardId, CanTakeDraw<CardPlayContext>()),
                ]))),
        PlayedCardDestinationZone = CardZone.ExhaustPile,
        TurnEndHandDestinationZone = CardZone.ExhaustPile,
    };

    // WHAT IS LEFT TO TAKE, and the bound is the master's own (§8.3): you borrow from a LATER DAY, and the
    // later days are the ones still standing in this Distribution — so the whole of what may be taken ahead
    // is the days ahead times the day's share, and on Day III there is nothing ahead to take.
    //
    // It is also the reason the sheet can re-offer itself at all. A lever that hands itself back for ever is
    // an infinite turn for anything that plays greedily, which is every walker this game is measured with;
    // Inanna's offering sheets stop at a debt of zero, and this one stops at the end of the Distribution.
    // Overconsumption BEYOND the Distribution is still reachable — that is exactly what §8.6 says a Borrowed
    // Measure is — but it is reached by making resources, not by asking her for them.
    private static ICombatExpression<TContext, bool> Available<TContext>(
        string storeId, string borrowedId, ICombatExpression<TContext, int> share) where TContext : class
    {
        var ahead = new MultiplyExpression<TContext>(
            new SubtractExpression<TContext>(Const<TContext>(DaysToADistribution), Day<TContext>()),
            share);

        return new OrExpression<TContext>(
            new AndExpression<TContext>(
                new NotExpression<TContext>(Has<TContext>(Nanshe, FinalDistributionId)),
                new ComparisonExpression<TContext>(
                    Stacks<TContext>(Applicant, borrowedId), ComparisonOperator.Less, ahead)),
            new AndExpression<TContext>(
                Has<TContext>(Nanshe, FinalDistributionId),
                new ComparisonExpression<TContext>(
                    Stacks<TContext>(Applicant, storeId),
                    ComparisonOperator.GreaterOrEqual, Const<TContext>(1))));
    }

    private static ICombatExpression<TContext, bool> CanTakeEnergy<TContext>() where TContext : class =>
        Available<TContext>(FinalEnergyId, BorrowedEnergyId,
            new CombatantMaxResourceExpression<TContext>(Applicant, StandardCombatIds.EnergyResource));

    private static ICombatExpression<TContext, bool> CanTakeDraw<TContext>() where TContext : class =>
        Available<TContext>(FinalDrawId, BorrowedDrawId,
            new CombatantCounterExpression<TContext>(Nanshe, DrawBase));

    // The sheet stands as long as there is anything left to move: a day the player may reallocate exactly
    // once is not an allocation decision.
    private static IEffectNode<CardPlayContext> Reoffer(
        string cardId, ICombatExpression<CardPlayContext, bool> available) =>
        new ConditionalEffectNode<CardPlayContext>(
            available,
            new CreateCardInstanceNode<CardPlayContext>(
                Applicant, new CardDefinitionId(cardId), CardZone.Hand, Const<CardPlayContext>(1)));

    // ── shared idioms ─────────────────────────────────────────────────────────────────────────────────────

    // Her calendar is the ROUND, and deliberately so: her twelve actions are one cycle of four Distributions,
    // so "which day is it" and "which pattern is she on" are pure functions of the round — which is what lets
    // the tablet show all three days in advance at all. The engine's own telegraph projection reads the
    // enemy's action for round + n, and an intent chosen from a counter would answer the same thing three
    // times over.
    private static ICombatExpression<TContext, int> Day<TContext>() where TContext : class =>
        new AddExpression<TContext>(
            new RemainderExpression<TContext>(Since<TContext>(), Const<TContext>(DaysToADistribution)),
            Const<TContext>(1));

    private static ICombatExpression<TContext, int> Distribution<TContext>() where TContext : class =>
        new DivideExpression<TContext>(Since<TContext>(), Const<TContext>(DaysToADistribution));

    private static ICombatExpression<TContext, int> Since<TContext>() where TContext : class =>
        new SubtractExpression<TContext>(new RoundNumberExpression<TContext>(), Const<TContext>(1));

    // A face that carries a NUMBER the fight keeps rewriting: struck off and written again, because a status
    // merges by adding and every one of these is a total rather than a contribution.
    private static IEffectNode<TContext> Rewrite<TContext>(
        string statusId, ICombatExpression<TContext, int> value) where TContext : class =>
        new CausalSequenceEffectNode<TContext>(
        [
            new RemoveStatusNode<TContext>(Applicant, new StatusDefinitionId(statusId)),
            new ConditionalEffectNode<TContext>(
                new ComparisonExpression<TContext>(value, ComparisonOperator.GreaterOrEqual, Const<TContext>(1)),
                new ApplyStatusNode<TContext>(
                    Applicant, new StatusDefinitionId(statusId), value, sourceSelector: Nanshe)),
        ]);
}
