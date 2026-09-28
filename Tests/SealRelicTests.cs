using System.Text.Json;
using BnbContent.Converter.Relics;

namespace BnbContent.Tests;

// A relic that lays Seal lays it the way a card does — with the "at 3 Seal, Ratify" conversion, which rides on
// the keyword's card node and not on the status (audit, 2026-09-28: Refuse Docket and Deferred Signet laid the
// bare status, so an enemy could sit at 3+ Seal and never be Ratified).
public class SealRelicTests
{
    [Theory]
    [InlineData("refuse_docket")]
    [InlineData("deferred_signet")]
    public void A_relic_that_seals_carries_the_ratify_conversion(string relicId)
    {
        var rule = RelicRules.All().Single(r => r.Id == relicId);
        var json = JsonSerializer.Serialize(rule.Triggers.Select(t => t.Program.GetRawText()));
        Assert.Contains("seal", json, StringComparison.Ordinal);
        Assert.Contains("ratified", json, StringComparison.Ordinal);
    }
}
