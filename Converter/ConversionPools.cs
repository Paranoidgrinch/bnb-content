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

    // Post-fight card reward: 3 pool cards on the ACT'S rarity curve, pick 1.
    //
    // It was a uniform draw until 2026-09-10 — its own comment said "uniform weight in Act 1" — so an Act-I
    // reward was as likely to hand over a Rare as a Common, and an Act-IV one no likelier to. Acts have GATES
    // (which cards enter the pool at all); what they did not have was a CURVE. Same table as the relics use,
    // and the same trick for applying it: each entry of a rarity is weighted by that rarity's share times the
    // size of every other class in the draw, so the class odds hold however many cards sit in each class.
    public IRewardSource CardRewardSource(int count = 3)
    {
        var (common, uncommon, rare) = RarityCurve[Math.Clamp(Act, 1, 5)];
        var classes = new[] { ("common", common), ("uncommon", uncommon), ("rare", rare) }
            .Select(c => (c.Item2, Cards: RewardCards.Where(card => card.Rarity == c.Item1).ToList()))
            .Where(c => c.Cards.Count > 0)
            .ToList();
        if (classes.Count == 0)
            throw new ConversionException($"act {Act} card pool", "holds no card of any known rarity");

        var entries = new List<RunPool<RewardOffer>.Entry>();
        foreach (var (share, cards) in classes)
        {
            var others = classes.Where(c => !ReferenceEquals(c.Cards, cards))
                .Aggregate(1, (product, c) => product * c.Cards.Count);
            foreach (var card in cards)
                entries.Add(new RunPool<RewardOffer>.Entry(CardOffer(card), share * others));
        }
        // Anything the pool holds that carries an unknown rarity still has to be offerable, or a card would
        // fall out of the game by being labelled wrongly. It draws at the Common share.
        foreach (var card in RewardCards.Where(c => c.Rarity is not ("common" or "uncommon" or "rare")))
            entries.Add(new RunPool<RewardOffer>.Entry(CardOffer(card), common));

        return new PoolRewardSource(new RunPool<RewardOffer>(entries), count);
    }

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

    // ── the act's rarity curve ────────────────────────────────────────────────────────────────────────────

    // WHAT AN ACT IS LIKELY TO HAND YOU, as one table for cards and relics alike. Until 2026-09-10 there was
    // no such curve anywhere in this game: acts have GATES (which cards and relics may appear at all) and
    // within a gated pool every entry was equally likely — the card reward's own comment said so outright,
    // "uniform weight in Act 1". The design deferred the numbers (BnB_Run_Systems_Master §"card reward rarity
    // probabilities" sits under the balance variables), so these are the user's, ratified 2026-09-10, and
    // they are the kind of number a balance pass is expected to move.
    //
    // The shares are per-CLASS odds, not per-entry weights: NormalRelicOfRarity turns them into weights that
    // hold whatever number of relics happens to sit in each class, and CardRewardSource does the same for
    // cards. That is what keeps the curve honest as pools grow.
    public static readonly IReadOnlyDictionary<int, (int Common, int Uncommon, int Rare)> RarityCurve =
        new Dictionary<int, (int, int, int)>
        {
            [1] = (65, 30, 5),
            [2] = (45, 40, 15),
            [3] = (30, 45, 25),
            [4] = (20, 45, 35),
            // Act V hands out no cards and no relics; the entry exists so a lookup cannot throw on the way
            // to a gauntlet that asks for neither.
            [5] = (20, 45, 35),
        };

    public (Relics.RelicAuthoring.Rarity Rarity, int Share)[] ActCurve()
    {
        var (common, uncommon, rare) = RarityCurve[Math.Clamp(Act, 1, 5)];
        // Fully qualified: `Relics` is a PROPERTY on this class (the ported list), and inside a method body
        // it shadows the namespace of the same name.
        return
        [
            (BnbContent.Converter.Relics.RelicAuthoring.Rarity.Common, common),
            (BnbContent.Converter.Relics.RelicAuthoring.Rarity.Uncommon, uncommon),
            (BnbContent.Converter.Relics.RelicAuthoring.Rarity.Rare, rare),
        ];
    }

    // A Normal relic drawn on the ACT'S curve — what a treasure chest and a standard relic reward hand over.
    public IRewardSource NormalRelicOnTheCurve(string where) => NormalRelicOfRarity(where, ActCurve());

    // What a SHOP-LIKE EVENT may stock: "eligible Normal or Shop Relics under standard Shop eligibility,
    // Event and Boss relics excluded" (master §1, and §Shop Relics names the three markets by name — the
    // Licensed Vendor, the Conceptual Toll, the Travelling Chandler). Until 2026-09-10 all three read
    // `Relics`, which is the PORTED v2 list, so the only relics ever sold at a fair were demo material.
    public IReadOnlyList<Relics.RelicAuthoring.BnbRelic> MarketRelicStock =>
        [.. NormalRelicStock, .. ShopRelicStock];

    // "A random eligible Common / Uncommon / Rare NORMAL relic", which is the Labyrinth's doors' own
    // wording — the Normal pool, not the event-exclusive one, gated by rarity. `mix` is the design's odds
    // between rarities ("60% Uncommon / 40% Rare"): each entry of a rarity is weighted by that rarity's
    // share times the SIZE of every other rarity in the draw, so the class odds are what the design wrote
    // however many relics happen to sit in each class.
    public IRewardSource NormalRelicOfRarity(string where, params (Relics.RelicAuthoring.Rarity Rarity, int Share)[] mix)
    {
        ArgumentNullException.ThrowIfNull(mix);
        var classes = mix
            .Select(m => (m.Share, Relics: NormalRelicStock.Where(r => r.Rarity == m.Rarity).ToList()))
            .ToList();
        foreach (var (_, relics) in classes)
            if (relics.Count == 0)
                throw new ConversionException(where, "the normal relic pool holds no relic of a named rarity");

        var entries = new List<RunPool<RewardOffer>.Entry>();
        foreach (var (share, relics) in classes)
        {
            var others = classes.Where(c => !ReferenceEquals(c.Relics, relics))
                .Aggregate(1, (product, c) => product * c.Relics.Count);
            foreach (var relic in relics)
                entries.Add(new RunPool<RewardOffer>.Entry(
                    new RewardOffer($"relic-{relic.Id}", [.. Grant(relic)]), share * others));
        }
        return new PoolRewardSource(new RunPool<RewardOffer>(entries), 1);
    }

    // Transform target pool: any reward-pool card (uniform), as the original draws its replacement
    // from the card-reward chooser.
    public RunPool<CardDefinitionId> TransformPool() => new(
        RewardCards.Select(c => new RunPool<CardDefinitionId>.Entry(new CardDefinitionId(c.Id), 1)).ToList());
}
