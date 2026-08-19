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
            // The card's TYPE (action/spell/form/argument/curse/…) is emitted as a combat tag alongside its own
            // tags, so type-sequencing enemy passives (Wrong-Window Scribe, Triplicate Examiner) can read it via
            // cardsPlayedThisTurnWithTag / firstCardPlayedHasTag. Distinct so an explicit type tag isn't doubled.
            Tags = tags.Append(card.Type).Distinct().Select(tag => new TagId(tag)).ToArray(),
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

            // damage = amount (base, optional) + min(stacks of <status> × amount_per_stack, cap?)
            // Base + capped scaling covers "N damage, +X per <status> up to +Y" (e.g. Queue-Crier's Lost Your
            // Place). The stacks are read on the effect's target unless status_on names another seat (Blank-Line
            // Leech scales on its OWN Paperwork while hitting the player), and per_stacks groups them ("+2 for
            // every 2 Paperwork").
            "damage_per_status" => new CombatNodeModel("dealDamage", Sel(),
                DamagePerStatusAmount(where, effect, effect.StatusOn is { } seat
                    ? targets.TryGetValue(seat, out var readSelector)
                        ? readSelector
                        : throw new ConversionException(where, $"unmapped status_on '{seat}'")
                    : Sel())),

            // set_counter: write a per-fight track (Queue Position, …). relative (default true) adds; else sets.
            // With a cap, the add is rewritten as an absolute min(current + amount, cap) — tracks like Momentum
            // that fill to a ceiling.
            "set_counter" => effect.Cap is { } counterCap
                ? new CombatNodeModel("setCombatantCounter", Sel(),
                    CombatAmountSpec.Binary("min",
                        CombatAmountSpec.Binary("add",
                            CombatAmountSpec.Counter(Sel(),
                                effect.Counter ?? throw new ConversionException(where, "set_counter without counter")),
                            CombatAmountSpec.FromConst(Amount())),
                        CombatAmountSpec.FromConst(counterCap)),
                    CounterId: effect.Counter!,
                    Relative: false)
                : new CombatNodeModel("setCombatantCounter", Sel(), CombatAmountSpec.FromConst(Amount()),
                    CounterId: effect.Counter ?? throw new ConversionException(where, "set_counter without counter"),
                    Relative: effect.Relative ?? true),

            // damage = amount (base) + min(counter × amount_per_stack, cap?) — the counter counterpart of
            // damage_per_status, for tracks an enemy keeps on itself (Stolen Sand).
            "damage_per_counter" => new CombatNodeModel("dealDamage", Sel(),
                DamagePerCounterAmount(where, effect)),

            // countdown_step: bring an active countdown one closer, or START one at `amount` when none runs.
            // The Three Appointments' scheduling moves ("reduce its Appointment Due by 1; if no countdown
            // exists, establish N instead") — a conditional the flat effect list cannot express otherwise.
            "countdown_step" => CombatNodeModel.Conditional(
                new CombatConditionSpec("compare", Sel(), ValueKind: "counter",
                    Op: ComparisonOperator.Greater, Right: 0,
                    Id: effect.Counter ?? throw new ConversionException(where, "countdown_step without counter")),
                new CombatNodeModel("setCombatantCounter", Sel(), CombatAmountSpec.FromConst(-1),
                    CounterId: effect.Counter!, Relative: true),
                new CombatNodeModel("setCombatantCounter", Sel(), CombatAmountSpec.FromConst(Amount()),
                    CounterId: effect.Counter!, Relative: false)),

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

    // amount(base) + min(counter(owner) × amount_per_stack, cap). The counter is always read on the acting
    // enemy — a track it keeps about itself, unlike damage_per_status which reads a status off a combatant.
    private static CombatAmountSpec DamagePerCounterAmount(string where, BabEffect effect)
    {
        var scaled = CombatAmountSpec.Binary("mul",
            CombatAmountSpec.Counter("source",
                effect.Counter ?? throw new ConversionException(where, "damage_per_counter without counter")),
            CombatAmountSpec.FromConst(effect.AmountPerStack
                ?? throw new ConversionException(where, "damage_per_counter without amount_per_stack")));

        if (effect.Cap is { } cap)
            scaled = CombatAmountSpec.Binary("min", scaled, CombatAmountSpec.FromConst(cap));

        return effect.Amount is { } baseAmount && baseAmount != 0
            ? CombatAmountSpec.Binary("add", CombatAmountSpec.FromConst(baseAmount), scaled)
            : scaled;
    }

    // amount(base) + min(statusStacks(readSelector, status) ÷ per_stacks × amount_per_stack, cap).
    // Base, cap and per_stacks are optional; per_stacks divides first (whole-number), so 3 Paperwork counted
    // "for every 2" is one group, not one and a half.
    private static CombatAmountSpec DamagePerStatusAmount(string where, BabEffect effect, string readSelector)
    {
        var stacks = new CombatAmountSpec("statusStacks", SelectorKey: readSelector,
            ReadId: effect.Status ?? throw new ConversionException(where, "damage_per_status without status"));

        var groups = effect.PerStacks is { } per
            ? per > 0
                ? CombatAmountSpec.Binary("div", stacks, CombatAmountSpec.FromConst(per))
                : throw new ConversionException(where, "per_stacks must be greater than zero")
            : stacks;

        var perStack = CombatAmountSpec.Binary("mul", groups,
            CombatAmountSpec.FromConst(effect.AmountPerStack
                ?? throw new ConversionException(where, "damage_per_status without amount_per_stack")));

        var scaled = effect.Cap is { } cap
            ? CombatAmountSpec.Binary("min", perStack, CombatAmountSpec.FromConst(cap))
            : perStack;

        return effect.Amount is { } baseAmount && baseAmount != 0
            ? CombatAmountSpec.Binary("add", CombatAmountSpec.FromConst(baseAmount), scaled)
            : scaled;
    }
}
