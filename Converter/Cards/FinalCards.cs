using RogueDeck.Core.Combat;
using RogueDeck.Scenario.Authoring;
using BnbCard = BnbContent.Converter.Cards.CardAuthoring.BnbCard;

namespace BnbContent.Converter.Cards;

// The final card pool: everything the design sheets in source-data/design define, in one place.
//
// It REPLACES the ported v2 cards wherever the ids meet (paper_cut, permit_a38, the Junk), and stands beside
// what is not a player reward card at all (the boss-given cards in ClauseCards/NoticeCards and friends).
// The v2 cards that nothing final has replaced yet are still emitted, because the ported EVENTS still name
// some of them; they go when Phase D replaces those events.
public static class FinalCards
{
    // Starters and Junk first; the 80 Bureaucrat and 50 general reward cards join them act by act.
    public static IReadOnlyList<BnbCard> All() =>
    [
        .. BureaucratStarter.All(),
        .. BureaucratActI.All(),
        .. GeneralActI.All(),
    ];

    public static IReadOnlyList<CardData> Compile() => All().Select(c => c.Compile()).ToList();

    // The statuses the final cards install or lean on: the keyword substrate plus the Rites, whose rule
    // lives on a status rather than in the card that plays it.
    public static IReadOnlyList<StatusData> Statuses() =>
        [.. Keywords.All(), .. BureaucratRites.All(), .. GeneralRites.All(), .. GeneralForgery.All()];

    // Ids the final pool owns — a ported card with one of these is dropped rather than duplicated.
    public static IReadOnlySet<string> Ids() => All().Select(c => c.Id).ToHashSet();

    // The reward pool, by the cumulative Act gate the design sheets give: reaching Act N makes every card
    // gated at N or earlier offerable. Starters and Junk are never rewards.
    public static IReadOnlyList<BnbCard> RewardPool(int act) => All()
        .Where(c => c.Rarity is "common" or "uncommon" or "rare" && c.Act <= act && !c.Id.EndsWith('+'))
        .ToList();
}
