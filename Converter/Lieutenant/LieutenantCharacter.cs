using RogueDeck.Core.Combat;
using RogueDeck.Run;
using RogueDeck.Scenario.Authoring;

namespace BnbContent.Converter.Lieutenant;

// The Lieutenant as a playable character (lieutenant_master.md §0–§1; LIEUTENANT_PLAN L1): her start, her standing
// status, and what is hers alone to be offered.
public static class LieutenantCharacter
{
    public const string Id = "lieutenant";
    public const string Name = "The Lieutenant";
    public const int MaxHp = 80; // plan L1 — the canon's, ten above the other three, until the bench says otherwise

    public static RunStart Start => new()
    {
        HeroName = Name,
        MaxHealth = MaxHp,
        StartingHealth = MaxHp,
        Resources = new Dictionary<string, int> { [StandardRunIds.Gold.Value] = 0 },
        Deck = [.. LieutenantCards.StarterDeck.Select(id => new CardDefinitionId(id))],
        CombatStatuses = [new StartingStatusSpec(new StatusDefinitionId(LieutenantKeywords.Command), 1)],
    };

    // Her cards (and their upgrades) and her relics: offered to her alone.
    public static IReadOnlyList<string> Exclusive => [.. LieutenantCards.All().Select(c => c.Id)];

    public static RunCharacter Roster() => new(Id, Start, Exclusive: Exclusive);
}
