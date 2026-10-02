using RogueDeck.Core.Combat;
using RogueDeck.Run;
using RogueDeck.Scenario.Authoring;
using static BnbContent.Converter.Cards.CardAuthoring;

namespace BnbContent.Converter.Events;

// GAMBLES (user, playtest feedback 2, G2: "ein paar langweilige events durch gamble events ersetzen, wo man rng
// entscheidungen mit high risk high reward bekommt"). Each one is a wager the player can see the odds of — the
// odds are written on the door — and whose outcome is a situation of its own (EventChoice.Outcomes), drawn from
// the run's RNG, so the seed repeats it and the player reads what happened before walking on.
public static class GambleEvents
{
    // ── Act I ─────────────────────────────────────────────────────────────────────────────────────────────

    public static BnbEvent FormLottery(ConversionPools pools) => Gamble(
        "form_lottery", "The Form Lottery",
        "A clerk behind a cage of wire sells tickets for a draw nobody has ever seen held. \"Thirty Gold. Every "
        + "form is a winner,\" he says, \"in principle.\"",
        "Buy a ticket (30 Gold) — 40 %: 120 Gold · 35 %: a Rare card · 25 %: nothing.",
        [Price(30)],
        Outcome("jackpot", 40, "The cage rattles. Your number was, against every regulation, drawn.", [Gold(120)]),
        Outcome("rare", 35, "Instead of money, the clerk hands over a form nobody else was allowed to keep.",
            [RareCard(pools, "form_lottery")]),
        Outcome("blank", 25, "\"Not a winner,\" says the clerk, \"yet.\" The ticket is filed under Hope.", []));

    public static BnbEvent StampWheel(ConversionPools pools) => Gamble(
        "stamp_wheel", "The Stamp Wheel",
        "A wheel of rubber stamps turns on a desk by itself. Whatever it lands on, it stamps — on the next thing "
        + "within reach. That would be you.",
        "Spin it — 30 %: a relic · 40 %: two cards improved · 30 %: lose 12 HP.",
        [],
        Outcome("relic", 30, "APPROVED. Something worth having slides out of the base of the wheel.",
            [new OfferRewardRunEffect(new RewardId("stamp_wheel:relic"), pools.NormalRelicOnTheCurve("stamp_wheel"), 1)
                { Kind = RewardKinds.Relic }]),
        Outcome("amended", 40, "AMENDED. Two of your procedures come back better than you filed them.",
            [new UpgradeCardsRunEffect(RunSelectors.DeckCards.Upgradable().Random(2))]),
        Outcome("rejected", 30, "REJECTED. The stamp comes down hard, and it comes down on your hand.",
            [new ApplyRunDamageRunEffect(12)]));

    // ── Act II ────────────────────────────────────────────────────────────────────────────────────────────

    public static BnbEvent DoubleOrNothing() => Gamble(
        "double_or_nothing", "Double or Nothing",
        "The archive's treasurer keeps a ledger with two columns, both titled \"Yours\". He offers to settle it "
        + "with a coin minted in a year the archive does not acknowledge.",
        "Wager 60 Gold — 50 %: win 150 Gold · 50 %: lose it.",
        [Price(60)],
        Outcome("heads", 50, "Heads. The treasurer writes your name in the first column, and then pays it.", [Gold(150)]),
        Outcome("tails", 50, "Tails. The treasurer writes your name in the second column. It means the same, "
            + "and he keeps the Gold.", []));

    public static BnbEvent AppealToChance(ConversionPools pools) => Gamble(
        "appeal_to_chance", "The Appeal to Chance",
        "A tribunal of one blindfolded clerk hears appeals by lot. Strike a document from your file and let the lot "
        + "decide what else goes with it.",
        "Strike a card of your choice — then 50 %: strike another of your choice · 50 %: a random card is rewritten.",
        [],
        Outcome("granted", 50, "Appeal granted. The clerk lets you strike a second document while the lot is warm.",
            [new RemoveCardsRunEffect(RunSelectors.DeckCards.ChooseByPlayer(1, "strike another card"))]),
        Outcome("remanded", 50, "Remanded. One of your documents comes back in a different hand entirely.",
            [new TransformCardsRunEffect(RunSelectors.DeckCards.Random(1), pools.TransformPool())]),
        effects: [new RemoveCardsRunEffect(RunSelectors.DeckCards.ChooseByPlayer(1, "strike a card from your deck"))]);

    // ── Act III ───────────────────────────────────────────────────────────────────────────────────────────

