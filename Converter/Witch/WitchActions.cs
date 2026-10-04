using RogueDeck.Core.Combat;
using RogueDeck.Scenario.Authoring;

namespace BnbContent.Converter.Witch;

// The Hedge Witch's two actions (hedge_witch_master.md §2.3–§2.6, §7–§8): putting a card from her hand into the
// cauldron, and BREW. Both are ACTIONS, not cards (CardDefinition.IsAction): used, never drawn, never counted as
// a card played. The cauldron is the engine's set-aside pile; its three slots are this file's rule.
//
// Every number here is the canon's BALANCE DRAFT (§23): ingredient 0/1 Energy, BREW 1, Fang 5 damage, Hex 2
// Hexed, Husk 4 Block, Hearth heal 1 lost HP, Fortune 15 % Misfortune; the five concentrated brews below.
public static class WitchActions
{
    public const string AddIngredient = "cauldron_add";
    public const string Brew = "cauldron_brew";
    public const string IngredientActionTag = "ingredient_action";
    public const string BrewActionTag = "brew_action";

    // A card the cauldron will not take (a fight's own cards, a relic's) — §16.3, written on at assembly.
    public const string Uncookable = "uncookable";

    public const int Slots = 3;

    // The ingredient families (§3) — every Witch card carries exactly one; a card with none cooks as Dregs (§6).
    public const string Fang = "fam_fang";
    public const string Hex = "fam_hex";
    public const string Husk = "fam_husk";
    public const string Hearth = "fam_hearth";
    public const string Fortune = "fam_fortune";
    public static readonly string[] Families = [Fang, Hex, Husk, Hearth, Fortune];

    // Special ingredients (§15, R8): a card that counts as one more of its family in a mixed brew (Knotted Cord),
    // and one whose place in the pot adds Ward Wax to whatever is brewed (Horn Spoon).
    public static string Double(string family) => family + "_double";
    public const string WaxRider = "rider_ward_wax";
    public const int WaxRiderStacks = 3;

    // §7 Concentrated and §3 per-ingredient drafts.
    private const int FangDamage = 5, HexHexed = 2, HuskBlock = 4, HearthHeal = 1, FortuneMisfortune = 3;
    private const int RedTeeth = 18, ThirdNight = 7, ShellWax = 8, HearthPhysic = 4, BlackCatsLuck = 10;

    public static IReadOnlyList<CardData> All() => [AddIngredientAction(), BrewAction()];

    private static ICombatantTargetSelector You => CombatantTargetSelectors.Source;
    private static ICombatantTargetSelector Target => CombatantTargetSelectors.EventTarget;

    private static ICombatExpression<CardPlayContext, int> Const(int value) => new ConstantExpression<CardPlayContext>(value);

    private static ICombatExpression<CardPlayContext, int> InPot(string? tag = null) =>
        new CombatantZoneCardCountExpression<CardPlayContext>(
            You, CardZone.SetAsidePile, tag is null ? null : new TagId(tag));

    private static ICombatExpression<CardPlayContext, int> InHand(string? tag = null) =>
        new CombatantZoneCardCountExpression<CardPlayContext>(You, CardZone.Hand, tag is null ? null : new TagId(tag));

    private static ICombatExpression<CardPlayContext, bool> Compare(
        ICombatExpression<CardPlayContext, int> left, ComparisonOperator op, ICombatExpression<CardPlayContext, int> right) =>
        new ComparisonExpression<CardPlayContext>(left, op, right);

    // ── put a card in the pot (§2.3) ──────────────────────────────────────────────────────────────────────

