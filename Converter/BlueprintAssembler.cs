using RogueDeck.Core.Combat;
using RogueDeck.Run;
using RogueDeck.Scenario.Authoring;

namespace BnbContent.Converter;

// Assembles the full RunBlueprint: every mapped section, the act's MAP RULES (the engine generates a fresh
// layout per run), the character roster, the presentation manifest. One call, deterministic per seed.
public static class BlueprintAssembler
{
    public const string GameTitle = "Bureaucrats & Broomsticks — Act I: The Old City Offices";

    public static RunBlueprint Build(BabData data, int seed)
    {
        var relics = data.Relics.Select(RelicMapper.Map).ToList();
        var pools = ConversionPools.Build(data, relics);
        var map = MapSpecBuilder.Build(data, pools, seed);

        // Only enemies an encounter actually fields contribute action definitions.
        var referencedEnemies = data.Encounters.SelectMany(e => e.Enemies).ToHashSet();
        var enemies = data.Enemies.Where(e => referencedEnemies.Contains(e.Id)).ToList();
        var enemiesById = data.Enemies.ToDictionary(e => e.Id);

        var events = data.Events.ToDictionary(e => e.Id, e => EventMapper.Map(e, pools));
        foreach (var (id, script) in map.Events)
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

        return new RunBlueprint(
            data.Bureaucrat.StartingDeck.Select(id => new CardDefinitionId(CardMapper.MapCardId(id))).ToList(),
            events,
            data.Encounters.Select(e => EncounterMapper.Map(e, enemiesById, data.Bureaucrat.StartingEnergy)).ToList(),
            // The final pool wins wherever the ids meet; the ported v2 cards it has not replaced yet stay,
            // because the ported events still name some of them.
            [
                .. data.Cards.Where(c => !Cards.FinalCards.Ids().Contains(CardMapper.MapCardId(c.Id)))
                    .Select(CardMapper.Map),
                .. Cards.FinalCards.Compile(),
                .. ClauseCards.Cards(), NoticeCards.Acknowledge(), DeputyUndersecretary.ReviewCard(),
                .. QueueCommissioner.Cards(), .. LordSealkeeper.Cards(), .. MunicipalDragon.Cards(),
                .. LivingCharter.Cards(),
            ],
            EnemyMapper.MapActions(enemies).ToList(),
            // The act's map is GENERATED per run from MapGeneration below; the authored map stays empty.
            new RunMap([]))
        {
            MapGeneration = map.Spec,
            Statuses = [.. StatusMapper.Map("statuses", data.Statuses), .. Cards.FinalCards.Statuses(), .. PassiveStatuses.All()],
            Relics = relics.Select(r => r.Relic).ToList(),
            Shops = map.Shops,
            Start = start,
            Characters = [new RunCharacter(data.Bureaucrat.Id, start)],
            MetaRules = [new MetaRule([RunResult.Victory], [new SetMetaFlag("bnb.act1.cleared")])],
            Presentation = BuildPresentation(data, relics, enemies),
        };
    }

    private static PresentationManifest BuildPresentation(
        BabData data, IReadOnlyList<MappedRelic> relics, IReadOnlyList<BabEnemy> enemies) => new()
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
            Relics = relics.ToDictionary(
                r => r.Relic.Id,
                r => new EntityPresentation
                {
                    Art = $"relics/{r.Relic.Id}.png",
                    FlavorText = r.Source.Description,
                    Rarity = r.Source.Rarity,
                    Tags = r.Source.Tags ?? [],
                }),
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
            Events = data.Events.ToDictionary(
                e => e.Id,
                e => new EntityPresentation { FlavorText = e.Name, Tags = e.Tags ?? [] }),
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
