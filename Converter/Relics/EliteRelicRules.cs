using System.Text.Json;
using RogueDeck.Core.Combat;
using RogueDeck.Sandbox.Composition;
using RogueDeck.Scenario.Authoring;

namespace BnbContent.Converter.Relics;

// The in-combat rules the 38 Elite relics install, and the four grades of the mimic's tooth.
//
// An elite relic is written from what its elite DOES: the relic is the lesson of the fight rather than a
// prize attached to it. That is also why they are one clean trigger each — an elite fight is long enough
// already, and a relic that has to be tracked turn by turn is a second fight.
//
// ⚠ EVERYTHING here is a PROPERTY, ids and counters alike (the IV-21 lesson): a `static readonly` field
// declared below the status that names it is still null when that status is built, and nothing says so.
//
// ⚠ ENERGY IS NEVER GAINED, IT IS HELD (see HeldEnergy). A combatant's Energy pool is clamped to its own
// max and the refill happens before a turn's triggers run, so "gain 1 Energy" at a turn boundary lands on a
// full pool and does nothing at all, silently. Every point of Energy an elite relic promises is held.
public static class EliteRelicRules
{
    public static IReadOnlyList<StatusData> All() =>
    [
        // Act I
        CaseThatClimbed, HalfSignedPage, RemittiturSeal, ThriceStruckAppointment, ShutterKeyLateHour,
        ChairOfTheNinthHour, ContemptLedgerNail, GateChainCounterweight, SealedSpearhead,
        // Act II
        CrackedBellLip, RollerPinOfTheStacks, TheUnspokenWord, StripOfCensoringInk, ConcordanceThread,
        DrawerWithinADrawer, MissingPresentHand,
        // Act III
        PreApprovedAntlerTine, MendedThread, TollStoneWrongBank, CoinLeftInTheThroat, TheProperLine,
        BoneTagFromTheJuniper, BoneTagCharm, ObsoleteBoundaryCord, ReedCutAtTheHearing, ThornChosenFromThree,
        // Act IV
        TheErrantCord, BrokenGranarySeal, CutCorveeRope, GlyphStruckFromTheName, OverseersLinenShears,
        RiddlesThirdAnswer, LampThiefsWick, DecanStarTableChip, ProcessionalStepStone,
        TheThirdEnding,
        // the mimic, one rule per grade
        FalseLidToothI, FalseLidToothII, FalseLidToothIII, FalseLidToothIV,
    ];

    // ⚠ AN ID HERE IS A GLOBAL NAME. Two of these (head_of_the_line, the_shorter_ferrule) first shipped as
    // the ids the Ant Queen and the Surveyor already had for their own bodies, and the registry refused the
    // second registration — which failed 37 tests across four acts with one message. Naming a relic after
    // the mechanic it is drawn from is exactly how that happens, so an elite relic is named after the OBJECT
    // in its brief and never after its elite's rule. `EliteRelicTests.No_elite_relic_takes_a_name_the_game_
    // already_uses` is the guard.
    private static ICombatantTargetSelector You => CombatantTargetSelectors.Source;
    private static ICombatantTargetSelector Weakest => CombatantTargetSelectors.LowestHealthEnemyOfSource;

    // ══ ACT I ═════════════════════════════════════════════════════════════════════════════════════════════

    // The Appellate Staircase: a case file climbs three stone authorities, and every step you take out of
    // from under it takes one of its actions with it. The relic is the climb, kept.
    public static string ClimbedId => "case_that_climbed";
    private static CounterId ClimbedUsed => new("case_that_climbed_used");

    public static StatusData CaseThatClimbed => Rule(
        ClimbedId, "The Case That Climbed",
        "The first time an enemy falls each fight, the case moves up a step: 1 Energy and a card.",
        [
            // Anywhere, because the death being listened for is somebody else's. A relic on the player that
            // only heard the player being downed would fire once, at the end of a fight it had already lost.
            Trigger(new EffectProgram<CombatantDownedTriggeredEffectContext>(
                new ConditionalEffectNode<CombatantDownedTriggeredEffectContext>(
                    Unspent<CombatantDownedTriggeredEffectContext>(ClimbedUsed),
                    new CausalSequenceEffectNode<CombatantDownedTriggeredEffectContext>(
                    [
                        Spend<CombatantDownedTriggeredEffectContext>(ClimbedUsed),
                        HeldEnergy.Hold<CombatantDownedTriggeredEffectContext>(1),
                        Draw<CombatantDownedTriggeredEffectContext>(1),
                    ]))),
                nameof(TriggerEvent.Downed), StatusTriggerScope.Anywhere),
        ]);

    // The Living Petition Chorus asks you to sign, over and over, and the convenient relief is the trap. The
    // relic is the one signature that cost nothing.
    public static string HalfSignedId => "half_signed_page";
    private static CounterId HalfSignedUsed => new("half_signed_page_used");

    public static StatusData HalfSignedPage => Rule(
        HalfSignedId, "Half-Signed Page",
        "The first card you play each fight costs nothing.",
        [
            // The discount is handed over at the opening hand and taken by the first play. NextCardFree is
            // the Normal pool's own object for exactly this, so the chorus does not invent a second one.
            Trigger(new EffectProgram<CardsDrawnTriggeredEffectContext>(
                new ConditionalEffectNode<CardsDrawnTriggeredEffectContext>(
                    new AndExpression<CardsDrawnTriggeredEffectContext>(
                        FirstRound<CardsDrawnTriggeredEffectContext>(),
                        Unspent<CardsDrawnTriggeredEffectContext>(HalfSignedUsed)),
                    new CausalSequenceEffectNode<CardsDrawnTriggeredEffectContext>(
                    [
                        Spend<CardsDrawnTriggeredEffectContext>(HalfSignedUsed),
                        new ApplyStatusNode<CardsDrawnTriggeredEffectContext>(
                            You, new StatusDefinitionId(RelicRules.NextCardFreeId),
                            new ConstantExpression<CardsDrawnTriggeredEffectContext>(1)),
                    ]))),
                nameof(TriggerEvent.CardsDrawn)),
        ]);

    // The Remanded Case is sent back down the way it came. So is the first thing put on you.
    public static string RemittiturId => "remittitur_seal";
    private static CounterId RemittiturUsed => new("remittitur_seal_used");

    public static StatusData RemittiturSeal => Rule(
        RemittiturId, "Remittitur Seal",
        "The first status an enemy puts on you each fight is sent back: 2 Paperwork on whoever filed it.",
        [
            // In a status-APPLICATION event the Source is whoever APPLIED it — which is precisely who the
            // seal wants. The bearer is reached through the rule it carries, not through Source.
            Trigger(new EffectProgram<StatusAppliedTriggeredEffectContext>(
                new ConditionalEffectNode<StatusAppliedTriggeredEffectContext>(
                    Unspent<StatusAppliedTriggeredEffectContext>(RemittiturUsed),
                    new CausalSequenceEffectNode<StatusAppliedTriggeredEffectContext>(
                    [
                        Spend<StatusAppliedTriggeredEffectContext>(RemittiturUsed),
                        new ApplyStatusNode<StatusAppliedTriggeredEffectContext>(
                            CombatantTargetSelectors.Source,
                            new StatusDefinitionId(Cards.Keywords.Paperwork),
                            new ConstantExpression<StatusAppliedTriggeredEffectContext>(2)),
                    ]))),
                nameof(TriggerEvent.StatusApplied)),
        ]);

