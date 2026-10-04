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

    // False-Bottom Pot's fourth card waits in RESERVE (a mark on it): in the pot, but not in the recipe.
    public const string ReserveMark = "reserve_ingredient";

    private static ICombatExpression<CardPlayContext, int> InRecipe(string? tag = null) =>
        new SubtractExpression<CardPlayContext>(InPot(tag),
            new CombatantZoneCardCountExpression<CardPlayContext>(
                You, CardZone.SetAsidePile, tag is null ? null : new TagId(tag), new TagId(ReserveMark)));

    private static ICombatExpression<CardPlayContext, bool> Wears(string rule) =>
        new TargetHasStatusExpression<CardPlayContext>(You, new StatusDefinitionId(rule));

    private static ICombatExpression<CardPlayContext, bool> Unspent(CounterId latch) =>
        Compare(new CombatantCounterExpression<CardPlayContext>(You, latch), ComparisonOperator.Equal, Const(0));

    private static ICombatExpression<CardPlayContext, bool> All(params ICombatExpression<CardPlayContext, bool>[] terms) =>
        terms.Skip(1).Aggregate(terms[0], (all, term) => new AndExpression<CardPlayContext>(all, term));

    private static IEffectNode<CardPlayContext> Spend(CounterId latch) =>
        new SetCombatantCounterNode<CardPlayContext>(You, latch, Const(1), relative: false);

    // How many slots the pot has: three, and a reserve with False-Bottom Pot.
    private static ICombatExpression<CardPlayContext, int> Capacity =>
        new AddExpression<CardPlayContext>(Const(Slots),
            new MinExpression<CardPlayContext>(Const(1),
                new CombatantStatusStacksExpression<CardPlayContext>(You, new StatusDefinitionId(WitchRules.FalseBottomPot))));

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
                Compare(InPot(), ComparisonOperator.Less, Capacity),
                Compare(new SubtractExpression<CardPlayContext>(InHand(), InHand(Uncookable)),
                    ComparisonOperator.Greater, Const(0)))),
        // Causal: what follows asks about the pot AFTER the chosen card is in it.
        Program = new EffectProgram<CardPlayContext>(new CausalSequenceEffectNode<CardPlayContext>(
        [
            CombatProgramModel.Build<CardPlayContext>(new CombatNodeModel("moveCardToZone", "source",
                Card: new CombatCardSpec("chosen", CardZone.Hand, Purpose: "choose a card for the pot",
                    ExcludeTag: Uncookable),
                ToZone: CardZone.SetAsidePile)).Root,
            new RemoveStatusNode<CardPlayContext>(You, new StatusDefinitionId(WitchKeywords.FreeIngredient)),
            // A fourth card is the reserve (False-Bottom Pot).
            new ConditionalEffectNode<CardPlayContext>(Compare(InPot(), ComparisonOperator.Greater, Const(Slots)),
                CombatProgramModel.Build<CardPlayContext>(new CombatNodeModel("markCardInstance", "source",
                    Card: new CombatCardSpec("inZone", CardZone.SetAsidePile, Index: Slots), ToTag: ReserveMark)).Root),
            // Rowan Pin: the first card in each turn gives Block.
            new ConditionalEffectNode<CardPlayContext>(All(Wears(WitchRules.RowanPin), Unspent(WitchRules.RowanPinLatch)),
                new CausalSequenceEffectNode<CardPlayContext>(
                [
                    Spend(WitchRules.RowanPinLatch),
                    new GainBlockNode<CardPlayContext>(You, Const(WitchRules.RowanPinBlock)),
                ])),
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
            Compare(InRecipe(), ComparisonOperator.GreaterOrEqual, Const(Slots))),
        Program = new EffectProgram<CardPlayContext>(new CausalSequenceEffectNode<CardPlayContext>(
        [
            // Bone Strainer: with Dregs in the pot, she names the family they count as.
            new ConditionalEffectNode<CardPlayContext>(
                All(Wears(WitchRules.BoneStrainer), Compare(Dregs, ComparisonOperator.Greater, Const(0))),
                CombatProgramModel.Build<CardPlayContext>(CombatNodeModel.ChooseOptions(1,
                    [.. FamilyNames],
                    [.. Families.Select((_, i) => new CombatNodeModel("setCombatantCounter", "source",
                        CombatAmountSpec.FromConst(i + 1), CounterId: StrainerChoice.value, Relative: false))],
                    "the Dregs count as")).Root),
            Concentrated(),
            new ConditionalEffectNode<CardPlayContext>(Compare(InRecipe(WaxRider), ComparisonOperator.Greater, Const(0)),
                Apply(Cards.Keywords.WardWax,
                    new MultiplyExpression<CardPlayContext>(InRecipe(WaxRider), Const(WaxRiderStacks)), You)),
            // Herb-Wife's Rack: the first Brew this combat with a Hearth ingredient heals more.
            new ConditionalEffectNode<CardPlayContext>(
                All(Wears(WitchRules.HerbWifesRack), Unspent(WitchRules.RackLatch),
                    Compare(InRecipe(Hearth), ComparisonOperator.Greater, Const(0))),
                new CausalSequenceEffectNode<CardPlayContext>(
                    [Spend(WitchRules.RackLatch), Heal(Const(WitchRules.HerbWifeHeal))])),
            // Apothecary's Scale: the first Brew this combat of three different families gives its Energy back.
            new ConditionalEffectNode<CardPlayContext>(
                All(Wears(WitchRules.ApothecarysScale), Unspent(WitchRules.ScaleLatch),
                    Compare(DistinctFamilies, ComparisonOperator.GreaterOrEqual, Const(Slots))),
                new CausalSequenceEffectNode<CardPlayContext>(
                [
                    Spend(WitchRules.ScaleLatch),
                    new GainResourceNode<CardPlayContext>(You, StandardCombatIds.EnergyResource, Const(1)),
                ])),
            // Greasy Ladle: the first Brew each turn draws a card.
            new ConditionalEffectNode<CardPlayContext>(
                All(Wears(WitchRules.GreasyLadle), Unspent(WitchKeywords.BrewsThisTurn)),
                new DrawCardsNode<CardPlayContext>(You, Const(1))),
            // Into the discard pile, every one (§2.4) — then the pot is empty and she shelters again. Unless she
            // never washes the pot, or Yesterday's Jar keeps the last one, or one waits in reserve.
            new ConditionalEffectNode<CardPlayContext>(Wears(WitchRules.NeverWashThePot), PickOneToKeep()),
            new ConditionalEffectNode<CardPlayContext>(All(Wears(WitchRules.YesterdaysJar), Unspent(WitchRules.JarLatch)),
                new CausalSequenceEffectNode<CardPlayContext>(
                [
                    Spend(WitchRules.JarLatch),
                    CombatProgramModel.Build<CardPlayContext>(new CombatNodeModel("setCardInstanceMarkCounter", "source",
                        CombatAmountSpec.FromConst(1), Card: new CombatCardSpec("inZone", CardZone.SetAsidePile, Index: Slots - 1),
                        CounterId: Kept.value, Relative: false)).Root,
                ])),
            EmptyThePot(),
            new SetCombatantCounterNode<CardPlayContext>(You, WitchKeywords.BrewsThisTurn, Const(1), relative: true),
            new SetCombatantCounterNode<CardPlayContext>(You, StrainerChoice, Const(0), relative: false),
        ])),
    };

    private static readonly string[] FamilyNames = ["Fang", "Hex", "Husk", "Hearth", "Fortune"];

    // Bone Strainer's choice: 1..5 for the family the Dregs count as, 0 for none.
    private static CounterId StrainerChoice => new("bone_strainer_choice");

    // What in the recipe has no family at all — Junk and anything else the pot was given.
    private static ICombatExpression<CardPlayContext, int> Dregs =>
        Families.Aggregate(InRecipe(), (left, family) => new SubtractExpression<CardPlayContext>(left, InRecipe(family)));

    private static ICombatExpression<CardPlayContext, int> DistinctFamilies =>
        Families.Select(f => (ICombatExpression<CardPlayContext, int>)new MinExpression<CardPlayContext>(Const(1), InRecipe(f)))
            .Aggregate((a, b) => new AddExpression<CardPlayContext>(a, b));

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
                Compare(InRecipe(family), ComparisonOperator.GreaterOrEqual, Const(Slots)), effect, @else: brew);
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

    // Only when that family is in the brew: a 0-damage hit is still a hit to everything that counts hits.
    private static IEffectNode<CardPlayContext> Each(string family, IEffectNode<CardPlayContext> effect) =>
        new ConditionalEffectNode<CardPlayContext>(Compare(Weight(family), ComparisonOperator.Greater, Const(0)), effect);

    // What a family is worth in a mixed brew: its cards, one more for each that counts double, and the Dregs if
    // Bone Strainer named this family.
    private static ICombatExpression<CardPlayContext, int> Weight(string family) =>
        new AddExpression<CardPlayContext>(
            new AddExpression<CardPlayContext>(InRecipe(family), InRecipe(Double(family))),
            new MultiplyExpression<CardPlayContext>(Dregs, Named(family)));

    // 1 when the strainer's choice is this family, 0 otherwise.
    private static ICombatExpression<CardPlayContext, int> Named(string family) =>
        new SubtractExpression<CardPlayContext>(Const(1),
            new MinExpression<CardPlayContext>(Const(1),
                new AbsExpression<CardPlayContext>(new SubtractExpression<CardPlayContext>(
                    new CombatantCounterExpression<CardPlayContext>(You, StrainerChoice),
                    Const(Array.IndexOf(Families, family) + 1)))));

    private static ICombatExpression<CardPlayContext, int> Times(string family, int each) =>
        new MultiplyExpression<CardPlayContext>(Weight(family), Const(each));

    private static IEffectNode<CardPlayContext> Damage(ICombatExpression<CardPlayContext, int> amount) =>
        new DealDamageNode<CardPlayContext>(Target, amount);

    private static IEffectNode<CardPlayContext> Apply(
        string status, ICombatExpression<CardPlayContext, int> stacks, ICombatantTargetSelector to) =>
        new ApplyStatusNode<CardPlayContext>(to, new StatusDefinitionId(status), stacks);

    // A card kept in the pot after a Brew carries a mark counter (Never Wash the Pot, Yesterday's Jar); the reserve
    // carries its mark. Everything else goes to the discard pile, and both marks are wiped.
    private static CounterId Kept => new("kept_in_the_pot");

    private static IEffectNode<CardPlayContext> PickOneToKeep() =>
        CombatProgramModel.Build<CardPlayContext>(new CombatNodeModel("setCardInstanceMarkCounter", "source",
            CombatAmountSpec.FromConst(1),
            Card: new CombatCardSpec("chosen", CardZone.SetAsidePile, Purpose: "keep one in the cauldron"),
            CounterId: Kept.value, Relative: false)).Root;

    private static IEffectNode<CardPlayContext> EmptyThePot()
    {
        var card = new IteratedCardExpression<CardPlayContext>();
        return new CausalSequenceEffectNode<CardPlayContext>(
        [
            new ForEachCardInZoneNode<CardPlayContext>(You, CardZone.SetAsidePile,
                new ConditionalEffectNode<CardPlayContext>(
                    new AndExpression<CardPlayContext>(
                        Compare(new CardInstanceMarkCounterExpression<CardPlayContext>(card, Kept), ComparisonOperator.Equal, Const(0)),
                        new NotExpression<CardPlayContext>(new CardInstanceHasMarkExpression<CardPlayContext>(card, new TagId(ReserveMark)))),
                    new MoveCardToZoneNode<CardPlayContext>(You, card, CardZone.DiscardPile),
                    @else: new SetCardInstanceMarkCounterNode<CardPlayContext>(You, card, Kept, Const(0)))),
            // The reserve is a plain ingredient of the new pot now.
            new ForEachCardInZoneNode<CardPlayContext>(You, CardZone.SetAsidePile,
                new MarkCardInstanceNode<CardPlayContext>(You, card, new TagId(ReserveMark), remove: true)),
        ]);
    }

    // Shut the Lid: nothing goes in or comes out until her next turn.
    private static ICombatExpression<CardPlayContext, bool> LidOpen =>
        new NotExpression<CardPlayContext>(
            new TargetHasStatusExpression<CardPlayContext>(You, new StatusDefinitionId(WitchRules.ShutTheLid)));

    // Hearth: heal HP lost during THIS combat only — never above the HP she began the fight on (§3.4). What the cap
    // would waste may become Ward Wax first (Keep the Drippings).
    public static IEffectNode<CardPlayContext> Heal(ICombatExpression<CardPlayContext, int> amount) =>
        WitchRules.Heal(You, amount);
}
