using RogueDeck.Core.Combat;
using RogueDeck.Run;
using RogueDeck.Scenario.Authoring;

namespace BnbContent.Converter.Witch;

// The Hedge Witch as a playable character (hedge_witch_master.md; plan W1): her start, her actions and standing
// status, and what is hers alone to be offered.
public static class WitchCharacter
{
    public const string Id = "hedge_witch";
    public const string Name = "The Hedge Witch";
    public const int MaxHp = 70; // plan E1 — the Bureaucrat's, until the bench says otherwise

    public static RunStart Start => StartWith(hotAfterBrew: false);

    public static RunStart StartWith(bool hotAfterBrew) => new()
    {
        HeroName = Name,
        MaxHealth = MaxHp,
        StartingHealth = MaxHp,
        Resources = new Dictionary<string, int> { [StandardRunIds.Gold.Value] = 0 },
        Deck = [.. WitchCards.StarterDeck.Select(id => new CardDefinitionId(id))],
        CombatActions = [new CardDefinitionId(WitchActions.AddIngredient), new CardDefinitionId(WitchActions.Brew)],
        CombatStatuses =
        [
            new StartingStatusSpec(new StatusDefinitionId(WitchKeywords.Cauldron), 1),
            new StartingStatusSpec(new StatusDefinitionId(WitchRules.Habits), 1),
            .. hotAfterBrew ? [new StartingStatusSpec(new StatusDefinitionId(WitchRules.HotRule), 1)] : Array.Empty<StartingStatusSpec>(),
        ],
    };

    // Her cards (and their upgrades) and, later, her relics: offered to her alone.
    public static IReadOnlyList<string> Exclusive =>
        [.. WitchCards.All().Select(c => c.Id), .. WitchRelics.All().Select(r => r.Id)];

    // `hotAfterBrew`: the E6 switch (WitchRules.HotRule), OFF in the shipped game.
    public static RunCharacter Roster(bool hotAfterBrew = false) => new(Id, StartWith(hotAfterBrew), Exclusive: Exclusive);
}