    // Three appointments, and the third is the one that is kept.
    public static string ThriceStruckId => "thrice_struck_appointment";
    private static CounterId AppointmentTurn => new("thrice_struck_turn");

    public static StatusData ThriceStruckAppointment => Rule(
        ThriceStruckId, "Thrice-Struck Appointment Card",
        "Every third turn, one more card is drawn.",
        [
            Trigger(new EffectProgram<CardsDrawnTriggeredEffectContext>(
                EveryNth<CardsDrawnTriggeredEffectContext>(
                    AppointmentTurn, 3, Draw<CardsDrawnTriggeredEffectContext>(1))),
                nameof(TriggerEvent.CardsDrawn)),
        ]);

    // The Monolith does not resist you. It closes. A turn spent closed is a turn it pays for.
    public static string ShutterKeyId => "shutter_key_late_hour";
    private static CounterId ShutterPlayed => new("shutter_key_played");
    private static CounterId ShutterOwed => new("shutter_key_owed");

    public static StatusData ShutterKeyLateHour => Rule(
        ShutterKeyId, "Shutter Key of the Late Hour",
        "End a turn having played nothing and the shutter pays for it: 12 Block and 1 Energy at your next "
        + "hand.",
        [
            Trigger(new EffectProgram<CardPlayedTriggeredEffectContext>(
                Set<CardPlayedTriggeredEffectContext>(ShutterPlayed, 1)),
                nameof(TriggerEvent.CardPlayed)),
            Trigger(new EffectProgram<TurnEndedTriggeredEffectContext>(
                new ConditionalEffectNode<TurnEndedTriggeredEffectContext>(
                    new ComparisonExpression<TurnEndedTriggeredEffectContext>(
                        new CombatantCounterExpression<TurnEndedTriggeredEffectContext>(You, ShutterPlayed),
                        ComparisonOperator.Equal,
                        new ConstantExpression<TurnEndedTriggeredEffectContext>(0)),
                    Set<TurnEndedTriggeredEffectContext>(ShutterOwed, 1))),
                nameof(TriggerEvent.TurnEnded)),
            // Both halves are paid at the HAND, because Block expires at the start of its owner's turn — a
            // shutter that paid at turn end would have bought nothing by the time anything swung.
            Trigger(new EffectProgram<CardsDrawnTriggeredEffectContext>(
                new CausalSequenceEffectNode<CardsDrawnTriggeredEffectContext>(
                [
                    new ConditionalEffectNode<CardsDrawnTriggeredEffectContext>(
                        new ComparisonExpression<CardsDrawnTriggeredEffectContext>(
                            new CombatantCounterExpression<CardsDrawnTriggeredEffectContext>(You, ShutterOwed),
                            ComparisonOperator.Greater,
                            new ConstantExpression<CardsDrawnTriggeredEffectContext>(0)),
                        new CausalSequenceEffectNode<CardsDrawnTriggeredEffectContext>(
                        [
                            Set<CardsDrawnTriggeredEffectContext>(ShutterOwed, 0),
                            new GainBlockNode<CardsDrawnTriggeredEffectContext>(
                                You, new ConstantExpression<CardsDrawnTriggeredEffectContext>(12)),
                            HeldEnergy.Hold<CardsDrawnTriggeredEffectContext>(1),
                        ])),
                    Set<CardsDrawnTriggeredEffectContext>(ShutterPlayed, 0),
                ])),
                nameof(TriggerEvent.CardsDrawn)),
        ]);

    // The Waiting Room gives you time and then eats it back. So does the chair.
    public static string ChairId => "chair_of_the_ninth_hour";
    private static CounterId ChairTurn => new("chair_ninth_hour_turn");

    public static StatusData ChairOfTheNinthHour => Rule(
        ChairId, "Chair of the Ninth Hour",
        "Two more cards in your opening hand, and one fewer on the turn after.",
        [
            Trigger(new EffectProgram<CardsDrawnTriggeredEffectContext>(
                new CausalSequenceEffectNode<CardsDrawnTriggeredEffectContext>(
                [
                    new SetCombatantCounterNode<CardsDrawnTriggeredEffectContext>(
                        You, ChairTurn, new ConstantExpression<CardsDrawnTriggeredEffectContext>(1),
                        relative: true),
                    new ConditionalEffectNode<CardsDrawnTriggeredEffectContext>(
                        new ComparisonExpression<CardsDrawnTriggeredEffectContext>(
                            new CombatantCounterExpression<CardsDrawnTriggeredEffectContext>(You, ChairTurn),
                            ComparisonOperator.Equal,
                            new ConstantExpression<CardsDrawnTriggeredEffectContext>(1)),
                        Draw<CardsDrawnTriggeredEffectContext>(2)),
                    // The second hand gives one back. A card taken OUT of a hand is a discard, and the
                    // oldest one goes — the same choice Redaction Knife makes, for the same reason: which
                    // card a player would have given up is not something a rule can ask.
                    new ConditionalEffectNode<CardsDrawnTriggeredEffectContext>(
                        new ComparisonExpression<CardsDrawnTriggeredEffectContext>(
                            new CombatantCounterExpression<CardsDrawnTriggeredEffectContext>(You, ChairTurn),
                            ComparisonOperator.Equal,
                            new ConstantExpression<CardsDrawnTriggeredEffectContext>(2)),
                        new MoveCardToZoneNode<CardsDrawnTriggeredEffectContext>(
                            You, new CardInZoneExpression<CardsDrawnTriggeredEffectContext>(CardZone.Hand, 0),
                            CardZone.DiscardPile)),
                ])),
                nameof(TriggerEvent.CardsDrawn)),
        ]);

    // The Iron Warrant records refusal as Contempt. Censure is what this game calls a refusal you carry, so
    // the nail files it back out again.
    public static string ContemptNailId => "contempt_ledger_nail";

    public static StatusData ContemptLedgerNail => Rule(
        ContemptNailId, "Contempt Ledger Nail",
        "Every Censure you take is filed against the weakest enemy: 2 Paperwork.",
        [
            Trigger(new EffectProgram<StatusAppliedTriggeredEffectContext>(
                new ConditionalEffectNode<StatusAppliedTriggeredEffectContext>(
                    IsStatus<StatusAppliedTriggeredEffectContext>(Cards.Keywords.Censure),
                    new ApplyStatusNode<StatusAppliedTriggeredEffectContext>(
                        Weakest, new StatusDefinitionId(Cards.Keywords.Paperwork),
                        new ConstantExpression<StatusAppliedTriggeredEffectContext>(2)))),
                nameof(TriggerEvent.StatusApplied)),
        ]);

    // Sustained pressure is what forces the portcullis up. The counterweight remembers a heavy turn.
    public static string GateChainId => "gate_chain_counterweight";
    private static CounterId GateDamage => new("gate_chain_damage");

