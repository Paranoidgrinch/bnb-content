using RogueDeck.Run;
using RogueDeck.Sandbox.Composition;
using RogueDeck.Sandbox.Run;

namespace BnbContent.Converter.Playtest;

// ── THE TRIAL RUNNER'S PACKAGE CHOICES (LT_RUNNER_PLAN, "Package choices take the better option") ─────────────
// Read off the static card rating (CardRating.PerEnergyById — points per Energy, the same reading as
// --card-rating, extended to every card the document carries):
//
//   card offer    the highest-rated card; skipped when every offer rates below the deck's average
//   relic         the first offered (there is no relic rating)
//   improvement   the card whose "+" gains the most by rating; when none gains, the most-played attack (what the
//                 bot has played on this progression so far), else the first offered
//   removal       the weakest card — a starter Strike or Defend first (the Lieutenant's Watch Cut, Buckler Up)
//
// Deterministic: the same offers, deck and play counts give the same answer.
public sealed class TrialPackages
{
    // The Lieutenant's starter Strike and Defend.
    public static readonly IReadOnlySet<string> StarterBasics = new HashSet<string>(StringComparer.Ordinal)
    {
        "lt_watch_cut", "lt_buckler_up",
    };

    private readonly IReadOnlyDictionary<string, CardRating.Value> _rating;

    public TrialPackages(RunBlueprint game) => _rating = CardRating.PerEnergyById(game);

    public double Rate(string id) => _rating.TryGetValue(id, out var v) ? v.PerEnergy : 0;

    private bool Attack(string id) => _rating.TryGetValue(id, out var v) && v.Attack;

    private static string Id(EntityArt art) => art.UpgradeLevel > 0 ? art.Id + "+" : art.Id;

    private string DeckId(RunCardInstance card) =>
        card.UpgradeLevel > 0 && _rating.ContainsKey(card.DefinitionId.value + "+") ? card.DefinitionId.value + "+" : card.DefinitionId.value;

    public enum Kind { Card, Relic, Improve, Remove, Other }

    public static Kind KindOf(EntitySelectionRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.Intent == RunChoiceIntent.Remove || request.Purpose.StartsWith("remove", StringComparison.OrdinalIgnoreCase))
            return Kind.Remove;
        if (request.Purpose.Contains("improve", StringComparison.OrdinalIgnoreCase) || request.Purpose.Contains("upgrade", StringComparison.OrdinalIgnoreCase))
            return Kind.Improve;
        var arts = Enumerable.Range(0, request.Displays.Count).Select(request.ArtAt).ToList();
        if (arts.Count > 0 && arts.All(a => a?.Kind == EntityArt.Relic))
            return Kind.Relic;
        if (arts.Count > 0 && arts.All(a => a?.Kind == EntityArt.Card))
            return Kind.Card;
        return Kind.Other;
    }

    public IReadOnlyList<int> Pick(EntitySelectionRequest request, RunState run, IReadOnlyDictionary<string, int> played)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(run);
        ArgumentNullException.ThrowIfNull(played);
        var n = request.Displays.Count;
        if (n == 0)
            return [];
        var ids = Enumerable.Range(0, n).Select(i => request.ArtAt(i) is { } art ? Id(art) : "").ToList();
        var deck = run.Deck.Select(DeckId).ToList();
        switch (KindOf(request))
        {
            case Kind.Card:
            {
                var best = Enumerable.Range(0, n).OrderByDescending(i => Rate(ids[i])).ThenBy(i => i).First();
                var average = deck.Count == 0 ? 0 : deck.Average(Rate);
                if (request.AllowSkip && Rate(ids[best]) < average)
                    return [];
                return [best];
            }
            case Kind.Relic:
                return [0];
            case Kind.Improve:
            {
                var gains = Enumerable.Range(0, n)
                    .Select(i => (i, Gain: _rating.ContainsKey(ids[i] + "+") ? Rate(ids[i] + "+") - Rate(ids[i]) : 0))
                    .OrderByDescending(x => x.Gain).ThenBy(x => x.i).ToList();
                if (gains[0].Gain > 0)
                    return [gains[0].i];
                var attack = Enumerable.Range(0, n).Where(i => Attack(ids[i]))
                    .OrderByDescending(i => played.GetValueOrDefault(ids[i])).ThenBy(i => i).ToList();
                return [attack.Count > 0 ? attack[0] : 0];
            }
            case Kind.Remove:
            {
                var basics = Enumerable.Range(0, n).Where(i => StarterBasics.Contains(ids[i])).ToList();
                var pool = basics.Count > 0 ? basics : [.. Enumerable.Range(0, n)];
                return [pool.OrderBy(i => Rate(ids[i])).ThenBy(i => i).First()];
            }
            default:
                return [.. Enumerable.Range(0, Math.Min(request.Count, n))];
        }
    }

    public string Describe(EntitySelectionRequest request, IReadOnlyList<int> picks)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(picks);
        string Name(int i) => request.ArtAt(i) is { } art ? $"{Id(art)} ({Rate(Id(art)):0.0})" : request.Displays[i];
        var offered = string.Join(", ", Enumerable.Range(0, request.Displays.Count).Select(Name));
        return $"{KindOf(request).ToString().ToLowerInvariant()} -> {(picks.Count == 0 ? "skip" : string.Join(", ", picks.Select(Name)))} of [{offered}]";
    }
}