    private static CardData AddIngredientAction() => new()
    {
        Id = AddIngredient,
        NameKey = "Into the Pot",
        DescriptionKey = "Put a card from your hand into the cauldron instead of playing it. The first each turn is free.",
        IsAction = true,
        Costs = [new ResourceCost(StandardCombatIds.EnergyResource, 1)],
        Tags = [new TagId(IngredientActionTag)],
        // A free slot, and something in hand the pot will take — and the lid not shut.
        PlayCondition = new AndExpression<CardPlayContext>(LidOpen,
            new AndExpression<CardPlayContext>(
                Compare(InPot(), ComparisonOperator.Less, Const(Slots)),
                Compare(new SubtractExpression<CardPlayContext>(InHand(), InHand(Uncookable)),
                    ComparisonOperator.Greater, Const(0)))),
        Program = new EffectProgram<CardPlayContext>(new SequenceEffectNode<CardPlayContext>(
        [
            CombatProgramModel.Build<CardPlayContext>(new CombatNodeModel("moveCardToZone", "source",
                Card: new CombatCardSpec("chosen", CardZone.Hand, Purpose: "choose a card for the pot",
                    ExcludeTag: Uncookable),
                ToZone: CardZone.SetAsidePile)).Root,
            new RemoveStatusNode<CardPlayContext>(You, new StatusDefinitionId(WitchKeywords.FreeIngredient)),
        ])),
    };

    // ── BREW (§2.4–§2.6, §7, §8) ──────────────────────────────────────────────────────────────────────────

    private static CardData BrewAction() => new()
    {
        Id = Brew,
        NameKey = "Brew",
        DescriptionKey = "With three ingredients in the cauldron: brew them. Three of one family make a concentrated " +
            "brew; any other mix gives what each ingredient gives. The ingredients then go to your discard pile.",
        IsAction = true,
        Costs = [new ResourceCost(StandardCombatIds.EnergyResource, 1)],
        Tags = [new TagId(BrewActionTag)],
        PlayCondition = new AndExpression<CardPlayContext>(LidOpen,
            Compare(InPot(), ComparisonOperator.GreaterOrEqual, Const(Slots))),
        Program = new EffectProgram<CardPlayContext>(new SequenceEffectNode<CardPlayContext>(
        [
            Concentrated(),
            new ConditionalEffectNode<CardPlayContext>(Compare(InPot(WaxRider), ComparisonOperator.Greater, Const(0)),
                Apply(Cards.Keywords.WardWax,
                    new MultiplyExpression<CardPlayContext>(InPot(WaxRider), Const(WaxRiderStacks)), You)),
            // Into the discard pile, every one (§2.4) — then the pot is empty and she shelters again. Unless she
            // never washes the pot: then she keeps one back (WitchRules).
            new ConditionalEffectNode<CardPlayContext>(
                new TargetHasStatusExpression<CardPlayContext>(You, new StatusDefinitionId(WitchRules.NeverWashThePot)),
                KeepOneBack(),
                @else: CombatProgramModel.Build<CardPlayContext>(new CombatNodeModel("moveCards", "source",
                    FromZone: CardZone.SetAsidePile, ToZone: CardZone.DiscardPile)).Root),
            new SetCombatantCounterNode<CardPlayContext>(You, WitchKeywords.BrewsThisTurn, Const(1), relative: true),
        ])),
    };

    // Three of one family: that family's named brew (§7). Otherwise the Mixed brew: the sum of what each
    // ingredient gives, in the canon's fixed order Hex → Fang → Husk → Hearth → Fortune (§2.6).
    private static IEffectNode<CardPlayContext> Concentrated()
    {
        IEffectNode<CardPlayContext> brew = Mixed();
        var named = new (string Family, IEffectNode<CardPlayContext> Effect)[]
        {
            (Fang, Damage(Const(RedTeeth))),
            (Hex, Apply(WitchKeywords.Hexed, Const(ThirdNight), Target)),
            (Husk, Apply(Cards.Keywords.WardWax, Const(ShellWax), You)),
            (Hearth, Heal(Const(HearthPhysic))),
            (Fortune, Apply(WitchKeywords.Misfortune, Const(BlackCatsLuck), Target)),
        };
        foreach (var (family, effect) in named.Reverse())
            brew = new ConditionalEffectNode<CardPlayContext>(
                Compare(InPot(family), ComparisonOperator.GreaterOrEqual, Const(Slots)), effect, @else: brew);
        return brew;
    }

