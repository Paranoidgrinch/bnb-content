using System.Text.Json;
using RogueDeck.Core.Combat;
using RogueDeck.Sandbox.Composition;
using RogueDeck.Scenario.Authoring;

namespace BnbContent.Converter;

// ACT V — THE DIVINE LEDGER, all six gods written (V-1 … V-6, 2026-09-09).
//
// The design's one SHARED rule for the whole act (boss master §Act V §4) is a UI rule: every god gets a
// prominent DIVINE RULE AREA in the combat screen, always in the same place, filled completely differently by
// each — the First Tablet, the Eanna Ledger, the Ration Tablet, the Lunar Calendar, the Oaths, the Decrees.
// The player should read it and know at once: this is where the god says what reality currently means.
//
// That area needs somewhere to get its words from, and the engine is the wrong place for them — a rule area is
// presentation, and the engine reads none of it. So it rides on the encounter's PRESENTATION (Extra), which is
// exactly what that dictionary is for: the frontend keys its panel off `divineRuleTitle` + `divineRule`, and an
// encounter without them shows no panel, which is every fight in Acts I–IV.
//
// At V-0 these lines were captions on ported placeholders — they said what each god IS. V-1 … V-6 replaced the
// fights and rewrote every line to describe the live state its area actually shows (the tablet's written rows,
// the ledger's claims, the moon's phase, the standing decree), which is where the area stopped being a caption.
public static partial class ActFive
{
    public const int Act = 5;

    // The Extra keys the frontend reads. Written down once here because they are a CONTRACT with the Godot
    // side rather than a name only this file uses.
    public const string RuleTitleKey = "divineRuleTitle";
    public const string RuleTextKey = "divineRule";

    public static IReadOnlyDictionary<string, (string Title, string Rule)> RuleAreas { get; } =
        new Dictionary<string, (string, string)>
        {
            [Gods.Nisaba.EncounterId] = (
                "The Reed Stylus",
                "What she writes happens. Each sentence stands beside her as a body with a countdown: break it in "
                + "time and it is erased, or it comes true. At 30 % only the Final Line is left — and it erases you."),
            ["act_5_inanna_mistress_of_the_eanna_ledger"] = (
                "The Eanna Ledger",
                "A card she claims is stamped PROPERTY OF EANNA. It stays yours, and its first play each turn "
                + "costs 1 Energy less — and every use of it writes 1 Temple Due. At the Procession what is "
                + "unpaid becomes Arrears, and Arrears do not go away."),
            [NansheEncounterId] = (
                "The Ration Tablet",
                "Three days to a Distribution, and all three portions are on the table before the first one "
                + "begins. Your natural Energy and your natural draw are your share — anything you take "
                + "beyond it comes out of a later day, and no day but the last falls below 1 and 1."),
            [NannaSinEncounterId] = (
                "The Lunar Calendar",
                "Eight phases, one a round, carrying the counts — · I · II · III · IV · III · II · I. The "
                + "card you play on tonight's count is Counted, and comes back free the next time that count "
                + "comes round. So does the move he made on it."),
            [UtuEncounterId] = (
                "Oaths and Witness",
                "He asks for a promise every turn and pays for it before you keep it. Nothing here is ever "
                + "forbidden to you: the card that breaks your Oath is played, and resolves — and then he has "
                + "seen it. What he sees he keeps, and every move he makes is read off it."),
            [EnlilEncounterId] = (
                "Decrees",
                "What stands here is not a threat — it is the rule the fight is being played under. A decree "
                + "is announced a full turn before it is real, runs the count beside it, and cannot be "
                + "removed, shortened or bargained away. Where the wording is universal it binds him too."),
        };

    // The presentation hints one encounter carries, empty for anything that is not a god.
    public static IReadOnlyDictionary<string, string> Extra(string encounterId) =>
        RuleAreas.TryGetValue(encounterId, out var area)
            ? new Dictionary<string, string> { [RuleTitleKey] = area.Title, [RuleTextKey] = area.Rule }
            : new Dictionary<string, string>();

    // Everything the act's gods put into the document. One line per god as they are built.
    public static IReadOnlyList<StatusData> All() =>
        [.. Gods.Nisaba.Statuses(), .. InannaStatuses(), .. NansheStatuses(), .. NannaSinStatuses(),
         .. UtuStatuses(), .. EnlilStatuses()];

    public static IReadOnlyList<CardData> GivenCards() =>
        [.. InannaLedgerCards(), .. NansheRationCards(), .. NannaSinCards()];

