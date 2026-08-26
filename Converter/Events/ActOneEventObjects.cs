using System.Text.Json;
using RogueDeck.Core.Combat;
using RogueDeck.Sandbox.Composition;
using RogueDeck.Scenario.Authoring;
using BnbContent.Converter.Cards;
using static BnbContent.Converter.Cards.CardAuthoring;

namespace BnbContent.Converter.Events;

// The shared Act-I event objects (BnB_Final_Events_Master_PostAudit.md, "Shared Act-I event objects"). Fifteen
// events are written out of this vocabulary, so it is built once, here.
//
// Three kinds of thing:
//   · TEMPORARY CARDS an event puts into the next fight (they never join the deck permanently);
//   · MARKINGS an event writes on ONE persistent card, which the next fight has to honour — these are run
//     card TAGS, dealt into the fight as per-instance marks, and read there by the marking rule;
//   · NEXT-COMBAT RULES, one-fight rules an event installs, each a status the fight opens with.
public static class ActOneEventObjects
{
    // ── Temporary cards ───────────────────────────────────────────────────────────────────────────────────

    public static readonly BnbCard MissingSignature = new(
        "missing_signature", "Missing Signature", JunkTag, 1,
        "Exhaust. If it is still in your hand at the end of your turn, file 1 Paperwork.",
        Seq(),
        Rarity: "junk", Tags: [ExhaustTag, TemporaryTag, "missing_signature"]);

    public static readonly BnbCard NoticeOfDelay = new(
        "notice_of_delay", "Notice of Delay", JunkTag, 1,
        "Retain. Exhaust. If it is still in your hand at the end of your turn, gain 1 Fatigue.",
        Seq(),
        Rarity: "junk", Tags: [ExhaustTag, TemporaryTag, "notice_of_delay"], RetainInHand: true);

    public static readonly BnbCard SummonsToAppear = new(
        "summons_to_appear", "Summons to Appear", JunkTag, 1,
        "Retain. Exhaust. If it is still in your hand at the end of your turn, take 5 damage.",
        Seq(),
        Rarity: "junk", Tags: [ExhaustTag, TemporaryTag, "summons_to_appear"], RetainInHand: true);

    // ADAPTATION: nothing hears a card being DRAWN, so the Fine Print taxes from the hand: while it sits
    // there, the turn's first card costs 1 more. Same tax, read off the hand instead of off the draw.
    public static readonly BnbCard FinePrint = new(
        "fine_print", "Fine Print", JunkTag, 0,
        "Unplayable. While it is in your hand, the first card you play each turn costs 1 more.",
        Seq(),
        Rarity: "junk", Tags: [UnplayableTag, ExhaustTag, TemporaryTag, "fine_print"]);

    public static readonly BnbCard WrongForm = new(
        "wrong_form", "Wrong Form", JunkTag, 0,
        "Exhaust. Discard another card.",
        new CombatNodeModel("moveCardToZone", You,
            Card: new CombatCardSpec("randomInZone", Zone: CardZone.Hand), ToZone: CardZone.DiscardPile),
        Rarity: "junk", Tags: [ExhaustTag, TemporaryTag, "wrong_form"]);

    public static IReadOnlyList<BnbCard> Cards() =>
        [MissingSignature, NoticeOfDelay, SummonsToAppear, FinePrint, WrongForm];

    // What three of them do when a turn ends with them still in hand. These are LIFECYCLE programs rather than
    // triggers: the engine runs them for the card actually sitting there, before the end-of-turn discard.
    public static IReadOnlyList<CardData> Compile() =>
    [
        .. Cards().Select(card => card.Compile()).Select(data => data.Id switch
        {
            "missing_signature" => data with { LifecyclePrograms = EndOfTurn(Apply(Keywords.Paperwork, 1)) },
            "notice_of_delay" => data with { LifecyclePrograms = EndOfTurn(Apply("fatigue", 1)) },
            "summons_to_appear" => data with
            {
                LifecyclePrograms = EndOfTurn(new DealDamageNode<CardLifecycleContext>(
                    CombatantTargetSelectors.Source,
                    new ConstantExpression<CardLifecycleContext>(5),
                    ignoresBlock: true, kind: DamageKind.DamageOverTime)),
            },
            _ => data,
        }),
    ];

