using RogueDeck.Core.Combat;
using RogueDeck.Scenario.Authoring;
using static BnbContent.Converter.Cards.CardAuthoring;

namespace BnbContent.Converter.Cards;

// The small shapes the general pool (GENERAL_POOL_PLAN.md, 2026-11-08) is written in. The pool belongs to no
// character, so nothing here may lean on a character's words: Energy, cards, Block, Doubt, Liable, Ward Wax and
// the piles are the whole vocabulary.
public static class GeneralPoolShapes
{
    // One scratch counter for "how much was there" questions a condition cannot ask directly. Every use writes
    // it immediately before reading it.
    public static CounterId ScratchId => new("general_pool_scratch");

    public static CombatAmountSpec Scratch => new("counter", SelectorKey: You, CounterId: ScratchId.value);

    public static CombatNodeModel SetScratch(CombatAmountSpec amount) =>
        new("setCombatantCounter", You, amount, CounterId: ScratchId.value, Relative: false);

    public static CombatNodeModel IfScratch(ComparisonOperator op, int right, CombatNodeModel then,
        CombatNodeModel? otherwise = null) =>
        If(new CombatConditionSpec("compare", You, ValueKind: "counter", Op: op, Right: right, Id: ScratchId.value),
            then, otherwise);

    // Fatigue taken now bites at the start of the next turn and then fades — "next turn, 1 less Energy".
    public const string Fatigue = "fatigue";

    // Pledge: the card may be played without enough Energy (engine tag); the engine writes the unpaid part to
    // this counter on the player before the card's program runs.
    public static readonly string PledgeTag = StandardCombatIds.PledgeTag.value;
    public static readonly string PledgeShortfall = StandardCombatIds.PledgeShortfallCounter.value;

    public static CombatAmountSpec Const(int value) => CombatAmountSpec.FromConst(value);

    public static CombatAmountSpec Minus(CombatAmountSpec a, CombatAmountSpec b) => CombatAmountSpec.Binary("sub", a, b);

    public static CombatAmountSpec Max(CombatAmountSpec a, CombatAmountSpec b) => CombatAmountSpec.Binary("max", a, b);

    // "Gain N Held Energy."
    public static CombatNodeModel Hold(int amount) => Apply(HeldEnergy.Id, amount, You);

    // "Gain N Energy." Where a pool has a ceiling (HeldEnergy), what does not fit is held, not lost: the pool is
    // read before and after the gain, and whatever did not arrive becomes Held Energy. An uncapped pool simply
    // takes it all.
    public static CombatNodeModel GainEnergy(int amount) => GainEnergy(Const(amount));

    public static CombatNodeModel GainEnergy(CombatAmountSpec amount) =>
        Seq(
            new CombatNodeModel("setCombatantCounter", You, amount, CounterId: EnergyAsked.value, Relative: false),
            new CombatNodeModel("setCombatantCounter", You, CurrentEnergy, CounterId: EnergyBefore.value, Relative: false),
            new CombatNodeModel("gainResource", You, Counter(EnergyAsked), Energy.value),
            SetScratch(Minus(Counter(EnergyAsked), Minus(CurrentEnergy, Counter(EnergyBefore)))),
            IfScratch(ComparisonOperator.GreaterOrEqual, 1, Apply(HeldEnergy.Id, Scratch, You)));

    private static CounterId EnergyAsked => new("general_pool_energy_asked");
    private static CounterId EnergyBefore => new("general_pool_energy_before");

    private static CombatAmountSpec Counter(CounterId id) => new("counter", SelectorKey: You, CounterId: id.value);

    private static CombatAmountSpec CurrentEnergy => new("currentResource", SelectorKey: You, ReadId: Energy.value);

    public static CombatAmountSpec InZone(CardZone zone) => CardsInZone(zone);

    public static CombatAmountSpec PlayedThisTurn => new("cardsPlayedThisTurn", SelectorKey: You);

    // "Deal N damage to a random enemy."
    public static CombatNodeModel DamageRandomEnemy(int amount) =>
        new("randomTargets", AllEnemies, Const(1),
            Children: [Damage(amount, "iterationTarget")]);

    public static CombatNodeModel DamageAll(int amount) => Damage(amount, AllEnemies);

    public static CombatNodeModel ApplyAll(string status, int stacks) => Apply(status, stacks, AllEnemies);

    // ── Unread ──────────────────────────────────────────────────────────────────────────────────────────────
    //
    // "Unread:" happens when a CARD EFFECT discards or exhausts the card from your hand — never at the end of
    // the turn (decision 2026-11-08, like Madness). The card carries this tag; the rule that hears the move
    // lives on the Applicant (GeneralPoolRules) and looks the effect up by the card's own id tag.
    public const string UnreadTag = "unread";

    public static string UnreadIdTag(string cardId) => "unread:" + cardId;

    public static IReadOnlyList<string> Unread(string cardId, params string[] more) =>
        [UnreadTag, UnreadIdTag(cardId), .. more];
}
