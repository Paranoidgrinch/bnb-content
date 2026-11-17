using RogueDeck.Core.Combat;
using RogueDeck.Run;
using RogueDeck.Scenario.Authoring;
using static BnbContent.Converter.Relics.RelicAuthoring;

namespace BnbContent.Converter.Relics;

// ── THE RUN MODIFIERS, AS RELICS (META_PROGRESSION_PLAN.md §3, user 2026-11-17) ───────────────────────────────
// Shells buy run modifiers in the manner of Risk of Rain 2's artifacts, and a run plays with the ones the player
// switched on. What a modifier DOES is written here, as a relic "mod_<id>" the run starts with: the frontend
// (bnb-godot Shells.ForRun) puts the armed ones among the starting relics, and the relic bar shows them — the
// run wears its own terms. A modifier whose relic is not defined here is shown in the shop but cannot be bought.
//
// The relics sit in the Event pool and nowhere else: no chest, shelf, boss or event ever offers one (like the
// trial's Infirmary). What happens "at the start of the run" rides on RunStarted, which a resumed run never
// raises again — a starting relic's pickup effects are not run by the engine.
public static class ModifierRelics
{
    public const string Prefix = "mod_";
    private const string Source = "Run modifier";

    public static IReadOnlyList<BnbRelic> All(IEnumerable<BnbRelic> normalRelics) =>
    [
        Overtime(),
        InheritedDebt(),
        GlassSeal(),
        SigningBonus(normalRelics),
    ];

    public static IReadOnlyList<StatusData> Statuses() => [GlassSealRule];

    private static ITriggeredRunEffectDefinition AtRunStart(params IRunEffectRequest[] effects) =>
        RunPrograms.On<RunStartedRunEvent>(effects);

    // Bad, +2: enemies start every fight with 1 Strength per act. One program per act, each gated on its act.
    private static BnbRelic Overtime() => Event(Prefix + "overtime", "Overtime", Source,
        "Run modifier. Enemies start every fight with 1 Strength per act.",
        runPrograms:
        [
            .. Enumerable.Range(1, 5).Select(act => RunPrograms.When<NodeEnteredRunEvent>(
                new AndExpression(
                    new EventBoolValueExpression(RunEventFields.NodeIsCombat),
                    new RunComparisonExpression(new ActNumberExpression(), RunComparisonOperator.Equal, new RunConstantExpression(act))),
                Openings.NextCombat(new CombatNodeModel("applyStatus", "allEnemies",
                    CombatAmountSpec.FromConst(act), StatusId: "strength")))),
        ]);

    // Bad, +1: the run opens with two curses in the deck (everyone starts with no gold already).
    public const string Curse = "tomb_curse";

    private static BnbRelic InheritedDebt() => Event(Prefix + "inherited_debt", "Inherited Debt", Source,
        "Run modifier. You started with two curses in your deck.",
        runPrograms: [AtRunStart(
            new AddCardToDeckRunEffect(new CardDefinitionId(Curse)),
            new AddCardToDeckRunEffect(new CardDefinitionId(Curse)))]);

    // Different, ±0: double damage dealt, half the max HP (taken off at the start, rounded so the hero keeps
    // the larger half).
    public const string GlassSealRuleId = "mod_glass_seal_rule";

    private static readonly StatusData GlassSealRule = Rule(GlassSealRuleId, "Glass Seal",
        "Run modifier: you deal double damage.", [],
        passives: [new PassiveModifierData(PassiveModifierPipeline.DamageDealt, PassiveModifierOperation.ScalePercent, 200, RestrictDamageKind: null)]);

    private static BnbRelic GlassSeal() => Event(Prefix + "glass_seal", "Glass Seal", Source,
        "Run modifier. You deal double damage, but have half your max HP.",
        runPrograms: [AtRunStart(new RepeatRunEffect(
            new DivideExpression(new MaxHealthExpression(), new RunConstantExpression(2)),
            [new ChangeMaxHealthRunEffect(-1)]))],
        combatRule: GlassSealRule);

    // Good, −1: one random rare relic from the general pool, handed over as the run starts.
    private static BnbRelic SigningBonus(IEnumerable<BnbRelic> normalRelics)
    {
        var rares = normalRelics.Where(r => r.Rarity == Rarity.Rare && r.Eligibility == Eligibility.General).ToList();
        return Event(Prefix + "signing_bonus", "Signing Bonus", Source,
            "Run modifier. You started with a random rare relic.",
            runPrograms: [AtRunStart(new DrawEffectsRunEffect(new RunPool<IReadOnlyList<IRunEffectRequest>>(
                [.. rares.Select(r => new RunPool<IReadOnlyList<IRunEffectRequest>>.Entry(
                    [new AddRelicByIdRunEffect(new RelicId(r.Id)), .. r.Pickup ?? []], 1))])))]);
    }
}
