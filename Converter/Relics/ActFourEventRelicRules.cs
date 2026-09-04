using System.Text.Json;
using RogueDeck.Core.Combat;
using RogueDeck.Sandbox.Composition;
using RogueDeck.Scenario.Authoring;

namespace BnbContent.Converter.Relics;

// The in-combat rules the five Act-IV Event relics install — the ones the Labyrinth's first ten doors hand
// over (the other four arrive with events 11–20).
//
// The Green Docket's prizes were about the SHAPE of a turn. The Labyrinth's are about the act's own five
// words: what a turn COST (the Cup), what fades and what is held in place (the Knot, the Jar), what the
// register does to the first thing entered in it (the Cartouche), and what a false measure is answered with
// (the Weight). Three of them are readings of keywords the act already owns rather than new machinery, which
// is the point: an Act-IV relic that did not speak Act IV would be an Act-III relic found in a tomb.
//
// ⚠ EVERYTHING here is a PROPERTY, ids and counters alike (the IV-21 lesson): a `static readonly` field
// declared below the status that names it is still null when that status is built, and nothing says so.
public static class ActFourEventRelicRules
{
    public static IReadOnlyList<StatusData> All() =>
    [
        CupOfTheLowestMark, RedLinenKnot, BlankCartouche, JarOfBorrowedBreath, BrokenRoyalWeight,
        PetitionChisel, TabletOfTheMissingName, FuneraryLinenCoil, MercyCounterweight,
        // …and the two OBJECTS two of them hand out. Neither is a relic: they are what a relic puts on you,
        // and they are here because the seam each of them needs — an amplification, a prohibition — is a
        // property of a status and of nothing else.
        NamelessAuthority, MercyWard, SpareHand,
    ];

    public const string CupId = "cup_of_the_lowest_mark";
    public const string KnotId = "red_linen_knot";
    public const string CartoucheId = "blank_cartouche";
    public const string JarId = "jar_of_borrowed_breath";
    public const string WeightId = "broken_royal_weight";
    public const string ChiselId = "petition_chisel";
    public const string TabletId = "tablet_of_the_missing_name";
    public const string CoilId = "funerary_linen_coil";
    public const string MercyId = "mercy_counterweight";

    // The two objects, which are not relics: what the Unnamed Throne writes over you, and the weight the
    // Merciful Balance lets you put on the pan.
    public const string AuthorityId = "nameless_authority";
    public const string WardId = "mercy_ward";

    // …and the third object, which belongs to no single relic: what "gain 1 Energy" has to mean when the
    // moment it is given is a moment the pool is already full.
    public const string SpareId = "spare_hand";

    public const int KnotBlock = 8;
    public const int WeightBlock = 10;
    public const int GrievanceCap = 3;

    private static ICombatantTargetSelector You => CombatantTargetSelectors.Source;

    private static CounterId CupUsed => new("cup_of_the_lowest_mark_used");
    private static CounterId CupOwed => new("cup_of_the_lowest_mark_owed");
    private static CounterId KnotUsed => new("red_linen_knot_used");
    private static CounterId CartoucheUsed => new("blank_cartouche_used");
    private static CounterId JarUsed => new("jar_of_borrowed_breath_used");
    private static CounterId JarOwed => new("jar_of_borrowed_breath_owed");
    private static CounterId WeightUsed => new("broken_royal_weight_used");
    private static CounterId Grievance => new("petition_chisel_grievance");
    private static CounterId GrievancePending => new("petition_chisel_pending");
    private static CounterId CoilUsed => new("funerary_linen_coil_used");
    private static CounterId MercyAccepted => new("mercy_counterweight_accepted");
    private static CounterId MercyOwed => new("mercy_counterweight_owed");

    // Written on a CARD rather than on a combatant: the turn a copy was played on, which is the one thing
    // that tells a card exhausted by its own play from a card the player put out of the fight on purpose.
    private static CounterId PlayedOn => new("funerary_linen_coil_played_on");

    // ── Cup of the Lowest Mark ────────────────────────────────────────────────────────────────────────────