    // ── Markings ──────────────────────────────────────────────────────────────────────────────────────────
    //
    // A marking is written on one card BETWEEN fights and honoured by the NEXT one. It rides across as a run
    // card tag, is dealt into the fight as a per-instance mark, and the marking rule — a status the fight
    // opens with — puts the card where the marking says.
    public const string Misfiled = "misfiled";        // starts in the discard pile
    public const string Sealed = "sealed";            // starts outside the deck; arrives in hand at round 3
    public const string FastTrack = "fast_track";     // guaranteed in the opening hand
    public const string UnderReview = "under_review"; // held out of the fight entirely; returns upgraded after

    // ADAPTATION (the Almost-Helpful Clerk): the design's "a temporary 0-cost Exhaust copy of the card you
    // chose" cannot be built — a card is Exhaust because its DEFINITION says so, and there is no per-instance
    // Exhaust mark, so a copy of an arbitrary card cannot be made temporary. The stamp is therefore put on the
    // card itself: it is in the opening hand and its first play is free. Same tempo, one card fewer.
    public const string Stamped = "stamped";          // opening hand, and that first play costs nothing

    // The PERMANENT marking. Unlike the four above it is never cleared, and the rule that reads it has to be in
    // every later fight rather than the next one (ActOneEventPrograms.CertifiedOriginal installs it).
    //
    // ADAPTATION: "cost −1 for that play AND gain Exhaust for that play" keeps only the discount, for the same
    // reason the stamp does — one play of one copy cannot be made to Exhaust.
    public const string CertifiedOriginal = "certified_original";

    // The three markings a single fight consumes. Under Review outlives it (the card is away for the fight and
    // comes back upgraded afterwards) and Certified Original is forever, so neither is expired with these.
    public static IReadOnlyList<string> SpentAfterOneFight() => [Misfiled, Sealed, FastTrack, Stamped];

    public static IReadOnlyList<string> Markings() => [Misfiled, Sealed, FastTrack, UnderReview, Stamped];

    public static readonly StatusData MarkingsRule = new()
    {
        Id = "act_one_markings",
        NameKey = "Filed Elsewhere",
        DescriptionKey = "What was done to your cards between fights is honoured here.",
        Polarity = StatusPolarity.Neutral,
        StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
        UsesStacks = true,
        Triggers =
        [
            Trigger(new EffectProgram<CardsDrawnTriggeredEffectContext>(
                new CausalSequenceEffectNode<CardsDrawnTriggeredEffectContext>(
                [
                    // Round 1: the markings say where a card STARTS. Each branch is a no-op when nothing
                    // carries that mark, so one rule serves all four.
                    new ConditionalEffectNode<CardsDrawnTriggeredEffectContext>(
                        OnRound<CardsDrawnTriggeredEffectContext>(1),
                        new CausalSequenceEffectNode<CardsDrawnTriggeredEffectContext>(
                        [
                            MoveMarked<CardsDrawnTriggeredEffectContext>(Misfiled, CardZone.DrawPile, CardZone.DiscardPile),
                            MoveMarked<CardsDrawnTriggeredEffectContext>(Misfiled, CardZone.Hand, CardZone.DiscardPile),
                            MoveMarked<CardsDrawnTriggeredEffectContext>(Sealed, CardZone.DrawPile, CardZone.BanishedPile),
                            MoveMarked<CardsDrawnTriggeredEffectContext>(Sealed, CardZone.Hand, CardZone.BanishedPile),
                            MoveMarked<CardsDrawnTriggeredEffectContext>(UnderReview, CardZone.DrawPile, CardZone.BanishedPile),
                            MoveMarked<CardsDrawnTriggeredEffectContext>(UnderReview, CardZone.Hand, CardZone.BanishedPile),
                            MoveMarked<CardsDrawnTriggeredEffectContext>(FastTrack, CardZone.DrawPile, CardZone.Hand),
                            MoveMarked<CardsDrawnTriggeredEffectContext>(Stamped, CardZone.DrawPile, CardZone.Hand),
                            // …and the stamped one is free the first time it is played: a per-instance price
                            // the play itself consumes, clamped at zero, so "−9" is simply "nothing".
                            Discount<CardsDrawnTriggeredEffectContext>(Stamped, 9),
                        ])),
                    // Round 3: what was sealed is unsealed. What is Under Review stays away for the fight.
                    new ConditionalEffectNode<CardsDrawnTriggeredEffectContext>(
                        OnRound<CardsDrawnTriggeredEffectContext>(3),
                        MoveMarked<CardsDrawnTriggeredEffectContext>(Sealed, CardZone.BanishedPile, CardZone.Hand)),
                ])),
                nameof(TriggerEvent.CardsDrawn)),
        ],
    };

