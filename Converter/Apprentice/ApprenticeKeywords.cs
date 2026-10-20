using System.Text.Json;
using RogueDeck.Core.Combat;
using RogueDeck.Sandbox.Composition;
using RogueDeck.Scenario.Authoring;

namespace BnbContent.Converter.Apprentice;

// The Sorcerer's Apprentice's keywords (sorcerers_apprentice_master.md §2–§3): the wand he carries into every fight
// and the four elements. What Singe, Flow, Grit and Gust DO is the engine's wand rules (RogueDeck-Core WandEngine,
// switched on by ApprenticeWand.Rules); these are their names, words and stacking for the screen and the archive.
// Numbers are the master's BALANCE DRAFT.
public static class ApprenticeKeywords
{
    public const string Wand = "apprentice_wand";
    public const string Singe = "singe";
    public const string Flow = "flow";
    public const string Grit = "grit";
    public const string Gust = "gust";

    public static IReadOnlyList<StatusData> All() =>
    [
        Status(Wand, "The Wand", StatusPolarity.Neutral,
            "Cards may be inscribed into the wand instead of played, and cast as a program in slot order. Playing a " +
            "card gives 1 Focus (up to 6). Inscribing costs 1 Focus (the first each combat is free); a cast costs " +
            "2 Focus for 1–2 cards, 3 for 3–4, 4 for 5–6, and adds Exhaustion (at most 8). Exhaustion falls by 3 at " +
            "the start of your turn."),
        Status(Singe, "Singe", StatusPolarity.Debuff,
            "When this enemy takes a direct hit, 1 Singe is spent and the hit deals 2 more damage, before Block. " +
            "Damage from statuses does not spend it."),
        Status(Flow, "Flow", StatusPolarity.Buff,
            "After a cast, the Flow you had when it began returns that much of its Focus cost (up to the cost) and is spent."),
        Status(Grit, "Grit", StatusPolarity.Buff,
            "Each enemy Attack hit spends 1 Grit and deals 2 less, before Block and Ward Wax."),
        Status(Gust, "Gust", StatusPolarity.Buff,
            "At the start of your next turn, after the draw: draw this many cards, then discard as many."),
        .. ApprenticeRules.All(),
    ];

    internal static StatusTriggerData Trigger<TContext>(EffectProgram<TContext> program, string trigger)
        where TContext : class =>
        new(trigger, JsonSerializer.SerializeToElement(program, CombatJson.CreateOptions<TContext>()));

    internal static StatusData Status(
        string id, string name, StatusPolarity polarity, string description,
        IReadOnlyList<StatusTriggerData>? triggers = null,
        IReadOnlyList<PassiveModifierData>? passives = null) => new()
        {
            Id = id,
            NameKey = name,
            DescriptionKey = description,
            Polarity = polarity,
            StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
            UsesStacks = true,
            Tags = [],
            PassiveModifiers = passives ?? [],
            Triggers = triggers ?? [],
        };
}
