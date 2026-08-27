using RogueDeck.Core.Combat;
using RogueDeck.Run;
using RogueDeck.Scenario.Authoring;

namespace BnbContent.Converter;

// Assembles the full RunBlueprint: every mapped section, the act's MAP RULES (the engine generates a fresh
// layout per run), the character roster, the presentation manifest. One call, deterministic per seed.
public static class BlueprintAssembler
{
    // The GAME's title. Which act the player is in is the act's own NameKey ("Act I: The Old City Offices",
    // "Act II: The Endless Archives"), carried per act in RunBlueprint.Acts — a run crosses more than one, so
    // the title cannot name one of them.
    public const string GameTitle = "Bureaucrats & Broomsticks";

    public static RunBlueprint Build(BabData data, int seed)
    {
        var relics = data.Relics.Select(RelicMapper.Map).ToList();
        // One set of card pools and one set of map rules PER ACT, each drawing only from its own act. The run
        // is one walk across all of them: the engine lays every act out at run start (RunSetup.BuildActPlan,
        // its own seed per act) and advances by itself when an act's boss falls.
        var pools = data.Acts.ToDictionary(a => a.Act, a => ConversionPools.Build(data, relics, a.Act));
        // The events an act AUTHORS (Act I's fifteen), built before its map so the map can draw from them.
        var authored = data.Acts.ToDictionary(
            a => a.Act, a => Events.AuthoredEvents.For(a.Act, pools[a.Act], new Random(seed + a.Act)));
        var maps = data.Acts
            .Select((act, index) =>
                (Act: act, Map: MapSpecBuilder.Build(data, pools[act.Act], seed + index, act,
                    authored[act.Act])))
            .ToList();

        // Only enemies an encounter actually fields contribute action definitions.
        var referencedEnemies = data.Encounters.SelectMany(e => e.Enemies).ToHashSet();
        var enemies = data.Enemies.Where(e => referencedEnemies.Contains(e.Id)).ToList();
        var enemiesById = data.Enemies.ToDictionary(e => e.Id);

        // Every door in the game is authored now — the ported v2 event JSON is gone, and with it the mapper
        // that converted it. What is left here is the act's own fifteen plus the furniture its map builds
        // (the waiting room, the treasure rooms), each offering what its OWN act offers.
        var events = new Dictionary<string, EventScript>();
        foreach (var authoredEvent in authored.Values.SelectMany(e => e))
            events[authoredEvent.Id] = authoredEvent.Script;
        foreach (var (_, actMap) in maps)
            foreach (var (id, script) in actMap.Events)
                events[id] = script;

        var start = new RunStart
        {
            HeroName = data.Bureaucrat.Name,
            MaxHealth = data.Bureaucrat.MaxHp,
            StartingHealth = data.Bureaucrat.MaxHp,
            Resources = new Dictionary<string, int> { [StandardRunIds.Gold.Value] = 0 },
            Deck = data.Bureaucrat.StartingDeck
                .Select(id => new CardDefinitionId(CardMapper.MapCardId(id))).ToList(),
        };

        var blueprint = new RunBlueprint(
            data.Bureaucrat.StartingDeck.Select(id => new CardDefinitionId(CardMapper.MapCardId(id))).ToList(),
            events,
            data.Encounters.Select(e => EncounterMapper.Map(e, enemiesById, data.Bureaucrat.StartingEnergy)).ToList(),
            // The final pool wins wherever the ids meet; the ported v2 cards it has not replaced yet stay,
            // because the ported events still name some of them.
            [
                .. data.Cards.Where(c => !Cards.FinalCards.Ids().Contains(CardMapper.MapCardId(c.Id)))
                    .Select(CardMapper.Map),
                .. Cards.FinalCards.Compile(),
                // The events' temporary cards: never dealt into a deck, only pushed into a fight.
                .. Events.ActOneEventObjects.Compile(), .. Events.ActTwoEventObjects.Compile(),
                .. ActThree.GivenCards(),
                .. ClauseCards.Cards(), NoticeCards.Acknowledge(), DeputyUndersecretary.ReviewCard(),
                .. QueueCommissioner.Cards(), .. LordSealkeeper.Cards(), .. MunicipalDragon.Cards(),
                .. LivingCharter.Cards(), .. Elites.ReturnBell.Cards(),
                .. Bosses.CuratorOfMisplacedHours.Cards(),
            ],
            EnemyMapper.MapActions(enemies).ToList(),
            // The act's map is GENERATED per run from MapGeneration below; the authored map stays empty.
            new RunMap([]))
        {
            // The first act's rules stay on the blueprint as well: they are the fallback for anything that asks
            // a document for "the" map (and what a one-act reader sees), while Acts below is what a run walks.
            MapGeneration = maps[0].Map.Spec,
            Acts = maps
                .Select(m => new RunAct(m.Act.Id, m.Map.Spec, NameKey: m.Act.Name))
                .ToList(),
            Statuses =
            [
                .. StatusMapper.Map("statuses", data.Statuses),
                .. Cards.FinalCards.Statuses(),
                .. Relics.FinalRelics.Statuses(),
                .. PassiveStatuses.All(),
                .. ActTwo.All(),
                .. ActThree.All(),
                .. Events.ActOneEventObjects.Statuses(), .. Events.ActTwoEventObjects.Statuses(),
            ],
            // The final relic pools replace the ported v2 relics wherever the ids meet; what is left of the
            // old pool still ships because the ported EVENTS grant some of it by name.
            Relics =
            [
                .. relics.Select(r => r.Relic).Where(r => !Relics.FinalRelics.All().Any(f => f.Id == r.Id)),
                .. Relics.FinalRelics.Compile(),
            ],
            Shops = maps.SelectMany(m => m.Map.Shops).ToDictionary(e => e.Key, e => e.Value),
            // What the authored events promise for AFTER a fight. The bodies live here once; the events that
            // hand them out name them (fx.installProgramById).
            // Both acts' promise bodies in one dictionary. An act's programs are its OWN — the extra card
            // reward draws from the act's pool — so a run that walks both carries both sets, under names that
            // say which act made the promise.
            Programs = AuthoredPrograms(pools),
            Start = start,
            Characters = [new RunCharacter(data.Bureaucrat.Id, start)],
            // Victory now means the WHOLE run — the city and the archives behind it. (The engine's meta rules
            // key off the run's result; a per-act flag would need an act-completed hook that is not data yet.)
            MetaRules = [new MetaRule([RunResult.Victory], [new SetMetaFlag("bnb.run.cleared")])],
            Presentation = BuildPresentation(
                data, relics, enemies, [.. authored.Values.SelectMany(e => e)]),
        };

        // …and then let anything the manifest did not name explain itself from its own rules text.
        return blueprint with { Presentation = WithEveryCard(blueprint.Presentation, blueprint.Cards) };
    }

