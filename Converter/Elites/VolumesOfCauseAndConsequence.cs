using System.Text.Json;
using RogueDeck.Core.Combat;
using RogueDeck.Scenario.Authoring;
using BnbContent.Converter.Cards;

namespace BnbContent.Converter.Elites;

// ── Volumes of Cause and Consequence (Act II elite) ───────────────────────────────────────────────────────
//
// Two volumes on separate stands with a Concordance line drawn between them. Causes cites your cards;
// Consequences hits you with whatever the citation turned out to mean. Fulfil a Causes reference and Causes
// itself is wounded — but Consequences comes down 6 harder. Fail it and you take the ordinary Act-II debt,
// and Consequences comes down 6 softer.
//
// The choice is never the same twice, because it depends on which volume you are trying to kill, how much
// Block is standing, and what Consequences is about to do.
public static class VolumesOfCauseAndConsequence
{
    public const string CausesId = "volume_of_causes";
    public const string ConsequencesId = "volume_of_consequences";

    public const string TheCausesId = "the_volume_of_causes";
    public const string TheConsequencesId = "the_volume_of_consequences";
    public const string CausesReferenceId = "causes_reference";
    public const string CausesReferenceMark = "referenced_by_causes";
    public const string SupportedId = "supported_result";
    public const string UnsupportedId = "unsupported_result";
    public const string SurvivorId = "concordance_broken";

    // What Causes has announced it will cite: 1 = an ordinary citation, 2 = a false premise, which redacts
    // the same card. Kept on the player, because that is who both ends of the rule can read.
    private static CounterId CitePendingCounter => new("causes_cite_pending");

    private const int FulfilledHpLoss = 9;
    private const int FulfilledBlockLoss = 8;
    private const int ConcordanceSwing = 6;

    private static readonly ICombatantTargetSelector Opponent = CombatantTargetSelectors.LowestHealthEnemyOfSource;
    private static readonly ICombatantTargetSelector Self = CombatantTargetSelectors.Source;

    // Seen from the PLAYER's side, which is where the fulfilment hook runs.
    private static ICombatantTargetSelector TheCauses =>
        CombatantTargetSelectors.AllEnemiesOfSourceWithStatus(new StatusDefinitionId(TheCausesId));
    private static ICombatantTargetSelector TheConsequences =>
        CombatantTargetSelectors.AllEnemiesOfSourceWithStatus(new StatusDefinitionId(TheConsequencesId));

    // …and from a VOLUME's side, where the failure hook and the survivor rule run.
    private static ICombatantTargetSelector AlliedConsequences =>
        CombatantTargetSelectors.AllAlliesOfSourceWithStatus(new StatusDefinitionId(TheConsequencesId));

    public static IEnumerable<StatusData> Statuses() =>
    [
        Marker(TheCausesId, "Volume of Causes",
            "Causes cites your cards. Fulfil a citation and Causes itself is wounded — but Consequences comes "
            + "down 6 harder."),
        Marker(TheConsequencesId, "Volume of Consequences",
            "Consequences hits you with whatever the citation turned out to mean: 6 harder when you fulfilled "
            + "it, 6 softer when you did not."),
        Concordance(SupportedId, "Supported Result",
            "The Concordance holds. The next direct attack from Consequences lands 6 harder.", +ConcordanceSwing),
        Concordance(UnsupportedId, "Unsupported Result",
            "The Concordance is broken. The next direct attack from Consequences lands 6 softer.", -ConcordanceSwing),
        Survivor(),
        // 10.2: the Concordance is written into the reference itself, on both of its outcomes.
        ActTwo.Reference(CausesReferenceId, "Cited by Causes", CausesReferenceMark,
            "A card the Volume of Causes has cited. Playing it wounds Causes and strengthens Consequences; "
            + "letting it go owes Causes an Overdue and weakens Consequences.",
            // Causes ANNOUNCES a citation in its intent and the citation lands after the player's next
            // draw — because a card cited during the enemy's turn is a card about to be discarded, and the
            // design means a citation you can actually answer. This is the same beat every other Act-II
            // citation uses.
            cite: CiteWhatWasAnnounced(),
            onFulfilled: Fulfilled(), onFailed: Failed()),
    ];