    // ── Next-combat rules ─────────────────────────────────────────────────────────────────────────────────

    public const string WitnessedProcedure = "witnessed_procedure";
    public const string RestrictedPublicHours = "restricted_public_hours";
    public const string AdministrativeExemption = "administrative_exemption";
    public const string WitnessProtection = "witness_protection";
    public const string PriorityNumber = "priority_number";
    public const string AuthorizedOvertime = "authorized_overtime";
    public const string CorrectWindow = "correct_window";

    public const string AuditNotice = "audit_notice";
    public const string GarnishedReward = "garnished_reward";
    public const string ExpeditedRoute = "expedited_route";
    public const string ReceiptOfPriorEffort = "receipt_of_prior_effort";
    public const string CertifiedOriginalRuleId = "certified_original_rule";

    public static IReadOnlyList<StatusData> Statuses() =>
    [
        MarkingsRule, WitnessedProcedureRule, RestrictedPublicHoursRule, AdministrativeExemptionRule,
        WitnessProtectionRule, PriorityNumberRule, AuthorizedOvertimeRule, CorrectWindowRule, FinePrintTax,
        AuditNoticeNote, GarnishedRewardNote, ExpeditedRouteNote, ReceiptOfPriorEffortRule,
        CertifiedOriginalRule,
    ];

    // "Playing the same primary card type twice consecutively gives 1 Doubt."
    public static readonly StatusData WitnessedProcedureRule = Rule(
        WitnessedProcedure, "Witnessed Procedure",
        "Repeat a card type and the witness files 1 Doubt against you.",
        [
            Trigger(new EffectProgram<CardPlayedTriggeredEffectContext>(
                new CausalSequenceEffectNode<CardPlayedTriggeredEffectContext>(
                [
                    new ConditionalEffectNode<CardPlayedTriggeredEffectContext>(
                        SameTypeAsLast(),
                        new ApplyStatusNode<CardPlayedTriggeredEffectContext>(
                            CombatantTargetSelectors.Source, new StatusDefinitionId(Keywords.Doubt),
                            new ConstantExpression<CardPlayedTriggeredEffectContext>(1))),
                    RecordType(),
                ])),
                nameof(TriggerEvent.CardPlayed)),
        ]);

    // "Round 1: −1 Energy; Round 2: +1 Energy; then ends."
    public static readonly StatusData RestrictedPublicHoursRule = Rule(
        RestrictedPublicHours, "Restricted Public Hours",
        "The counter opens late: 1 Energy less this round, 1 more the next.",
        [
            Trigger(new EffectProgram<CardsDrawnTriggeredEffectContext>(
                new CausalSequenceEffectNode<CardsDrawnTriggeredEffectContext>(
                [
                    new ConditionalEffectNode<CardsDrawnTriggeredEffectContext>(
                        OnRound<CardsDrawnTriggeredEffectContext>(1),
                        new LoseResourceNode<CardsDrawnTriggeredEffectContext>(
                            CombatantTargetSelectors.Source, StandardCombatIds.EnergyResource,
                            new ConstantExpression<CardsDrawnTriggeredEffectContext>(1))),
                    new ConditionalEffectNode<CardsDrawnTriggeredEffectContext>(
                        OnRound<CardsDrawnTriggeredEffectContext>(2),
                        new CausalSequenceEffectNode<CardsDrawnTriggeredEffectContext>(
                        [
                            HeldEnergy.Hold<CardsDrawnTriggeredEffectContext>(1),
                            new RemoveStatusNode<CardsDrawnTriggeredEffectContext>(
                                CombatantTargetSelectors.Source,
                                new StatusDefinitionId(RestrictedPublicHours)),
                        ])),
                ])),
                nameof(TriggerEvent.CardsDrawn)),
        ]);