    // "First time each combat you end a turn with exactly 1 unspent Energy, heal 4 HP and draw +1 next turn."
    //
    // The nilometer reads the LOWEST mark, so the cup is interested in the turn you very nearly spent. The
    // "+1 next turn" is the act's LEDGER idiom rather than a status: what is owed is written at the turn's
    // end and paid at the next hand, because a one-turn draw status applied AT a turn end would have to
    // survive its own fading rule.
    public static StatusData CupOfTheLowestMark => Rule(
        CupId, "Cup of the Lowest Mark",
        "The first turn each fight you end with exactly one Energy unspent, the cup fills: heal 4, and draw "
        + "one more at your next hand.",
        [
            Trigger(new EffectProgram<TurnEndedTriggeredEffectContext>(
                new ConditionalEffectNode<TurnEndedTriggeredEffectContext>(
                    new AndExpression<TurnEndedTriggeredEffectContext>(
                        Unspent<TurnEndedTriggeredEffectContext>(CupUsed),
                        new ComparisonExpression<TurnEndedTriggeredEffectContext>(
                            new CombatantCurrentResourceExpression<TurnEndedTriggeredEffectContext>(
                                You, StandardCombatIds.EnergyResource),
                            ComparisonOperator.Equal,
                            new ConstantExpression<TurnEndedTriggeredEffectContext>(1))),
                    new CausalSequenceEffectNode<TurnEndedTriggeredEffectContext>(
                    [
                        Spend<TurnEndedTriggeredEffectContext>(CupUsed),
                        Set<TurnEndedTriggeredEffectContext>(CupOwed, 1),
                        new HealNode<TurnEndedTriggeredEffectContext>(
                            You, new ConstantExpression<TurnEndedTriggeredEffectContext>(4)),
                    ]))),
                nameof(TriggerEvent.TurnEnded)),
            Trigger(new EffectProgram<CardsDrawnTriggeredEffectContext>(
                new ConditionalEffectNode<CardsDrawnTriggeredEffectContext>(
                    new ComparisonExpression<CardsDrawnTriggeredEffectContext>(
                        new CombatantCounterExpression<CardsDrawnTriggeredEffectContext>(You, CupOwed),
                        ComparisonOperator.Greater,
                        new ConstantExpression<CardsDrawnTriggeredEffectContext>(0)),
                    new CausalSequenceEffectNode<CardsDrawnTriggeredEffectContext>(
                    [
                        Set<CardsDrawnTriggeredEffectContext>(CupOwed, 0),
                        new DrawCardsNode<CardsDrawnTriggeredEffectContext>(
                            You, new ConstantExpression<CardsDrawnTriggeredEffectContext>(1)),
                    ]))),
                nameof(TriggerEvent.CardsDrawn)),
        ]);

    // ── Red Linen Knot ────────────────────────────────────────────────────────────────────────────────────

    // "Start combat with 8 Block. First time each combat a positive Status would naturally lose stacks or
    // duration, prevent that loss and gain 8 Block."
    //
    // NOT AN ADAPTATION, a reading: preventing one natural fade is EMBALMED, which this act already owns and
    // already writes down every time it spends itself (ActFour.DecaysPreserved). So the knot opens the fight
    // with 8 Block and one Embalmed, and pays its second 8 Block at the hand after the linen has held —
    // which is also the only place Block may be given, since Block expires at the start of its owner's turn.
    public static StatusData RedLinenKnot => Rule(
        KnotId, "Red Linen Knot",
        "The fight opens with 8 Block and one Embalmed. The first time the linen holds something in place "
        + "that would have faded, you are wrapped again: 8 Block.",
        [
            Trigger(new EffectProgram<CardsDrawnTriggeredEffectContext>(
                new CausalSequenceEffectNode<CardsDrawnTriggeredEffectContext>(
                [
                    new ConditionalEffectNode<CardsDrawnTriggeredEffectContext>(
                        FirstRound<CardsDrawnTriggeredEffectContext>(),
                        new CausalSequenceEffectNode<CardsDrawnTriggeredEffectContext>(
                        [
                            new GainBlockNode<CardsDrawnTriggeredEffectContext>(
                                You, new ConstantExpression<CardsDrawnTriggeredEffectContext>(KnotBlock)),
                            new ApplyStatusNode<CardsDrawnTriggeredEffectContext>(
                                You, new StatusDefinitionId(ActFour.EmbalmedId),
                                new ConstantExpression<CardsDrawnTriggeredEffectContext>(1)),
                        ])),
                    new ConditionalEffectNode<CardsDrawnTriggeredEffectContext>(
                        new AndExpression<CardsDrawnTriggeredEffectContext>(
                            Unspent<CardsDrawnTriggeredEffectContext>(KnotUsed),
                            new ComparisonExpression<CardsDrawnTriggeredEffectContext>(
                                new CombatantCounterExpression<CardsDrawnTriggeredEffectContext>(
                                    You, ActFour.DecaysPreserved),
                                ComparisonOperator.Greater,
                                new ConstantExpression<CardsDrawnTriggeredEffectContext>(0))),
                        new CausalSequenceEffectNode<CardsDrawnTriggeredEffectContext>(
                        [
                            Spend<CardsDrawnTriggeredEffectContext>(KnotUsed),
                            new GainBlockNode<CardsDrawnTriggeredEffectContext>(
                                You, new ConstantExpression<CardsDrawnTriggeredEffectContext>(KnotBlock)),
                        ])),
                ])),
                nameof(TriggerEvent.CardsDrawn)),
        ]);

    // ── Blank Cartouche ───────────────────────────────────────────────────────────────────────────────────

