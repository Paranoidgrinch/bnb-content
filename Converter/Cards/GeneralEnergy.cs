using RogueDeck.Core.Combat;
using RogueDeck.Scenario.Authoring;
using static BnbContent.Converter.Cards.CardAuthoring;
using static BnbContent.Converter.Cards.GeneralPoolShapes;

namespace BnbContent.Converter.Cards;

// General pool, axis 1: Energy (GENERAL_POOL_PLAN.md §4, approved 2026-11-08). Energy now, Held Energy for
// later, cheaper cards, and two Rares that turn what is left over into something.
public static class GeneralEnergy
{
    // ── Common ────────────────────────────────────────────────────────────────────────────────────────────

    private static readonly BnbCard StolenHour = new(
        "stolen_hour", "Stolen Hour", WorkingTag, 0,
        "Gain 2 Energy. Exhaust.",
        GainEnergy(2),
        Tags: [ExhaustTag]);

    private static readonly BnbCard BankedEmbers = new(
        "banked_embers", "Banked Embers", WorkingTag, 1,
        "Gain 2 Held Energy. Draw 1 card.",
        Seq(Hold(2), Draw(1)));

    private static readonly BnbCard TallowStub = new(
        "tallow_stub", "Tallow Stub", WorkingTag, 0,
        "Gain 1 Held Energy. Unread: gain 1 Held Energy.",
        Hold(1),
        Tags: Unread("tallow_stub"));

    // Kept from the old pool (was Sanctioned Charm, sharpening pass 2026-10-21), now a Common.
    private static readonly BnbCard SecondWind = new(
        "sanctioned_charm", "Second Wind", WorkingTag, 0,
        "Gain 1 Energy. Draw 1 card. Exhaust.",
        Seq(GainEnergy(1), Draw(1)),
        Tags: [ExhaustTag]);

    // ── Uncommon ──────────────────────────────────────────────────────────────────────────────────────────

    private static readonly BnbCard CoinUnderTheTongue = new(
        "coin_under_the_tongue", "Coin Under the Tongue", WorkingTag, 1,
        "Pledge. Draw 2 cards. The next card you play this turn costs 1 less.",
        Seq(Pledged(), Draw(2), Apply(GeneralForgery.Coin, 1, You)),
        Rarity: "uncommon", Tags: [PledgeTag, GeneralForgery.ForgeryTag]);

    // Kept; both halves now last this turn only (user, 2026-11-08).
    private static readonly BnbCard FalseSignature = new(
        "false_signature", "False Signature", WorkingTag, 0,
        "Your next card this turn costs 1 less. The card after it this turn costs 1 more. Exhaust.",
        Apply(GeneralForgery.Discount, 1, You),
        Rarity: "uncommon", Tags: [ExhaustTag, GeneralForgery.ForgeryTag]);

    private static readonly BnbCard MeltedDown = new(
        "melted_down", "Melted Down", WorkingTag, 0,
        "Exhaust a card from your hand. Gain Energy equal to its cost.",
        MeltDown(draw: 0),
        Rarity: "uncommon", Tags: [MeltedDownTag]);

    private static readonly BnbCard WickBurntShort = new(
        "wick_burnt_short", "Wick Burnt Short", WorkingTag, 0,
        "Gain 2 Energy. At the start of your next turn, gain 1 Fatigue.",
        Seq(GainEnergy(2), Apply(Fatigue, 1, You)),
        Rarity: "uncommon", Act: 2);

    // ── Rare ──────────────────────────────────────────────────────────────────────────────────────────────

    private static readonly BnbCard VigilLamp = new(
        "vigil_lamp", "Vigil Lamp", RiteTag, 0,
        "For the rest of this combat, at the end of your turn gain 8 Block for each Energy you have left. Exhaust.",
        InstallRite(GeneralPoolRules.VigilLamp),
        Rarity: "rare", Act: 2);

    private static readonly BnbCard MisersCoffer = new(
        "misers_coffer", "Miser's Coffer", RiteTag, 1,
        "Whenever you end your turn with Energy left, keep up to 2 of it as Held Energy. Exhaust.",
        InstallRite(GeneralPoolRules.MisersCoffer),
        Rarity: "rare", Act: 3);

