using RogueDeck.Core.Combat;
using RogueDeck.Run;
using static BnbContent.Converter.Relics.RelicAuthoring;

namespace BnbContent.Converter.Relics;

// The 24 Shop relics, from `source-data/design/BnB_Final_Relics_Master_PostAudit.md` §4. They are bought, never
// found: the Shop pool stocks the shop's own relic shelf and the three shop-like events, and never turns up in
// a treasure chest or a combat reward.
//
// Almost all of them are ECONOMY, and economy relics are not reactions. A discount is not something that
// happens to you at a moment you might miss — it is simply true of the shelf while the relic is worn, so the
// shop ASKS what the player is carrying as it prices and stocks itself. That is why most of these relics carry
// price rules, stock grants, services, credit or reward rules rather than run programs; the run programs are
// only for the things that really are moments (a purchase, a victory, walking into a shop).
//
// "The first time each Act" is written as a run FLAG. The blueprint is one Act, so a flag that is set and never
// cleared is exactly right today; when Acts II–V arrive, the act boundary has to clear them — see ADAPTATIONS.
public static class ShopRelics
{
    // ── the vocabulary these relics share ─────────────────────────────────────────────────────────────────

    // Banked things that are NOT Gold: they cannot be lost, they are not Gold spent, and they buy only what
    // they say. Modelling them as their own resources keeps all of that true by construction.
    public static readonly RunResourceId Waiver = new("waiver");
    public static readonly RunResourceId ArchiveVoucher = new("archive_voucher");
    public static readonly RunResourceId Punch = new("punch");

    public static readonly RunCounterId Debt = new("debt");
    public static readonly RunCounterId Receipts = new("receipts");
    public static readonly RunCounterId WarrantyValue = new("warranty_value");

    // What the shop's shelves are called and how its stock is labelled. A price rule finds nothing unless the
    // shelf says what it is holding — see ShopTemplate, which stamps these.
    public const string CardShelf = "cards";
    public const string RelicShelf = "relics";
    public const string NormalRelic = "normal";
    public const string Removal = "removal";
    public const string Extra = "extra";

