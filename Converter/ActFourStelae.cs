using RogueDeck.Core.Combat;

namespace BnbContent.Converter;

// Act IV, Stage 1 — The Boundary Stelae. Two officials with measuring instruments, and between them the whole
// of the act's first word.
//
//   The Reed-Cord Surveyor raises a measure and then answers HOW FAR you were from it — the stage's lesson is
//   that precision matters and not merely compliance, so its consequence has bands rather than a verdict.
//   The Crooked Rod Bearer raises a measure whose standard alternates 1 → 3 → 1 → 3. The standard is wrong.
//   The bureaucracy is still predictable, and that is the joke the whole act is built on.
//
// Put them in one room (Encounter 3) and §3.1 decides between them: only ONE Weighed check stands at a time —
// the Primary Measure — and whoever raises it first that turn owns it. The other does not raise a second,
// contradictory one; it OBSERVES the same measure's result (§3.2). That is why both pressure intents ask
// whether a measure is already standing before they raise theirs, and why both consequence intents read the
// record rather than a check of their own.
public static partial class ActFour
{
    public const string SurveyorEnemyId = "reed_cord_surveyor";
    public const string RodBearerEnemyId = "crooked_rod_bearer";

    // What the Surveyor demands. A constant, and deliberately a small one: the solvability filter the elite
    // master calls for (§6.2 — never an impossible requirement) is elite machinery, written once at IV-12,
    // and a standard enemy in the act's opening stage has no business asking for anything a starting hand
    // cannot pay. Two is reachable with one 2-cost card or two 1-cost cards out of three Energy.
    private const int SurveyorMeasure = 2;

    public static EffectProgram<EnemyActionContext>? Intent(string enemyId, string intentId) =>
        $"{enemyId}.{intentId}" switch
        {
            "reed_cord_surveyor.set_the_measure" => SetTheMeasure(10, Const(SurveyorMeasure)),
            "reed_cord_surveyor.re_tension_cord" => SurveyError(16),
            "crooked_rod_bearer.crooked_measure" => CrookedMeasure(11),
            "crooked_rod_bearer.brace_the_standard" => BraceTheStandard(17),
            _ => GranaryIntent(enemyId, intentId)
                 ?? BasinIntent(enemyId, intentId)
                 ?? CausewayIntent(enemyId, intentId)
                 ?? YardIntent(enemyId, intentId)
                 ?? MonumentIntent(enemyId, intentId)
                 ?? LinenIntent(enemyId, intentId)
                 ?? WarrenIntent(enemyId, intentId)
                 ?? CartoucheIntent(enemyId, intentId)
                 ?? BalanceIntent(enemyId, intentId)
                 ?? EliteIntent(enemyId, intentId)
                 ?? BossIntent(enemyId, intentId),
        };

    // ── the Reed-Cord Surveyor ────────────────────────────────────────────────────────────────────────────

    // Strike, and raise the measure — unless one already stands, in which case this body is measuring against
    // somebody else's standard and merely strikes. That single condition IS §3.1: no second check is ever
    // created, so the two officials in Encounter 3 cannot demand contradictory things at once.
    private static EffectProgram<EnemyActionContext> SetTheMeasure(
        int damage, ICombatExpression<EnemyActionContext, int> required) =>
        new(new CausalSequenceEffectNode<EnemyActionContext>(
        [
            new DealDamageNode<EnemyActionContext>(Applicant, Const(damage)),
            new ConditionalEffectNode<EnemyActionContext>(
                new NotExpression<EnemyActionContext>(
                    new TargetHasStatusExpression<EnemyActionContext>(
                        Applicant, new StatusDefinitionId(WeighedId))),
                new ApplyStatusNode<EnemyActionContext>(
                    Applicant, new StatusDefinitionId(WeighedId), required)),
        ]));

