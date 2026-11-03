using RogueDeck.Core.Combat;
using RogueDeck.Scenario.Authoring;

namespace BnbContent.Converter;

// Act III boss 3 — **Grandmother Clause** (350 HP).
//
// Every turn she presses a gift on you, and her gifts are not kind. Play it and she loses 6 HP — she was so hoping
// you would. Leave it in your hand at the end of the turn and you break her law. From half her health her gifts
// cost 1 Energy.
//
//   Thorned Shawl  — lose 5 HP, draw 2 cards.
//   Heavy Pie      — gain 2 Energy, gain 2 Doubt.
//   Lavender Sachet — gain 12 Block, discard 2 cards.
//
// REWORKED 2026-11-03 (user, Act III pass): the Courtesies, the House Rules, the accepted promises and the
// Wergild are gone.
public static partial class ActThree
{
    public const string GrandmotherEnemyId = "grandmother_clause";
    public const string GrandmotherId = "the_courtesies";
    public const string GrandmotherHalfId = "grandmother_insists";
    public const string GrandmotherGiftTag = "grandmother_gift";

    public const int GiftPlayedHealth = 6;

    private static ICombatantTargetSelector Grandmother { get; } = Elite(GrandmotherId);

    private static readonly string[] GrandmotherGifts = ["thorned_shawl", "heavy_pie", "lavender_sachet"];
    private static string Dear(string gift) => $"{gift}_dear";

    private static IEnumerable<StatusData> GrandmotherStatuses() =>
    [
        TheCourtesies(),
        Marker(GrandmotherHalfId, "She Insists", "Her gifts cost 1 Energy now."),
    ];

    // Her gifts, and their dearer twins for the second half of the fight.
    public static IEnumerable<CardData> GrandmotherGiftCards()
    {
        IEffectNode<CardPlayContext> Thanked(params IEffectNode<CardPlayContext>[] steps) => Seq(
        [
            .. steps,
            new SetHealthNode<CardPlayContext>(Grandmother,
                new SubtractExpression<CardPlayContext>(
                    new CombatantCurrentHealthExpression<CardPlayContext>(Grandmother), N(GiftPlayedHealth))),
        ]);
        var shawl = Thanked(LoseHealth(5), new DrawCardsNode<CardPlayContext>(CombatantTargetSelectors.Source, N(2)));
        var pie = Thanked(
            new GainResourceNode<CardPlayContext>(CombatantTargetSelectors.Source, StandardCombatIds.EnergyResource, N(2)),
            new ApplyStatusNode<CardPlayContext>(CombatantTargetSelectors.Source, new StatusDefinitionId(Cards.Keywords.Doubt), N(2)));
        var sachet = Thanked(new GainBlockNode<CardPlayContext>(CombatantTargetSelectors.Source, N(12)), DiscardChosen(2));
        foreach (var cost in new[] { 0, 1 })
        {
            string Id(string gift) => cost == 0 ? gift : Dear(gift);
            yield return Gift(Id("thorned_shawl"), "Thorned Shawl", "Lose 5 HP. Draw 2 cards. Grandmother loses 6 HP.", GrandmotherGiftTag, shawl, cost);
            yield return Gift(Id("heavy_pie"), "Heavy Pie", "Gain 2 Energy. Gain 2 Doubt. Grandmother loses 6 HP.", GrandmotherGiftTag, pie, cost);
            yield return Gift(Id("lavender_sachet"), "Lavender Sachet", "Gain 12 Block. Discard 2 cards. Grandmother loses 6 HP.", GrandmotherGiftTag, sachet, cost);
        }
    }

    // The giving is the Witchling's, with two baskets: her plain gifts, or — once she insists — the dear ones.
    private static StatusData TheCourtesies()
    {
        var give = new ConditionalEffectNode<TurnStartedTriggeredEffectContext>(
            Wearing<TurnStartedTriggeredEffectContext>(Grandmother, GrandmotherHalfId),
            GiveAGift(Grandmother, [.. GrandmotherGifts.Select(Dear)]),
            @else: GiveAGift(Grandmother, GrandmotherGifts));
        return Rule(GrandmotherId, "The Courtesies",
            "Each turn she gives you a gift: play it and she loses 6 HP; keep it and you break her law. From half health they cost 1.",
            [
                On("TurnStarted", give, StatusTriggerScope.Anywhere),
                On("TurnEnded", AGiftRefused(Grandmother, GrandmotherGiftTag), StatusTriggerScope.Anywhere),
                On("DamageTaken", OnceBelow(50, GrandmotherHalfId, new NoOpEffectNode<DamageReceivedTriggeredEffectContext>())),
            ]);
    }

    private static EffectProgram<EnemyActionContext>? GrandmotherIntent(string enemyId, string intentId) => null;
}