    public static IReadOnlyList<BnbCard> All() =>
    [
        StolenHour, StolenHour.Upgraded("Gain 3 Energy. Exhaust.", GainEnergy(3)),
        BankedEmbers, BankedEmbers.Upgraded("Gain 3 Held Energy. Draw 1 card.", Seq(Hold(3), Draw(1))),
        TallowStub, TallowStub.Upgraded("Gain 2 Held Energy. Unread: gain 2 Held Energy.", Hold(2),
            tags: Unread("tallow_stub+")),
        SecondWind, SecondWind.Upgraded("Gain 1 Energy. Draw 2 cards. Exhaust.", Seq(GainEnergy(1), Draw(2))),

        CoinUnderTheTongue, CoinUnderTheTongue.Upgraded(
            "Pledge. Draw 3 cards. The next card you play this turn costs 1 less.",
            Seq(Pledged(), Draw(3), Apply(GeneralForgery.Coin, 1, You))),
        FalseSignature, FalseSignature.Upgraded(
            "Your next card this turn costs 2 less. The card after it this turn costs 1 more. Exhaust.",
            Apply(GeneralForgery.DiscountPlus, 1, You)),
        MeltedDown, MeltedDown.Upgraded(
            "Exhaust a card from your hand. Gain Energy equal to its cost. Draw 1 card.", MeltDown(draw: 1)),
        WickBurntShort, WickBurntShort.Upgraded(
            "Gain 3 Energy. At the start of your next turn, gain 1 Fatigue.",
            Seq(GainEnergy(3), Apply(Fatigue, 1, You))),

        VigilLamp, VigilLamp.Upgraded(
            "For the rest of this combat, at the end of your turn gain 12 Block for each Energy you have left. " +
            "Exhaust.", InstallRite(GeneralPoolRules.VigilLamp + "+")),
        MisersCoffer, MisersCoffer.Upgraded(
            "Whenever you end your turn with Energy left, keep up to 3 of it as Held Energy. Exhaust.",
            InstallRite(GeneralPoolRules.MisersCoffer + "+")),
    ];

    public static IReadOnlyList<(string CardId, CombatNodeModel Effect)> Unreads() =>
    [
        ("tallow_stub", Hold(1)),
        ("tallow_stub+", Hold(2)),
    ];

    // ── shapes ────────────────────────────────────────────────────────────────────────────────────────────

    // "Exhaust a card from your hand. Gain Energy equal to its cost." The cost is read off the chosen card while
    // it is still in hand, so the card is marked first, priced, and then exhausted.
    private const string MeltedDownTag = "melted_down";
    private const string Melting = "melted_down_pick";

    private static CombatNodeModel MeltDown(int draw) =>
        Seq(
            new CombatNodeModel("markCardInstance", You,
                Card: new CombatCardSpec("chosen", CardZone.Hand, Purpose: "choose a card to melt down",
                    ExcludeTag: MeltedDownTag),
                ToTag: Melting),
            CombatNodeModel.ForEachCard(You, CardZone.Hand, Seq(
                SetScratch(new CombatAmountSpec("cardCost", ReadId: Energy.value, ReadCard: new CombatCardSpec("iterated"))),
                new CombatNodeModel("markCardInstance", You, Card: new CombatCardSpec("iterated"), ToTag: Melting,
                    Relative: true),
                new CombatNodeModel("moveCardToZone", You, Card: new CombatCardSpec("iterated"),
                    ToZone: CardZone.ExhaustPile),
                IfScratch(ComparisonOperator.GreaterOrEqual, 1, GainEnergy(Scratch))),
                mark: Melting),
            draw > 0 ? Draw(draw) : Seq());

    // Pledge: the engine let the card be played short and wrote down by how much; the shortfall is next turn's
    // Fatigue.
    private static CombatNodeModel Pledged() =>
        Seq(
            SetScratch(new CombatAmountSpec("counter", SelectorKey: You, CounterId: PledgeShortfall)),
            IfScratch(ComparisonOperator.GreaterOrEqual, 1, Apply(Fatigue, Scratch, You)));
}
