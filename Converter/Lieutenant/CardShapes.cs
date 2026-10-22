using RogueDeck.Core.Combat;
using RogueDeck.Scenario.Authoring;
using static BnbContent.Converter.Cards.CardAuthoring;

namespace BnbContent.Converter.Lieutenant;

// The pieces a Lieutenant card is written in (lieutenant_master.md §2): issuing a promise, paying with Authority or
// Strength, and the questions her cards ask of the table.
internal static class CardShapes
{
    public const string Strength = LieutenantKeywords.Strength;
    public const string Authority = LieutenantKeywords.Authority;
    public const string OpenGuard = LieutenantKeywords.OpenGuard;

    private static CombatNodeModel Clear(string status, string on) => new("removeStatus", on, StatusId: status);

    // An Order is issued LAST, after everything else the card does (§2.3); a second copy replaces the first.
    public static CombatNodeModel Issue(OrderSpec order) =>
        Seq(Clear(order.Id, You), Reset(Obligations.Armed(order.Id), You), Apply(order.Id, 1, You));

    // A Challenge lands on a target that is still standing once the card's damage is done (§2.4).
    public static CombatNodeModel Issue(ChallengeSpec challenge, string on = Target) =>
        If(new CombatConditionSpec("isAlive", on), Seq(
            Clear(challenge.Id, on), Reset(Obligations.Armed(challenge.Id), on), Reset(Obligations.Progress(challenge.Id), on),
            Reset(Obligations.Grace(challenge.Id), on), Reset(Obligations.Remanded(challenge.Id), on),
            Apply(challenge.Id, 1, on)));

    private static CombatNodeModel Reset(string counter, string on) =>
        new("setCombatantCounter", on, CombatAmountSpec.FromConst(0), CounterId: counter, Relative: false);

    // Gain Authority up to the cap; the rest is lost (§2.2).
    public static CombatNodeModel GainAuthority(int amount) =>
        If(new CombatConditionSpec("compare", You, ValueKind: "statusStacks", Op: ComparisonOperator.Less,
                Right: LieutenantKeywords.AuthorityCap, Id: Authority),
            Apply(Authority, AtMost(CombatAmountSpec.Binary("sub",
                CombatAmountSpec.FromConst(LieutenantKeywords.AuthorityCap), Stacks(Authority, You)), amount), You));

    public static CombatConditionSpec Has(string status, int atLeast = 1, string on = You) => HasStacks(status, atLeast, on);

    // "You may spend N Authority to …" — offered only when she has it (L10).
    public static CombatNodeModel MaySpendAuthority(int cost, string offer, CombatNodeModel then, CombatNodeModel? otherwise = null) =>
        If(Has(Authority, cost),
            CombatNodeModel.ChooseOptions(1, [$"Spend {cost} Authority: {offer}", "Keep the Authority"],
                [Seq(Remove(Authority, cost, You), Apply(LieutenantKeywords.AuthorityPulse, cost, You), then),
                 otherwise ?? Seq()],
                "Authority"),
            otherwise ?? Seq());

    // "You may spend N Strength to …" — positive Strength only (L9). What was really paid is written down for the
    // cards and relics that read it (LieutenantKeywords.StrengthPulse).
    public static CombatNodeModel MaySpendStrength(int cost, string offer, CombatNodeModel then, CombatNodeModel? otherwise = null) =>
        If(Has(Strength, cost),
            CombatNodeModel.ChooseOptions(1, [$"Spend {cost} Strength: {offer}", "Keep the Strength"],
                [Seq(PayStrength(cost), then), otherwise ?? Seq()],
                "Strength"),
            otherwise ?? Seq());

    // A card program cannot announce; it leaves a pulse, which her standing status turns into the announcement.
    public static CombatNodeModel PayStrength(int cost) =>
        Seq(Remove(Strength, cost, You), Apply(LieutenantKeywords.StrengthPulse, cost, You));

    // "Your next N Deeds this turn cost … less": the status keeps count, and the hand is priced at once.
    public static CombatNodeModel Discount(string status, string tag, int off, int uses) => Seq(
        Apply(status, uses, You),
        CombatNodeModel.ForEachCard(You, CardZone.Hand,
            new CombatNodeModel("setCardInstanceMarkCounter", You, CombatAmountSpec.FromConst(-off),
                Card: new CombatCardSpec("iterated"), CounterId: StandardCombatIds.CardCostDeltaCounter.value),
            tag: tag));

    // "If the target is Challenged" — it carries any of the Challenges.
    public static CombatConditionSpec Challenged(string on = Target) =>
        new("compare", You, ValueKind: "counter", Op: ComparisonOperator.Greater, Right: 0, Id: Scratch);

    public static CombatNodeModel IfChallenged(CombatNodeModel then, CombatNodeModel? otherwise = null, string on = Target) =>
        Seq(SetScratch(Obligations.ChallengedAmount(on)), If(Challenged(on), then, otherwise));

    // A scratch number on her, for questions a condition cannot ask directly.
    public const string Scratch = "lt_scratch";

    public static CombatNodeModel SetScratch(CombatAmountSpec amount) =>
        new("setCombatantCounter", You, amount, CounterId: Scratch, Relative: false);

    public static CombatConditionSpec ScratchAtLeast(int n) =>
        new("compare", You, ValueKind: "counter", Op: ComparisonOperator.GreaterOrEqual, Right: n, Id: Scratch);
}
