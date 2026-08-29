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
        .. BureaucratActII.All(),
        .. GeneralActII.All(),
        .. BureaucratActIII.All(),
        .. GeneralActIII.All(),
        .. ActIVCards.All(),
    ];

    public static IReadOnlyList<CardData> Compile() => All().Select(c => c.Compile()).ToList();

    // The statuses the final cards install or lean on: the keyword substrate plus the Rites, whose rule
    // lives on a status rather than in the card that plays it.
    public static IReadOnlyList<StatusData> Statuses() =>
    [
        .. Keywords.All(), .. BureaucratRites.All(), .. BureaucratArchive.All(),
        .. BureaucratHistory.All(), .. ActIVRites.All(), .. GeneralWax.All(),
        .. GeneralRites.All(), .. GeneralForgery.All(), .. GeneralPrevention.All(),
    ];

    // Ids the final pool owns — a ported card with one of these is dropped rather than duplicated.
    public static IReadOnlySet<string> Ids() => All().Select(c => c.Id).ToHashSet();

    // The reward pool, by the cumulative Act gate the design sheets give: reaching Act N makes every card
    // gated at N or earlier offerable. Starters and Junk are never rewards.
    public static IReadOnlyList<BnbCard> RewardPool(int act) => Offerable(All(), act);

    // The same pool, split the way a shop asks for it (BnB_Run_Systems_Master §2.1/§2.2): the General pool is
    // character-unspecific and every future character keeps it, the Character pool is the played character's
    // own — today the Bureaucrat's. The two partition RewardPool exactly; nothing is in both and nothing is
    // in neither, which is what the WHICH POOL a card belongs to means. It is not a field on the card because
    // it is not a property a card was given — it is which sheet it was written on, and that is the file it
    // lives in.
    public static IReadOnlyList<BnbCard> GeneralPool(int act) => Offerable(
        [.. GeneralActI.All(), .. GeneralActII.All(), .. GeneralActIII.All(), .. ActIVCards.General()], act);

    public static IReadOnlyList<BnbCard> CharacterPool(int act) => Offerable(
        [.. BureaucratActI.All(), .. BureaucratActII.All(), .. BureaucratActIII.All(),
         .. ActIVCards.Bureaucrat()], act);

    // Starters and Junk are never rewards, and neither is an upgraded twin — the "+" version is what an
    // improvement makes, not what a shelf sells.
    private static IReadOnlyList<BnbCard> Offerable(IEnumerable<BnbCard> cards, int act) => cards
        .Where(c => c.Rarity is "common" or "uncommon" or "rare" && c.Act <= act && !c.Id.EndsWith('+'))
        .ToList();
}