    // ── 10.2 Concordance, fulfilled ───────────────────────────────────────────────────────────────────────
    //
    // "Causes loses 9 HP and up to 8 current Block; Consequences gains Supported Result." The HP loss is not a
    // Damage event, so it is a health SET — Causes' own Block cannot stop the thing its own citation cost it.
    private static IEffectNode<CardPlayedTriggeredEffectContext> Fulfilled() =>
        new CausalSequenceEffectNode<CardPlayedTriggeredEffectContext>(
        [
            new ForEachTargetEffectNode<CardPlayedTriggeredEffectContext>(TheCauses,
                new CausalSequenceEffectNode<CardPlayedTriggeredEffectContext>(
                [
                    new SetHealthNode<CardPlayedTriggeredEffectContext>(
                        CombatantTargetSelectors.IterationTarget,
                        new SubtractExpression<CardPlayedTriggeredEffectContext>(
                            new CombatantCurrentHealthExpression<CardPlayedTriggeredEffectContext>(
                                CombatantTargetSelectors.IterationTarget),
                            new ConstantExpression<CardPlayedTriggeredEffectContext>(FulfilledHpLoss))),
                    new ModifyDefensivePoolNode<CardPlayedTriggeredEffectContext>(
                        CombatantTargetSelectors.IterationTarget, StandardCombatIds.BlockDefensivePool,
                        new NegateExpression<CardPlayedTriggeredEffectContext>(
                            new MinExpression<CardPlayedTriggeredEffectContext>(
                                new ConstantExpression<CardPlayedTriggeredEffectContext>(FulfilledBlockLoss),
                                new CombatantDefensivePoolExpression<CardPlayedTriggeredEffectContext>(
                                    CombatantTargetSelectors.IterationTarget,
                                    StandardCombatIds.BlockDefensivePool)))),
                ])),
            SetResult<CardPlayedTriggeredEffectContext>(TheConsequences, SupportedId, UnsupportedId),
        ]);

    // "Normal Reference failure occurs. Then Consequences gains Unsupported Result." The failure hook runs on
    // Causes at its own turn start, so Consequences is an ALLY here, not an enemy.
    private static IEffectNode<TurnStartedTriggeredEffectContext> Failed() =>
        SetResult<TurnStartedTriggeredEffectContext>(AlliedConsequences, UnsupportedId, SupportedId);

    // "Supported and Unsupported replace one another. They do not stack." One goes on as the other comes off,
    // and re-applying the one already standing is not a second copy — the status merges at one stack.
    private static IEffectNode<TContext> SetResult<TContext>(
        ICombatantTargetSelector consequences, string gained, string replaced) where TContext : class =>
        new ForEachTargetEffectNode<TContext>(consequences,
            new CausalSequenceEffectNode<TContext>(
            [
                new RemoveStatusNode<TContext>(
                    CombatantTargetSelectors.IterationTarget, new StatusDefinitionId(replaced)),
                new RemoveStatusNode<TContext>(
                    CombatantTargetSelectors.IterationTarget, new StatusDefinitionId(gained)),
                new ApplyStatusNode<TContext>(
                    CombatantTargetSelectors.IterationTarget, new StatusDefinitionId(gained),
                    new ConstantExpression<TContext>(1)),
            ]));

