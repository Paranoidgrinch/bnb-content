using RogueDeck.Core.Combat;
using RogueDeck.Run;

using BnbContent.Converter.Relics;
using static BnbContent.Converter.Relics.RelicAuthoring;

namespace BnbContent.Converter;

// The shared random pools every mapper draws offers from: the card-reward pool, the transform pool (the same
// cards), and the event-relic pool (non-boss relics allowed for the bureaucrat), each relic offer bundling
// its pickup effects (see RelicMapper).
//
// The reward pool is the FINAL Bureaucrat pool, gated by Act as the design sheet gates it: reaching Act N
// makes every card gated at N or earlier offerable. Starters and Junk are never offered. Rarity weighting is
// a balance question and is deliberately still flat — see the design's deferred balance pass.
//
// There is one instance PER ACT, and every offer an act makes — its fights' card rewards, its shop's shelves,
// its treasure rooms, the events that belong to it — is built from that act's pool. An Act-II fight handing
// out Act-I commons would make the archives read like the city.
public sealed class ConversionPools
{
    public required int Act { get; init; }

    public required IReadOnlyList<Cards.CardAuthoring.BnbCard> RewardCards { get; init; }
    public required IReadOnlyList<MappedRelic> Relics { get; init; }

    // The four pools a SHOP asks for by name (BnB_Run_Systems_Master §4.2/§4.3). A shop does not draw from
    // "the cards" and "the relics" — it fills three General slots, four Character slots, two Shop-relic slots
    // and two Normal-relic slots, each from its own pool. Kept apart here because the moment they are one bag
    // the shelf can no longer be the shape the design fixed.
    public required IReadOnlyList<Cards.CardAuthoring.BnbCard> GeneralCards { get; init; }
    public required IReadOnlyList<Cards.CardAuthoring.BnbCard> CharacterCards { get; init; }
    public required IReadOnlyList<BnbRelic> ShopRelicStock { get; init; }
    public required IReadOnlyList<BnbRelic> NormalRelicStock { get; init; }

    public static ConversionPools Build(BabData data, IReadOnlyList<MappedRelic> relics, int act) => new()
    {
        Act = act,
        RewardCards = Cards.FinalCards.RewardPool(act),
        GeneralCards = Cards.FinalCards.GeneralPool(act),
        CharacterCards = Cards.FinalCards.CharacterPool(act),
        // The final relic pools, gated by who is being played. Neither can hold an Event or a Boss relic —
        // those are separate pools by construction (RelicAuthoring.Pool), which is how §2.5/§2.6 keeps them
        // out of a shop rather than by a filter that could be forgotten.
        ShopRelicStock = FinalRelics.Pool(Pool.Shop),
        NormalRelicStock = FinalRelics.Pool(Pool.Normal),
        Relics = relics
            .Where(r => EligibleForEvents(r, data.Bureaucrat.Id))
            .ToList(),
    };

    private static bool EligibleForEvents(MappedRelic mapped, string classId)
    {
        var source = mapped.Source;
        return source.Rarity != "boss"
            && (source.AllowedClasses is null || source.AllowedClasses.Contains(classId));
    }

    // One final relic's grant: the relic, plus whatever it does the moment it is taken. The engine has no
    // per-relic pickup hook, so every place that hands one over carries them — a shelf included.
    public static IReadOnlyList<IRunEffectRequest> Grant(BnbRelic relic) =>
        [new AddRelicByIdRunEffect(new RelicId(relic.Id)), .. relic.Pickup ?? []];

    // One relic offer: grant the relic + its bundled pickup effects.
    public static RewardOffer RelicOffer(MappedRelic mapped) => new(
        $"relic-{mapped.Relic.Id}",
        new IRunEffectRequest[] { new AddRelicByIdRunEffect(new RelicId(mapped.Relic.Id)) }
            .Concat(mapped.PickupEffects).ToArray());

    public static RewardOffer CardOffer(
        Cards.CardAuthoring.BnbCard card, IReadOnlyList<string>? tags = null) => new(
        $"card-{card.Id}",
        [
            new AddCardToDeckRunEffect(new CardDefinitionId(card.Id)),
            // The card the deck just gained is the one the offer is about — the tag rides along with the take,
            // so a declined offer writes nothing.
            .. (tags ?? []).Select(tag => (IRunEffectRequest)new TagCardsRunEffect(
                RunSelectors.LastAddedCard, new RunCardTagId(tag), true)),
        ]);

    // Post-fight card reward: 3 random pool cards, pick 1 (uniform weight in Act 1).
    public IRewardSource CardRewardSource(int count = 3) => new PoolRewardSource(
        new RunPool<RewardOffer>(RewardCards.Select(c => new RunPool<RewardOffer>.Entry(CardOffer(c), 1)).ToList()),
        count);

    // A reward drawn from ONE rarity — "a Rare Card Reward", "choose 1 of 3 Uncommon cards". The archives' doors
    // ask for these by name, and a uniform draw from the whole act pool would quietly hand out commons instead.
    // `tags` are run card tags written on whatever the player takes, so an offer can BE the thing the event
    // promised ("choose one of three, and it starts the next fight in a Reservation") without a second prompt
    // that could land on the wrong card if the reward is declined.
    public IRewardSource CardRewardSource(string rarity, int count = 3, IReadOnlyList<string>? tags = null)
    {
        var eligible = RewardCards.Where(c => c.Rarity == rarity).ToList();
        if (eligible.Count == 0)
            throw new ConversionException($"act {Act} card pool", $"holds no '{rarity}' card to offer");
        return new PoolRewardSource(
            new RunPool<RewardOffer>(
                eligible.Select(c => new RunPool<RewardOffer>.Entry(CardOffer(c, tags), 1)).ToList()),
            count);
    }

    // Event relic grant: ONE random eligible relic (optionally tag-filtered), auto-taken.
    public IRewardSource RelicGrantSource(string? tag, string where)
    {
        var eligible = tag is null
            ? Relics
            : Relics.Where(r => (r.Source.Tags ?? []).Contains(tag)).ToList();
        if (eligible.Count == 0)
            throw new ConversionException(where, $"no event-eligible relics{(tag is null ? "" : $" with tag '{tag}'")}");
        return new PoolRewardSource(
            new RunPool<RewardOffer>(eligible.Select(r => new RunPool<RewardOffer>.Entry(RelicOffer(r), 1)).ToList()),
            1);
    }

    // Transform target pool: any reward-pool card (uniform), as the original draws its replacement
    // from the card-reward chooser.
    public RunPool<CardDefinitionId> TransformPool() => new(
        RewardCards.Select(c => new RunPool<CardDefinitionId>.Entry(new CardDefinitionId(c.Id), 1)).ToList());
}
