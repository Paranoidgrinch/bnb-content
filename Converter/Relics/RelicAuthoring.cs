using RogueDeck.Core.Combat;
using RogueDeck.Run;
using RogueDeck.Scenario.Authoring;
using BnbContent.Converter.Cards;

namespace BnbContent.Converter.Relics;

// Shared shape and builders for the final relic pools.
//
// A relic has two faces. Between fights it is a set of RUN programs — gold, healing, upgrades, things that
// happen when a node is entered or a combat resolves. Inside a fight it is a RULE, and the way to put a rule
// into every fight is to hand the player a hidden status when the fight opens: the status carries the rule,
// exactly as a Rite card's status does. So `Combat(...)` takes the same StatusData the card pools build, and
// the relic's run program is just "apply it at the start of every combat".
//
// The four pools never mix. Which pool a relic belongs to decides where it can be found, and the design is
// explicit that a Boss or Event relic must never turn up in a shop or a treasure chest.
public static class RelicAuthoring
{
    public enum Pool { Normal, Shop, Event, Boss }

    public enum Rarity { Common, Uncommon, Rare, Boss, Shop, Event }

    // Who may be offered it. The Bureaucrat-specific ones are only eligible while that character is played.
    public enum Eligibility { General, Bureaucrat }

    public sealed record BnbRelic(
        string Id,
        string Name,
        string Text,
        Pool Pool,
        Rarity Rarity = Rarity.Common,
        Eligibility Eligibility = Eligibility.General,
        // What happens the moment it is picked up (gold, healing, a card removed…). The engine has no
        // per-relic pickup hook, so these are bundled into every place that GRANTS the relic.
        IReadOnlyList<IRunEffectRequest>? Pickup = null,
        // What it does between fights.
        IReadOnlyList<ITriggeredRunEffectDefinition>? RunPrograms = null,
        // The rule it puts into every fight, as the status that carries it.
        StatusData? CombatRule = null,
        // Which Act's boss gives it (Boss pool only), for the source index.
        string? Source = null)
    {
        public RelicData Compile() => new()
        {
            Id = Id,
            DisplayName = Name,
            RunPrograms = CombatRule is { } rule
                ? [.. RunPrograms ?? [], Converter.Openings.EveryCombat(
                    new CombatNodeModel("applyStatus", "source", CombatAmountSpec.FromConst(1),
                        StatusId: rule.Id))]
                : RunPrograms ?? [],
        };
    }

    // ── builders ──────────────────────────────────────────────────────────────────────────────────────────

    public static BnbRelic Normal(
        string id, string name, Rarity rarity, string text,
        Eligibility eligibility = Eligibility.General,
        IReadOnlyList<IRunEffectRequest>? pickup = null,
        IReadOnlyList<ITriggeredRunEffectDefinition>? runPrograms = null,
        StatusData? combatRule = null) =>
        new(id, name, text, Pool.Normal, rarity, eligibility, pickup, runPrograms, combatRule);

    public static BnbRelic Shop(
        string id, string name, string text,
        Eligibility eligibility = Eligibility.General,
        IReadOnlyList<IRunEffectRequest>? pickup = null,
        IReadOnlyList<ITriggeredRunEffectDefinition>? runPrograms = null,
        StatusData? combatRule = null) =>
        new(id, name, text, Pool.Shop, Rarity.Shop, eligibility, pickup, runPrograms, combatRule);

    public static BnbRelic Event(
        string id, string name, string source, string text,
        IReadOnlyList<IRunEffectRequest>? pickup = null,
        IReadOnlyList<ITriggeredRunEffectDefinition>? runPrograms = null,
        StatusData? combatRule = null) =>
        new(id, name, text, Pool.Event, Rarity.Event, Eligibility.General, pickup, runPrograms, combatRule,
            Source: source);

    public static BnbRelic Boss(
        string id, string name, string source, string text,
        IReadOnlyList<IRunEffectRequest>? pickup = null,
        IReadOnlyList<ITriggeredRunEffectDefinition>? runPrograms = null,
        StatusData? combatRule = null) =>
        new(id, name, text, Pool.Boss, Rarity.Boss, Eligibility.General, pickup, runPrograms, combatRule,
            Source: source);

    // ── run-program shorthands ────────────────────────────────────────────────────────────────────────────

    public static ITriggeredRunEffectDefinition AfterEveryVictory(params IRunEffectRequest[] effects) =>
        RunPrograms.When<CombatResolvedRunEvent>(
            new EventBoolValueExpression(RunEventFields.CombatVictory), effects);

    public static ITriggeredRunEffectDefinition OnPurchase(params IRunEffectRequest[] effects) =>
        RunPrograms.On<ShopItemPurchasedRunEvent>(effects);

    public static ITriggeredRunEffectDefinition OnEveryNode(params IRunEffectRequest[] effects) =>
        RunPrograms.On<NodeEnteredRunEvent>(effects);

    public static IRunEffectRequest Gold(int amount) =>
        new ChangeResourceRunEffect(StandardRunIds.Gold, amount);

    public static IRunEffectRequest Heal(int amount) => new HealRunEffect(amount);

    public static IRunEffectRequest MaxHealth(int amount) => new ChangeMaxHealthRunEffect(amount);

    // ── the combat rule a relic installs ──────────────────────────────────────────────────────────────────

    // A relic's in-combat rule, as the status that carries it. Neutral and unremarkable on purpose: it is the
    // relic being present, not something done to the player, so nothing that cleanses or refuses statuses
    // should touch it.
    public static StatusData Rule(
        string id, string name, string description, IReadOnlyList<StatusTriggerData> triggers,
        IReadOnlyList<PassiveModifierData>? passives = null,
        IReadOnlyList<string>? tags = null) => new()
        {
            Id = id,
            NameKey = name,
            DescriptionKey = description,
            Polarity = StatusPolarity.Neutral,
            StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
            UsesStacks = true,
            Tags = tags ?? [],
            PassiveModifiers = passives ?? [],
            Triggers = triggers,
        };
}
