using RogueDeck.Core.Combat;
using RogueDeck.Scenario.Authoring;

namespace BnbContent.Converter.Apprentice;

// The wand's rulebook as this game writes it (master §3, every number a BALANCE DRAFT) and the Apprentice's five
// actions on it. The actions are ACTIONS (CardDefinition.IsAction): used, not drawn, never a card played; each is
// refused, with its reason, until it can be taken (WandCanExpression).
public static class ApprenticeWand
{
    public const string Inscribe = "wand_inscribe";
    public const string Cast = "wand_cast";
    public const string Swap = "wand_swap";
    public const string Withdraw = "wand_withdraw";
    public const string BurnOut = "wand_burn_out";
    public const string ActionTag = "wand_action";

    public static IReadOnlyList<string> Actions => [Inscribe, Cast, Swap, Withdraw, BurnOut];

    public const int MaxFocus = 6;

    public static WandRules Rules => new()
    {
        Bearer = new StatusDefinitionId(ApprenticeKeywords.Wand),
        MaxFocus = MaxFocus,
        NoFocusTags = [new TagId(Cards.CardAuthoring.JunkTag), new TagId(Cards.CardAuthoring.UnplayableTag)],
        UninscribableTags = [new TagId(Cards.CardAuthoring.JunkTag)],
        TemporaryTag = new TagId(Cards.CardAuthoring.TemporaryTag),
        Singe = new StatusDefinitionId(ApprenticeKeywords.Singe),
        Flow = new StatusDefinitionId(ApprenticeKeywords.Flow),
        Grit = new StatusDefinitionId(ApprenticeKeywords.Grit),
        Gust = new StatusDefinitionId(ApprenticeKeywords.Gust),
        Rote = new StatusDefinitionId(ApprenticeKeywords.Rote),
        Poise = new StatusDefinitionId(ApprenticeKeywords.Poise),
        WardWax = new StatusDefinitionId(Cards.Keywords.WardWax),
        Hooks = ApprenticeRules.Hooks,
        CastBoosts = ApprenticeRules.CastBoosts,
    };

    public static IReadOnlyList<CardData> All() =>
    [
        Action(Inscribe, "Inscribe", WandAction.Inscribe,
            "Put a card from your hand into a free wand slot instead of playing it. The first each combat is free, " +
            "then 1 Focus."),
        Action(Cast, "Cast", WandAction.Cast,
            "Read the wand from its first slot to its last, each card doing its wand part. Costs 2 Focus for 1–2 " +
            "cards, 3 for 3–4, 4 for 5–6, and adds Exhaustion."),
        Action(Swap, "Swap", WandAction.Swap, "Two wand cards trade places. 1 Focus."),
        Action(Withdraw, "Withdraw", WandAction.Withdraw, "A wand card goes to your discard pile. 1 Focus."),
        Action(BurnOut, "Burn Out", WandAction.BurnOut,
            "Once per turn, while the wand has Exhaustion: Exhaust a wand card and lower Exhaustion by 3."),
    ];

    private static CardData Action(string id, string name, WandAction action, string text) => new()
    {
        Id = id,
        NameKey = name,
        DescriptionKey = text,
        IsAction = true,
        Tags = [new TagId(ActionTag)],
        PlayCondition = new WandCanExpression<CardPlayContext>(action),
        Program = new EffectProgram<CardPlayContext>(new WandNode<CardPlayContext>(action)),
    };
}