    // "Draw +1 on turn 1. First time each combat you gain Inscribed, remove 1 Inscribed."
    //
    // A cartouche with no name in it: the first thing entered on your behalf slides straight back off. Both
    // halves are the same object — an extra card for a register that will not hold the first entry.
    //
    // ⚠ In a status-APPLICATION event the Source is whoever APPLIED it, so every reach here is to the
    // combatant wearing the cartouche, addressed by the rule it carries.
    public static StatusData BlankCartouche => Rule(
        CartoucheId, "Blank Cartouche",
        "An extra card in your first hand. The first Inscribed you gain each fight finds no name to be "
        + "written under and comes straight off again.",
        [
            Trigger(new EffectProgram<CardsDrawnTriggeredEffectContext>(
                new ConditionalEffectNode<CardsDrawnTriggeredEffectContext>(
                    FirstRound<CardsDrawnTriggeredEffectContext>(),
                    new DrawCardsNode<CardsDrawnTriggeredEffectContext>(
                        You, new ConstantExpression<CardsDrawnTriggeredEffectContext>(1)))),
                nameof(TriggerEvent.CardsDrawn)),
            Trigger(SlidesOff<StatusAppliedTriggeredEffectContext>(), nameof(TriggerEvent.StatusApplied)),
            Trigger(SlidesOff<StatusMergedTriggeredEffectContext>(), nameof(TriggerEvent.StatusMerged)),
        ]);

    private static EffectProgram<TContext> SlidesOff<TContext>() where TContext : class
    {
        var wearer = ActFour.Bearer(CartoucheId);
        return new EffectProgram<TContext>(
            new ConditionalEffectNode<TContext>(
                new AndExpression<TContext>(
                    new TriggerEventStatusIsExpression<TContext>(new StatusDefinitionId(ActFour.InscribedId)),
                    new ComparisonExpression<TContext>(
                        new CombatantCounterExpression<TContext>(wearer, CartoucheUsed),
                        ComparisonOperator.Equal, new ConstantExpression<TContext>(0))),
                new CausalSequenceEffectNode<TContext>(
                [
                    new SetCombatantCounterNode<TContext>(
                        wearer, CartoucheUsed, new ConstantExpression<TContext>(1), relative: false),
                    new ModifyStatusStacksNode<TContext>(
                        wearer, new StatusDefinitionId(ActFour.InscribedId),
                        new ConstantExpression<TContext>(-1)),
                ])));
    }

    // ── Jar of Borrowed Breath ────────────────────────────────────────────────────────────────────────────

    // "First time each combat a temporary negative status leaves you completely, draw 1 and heal 3."
    //
    // A status whose last stack goes is an EXPIRY and one taken off whole is a REMOVAL, and the design's
    // "leaves you completely" is both — so the jar listens on both doors and answers on whichever comes
    // first. The design's "temporary" is dropped: nothing in a fight remembers whether an affliction was
    // meant to last.
    //
    // The heal is paid where it happens; the CARD is a ledger paid at the next hand, because the commonest
    // moment for an affliction to leave is the turn's end, and a card drawn into a hand that is about to be
    // discarded is not a card.
    public static StatusData JarOfBorrowedBreath => Rule(
        JarId, "Jar of Borrowed Breath",
        "The first affliction to leave you completely each fight is breath given back: heal 3, and one more "
        + "card at your next hand.",
        [
            // ⚠ Both doors are ANYWHERE, and StatusExpired is why: under Bearer scope that trigger does not
            // mean "on the wearer", it means "the wearer's own status is the one that ran out" — the jar
            // would only ever hear itself go. Anywhere hears every expiry in the fight, so the program's
            // first question is whether the thing that left left the jar's owner.
            Trigger(BreathReturned<StatusExpiredTriggeredEffectContext>(),
                nameof(TriggerEvent.StatusExpired), StatusTriggerScope.Anywhere),
            Trigger(BreathReturned<StatusRemovedTriggeredEffectContext>(),
                nameof(TriggerEvent.StatusRemoved), StatusTriggerScope.Anywhere),
            Trigger(new EffectProgram<CardsDrawnTriggeredEffectContext>(
                new ConditionalEffectNode<CardsDrawnTriggeredEffectContext>(
                    new ComparisonExpression<CardsDrawnTriggeredEffectContext>(
                        new CombatantCounterExpression<CardsDrawnTriggeredEffectContext>(You, JarOwed),
                        ComparisonOperator.Greater,
                        new ConstantExpression<CardsDrawnTriggeredEffectContext>(0)),
                    new CausalSequenceEffectNode<CardsDrawnTriggeredEffectContext>(
                    [
                        Set<CardsDrawnTriggeredEffectContext>(JarOwed, 0),
                        new DrawCardsNode<CardsDrawnTriggeredEffectContext>(
                            You, new ConstantExpression<CardsDrawnTriggeredEffectContext>(1)),
                    ]))),
                nameof(TriggerEvent.CardsDrawn)),
        ]);