    // ── 10.3 / 10.4 The two results ───────────────────────────────────────────────────────────────────────
    //
    // A passive modifier on direct damage, spent by the attack that carries it. Enforce the Result is
    // therefore written as a flat 17 and comes out at 11 / 17 / 23 without the number being stated three
    // times — and every other Consequences attack is swung by the Concordance too, which is what the design's
    // "the next direct Consequences attack" says.
    private static StatusData Concordance(string id, string name, string description, int swing)
    {
        var spend = new EffectProgram<DamageDealtTriggeredEffectContext>(
            new RemoveStatusNode<DamageDealtTriggeredEffectContext>(Self, new StatusDefinitionId(id)));

        return new StatusData
        {
            Id = id,
            NameKey = name,
            DescriptionKey = description,
            Polarity = swing > 0 ? StatusPolarity.Buff : StatusPolarity.Debuff,
            StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
            UsesStacks = false,
            Tags = [],
            PassiveModifiers =
            [
                new PassiveModifierData(PassiveModifierPipeline.DamageDealt,
                    PassiveModifierOperation.AddFlat, swing, RestrictDamageKind: DamageKind.Direct),
            ],
            Triggers =
            [
                new StatusTriggerData("DamageDealt", JsonSerializer.SerializeToElement(
                    spend, CombatJson.CreateOptions<DamageDealtTriggeredEffectContext>())),
            ],
        };
    }

    // ── 10.8 Survivor state ───────────────────────────────────────────────────────────────────────────────
    //
    // "If one Volume dies, the Concordance breaks, the survivor loses Supported/Unsupported and gains 1
    // Strength." Carried by both volumes and fired on the BEARER's own downing, which is what makes it run
    // exactly once — an Anywhere-scoped copy on each volume would run twice for one death and hand out two
    // Strength.
    //
    // The thing to know about a Downed program: `Source` is the DOWNED combatant, not the status's bearer
    // (CombatantDownedTriggeredEffectTargetResolver). So the survivor is written as "the allies of the one
    // that just fell" — and because ally selectors resolve living combatants only, a simultaneous death finds
    // no survivor and hands out nothing, which is the design's own rule.
    private static StatusData Survivor()
    {
        var survivor = CombatantTargetSelectors.AllAlliesOfSourceWithStatus(new StatusDefinitionId(SurvivorId));

        var program = new EffectProgram<CombatantDownedTriggeredEffectContext>(
            new ForEachTargetEffectNode<CombatantDownedTriggeredEffectContext>(survivor,
                new CausalSequenceEffectNode<CombatantDownedTriggeredEffectContext>(
                [
                    new RemoveStatusNode<CombatantDownedTriggeredEffectContext>(
                        CombatantTargetSelectors.IterationTarget, new StatusDefinitionId(SupportedId)),
                    new RemoveStatusNode<CombatantDownedTriggeredEffectContext>(
                        CombatantTargetSelectors.IterationTarget, new StatusDefinitionId(UnsupportedId)),
                    new ApplyStatusNode<CombatantDownedTriggeredEffectContext>(
                        CombatantTargetSelectors.IterationTarget, new StatusDefinitionId("strength"),
                        new ConstantExpression<CombatantDownedTriggeredEffectContext>(1)),
                ])));

        return Rule(SurvivorId, "Concordance Broken",
            "One volume left standing. The line between them is gone, and it reads alone.",
            [
                new StatusTriggerData("Downed", JsonSerializer.SerializeToElement(
                    program, CombatJson.CreateOptions<CombatantDownedTriggeredEffectContext>())),
            ]);
    }