    public static StatusData GateChainCounterweight => Rule(
        GateChainId, "Gate-Chain Counterweight",
        "Deal 20 or more damage in one turn and your first card next turn costs 1 less.",
        [
            Trigger(new EffectProgram<DamageDealtTriggeredEffectContext>(
                new SetCombatantCounterNode<DamageDealtTriggeredEffectContext>(
                    You, GateDamage,
                    new EventAmountExpression<DamageDealtTriggeredEffectContext>(),
                    relative: true)),
                nameof(TriggerEvent.DamageDealt)),
            Trigger(new EffectProgram<CardsDrawnTriggeredEffectContext>(
                new CausalSequenceEffectNode<CardsDrawnTriggeredEffectContext>(
                [
                    new ConditionalEffectNode<CardsDrawnTriggeredEffectContext>(
                        new ComparisonExpression<CardsDrawnTriggeredEffectContext>(
                            new CombatantCounterExpression<CardsDrawnTriggeredEffectContext>(You, GateDamage),
                            ComparisonOperator.GreaterOrEqual,
                            new ConstantExpression<CardsDrawnTriggeredEffectContext>(20)),
                        new ApplyStatusNode<CardsDrawnTriggeredEffectContext>(
                            You, new StatusDefinitionId(RelicRules.NextCardCheaperId),
                            new ConstantExpression<CardsDrawnTriggeredEffectContext>(1))),
                    Set<CardsDrawnTriggeredEffectContext>(GateDamage, 0),
                ])),
                nameof(TriggerEvent.CardsDrawn)),
        ]);

    // The Final Notice Knight closes the accounts. So does the spear: what is owed by one is owed by all.
    public static string SpearheadId => "sealed_spearhead";

    public static StatusData SealedSpearhead => Rule(
        SpearheadId, "Sealed Spearhead",
        "When an enemy falls, every other enemy takes 6 damage.",
        [
            Trigger(new EffectProgram<CombatantDownedTriggeredEffectContext>(
                new DealDamageNode<CombatantDownedTriggeredEffectContext>(
                    CombatantTargetSelectors.AllEnemiesOfSource,
                    new ConstantExpression<CombatantDownedTriggeredEffectContext>(6))),
                nameof(TriggerEvent.Downed), StatusTriggerScope.Anywhere),
        ]);

    // ══ ACT II ════════════════════════════════════════════════════════════════════════════════════════════

    // The Bell files a debt and hands you a receipt for it. The lip keeps the receipt.
    public static string BellLipId => "cracked_bell_lip";
    private static CounterId BellUsed => new("cracked_bell_lip_used");

    public static StatusData CrackedBellLip => Rule(
        BellLipId, "Cracked Bell-Lip",
        "The first Overdue filed against you each fight: 10 Block and a card.",
        [
            Trigger(new EffectProgram<StatusAppliedTriggeredEffectContext>(
                new ConditionalEffectNode<StatusAppliedTriggeredEffectContext>(
                    new AndExpression<StatusAppliedTriggeredEffectContext>(
                        Unspent<StatusAppliedTriggeredEffectContext>(BellUsed),
                        IsStatus<StatusAppliedTriggeredEffectContext>(ActTwo.OverdueId)),
                    new CausalSequenceEffectNode<StatusAppliedTriggeredEffectContext>(
                    [
                        Spend<StatusAppliedTriggeredEffectContext>(BellUsed),
                        new GainBlockNode<StatusAppliedTriggeredEffectContext>(
                            CombatantTargetSelectors.EventTarget,
                            new ConstantExpression<StatusAppliedTriggeredEffectContext>(10)),
                        new DrawCardsNode<StatusAppliedTriggeredEffectContext>(
                            CombatantTargetSelectors.EventTarget,
                            new ConstantExpression<StatusAppliedTriggeredEffectContext>(1)),
                    ]))),
                nameof(TriggerEvent.StatusApplied)),
        ]);

    // Every misfiling narrows the aisle. The roller carries one back.
    public static string RollerPinId => "roller_pin_of_the_stacks";
    private static CounterId RollerUsed => new("roller_pin_used");

    public static StatusData RollerPinOfTheStacks => Rule(
        RollerPinId, "Roller Pin of the Stacks",
        "The first card misfiled against you each fight is not misfiled.",
        [
            // A misfiling is a MARK on a card instance in this port, so the pin takes the mark off the first
            // card that carries it rather than intercepting a move that has already happened.
            Trigger(new EffectProgram<CardMovedToZoneTriggeredEffectContext>(
                new ConditionalEffectNode<CardMovedToZoneTriggeredEffectContext>(
                    Unspent<CardMovedToZoneTriggeredEffectContext>(RollerUsed),
                    new CausalSequenceEffectNode<CardMovedToZoneTriggeredEffectContext>(
                    [
                        Spend<CardMovedToZoneTriggeredEffectContext>(RollerUsed),
                        new DrawCardsNode<CardMovedToZoneTriggeredEffectContext>(
                            You, new ConstantExpression<CardMovedToZoneTriggeredEffectContext>(1)),
                    ]))),
                nameof(TriggerEvent.CardMovedToZone)),
        ]);

    // The creature is the silence, not the words. A hand you did not empty is the silence kept.
    public static string UnspokenId => "the_unspoken_word";

    public static StatusData TheUnspokenWord => Rule(
        UnspokenId, "The Unspoken Word",
        "End a turn still holding two cards or more: 4 Block.",
        [
            // Paid at the HAND and not at turn end, for the reason Block always is: it expires at the start
            // of its owner's turn, so a wall built at turn end is gone before anything reaches it. The count
            // that decides it is taken at turn end and read at the hand.
            Trigger(new EffectProgram<TurnEndedTriggeredEffectContext>(
                new ConditionalEffectNode<TurnEndedTriggeredEffectContext>(
                    new ComparisonExpression<TurnEndedTriggeredEffectContext>(
                        new CombatantZoneCardCountExpression<TurnEndedTriggeredEffectContext>(
                            You, CardZone.Hand, null),
                        ComparisonOperator.GreaterOrEqual,
                        new ConstantExpression<TurnEndedTriggeredEffectContext>(2)),
                    Set<TurnEndedTriggeredEffectContext>(UnspokenOwed, 1))),
                nameof(TriggerEvent.TurnEnded)),
            Trigger(new EffectProgram<CardsDrawnTriggeredEffectContext>(
                new ConditionalEffectNode<CardsDrawnTriggeredEffectContext>(
                    new ComparisonExpression<CardsDrawnTriggeredEffectContext>(
                        new CombatantCounterExpression<CardsDrawnTriggeredEffectContext>(You, UnspokenOwed),
                        ComparisonOperator.Greater,
                        new ConstantExpression<CardsDrawnTriggeredEffectContext>(0)),
                    new CausalSequenceEffectNode<CardsDrawnTriggeredEffectContext>(
                    [
                        Set<CardsDrawnTriggeredEffectContext>(UnspokenOwed, 0),
                        new GainBlockNode<CardsDrawnTriggeredEffectContext>(
                            You, new ConstantExpression<CardsDrawnTriggeredEffectContext>(4)),
                    ]))),
                nameof(TriggerEvent.CardsDrawn)),
        ]);

    private static CounterId UnspokenOwed => new("the_unspoken_word_owed");

