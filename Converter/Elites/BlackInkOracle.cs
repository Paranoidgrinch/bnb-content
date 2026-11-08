using System.Text.Json;
using RogueDeck.Core.Combat;
using RogueDeck.Scenario.Authoring;

namespace BnbContent.Converter.Elites;

// ── Black-Ink Oracle (Act II elite) ───────────────────────────────────────────────────────────────────────
//
// An oracle of lacquered plaques whose face is interrupted by strips of censoring ink. Every second turn, once your
// hand is drawn, it CLASHES with you: you see the top card of your draw pile and may put it on the bottom first;
// then it reveals one of its plates (cost 0, 1 or 2) against the card now on top. Higher cost wins — you draw that
// card; lower loses — 5 damage; a tie is nothing.
//
// The player wears the rules (EncounterPassives.HeroOpeningStatuses), and with them the sight of the top card of
// their draw pile (Disclosure) — the decision is made looking at it. The plate is a fight-random roll, shown on
// the Oracle as the plate it last revealed: the Oracle has no deck of its own.
//
// REWORKED 2026-11-03 (user, Act II trial): Black Ink, the Devouring, Reveal, Decline and the citation are gone.
// REWORKED 2026-11-08 (CONTENT_REVIEW E8): the cost quiz ("guess what this card costs") became a Clash, so the
// player decides something instead of remembering a number. MTG: Clash (Lorwyn).
public static class BlackInkOracle
{
    public const string EnemyId = "black_ink_oracle";

    public const string TheOracleId = "the_black_ink_oracle";
    public const string OracleRulesId = "riddle_rules";

    public const int WrongDamage = 5;
    public const int Plates = 3;   // plates cost 0, 1 or 2
    public static string PlateId(int cost) => $"oracle_plate_{cost}";
    private static readonly string[] PlateNames = ["Plate of Nought", "Plate of One", "Plate of Two"];

    // Whether the last turn was a Clash: they alternate, from the first. The plate rolled is kept beside it.
    private static CounterId AskedCounter => new("oracle_asked");
    public static CounterId PlateCounter => new("oracle_plate");

    private static readonly ICombatantTargetSelector Self = CombatantTargetSelectors.Source;

    private const string ClashText =
        "Every second turn it Clashes: you see your top card and may put it on the bottom, then it reveals a plate "
        + "(cost 0-2). Your top card costs more: draw it. Less: take 5 damage.";

    public static IEnumerable<StatusData> Statuses() =>
    [
        Rule(TheOracleId, "The Black-Ink Oracle", ClashText, []),
        Rules(),
        .. Enumerable.Range(0, Plates).Select(cost => Rule(PlateId(cost), PlateNames[cost],
            $"The plate it revealed in the last Clash: cost {cost}.", [])),
    ];

    private static StatusData Rules()
    {
        var asked = E<CardsDrawnTriggeredEffectContext>.Counter(Self, AskedCounter);
        var top = new CardInOwnerZoneExpression<CardsDrawnTriggeredEffectContext>(Self, CardZone.DrawPile);
        var yours = E<CardsDrawnTriggeredEffectContext>.CostOf(top);
        var plate = E<CardsDrawnTriggeredEffectContext>.Counter(Self, PlateCounter);
        var it = E<CardsDrawnTriggeredEffectContext>.It;

        var clash = E<CardsDrawnTriggeredEffectContext>.Seq(
            // Option 0 keeps the card: a fight without a chooser leaves the pile as it is.
            new ChooseOptionsNode<CardsDrawnTriggeredEffectContext>(
                [E<CardsDrawnTriggeredEffectContext>.Nothing,
                 E<CardsDrawnTriggeredEffectContext>.Move(top, CardZone.DrawPile, ZonePlacement.Bottom)],
                ["keep it on top", "put it on the bottom"],
                count: 1,
                purpose: "Clash with the Oracle: your top card against its plate (cost 0-2), higher cost wins. "
                    + "Win: draw it. Lose: 5 damage. Keep your top card, or put it on the bottom first?"),
            E<CardsDrawnTriggeredEffectContext>.Set(Self, PlateCounter,
                new RandomBelowExpression<CardsDrawnTriggeredEffectContext>(Plates)),
            E<CardsDrawnTriggeredEffectContext>.Each(E<CardsDrawnTriggeredEffectContext>.Wearers(TheOracleId),
                new SequenceEffectNode<CardsDrawnTriggeredEffectContext>(
                [
                    .. Enumerable.Range(0, Plates).Select(cost => E<CardsDrawnTriggeredEffectContext>.Remove(it, PlateId(cost))),
                    .. Enumerable.Range(0, Plates).Select(cost => E<CardsDrawnTriggeredEffectContext>.If(
                        E<CardsDrawnTriggeredEffectContext>.Is(plate, ComparisonOperator.Equal, cost),
                        E<CardsDrawnTriggeredEffectContext>.Apply(it, PlateId(cost)))),
                ])),
            // Won: the card that won is drawn. Lost: 5 damage, the Oracle's, which no Strength of the player's touches.
            E<CardsDrawnTriggeredEffectContext>.If(
                E<CardsDrawnTriggeredEffectContext>.Is(yours, ComparisonOperator.Greater, plate),
                E<CardsDrawnTriggeredEffectContext>.Draw(1),
                E<CardsDrawnTriggeredEffectContext>.If(
                    E<CardsDrawnTriggeredEffectContext>.Is(yours, ComparisonOperator.Less, plate),
                    new DealDamageNode<CardsDrawnTriggeredEffectContext>(
                        Self, E<CardsDrawnTriggeredEffectContext>.C(WrongDamage), kind: DamageKind.DamageOverTime))));

        var program = new EffectProgram<CardsDrawnTriggeredEffectContext>(
            E<CardsDrawnTriggeredEffectContext>.If(E<CardsDrawnTriggeredEffectContext>.Present(TheOracleId),
                E<CardsDrawnTriggeredEffectContext>.If(
                    E<CardsDrawnTriggeredEffectContext>.Is(asked, ComparisonOperator.Equal, 0),
                    E<CardsDrawnTriggeredEffectContext>.Seq(
                        E<CardsDrawnTriggeredEffectContext>.Set(Self, AskedCounter, E<CardsDrawnTriggeredEffectContext>.C(1)),
                        E<CardsDrawnTriggeredEffectContext>.If(
                            E<CardsDrawnTriggeredEffectContext>.Is(
                                E<CardsDrawnTriggeredEffectContext>.InZone(Self, CardZone.DrawPile), ComparisonOperator.Greater, 0),
                            clash)),
                    E<CardsDrawnTriggeredEffectContext>.Set(Self, AskedCounter, E<CardsDrawnTriggeredEffectContext>.C(0)))));

        return Rule(OracleRulesId, "The Oracle's Clash", ClashText,
            [
                new StatusTriggerData("CardsDrawn", JsonSerializer.SerializeToElement(
                    TheOpeningHand.Only(program), CombatJson.CreateOptions<CardsDrawnTriggeredEffectContext>())),
            ]) with { Disclosure = new DisclosureData(DrawPileCards: 1) };
    }

    // Its intents are plain and authored in the source data.
    public static EffectProgram<EnemyActionContext>? Intent(string intentId) => null;

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
