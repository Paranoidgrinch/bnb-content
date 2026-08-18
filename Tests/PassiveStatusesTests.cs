using System.Text.Json;
using BnbContent.Converter;
using RogueDeck.Core.Combat;
using Xunit;

namespace BnbContent.Tests;

// Reworked enemy passives are authored as statuses-with-triggers built from RAW effect programs (the arc's
// richer expressions aren't in CombatNodeModel). This proves such a passive is produced and that its trigger
// program round-trips through the CombatJson converters — the same path game.roguedeck.json / BuildContent use.
public class PassiveStatusesTests
{
    [Fact]
    public void Queue_advances_passive_is_authored_with_a_turn_started_trigger()
    {
        var queue = Assert.Single(PassiveStatuses.All(), s => s.Id == PassiveStatuses.QueueAdvancesId);
        Assert.False(queue.UsesStacks);
        var trigger = Assert.Single(queue.Triggers);
        Assert.Equal("TurnStarted", trigger.Event);
    }

    [Fact]
    public void The_trigger_program_round_trips_through_combat_json()
    {
        var trigger = PassiveStatuses.All().Single(s => s.Id == PassiveStatuses.QueueAdvancesId).Triggers.Single();
        var options = CombatJson.CreateOptions<TurnStartedTriggeredEffectContext>();

        // The stored program deserializes back into a runnable EffectProgram (would throw if a node/expression
        // discriminator were unregistered) and re-serializes identically.
        var json = trigger.Program.GetRawText();
        var reloaded = JsonSerializer.Deserialize<EffectProgram<TurnStartedTriggeredEffectContext>>(json, options);
        Assert.NotNull(reloaded);
        Assert.Equal(json, JsonSerializer.Serialize(reloaded, options));
    }
}
