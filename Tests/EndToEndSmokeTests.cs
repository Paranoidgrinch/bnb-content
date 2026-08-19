using BnbContent.Converter;
using RogueDeck.Core.Combat;
using RogueDeck.Run;
using RogueDeck.Sandbox.Composition;
using RogueDeck.Sandbox.Run;

namespace BnbContent.Tests;

// The conversion gates (plan C3): the generated document passes the engine's export gate, survives its
// own round trip byte-for-byte, and an actual run plays through the REAL host path
// (RunPlayback.BuildContent + the interactive drivers): walk the map, win the first fight with the
// starter deck, collect the spoils. Whatever fails here fails in Godot too.
public class EndToEndSmokeTests
{
    private const int Seed = 20260717;
    private static readonly RunBlueprint Blueprint =
        BlueprintAssembler.Build(BabData.Load(TestData.Directory), Seed);

    [Fact]
    public void The_document_passes_the_export_gate()
    {
        Assert.Empty(RunDocumentValidator.ValidateForExport(Blueprint));
    }

    [Fact]
    public void The_document_round_trips_byte_for_byte()
    {
        var options = RunJson.CreateOptions();
        var json = RunJson.ToJson(Blueprint, options);
        Assert.Equal(json, RunJson.ToJson(RunJson.BlueprintFromJson(json, options), options));
    }

    // The act is GENERATED per run now, so the document carries rules instead of nodes: every generated layout
    // must honour the audit's per-path minimums (docs/bnb-act-map-specs.md) and end on the boss.
    [Theory]
    [InlineData(1)]
    [InlineData(7)]
    [InlineData(20260819)]
    public void Every_generated_act_honours_the_per_path_minimums(int seed)
    {
        Assert.Empty(Blueprint.Map.Nodes); // nothing baked
        var spec = Blueprint.MapGeneration!;

        var generated = RuleBasedMapGenerator.Generate(spec, seed, startingLoadout: 0,
            new BalanceCalculator(Blueprint.Balance, Blueprint.Encounters),
            (kind, coord, encounter, nodeRef) => MapNodeRealizer.Realize(spec, kind, encounter, nodeRef));

        Assert.Empty(MapConstraintValidator.Validate(generated, spec));

        var roles = generated.Roles;
        Assert.Equal(1, roles.Values.Count(r => r == MapNodeKind.Boss));
        Assert.Contains(roles.Values, r => r == MapNodeKind.Shop);
        // Every fight pays out — a generated act keeps its reward economy.
        Assert.All(generated.Map.Nodes.Select(n => n.Payload).OfType<EncounterRef>(),
            fight => Assert.NotNull(fight.VictoryReward));
    }

    [Fact]
    public void A_real_run_wins_its_first_fight_and_collects_the_spoils()
    {
        // Reload from JSON first — the run must work from the exported document, not the in-memory one.
        var options = RunJson.CreateOptions();
        var blueprint = RunJson.BlueprintFromJson(RunJson.ToJson(Blueprint, options), options);

        var play = new RunPlayback(() => { });
        play.Start(blueprint, seed: 7, interactive: true);
        var session = play.Session!;
        Assert.Null(play.Error);
        using (play)
        {
            // Walk the GENERATED act until the first fight: a path choice parks first, and the stops before it
            // can be events, treasure or a rest — steer toward a combat node and resolve whatever else appears.
            for (var guard = 0; play.CombatDriver!.Current is null && guard < 60; guard++)
            {
                if (session.IsAwaitingNodeChoice)
                {
                    var choices = session.PendingNodeChoices;
                    var combatNode = choices.FirstOrDefault(n => n.Type == StandardRunIds.CombatNode) ?? choices[0];
                    session.PickNode(combatNode.Id.Value);
                }
                else if (session.IsAwaitingChoice)
                    session.Pick(session.PendingSituation!.Choices[0].Id);
                else if (session.IsAwaitingEntities)
                    session.PickEntities([0]);
                else if (session.IsAwaitingInterlude)
                    session.Continue();
                else
                    break;
                Assert.Null(session.Error);
            }
            var combat = play.CombatDriver!.Current;
            Assert.True(session.Error is null, session.Error);
            Assert.NotNull(combat);
            Assert.Equal(5, combat!.Hand.Count); // the standard draw from the 10-card starter deck

            // Play every affordable, playable card at the first living enemy, end the turn, repeat.
            var goldBefore = session.Run.GetResource(StandardRunIds.Gold);
            for (var turn = 0; turn < 30 && play.CombatDriver.Current is not null; turn++)
            {
                var state = play.CombatDriver.Current!;
                while (true)
                {
                    var current = play.CombatDriver.Current;
                    if (current is null)
                        break; // fight resolved mid-play
                    var hero = current.State.GetCombatant(current.HeroId);
                    var energy = hero.Resources[StandardCombatIds.EnergyResource].Current;
                    var playable = current.Hand.FirstOrDefault(c =>
                        !c.DefinitionId.value.Contains("red_tape") && !c.DefinitionId.value.Contains("unsigned_form")
                        && CostOf(blueprint, c.DefinitionId) <= energy);
                    if (playable is null)
                        break;
                    var target = current.State.Combatants.FirstOrDefault(x => x.Id != current.HeroId && x.IsAlive);
                    if (target is null)
                        break;
                    play.CombatDriver.PlayCard(playable.Id, target.Id);
                    Assert.Null(session.Error);
                }
                if (play.CombatDriver.Current is null)
                    break;
                play.CombatDriver.EndTurn();
                Assert.Null(session.Error);
            }
            Assert.Null(play.CombatDriver.Current); // the fight ended

            // Victory spoils: the single "spoils" entity pick (gold + nested card offer), then the card.
            while (session.IsAwaitingInterlude)
                session.Continue();
            Assert.True(session.IsAwaitingEntities, "expected the victory spoils to park an entity pick");
            session.PickEntities([0]);
            Assert.Null(session.Error);
            Assert.True(session.Run.GetResource(StandardRunIds.Gold) > goldBefore); // easy-tier gold landed

            Assert.True(session.IsAwaitingEntities); // the pick-1-of-3 card reward
            Assert.Equal(3, session.PendingEntities!.Displays.Count);
            session.PickEntities([0]);
            Assert.Null(session.Error);
            Assert.Equal(11, session.Run.Deck.Count); // 10 starters + the reward pick
        }
    }

    private static int CostOf(RunBlueprint blueprint, CardDefinitionId definition) =>
        blueprint.Cards.First(c => c.Id == definition.value).Costs
            .Where(c => c.ResourceId == StandardCombatIds.EnergyResource)
            .Sum(c => c.Amount);
}
