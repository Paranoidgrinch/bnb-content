using RogueDeck.Core.Combat;
using RogueDeck.Run;

namespace BnbContent.Converter;

// The shared random pools every mapper draws offers from: the card-reward pool (Act-1 bureaucrat
// commons/uncommons/rares, uniform weight in Act 1), the transform pool (same cards), and the
// event-relic pool (non-boss relics allowed for the bureaucrat), each relic offer bundling its
// pickup effects (see RelicMapper).
public sealed class ConversionPools
{
    public required IReadOnlyList<BabCard> RewardCards { get; init; }
    public required IReadOnlyList<MappedRelic> Relics { get; init; }

    public static ConversionPools Build(BabData data, IReadOnlyList<MappedRelic> relics) => new()
    {
        RewardCards = data.Cards
            .Where(c => c.CardClass == data.Bureaucrat.Id
                && c.Rarity is "common" or "uncommon" or "rare"
                && !(c.Tags ?? []).Contains("upgraded"))
            .ToList(),
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

    // One relic offer: grant the relic + its bundled pickup effects.
    public static RewardOffer RelicOffer(MappedRelic mapped) => new(
        $"relic-{mapped.Relic.Id}",
        new IRunEffectRequest[] { new AddRelicByIdRunEffect(new RelicId(mapped.Relic.Id)) }
            .Concat(mapped.PickupEffects).ToArray());

    public static RewardOffer CardOffer(BabCard card) => new(
        $"card-{CardMapper.MapCardId(card.Id)}",
        [new AddCardToDeckRunEffect(new CardDefinitionId(CardMapper.MapCardId(card.Id)))]);

    // Post-fight card reward: 3 random pool cards, pick 1 (uniform weight in Act 1).
    public IRewardSource CardRewardSource(int count = 3) => new PoolRewardSource(
        new RunPool<RewardOffer>(RewardCards.Select(c => new RunPool<RewardOffer>.Entry(CardOffer(c), 1)).ToList()),
        count);

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
        RewardCards.Select(c => new RunPool<CardDefinitionId>.Entry(
            new CardDefinitionId(CardMapper.MapCardId(c.Id)), 1)).ToList());
}