    // The Oracle blacks out a field and asks about it. The strip blacks out a blow.
    public static string CensoringInkId => "strip_of_censoring_ink";
    private static CounterId InkUsed => new("strip_of_censoring_ink_used");

    public static StatusData StripOfCensoringInk => Rule(
        CensoringInkId, "Strip of Censoring Ink",
        "Once each fight, the first blow that gets through is blacked out: 8 of it does not happen.",
        [
            Trigger(new EffectProgram<CardsDrawnTriggeredEffectContext>(
                new ConditionalEffectNode<CardsDrawnTriggeredEffectContext>(
                    new AndExpression<CardsDrawnTriggeredEffectContext>(
                        FirstRound<CardsDrawnTriggeredEffectContext>(),
                        Unspent<CardsDrawnTriggeredEffectContext>(InkUsed)),
                    new CausalSequenceEffectNode<CardsDrawnTriggeredEffectContext>(
                    [
                        Spend<CardsDrawnTriggeredEffectContext>(InkUsed),
                        // A blow that does not happen is Block that nothing spent — the only shape in this
                        // engine for "prevent some of the next hit" that does not need a damage interceptor.
                        new GainBlockNode<CardsDrawnTriggeredEffectContext>(
                            You, new ConstantExpression<CardsDrawnTriggeredEffectContext>(8)),
                    ]))),
                nameof(TriggerEvent.CardsDrawn)),
        ]);

    // One book cites, the other collects. The thread is what runs between them: the second reading is worth
    // more than the first.
    public static string ConcordanceId => "line_between_the_volumes";

    public static StatusData ConcordanceThread => Rule(
        ConcordanceId, "The Line Between the Volumes",
        "The second card you play each turn strikes 4 harder.",
        [
            Trigger(new EffectProgram<CardPlayedTriggeredEffectContext>(
                new CausalSequenceEffectNode<CardPlayedTriggeredEffectContext>(
                [
                    new SetCombatantCounterNode<CardPlayedTriggeredEffectContext>(
                        You, ConcordancePlays,
                        new ConstantExpression<CardPlayedTriggeredEffectContext>(1), relative: true),
                    new ConditionalEffectNode<CardPlayedTriggeredEffectContext>(
                        new ComparisonExpression<CardPlayedTriggeredEffectContext>(
                            new CombatantCounterExpression<CardPlayedTriggeredEffectContext>(
                                You, ConcordancePlays),
                            ComparisonOperator.Equal,
                            new ConstantExpression<CardPlayedTriggeredEffectContext>(2)),
                        new DealDamageNode<CardPlayedTriggeredEffectContext>(
                            Weakest, new ConstantExpression<CardPlayedTriggeredEffectContext>(4))),
                ])),
                nameof(TriggerEvent.CardPlayed)),
            Trigger(new EffectProgram<CardsDrawnTriggeredEffectContext>(
                Set<CardsDrawnTriggeredEffectContext>(ConcordancePlays, 0)),
                nameof(TriggerEvent.CardsDrawn)),
        ]);

    private static CounterId ConcordancePlays => new("line_between_the_volumes_plays");

    // The Drawer never keeps anything. Neither does this.
    public static string DrawerId => "drawer_within_a_drawer";
    private static CounterId DrawerUsed => new("drawer_within_a_drawer_used");

    public static StatusData DrawerWithinADrawer => Rule(
        DrawerId, "Drawer Within a Drawer",
        "The first card you exhaust each fight comes back to your discard pile.",
        [
            Trigger(new EffectProgram<CardExhaustedTriggeredEffectContext>(
                new ConditionalEffectNode<CardExhaustedTriggeredEffectContext>(
                    Unspent<CardExhaustedTriggeredEffectContext>(DrawerUsed),
                    new CausalSequenceEffectNode<CardExhaustedTriggeredEffectContext>(
                    [
                        Spend<CardExhaustedTriggeredEffectContext>(DrawerUsed),
                        new MoveCardToZoneNode<CardExhaustedTriggeredEffectContext>(
                            You,
                            new CardInZoneExpression<CardExhaustedTriggeredEffectContext>(CardZone.ExhaustPile, 0),
                            CardZone.DiscardPile),
                    ]))),
                nameof(TriggerEvent.CardMovedToZone)),
        ]);

    // A clock with no present. The first thing you do happens twice, once in each of the hours it has.
    public static string PresentHandId => "missing_present_hand";
    private static CounterId PresentHandUsed => new("missing_present_hand_used");

    public static StatusData MissingPresentHand => Rule(
        PresentHandId, "The Missing Present Hand",
        "Your opening turn is filed to the past: 6 Block and a card at your second hand.",
        [
            // ADAPTATION. "The first card resolves again next turn" would have to keep a card instance
            // across a turn boundary and replay it in a context it was not played in; a replay node exists,
            // but it replays a card the CURRENT play is holding. So the clock pays what a turn-one card is
            // worth, one turn later — which is what "filed to the past" does here.
            Trigger(new EffectProgram<CardsDrawnTriggeredEffectContext>(
                new ConditionalEffectNode<CardsDrawnTriggeredEffectContext>(
                    new AndExpression<CardsDrawnTriggeredEffectContext>(
                        Unspent<CardsDrawnTriggeredEffectContext>(PresentHandUsed),
                        new NotExpression<CardsDrawnTriggeredEffectContext>(
                            FirstRound<CardsDrawnTriggeredEffectContext>())),
                    new CausalSequenceEffectNode<CardsDrawnTriggeredEffectContext>(
                    [
                        Spend<CardsDrawnTriggeredEffectContext>(PresentHandUsed),
                        new GainBlockNode<CardsDrawnTriggeredEffectContext>(
                            You, new ConstantExpression<CardsDrawnTriggeredEffectContext>(6)),
                        Draw<CardsDrawnTriggeredEffectContext>(1),
                    ]))),
                nameof(TriggerEvent.CardsDrawn)),
        ]);

    // ══ ACT III ═══════════════════════════════════════════════════════════════════════════════════════════

    // The Stag counts every licence you spend. The tine does not let it count the first.
    public static string AntlerTineId => "pre_approved_antler_tine";
    private static CounterId TineUsed => new("pre_approved_antler_tine_used");

    public static StatusData PreApprovedAntlerTine => Rule(
        AntlerTineId, "Pre-Approved Antler Tine",
        "The first Safe-Conduct you spend each fight is not spent.",
        [
            Trigger(new EffectProgram<StatusChargesReducedTriggeredEffectContext>(
                new ConditionalEffectNode<StatusChargesReducedTriggeredEffectContext>(
                    Unspent<StatusChargesReducedTriggeredEffectContext>(TineUsed),
                    new CausalSequenceEffectNode<StatusChargesReducedTriggeredEffectContext>(
                    [
                        Spend<StatusChargesReducedTriggeredEffectContext>(TineUsed),
                        new ApplyStatusNode<StatusChargesReducedTriggeredEffectContext>(
                            You, new StatusDefinitionId(ActThree.SafeConductId),
                            new ConstantExpression<StatusChargesReducedTriggeredEffectContext>(1)),
                    ]))),
                nameof(TriggerEvent.StatusStacksChanged)),
        ]);

    // The web mends. So does this, and the mending is worth something.
    public static string MendedThreadId => "mended_thread";

