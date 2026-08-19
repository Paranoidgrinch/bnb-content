using System.Text.Json;
using RogueDeck.Core.Combat;
using RogueDeck.Run;

namespace BnbContent.Converter;

// Cross-combatant enemy passives — reactions to PLAYER actions — authored as per-ENCOUNTER triggered effects
// (EncounterTriggerData), not owner-scoped status triggers (which fire only when the bearer is the event
// subject). Keyed by enemy id; EncounterMapper aggregates the triggers of an encounter's enemies onto the
// EncounterDefinition, so a trigger is active exactly when its enemy is in the fight. Programs are RAW
// EffectPrograms (the arc's richer expressions aren't in CombatNodeModel), serialized via CombatJson; they
// self-gate and target the enemy via AllEnemiesOfSource (source = the acting hero).
public static class EncounterPassives
{
    // The B&B card TYPES used for card-type sequencing (emitted as combat tags by CardMapper).
    private static readonly string[] CardTypes = { "action", "spell", "form" };

    public static IReadOnlyList<EncounterTriggerData> ForEnemy(string enemyId) => enemyId switch
    {
        "wrong_window_scribe" => [NotThisCounter()],
        "triplicate_examiner" => [ThreeCopiesRequired()],
        "oath_candle" => [WitnessTheSeal()],
        "contradictory_signpost" => [BothDirectionsMandatory()],
        _ => Array.Empty<EncounterTriggerData>(),
    };

    // "Both Directions Mandatory" (Contradictory Signpost): the FIRST card the player plays each turn picks the
    // direction — an Attack takes the LEFT road (Dangerous Shortcut), anything else the RIGHT one (Long
    // Administrative Route). The choice is stored as a counter on the Signpost, which its intent rules read;
    // playing no card at all leaves the counter at 0 and it posts "No Route Listed" instead.
    private static EncounterTriggerData BothDirectionsMandatory()
    {
        var signpost = CombatantTargetSelectors.AllEnemiesOfSourceWithStatus(
            new StatusDefinitionId(PassiveStatuses.BothDirectionsMandatoryId));

        EffectProgram<CardPlayedTriggeredEffectContext> Program() =>
            new(new ConditionalEffectNode<CardPlayedTriggeredEffectContext>(
                new ComparisonExpression<CardPlayedTriggeredEffectContext>(
                    new CardsPlayedThisTurnExpression<CardPlayedTriggeredEffectContext>(CombatantTargetSelectors.Source),
                    ComparisonOperator.Equal,
                    new ConstantExpression<CardPlayedTriggeredEffectContext>(1)),
                new ConditionalEffectNode<CardPlayedTriggeredEffectContext>(
                    new FirstCardPlayedThisTurnHasTagExpression<CardPlayedTriggeredEffectContext>(
                        CombatantTargetSelectors.Source, new TagId("attack")),
                    Route(signpost, 1),
                    Route(signpost, 2))));

        return new EncounterTriggerData("CardPlayed",
            JsonSerializer.SerializeToElement(Program(), CombatJson.CreateOptions<CardPlayedTriggeredEffectContext>()));
    }

    private static SetCombatantCounterNode<CardPlayedTriggeredEffectContext> Route(
        ICombatantTargetSelector signpost, int route) =>
        new(signpost, PassiveStatuses.SignpostedRouteCounter,
            new ConstantExpression<CardPlayedTriggeredEffectContext>(route), relative: false);

    // "Witness the Seal" (Oath Candle): the first time each round ANOTHER enemy gains Block, that enemy gains 3
    // more. Everything the program needs is expressed with selectors, since an encounter trigger has no filters
    // and cannot name a combatant:
    //   · `alliesWithStatus(witness_the_seal)` = the Candle, but only on the GAINER'S side — when the hero
    //     guards itself the loop finds nobody, so the hero is never witnessed;
    //   · the loop body runs once per Candle present, which is also the "is the Candle here" gate;
    //   · inside it, `iterationTarget` IS the Candle, so its once-per-round latch can be read and written;
    //   · the gainer carrying the marker means the Candle witnessed itself — the design's "no recursion".
    // The latch is cleared at RoundEnded by the marker status (PassiveStatuses.WitnessTheSeal).
    private static EncounterTriggerData WitnessTheSeal()
    {
        var marker = new StatusDefinitionId(PassiveStatuses.WitnessTheSealId);
        var gainer = CombatantTargetSelectors.EventTarget;
        var candle = CombatantTargetSelectors.IterationTarget;

        var body = new ConditionalEffectNode<BlockGainedTriggeredEffectContext>(
            new AndExpression<BlockGainedTriggeredEffectContext>(
                new ComparisonExpression<BlockGainedTriggeredEffectContext>(
                    new CombatantStatusStacksExpression<BlockGainedTriggeredEffectContext>(gainer, marker),
                    ComparisonOperator.Equal,
                    new ConstantExpression<BlockGainedTriggeredEffectContext>(0)),
                new ComparisonExpression<BlockGainedTriggeredEffectContext>(
                    new CombatantCounterExpression<BlockGainedTriggeredEffectContext>(
                        candle, PassiveStatuses.WitnessedThisRoundCounter),
                    ComparisonOperator.Equal,
                    new ConstantExpression<BlockGainedTriggeredEffectContext>(0))),
            new SequenceEffectNode<BlockGainedTriggeredEffectContext>(new IEffectNode<BlockGainedTriggeredEffectContext>[]
            {
                new GainBlockNode<BlockGainedTriggeredEffectContext>(
                    gainer, new ConstantExpression<BlockGainedTriggeredEffectContext>(3)),
                new SetCombatantCounterNode<BlockGainedTriggeredEffectContext>(
                    candle, PassiveStatuses.WitnessedThisRoundCounter,
                    new ConstantExpression<BlockGainedTriggeredEffectContext>(1), relative: false),
            }));

        var program = new EffectProgram<BlockGainedTriggeredEffectContext>(
            new ForEachTargetEffectNode<BlockGainedTriggeredEffectContext>(
                CombatantTargetSelectors.AllAlliesOfSourceWithStatus(marker), body));

        return new EncounterTriggerData("BlockGained",
            JsonSerializer.SerializeToElement(program, CombatJson.CreateOptions<BlockGainedTriggeredEffectContext>()));
    }