    // Every authored run program the document ships: the bodies an event installs by name.
    private static IReadOnlyDictionary<string, ITriggeredRunEffectDefinition>? AuthoredPrograms(
        IReadOnlyDictionary<int, ConversionPools> pools)
    {
        var programs = new Dictionary<string, ITriggeredRunEffectDefinition>();
        if (pools.TryGetValue(Events.ActOneEvents.Act, out var actOne))
            foreach (var (id, body) in Events.ActOneEventPrograms.All(actOne))
                programs[id] = body;
        if (pools.TryGetValue(Events.ActTwoEvents.Act, out var actTwo))
            foreach (var (id, body) in Events.ActTwoEventPrograms.All(actTwo))
                programs[id] = body;
        return programs.Count > 0 ? programs : null;
    }

    // Anything the document ships that the manifest above did not name explains itself from its OWN
    // DescriptionKey. That is what the encounter-given cards have — a Notice, a Clause, a Fragment, a boss's
    // action card — and they are the cards a player meets without warning, so they are the last ones that
    // should reach the hand unexplained.
    private static PresentationManifest WithEveryCard(PresentationManifest manifest, IReadOnlyList<CardData> cards)
    {
        var byId = new Dictionary<string, EntityPresentation>(manifest.Cards, StringComparer.Ordinal);
        foreach (var card in cards)
        {
            if (byId.TryGetValue(card.Id, out var known) && !string.IsNullOrWhiteSpace(known.FlavorText))
                continue;
            if (string.IsNullOrWhiteSpace(card.DescriptionKey))
                continue;
            byId[card.Id] = new EntityPresentation
            {
                Art = $"cards/{card.Id.TrimEnd('+')}.png",
                FlavorText = card.DescriptionKey,
                Tags = [.. card.Tags.Select(t => t.value)],
            };
        }
        return manifest with { Cards = byId };
    }