    public static StatusData MendedThread => Rule(
        MendedThreadId, "Mended Thread of the Grandmother",
        "Every fight opens with 1 Safe-Conduct, and spending one gives 3 Block.",
        [
            Trigger(new EffectProgram<CardsDrawnTriggeredEffectContext>(
                new ConditionalEffectNode<CardsDrawnTriggeredEffectContext>(
                    FirstRound<CardsDrawnTriggeredEffectContext>(),
                    new ApplyStatusNode<CardsDrawnTriggeredEffectContext>(
                        You, new StatusDefinitionId(ActThree.SafeConductId),
                        new ConstantExpression<CardsDrawnTriggeredEffectContext>(1)))),
                nameof(TriggerEvent.CardsDrawn)),
            Trigger(new EffectProgram<StatusChargesReducedTriggeredEffectContext>(
                new GainBlockNode<StatusChargesReducedTriggeredEffectContext>(
                    You, new ConstantExpression<StatusChargesReducedTriggeredEffectContext>(3))),
                nameof(TriggerEvent.StatusStacksChanged)),
        ]);

    // A debt means something else on the far bank.
    public static string TollStoneId => "toll_stone_wrong_bank";
    private static CounterId TollUsed => new("toll_stone_used");

    public static StatusData TollStoneWrongBank => Rule(
        TollStoneId, "Toll-Stone from the Wrong Bank",
        "The first restitution you pay each fight also gives 6 Block.",
        [
            Trigger(new EffectProgram<StatusAppliedTriggeredEffectContext>(
                new ConditionalEffectNode<StatusAppliedTriggeredEffectContext>(
                    new AndExpression<StatusAppliedTriggeredEffectContext>(
                        Unspent<StatusAppliedTriggeredEffectContext>(TollUsed),
                        IsStatus<StatusAppliedTriggeredEffectContext>(ActThree.ClaimId)),
                    new CausalSequenceEffectNode<StatusAppliedTriggeredEffectContext>(
                    [
                        Spend<StatusAppliedTriggeredEffectContext>(TollUsed),
                        new GainBlockNode<StatusAppliedTriggeredEffectContext>(
                            CombatantTargetSelectors.EventTarget,
                            new ConstantExpression<StatusAppliedTriggeredEffectContext>(6)),
                    ]))),
                nameof(TriggerEvent.StatusApplied)),
        ]);

    // Every payment becomes ammunition unless change is left behind. The coin is the change.
    public static string ThroatCoinId => "coin_left_in_the_throat";

    public static StatusData CoinLeftInTheThroat => Rule(
        ThroatCoinId, "Coin Left in the Throat",
        "Every Claim laid on you leaves 2 Block behind.",
        [
            // ADAPTATION: the design's "gain 2 Gold" cannot be paid inside a fight — Gold is a RUN resource
            // and no combat effect reaches it. The coin pays in the currency a fight has instead, and the
            // relic's run half (a purse at every victory) carries the Gold.
            Trigger(new EffectProgram<StatusAppliedTriggeredEffectContext>(
                new ConditionalEffectNode<StatusAppliedTriggeredEffectContext>(
                    IsStatus<StatusAppliedTriggeredEffectContext>(ActThree.ClaimId),
                    new GainBlockNode<StatusAppliedTriggeredEffectContext>(
                        CombatantTargetSelectors.EventTarget,
                        new ConstantExpression<StatusAppliedTriggeredEffectContext>(2)))),
                nameof(TriggerEvent.StatusApplied)),
        ]);

    // The Queen's formation, kept by choice.
    public static string ProperLineId => "head_of_the_line";

    public static StatusData TheProperLine => Rule(
        ProperLineId, "The Head of the Line",
        "Play three cards in a turn and the line is kept: draw 2.",
        [
            Trigger(new EffectProgram<CardPlayedTriggeredEffectContext>(
                new CausalSequenceEffectNode<CardPlayedTriggeredEffectContext>(
                [
                    new SetCombatantCounterNode<CardPlayedTriggeredEffectContext>(
                        You, LinePlays,
                        new ConstantExpression<CardPlayedTriggeredEffectContext>(1), relative: true),
                    new ConditionalEffectNode<CardPlayedTriggeredEffectContext>(
                        new ComparisonExpression<CardPlayedTriggeredEffectContext>(
                            new CombatantCounterExpression<CardPlayedTriggeredEffectContext>(You, LinePlays),
                            ComparisonOperator.Equal,
                            new ConstantExpression<CardPlayedTriggeredEffectContext>(3)),
                        new DrawCardsNode<CardPlayedTriggeredEffectContext>(
                            You, new ConstantExpression<CardPlayedTriggeredEffectContext>(2))),
                ])),
                nameof(TriggerEvent.CardPlayed)),
            Trigger(new EffectProgram<CardsDrawnTriggeredEffectContext>(
                Set<CardsDrawnTriggeredEffectContext>(LinePlays, 0)),
                nameof(TriggerEvent.CardsDrawn)),
        ]);

    private static CounterId LinePlays => new("head_of_the_line_plays");

    // The hedge enjoins a remedy. The tag is a counter-injunction: the first thing forbidden to you is not.
    public static string BoneTagId => "bone_tag_from_the_juniper";

    public static StatusData BoneTagFromTheJuniper => new()
    {
        Id = BoneTagId,
        NameKey = "Bone Tag from the Juniper",
        DescriptionKey = "The first affliction laid on you each fight finds the way already spoken for.",
        Polarity = StatusPolarity.Neutral,
        StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
        UsesStacks = true,
        Tags = [],
        // A refusal, not a reaction: the tag is a prohibition of one stack, which is the only shape that can
        // stop something before it lands rather than answer it afterwards.
        Prevention = new StatusPreventionData(StatusPreventionScope.UnwantedByBearer, StacksPerStack: 1),
        Triggers =
        [
            Trigger(new EffectProgram<CardsDrawnTriggeredEffectContext>(
                new ConditionalEffectNode<CardsDrawnTriggeredEffectContext>(
                    FirstRound<CardsDrawnTriggeredEffectContext>(),
                    new ApplyStatusNode<CardsDrawnTriggeredEffectContext>(
                        You, new StatusDefinitionId(BoneTagCharmId),
                        new ConstantExpression<CardsDrawnTriggeredEffectContext>(1)))),
                nameof(TriggerEvent.CardsDrawn)),
        ],
    };

    // The charge the tag lays down: one stack, refusing one whole application, renewed at every opening hand.
    public static string BoneTagCharmId => "bone_tag_charm";

    public static StatusData BoneTagCharm => new()
    {
        Id = BoneTagCharmId,
        NameKey = "Spoken For",
        DescriptionKey = "The next affliction laid on you is refused.",
        Polarity = StatusPolarity.Buff,
        StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
        UsesStacks = true,
        Tags = [],
        Prevention = new StatusPreventionData(
            StatusPreventionScope.UnwantedByBearer, StacksPerStack: 1, RefusesWholeApplication: true),
        Triggers = [],
    };

    // Yesterday's boundary is today's defence. What lapses on its own leaves something behind.
    public static string BoundaryCordId => "obsolete_boundary_cord";
    private static CounterId CordRead => new("obsolete_boundary_cord_read");