    private static EffectProgram<TContext> BreathReturned<TContext>() where TContext : class
    {
        var wearer = ActFour.Bearer(JarId);
        return new EffectProgram<TContext>(
            new ConditionalEffectNode<TContext>(
                new AndExpression<TContext>(
                    new AndExpression<TContext>(
                        // It left the jar's owner, and it was an affliction.
                        new TargetHasStatusExpression<TContext>(
                            CombatantTargetSelectors.EventTarget, new StatusDefinitionId(JarId)),
                        new TriggerEventStatusPolarityIsExpression<TContext>(StatusPolarity.Debuff)),
                    new ComparisonExpression<TContext>(
                        new CombatantCounterExpression<TContext>(wearer, JarUsed),
                        ComparisonOperator.Equal, new ConstantExpression<TContext>(0))),
                new CausalSequenceEffectNode<TContext>(
                [
                    new SetCombatantCounterNode<TContext>(
                        wearer, JarUsed, new ConstantExpression<TContext>(1), relative: false),
                    new SetCombatantCounterNode<TContext>(
                        wearer, JarOwed, new ConstantExpression<TContext>(1), relative: false),
                    new HealNode<TContext>(wearer, new ConstantExpression<TContext>(3)),
                ])));
    }

    // ── Broken Royal Weight ───────────────────────────────────────────────────────────────────────────────

    // "Start combat with 10 Block. Once per combat when Weighed is failed, prevent the direct HP loss and
    // gain Burdened 1 instead."
    //
    // ADAPTATION: a failed measure costs no HP in this port. The act answers a miss through the body that
    // SET the measure, by error band, so there is no direct loss to intercept — what the design is buying is
    // "the first miss does not hurt". The broken weight pays it the only way a relic can: the first failed
    // measure each fight is answered at the next hand with another 10 Block, and the false weight is heavier
    // for having been used — one Burdened. Block at the hand rather than at the miss, because Block given
    // during a turn that has already begun is swept away at the start of the next one.
    public static StatusData BrokenRoyalWeight => Rule(
        WeightId, "Broken Royal Weight",
        "The fight opens with 10 Block. The first measure you miss each fight is taken on the weight "
        + "instead: 10 Block at your next hand, and one Burdened for using a false weight.",
        [
            Trigger(new EffectProgram<CardsDrawnTriggeredEffectContext>(
                new CausalSequenceEffectNode<CardsDrawnTriggeredEffectContext>(
                [
                    new ConditionalEffectNode<CardsDrawnTriggeredEffectContext>(
                        FirstRound<CardsDrawnTriggeredEffectContext>(),
                        new GainBlockNode<CardsDrawnTriggeredEffectContext>(
                            You, new ConstantExpression<CardsDrawnTriggeredEffectContext>(WeightBlock))),
                    new ConditionalEffectNode<CardsDrawnTriggeredEffectContext>(
                        new AndExpression<CardsDrawnTriggeredEffectContext>(
                            Unspent<CardsDrawnTriggeredEffectContext>(WeightUsed),
                            new ComparisonExpression<CardsDrawnTriggeredEffectContext>(
                                new CombatantCounterExpression<CardsDrawnTriggeredEffectContext>(
                                    You, ActFour.MeasuresFailed),
                                ComparisonOperator.Greater,
                                new ConstantExpression<CardsDrawnTriggeredEffectContext>(0))),
                        new CausalSequenceEffectNode<CardsDrawnTriggeredEffectContext>(
                        [
                            Spend<CardsDrawnTriggeredEffectContext>(WeightUsed),
                            new GainBlockNode<CardsDrawnTriggeredEffectContext>(
                                You, new ConstantExpression<CardsDrawnTriggeredEffectContext>(WeightBlock)),
                            new ApplyStatusNode<CardsDrawnTriggeredEffectContext>(
                                You, new StatusDefinitionId(ActFour.BurdenedId),
                                new ConstantExpression<CardsDrawnTriggeredEffectContext>(1)),
                        ])),
                ])),
                nameof(TriggerEvent.CardsDrawn)),
        ]);

    // ── Petition Chisel ───────────────────────────────────────────────────────────────────────────────────