    private static PresentationManifest BuildPresentation(
        BabData data, IReadOnlyList<MappedRelic> relics, IReadOnlyList<BabEnemy> enemies,
        IReadOnlyList<Events.BnbEvent> authored) => new()
        {
            Cards = data.Cards
                .Where(c => !Cards.FinalCards.Ids().Contains(CardMapper.MapCardId(c.Id)))
                .ToDictionary(
                    c => CardMapper.MapCardId(c.Id),
                    c => new EntityPresentation
                    {
                        Art = $"cards/{c.Id}.png",
                        FlavorText = c.Text,
                        Rarity = c.Rarity,
                        Tags = (c.Tags ?? []).Append(c.Type).ToList(),
                    })
                .Concat(Cards.FinalCards.All().ToDictionary(
                    c => c.Id,
                    c => new EntityPresentation
                    {
                        Art = $"cards/{c.Id.TrimEnd('+')}.png",
                        // The engine has no rules-text renderer: a card's ability text IS presentation, and
                        // this is what both UIs show on a reward or in the hand.
                        FlavorText = c.Text,
                        Rarity = c.Rarity,
                        Tags = c.AllTags.ToList(),
                    }))
                .ToDictionary(e => e.Key, e => e.Value),
            // Both kinds of relic, because both are worn: the ported ones carry their text in the source data,
            // the AUTHORED ones (the final pools — normal, shop, boss, event) carry it on the authoring record.
            // Only the ported ones used to get an entry, so two thirds of the relics in the game showed the
            // player a name and nothing else on hover.
            Relics = relics
                .ToDictionary(
                    r => r.Relic.Id,
                    r => new EntityPresentation
                    {
                        Art = $"relics/{r.Relic.Id}.png",
                        FlavorText = r.Source.Description,
                        Rarity = r.Source.Rarity,
                        Tags = r.Source.Tags ?? [],
                    })
                .Concat(Relics.FinalRelics.All().ToDictionary(
                    r => r.Id,
                    r => new EntityPresentation
                    {
                        Art = $"relics/{r.Id}.png",
                        FlavorText = r.Text,
                        Rarity = r.Rarity.ToString().ToLowerInvariant(),
                        Tags = [],
                    }))
                // An id in both places is the authored one: that is the relic the document ships.
                .GroupBy(e => e.Key)
                .ToDictionary(g => g.Key, g => g.Last().Value),
            Statuses = data.Statuses.ToDictionary(
                s => s.Id,
                s => new EntityPresentation
                {
                    Icon = $"statuses/{s.Id}.png",
                    FlavorText = s.Description,
                    Tags = s.Tags ?? [],
                }),
            Enemies = enemies.ToDictionary(
                e => e.Id,
                e => new EntityPresentation
                {
                    Art = $"enemies/{e.Id}.png",
                    Tags = e.Tags ?? [],
                }),
            Encounters = data.Encounters.ToDictionary(
                e => e.Id,
                e => new EntityPresentation
                {
                    FlavorText = e.Name,
                    Tags = [e.Difficulty, .. e.Tags ?? []],
                }),
            Events = authored.ToDictionary(
                a => a.Id,
                a => new EntityPresentation { FlavorText = a.Name, Tags = a.Tags ?? [] }),
            Characters = new Dictionary<string, EntityPresentation>
            {
                [data.Bureaucrat.Id] = new()
                {
                    Art = $"characters/{data.Bureaucrat.Id}.png",
                    FlavorText = "Armed with forms, stamps, and a fireproof sense of procedure.",
                },
            },
            Game = new EntityPresentation
            {
                Art = "title.png",
                FlavorText = GameTitle,
            },
        };
}