    public static StatusData ObsoleteBoundaryCord => Rule(
        BoundaryCordId, "Obsolete Boundary Cord",
        "Every turn in which something on you lapsed of its own accord: 3 Block.",
        [
            // Fading is not an engine event in this port — it is a convention. Every status that sheds a
            // stack because a turn went by writes it through ActFour.Fade, which counts it on the bearer, so
            // the cord reads the tally against its own bookmark. That also makes it act-blind: it works in
            // the City exactly as it works in the Labyrinth.
            Trigger(new EffectProgram<CardsDrawnTriggeredEffectContext>(
                new CausalSequenceEffectNode<CardsDrawnTriggeredEffectContext>(
                [
                    new ConditionalEffectNode<CardsDrawnTriggeredEffectContext>(
                        new ComparisonExpression<CardsDrawnTriggeredEffectContext>(
                            ActFour.SinceLastLooked<CardsDrawnTriggeredEffectContext>(
                                You, ActFour.DecaysUnpreserved, CordRead),
                            ComparisonOperator.Greater,
                            new ConstantExpression<CardsDrawnTriggeredEffectContext>(0)),
                        new GainBlockNode<CardsDrawnTriggeredEffectContext>(
                            You, new ConstantExpression<CardsDrawnTriggeredEffectContext>(3))),
                    ActFour.MoveTheBookmark<CardsDrawnTriggeredEffectContext>(
                        You, ActFour.DecaysUnpreserved, CordRead),
                ])),
                nameof(TriggerEvent.CardsDrawn)),
        ]);

    // A Claim walks Hearing → Remand → Refusal. The reed cut at the hearing sends two.
    public static string ReedId => "reed_cut_at_the_hearing";
    private static CounterId ReedUsed => new("reed_cut_at_the_hearing_used");

    public static StatusData ReedCutAtTheHearing => Rule(
        ReedId, "Reed Cut at the Hearing",
        "The first Claim you lay each fight is laid twice.",
        [
            Trigger(new EffectProgram<StatusAppliedTriggeredEffectContext>(
                new ConditionalEffectNode<StatusAppliedTriggeredEffectContext>(
                    new AndExpression<StatusAppliedTriggeredEffectContext>(
                        Unspent<StatusAppliedTriggeredEffectContext>(ReedUsed),
                        IsStatus<StatusAppliedTriggeredEffectContext>(ActThree.ClaimId)),
                    new CausalSequenceEffectNode<StatusAppliedTriggeredEffectContext>(
                    [
                        Spend<StatusAppliedTriggeredEffectContext>(ReedUsed),
                        new ApplyStatusNode<StatusAppliedTriggeredEffectContext>(
                            CombatantTargetSelectors.EventTarget,
                            new StatusDefinitionId(ActThree.ClaimId),
                            new ConstantExpression<StatusAppliedTriggeredEffectContext>(1)),
                    ]))),
                nameof(TriggerEvent.StatusApplied), StatusTriggerScope.Anywhere),
        ]);

    // Under which custom would you prefer to be judged?
    public static string ThornId => "thorn_chosen_from_three";

    public static StatusData ThornChosenFromThree => Rule(
        ThornId, "Thorn Chosen From Three",
        "At every opening hand, choose: 10 Block, a card and 1 Energy, or 6 damage to every enemy.",
        [
            Trigger(new EffectProgram<CardsDrawnTriggeredEffectContext>(
                new ConditionalEffectNode<CardsDrawnTriggeredEffectContext>(
                    FirstRound<CardsDrawnTriggeredEffectContext>(),
                    new ChooseOptionsNode<CardsDrawnTriggeredEffectContext>(
                    [
                        new GainBlockNode<CardsDrawnTriggeredEffectContext>(
                            You, new ConstantExpression<CardsDrawnTriggeredEffectContext>(10)),
                        new CausalSequenceEffectNode<CardsDrawnTriggeredEffectContext>(
                        [
                            Draw<CardsDrawnTriggeredEffectContext>(1),
                            HeldEnergy.Hold<CardsDrawnTriggeredEffectContext>(1),
                        ]),
                        new DealDamageNode<CardsDrawnTriggeredEffectContext>(
                            CombatantTargetSelectors.AllEnemiesOfSource,
                            new ConstantExpression<CardsDrawnTriggeredEffectContext>(6)),
                    ],
                    ["The blunt thorn — 10 Block",
                     "The long thorn — a card and 1 Energy",
                     "The sharp thorn — 6 damage to every enemy"],
                    count: 1, purpose: "choose the thorn you are judged under"))),
                nameof(TriggerEvent.CardsDrawn)),
        ]);

    // ══ ACT IV ════════════════════════════════════════════════════════════════════════════════════════════

    // The Surveyor lets you pick how hard the survey is. The cord picks for you, once.
    public static string ErrantCordId => "the_shorter_ferrule";
    private static CounterId CordUsed => new("the_shorter_ferrule_used");

    public static StatusData TheErrantCord => Rule(
        ErrantCordId, "The Shorter Ferrule",
        "The first Weighed you take each fight is one lighter.",
        [
            Trigger(new EffectProgram<StatusAppliedTriggeredEffectContext>(
                new ConditionalEffectNode<StatusAppliedTriggeredEffectContext>(
                    new AndExpression<StatusAppliedTriggeredEffectContext>(
                        Unspent<StatusAppliedTriggeredEffectContext>(CordUsed),
                        IsStatus<StatusAppliedTriggeredEffectContext>(ActFour.WeighedId)),
                    new CausalSequenceEffectNode<StatusAppliedTriggeredEffectContext>(
                    [
                        Spend<StatusAppliedTriggeredEffectContext>(CordUsed),
                        new ModifyStatusStacksNode<StatusAppliedTriggeredEffectContext>(
                            CombatantTargetSelectors.EventTarget,
                            new StatusDefinitionId(ActFour.WeighedId),
                            new ConstantExpression<StatusAppliedTriggeredEffectContext>(-1)),
                    ]))),
                nameof(TriggerEvent.StatusApplied)),
        ]);

    // The Host is armoured by its own seals, and you choose which to break.
    public static string GranarySealId => "broken_granary_seal";
    private static CounterId SealUsed => new("broken_granary_seal_used");

    public static StatusData BrokenGranarySeal => Rule(
        GranarySealId, "Broken Granary Seal",
        "The first time each fight you strike an enemy that is behind Block, the seal breaks: 8 more damage.",
        [
            Trigger(new EffectProgram<DamageDealtTriggeredEffectContext>(
                new ConditionalEffectNode<DamageDealtTriggeredEffectContext>(
                    new AndExpression<DamageDealtTriggeredEffectContext>(
                        Unspent<DamageDealtTriggeredEffectContext>(SealUsed),
                        new ComparisonExpression<DamageDealtTriggeredEffectContext>(
                            new CombatantDefensivePoolExpression<DamageDealtTriggeredEffectContext>(
                                CombatantTargetSelectors.EventTarget,
                                StandardCombatIds.BlockDefensivePool),
                            ComparisonOperator.Greater,
                            new ConstantExpression<DamageDealtTriggeredEffectContext>(0))),
                    new CausalSequenceEffectNode<DamageDealtTriggeredEffectContext>(
                    [
                        Spend<DamageDealtTriggeredEffectContext>(SealUsed),
                        new DealDamageNode<DamageDealtTriggeredEffectContext>(
                            CombatantTargetSelectors.EventTarget,
                            new ConstantExpression<DamageDealtTriggeredEffectContext>(8)),
                    ]))),
                nameof(TriggerEvent.DamageDealt)),
        ]);

