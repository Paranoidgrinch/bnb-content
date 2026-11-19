using BnbContent.Converter;

namespace BnbContent.Tests;

// The hand-written outcome lines (source-data/events/event_outcomes.json, playtest 2026-11-19) are keyed
// event/situation/choice. A key that names nothing is a line nobody will ever see — a typo, or an event reworked
// under it — so every key has to name a choice the game document really has.
public class EventOutcomeTextTests
{
    [Fact]
    public void Every_outcome_line_names_a_real_choice()
    {
        var data = BabData.Load(TestData.Directory);
        var blueprint = BlueprintAssembler.Build(data, 20260717);
        Assert.NotEmpty(data.EventOutcomes);
        var missing = data.EventOutcomes.Keys.Where(key =>
        {
            var parts = key.Split('/');
            return parts.Length != 3
                || !blueprint.Events.TryGetValue(parts[0], out var script)
                || !script.Situations.TryGetValue(parts[1], out var situation)
                || situation.Choices.All(c => c.Id != parts[2]);
        }).ToList();
        Assert.True(missing.Count == 0, "outcome lines for no choice: " + string.Join(", ", missing));
        Assert.Contains("event-outcomes", blueprint.Presentation.Game!.Extra.Keys);
    }
}
