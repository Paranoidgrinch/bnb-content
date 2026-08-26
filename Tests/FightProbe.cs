using BnbContent.Converter;
using RogueDeck.Core.Combat;
using RogueDeck.Run;
using RogueDeck.Sandbox.Composition;
using RogueDeck.Sandbox.Run;
using RogueDeck.Scenario.Authoring;

namespace BnbContent.Tests;

// Drives ONE authored fight through the real host path (RunPlayback → BuildContent → live combat) without
// walking the act: the converted game blueprint is reused whole — same statuses, cards, enemy actions — and
// only the map is replaced by a single combat node pointing at a probe encounter. That keeps every mechanic
// test honest about wiring (a missing status registration or executor fails here, as it would in Godot).
internal static class FightProbe
{
    public static readonly RunBlueprint Game =
        BlueprintAssembler.Build(BabData.Load(TestData.Directory), seed: 20260717);

    // `deck` stacks the hero's deck with authored cards (repeated as given) when a test needs a specific card
    // in hand — e.g. one that files Paperwork onto an enemy. Empty ⇒ the character's real starting deck.
    public static RunBlueprint OneFight(
        EncounterDefinition probe, IReadOnlyList<string>? deck = null, int? health = null)
    {
        var blueprint = Game with
        {
            Encounters = [probe],
            Map = new RunMap([new Node(new NodeId("probe"), StandardRunIds.CombatNode, new EncounterRef(probe.Id))]),
            // The real game GENERATES its map per run, act by act, which would replace the probe node with a
            // whole act (drawn from encounters this blueprint no longer holds). A probe is one fight.
            MapGeneration = null,
            Acts = null,
        };

        if (deck is not null and not { Count: 0 })
            blueprint = blueprint with
            {
                Deck = deck.Select(id => new CardDefinitionId(id)).ToList(),
                Start = blueprint.Start with { Deck = deck.Select(id => new CardDefinitionId(id)).ToList() },
                Characters = [],
            };

        // `health` buys a probe room to reach a late mechanic (the Knight's refused enforcement) without the
        // fight ending first — the probe is a mechanism test, not a balance sample.
        return health is not { } hp
            ? blueprint
            : blueprint with
            {
                Start = blueprint.Start with { MaxHealth = hp, StartingHealth = hp },
                Characters = [],
            };
    }

    // A solo encounter with one AUTHORED enemy: its real roster entry (HP, passives carried from the first
    // bell, intent rules) is taken from the converted game and only narrowed to the intent under test, plus any
    // extra statuses the test wants it to open with. Hand-building the entry instead would quietly drop the
    // enemy's own starting statuses — the very passives most tests are about.
    public static EncounterDefinition Solo(
        string enemyId, string intentId, params (string Status, int Stacks)[] startingStatuses) =>
        Solo(enemyId, intentId, energy: 3, startingStatuses);

    // `energy` raises the hero's per-turn energy when a test needs several cards inside ONE player turn (the
    // Ward's per-turn damage threshold, say) — the fight is a probe, not a balance sample.
    // As Solo, but the HERO also opens with the given statuses — how a keyword that lives on the player
    // (Lien, Ward Wax, Blood Ink) is put on the table without a card that grants it.
    public static EncounterDefinition SoloAgainstHero(
        string enemyId, string intentId, int energy, params (string Status, int Stacks)[] heroStatuses)
    {
        var probe = Solo(enemyId, intentId, energy);
        return new EncounterDefinition(probe.Id, probe.Enemies, probe.HeroResources,
            [.. probe.HeroStartingStatuses ?? [],
             .. heroStatuses.Select(s => new StartingStatusSpec(new StatusDefinitionId(s.Status), s.Stacks))],
            probe.HeroDisplayName, probe.CardsDrawnPerTurn, probe.TriggeredEffects);
    }

    public static EncounterDefinition Solo(
        string enemyId, string intentId, int energy, params (string Status, int Stacks)[] startingStatuses)
    {
        var authored = Game.Encounters
            .SelectMany(e => e.Enemies)
            .FirstOrDefault(e => e.Id == enemyId)
            ?? throw new InvalidOperationException($"no authored encounter fields '{enemyId}'");

        var probe = authored with
        {
            Actions = [new EnemyActionDefinitionId($"{enemyId}.{intentId}")],
            StartingStatuses =
            [
                .. authored.StartingStatuses ?? [],
                .. startingStatuses.Select(s => new StartingStatusSpec(new StatusDefinitionId(s.Status), s.Stacks)),
            ],
        };

        return new EncounterDefinition(new EncounterId($"probe.{enemyId}"), [probe],
            [new ResourceSpec(StandardCombatIds.EnergyResource, energy, energy)],
            heroStartingStatuses: HeroStatuses(enemyId),
            triggeredEffects: EncounterPassives.ForEnemy(enemyId));
    }