    // "Prevent first Panic, Doubt, Paperwork or Fatigue application." The engine's own debuff block, which is
    // spent by the status carrying it — so it is exactly the FIRST one.
    public static readonly StatusData AdministrativeExemptionRule = new()
    {
        Id = AdministrativeExemption,
        NameKey = "Administrative Exemption",
        DescriptionKey = "The first thing filed against you this fight is refused.",
        Polarity = StatusPolarity.Buff,
        StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
        UsesStacks = true,
        DebuffBlock = new StatusDebuffBlockData([]),
    };

    // "Start with 10 Block; first direct HP-damage event is prevented."
    //
    // ADAPTATION: preventing one damage EVENT is not something a rule can install — the engine has no
    // incoming-damage interceptor for content — so the protection is paid as Block: 10 at the opening, and 10
    // more the first time the fight actually reaches the player.
    public static readonly StatusData WitnessProtectionRule = Rule(
        WitnessProtection, "Witness Protection",
        "10 Block, and 10 more the first time you are hurt.",
        [
            Trigger(new EffectProgram<CardsDrawnTriggeredEffectContext>(
                new ConditionalEffectNode<CardsDrawnTriggeredEffectContext>(
                    OnRound<CardsDrawnTriggeredEffectContext>(1),
                    new GainBlockNode<CardsDrawnTriggeredEffectContext>(
                        CombatantTargetSelectors.Source,
                        new ConstantExpression<CardsDrawnTriggeredEffectContext>(10)))),
                nameof(TriggerEvent.CardsDrawn)),
            Trigger(new EffectProgram<DamageReceivedTriggeredEffectContext>(
                new CausalSequenceEffectNode<DamageReceivedTriggeredEffectContext>(
                [
                    new GainBlockNode<DamageReceivedTriggeredEffectContext>(
                        CombatantTargetSelectors.Source,
                        new ConstantExpression<DamageReceivedTriggeredEffectContext>(10)),
                    new RemoveStatusNode<DamageReceivedTriggeredEffectContext>(
                        CombatantTargetSelectors.Source, new StatusDefinitionId(WitnessProtection)),
                ])),
                nameof(TriggerEvent.DamageTaken)),
        ]);

    // "Turn 1: +1 Energy, draw +2."
    public static readonly StatusData PriorityNumberRule = Rule(
        PriorityNumber, "Priority Number",
        "Your number is called first: an extra Energy and two extra cards.",
        [
            Trigger(new EffectProgram<CardsDrawnTriggeredEffectContext>(
                new ConditionalEffectNode<CardsDrawnTriggeredEffectContext>(
                    OnRound<CardsDrawnTriggeredEffectContext>(1),
                    new CausalSequenceEffectNode<CardsDrawnTriggeredEffectContext>(
                    [
                        HeldEnergy.Hold<CardsDrawnTriggeredEffectContext>(1),
                        new DrawCardsNode<CardsDrawnTriggeredEffectContext>(
                            CombatantTargetSelectors.Source,
                            new ConstantExpression<CardsDrawnTriggeredEffectContext>(2)),
                    ]))),
                nameof(TriggerEvent.CardsDrawn)),
        ]);

    // "Once in combat, unused Energy carries to the next turn."
    public static readonly StatusData AuthorizedOvertimeRule = Rule(
        AuthorizedOvertime, "Authorized Overtime",
        "Once this fight, what you do not spend is carried into your next turn.",
        [
            Trigger(new EffectProgram<TurnEndedTriggeredEffectContext>(
                new ConditionalEffectNode<TurnEndedTriggeredEffectContext>(
                    new ComparisonExpression<TurnEndedTriggeredEffectContext>(
                        new CombatantCurrentResourceExpression<TurnEndedTriggeredEffectContext>(
                            CombatantTargetSelectors.Source, StandardCombatIds.EnergyResource),
                        ComparisonOperator.Greater, new ConstantExpression<TurnEndedTriggeredEffectContext>(0)),
                    new CausalSequenceEffectNode<TurnEndedTriggeredEffectContext>(
                    [
                        HeldEnergy.Hold(new CombatantCurrentResourceExpression<TurnEndedTriggeredEffectContext>(
                            CombatantTargetSelectors.Source, StandardCombatIds.EnergyResource)),
                        new RemoveStatusNode<TurnEndedTriggeredEffectContext>(
                            CombatantTargetSelectors.Source, new StatusDefinitionId(AuthorizedOvertime)),
                    ]))),
                nameof(TriggerEvent.TurnEnded)),
        ]);

