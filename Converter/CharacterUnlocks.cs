namespace BnbContent.Converter;

// WHO MAY BE PLAYED (user, 2026-10-23): a new profile plays the Lieutenant only. Beating the Act I boss for the first
// time — as anyone — unlocks the Sorcerer's Apprentice; beating the Act II boss AS HIM unlocks the Hedge Witch;
// beating the Act III boss AS HER unlocks the Bureaucrat.
//
// The roster carries each lock as its UnlockFlag (the engine's gate, MetaProgression.AvailableCharacters). WHEN a
// flag is earned is read by the frontend off the game's own words, "unlock:<character>" = "<who>:<bosses>" — who
// must be playing (or "any"), and how many act bosses that run must have beaten — so the chain lives here, with
// the content, and the frontend only counts. "unlock.hint:<character>" is what the locked card says.
public static class CharacterUnlocks
{
    public const string Any = "any";

    public static readonly IReadOnlyList<(string Character, string Flag, string By, int Bosses, string Hint)> Chain =
    [
        (Apprentice.ApprenticeCharacter.Id, "bnb.unlocked.sorcerers_apprentice", Any, 1,
            "Defeat the Act I boss to unlock."),
        (Witch.WitchCharacter.Id, "bnb.unlocked.hedge_witch", Apprentice.ApprenticeCharacter.Id, 2,
            "Defeat the Act II boss as the Sorcerer's Apprentice to unlock."),
        (ConversionPools.BureaucratId, "bnb.unlocked.bureaucrat", Witch.WitchCharacter.Id, 3,
            "Defeat the Act III boss as the Hedge Witch to unlock."),
    ];

    public static string? FlagOf(string character) => Chain.FirstOrDefault(c => c.Character == character).Flag;

    public static IReadOnlyDictionary<string, string> Extra() =>
        Chain.SelectMany(c => new[]
        {
            KeyValuePair.Create($"unlock:{c.Character}", $"{c.By}:{c.Bosses}"),
            KeyValuePair.Create($"unlock.hint:{c.Character}", c.Hint),
        }).ToDictionary(p => p.Key, p => p.Value);
}