    // Every probe marks the hero exactly as EncounterMapper does — the applicant marker a passive needs to
    // tell "this happened to the player".
    private static IReadOnlyList<StartingStatusSpec> HeroStatuses(params string[] enemyIds) =>
    [
        new StartingStatusSpec(new StatusDefinitionId(PassiveStatuses.ApplicantId), 1),
        // …plus whatever the roster serves ON the player at the first bell (the Knight's Final Notice).
        .. enemyIds.Distinct().SelectMany(EncounterPassives.HeroOpeningStatuses),
    ];

    // A multi-enemy probe: each member is an authored enemy narrowed to one intent, optionally at the reduced
    // HP its encounter fields it at. Roster order is turn order.
    public static EncounterDefinition Roster(
        string probeId, params (string EnemyId, string IntentId, int? MaxHealth)[] members) =>
        Roster(probeId, energy: 3, members);

    public static EncounterDefinition Roster(
        string probeId, int energy, params (string EnemyId, string IntentId, int? MaxHealth)[] members)
    {
        var roster = members.Select(m =>
        {
            var authored = Game.Encounters.SelectMany(e => e.Enemies).FirstOrDefault(e => e.Id == m.EnemyId)
                ?? throw new InvalidOperationException($"no authored encounter fields '{m.EnemyId}'");
            return authored with
            {
                Actions = [new EnemyActionDefinitionId($"{m.EnemyId}.{m.IntentId}")],
                MaxHealth = m.MaxHealth ?? authored.MaxHealth,
            };
        }).ToList();

        // Cross-combatant passives live on the ENCOUNTER, so a probe must carry them exactly as EncounterMapper
        // would — otherwise a passive silently does nothing in the very test meant to prove it.
        return new EncounterDefinition(new EncounterId($"probe.{probeId}"), roster,
            [new ResourceSpec(StandardCombatIds.EnergyResource, energy, energy)],
            heroStartingStatuses: HeroStatuses([.. members.Select(m => m.EnemyId)]),
            triggeredEffects: members.Select(m => m.EnemyId).Distinct()
                .SelectMany(EncounterPassives.ForEnemy).ToList());
    }

    // The REAL authored encounter, exactly as the game fields it (roster, per-encounter HP, intents).
    // `energy` raises only the hero's pool, for probes that must land several cards inside one turn.
    public static EncounterDefinition Authored(string encounterId, int? energy = null)
    {
        var authored = Game.Encounters.FirstOrDefault(e => e.Id.Value == encounterId)
            ?? throw new InvalidOperationException($"no encounter '{encounterId}'");

        return energy is not { } pool
            ? authored
            : new EncounterDefinition(authored.Id, authored.Enemies,
                [new ResourceSpec(StandardCombatIds.EnergyResource, pool, pool)],
                authored.HeroStartingStatuses, authored.HeroDisplayName, authored.CardsDrawnPerTurn,
                authored.TriggeredEffects);
    }

    // Starts the probe fight and hands back the live playback plus the enemy's id.
    public static (RunPlayback Play, InteractiveRunSession Session, CombatantId EnemyId) Start(
        EncounterDefinition probe, IReadOnlyList<string>? deck = null, int? health = null)
    {
        var play = new RunPlayback(() => { });
        play.Start(OneFight(probe, deck, health), seed: 1, interactive: true);
        var session = play.Session!;
        Assert.True(play.Error is null, play.Error);
        while (session.IsAwaitingInterlude)
            session.Continue();
        Assert.True(session.Error is null, session.Error);

        var combat = play.CombatDriver!.Current!;
        var enemyId = combat.State.Combatants.First(c => c.Id != combat.HeroId).Id;
        return (play, session, enemyId);
    }

    public static int StacksOf(CombatantState combatant, string status) =>
        combatant.Statuses.Where(s => s.DefinitionId == new StatusDefinitionId(status)).Sum(s => s.Stacks);
}