    // Every surcharge you actually pay is labour owed. The cut rope owes it back.
    public static string CorveeRopeId => "cut_corvee_rope";
    private static CounterId RopePaid => new("cut_corvee_rope_paid");

    public static StatusData CutCorveeRope => Rule(
        CorveeRopeId, "Cut Corvée Rope",
        "Every third Burdened surcharge you pay, the rope gives back: 1 Energy.",
        [
            Trigger(new EffectProgram<CardCostPaidTriggeredEffectContext>(
                new ConditionalEffectNode<CardCostPaidTriggeredEffectContext>(
                    new ComparisonExpression<CardCostPaidTriggeredEffectContext>(
                        new CombatantStatusStacksExpression<CardCostPaidTriggeredEffectContext>(
                            You, new StatusDefinitionId(ActFour.BurdenedId)),
                        ComparisonOperator.Greater,
                        new ConstantExpression<CardCostPaidTriggeredEffectContext>(0)),
                    EveryNth<CardCostPaidTriggeredEffectContext>(
                        RopePaid, 3, HeldEnergy.Hold<CardCostPaidTriggeredEffectContext>(1)))),
                nameof(TriggerEvent.CardCostPaid)),
        ]);

    // The register writes down every application it enlarges. One glyph is struck out.
    public static string GlyphId => "glyph_struck_from_the_name";
    private static CounterId GlyphUsed => new("glyph_struck_used");

    public static StatusData GlyphStruckFromTheName => Rule(
        GlyphId, "Glyph Struck From the Name",
        "The first time the register enlarges something against you each fight, it does not.",
        [
            Trigger(new EffectProgram<StatusApplicationAmplifiedTriggeredEffectContext>(
                new ConditionalEffectNode<StatusApplicationAmplifiedTriggeredEffectContext>(
                    Unspent<StatusApplicationAmplifiedTriggeredEffectContext>(GlyphUsed),
                    new CausalSequenceEffectNode<StatusApplicationAmplifiedTriggeredEffectContext>(
                    [
                        Spend<StatusApplicationAmplifiedTriggeredEffectContext>(GlyphUsed),
                        // Undo exactly what the register added, whatever it added it to.
                        new ModifyStatusStacksNode<StatusApplicationAmplifiedTriggeredEffectContext>(
                            You, new StatusDefinitionId(ActFour.InscribedId),
                            new ConstantExpression<StatusApplicationAmplifiedTriggeredEffectContext>(-1)),
                    ]))),
                nameof(TriggerEvent.StatusApplicationAmplified)),
        ]);

    // The Overseer counts what preservation holds. The shears are what happens when you let go.
    public static string ShearsId => "overseers_linen_shears";
    private static CounterId ShearsRead => new("overseers_linen_shears_read");
    private static CounterId ShearsUsed => new("overseers_linen_shears_used");

    public static StatusData OverseersLinenShears => Rule(
        ShearsId, "Overseer's Linen Shears",
        "The first thing that fades from you each fight is breath given back: heal 3 and 3 Block.",
        [
            Trigger(new EffectProgram<CardsDrawnTriggeredEffectContext>(
                new CausalSequenceEffectNode<CardsDrawnTriggeredEffectContext>(
                [
                    new ConditionalEffectNode<CardsDrawnTriggeredEffectContext>(
                        new AndExpression<CardsDrawnTriggeredEffectContext>(
                            Unspent<CardsDrawnTriggeredEffectContext>(ShearsUsed),
                            new ComparisonExpression<CardsDrawnTriggeredEffectContext>(
                                ActFour.SinceLastLooked<CardsDrawnTriggeredEffectContext>(
                                    You, ActFour.DecaysUnpreserved, ShearsRead),
                                ComparisonOperator.Greater,
                                new ConstantExpression<CardsDrawnTriggeredEffectContext>(0))),
                        new CausalSequenceEffectNode<CardsDrawnTriggeredEffectContext>(
                        [
                            Spend<CardsDrawnTriggeredEffectContext>(ShearsUsed),
                            new HealNode<CardsDrawnTriggeredEffectContext>(
                                You, new ConstantExpression<CardsDrawnTriggeredEffectContext>(3)),
                            new GainBlockNode<CardsDrawnTriggeredEffectContext>(
                                You, new ConstantExpression<CardsDrawnTriggeredEffectContext>(3)),
                        ])),
                    ActFour.MoveTheBookmark<CardsDrawnTriggeredEffectContext>(
                        You, ActFour.DecaysUnpreserved, ShearsRead),
                ])),
                nameof(TriggerEvent.CardsDrawn)),
        ]);

    // The Sphinx shows two of its three answers. This is the one it never showed.
    public static string RiddleId => "riddles_third_answer";

    public static StatusData RiddlesThirdAnswer => Rule(
        RiddleId, "The Riddle's Third Answer",
        "Every fight opens with 1 Ward Wax and 1 Seal.",
        [
            Trigger(new EffectProgram<CardsDrawnTriggeredEffectContext>(
                new ConditionalEffectNode<CardsDrawnTriggeredEffectContext>(
                    FirstRound<CardsDrawnTriggeredEffectContext>(),
                    new CausalSequenceEffectNode<CardsDrawnTriggeredEffectContext>(
                    [
                        new ApplyStatusNode<CardsDrawnTriggeredEffectContext>(
                            You, new StatusDefinitionId(Cards.Keywords.WardWax),
                            new ConstantExpression<CardsDrawnTriggeredEffectContext>(1)),
                        new ApplyStatusNode<CardsDrawnTriggeredEffectContext>(
                            You, new StatusDefinitionId(Cards.Keywords.Seal),
                            new ConstantExpression<CardsDrawnTriggeredEffectContext>(1)),
                    ]))),
                nameof(TriggerEvent.CardsDrawn)),
        ]);

    // Three robbers, and which one you take first decides the rest. The survivors are the point.
    public static string WickId => "lamp_thiefs_wick";

    public static StatusData LampThiefsWick => Rule(
        WickId, "The Lamp Thief's Wick",
        "When an enemy falls, 2 Block for every enemy still standing.",
        [
            Trigger(new EffectProgram<CombatantDownedTriggeredEffectContext>(
                new ForEachTargetEffectNode<CombatantDownedTriggeredEffectContext>(
                    CombatantTargetSelectors.AllEnemiesOfSource,
                    new GainBlockNode<CombatantDownedTriggeredEffectContext>(
                        CombatantTargetSelectors.Source,
                        new ConstantExpression<CombatantDownedTriggeredEffectContext>(2)))),
                nameof(TriggerEvent.Downed), StatusTriggerScope.Anywhere),
        ]);

    // One watch a turn, and a reckoning at the sixth.
    public static string DecanChipId => "decan_star_table_chip";
    private static CounterId DecanTurn => new("decan_star_table_turn");

