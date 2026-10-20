using RogueDeck.Core.Combat;
using RogueDeck.Scenario.Authoring;

namespace BnbContent.Converter.Apprentice;

// The words a wand imprint is written in (sorcerers_apprentice_master.md §4): one builder per operation, one per
// Speak atom, so a card's WAND line reads like its text. The engine's WandImprint is the contract; this is only
// the shorthand.
public static class Imprints
{
    private static ICombatExpression<CardPlayContext, int> Const(int value) => new ConstantExpression<CardPlayContext>(value);

    public static ICombatExpression<CardPlayContext, int> Amount(CombatAmountSpec spec) =>
        CombatProgramModel.BuildAmount<CardPlayContext>(spec);

    public static ICombatExpression<CardPlayContext, bool> Condition(CombatConditionSpec spec) =>
        CombatProgramModel.BuildCondition<CardPlayContext>(spec);

    public static ICombatExpression<CardPlayContext, int> Wand(WandField field) => new WandValueExpression<CardPlayContext>(field);

    // ── Speak atoms ────────────────────────────────────────────────────────────────────────────────────────

    public static WandAtom Hit(int amount, int hits = 1, WandTargeting to = WandTargeting.Target) =>
        new() { Kind = WandAtomKind.Damage, Amount = Const(amount), Hits = hits, Targeting = to };

    public static WandAtom Hit(ICombatExpression<CardPlayContext, int> amount, WandTargeting to = WandTargeting.Target) =>
        new() { Kind = WandAtomKind.Damage, Amount = amount, Targeting = to };

    public static WandAtom Guard(int amount) => new() { Kind = WandAtomKind.Block, Amount = Const(amount) };

    public static WandAtom Guard(ICombatExpression<CardPlayContext, int> amount) => new() { Kind = WandAtomKind.Block, Amount = amount };

    public static WandAtom Wax(int stacks) => new() { Kind = WandAtomKind.WardWax, Amount = Const(stacks) };

    // A status on the target (or on yourself).
    public static WandAtom Put(string status, int stacks, WandTargeting to = WandTargeting.Target) =>
        new() { Kind = WandAtomKind.Status, Amount = Const(stacks), Status = new StatusDefinitionId(status), Targeting = to };

    public static WandAtom Put(string status, ICombatExpression<CardPlayContext, int> stacks, WandTargeting to = WandTargeting.Target) =>
        new() { Kind = WandAtomKind.Status, Amount = stacks, Status = new StatusDefinitionId(status), Targeting = to };

    public static WandAtom Gain(string status, int stacks) => Put(status, stacks, WandTargeting.Self);

    // Something the Speak does that no copy may take (draw, convert, cleanse): a plain program.
    public static WandAtom Do(CombatNodeModel program, bool afterCast = false) => new()
    {
        Kind = WandAtomKind.Program,
        Copyable = false,
        AfterCast = afterCast,
        Program = CombatProgramModel.Build<CardPlayContext>(program),
    };

    public static WandAtom Only(this WandAtom atom, WandPosition position) => atom with { Only = position };
    public static WandAtom When(this WandAtom atom, CombatConditionSpec condition) => atom with { When = Condition(condition) };
    public static WandAtom When(this WandAtom atom, ICombatExpression<CardPlayContext, bool> condition) => atom with { When = condition };
    public static WandAtom Plus(this WandAtom atom, int bonus, WandPosition at) => atom with { Bonus = bonus, BonusAt = at };
    public static WandAtom Private(this WandAtom atom) => atom with { Copyable = false };

    // ── operations ─────────────────────────────────────────────────────────────────────────────────────────

    public static WandImprint Speak(params WandAtom[] atoms) => new() { Operation = WandOperation.Speak, Atoms = atoms };

    public static WandImprint Heighten(int percent, int stacks = 1, WandFilter filter = WandFilter.Any, int strain = 0) =>
        new() { Operation = WandOperation.Heighten, Percent = percent, Amount = stacks, Filter = filter, Exhaustion = strain };

    public static WandImprint Contrast(int percent, int flowOtherwise) =>
        new() { Operation = WandOperation.Heighten, Rule = WandRule.Contrast, Percent = percent, Amount = flowOtherwise };

    public static WandImprint Brand(string status, int each, int cap) =>
        new() { Operation = WandOperation.Heighten, Rule = WandRule.Brand, Status = new StatusDefinitionId(status), Amount = each, Cap = cap };

    public static WandImprint Echo(int percent, WandFilter filter = WandFilter.Any, bool burns = false, params WandAtom[] fallback) =>
        new() { Operation = WandOperation.Echo, Percent = percent, Filter = filter, Burns = burns, Fallback = fallback };

    public static WandImprint Shape(WandRule rule, int percent = 0, int amount = 0, int cap = 0, string? status = null,
        bool burns = false, params WandAtom[] fallback) => new()
        {
            Operation = WandOperation.Shape,
            Rule = rule,
            Percent = percent,
            Amount = amount,
            Cap = cap,
            Status = status is null ? null : new StatusDefinitionId(status),
            Burns = burns,
            Fallback = fallback,
        };

    public static WandImprint Clause(WandRule rule, int percent = 0, int amount = 0, int amount2 = 0, int cap = 0) =>
        new() { Operation = WandOperation.Clause, Rule = rule, Percent = percent, Amount = amount, Amount2 = amount2, Cap = cap };

    public static WandImprint Order(WandRule rule, int percent = 0, int amount = 0) =>
        new() { Operation = WandOperation.Order, Rule = rule, Percent = percent, Amount = amount };

    public static WandImprint Keep(int percent, WandFilter filter = WandFilter.Any, WandStoreTrigger trigger = WandStoreTrigger.NextCast) =>
        new() { Operation = WandOperation.Keep, Percent = percent, Filter = filter, Trigger = trigger };

    public static WandImprint KeepLast(int percent, WandStoreTrigger trigger, params WandAtom[] fallback) =>
        new() { Operation = WandOperation.Keep, Rule = WandRule.StoreLast, Percent = percent, Trigger = trigger, Fallback = fallback };

    public static WandImprint AshTrace(int damage) =>
        new() { Operation = WandOperation.Keep, Rule = WandRule.AshTrace, Amount = damage, Heavy = false };

    public static WandImprint Ground(int relief) => new() { Operation = WandOperation.Ground, Amount = relief };

    public static WandImprint Ground(WandRule rule, int amount = 0, int amount2 = 0, int percent = 0, int cap = 0) =>
        new() { Operation = WandOperation.Ground, Rule = rule, Amount = amount, Amount2 = amount2, Percent = percent, Cap = cap };

    public static WandImprint Burn(int relief, params WandAtom[] atoms) =>
        new() { Operation = WandOperation.Burn, Amount = relief, Atoms = atoms, Burns = true };

    public static WandImprint Recall(int uses) => new() { Operation = WandOperation.Burn, Rule = WandRule.Recall, Amount = uses };

    public static WandImprint Stop(int relief) => new() { Operation = WandOperation.Stop, Amount = relief };

    public static WandImprint FinalWord(int damage, int kinds, int strain) =>
        new() { Operation = WandOperation.Stop, Rule = WandRule.FinalWord, Amount = damage, Cap = kinds, Amount2 = strain };

    // A Speak's own Ground: when another Speak follows it, or when a condition holds.
    public static WandImprint GroundingIfFollowed(this WandImprint speak, int relief) => speak with { GroundIfSpeakFollows = relief };

    public static WandImprint GroundingWhen(this WandImprint speak, int relief, ICombatExpression<CardPlayContext, bool> when) =>
        speak with { GroundAmount = relief, GroundWhen = when };
}