    // ── 10.5 / 10.6 Intents ───────────────────────────────────────────────────────────────────────────────
    public static EffectProgram<EnemyActionContext>? Intent(string enemyId, string intentId) =>
        $"{enemyId}.{intentId}" switch
        {
            // Causes cites your hand and guards itself behind the citation.
            $"{CausesId}.establish_the_premise" => Program(new CausalSequenceEffectNode<EnemyActionContext>(
                [Announce(1), new GainBlockNode<EnemyActionContext>(Self, Const(8))])),
            $"{CausesId}.cause_without_warning" => Program(Damage(14)),
            // "Reference 1 valid current-hand card. That same card also becomes Redacted."
            $"{CausesId}.insert_a_false_premise" => Program(Announce(2)),
            $"{CausesId}.repeat_the_argument" => Program(new CausalSequenceEffectNode<EnemyActionContext>(
            [
                Damage(10),
                new ApplyStatusNode<EnemyActionContext>(
                    Opponent, new StatusDefinitionId(Keywords.Paperwork), Const(1)),
            ])),

            // Consequences hits with whatever the Concordance says. 17 flat; the result swings it to 11 or 23
            // and is spent by the blow.
            $"{ConsequencesId}.enforce_the_result" => Program(Damage(17)),
            $"{ConsequencesId}.consequence_without_cause" => Program(
                new CausalSequenceEffectNode<EnemyActionContext>(
                [
                    Damage(12),
                    new ApplyStatusNode<EnemyActionContext>(
                        Opponent, new StatusDefinitionId(Keywords.Doubt), Const(1)),
                ])),
            $"{ConsequencesId}.return_to_the_premise" => Program(
                new GainBlockNode<EnemyActionContext>(Self, Const(8))),
            // "Result Filed as Fact: BOTH volumes gain 14 Block."
            $"{ConsequencesId}.result_filed_as_fact" => Program(
                new ForEachTargetEffectNode<EnemyActionContext>(
                    CombatantTargetSelectors.AllAlliesOfSourceWithStatus(new StatusDefinitionId(SurvivorId)),
                    new GainBlockNode<EnemyActionContext>(
                        CombatantTargetSelectors.IterationTarget, Const(14)))),
            _ => null,
        };

    // The intent's half: announce what will be cited on the player's next hand.
    private static IEffectNode<EnemyActionContext> Announce(int kind) =>
        new SetCombatantCounterNode<EnemyActionContext>(
            Opponent, CitePendingCounter, Const(kind), relative: false);

    // The draw's half: cite the first card of the new hand, and redact it too if a false premise was the
    // thing announced.
    private static IEffectNode<CardsDrawnTriggeredEffectContext> CiteWhatWasAnnounced()
    {
        var player = CombatantTargetSelectors.Source;
        var first = new CardInZoneExpression<CardsDrawnTriggeredEffectContext>(CardZone.Hand, 0);
        var pending = new CombatantCounterExpression<CardsDrawnTriggeredEffectContext>(
            player, CitePendingCounter);

        return new ConditionalEffectNode<CardsDrawnTriggeredEffectContext>(
            new ComparisonExpression<CardsDrawnTriggeredEffectContext>(
                pending, ComparisonOperator.Greater,
                new ConstantExpression<CardsDrawnTriggeredEffectContext>(0)),
            new CausalSequenceEffectNode<CardsDrawnTriggeredEffectContext>(
            [
                new MarkCardInstanceNode<CardsDrawnTriggeredEffectContext>(
                    player, first, new TagId(CausesReferenceMark)),
                new ConditionalEffectNode<CardsDrawnTriggeredEffectContext>(
                    new ComparisonExpression<CardsDrawnTriggeredEffectContext>(
                        pending, ComparisonOperator.Equal,
                        new ConstantExpression<CardsDrawnTriggeredEffectContext>(2)),
                    ActTwo.Redact<CardsDrawnTriggeredEffectContext>(player, first)),
                new SetCombatantCounterNode<CardsDrawnTriggeredEffectContext>(
                    player, CitePendingCounter,
                    new ConstantExpression<CardsDrawnTriggeredEffectContext>(0), relative: false),
            ]));
    }

    private static EffectProgram<EnemyActionContext> Program(IEffectNode<EnemyActionContext> body) => new(body);

    private static IEffectNode<EnemyActionContext> Damage(int amount) =>
        new DealDamageNode<EnemyActionContext>(Opponent, Const(amount));

    private static ConstantExpression<EnemyActionContext> Const(int value) => new(value);

    // A marker still owes the player an explanation on hover: naming it twice explains nothing.
    private static StatusData Marker(string id, string name, string description) =>
        Rule(id, name, description, []);

    private static StatusData Rule(
        string id, string name, string description, IReadOnlyList<StatusTriggerData> triggers) => new()
        {
            Id = id,
            NameKey = name,
            DescriptionKey = description,
            Polarity = StatusPolarity.Neutral,
            StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
            UsesStacks = false,
            Tags = [],
            PassiveModifiers = [],
            Triggers = triggers,
        };
}