    public static EffectProgram<EnemyActionContext>? Intent(string enemyId, string intentId) =>
        Gods.Nisaba.Intent(enemyId, intentId)
        ?? InannaIntent(enemyId, intentId)
        ?? NansheIntent(enemyId, intentId)
        ?? NannaSinIntent(enemyId, intentId)
        ?? UtuIntent(enemyId, intentId)
        ?? EnlilIntent(enemyId, intentId);

    // ── shared idioms ─────────────────────────────────────────────────────────────────────────────────────

    private static ConstantExpression<TContext> Const<TContext>(int value) where TContext : class => new(value);

    private static ICombatExpression<TContext, int> Stacks<TContext>(
        ICombatantTargetSelector body, string statusId) where TContext : class =>
        new CombatantStatusStacksExpression<TContext>(body, new StatusDefinitionId(statusId));

    private static ICombatExpression<TContext, bool> Has<TContext>(
        ICombatantTargetSelector body, string statusId) where TContext : class =>
        new TargetHasStatusExpression<TContext>(body, new StatusDefinitionId(statusId));

    // "The body whose rule this is" — the one combatant wearing the tablet.
    private static ICombatantTargetSelector Bearer(string ruleId) =>
        CombatantTargetSelectors.FirstTarget(
            CombatantTargetSelectors.WithStatus(
                CombatantTargetSelectors.AllAliveCombatants, new StatusDefinitionId(ruleId)));

    private static ICombatantTargetSelector Applicant { get; } =
        CombatantTargetSelectors.FirstTarget(
            CombatantTargetSelectors.WithStatus(
                CombatantTargetSelectors.AllAliveCombatants,
                new StatusDefinitionId(PassiveStatuses.ApplicantId)));

    private static ICombatExpression<TContext, bool> PlayersTurn<TContext>() where TContext : class =>
        new TargetHasStatusExpression<TContext>(
            CombatantTargetSelectors.Source, new StatusDefinitionId(PassiveStatuses.ApplicantId));

    private static IEffectNode<EnemyActionContext> Seq(params IEffectNode<EnemyActionContext>[] steps) =>
        new CausalSequenceEffectNode<EnemyActionContext>(steps);

    private static IEffectNode<EnemyActionContext> Hit(int damage) =>
        new DealDamageNode<EnemyActionContext>(Applicant, Const<EnemyActionContext>(damage));

    private static IEffectNode<EnemyActionContext> Guard(int block) =>
        new GainBlockNode<EnemyActionContext>(
            CombatantTargetSelectors.Source, Const<EnemyActionContext>(block));

    private static IEffectNode<EnemyActionContext> Debuff(string statusId, int stacks) =>
        new ApplyStatusNode<EnemyActionContext>(
            Applicant, new StatusDefinitionId(statusId), Const<EnemyActionContext>(stacks));

    private static StatusTriggerData Trigger<TContext>(
        EffectProgram<TContext> program, string trigger,
        StatusTriggerScope scope = StatusTriggerScope.Bearer) where TContext : class =>
        new(trigger, JsonSerializer.SerializeToElement(program, CombatJson.CreateOptions<TContext>()), scope);

    private static StatusData Face(string id, string name, string description, bool stacks) => new()
    {
        Id = id,
        NameKey = name,
        DescriptionKey = description,
        Polarity = StatusPolarity.Neutral,
        StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
        UsesStacks = stacks,
        Tags = [],
        PassiveModifiers = [],
        Triggers = [],
    };

    private static StatusData Announcement(string id, string name, string description) =>
        Face(id, name, description, stacks: false);

    // A sentence's mark on the turn it was written about, and no longer: it comes off at the end of that
    // turn whether it was felt or not.
    private static StatusData Worn(
        string id, string name, string description, StatusPolarity polarity, PassiveModifierData modifier) => new()
    {
        Id = id,
        NameKey = name,
        DescriptionKey = description,
        Polarity = polarity,
        StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
        UsesStacks = true,
        Tags = [],
        PassiveModifiers = [modifier],
        Triggers =
        [
            Trigger(new EffectProgram<TurnEndedTriggeredEffectContext>(
                new RemoveStatusNode<TurnEndedTriggeredEffectContext>(
                    CombatantTargetSelectors.Source, new StatusDefinitionId(id))),
                nameof(TriggerEvent.TurnEnded)),
        ],
    };

}