    private static IEffectNode<CardPlayContext> Mixed() => new SequenceEffectNode<CardPlayContext>(
    [
        Each(Hex, Apply(WitchKeywords.Hexed, Times(Hex, HexHexed), Target)),
        Each(Fang, Damage(Times(Fang, FangDamage))),
        Each(Husk, new GainBlockNode<CardPlayContext>(You, Times(Husk, HuskBlock))),
        Each(Hearth, Heal(Times(Hearth, HearthHeal))),
        Each(Fortune, Apply(WitchKeywords.Misfortune, Times(Fortune, FortuneMisfortune), Target)),
    ]);

    // Only when that family is in the pot: a 0-damage hit is still a hit to everything that counts hits.
    private static IEffectNode<CardPlayContext> Each(string family, IEffectNode<CardPlayContext> effect) =>
        new ConditionalEffectNode<CardPlayContext>(Compare(InPot(family), ComparisonOperator.Greater, Const(0)), effect);

    // What a family is worth in a mixed brew: its cards, and one more for each that counts double.
    private static ICombatExpression<CardPlayContext, int> Times(string family, int each) =>
        new MultiplyExpression<CardPlayContext>(
            new AddExpression<CardPlayContext>(InPot(family), InPot(Double(family))), Const(each));

    private static IEffectNode<CardPlayContext> Damage(ICombatExpression<CardPlayContext, int> amount) =>
        new DealDamageNode<CardPlayContext>(Target, amount);

    private static IEffectNode<CardPlayContext> Apply(
        string status, ICombatExpression<CardPlayContext, int> stacks, ICombatantTargetSelector to) =>
        new ApplyStatusNode<CardPlayContext>(to, new StatusDefinitionId(status), stacks);

    // Never Wash the Pot: she picks the one to keep (a mark on the card), the rest go, and the mark is wiped.
    private static CounterId Kept => new("kept_in_the_pot");

    private static IEffectNode<CardPlayContext> KeepOneBack() => new CausalSequenceEffectNode<CardPlayContext>(
    [
        CombatProgramModel.Build<CardPlayContext>(new CombatNodeModel("setCardInstanceMarkCounter", "source",
            CombatAmountSpec.FromConst(1),
            Card: new CombatCardSpec("chosen", CardZone.SetAsidePile, Purpose: "keep one in the cauldron"),
            CounterId: Kept.value, Relative: false)).Root,
        new ForEachCardInZoneNode<CardPlayContext>(You, CardZone.SetAsidePile,
            new ConditionalEffectNode<CardPlayContext>(
                Compare(new CardInstanceMarkCounterExpression<CardPlayContext>(new IteratedCardExpression<CardPlayContext>(), Kept),
                    ComparisonOperator.Equal, Const(0)),
                new MoveCardToZoneNode<CardPlayContext>(You, new IteratedCardExpression<CardPlayContext>(), CardZone.DiscardPile),
                @else: new SetCardInstanceMarkCounterNode<CardPlayContext>(
                    You, new IteratedCardExpression<CardPlayContext>(), Kept, Const(0)))),
    ]);

    // Shut the Lid: nothing goes in or comes out until her next turn.
    private static ICombatExpression<CardPlayContext, bool> LidOpen =>
        new NotExpression<CardPlayContext>(
            new TargetHasStatusExpression<CardPlayContext>(You, new StatusDefinitionId(WitchRules.ShutTheLid)));

    // Hearth: heal HP lost during THIS combat only — never above the HP she began the fight on (§3.4). What the cap
    // would waste may become Ward Wax first (Keep the Drippings).
    public static IEffectNode<CardPlayContext> Heal(ICombatExpression<CardPlayContext, int> amount) =>
        new CausalSequenceEffectNode<CardPlayContext>([WitchRules.Drippings(You, amount), CappedHeal(amount)]);

    private static IEffectNode<CardPlayContext> CappedHeal(ICombatExpression<CardPlayContext, int> amount) =>
        new HealNode<CardPlayContext>(You,
            new MinExpression<CardPlayContext>(amount,
                new MaxExpression<CardPlayContext>(Const(0),
                    new SubtractExpression<CardPlayContext>(
                        new CombatantCounterExpression<CardPlayContext>(You, WitchKeywords.CombatStartHp),
                        new CombatantCurrentHealthExpression<CardPlayContext>(You)))));
}