    public static IReadOnlyList<BnbRelic> All() =>
    [
        // ── 1 ─────────────────────────────────────────────────────────────────────────────────────────────
        // The Appraised card pays whether you take it or leave the whole reward.
        Shop("pawnbrokers_loupe", "Pawnbroker's Loupe",
            "Whenever a normal card reward is generated, one random card is Appraised: take it and gain 12 Gold. "
            + "Skip the entire reward and gain 6 Gold.",
            rewardRules:
            [
                new AppendOfferGrantRule(
                    CardReward, [Gold(12)], Count: 1, OfferTags: ["appraised"]),
            ],
            runPrograms: [WhenSkipping(CardReward, Gold(6))]),

        // ── 2 ─────────────────────────────────────────────────────────────────────────────────────────────
        // Every third purchase pays. The tally is bumped and then READ by the same reaction: effects resolve in
        // the order they were enqueued, so the conditional sees the purchase it was enqueued alongside.
        Shop("copper_receipt_roll", "Copper Receipt Roll",
            "After every third purchase made in Shops, gain 35 Gold. Card removal counts as a purchase.",
            runPrograms:
            [
                RunPrograms.On<ShopItemPurchasedRunEvent>(
                    new IncrementCounterRunEffect(Receipts, 1),
                    new ConditionalRunEffect(
                        new RunComparisonExpression(
                            RunExpr.Counter(Receipts), RunComparisonOperator.GreaterOrEqual, RunExpr.Const(3)),
                        Gold(35), new SetCounterRunEffect(Receipts, 0))),
            ]),

        // ── 3 ─────────────────────────────────────────────────────────────────────────────────────────────
        // The mark and the blood price are the same flag, so the relic can only ever cost you once per Act and
        // only for the relic it actually made cheaper. The discount marks the first Normal Relic on the shelf;
        // the 5 HP is charged when a Normal Relic is bought — see ADAPTATIONS.
        Shop("secondhand_reliquary", "Secondhand Reliquary",
            "The first time each Act you enter a Shop, one Normal Relic for sale is Secondhand: it costs 30% "
            + "less, and buying it costs 5 HP.",
            shopPrices:
            [
                new ShopPriceRule(
                    new ShopPriceMatch(ShopEntryKinds.Relic, [NormalRelic]),
                    PercentDelta: -30,
                    Limit: ShopPriceRuleLimit.FirstMatchPerVisit,
                    Condition: Unspent("secondhand_reliquary")),
            ],
            runPrograms:
            [
                RunPrograms.When<ShopItemPurchasedRunEvent>(
                    RunExpr.And(
                        RunExpr.And(
                            RunEventValues.ShopItemIsKind(ShopEntryKinds.Relic),
                            RunEventValues.ShopItemHasTag(NormalRelic)),
                        Unspent("secondhand_reliquary")),
                    new ApplyRunDamageRunEffect(5),
                    Spend("secondhand_reliquary")),
            ]),

        // ── 4 ─────────────────────────────────────────────────────────────────────────────────────────────
        // An elite is an ordinary combat node wearing a tag; without the tag nothing downstream could tell this
        // win from any other.
        Shop("bounty_hook", "Bounty Hook",
            "After defeating an Elite, gain 20 Gold — or 35 if you finished that combat below half your Max HP.",
            runPrograms:
            [
                RunPrograms.When<CombatResolvedRunEvent>(
                    RunExpr.And(
                        RunEventValues.CombatWasVictory,
                        RunEventValues.NodeHasTag(MapNodeTags.Elite)),
                    new ConditionalRunEffect(
                        new RunComparisonExpression(
                            RunExpr.Multiply(RunEventValues.CombatHeroHpRemaining, RunExpr.Const(2)),
                            RunComparisonOperator.LessThan, RunExpr.MaxHealth),
                        [Gold(35)],
                        [Gold(20)])),
            ]),

        // ── 5 ─────────────────────────────────────────────────────────────────────────────────────────────
        // "At the end of each Act" is a real moment now, so the purse simply waits for it.
        Shop("witchmarket_purse", "Witchmarket Purse",
            "At the end of each Act, gain 20 Gold for every full 100 Gold you own, up to 60 Gold.",
            runPrograms:
            [
                RunPrograms.On<ActCompletedRunEvent>(
                    new ComputedResourceRunEffect(StandardRunIds.Gold,
                        RunExpr.Min(
                            RunExpr.Multiply(
                                RunExpr.Divide(RunExpr.Resource(StandardRunIds.Gold), RunExpr.Const(100)),
                                RunExpr.Const(20)),
                            RunExpr.Const(60)))),
            ]),

        // ── 6 ─────────────────────────────────────────────────────────────────────────────────────────────
        // Rejecting a relic is not a refusal the engine has to model — it is simply another offer on the table.
        Shop("bent_auction_gavel", "Bent Auction Gavel",
            "Whenever a random Normal Relic is offered as a reward, you may reject it and gain 65 Gold instead. "
            + "Boss, Event and purchased relics are excluded.",
            rewardRules:
            [
                new AddRewardOfferRule(
                    new RewardMatch(RewardKinds.Relic, [NormalRelic], NoneTag: ["boss", "event", "purchased"]),
                    new RewardOffer("sell-the-relic", [Gold(65)], RewardKinds.Resource, ["refusal"])),
            ]),

        // ── 7 ─────────────────────────────────────────────────────────────────────────────────────────────
        Shop("wastebrokers_permit", "Wastebroker's Permit",
            "Whenever you Archive a card, record 1 Salvage, up to 3 per combat. After a victory, gain 5 Gold "
            + "per Salvage.",
            eligibility: Eligibility.Bureaucrat,
            combatRule: ShopRelicRules.WastebrokersPermit,
            runPrograms:
            [
                AfterVictory(new ComputedResourceRunEffect(StandardRunIds.Gold,
                    RunExpr.Multiply(Tallied(ShopRelicRules.Salvage), RunExpr.Const(5)))),
            ]),

        // ── 8 ─────────────────────────────────────────────────────────────────────────────────────────────
        // The fight adds up what each corpse was worth; the cap is applied here, where the whole fight's total
        // is known — capping the running tally would silently lose the last enemy's share.
        Shop("filing_fee_stamp", "Filing-Fee Stamp",
            "At combat end, each enemy that died with 5+ Paperwork grants 6 Gold, and 4 more if it died with "
            + "10+. At most 20 Gold per combat.",
            eligibility: Eligibility.Bureaucrat,
            combatRule: ShopRelicRules.FilingFeeStamp,
            runPrograms:
            [
                AfterVictory(new ComputedResourceRunEffect(StandardRunIds.Gold,
                    RunExpr.Min(Tallied(ShopRelicRules.FilingFee), RunExpr.Const(20)))),
            ]),

        // ── 9 ─────────────────────────────────────────────────────────────────────────────────────────────
        // The discount and the surcharge that follows it are two rules reading the same pair of flags, so the
        // shop always shows the price the next removal will actually cost.
        Shop("scriveners_shears", "Scrivener's Shears",
            "The first card removal you buy each Act costs 50% less; the next one that Act costs 25% more.",
            shopPrices:
            [
                new ShopPriceRule(RemovalService, PercentDelta: -50, Condition: Unspent("shears_discount")),
                new ShopPriceRule(RemovalService, PercentDelta: 25,
                    Condition: RunExpr.And(Spent("shears_discount"), Unspent("shears_penalty"))),
            ],
            runPrograms:
            [
                RunPrograms.When<ShopItemPurchasedRunEvent>(
                    RunEventValues.ShopItemHasTag(Removal),
                    new ConditionalRunEffect(
                        Unspent("shears_discount"),
                        [Spend("shears_discount")],
                        [Spend("shears_penalty")])),
            ]),

        // ── 10 ────────────────────────────────────────────────────────────────────────────────────────────
        // "You may" is a reward you are allowed to walk away from. The card it sharpens is the one the shop
        // just handed over: the purchase event fires after the payload has resolved, so it is already yours.
        Shop("apprentices_whetstone", "Apprentice's Whetstone",
            "Whenever you purchase a card, you may pay 20 additional Gold to upgrade it immediately.",
            runPrograms:
            [
                RunPrograms.When<ShopItemPurchasedRunEvent>(
                    RunExpr.And(
                        RunEventValues.ShopItemIsKind(ShopEntryKinds.Card),
                        RunExpr.HasResource(StandardRunIds.Gold, 20)),
                    new OfferRewardRunEffect(
                        new RewardId("whetstone"),
                        [
                            new RewardOffer("sharpen",
                            [
                                Gold(-20),
                                new UpgradeCardsRunEffect(RunSelectors.LastAddedCard),
                            ], RewardKinds.Card, ["upgrade"]),
                        ],
                        1)),
            ]),

        // ── 11 ────────────────────────────────────────────────────────────────────────────────────────────
        // A service the player CARRIES into every shop. Not repeatable, which is what "once per Shop visit"
        // already means to a shop service.
        Shop("backroom_kettle", "Backroom Kettle",
            "Once per Shop visit, pay 25 Gold to heal 8 HP.",
            shopServices:
            [
                new ShopService("backroom-kettle", StandardRunIds.Gold, 25, [Heal(8)],
                    TextKey: "A kettle is on in the back room."),
            ]),

        // ── 12 ────────────────────────────────────────────────────────────────────────────────────────────
        // Two halves of one promise: the shop it is bought in gets the extra slot immediately, and every future
        // shop gets it because the relic is being worn. The surcharge is an ordinary price rule matching the
        // tag the grant stamps on the extra one.
        Shop("crooked_display_case", "Crooked Display Case",
            "Adds one additional Normal Relic to this Shop and to every future Shop. The extra relic costs 20% more.",
            pickup: [new AddShopStockRunEffect(RelicShelf, 1, [Extra])],
            shopStock: [new ShopStockGrant(RelicShelf, 1, [Extra])],
            shopPrices: [new ShopPriceRule(new ShopPriceMatch(AnyTag: [Extra]), PercentDelta: 20)]),

        // ── 13 ────────────────────────────────────────────────────────────────────────────────────────────
        Shop("turnover_bell", "Turnover Bell",
            "Once per Shop visit, pay 30 Gold to replace all unsold cards with new ones. Relics and services "
            + "are unaffected.",
            shopServices:
            [
                new ShopService("turnover-bell", StandardRunIds.Gold, 30,
                    [new RestockShopStockRunEffect(CardShelf)],
                    TextKey: "Ring for fresh stock."),
            ]),

        // ── 14 ────────────────────────────────────────────────────────────────────────────────────────────
        // Debt is a promise, not negative Gold: it lives on a counter, it cannot be spent, and it is not Gold
        // spent. Half of every Gold gain goes to paying it down — which is why the gain has to say WHICH
        // resource it was, or a Voucher arriving would pay the shopkeeper too.
        Shop("debtors_signet", "Debtor's Signet",
            "You may buy what you cannot afford; the remainder becomes Debt, up to 100. While in Debt, half of "
            + "any Gold you gain, rounded up, repays it.",
            shopDebt: [new ShopDebtTerms(Debt, 100)],
            runPrograms:
            [
                RunPrograms.When<ResourceChangedRunEvent>(
                    RunExpr.And(
                        RunExpr.And(
                            RunEventValues.ResourceIs(StandardRunIds.Gold),
                            new RunComparisonExpression(
                                RunEventValues.ResourceDelta, RunComparisonOperator.GreaterThan, RunExpr.Const(0))),
                        new RunComparisonExpression(
                            RunExpr.Counter(Debt), RunComparisonOperator.GreaterThan, RunExpr.Const(0))),
                    new ComputedResourceRunEffect(StandardRunIds.Gold, RunExpr.Negate(Repayment)),
                    new ComputedCounterRunEffect(Debt, RunExpr.Negate(Repayment))),
            ]),

        // ── 15 ────────────────────────────────────────────────────────────────────────────────────────────
        // A Waiver is not credit that stops at the price: ALL of them are consumed however few were needed, so
        // it is a flat price bend plus a purchase that empties the drawer.
        Shop("notarys_waiver", "Notary's Waiver",
            "Whenever you Ratify, gain 1 Waiver, up to 4. Each Waiver takes 10 Gold off a card removal, and "
            + "buying one spends them all.",
            eligibility: Eligibility.Bureaucrat,
            combatRule: ShopRelicRules.NotarysWaiver,
            shopPrices:
            [
                new ShopPriceRule(RemovalService,
                    FlatDelta: RunExpr.Multiply(RunExpr.Resource(Waiver), RunExpr.Const(-10))),
            ],
            runPrograms:
            [
                // Banked at the end of the fight rather than the instant you Ratify — see ADAPTATIONS.
                AfterVictory(new ComputedResourceRunEffect(Waiver,
                    RunExpr.Max(
                        RunExpr.Const(0),
                        RunExpr.Min(
                            Tallied(ShopRelicRules.RatifyTally),
                            RunExpr.Subtract(RunExpr.Const(4), RunExpr.Resource(Waiver)))))),
                RunPrograms.When<ShopItemPurchasedRunEvent>(
                    RunEventValues.ShopItemHasTag(Removal),
                    new ComputedResourceRunEffect(Waiver, RunExpr.Negate(RunExpr.Resource(Waiver)))),
            ]),

        // ── 16 ────────────────────────────────────────────────────────────────────────────────────────────
        Shop("priority_window_pass", "Priority Window Pass",
            "The first Form or Queue card in each Shop is 10% cheaper, and buying one refunds up to 15 of the "
            + "Gold you actually paid.",
            eligibility: Eligibility.Bureaucrat,
            shopPrices:
            [
                new ShopPriceRule(
                    new ShopPriceMatch(ShopEntryKinds.Card, ["form", "queue"]),
                    PercentDelta: -10,
                    Limit: ShopPriceRuleLimit.FirstMatchPerVisit),
            ],
            runPrograms:
            [
                RunPrograms.When<ShopItemPurchasedRunEvent>(
                    RunExpr.And(
                        RunEventValues.ShopItemIsKind(ShopEntryKinds.Card),
                        RunExpr.Or(
                            RunEventValues.ShopItemHasTag("form"),
                            RunEventValues.ShopItemHasTag("queue"))),
                    new ComputedResourceRunEffect(StandardRunIds.Gold,
                        RunExpr.Min(RunEventValues.ShopCurrencyPaid, RunExpr.Const(15)))),
            ]),

        // ── 17 ────────────────────────────────────────────────────────────────────────────────────────────
        // The reward's own source is asked for the second relic, so what is revealed is whatever that chest
        // could have held anyway.
        Shop("twin_lock_chest_key", "Twin-Lock Chest Key",
            "Whenever a Normal Relic is offered, two are revealed and you choose one.",
            rewardRules:
            [
                new DrawMoreOffersRule(
                    new RewardMatch(RewardKinds.Relic, [NormalRelic], NoneTag: ["boss", "event"])),
            ]),

        // ── 18 ────────────────────────────────────────────────────────────────────────────────────────────
        // "Offered upgraded": taking it adds the card and then sharpens the one you just took.
        Shop("appraisers_chalk", "Appraiser's Chalk",
            "Whenever a normal card reward is generated, one of its cards is offered upgraded.",
            rewardRules:
            [
                new AppendOfferGrantRule(
                    CardReward,
                    [new UpgradeCardsRunEffect(RunSelectors.LastAddedCard)],
                    Count: 1,
                    OfferTags: ["upgraded"]),
            ]),

        // ── 19 ────────────────────────────────────────────────────────────────────────────────────────────
        // "Without entering combat" is not something the run can observe, so any resolved Event counts — see
        // ADAPTATIONS.
        Shop("guest_favor_token", "Guest-Favor Token",
            "The first time each Act you resolve an Event, choose 25 Gold or upgrade a card in your deck.",
            runPrograms:
            [
                RunPrograms.When<EventChoiceMadeRunEvent>(
                    Unspent("guest_favor"),
                    new OfferRewardRunEffect(
                        new RewardId("guest-favor"),
                        [
                            new RewardOffer("guest-gold", [Gold(25)], RewardKinds.Resource),
                            // The design's alternative is a special two-card reward; a relic definition has no
                            // handle on the Act's card pool, so the favour sharpens a card you already carry
                            // instead — see ADAPTATIONS.
                            new RewardOffer("guest-upgrade",
                                [new UpgradeCardsRunEffect(
                                    RunSelectors.DeckCards.ChooseByPlayer(1, "upgrade a card"))],
                                RewardKinds.Card),
                        ],
                        1),
                    Spend("guest_favor")),
            ]),

        // ── 20 ────────────────────────────────────────────────────────────────────────────────────────────
        // A Punch is credit: it settles part of a price without any Gold leaving the purse, and it is spent in
        // whole units that never overpay. The design lets the player choose how many to redeem; here they are
        // simply used as far as they help — see ADAPTATIONS.
        Shop("merchant_punchcard", "Merchant Punchcard",
            "Entering a Shop earns 1 Punch, up to 3. Each Punch takes 20 Gold off a purchase.",
            shopCredit: [new ShopCreditSource(Punch, 20, StandardRunIds.Gold)],
            runPrograms:
            [
                RunPrograms.When<NodeEnteredRunEvent>(
                    RunExpr.And(
                        RunEventValues.NodeHasTag(MapNodeTags.Shop),
                        new RunComparisonExpression(
                            RunExpr.Resource(Punch), RunComparisonOperator.LessThan, RunExpr.Const(3))),
                    new ChangeResourceRunEffect(Punch, 1)),
            ]),

        // ── 21 ────────────────────────────────────────────────────────────────────────────────────────────
        // The warranty pays out on your next visit rather than letting you hand the relic back — returning a
        // specific relic is not something a rule can name. See ADAPTATIONS.
        Shop("warranty_tag", "Warranty Tag",
            "The first Relic you buy each Act is under warranty: the next Shop you enter refunds half the Gold "
            + "you actually paid for it.",
            runPrograms:
            [
                RunPrograms.When<ShopItemPurchasedRunEvent>(
                    RunExpr.And(
                        RunEventValues.ShopItemIsKind(ShopEntryKinds.Relic),
                        Unspent("warranty")),
                    new ComputedCounterRunEffect(WarrantyValue,
                        RunExpr.Divide(RunEventValues.ShopCurrencyPaid, RunExpr.Const(2))),
                    Spend("warranty")),
                RunPrograms.When<NodeEnteredRunEvent>(
                    RunExpr.And(
                        RunEventValues.NodeHasTag(MapNodeTags.Shop),
                        new RunComparisonExpression(
                            RunExpr.Counter(WarrantyValue), RunComparisonOperator.GreaterThan, RunExpr.Const(0))),
                    new ComputedResourceRunEffect(StandardRunIds.Gold, RunExpr.Counter(WarrantyValue)),
                    new SetCounterRunEffect(WarrantyValue, 0)),
            ]),

        // ── 22 ────────────────────────────────────────────────────────────────────────────────────────────
        // A purchase and a mugging are both just Gold leaving; only where it happened tells them apart.
        Shop("indemnity_stamp", "Indemnity Stamp",
            "When picked up, gain 20 Gold. Whenever you lose Gold outside a Shop, recover half of it, up to 50.",
            pickup: [Gold(20)],
            runPrograms:
            [
                RunPrograms.When<ResourceChangedRunEvent>(
                    RunExpr.And(
                        RunExpr.And(
                            RunEventValues.ResourceIs(StandardRunIds.Gold),
                            new RunComparisonExpression(
                                RunEventValues.ResourceDelta, RunComparisonOperator.LessThan, RunExpr.Const(0))),
                        RunExpr.Not(RunExpr.InShop)),
                    new ComputedResourceRunEffect(StandardRunIds.Gold,
                        RunExpr.Min(
                            RunExpr.Divide(RunExpr.Negate(RunEventValues.ResourceDelta), RunExpr.Const(2)),
                            RunExpr.Const(50)))),
            ]),

        // ── 23 ────────────────────────────────────────────────────────────────────────────────────────────
        // A Voucher is not Gold — it persists, it cannot be lost, and spending it is not spending Gold — so it
        // is its own resource that the till accepts.
        Shop("archive_voucher_roll", "Archive Voucher Roll",
            "After winning a combat in which you Archived at least 2 cards, gain 1 Archive Voucher, up to 5. "
            + "Each Voucher is 10 Gold of Shop credit.",
            eligibility: Eligibility.Bureaucrat,
            combatRule: ShopRelicRules.ArchiveVoucherRoll,
            shopCredit: [new ShopCreditSource(ArchiveVoucher, 10, StandardRunIds.Gold)],
            runPrograms:
            [
                RunPrograms.When<CombatResolvedRunEvent>(
                    RunExpr.And(
                        RunEventValues.CombatWasVictory,
                        RunExpr.And(
                            new RunComparisonExpression(
                                Tallied(ShopRelicRules.ArchiveTally),
                                RunComparisonOperator.GreaterOrEqual, RunExpr.Const(2)),
                            new RunComparisonExpression(
                                RunExpr.Resource(ArchiveVoucher), RunComparisonOperator.LessThan, RunExpr.Const(5)))),
                    new ChangeResourceRunEffect(ArchiveVoucher, 1)),
            ]),

        // ── 24 ────────────────────────────────────────────────────────────────────────────────────────────
        // Three separate first-purchases, one per card type, each with its own flag.
        Shop("departmental_purchase_order", "Departmental Purchase Order",
            "Each Act, the first Deed, the first Working and the first Rite you buy each refund up to 15 of the "
            + "Gold you actually paid.",
            eligibility: Eligibility.Bureaucrat,
            runPrograms:
            [
                FirstOfTypeRefunds("deed"),
                FirstOfTypeRefunds("working"),
                FirstOfTypeRefunds("rite"),
            ]),
    ];