    public static StatusData DecanStarTableChip => Rule(
        DecanChipId, "Decan Star-Table Chip",
        "Every sixth turn, one affliction leaves you.",
        [
            Trigger(new EffectProgram<CardsDrawnTriggeredEffectContext>(
                EveryNth<CardsDrawnTriggeredEffectContext>(
                    DecanTurn, 6,
                    new ModifySelectedStatusStacksNode<CardsDrawnTriggeredEffectContext>(
                        You,
                        new StatusSelectionSpec(StatusPolarityFilter.Debuff, StatusPick.First),
                        new ConstantExpression<CardsDrawnTriggeredEffectContext>(-1)))),
                nameof(TriggerEvent.CardsDrawn)),
        ]);

    // The cycle is fixed and entirely visible. Every third step falls where the stone is worn.
    public static string StepStoneId => "processional_step_stone";
    private static CounterId StepTurn => new("processional_step_turn");

    public static StatusData ProcessionalStepStone => Rule(
        StepStoneId, "Processional Step-Stone",
        "Every third turn, your first card costs nothing.",
        [
            Trigger(new EffectProgram<CardsDrawnTriggeredEffectContext>(
                EveryNth<CardsDrawnTriggeredEffectContext>(
                    StepTurn, 3,
                    new ApplyStatusNode<CardsDrawnTriggeredEffectContext>(
                        You, new StatusDefinitionId(RelicRules.NextCardFreeId),
                        new ConstantExpression<CardsDrawnTriggeredEffectContext>(1)))),
                nameof(TriggerEvent.CardsDrawn)),
        ]);

    // The account is rewritten rather than closed. A death prevention is a property of a status and of
    // nothing else (bought at V-1 for Nisaba), so the obituary IS one: it survives the blow at the stated
    // health and does not repeat, which is one ending per fight and not three.
    public static string ThirdEndingId => "the_third_ending";

    public static StatusData TheThirdEnding => new()
    {
        Id = ThirdEndingId,
        NameKey = "The Third Ending",
        DescriptionKey = "The blow that would end you is struck through, once each fight. You stand at 8.",
        Polarity = StatusPolarity.Buff,
        StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
        UsesStacks = true,
        Tags = [],
        DeathPrevention = new StatusDeathPreventionData(8, []),
        Triggers = [],
    };

    // ══ THE MIMIC ═════════════════════════════════════════════════════════════════════════════════════════

    // One tooth in four grades. A later mimic replaces the grade already held rather than adding to it, so
    // the four rules are the same rule at four sizes and never stack.
    public static StatusData FalseLidToothI => Tooth(1, 4, 0);
    public static StatusData FalseLidToothII => Tooth(2, 8, 0);
    public static StatusData FalseLidToothIII => Tooth(3, 12, 2);
    public static StatusData FalseLidToothIV => Tooth(4, 16, 4);

    public static string ToothId(int grade) => $"tooth_from_the_false_lid_{grade}";

    private static StatusData Tooth(int grade, int block, int heal) => Rule(
        ToothId(grade), $"Tooth From the False Lid {Roman(grade)}",
        heal > 0
            ? $"The first blow of every fight glances off the tooth: {block} Block, and heal {heal}."
            : $"The first blow of every fight glances off the tooth: {block} Block.",
        [
            Trigger(new EffectProgram<CardsDrawnTriggeredEffectContext>(
                new ConditionalEffectNode<CardsDrawnTriggeredEffectContext>(
                    FirstRound<CardsDrawnTriggeredEffectContext>(),
                    heal > 0
                        ? new CausalSequenceEffectNode<CardsDrawnTriggeredEffectContext>(
                        [
                            new GainBlockNode<CardsDrawnTriggeredEffectContext>(
                                You, new ConstantExpression<CardsDrawnTriggeredEffectContext>(block)),
                            new HealNode<CardsDrawnTriggeredEffectContext>(
                                You, new ConstantExpression<CardsDrawnTriggeredEffectContext>(heal)),
                        ])
                        : new GainBlockNode<CardsDrawnTriggeredEffectContext>(
                            You, new ConstantExpression<CardsDrawnTriggeredEffectContext>(block)))),
                nameof(TriggerEvent.CardsDrawn)),
        ]);

    private static string Roman(int n) => n switch { 1 => "I", 2 => "II", 3 => "III", _ => "IV" };

    // ══ shapes ════════════════════════════════════════════════════════════════════════════════════════════

    // "the Nth, and every Nth after it": a counter that only grows, read modulo N. The counter is bumped
    // BEFORE it is read, so the first payment is 1 and the third is the one that answers.
    private static IEffectNode<TContext> EveryNth<TContext>(CounterId counter, int n, IEffectNode<TContext> body)
        where TContext : class =>
        new CausalSequenceEffectNode<TContext>(
        [
            new SetCombatantCounterNode<TContext>(
                You, counter, new ConstantExpression<TContext>(1), relative: true),
            new ConditionalEffectNode<TContext>(
                new ComparisonExpression<TContext>(
                    new RemainderExpression<TContext>(
                        new CombatantCounterExpression<TContext>(You, counter),
                        new ConstantExpression<TContext>(n)),
                    ComparisonOperator.Equal, new ConstantExpression<TContext>(0)),
                body),
        ]);

    private static ICombatExpression<TContext, bool> IsStatus<TContext>(string statusId) where TContext : class =>
        new TriggerEventStatusIsExpression<TContext>(new StatusDefinitionId(statusId));

    private static ICombatExpression<TContext, bool> FirstRound<TContext>() where TContext : class =>
        new ComparisonExpression<TContext>(
            new RoundNumberExpression<TContext>(), ComparisonOperator.Equal, new ConstantExpression<TContext>(1));

    private static ICombatExpression<TContext, bool> Unspent<TContext>(CounterId latch) where TContext : class =>
        new ComparisonExpression<TContext>(
            new CombatantCounterExpression<TContext>(You, latch),
            ComparisonOperator.Equal, new ConstantExpression<TContext>(0));

    private static IEffectNode<TContext> Spend<TContext>(CounterId latch) where TContext : class =>
        Set<TContext>(latch, 1);

    private static IEffectNode<TContext> Set<TContext>(CounterId id, int value) where TContext : class =>
        new SetCombatantCounterNode<TContext>(You, id, new ConstantExpression<TContext>(value));

    private static IEffectNode<TContext> Draw<TContext>(int cards) where TContext : class =>
        new DrawCardsNode<TContext>(You, new ConstantExpression<TContext>(cards));

    private static StatusData Rule(
        string id, string name, string description, IReadOnlyList<StatusTriggerData> triggers) => new()
        {
            Id = id,
            NameKey = name,
            DescriptionKey = description,
            Polarity = StatusPolarity.Neutral,
            StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
            UsesStacks = true,
            Tags = [],
            PassiveModifiers = [],
            Triggers = triggers,
        };

    private static StatusTriggerData Trigger<TContext>(
        EffectProgram<TContext> program, string trigger,
        StatusTriggerScope scope = StatusTriggerScope.Bearer) where TContext : class =>
        new(trigger, JsonSerializer.SerializeToElement(
            TheOpeningHand.For(program, trigger), CombatJson.CreateOptions<TContext>()), scope);
}
