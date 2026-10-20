using RogueDeck.Core.Combat;
using RogueDeck.Run;
using RogueDeck.Scenario.Authoring;

namespace BnbContent.Converter.Apprentice;

// The Sorcerer's Apprentice as a playable character (sorcerers_apprentice_master.md; APPRENTICE_PLAN A1): his start,
// his wand and its five actions, and what is his alone to be offered.
public static class ApprenticeCharacter
{
    public const string Id = "sorcerers_apprentice";
    public const string Name = "The Sorcerer's Apprentice";
    public const int MaxHp = 70; // plan A1 — the baseline the other two start from, until the bench says otherwise

    public static RunStart Start => new()
    {
        HeroName = Name,
        MaxHealth = MaxHp,
        StartingHealth = MaxHp,
        Resources = new Dictionary<string, int> { [StandardRunIds.Gold.Value] = 0 },
        Deck = [.. ApprenticeCards.StarterDeck.Select(id => new CardDefinitionId(id))],
        CombatActions = [.. ApprenticeWand.Actions.Select(id => new CardDefinitionId(id))],
        CombatStatuses = [new StartingStatusSpec(new StatusDefinitionId(ApprenticeKeywords.Wand), 1)],
    };

    // His cards (and their upgrades) and his relics: offered to him alone.
    public static IReadOnlyList<string> Exclusive =>
        [.. ApprenticeCards.All().Select(c => c.Id), .. ApprenticeRelics.All().Select(r => r.Id)];

    public static RunCharacter Roster() => new(Id, Start, Exclusive: Exclusive);
}