    public static BnbEvent NotarysDice() => Gamble(
        "notarys_dice", "The Notary's Dice",
        "Under a rowan, a notary throws dice that are also seals. \"Put something of yourself on the table,\" she "
        + "says, \"and the next three hearings go your way. Or theirs.\"",
        "Stake 8 HP — 50 %: the next 3 fights you start with 1 extra Energy · 50 %: the next 3 fights every enemy "
        + "starts with 2 Strength.",
        [Blood(8)],
        Outcome("sealed", 50, "The seals land face up. For three hearings, you will arrive early and prepared.",
            [Openings.ForFights(3, Energy_(1))]),
        Outcome("broken", 50, "The seals land face down. Word goes ahead of you: three hearings will be against you.",
            [Openings.ForFights(3, CombatNodeModel.ForEach("allEnemies",
                new CombatNodeModel("applyStatus", "iterationTarget", CombatAmountSpec.FromConst(2), StatusId: "strength")))]));

    // ── Act IV ────────────────────────────────────────────────────────────────────────────────────────────

    public static BnbEvent SealedOffer(ConversionPools pools) => Gamble(
        "sealed_offer", "The Sealed Offer",
        "Three envelopes on a basalt table, each sealed with the same cartouche. A scribe will let you take one, "
        + "and will not say what is inside any of them. He says it twice.",
        "Open an envelope — 35 %: a relic · 40 %: 150 Gold · 25 %: a curse and 50 Gold.",
        [],
        Outcome("relic", 35, "Inside is a deed to something old, and the thing itself, folded very small.",
            [new OfferRewardRunEffect(new RewardId("sealed_offer:relic"), pools.NormalRelicOnTheCurve("sealed_offer"), 1)
                { Kind = RewardKinds.Relic }]),
        Outcome("gold", 40, "Inside is a payment order, already honoured. The Gold is real.", [Gold(150)]),
        Outcome("curse", 25, "Inside is a form that has your name on it already, and fifty Gold for the trouble "
            + "of keeping it.", [new AddCardToDeckRunEffect(new CardDefinitionId("unsigned_form")), Gold(50)]));

    // ── the shape ─────────────────────────────────────────────────────────────────────────────────────────

    private sealed record GambleOutcome(string Id, int Weight, string Text, IReadOnlyList<IRunEffectRequest> Effects);

    private static GambleOutcome Outcome(string id, int weight, string text, IReadOnlyList<IRunEffectRequest> effects) =>
        new(id, weight, text, effects);

    // The door, the wager, and its outcomes: "start" offers the wager (its costs and its own effects first) or
    // walking away; the wager leads to one outcome by weight; the outcome pays out on Continue.
    private static BnbEvent Gamble(
        string id, string name, string text, string wager, IReadOnlyList<RunCost> costs,
        GambleOutcome first, GambleOutcome second, GambleOutcome? third = null,
        IReadOnlyList<IRunEffectRequest>? effects = null)
    {
        var outcomes = new[] { first, second, third }.OfType<GambleOutcome>().ToList();
        var situations = new List<EventSituation>
        {
            new("start", text,
            [
                new EventChoice("wager", effects ?? [], TextKey: wager, Costs: costs.Count > 0 ? costs : null,
                    Outcomes: [.. outcomes.Select(o => new EventOutcome($"outcome:{o.Id}", o.Weight))]),
                new EventChoice("walk_away", [], NextSituationId: "walked", TextKey: "Walk away."),
            ]),
            new("walked", "You leave the odds to someone else.", [new EventChoice("continue", [], TextKey: "Continue")]),
        };
        situations.AddRange(outcomes.Select(o => new EventSituation($"outcome:{o.Id}", o.Text,
            [new EventChoice("continue", o.Effects, TextKey: "Continue")])));
        return new BnbEvent(id, name, new EventScript("start", situations), Tags: ["gamble"]);
    }

    private static IRunEffectRequest Gold(int amount) => new ChangeResourceRunEffect(StandardRunIds.Gold, amount);

    private static RunCost Price(int gold) =>
        new(RunExpr.HasResource(StandardRunIds.Gold, gold), [new ChangeResourceRunEffect(StandardRunIds.Gold, -gold)]);

    // HP as a stake: never one that kills — the wager is only offered while the player can pay it and live.
    private static RunCost Blood(int hp) =>
        new(RunExpr.GreaterThan(RunExpr.CurrentHealth, RunExpr.Const(hp)), [new ApplyRunDamageRunEffect(hp)]);

    private static IRunEffectRequest RareCard(ConversionPools pools, string where) =>
        new OfferRewardRunEffect(new RewardId($"{where}:rare"), pools.CardRewardSource("rare", 3), 1)
            { Kind = RewardKinds.Card };
}
