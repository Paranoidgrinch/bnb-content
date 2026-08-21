using System.Text.Json;
using RogueDeck.Core.Combat;
using RogueDeck.Sandbox.Composition;
using RogueDeck.Scenario.Authoring;
using BnbContent.Converter.Cards;
using static BnbContent.Converter.Relics.RelicAuthoring;

namespace BnbContent.Converter.Relics;

// The in-combat rules the Event relics install. Unlike the Shop pool, almost every Event relic is a rule of
// the FIGHT — they are one-off prizes from a single named branch, and they read like small Rites.
public static class EventRelicRules
{
    public static IReadOnlyList<StatusData> All() =>
        [OriginalityStamp, UnclaimedPropertyTag, UncalledTicket, UncalledTicketBoon, ThresholdWard,
         InheritedBoneFolder];

    // "Once per combat, the first non-Junk card you play is copied into your hand, and the copy is cheaper."
    // The copy cannot be cheapened directly — nothing hands back a handle on the instance that was just made —
    // so the discount rides on the wearer as "your next card costs 1 less", which is where the card pools put
    // it too.
    public static readonly StatusData OriginalityStamp = Rule(
        "originality_stamp_rule", "Originality Stamp",
        "The first card you play each fight is worth copying.",
        [
            Trigger(new EffectProgram<CardPlayedTriggeredEffectContext>(
                Once<CardPlayedTriggeredEffectContext>("originality_stamp",
                    new ConditionalEffectNode<CardPlayedTriggeredEffectContext>(
                        NotJunk<CardPlayedTriggeredEffectContext>(),
                        new CausalSequenceEffectNode<CardPlayedTriggeredEffectContext>(
                        [
                            new CreateCardCopyNode<CardPlayedTriggeredEffectContext>(
                                CombatantTargetSelectors.Source,
                                new TriggerEventCardInstanceExpression<CardPlayedTriggeredEffectContext>(),
                                CardZone.Hand,
                                new ConstantExpression<CardPlayedTriggeredEffectContext>(1)),
                            new ApplyStatusNode<CardPlayedTriggeredEffectContext>(
                                CombatantTargetSelectors.Source,
                                new StatusDefinitionId(RelicRules.NextCardCheaperId),
                                new ConstantExpression<CardPlayedTriggeredEffectContext>(1)),
                        ])))),
                nameof(TriggerEvent.CardPlayed)),
        ]);

    // "Combat start: mark a random non-Junk card; the first time it enters your hand it costs 1 less." The
    // draw pile is already shuffled, so the card on top of it IS the random one, and the mark rides on the
    // instance until it is played — see ADAPTATIONS.
    public static readonly StatusData UnclaimedPropertyTag =
        AtOpening("unclaimed_property_tag_rule", "Unclaimed Property Tag",
            "One card in your deck was never claimed, and comes cheap.",
            Cheapen<CardsDrawnTriggeredEffectContext>(CardZone.DrawPile, 1, 1));

    // "Once per combat, end a turn holding a card you could not afford: put it back on top of the draw pile,
    // and next turn gain 1 Energy and draw 1." What a rule cannot ask is which card was unaffordable, so the
    // ticket takes whatever is still in hand — see ADAPTATIONS.
    public static readonly StatusData UncalledTicket = Rule(
        "uncalled_ticket_rule", "Uncalled Ticket",
        "A card you never got to play is called first next turn.",
        [
            Trigger(new EffectProgram<TurnEndedTriggeredEffectContext>(
                Once<TurnEndedTriggeredEffectContext>("uncalled_ticket",
                    new ForEachCardInZoneNode<TurnEndedTriggeredEffectContext>(
                        CombatantTargetSelectors.Source, CardZone.Hand,
                        new CausalSequenceEffectNode<TurnEndedTriggeredEffectContext>(
                        [
                            new MoveCardToZoneNode<TurnEndedTriggeredEffectContext>(
                                CombatantTargetSelectors.Source,
                                new IteratedCardExpression<TurnEndedTriggeredEffectContext>(),
                                CardZone.DrawPile),
                            new ApplyStatusNode<TurnEndedTriggeredEffectContext>(
                                CombatantTargetSelectors.Source,
                                new StatusDefinitionId("uncalled_ticket_boon"),
                                new ConstantExpression<TurnEndedTriggeredEffectContext>(1)),
                        ]),
                        takeFirst: 1))),
                nameof(TriggerEvent.TurnEnded)),
        ]);