    // "Each enemy action that directly applies one or more negative statuses records 1 Grievance, max 3. At
    // the start of a turn with 3: consume all, draw 2, gain 1 Energy, remove 1 stack of one negative status."
    //
    // The wall is a wall of COMPLAINTS, so the chisel counts them, and what it counts is ACTIONS and not
    // afflictions: a single blow that lands three markings is one grievance, which is what the design says
    // and what an action-scoped engine event can actually answer. The affliction sets a flag as it lands and
    // the action that carried it converts the flag when it closes — the one place "one or more" is countable.
    //
    // ⚠ In a status-APPLICATION event `source` is the APPLIER, so a self-inflicted marking would grieve
    // against you; the program asks whether the applier is the wearer and stays quiet if it is. The wearer is
    // reached everywhere by the rule it carries, never by `source`.
    public static StatusData PetitionChisel => Rule(
        ChiselId, "Petition Chisel",
        "Every enemy action that marks you is one Grievance, at most 3. At the start of a turn with 3, they "
        + "are filed all at once: draw 2, take a Spare Hand, and one affliction loses a stack.",
        [
            Trigger(Grieves<StatusAppliedTriggeredEffectContext>(), nameof(TriggerEvent.StatusApplied)),
            Trigger(Grieves<StatusMergedTriggeredEffectContext>(), nameof(TriggerEvent.StatusMerged)),
            // ANYWHERE, because the action that has just closed is somebody ELSE's — under Bearer scope this
            // trigger would only ever hear the wearer's own card plays.
            Trigger(Files<ActionResolvedTriggeredEffectContext>(),
                nameof(TriggerEvent.ActionResolved), StatusTriggerScope.Anywhere),
            Trigger(new EffectProgram<TurnStartedTriggeredEffectContext>(
                new ConditionalEffectNode<TurnStartedTriggeredEffectContext>(
                    new ComparisonExpression<TurnStartedTriggeredEffectContext>(
                        new CombatantCounterExpression<TurnStartedTriggeredEffectContext>(You, Grievance),
                        ComparisonOperator.GreaterOrEqual,
                        new ConstantExpression<TurnStartedTriggeredEffectContext>(GrievanceCap)),
                    new CausalSequenceEffectNode<TurnStartedTriggeredEffectContext>(
                    [
                        Set<TurnStartedTriggeredEffectContext>(Grievance, 0),
                        new DrawCardsNode<TurnStartedTriggeredEffectContext>(
                            You, new ConstantExpression<TurnStartedTriggeredEffectContext>(2)),
                        // "Gain 1 Energy" at a turn's start, which is a full pool — see SpareHand.
                        new ApplyStatusNode<TurnStartedTriggeredEffectContext>(
                            You, new StatusDefinitionId(SpareId),
                            new ConstantExpression<TurnStartedTriggeredEffectContext>(1)),
                        // "One negative status, if possible" — the status-instance selector, which is silent
                        // when the wearer is carrying nothing to take a stack off.
                        new ModifySelectedStatusStacksNode<TurnStartedTriggeredEffectContext>(
                            You,
                            new StatusSelectionSpec(StatusPolarityFilter.Debuff, StatusPick.First),
                            new ConstantExpression<TurnStartedTriggeredEffectContext>(-1)),
                    ]))),
                nameof(TriggerEvent.TurnStarted)),
        ]);

    private static EffectProgram<TContext> Grieves<TContext>() where TContext : class
    {
        var wearer = ActFour.Bearer(ChiselId);
        return new EffectProgram<TContext>(
            new ConditionalEffectNode<TContext>(
                new AndExpression<TContext>(
                    new TriggerEventStatusPolarityIsExpression<TContext>(StatusPolarity.Debuff),
                    // …and it was done TO the wearer, by somebody else.
                    new NotExpression<TContext>(
                        new TargetHasStatusExpression<TContext>(
                            CombatantTargetSelectors.Source, new StatusDefinitionId(ChiselId)))),
                new SetCombatantCounterNode<TContext>(
                    wearer, GrievancePending, new ConstantExpression<TContext>(1), relative: false)));
    }

    private static EffectProgram<TContext> Files<TContext>() where TContext : class
    {
        var wearer = ActFour.Bearer(ChiselId);
        return new EffectProgram<TContext>(
            new ConditionalEffectNode<TContext>(
                new ComparisonExpression<TContext>(
                    new CombatantCounterExpression<TContext>(wearer, GrievancePending),
                    ComparisonOperator.Greater, new ConstantExpression<TContext>(0)),
                new CausalSequenceEffectNode<TContext>(
                [
                    new SetCombatantCounterNode<TContext>(
                        wearer, GrievancePending, new ConstantExpression<TContext>(0), relative: false),
                    new ConditionalEffectNode<TContext>(
                        new ComparisonExpression<TContext>(
                            new CombatantCounterExpression<TContext>(wearer, Grievance),
                            ComparisonOperator.Less, new ConstantExpression<TContext>(GrievanceCap)),
                        new SetCombatantCounterNode<TContext>(
                            wearer, Grievance, new ConstantExpression<TContext>(1), relative: true)),
                ])));
    }

    // ── Tablet of the Missing Name ────────────────────────────────────────────────────────────────────────