    // "Each round displays one eligible primary type; first card of that type costs 1 less that round."
    //
    // ADAPTATION: the open window cannot be DISPLAYED — a frontend shows statuses and intents, not a rule's
    // inner state — so it rotates predictably instead: Deeds, then Workings, then Rites, by round.
    public static readonly StatusData CorrectWindowRule = Rule(
        CorrectWindow, "Correct Window",
        "One counter is open each round: Deeds, then Workings, then Rites. Its first card costs 1 less.",
        [
            Trigger(new EffectProgram<CardsDrawnTriggeredEffectContext>(
                new CausalSequenceEffectNode<CardsDrawnTriggeredEffectContext>(
                [
                    CheapenWindow(1, DeedTag), CheapenWindow(2, WorkingTag), CheapenWindow(0, RiteTag),
                ])),
                nameof(TriggerEvent.CardsDrawn)),
        ]);

    // The Fine Print's tax, while the card sits in hand.
    public static readonly StatusData FinePrintTax = Rule(
        "fine_print_tax", "Fine Print",
        "While the Fine Print is in your hand, the first card you play each turn costs 1 more.",
        [
            Trigger(new EffectProgram<CardsDrawnTriggeredEffectContext>(
                new ConditionalEffectNode<CardsDrawnTriggeredEffectContext>(
                    new ComparisonExpression<CardsDrawnTriggeredEffectContext>(
                        new CombatantZoneCardCountExpression<CardsDrawnTriggeredEffectContext>(
                            CombatantTargetSelectors.Source, CardZone.Hand, new TagId("fine_print")),
                        ComparisonOperator.Greater,
                        new ConstantExpression<CardsDrawnTriggeredEffectContext>(0)),
                    new ForEachCardInZoneNode<CardsDrawnTriggeredEffectContext>(
                        CombatantTargetSelectors.Source, CardZone.Hand,
                        new SetCardInstanceMarkCounterNode<CardsDrawnTriggeredEffectContext>(
                            CombatantTargetSelectors.Source,
                            new IteratedCardExpression<CardsDrawnTriggeredEffectContext>(),
                            StandardCombatIds.CardCostDeltaCounter,
                            new ConstantExpression<CardsDrawnTriggeredEffectContext>(1), relative: true),
                        takeFirst: 1))),
                nameof(TriggerEvent.CardsDrawn)),
        ]);

    // ── what the run does after the fight, announced inside it ────────────────────────────────────────────
    //
    // Three of the shared rules are not rules of the fight at all — they are what the RUN does when the fight
    // is over (a fee taken out of the purse, a purse that never arrives, a purse traded for a weaker enemy).
    // The work is done by an authored run program (ActOneEventPrograms); what lives here is the NOTE the fight
    // wears, so a promise the player was given at an event is visible at the table where it is paid.

    public static readonly StatusData AuditNoticeNote = Note(
        AuditNotice, "Audit Notice",
        "This fight is audited: afterwards you lose 4 Gold for every point of health it cost you, up to 80.");

    public static readonly StatusData GarnishedRewardNote = Note(
        GarnishedReward, "Garnished Reward",
        "Your fee for this fight has already been claimed by someone else. It pays no Gold.");

    public static readonly StatusData ExpeditedRouteNote = Note(
        ExpeditedRoute, "Expedited Route",
        "You were sent the short way round: your opposition arrived diminished, and unpaid work pays no Gold.");

    // "Next combat visibly pays 125 Gold if won by end of round 3, otherwise 25." The fight cannot pay anything
    // — the purse is the run's — so it writes down how long it took and the run reads that off the result.
    public static readonly CounterId RoundsTaken = new("rounds_taken");

