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

    [Fact]
    public void The_baked_map_carries_the_expected_station_types()
    {
        var types = Blueprint.Map.Nodes.Select(n => n.Type.Value).ToList();
        Assert.Contains("combat", types);
        Assert.Contains("event", types);
        Assert.Contains("shop", types); // the adapted city shop
        Assert.Contains(Blueprint.Events.Keys, id => id.StartsWith("rest:"));     // the waiting room
        Assert.True(Blueprint.Map.Nodes.Count > 20);
        Assert.Contains(Blueprint.Map.Nodes, n => n.Id.Value == "act_1_boss");
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
            // Reach the first fight (entry rows have 2+ lanes → a path choice parks first).
            for (var guard = 0; play.CombatDriver!.Current is null && guard < 10; guard++)
            {
                if (session.IsAwaitingNodeChoice)
                    session.PickNode(session.PendingNodeChoices[0].Id.Value);
                else if (session.IsAwaitingInterlude)
                    session.Continue();
                else
                    break;
            }
            var combat = play.CombatDriver!.Current;
            Assert.NotNull(combat);
            Assert.Null(session.Error);
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