    // The ticket's promise, as a thing that can be spent: the boon is applied at the end of one turn and pays
    // out on the next turn's draw, then removes itself.
    public static readonly StatusData UncalledTicketBoon = Rule(
        "uncalled_ticket_boon", "Called Next", "Your ticket comes up next turn.",
        [
            Trigger(new EffectProgram<CardsDrawnTriggeredEffectContext>(
                new CausalSequenceEffectNode<CardsDrawnTriggeredEffectContext>(
                [
                    Energy<CardsDrawnTriggeredEffectContext>(1),
                    Draw<CardsDrawnTriggeredEffectContext>(1),
                    new RemoveStatusNode<CardsDrawnTriggeredEffectContext>(
                        CombatantTargetSelectors.Source, new StatusDefinitionId("uncalled_ticket_boon")),
                ])),
                nameof(TriggerEvent.CardsDrawn)),
        ]);

    // "Start combat with 6 Block. The first time an enemy gains a positive status each combat, gain 1 Energy
    // and 6 Block." The rule watches the whole fight, so it has to check that whoever gained the status is not
    // the wearer — a buff the player gives itself is not what the ward is for.
    public static readonly StatusData ThresholdWard = Rule(
        "threshold_ward_rule", "Threshold Ward",
        "The seal guards the door, and answers when the other side is blessed.",
        [
            Trigger(new EffectProgram<CardsDrawnTriggeredEffectContext>(
                Once<CardsDrawnTriggeredEffectContext>("threshold_ward_open",
                    Block<CardsDrawnTriggeredEffectContext>(6))),
                nameof(TriggerEvent.CardsDrawn)),
            Trigger(new EffectProgram<StatusAppliedTriggeredEffectContext>(
                OnOtherSideBlessed<StatusAppliedTriggeredEffectContext>()),
                nameof(TriggerEvent.StatusApplied), StatusTriggerScope.Anywhere),
            Trigger(new EffectProgram<StatusMergedTriggeredEffectContext>(
                OnOtherSideBlessed<StatusMergedTriggeredEffectContext>()),
                nameof(TriggerEvent.StatusMerged), StatusTriggerScope.Anywhere),
        ]);

    // "Combat start: mark a random unupgraded non-Junk card; the first time it is drawn, upgrade it and make
    // it cheaper — and if nothing is eligible, draw an extra card on turn 1." Nothing upgrades a card mid-fight,
    // so the folder keeps the cheaper half and gives the extra card unconditionally — see ADAPTATIONS.
    public static readonly StatusData InheritedBoneFolder =
        AtOpening("inherited_bone_folder_rule", "Inherited Bone Folder",
            "An old tool makes one page easier, and finds you another.",
            new CausalSequenceEffectNode<CardsDrawnTriggeredEffectContext>(
            [
                Cheapen<CardsDrawnTriggeredEffectContext>(CardZone.DrawPile, 1, 1),
                Draw<CardsDrawnTriggeredEffectContext>(1),
            ]));

    // ── shapes ────────────────────────────────────────────────────────────────────────────────────────────

    // A rule that fires once, on the opening draw.
    private static StatusData AtOpening(
        string id, string name, string description, IEffectNode<CardsDrawnTriggeredEffectContext> body) =>
        Rule(id, name, description,
        [
            Trigger(new EffectProgram<CardsDrawnTriggeredEffectContext>(Once<CardsDrawnTriggeredEffectContext>(id, body)),
                nameof(TriggerEvent.CardsDrawn)),
        ]);