    public static readonly StatusData ReceiptOfPriorEffortRule = Rule(
        ReceiptOfPriorEffort, "Receipt of Prior Effort",
        "Prior effort is on file: win by the end of round 3 and the claim pays 125 Gold, later only 25.",
        [
            Trigger(new EffectProgram<CardsDrawnTriggeredEffectContext>(
                new SetCombatantCounterNode<CardsDrawnTriggeredEffectContext>(
                    CombatantTargetSelectors.Source, RoundsTaken,
                    new RoundNumberExpression<CardsDrawnTriggeredEffectContext>(), relative: false)),
                nameof(TriggerEvent.CardsDrawn)),
        ]);

    // The permanent marking's rule. Unlike the four one-fight markings it is installed in EVERY later fight, so
    // the discount is put on the marked copy wherever it is standing when the fight opens — in the opening hand
    // or still in the draw pile — and the play itself consumes it.
    public static readonly StatusData CertifiedOriginalRule = Rule(
        CertifiedOriginalRuleId, "Certified Original",
        "A certified original is cheaper to file: the first time you play it each fight it costs 1 less.",
        [
            Trigger(new EffectProgram<CardsDrawnTriggeredEffectContext>(
                new ConditionalEffectNode<CardsDrawnTriggeredEffectContext>(
                    OnRound<CardsDrawnTriggeredEffectContext>(1),
                    new CausalSequenceEffectNode<CardsDrawnTriggeredEffectContext>(
                    [
                        Discount<CardsDrawnTriggeredEffectContext>(CertifiedOriginal, 1),
                        DiscountIn<CardsDrawnTriggeredEffectContext>(CertifiedOriginal, 1, CardZone.DrawPile),
                    ]))),
                nameof(TriggerEvent.CardsDrawn)),
        ]);

    // ── shorthands ────────────────────────────────────────────────────────────────────────────────────────

    private static readonly CounterId LastType = new("witnessed_last_type");

    private static IReadOnlyDictionary<CardLifecycleTrigger, EffectProgram<CardLifecycleContext>> EndOfTurn(
        IEffectNode<CardLifecycleContext> body) =>
        new Dictionary<CardLifecycleTrigger, EffectProgram<CardLifecycleContext>>
        {
            [CardLifecycleTrigger.TurnEndInHand] = new(body),
        };

    private static IEffectNode<CardLifecycleContext> Apply(string status, int stacks) =>
        new ApplyStatusNode<CardLifecycleContext>(
            CombatantTargetSelectors.Source, new StatusDefinitionId(status),
            new ConstantExpression<CardLifecycleContext>(stacks));

    private static ICombatExpression<TContext, bool> OnRound<TContext>(int round) where TContext : class =>
        new ComparisonExpression<TContext>(
            new RoundNumberExpression<TContext>(), ComparisonOperator.Equal,
            new ConstantExpression<TContext>(round));

    private static IEffectNode<TContext> MoveMarked<TContext>(string mark, CardZone from, CardZone to)
        where TContext : class =>
        new ForEachCardInZoneNode<TContext>(
            CombatantTargetSelectors.Source, from,
            new MoveCardToZoneNode<TContext>(
                CombatantTargetSelectors.Source, new IteratedCardExpression<TContext>(), to),
            markFilter: new TagId(mark));

    // A price written on the marked COPY rather than on its kind: the card's own cost, minus `amount`, clamped
    // at zero by the engine and consumed by the play it pays for.
    private static IEffectNode<TContext> Discount<TContext>(string mark, int amount) where TContext : class =>
        DiscountIn<TContext>(mark, amount, CardZone.Hand);

    private static IEffectNode<TContext> DiscountIn<TContext>(string mark, int amount, CardZone zone)
        where TContext : class =>
        new ForEachCardInZoneNode<TContext>(
            CombatantTargetSelectors.Source, zone,
            new SetCardInstanceMarkCounterNode<TContext>(
                CombatantTargetSelectors.Source, new IteratedCardExpression<TContext>(),
                StandardCombatIds.CardCostDeltaCounter,
                new ConstantExpression<TContext>(-amount), relative: true),
            markFilter: new TagId(mark));

    // A status that carries no rule at all: a line the fight shows because the RUN is going to act on it.
    private static StatusData Note(string id, string name, string description) => new()
    {
        Id = id,
        NameKey = name,
        DescriptionKey = description,
        Polarity = StatusPolarity.Neutral,
        StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
        UsesStacks = true,
    };