    // ── shared pieces ─────────────────────────────────────────────────────────────────────────────────────

    private static readonly RewardMatch CardReward =
        new(RewardKinds.Card, [NormalRelic], NoneTag: ["boss", "event"]);

    private static readonly ShopPriceMatch RemovalService = new(AnyTag: [Removal]);

    // Half of a Gold gain, rounded up, but never more than is still owed.
    private static readonly IRunExpression<int> Repayment =
        RunExpr.Min(
            RunExpr.Divide(RunExpr.Add(RunEventValues.ResourceDelta, RunExpr.Const(1)), RunExpr.Const(2)),
            RunExpr.Counter(Debt));

    // What a fight left behind for the run to collect.
    private static IRunExpression<int> Tallied(CounterId counter) =>
        RunEventValues.CombatCounter(counter.ToString());

    private static ITriggeredRunEffectDefinition AfterVictory(params IRunEffectRequest[] effects) =>
        RunPrograms.When<CombatResolvedRunEvent>(RunEventValues.CombatWasVictory, effects);

    private static ITriggeredRunEffectDefinition WhenSkipping(RewardMatch match, params IRunEffectRequest[] effects) =>
        RunPrograms.When<RewardSkippedRunEvent>(
            RunExpr.And(
                RunEventValues.RewardIsKind(match.Kind!),
                RunEventValues.RewardHasTag(match.AnyTag![0])),
            effects);

    private static ITriggeredRunEffectDefinition FirstOfTypeRefunds(string type) =>
        RunPrograms.When<ShopItemPurchasedRunEvent>(
            RunExpr.And(
                RunEventValues.ShopItemHasTag(type),
                Unspent($"dpo_{type}")),
            new ComputedResourceRunEffect(StandardRunIds.Gold,
                RunExpr.Min(RunEventValues.ShopCurrencyPaid, RunExpr.Const(15))),
            Spend($"dpo_{type}"));

    // "The first time each Act" — an ACT flag, which the act boundary forgets. A run flag would make every one
    // of these relics a once-per-RUN relic the moment a second act existed.
    private static IRunExpression<bool> Unspent(string what) => RunExpr.Not(Spent(what));

    private static IRunExpression<bool> Spent(string what) => RunExpr.ActFlag(new RunFlagId(what));

    private static IRunEffectRequest Spend(string what) => new SetActFlagRunEffect(new RunFlagId(what), true);
}