    // Brace the cord, and answer the last completed measure by ERROR BAND: exact is let pass, one step away
    // is a minor consequence, two or more is the stronger one. The record is 1 + the distance, so 1 is exact,
    // 2 is a near miss, and 3 or more is a major one. A fight in which no measure has been taken yet reads 0,
    // and 0 answers nothing.
    private static EffectProgram<EnemyActionContext> SurveyError(int block) =>
        new(new CausalSequenceEffectNode<EnemyActionContext>(
        [
            new GainBlockNode<EnemyActionContext>(CombatantTargetSelectors.Source, Const(block)),
            new ConditionalEffectNode<EnemyActionContext>(
                AtLeast(3),
                Paperwork(2),
                new ConditionalEffectNode<EnemyActionContext>(AtLeast(2), Paperwork(1))),
        ]));

    // ── the Crooked Rod Bearer ────────────────────────────────────────────────────────────────────────────

    // Strike, and raise the crooked standard: 1, then 3, then 1 again. The step is kept on the Bearer itself,
    // so two Bearers in one room would each keep their own rhythm — and it is advanced only when a measure was
    // actually raised, or a turn spent measuring against someone else's standard would silently skip a step
    // and break the sequence the player is being taught to read.
    private static EffectProgram<EnemyActionContext> CrookedMeasure(int damage) =>
        new(new CausalSequenceEffectNode<EnemyActionContext>(
        [
            new DealDamageNode<EnemyActionContext>(Applicant, Const(damage)),
            new ConditionalEffectNode<EnemyActionContext>(
                new NotExpression<EnemyActionContext>(
                    new TargetHasStatusExpression<EnemyActionContext>(
                        Applicant, new StatusDefinitionId(WeighedId))),
                new CausalSequenceEffectNode<EnemyActionContext>(
                [
                    new ApplyStatusNode<EnemyActionContext>(
                        Applicant, new StatusDefinitionId(WeighedId),
                        // 1 + 2 × (step mod 2): one, then three, then one.
                        new AddExpression<EnemyActionContext>(
                            Const(1),
                            new MultiplyExpression<EnemyActionContext>(
                                Const(2),
                                new RemainderExpression<EnemyActionContext>(
                                    new CombatantCounterExpression<EnemyActionContext>(
                                        CombatantTargetSelectors.Source, CrookedStep),
                                    Const(2))))),

                    new SetCombatantCounterNode<EnemyActionContext>(
                        CombatantTargetSelectors.Source, CrookedStep, Const(1), relative: true),
                ])),
        ]));

    // Brace the standard, and file for a failed measure — any failure, one sheet. The Bearer does not care
    // how far off you were; only the Surveyor measures error. That difference between the two officials is
    // the stage.
    private static EffectProgram<EnemyActionContext> BraceTheStandard(int block) =>
        new(new CausalSequenceEffectNode<EnemyActionContext>(
        [
            new GainBlockNode<EnemyActionContext>(CombatantTargetSelectors.Source, Const(block)),
            new ConditionalEffectNode<EnemyActionContext>(AtLeast(2), Paperwork(1)),
        ]));

    // ── reading the record ────────────────────────────────────────────────────────────────────────────────

    // "The last completed measure came to at least this" — 2 is any failure, 3 is a major one.
    private static ICombatExpression<EnemyActionContext, bool> AtLeast(int result) =>
        new ComparisonExpression<EnemyActionContext>(
            new CombatantCounterExpression<EnemyActionContext>(Applicant, MeasureResult),
            ComparisonOperator.GreaterOrEqual, Const(result));

    private static ApplyStatusNode<EnemyActionContext> Paperwork(int sheets) =>
        new(Applicant, new StatusDefinitionId(Cards.Keywords.Paperwork), Const(sheets));

    private static ConstantExpression<EnemyActionContext> Const(int value) => new(value);

    // The player: the one combatant every fight marks as the applicant.
    private static ICombatantTargetSelector Applicant { get; } =
        CombatantTargetSelectors.FirstTarget(
            CombatantTargetSelectors.WithStatus(
                CombatantTargetSelectors.AllAliveCombatants,
                new StatusDefinitionId(PassiveStatuses.ApplicantId)));
}