    private static IEffectNode<CardsDrawnTriggeredEffectContext> CheapenWindow(int remainder, string tag) =>
        new ConditionalEffectNode<CardsDrawnTriggeredEffectContext>(
            new ComparisonExpression<CardsDrawnTriggeredEffectContext>(
                new RemainderExpression<CardsDrawnTriggeredEffectContext>(
                    new RoundNumberExpression<CardsDrawnTriggeredEffectContext>(),
                    new ConstantExpression<CardsDrawnTriggeredEffectContext>(3)),
                ComparisonOperator.Equal, new ConstantExpression<CardsDrawnTriggeredEffectContext>(remainder)),
            new ForEachCardInZoneNode<CardsDrawnTriggeredEffectContext>(
                CombatantTargetSelectors.Source, CardZone.Hand,
                new SetCardInstanceMarkCounterNode<CardsDrawnTriggeredEffectContext>(
                    CombatantTargetSelectors.Source,
                    new IteratedCardExpression<CardsDrawnTriggeredEffectContext>(),
                    StandardCombatIds.CardCostDeltaCounter,
                    new ConstantExpression<CardsDrawnTriggeredEffectContext>(-1), relative: true),
                tagFilter: new TagId(tag), takeFirst: 1));

    private static ICombatExpression<CardPlayedTriggeredEffectContext, bool> SameTypeAsLast()
    {
        ICombatExpression<CardPlayedTriggeredEffectContext, bool> same =
            new ComparisonExpression<CardPlayedTriggeredEffectContext>(
                new ConstantExpression<CardPlayedTriggeredEffectContext>(0), ComparisonOperator.Equal,
                new ConstantExpression<CardPlayedTriggeredEffectContext>(1)); // never
        var value = 1;
        foreach (var tag in new[] { DeedTag, WorkingTag, RiteTag })
        {
            same = new OrExpression<CardPlayedTriggeredEffectContext>(same,
                new AndExpression<CardPlayedTriggeredEffectContext>(
                    PlayedHasTag(tag),
                    new ComparisonExpression<CardPlayedTriggeredEffectContext>(
                        new CombatantCounterExpression<CardPlayedTriggeredEffectContext>(
                            CombatantTargetSelectors.Source, LastType),
                        ComparisonOperator.Equal,
                        new ConstantExpression<CardPlayedTriggeredEffectContext>(value))));
            value++;
        }
        return same;
    }

    private static IEffectNode<CardPlayedTriggeredEffectContext> RecordType()
    {
        var nodes = new List<IEffectNode<CardPlayedTriggeredEffectContext>>();
        var value = 1;
        foreach (var tag in new[] { DeedTag, WorkingTag, RiteTag })
        {
            var recorded = value;
            nodes.Add(new ConditionalEffectNode<CardPlayedTriggeredEffectContext>(
                PlayedHasTag(tag),
                new SetCombatantCounterNode<CardPlayedTriggeredEffectContext>(
                    CombatantTargetSelectors.Source, LastType,
                    new ConstantExpression<CardPlayedTriggeredEffectContext>(recorded), relative: false)));
            value++;
        }
        return new CausalSequenceEffectNode<CardPlayedTriggeredEffectContext>(nodes);
    }

    private static ICombatExpression<CardPlayedTriggeredEffectContext, bool> PlayedHasTag(string tag) =>
        new CardInstanceHasTagExpression<CardPlayedTriggeredEffectContext>(
            new TriggerEventCardInstanceExpression<CardPlayedTriggeredEffectContext>(), new TagId(tag));

    private static StatusData Rule(
        string id, string name, string description, IReadOnlyList<StatusTriggerData> triggers) => new()
        {
            Id = id,
            NameKey = name,
            DescriptionKey = description,
            Polarity = StatusPolarity.Neutral,
            StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
            UsesStacks = true,
            Triggers = triggers,
        };

    private static StatusTriggerData Trigger<TContext>(EffectProgram<TContext> program, string trigger)
        where TContext : class =>
        new(trigger, JsonSerializer.SerializeToElement(program, CombatJson.CreateOptions<TContext>()),
            StatusTriggerScope.Bearer);
}
