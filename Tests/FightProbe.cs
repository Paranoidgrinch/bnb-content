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

    public static RunBlueprint OneFight(EncounterDefinition probe) => Game with
    {
        Encounters = [probe],
        Map = new RunMap([new Node(new NodeId("probe"), StandardRunIds.CombatNode, new EncounterRef(probe.Id))]),
    };

    // A solo encounter with one AUTHORED enemy: its real roster entry (HP, passives carried from the first
    // bell, intent rules) is taken from the converted game and only narrowed to the intent under test, plus any
    // extra statuses the test wants it to open with. Hand-building the entry instead would quietly drop the
    // enemy's own starting statuses — the very passives most tests are about.
    public static EncounterDefinition Solo(
        string enemyId, string intentId, params (string Status, int Stacks)[] startingStatuses)
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
            [new ResourceSpec(StandardCombatIds.EnergyResource, 3, 3)]);
    }

    // Starts the probe fight and hands back the live playback plus the enemy's id.
    public static (RunPlayback Play, InteractiveRunSession Session, CombatantId EnemyId) Start(EncounterDefinition probe)
    {
        var play = new RunPlayback(() => { });
        play.Start(OneFight(probe), seed: 1, interactive: true);
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