    // "Start combat with 1 Nameless Authority. The first positive-status gain each combat consumes it and
    // increases that gain by 50%, rounded up, minimum +1 stack. If you have Inscribed afterward, remove 1."
    //
    // ADAPTATION, and the only one these four needed: the engine's register ADDS STACKS, it does not scale —
    // so the authority pays a flat +1 rather than half. For every blessing of one or two stacks that IS the
    // design's own arithmetic (50 % of 1 and of 2, rounded up, is 1); a gain of three or more is worth one
    // stack less than the throne promised, which is the whole of the deviation.
    //
    // The rest is exact, and it is why the authority is its own object rather than a line in the relic: the
    // amplification seam is a property of a STATUS, so "start with one and it is spent on the first blessing"
    // is a status with one stack. The anti-synergy clause — the throne will not have you registered twice —
    // is the relic's own trigger, listening for its authority to be the amplifier that fired.
    public static StatusData TabletOfTheMissingName => Rule(
        TabletId, "Tablet of the Missing Name",
        "Every fight opens with one Nameless Authority: the first blessing you gain lands one stack larger. "
        + "A name restored will not be entered twice — if the register holds you afterwards, 1 Inscribed "
        + "comes off.",
        [
            Trigger(new EffectProgram<CardsDrawnTriggeredEffectContext>(
                new ConditionalEffectNode<CardsDrawnTriggeredEffectContext>(
                    FirstRound<CardsDrawnTriggeredEffectContext>(),
                    new ApplyStatusNode<CardsDrawnTriggeredEffectContext>(
                        You, new StatusDefinitionId(AuthorityId),
                        new ConstantExpression<CardsDrawnTriggeredEffectContext>(1)))),
                nameof(TriggerEvent.CardsDrawn)),
            Trigger(NotEnteredTwice<StatusApplicationAmplifiedTriggeredEffectContext>(),
                nameof(TriggerEvent.StatusApplicationAmplified)),
        ]);

    private static EffectProgram<TContext> NotEnteredTwice<TContext>() where TContext : class
    {
        var wearer = ActFour.Bearer(TabletId);
        return new EffectProgram<TContext>(
            new ConditionalEffectNode<TContext>(
                new AndExpression<TContext>(
                    new TriggerEventAmplifierIsExpression<TContext>(new StatusDefinitionId(AuthorityId)),
                    new TargetHasStatusExpression<TContext>(
                        wearer, new StatusDefinitionId(ActFour.InscribedId))),
                new ModifyStatusStacksNode<TContext>(
                    wearer, new StatusDefinitionId(ActFour.InscribedId),
                    new ConstantExpression<TContext>(-1))));
    }

    // The throne's authority: a name where there was none, spent on the first thing entered under it.
    // Neutral, like the register it answers, so that being about to be amplified is not itself a blessing
    // another amplifier can enlarge.
    public static StatusData NamelessAuthority => new()
    {
        Id = AuthorityId,
        NameKey = "Nameless Authority",
        DescriptionKey =
            "Authority borrowed from a name nobody can read. The next blessing applied to you lands with 1 "
            + "more stack, and the authority is spent doing it.",
        Polarity = StatusPolarity.Neutral,
        StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
        UsesStacks = true,
        Triggers = [],
        Amplification = new StatusAmplificationData(
            StatusAmplificationScope.Buffs, AddStacks: 1, StacksSpent: 1),
    };

    // ── Funerary Linen Coil ───────────────────────────────────────────────────────────────────────────────

    // "Once per combat, the first non-Junk card deliberately Exhausted, Archived or player-Banished without
    // being played normally heals 4 HP and draws 1."
    //
    // All three of the design's words are one engine fact — a card landing in the Exhaust or Banished pile —
    // and the whole difficulty is the last clause, because a card that exhausts ITSELF on being played makes
    // exactly the same move out of exactly the same hand. Nothing in the move says which it was.
    //
    // So the coil writes it down: playing a card stamps that copy with the turn it was played on, and a card
    // arriving in the pile on the turn of its own stamp is a card that was played. Deliberate disposal — an
    // Archive, a discard-for-cost, a banish — carries no stamp for this turn and is answered.
    public static StatusData FuneraryLinenCoil => Rule(
        CoilId, "Funerary Linen Coil",
        "The first card each fight you put out of the fight yourself — archived, exhausted, banished, but "
        + "never merely played — is wrapped properly: heal 4 and draw 1. Junk is not worth the linen.",
        [
            Trigger(new EffectProgram<CardPlayedTriggeredEffectContext>(
                new SetCardInstanceMarkCounterNode<CardPlayedTriggeredEffectContext>(
                    You, new TriggerEventCardInstanceExpression<CardPlayedTriggeredEffectContext>(),
                    PlayedOn, new TurnNumberExpression<CardPlayedTriggeredEffectContext>())),
                nameof(TriggerEvent.CardPlayed)),
            Trigger(Wraps<CardMovedToZoneTriggeredEffectContext>(),
                nameof(TriggerEvent.CardMovedToZone)),
        ]);

