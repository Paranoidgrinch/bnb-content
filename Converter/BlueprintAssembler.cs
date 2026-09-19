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
        // One set of card pools and one set of map rules PER ACT, each drawing only from its own act. The run
        // is one walk across all of them: the engine lays every act out at run start (RunSetup.BuildActPlan,
        // its own seed per act) and advances by itself when an act's boss falls.
        var pools = data.Acts.ToDictionary(a => a.Act, a => ConversionPools.Build(a.Act));
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
            Deck = data.Bureaucrat.StartingDeck.Select(id => new CardDefinitionId(id)).ToList(),
        };

        var blueprint = new RunBlueprint(
            data.Bureaucrat.StartingDeck.Select(id => new CardDefinitionId(id)).ToList(),
            events,
            data.Encounters.Select(e => EncounterMapper.Map(e, enemiesById, data.Bureaucrat.StartingEnergy)).ToList(),
            // The authored pool and nothing else. The ported v2 cards used to ride along here because the
            // ported events named some of them; those events were replaced act by act, and on 2026-09-10 the
            // document was checked for what still reached the leftovers — nothing did, in any pool, event,
            // encounter, reward or card program. A card the game cannot deal is not content, it is noise in
            // every list that counts cards.
            [
                .. Cards.FinalCards.Compile(),
                // The events' temporary cards: never dealt into a deck, only pushed into a fight.
                .. Events.ActOneEventObjects.Compile(), .. Events.ActTwoEventObjects.Compile(),
                .. ActThree.GivenCards(),
                // The Act-IV elites' offers: two boundaries and three seals, never dealt into a deck.
                .. ActFour.EliteCards(),
                // …and the bosses' own: the Architect's blueprints, the Lady's four seals.
                .. ActFour.BossCards(),
                // …and Act V's: the reed sheets the First Tablet lays in hand.
                .. ActFive.GivenCards(),
                .. Relics.ActThreeBossRelicCards.All(),
                .. Relics.ActFourBossRelicCards.All(),
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
            // …and its strategic twin beside it, for the same reason and with the same reach: a document that
            // carries only one of the two would offer a generator choice it cannot honour (plan §2.3 — the
            // switch is a property of the DOCUMENT, not an argument the caller picks).
            StrategicMapGeneration = maps[0].Map.Strategic,
            Acts = maps
                .Select(m => new RunAct(m.Act.Id, m.Map.Spec, NameKey: m.Act.Name)
                {
                    StrategicMapGeneration = m.Map.Strategic,
                    // ── THE GATES OF AN ACT PUT THE BODY BACK TOGETHER ───────────────────────────────────
                    // Whatever the last act cost, the next one begins whole. It is a design decision and it
                    // is worth naming both halves of what it buys:
                    //
                    //   FOR THE PLAYER — an act is a chapter with its own arc rather than a tax on the one
                    //   before it. A boss fought at four health stops being a fight and starts being a
                    //   verdict passed three rooms earlier, and a run that limps into act three at nine
                    //   health was decided in act two by nothing the player can still answer.
                    //
                    //   FOR EVERY MEASUREMENT WE TAKE — it makes the run DECOMPOSABLE. Health is the one
                    //   number that ties act four's difficulty to act one's luck; cut it at the gates and
                    //   each act can be asked about on its own, and what crosses the boundary is only the
                    //   deck, the relics and the purse. That is the difference between a search that
                    //   multiplies across five acts and one that adds.
                    //
                    // ⚠ ACT I RECEIVES IT TOO, and receives nothing: a run starts at full health, so healing
                    // the missing none is a no-op. The rule is stated for every act rather than for "every
                    // act but the first", because the second phrasing is the kind nobody remembers.
                    //
                    // ⚠ ACT V TOO — the gauntlet of three bosses. Its design says no healing BETWEEN them
                    // (BnB Boss Master §Act V), which this does not touch: it opens the doors at full and
                    // then hands out nothing until the end. "The build that enters is the build that must
                    // win" reads better when the build enters whole.
                    Opening = [new ComputedHealRunEffect(RunExpr.MissingHealth)],
                })
                .ToList(),
            Statuses =
            [
                .. StatusMapper.Map("statuses", data.Statuses),
                .. Cards.FinalCards.Statuses(),
                .. Relics.FinalRelics.Statuses(),
                .. PassiveStatuses.All(),
                .. ActTwo.All(),
                .. ActThree.All(),
                .. ActFour.All(),
                .. ActFive.All(),
                .. Events.ActOneEventObjects.Statuses(), .. Events.ActTwoEventObjects.Statuses(),
                .. Events.ActThreeEventObjects.Statuses(),
            ],
            // ONLY THE AUTHORED POOLS SHIP. Until 2026-09-10 every ported v2 relic whose id did not meet a
            // final one shipped alongside them — 47 of them — because the ported events granted some by
            // name. Nothing does any more: every faucet in the game (elite, boss, mimic, treasure, shop, the
            // three markets and the events that award "a random relic") draws from a final pool, so the old
            // definitions are dead weight that the relic shelf would have had to draw a frame for.
            Relics = [.. Relics.FinalRelics.Compile()],
            Shops = maps.SelectMany(m => m.Map.Shops).ToDictionary(e => e.Key, e => e.Value),
            // What the authored events promise for AFTER a fight. The bodies live here once; the events that
            // hand them out name them (fx.installProgramById).
            // Every act's promise bodies in one dictionary. An act's programs are its OWN — the extra card
            // reward draws from the act's pool — so a run that walks them all carries every set, under names
            // that say which act made the promise.
            Programs = AuthoredPrograms(pools),
            Start = start,
            Characters = [new RunCharacter(data.Bureaucrat.Id, start)],
            // Victory now means the WHOLE run — the city and the archives behind it. (The engine's meta rules
            // key off the run's result; a per-act flag would need an act-completed hook that is not data yet.)
            MetaRules = [new MetaRule([RunResult.Victory], [new SetMetaFlag("bnb.run.cleared")])],
            Presentation = BuildPresentation(
                data, enemies, [.. authored.Values.SelectMany(e => e)]),
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
        if (pools.TryGetValue(Events.ActThreeEvents.Act, out var actThree))
            foreach (var (id, body) in Events.ActThreeEventPrograms.All(actThree))
                programs[id] = body;
        if (pools.TryGetValue(Events.ActFourEvents.Act, out var actFour))
            foreach (var (id, body) in Events.ActFourEventPrograms.All(actFour))
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
        BabData data, IReadOnlyList<BabEnemy> enemies, IReadOnlyList<Events.BnbEvent> authored)
    {
        // Worked out once over all 294 encounters, not once per body.
        var enemyRoles = EnemyRole.Of(data);
        return new()
        {
            Cards = Cards.FinalCards.All().ToDictionary(
                c => c.Id,
                c => new EntityPresentation
                {
                    // An improvement changes what a card DOES, not what it is a picture of: `levy_stamp+`
                    // draws `levy_stamp.png`, and no file name in this game carries a `+`.
                    Art = $"cards/{c.Id.TrimEnd('+')}.png",
                    // The engine has no rules-text renderer: a card's ability text IS presentation, and
                    // this is what both UIs show on a reward or in the hand.
                    FlavorText = c.Text,
                    Rarity = c.Rarity,
                    Tags = c.AllTags.ToList(),
                }),
            // One entry per relic the document actually ships, which since 2026-09-10 is the authored pools
            // and nothing else. `Art` is the file the frontend looks for and the id IS the name of it.
            Relics = Relics.FinalRelics.All().ToDictionary(
                r => r.Id,
                r => new EntityPresentation
                {
                    Art = $"relics/{r.Id}.png",
                    FlavorText = r.Text,
                    Rarity = r.Rarity.ToString().ToLowerInvariant(),
                    // WHICH POOL A RELIC CAME FROM IS A LOOK, NOT A RULE. The visual canon does not describe
                    // relics one by one and then a border: it fixes ONE frame per pool (§10.4 "Pool frames and
                    // color identity" — normal slate, shop copper, event pale violet, boss dark purple and
                    // antique gold; the elite canon §2 adds the plainer boss frame the elites and the mimic
                    // wear), so a shelf of relics is read by pool before a single object on it is recognised.
                    // `Frame` is the contract's own slot for exactly that ("card-frame / border style"), the
                    // engine ignores it, and the pool name is the value because the pool IS the frame here.
                    Frame = r.Pool.ToString().ToLowerInvariant(),
                    Tags = [],
                }),
            Statuses = data.Statuses
                .ToDictionary(
                    s => s.Id,
                    s => new EntityPresentation
                    {
                        Icon = $"statuses/{s.Id}.png",
                        FlavorText = s.Description,
                        Tags = s.Tags ?? [],
                    })
                // …and the phase markers, which are authored rather than ported and so had no entry at all.
                // A frontend cannot tell "the boss is in its second phase" from "the boss has 3 Paperwork" by
                // looking at either — the status has to say which it is, and this is where it says it.
                .Concat(BossPhases.Markers.Select(id => KeyValuePair.Create(
                    id, new EntityPresentation { Tags = [BossPhases.PhaseTag] })))
                .GroupBy(e => e.Key)
                .ToDictionary(g => g.Key, g => g.Last().Value),
            // Every body in the game is a picture too, on the same terms as a card or a relic: the id is the
            // file name, `Art` is the path the frontend resolves, and an empty slot draws the stick figure
            // that stands there today. Two fields beyond the path, both of them things the frontend cannot
            // work out for itself:
            //   • the NAME, because a body's display name lives inside the encounters that use it, and the
            //     art table has to be able to say who it is drawing;
            //   • WHAT KIND of body it is (`Frame`, the contract's own look slot — the same one the relic
            //     shelf reads its pool from). A standard enemy, an elite, a boss and a mimic are drawn at
            //     different sizes and with different frames, and NONE of that is a property of the enemy: it
            //     is a property of the fights it appears in. So it is worked out here, once, from the
            //     encounters — the frontend never sees an encounter while it is drawing a body.
            Enemies = enemies.ToDictionary(
                e => e.Id,
                e => new EntityPresentation
                {
                    Art = $"enemies/{e.Id}.png",
                    FlavorText = e.Name,
                    Frame = enemyRoles.GetValueOrDefault(e.Id, EnemyRole.Standard),
                    Tags = e.Tags ?? [],
                }),
            Encounters = data.Encounters.ToDictionary(
                e => e.Id,
                e => new EntityPresentation
                {
                    // ★ THE ROOM A FIGHT HAPPENS IN IS A PICTURE, AND THE SLOT FOR IT WAS ALREADY HERE. Every
                    // encounter has carried an `Art` since the manifest was written and all 294 of them were
                    // null; nothing new is declared, the slot that exists is filled. It is filled PER ACT
                    // because that is what the design asks for — and filling it per act buys per-encounter
                    // for free: the day one boss deserves a room of its own, one value changes and nothing
                    // else does. (D3's rule a third time: the path of the contract is the path on disk.)
                    Art = $"backgrounds/{ActSlot(data, e.Act)}.png",
                    FlavorText = e.Name,
                    Tags = [e.Difficulty, .. e.Tags ?? []],
                    // Act V's shared rule is a UI rule: each god owns a Divine Rule Area, and this is where its
                    // words come from (ActFive). Empty for every other fight in the game, which shows no panel.
                    Extra = ActFive.Extra(e.Id),
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

    // Which picture an act's rooms are drawn on. The act NUMBER an encounter carries is the authority (the
    // loader says so in as many words: which encounters belong to an act is decided by the number the entry
    // carries, not by the file it came from), and the act's own id is the file name — so a sixth act brings
    // its background with it and nothing here has to learn about it.
    private static string ActSlot(BabData data, int act) =>
        act >= 1 && act <= data.Acts.Count ? data.Acts[act - 1].Id : $"act_{act}";

}

// WHAT KIND OF BODY THIS IS. An enemy id says nothing about whether it is a mook, an elite or a god — the
// FIGHT says it, in its difficulty, and the same id can be met in more than one fight. The strongest room a
// body is ever used in is what it is: a creature that guards a boss room is drawn as a boss even if it also
// turns up as filler somewhere, because the biggest thing it has to look like is the thing it has to look
// like. Ranked, not merged: "elite or boss" is not a look.
public static class EnemyRole
{
    public const string Standard = "standard";
    public const string Elite = "elite";
    public const string Boss = "boss";
    public const string Mimic = "mimic";

    // Weakest first. This array is the ONLY statement of the ranking — the strongest room a body is met in
    // wins, and "strongest" is this order and nothing else.
    private static readonly string[] ByStrength = [Standard, Mimic, Elite, Boss];

    // The difficulty an encounter declares, as a body's look. Everything the source data calls easy, normal
    // or hard is one thing to draw: a standard body.
    private static string Of(string? difficulty) => difficulty?.ToLowerInvariant() switch
    {
        "boss" => Boss,
        "elite" => Elite,
        "mimic" => Mimic,
        _ => Standard,
    };

    public static IReadOnlyDictionary<string, string> Of(BabData data)
    {
        ArgumentNullException.ThrowIfNull(data);
        var roles = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var encounter in data.Encounters)
        {
            var role = Of(encounter.Difficulty);
            foreach (var id in encounter.Enemies)
            {
                if (!roles.TryGetValue(id, out var known)
                    || Array.IndexOf(ByStrength, role) > Array.IndexOf(ByStrength, known))
                {
                    roles[id] = role;
                }
            }
        }
        return roles;
    }
}