    private static IEffectNode<TContext> OnOtherSideBlessed<TContext>() where TContext : class =>
        new ConditionalEffectNode<TContext>(
            new AndExpression<TContext>(
                // Not the wearer: the ward answers the other side being blessed, not the player buffing itself.
                new NotExpression<TContext>(
                    new TargetHasStatusExpression<TContext>(
                        CombatantTargetSelectors.EventTarget, new StatusDefinitionId("threshold_ward_rule"))),
                new ComparisonExpression<TContext>(
                    new CombatantStacksByPolarityExpression<TContext>(
                        CombatantTargetSelectors.EventTarget, StatusPolarity.Buff),
                    ComparisonOperator.Greater, new ConstantExpression<TContext>(0))),
            new ForEachTargetEffectNode<TContext>(
                CombatantTargetSelectors.WithStatus(
                    CombatantTargetSelectors.AllCombatants, new StatusDefinitionId("threshold_ward_rule")),
                new ConditionalEffectNode<TContext>(
                    new ComparisonExpression<TContext>(
                        new CombatantCounterExpression<TContext>(
                            CombatantTargetSelectors.IterationTarget, new CounterId("threshold_ward_paid")),
                        ComparisonOperator.Equal, new ConstantExpression<TContext>(0)),
                    new CausalSequenceEffectNode<TContext>(
                    [
                        new GainResourceNode<TContext>(
                            CombatantTargetSelectors.IterationTarget, StandardCombatIds.EnergyResource,
                            new ConstantExpression<TContext>(1)),
                        new GainBlockNode<TContext>(
                            CombatantTargetSelectors.IterationTarget, new ConstantExpression<TContext>(6)),
                        new SetCombatantCounterNode<TContext>(
                            CombatantTargetSelectors.IterationTarget, new CounterId("threshold_ward_paid"),
                            new ConstantExpression<TContext>(1), relative: false),
                    ]))));

    // Mark the first `cards` instances of a zone as costing `by` less. The mark rides on the INSTANCE, which is
    // what makes "that card is cheaper" true wherever the card goes.
    private static IEffectNode<TContext> Cheapen<TContext>(CardZone zone, int cards, int by)
        where TContext : class =>
        new ForEachCardInZoneNode<TContext>(
            CombatantTargetSelectors.Source, zone,
            new SetCardInstanceMarkCounterNode<TContext>(
                CombatantTargetSelectors.Source,
                new IteratedCardExpression<TContext>(),
                StandardCombatIds.CardCostDeltaCounter,
                new ConstantExpression<TContext>(-by), relative: false),
            takeFirst: cards);

    private static ICombatExpression<TContext, bool> NotJunk<TContext>() where TContext : class =>
        new NotExpression<TContext>(
            new TriggerEventSourceCardHasTagExpression<TContext>(new TagId(CardAuthoring.JunkTag)));

    private static IEffectNode<TContext> Once<TContext>(string id, IEffectNode<TContext> body)
        where TContext : class =>
        new ConditionalEffectNode<TContext>(
            new ComparisonExpression<TContext>(
                new CombatantCounterExpression<TContext>(
                    CombatantTargetSelectors.Source, new CounterId(id + "_done")),
                ComparisonOperator.Equal, new ConstantExpression<TContext>(0)),
            new CausalSequenceEffectNode<TContext>(
            [
                body,
                new SetCombatantCounterNode<TContext>(
                    CombatantTargetSelectors.Source, new CounterId(id + "_done"),
                    new ConstantExpression<TContext>(1), relative: false),
            ]));

    private static IEffectNode<TContext> Block<TContext>(int amount) where TContext : class =>
        new GainBlockNode<TContext>(CombatantTargetSelectors.Source, new ConstantExpression<TContext>(amount));

    private static IEffectNode<TContext> Draw<TContext>(int cards) where TContext : class =>
        new DrawCardsNode<TContext>(CombatantTargetSelectors.Source, new ConstantExpression<TContext>(cards));

    private static IEffectNode<TContext> Energy<TContext>(int amount) where TContext : class =>
        new GainResourceNode<TContext>(CombatantTargetSelectors.Source, StandardCombatIds.EnergyResource,
            new ConstantExpression<TContext>(amount));

    private static StatusTriggerData Trigger<TContext>(
        EffectProgram<TContext> program, string trigger,
        StatusTriggerScope scope = StatusTriggerScope.Bearer) where TContext : class =>
        new(trigger, JsonSerializer.SerializeToElement(program, CombatJson.CreateOptions<TContext>()), scope);
}
