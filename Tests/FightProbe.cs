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
    public static RunBlueprint OneFight(EncounterDefinition probe, IReadOnlyList<string>? deck = null)
    {
        var blueprint = Game with
        {
            Encounters = [probe],
            Map = new RunMap([new Node(new NodeId("probe"), StandardRunIds.CombatNode, new EncounterRef(probe.Id))]),
        };

        return deck is null or { Count: 0 }
            ? blueprint
            : blueprint with
            {
                Deck = deck.Select(id => new CardDefinitionId(id)).ToList(),
                Start = blueprint.Start with { Deck = deck.Select(id => new CardDefinitionId(id)).ToList() },
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
            heroStartingStatuses: HeroStatuses(),
            triggeredEffects: EncounterPassives.ForEnemy(enemyId));
    }

    // Every probe marks the hero exactly as EncounterMapper does — the applicant marker a passive needs to
    // tell "this happened to the player".
    private static IReadOnlyList<StartingStatusSpec> HeroStatuses() =>
        [new StartingStatusSpec(new StatusDefinitionId(PassiveStatuses.ApplicantId), 1)];

    // A multi-enemy probe: each member is an authored enemy narrowed to one intent, optionally at the reduced
    // HP its encounter fields it at. Roster order is turn order.
    public static EncounterDefinition Roster(
        string probeId, params (string EnemyId, string IntentId, int? MaxHealth)[] members)
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
            [new ResourceSpec(StandardCombatIds.EnergyResource, 3, 3)],
            heroStartingStatuses: HeroStatuses(),
            triggeredEffects: members.Select(m => m.EnemyId).Distinct()
                .SelectMany(EncounterPassives.ForEnemy).ToList());
    }

    // The REAL authored encounter, exactly as the game fields it (roster, per-encounter HP, intents).
    public static EncounterDefinition Authored(string encounterId) =>
        Game.Encounters.FirstOrDefault(e => e.Id.Value == encounterId)
        ?? throw new InvalidOperationException($"no encounter '{encounterId}'");

    // Starts the probe fight and hands back the live playback plus the enemy's id.
    public static (RunPlayback Play, InteractiveRunSession Session, CombatantId EnemyId) Start(
        EncounterDefinition probe, IReadOnlyList<string>? deck = null)
    {
        var play = new RunPlayback(() => { });
        play.Start(OneFight(probe, deck), seed: 1, interactive: true);
        var session = play.Session!;
        Assert.Null(play.Error);
        while (session.IsAwaitingInterlude)
            session.Continue();
        Assert.Null(session.Error);

        var combat = play.CombatDriver!.Current!;
        var enemyId = combat.State.Combatants.First(c => c.Id != combat.HeroId).Id;
        return (play, session, enemyId);
    }

    public static int StacksOf(CombatantState combatant, string status) =>
        combatant.Statuses.Where(s => s.DefinitionId == new StatusDefinitionId(status)).Sum(s => s.Stacks);
}