    // "Not This Counter": the first non-Junk card TYPE each turn is the "Wrong Window"; the first LATER card of
    // that same type makes the Scribe gain 5 Block. Encoded as: on the player's 2nd card of the turn's OPENING
    // type, the Scribe (all enemies of the card's source) gains 5 Block. Faithful simplification: the opening
    // type is literally the first card's type (Junk not skipped) — see ADAPTATIONS.md.
    private static EncounterTriggerData NotThisCounter() =>
        OnNthCardOfTheOpeningType(2,
            new GainBlockNode<CardPlayedTriggeredEffectContext>(
                CombatantTargetSelectors.AllEnemiesOfSource,
                new ConstantExpression<CardPlayedTriggeredEffectContext>(5)));

    // "Three Copies Required": the turn's opening card TYPE is what the Examiner demands in triplicate — the
    // player's THIRD card of that type gives the Examiner 8 Block and the player 1 Doubt. Same simplification
    // as Not This Counter (opening type = literally the first card's type).
    private static EncounterTriggerData ThreeCopiesRequired() =>
        OnNthCardOfTheOpeningType(3,
            new SequenceEffectNode<CardPlayedTriggeredEffectContext>(new IEffectNode<CardPlayedTriggeredEffectContext>[]
            {
                new GainBlockNode<CardPlayedTriggeredEffectContext>(
                    CombatantTargetSelectors.AllEnemiesOfSource,
                    new ConstantExpression<CardPlayedTriggeredEffectContext>(8)),
                // The card's player — the hero in a solo fight — takes the Doubt.
                new ApplyStatusNode<CardPlayedTriggeredEffectContext>(
                    CombatantTargetSelectors.Source,
                    new StatusDefinitionId("doubt"),
                    new ConstantExpression<CardPlayedTriggeredEffectContext>(1)),
            }));

    // The shared shape of the counter passives: fire `effect` when the player plays their Nth card of the type
    // that OPENED the turn. Exactly-N (not ≥N) makes it once per player turn on its own — no cooldown state.
    // The opening type isn't readable as a value, so the program ORs the per-type cases.
    private static EncounterTriggerData OnNthCardOfTheOpeningType(int n, IEffectNode<CardPlayedTriggeredEffectContext> effect)
    {
        ICombatExpression<CardPlayedTriggeredEffectContext, bool>? condition = null;
        foreach (var type in CardTypes)
        {
            var tag = new TagId(type);
            var nthOfOpeningType = new AndExpression<CardPlayedTriggeredEffectContext>(
                new FirstCardPlayedThisTurnHasTagExpression<CardPlayedTriggeredEffectContext>(
                    CombatantTargetSelectors.Source, tag),
                new ComparisonExpression<CardPlayedTriggeredEffectContext>(
                    new CardsPlayedThisTurnWithTagExpression<CardPlayedTriggeredEffectContext>(
                        CombatantTargetSelectors.Source, tag),
                    ComparisonOperator.Equal,
                    new ConstantExpression<CardPlayedTriggeredEffectContext>(n)));
            condition = condition is null
                ? nthOfOpeningType
                : new OrExpression<CardPlayedTriggeredEffectContext>(condition, nthOfOpeningType);
        }

        var program = new EffectProgram<CardPlayedTriggeredEffectContext>(
            new ConditionalEffectNode<CardPlayedTriggeredEffectContext>(condition!, effect));

        return new EncounterTriggerData("CardPlayed",
            JsonSerializer.SerializeToElement(program, CombatJson.CreateOptions<CardPlayedTriggeredEffectContext>()));
    }
}