    private static EffectProgram<TContext> Wraps<TContext>() where TContext : class
    {
        var card = new TriggerEventCardInstanceExpression<TContext>();
        return new EffectProgram<TContext>(
            new ConditionalEffectNode<TContext>(
                new AndExpression<TContext>(
                    new AndExpression<TContext>(
                        // Out of the fight for good, one way or the other…
                        new OrExpression<TContext>(
                            new TriggerEventCardZoneExpression<TContext>(CardZone.ExhaustPile),
                            new TriggerEventCardZoneExpression<TContext>(CardZone.BanishedPile)),
                        // …and not merely played there this turn.
                        new NotExpression<TContext>(
                            new ComparisonExpression<TContext>(
                                new CardInstanceMarkCounterExpression<TContext>(card, PlayedOn),
                                ComparisonOperator.Equal, new TurnNumberExpression<TContext>()))),
                    new AndExpression<TContext>(
                        new NotExpression<TContext>(
                            new CardInstanceHasTagExpression<TContext>(
                                card, new TagId(Cards.CardAuthoring.JunkTag))),
                        Unspent<TContext>(CoilUsed))),
                new CausalSequenceEffectNode<TContext>(
                [
                    Spend<TContext>(CoilUsed),
                    new HealNode<TContext>(You, new ConstantExpression<TContext>(4)),
                    new DrawCardsNode<TContext>(You, new ConstantExpression<TContext>(1)),
                ])));
    }

    // ── Mercy Counterweight ───────────────────────────────────────────────────────────────────────────────

    // "First time each combat you would gain a negative status, choose: reduce the application by 1 stack; or
    // accept it normally and next turn gain 1 Energy and draw +1. Then inactive for the combat."
    //
    // ADAPTATION of WHEN the question is asked, not of what it buys. Reducing an application by a stack is a
    // prohibition, and a prohibition is a property of a status rather than a program — nothing can stop an
    // application halfway through to ask the player something. So the balance asks at the fight's first hand,
    // which is also the only moment the answer is a real decision: put your weight on the pan now (a ward
    // that eats one stack of the first affliction and is spent), or leave the pan empty and be paid for what
    // lands (1 Energy and a card at the hand after it).
    //
    // The payment is a LEDGER for the reason everything in this act is: an affliction most often lands on the
    // enemy's turn, and a card drawn into a hand about to be discarded is not a card.
    public static StatusData MercyCounterweight => Rule(
        MercyId, "Mercy Counterweight",
        "At your first hand each fight, choose a pan: mercy, and the first affliction lands one stack "
        + "lighter; or payment, and the first affliction to land pays a Spare Hand and a card at your next "
        + "hand.",
        [
            Trigger(new EffectProgram<CardsDrawnTriggeredEffectContext>(
                new CausalSequenceEffectNode<CardsDrawnTriggeredEffectContext>(
                [
                    new ConditionalEffectNode<CardsDrawnTriggeredEffectContext>(
                        FirstRound<CardsDrawnTriggeredEffectContext>(),
                        new ChooseOptionsNode<CardsDrawnTriggeredEffectContext>(
                        [
                            new ApplyStatusNode<CardsDrawnTriggeredEffectContext>(
                                You, new StatusDefinitionId(WardId),
                                new ConstantExpression<CardsDrawnTriggeredEffectContext>(1)),
                            Set<CardsDrawnTriggeredEffectContext>(MercyAccepted, 1),
                        ],
                        ["Mercy — the first affliction lands one stack lighter",
                         "Payment — take it whole, and be paid for it"],
                        count: 1, purpose: "choose which pan the counterweight sits on")),
                    // …and the payment itself, owed since the affliction landed.
                    new ConditionalEffectNode<CardsDrawnTriggeredEffectContext>(
                        new ComparisonExpression<CardsDrawnTriggeredEffectContext>(
                            new CombatantCounterExpression<CardsDrawnTriggeredEffectContext>(You, MercyOwed),
                            ComparisonOperator.Greater,
                            new ConstantExpression<CardsDrawnTriggeredEffectContext>(0)),
                        new CausalSequenceEffectNode<CardsDrawnTriggeredEffectContext>(
                        [
                            Set<CardsDrawnTriggeredEffectContext>(MercyOwed, 0),
                            new ApplyStatusNode<CardsDrawnTriggeredEffectContext>(
                                You, new StatusDefinitionId(SpareId),
                                new ConstantExpression<CardsDrawnTriggeredEffectContext>(1)),
                            new DrawCardsNode<CardsDrawnTriggeredEffectContext>(
                                You, new ConstantExpression<CardsDrawnTriggeredEffectContext>(1)),
                        ])),
                ])),
                nameof(TriggerEvent.CardsDrawn)),
            Trigger(Accepts<StatusAppliedTriggeredEffectContext>(), nameof(TriggerEvent.StatusApplied)),
            Trigger(Accepts<StatusMergedTriggeredEffectContext>(), nameof(TriggerEvent.StatusMerged)),
        ]);

