using BnbContent.Converter;
using RogueDeck.Core.Combat;

namespace BnbContent.Tests;

// Conversion gate for the STATUS table, run against the real source data. The card half of this file went
// with the ported v2 cards on 2026-09-10: the mapper it exercised (CardMapper) no longer exists, because the
// document no longer ships a card that came out of the v2 JSON. The statuses did not go — the authored pools
// speak the same status vocabulary, so `statuses/statuses.json` is still loaded and still has to convert.
public class StatusMappingTests
{
    private static readonly BabData Data = BabData.Load(TestData.Directory);

    [Fact]
    public void Every_status_converts_with_its_ported_semantics()
    {
        var statuses = StatusMapper.Map("statuses", Data.Statuses);
        // Paperwork and Doubt moved to Cards/Keywords.cs when the final card design gave them rules this
        // mapper had only approximated; what is left here are the four remaining ported statuses and Bookworm.
        Assert.Equal(5, statuses.Count);
        Assert.DoesNotContain(statuses, s => s.Id is "paperwork" or "doubt");

        var poison = statuses.First(s => s.Id == "poison");
        Assert.Contains(StandardCombatIds.DamageOverTimeTag.value, poison.Tags);
        Assert.Single(poison.Triggers); // decays one stack at the bearer's turn end

        var panic = statuses.First(s => s.Id == "panic");
        var panicPassive = Assert.Single(panic.PassiveModifiers);
        Assert.Equal(PassiveModifierPipeline.TurnStartDraw, panicPassive.Pipeline);
        Assert.Equal(-1, panicPassive.Magnitude);

        // Bookworm cancels the bearer's Paperwork at its turn start, before the tick (BookwormStatusTests
        // proves the numbers in a live fight).
        var bookworm = statuses.First(s => s.Id == "bookworm");
        Assert.Equal(StatusPolarity.Buff, bookworm.Polarity);
        Assert.Equal("TurnStarted", Assert.Single(bookworm.Triggers).Event);
    }
}
