using RogueDeck.Core.Combat;
using RogueDeck.Scenario.Authoring;

namespace BnbContent.Converter;

// Original card JSON → engine CardData. The original's card types (action/form/argument/curse) and
// most tags have no rules semantics — they ride along as tags/presentation. Rules semantics ported:
// the effect list (below), the played-card exhaust tags, and the "unplayable" tag (same keyword in
// both games). "_plus" ids become "<base>+" so the engine's UpgradeSuffix deck mapper finds them.
public static class CardMapper
{
    // The original engine exhausts a played card carrying any of these tags (bab/combat/deck.py).
    private static readonly string[] ExhaustWhenPlayedTags = ["exhaust", "vanish", "single_use", "temporary"];

    public static string MapCardId(string babId) =>
        babId.EndsWith("_plus", StringComparison.Ordinal)
            ? babId[..^"_plus".Length] + "+"
            : babId;

    public static CardData Map(BabCard card)
    {
        var where = $"card '{card.Id}'";
        var tags = card.Tags ?? [];
        return new CardData
        {
            Id = MapCardId(card.Id),
            NameKey = card.Name,
            Costs = card.Cost == 0
                ? []
                : [new ResourceCost(StandardCombatIds.EnergyResource, card.Cost)],
            Tags = tags.Select(tag => new TagId(tag)).ToArray(),
            PlayedCardDestinationZone = tags.Any(ExhaustWhenPlayedTags.Contains)
                ? CardZone.ExhaustPile
                : CardZone.DiscardPile,
            Program = CombatProgramModel.Build<CardPlayContext>(
                EffectMapper.MapAll(where, card.Effects, EffectMapper.CardTargets)),
        };
    }
}

// The shared effect-DSL table: original effect entries → combat program nodes. Cards and enemy
// intents use the same vocabulary with different target words, so the selector table is a parameter.
public static class EffectMapper
{
    // Card effects speak from the player's seat…
    public static readonly IReadOnlyDictionary<string, string> CardTargets = new Dictionary<string, string>
    {
        ["enemy"] = "eventTarget",   // the chosen target
        ["all_enemies"] = "allEnemies",
        ["self"] = "source",
    };

    // …enemy intent effects from the enemy's.
    public static readonly IReadOnlyDictionary<string, string> EnemyTargets = new Dictionary<string, string>
    {
        ["player"] = "eventTarget",  // the enemy's chosen target: the hero
        ["owner"] = "source",
        ["self"] = "source",
        ["all_enemies"] = "allAllies", // the enemy's own side
    };

    public static CombatNodeModel MapAll(
        string where, IReadOnlyList<BabEffect>? effects, IReadOnlyDictionary<string, string> targets)
    {
        var mapped = (effects ?? []).Select((effect, index) => Map($"{where} effect[{index}]", effect, targets)).ToList();
        return mapped.Count switch
        {
            // Curses/junk legitimately do nothing when played (or are unplayable) — an empty sequence.
            0 => CombatNodeModel.Sequence([]),
            1 => mapped[0],
            _ => CombatNodeModel.Sequence(mapped),
        };
    }

    public static CombatNodeModel Map(
        string where, BabEffect effect, IReadOnlyDictionary<string, string> targets)
    {
        string Sel() => effect.Target is null
            ? throw new ConversionException(where, $"'{effect.Type}' is missing its target")
            : targets.TryGetValue(effect.Target, out var selector)
                ? selector
                : throw new ConversionException(where, $"unmapped target '{effect.Target}'");
        int Amount() => effect.Amount
            ?? throw new ConversionException(where, $"'{effect.Type}' is missing its amount");

        return effect.Type switch
        {
            "deal_damage" => new CombatNodeModel("dealDamage", Sel(), CombatAmountSpec.FromConst(Amount())),

            "gain_block" => new CombatNodeModel("gainBlock", Sel(), CombatAmountSpec.FromConst(Amount())),

            "apply_status" => new CombatNodeModel("applyStatus", Sel(), CombatAmountSpec.FromConst(Amount()),
                StatusId: effect.Status ?? throw new ConversionException(where, "apply_status without status")),

            "draw_cards" => new CombatNodeModel("drawCards", "source", CombatAmountSpec.FromConst(Amount())),

            // damage = stacks of <status> on the target × amount_per_stack
            "damage_per_status" => new CombatNodeModel("dealDamage", Sel(), CombatAmountSpec.Binary("mul",
                new CombatAmountSpec("statusStacks", SelectorKey: Sel(),
                    ReadId: effect.Status ?? throw new ConversionException(where, "damage_per_status without status")),
                CombatAmountSpec.FromConst(effect.AmountPerStack
                    ?? throw new ConversionException(where, "damage_per_status without amount_per_stack")))),

            "create_card" => new CombatNodeModel("createCardInstance", "source",
                CombatAmountSpec.FromConst(effect.Copies ?? 1),
                ToDefinition: CardMapper.MapCardId(effect.CardId
                    ?? throw new ConversionException(where, "create_card without card_id")),
                ToZone: effect.Destination switch
                {
                    "hand" => CardZone.Hand,
                    "discard_pile" or null => CardZone.DiscardPile,
                    "draw_pile" => CardZone.DrawPile,
                    var other => throw new ConversionException(where, $"unmapped create_card destination '{other}'"),
                }),

            "gain_resource" => effect.Resource == "energy"
                ? new CombatNodeModel("gainResource", "source", CombatAmountSpec.FromConst(Amount()),
                    StandardCombatIds.EnergyResource.value)
                : throw new ConversionException(where, $"unmapped resource '{effect.Resource}'"),

            "exhaust_cards_by_tag" => CombatNodeModel.ForEachCard("source", CardZone.Hand,
                new CombatNodeModel("moveCardToZone", ToZone: CardZone.ExhaustPile,
                    Card: new CombatCardSpec("iterated")),
                tag: effect.Tag ?? throw new ConversionException(where, "exhaust_cards_by_tag without tag"),
                takeFirst: effect.Amount),

            "gain_strength" => new CombatNodeModel("applyStatus", Sel(), CombatAmountSpec.FromConst(Amount()),
                StatusId: "strength"),

            var other => throw new ConversionException(where, $"unmapped effect type '{other}'"),
        };
    }
}