    private static EffectProgram<TContext> Accepts<TContext>() where TContext : class
    {
        var wearer = ActFour.Bearer(MercyId);
        return new EffectProgram<TContext>(
            new ConditionalEffectNode<TContext>(
                new AndExpression<TContext>(
                    new TriggerEventStatusPolarityIsExpression<TContext>(StatusPolarity.Debuff),
                    new ComparisonExpression<TContext>(
                        new CombatantCounterExpression<TContext>(wearer, MercyAccepted),
                        ComparisonOperator.Greater, new ConstantExpression<TContext>(0))),
                new CausalSequenceEffectNode<TContext>(
                [
                    // Spent on the first one that lands: the pan is empty again afterwards.
                    new SetCombatantCounterNode<TContext>(
                        wearer, MercyAccepted, new ConstantExpression<TContext>(0), relative: false),
                    new SetCombatantCounterNode<TContext>(
                        wearer, MercyOwed, new ConstantExpression<TContext>(1), relative: false),
                ])));
    }

    // The weight on the pan: one stack of one affliction, refused and paid for.
    public static StatusData MercyWard => new()
    {
        Id = WardId,
        NameKey = "Mercy",
        DescriptionKey =
            "Weight on the merciful pan. The next affliction applied to you lands with 1 stack fewer, and "
            + "the weight is spent taking it.",
        Polarity = StatusPolarity.Buff,
        StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
        UsesStacks = true,
        Triggers = [],
        Prevention = new StatusPreventionData(StatusPreventionScope.Debuffs, StacksPerStack: 1),
    };

    // ── Spare Hand — what "+1 Energy" means at a full pool ────────────────────────────────────────────────

    // ADAPTATION, and a shared one: an Energy pool has a MAXIMUM and the engine will not fill it past that,
    // so "gain 1 Energy" given at a turn's START — the Petition Chisel's filing, the Fixed-Day Festival's
    // drum — is given into a pool the refill has just filled, and buys nothing at all. It is not a bug in
    // either of them: an energy gain is only ever worth what is missing.
    //
    // What the design MEANS by it is one more card out of the turn, so that is what is given, in the exact
    // grammar Act IV already owns: this is Burdened with the sign turned round. A card costs 1 less while a
    // Spare is held, playing one spends a Spare, and whatever is left goes at the end of the turn — because
    // unspent Energy does not keep either.
    public static StatusData SpareHand => new()
    {
        Id = SpareId,
        NameKey = "Spare Hand",
        DescriptionKey =
            "A card in hand you were not going to be able to afford. One card costs 1 less Energy, and "
            + "playing it spends the spare; what is left goes at the end of your turn.",
        Polarity = StatusPolarity.Buff,
        StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
        UsesStacks = true,
        PassiveModifiers =
        [
            new PassiveModifierData(PassiveModifierPipeline.CardCost,
                PassiveModifierOperation.AddFlat, -1, RestrictDamageKind: null),
        ],
        Triggers =
        [
            // Worked off by the card being PLAYED and not by the surcharge being paid, which is where this
            // parts company with Burdened: a spare that brings a card to nothing has still been spent, and a
            // stack that waited for a payment of more than zero would discount the whole turn.
            Trigger(new EffectProgram<CardPlayedTriggeredEffectContext>(
                new ModifyStatusStacksNode<CardPlayedTriggeredEffectContext>(
                    You, new StatusDefinitionId(SpareId),
                    new ConstantExpression<CardPlayedTriggeredEffectContext>(-1))),
                nameof(TriggerEvent.CardPlayed)),
            Trigger(new EffectProgram<TurnEndedTriggeredEffectContext>(
                new RemoveStatusNode<TurnEndedTriggeredEffectContext>(
                    You, new StatusDefinitionId(SpareId))),
                nameof(TriggerEvent.TurnEnded)),
        ],
    };

    // ── the shared shapes ─────────────────────────────────────────────────────────────────────────────────

    private static ICombatExpression<TContext, bool> FirstRound<TContext>() where TContext : class =>
        new ComparisonExpression<TContext>(
            new RoundNumberExpression<TContext>(),
            ComparisonOperator.Equal, new ConstantExpression<TContext>(1));

    private static ICombatExpression<TContext, bool> Unspent<TContext>(CounterId latch) where TContext : class =>
        new ComparisonExpression<TContext>(
            new CombatantCounterExpression<TContext>(You, latch),
            ComparisonOperator.Equal, new ConstantExpression<TContext>(0));

    private static IEffectNode<TContext> Spend<TContext>(CounterId latch) where TContext : class =>
        Set<TContext>(latch, 1);

    private static IEffectNode<TContext> Set<TContext>(CounterId id, int value) where TContext : class =>
        new SetCombatantCounterNode<TContext>(
            You, id, new ConstantExpression<TContext>(value), relative: false);

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

    private static StatusTriggerData Trigger<TContext>(
        EffectProgram<TContext> program, string trigger,
        StatusTriggerScope scope = StatusTriggerScope.Bearer)
        where TContext : class =>
        new(trigger, JsonSerializer.SerializeToElement(program, CombatJson.CreateOptions<TContext>()), scope);
}
