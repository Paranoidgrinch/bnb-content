using RogueDeck.Core.Combat;
using RogueDeck.Run;
using RogueDeck.Scenario.Authoring;

namespace BnbContent.Converter;

// ACT V — THE DIVINE LEDGER. Since the rework of 2026-11-05 (ACT_V_REWORK_PLAN.md) the six gods live in
// Converter/Gods, one class each; this is the registry that hands them to the rest of the converter.
//
// The design's one SHARED rule for the whole act (boss master §Act V §4) is a UI rule: every god gets a
// prominent DIVINE RULE AREA in the combat screen, always in the same place, filled differently by each.
// It rides on the encounter's PRESENTATION (Extra): the frontend keys its panel off `divineRuleTitle` +
// `divineRule`, and an encounter without them shows no panel, which is every fight in Acts I–IV. The live part
// of the area (the decree now and next, the moon now and next) is read off the statuses tagged
// `divine_now` (LiveTag) on the god's own body.
public static class ActFive
{
    public const int Act = 5;

    // The Extra keys the frontend reads. Written down once here because they are a CONTRACT with the Godot
    // side rather than a name only this file uses.
    public const string RuleTitleKey = "divineRuleTitle";
    public const string RuleTextKey = "divineRule";

    // The presentation tag of a status the rule area reads live: the state of the rule now, and what comes next.
    public const string LiveTag = "divine_now";

    public static IReadOnlyDictionary<string, (string Title, string Rule)> RuleAreas { get; } =
        new Dictionary<string, (string, string)>
        {
            [Gods.Nisaba.EncounterId] = (
                "The Reed Stylus",
                "What she writes happens. Each sentence stands beside her as a body with a countdown: break it in "
                + "time and it is erased, or it comes true. At 30 % only the Final Line is left — and it erases you."),
            [Gods.Inanna.EncounterId] = (
                "The Seven Gates",
                "She comes down wearing the seven regalia of the Descent. Every 130 HP she loses she passes a gate, "
                + "and you choose which of them she leaves there. With the last one gone she is the Queen of Battle."),
            [Gods.Nanshe.EncounterId] = (
                "The Just Measure",
                "Every day has its portion: 3 Energy and 5 cards. What you take from tomorrow comes off the next "
                + "day, never below 1 and 1, and what is still owed after Day III is collected: 10 a point."),
            [Gods.NannaSuen.EncounterId] = (
                "The Counted Moon",
                "A fixed ring of four phases, one a round: New Moon, Waxing, Full Moon, Waning. Each phase has one "
                + "move and one rule; the phase now and the next are on him. Every orbit he completes makes him brighter."),
            [Gods.Utu.EncounterId] = (
                "The Oath and the Saw",
                "Each turn three oaths are in your hand; the Block is paid at once. Deal him less than you swore and he "
                + "gains a Witness. His Saw of Judgment returns half of what you dealt him the turn before."),
            [Gods.Enlil.EncounterId] = (
                "The Ring of Decrees",
                "Eight decrees in a ring; exactly one is in force each round, and the next is always announced. "
                + "Every decree binds him as it binds you, and nothing removes, shortens or changes one."),
        };

    // The presentation hints one encounter carries, empty for anything that is not a god.
    public static IReadOnlyDictionary<string, string> Extra(string encounterId) =>
        RuleAreas.TryGetValue(encounterId, out var area)
            ? new Dictionary<string, string> { [RuleTitleKey] = area.Title, [RuleTextKey] = area.Rule }
            : new Dictionary<string, string>();

    // The statuses the rule area shows live, in the order they are read.
    public static IReadOnlyList<string> LiveStatuses() =>
    [
        .. Gods.NannaSuen.Ring.Select(p => Gods.NannaSuen.PhaseId(p.Key)),
        .. Gods.NannaSuen.Ring.Select(p => Gods.NannaSuen.NextId(p.Key)),
        .. Gods.Enlil.Ring.Select(d => Gods.Enlil.DecreeId(d.Key)),
        .. Gods.Enlil.Ring.Select(d => Gods.Enlil.NextId(d.Key)),
    ];

    // Everything the act's gods put into the document.
    public static IReadOnlyList<StatusData> All() =>
    [
        .. Gods.GodShapes.Words(), .. Gods.Nisaba.Statuses(), .. Gods.Inanna.Statuses(), .. Gods.Nanshe.Statuses(),
        .. Gods.NannaSuen.Statuses(), .. Gods.Utu.Statuses(), .. Gods.Enlil.Statuses(),
    ];

    public static IReadOnlyList<CardData> GivenCards() =>
        [.. Gods.Nanshe.Cards(), .. Gods.NannaSuen.Cards(), .. Gods.Utu.Cards(), .. Gods.Enlil.Cards()];

    public static EffectProgram<EnemyActionContext>? Intent(string enemyId, string intentId) =>
        Gods.Nisaba.Intent(enemyId, intentId)
        ?? Gods.Inanna.Intent(enemyId, intentId)
        ?? Gods.Nanshe.Intent(enemyId, intentId)
        ?? Gods.NannaSuen.Intent(enemyId, intentId)
        ?? Gods.Utu.Intent(enemyId, intentId)
        ?? Gods.Enlil.Intent(enemyId, intentId);

    // What each god brings onto the player at the first bell.
    public static IReadOnlyList<StartingStatusSpec> HeroOpening(string enemyId) => enemyId switch
    {
        Gods.Nanshe.EnemyId => Gods.Nanshe.HeroOpening(),
        Gods.Enlil.EnemyId => Gods.Enlil.HeroOpening(),
        _ => [],
    };

    // What falls with what.
    public static IReadOnlyList<EncounterTriggerData> Triggers(string enemyId) => enemyId switch
    {
        Gods.Nisaba.EnemyId => Gods.Nisaba.Triggers(),
        _ => [],
    };
}
