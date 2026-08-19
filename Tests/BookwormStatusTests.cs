using BnbContent.Converter;
using RogueDeck.Core.Combat;
using RogueDeck.Run;
using RogueDeck.Sandbox.Composition;
using RogueDeck.Sandbox.Run;
using RogueDeck.Scenario.Authoring;

namespace BnbContent.Tests;

// Bookworm X — the reworked Act-I anti-Paperwork status — proven in a LIVE fight, not just in the mapping:
// "immediately before that enemy's Paperwork resolves, remove up to X Paperwork and the same number of
// Bookworm stacks". Everything runs through the real host path (RunPlayback → BuildContent → combat), so a
// wiring gap or a trigger-ordering regression fails here.
public class BookwormStatusTests
{
    private static readonly RunBlueprint Game = BlueprintAssembler.Build(BabData.Load(TestData.Directory), 20260717);
    private static readonly StatusDefinitionId Paperwork = new("paperwork");
    private static readonly StatusDefinitionId Bookworm = new("bookworm");

    // A one-fight blueprint carved out of the REAL converted game — same statuses, cards and enemy actions,
    // but the map is a single combat node pointing at a probe encounter. This is how any authored mechanic
    // can be driven in a live fight without walking the act.
    private static RunBlueprint OneFight(EncounterDefinition probe) => Game with
    {
        Encounters = [probe],
        Map = new RunMap([new Node(new NodeId("probe"), StandardRunIds.CombatNode, new EncounterRef(probe.Id))]),
    };

    private static EncounterDefinition Beetle(int paperwork, int bookworm) =>
        new(new EncounterId("bookworm_probe"),
            [new EncounterEnemy("filing_beetle", 40,
                [new EnemyActionDefinitionId("filing_beetle.mandible_stamp")],
                StartingStatuses:
                [
                    new StartingStatusSpec(Paperwork, paperwork),
                    new StartingStatusSpec(Bookworm, bookworm),
                ],
                DisplayName: "Filing Beetle")],
            [new ResourceSpec(StandardCombatIds.EnergyResource, 3, 3)]);

    // 5 Paperwork + 2 Bookworm → 3 Paperwork + 0 Bookworm, and the tick that follows deals 3, not 5.
    [Fact]
    public void Bookworm_erases_paperwork_before_it_ticks()
    {
        var (beetle, _) = FightOneEnemyTurn(Beetle(paperwork: 5, bookworm: 2));

        Assert.Equal(3, StacksOf(beetle, Paperwork));
        Assert.Equal(0, StacksOf(beetle, Bookworm));
        Assert.Equal(37, beetle.Health.Current); // 40 − 3 ticked, not − 5
    }

    // More Bookworm than Paperwork: only as much is spent as there was Paperwork, the rest remains.
    [Fact]
    public void Surplus_bookworm_remains_after_erasing_what_paperwork_there_was()
    {
        var (beetle, _) = FightOneEnemyTurn(Beetle(paperwork: 1, bookworm: 3));

        Assert.Equal(0, StacksOf(beetle, Paperwork));
        Assert.Equal(2, StacksOf(beetle, Bookworm));
        Assert.Equal(40, beetle.Health.Current); // nothing left to tick
    }

    // No Paperwork at all: Bookworm is not spent (it waits for the filing that is surely coming).
    [Fact]
    public void Bookworm_is_not_spent_without_paperwork()
    {
        var (beetle, _) = FightOneEnemyTurn(Beetle(paperwork: 0, bookworm: 2));

        Assert.Equal(2, StacksOf(beetle, Bookworm));
        Assert.Equal(40, beetle.Health.Current);
    }

    // Plays the fight up to the point where the enemy has taken its turn, and hands back its state.
    private static (CombatantState Enemy, InteractiveRunSession Session) FightOneEnemyTurn(EncounterDefinition probe)
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

        play.CombatDriver.EndTurn(); // → the beetle's turn starts: Bookworm first, then the Paperwork tick
        Assert.Null(session.Error);

        return (play.CombatDriver.Current!.State.GetCombatant(enemyId), session);
    }

    private static int StacksOf(CombatantState combatant, StatusDefinitionId status) =>
        combatant.Statuses.Where(s => s.DefinitionId == status).Sum(s => s.Stacks);
}
