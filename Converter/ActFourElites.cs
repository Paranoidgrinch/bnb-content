using RogueDeck.Core.Combat;
using RogueDeck.Scenario.Authoring;

namespace BnbContent.Converter;

// Act IV — the elite layer. What is shared between its encounters, and the one rule the elite master states
// as binding on all of them.
//
// Like Act III's, these are `partial class ActFour` files: an elite speaks the act's five words and raises
// the act's one measure, and everything it adds beyond that is a counter that lives and dies inside its own
// encounter (§6.1 — Boundary Error, Granary Seals, Labor Owed, and the rest are encounter-local by
// construction, because they are counters and statuses on the elite's own body).
public static partial class ActFour
{
    // The act's bosses, kept apart from the elites only so the pool tests can count them.
    public static readonly IReadOnlySet<string> BossIdentities = new HashSet<string>(StringComparer.Ordinal)
    {
        PharaohEnemyId,
        WeigherEnemyId,
        ArchitectEnemyId,
        LadyEnemyId,
        ScribeEnemyId,
        MotherEnemyId,
    };

    public static IReadOnlyList<StatusData> BossStatuses() =>
        [.. PharaohStatuses(), .. WeigherStatuses(), .. ArchitectStatuses(), .. LadyStatuses(), .. ScribeStatuses(), .. MotherStatuses()];

    // The bosses' own offers: the Architect's four blueprints and the Lady's four seals, never dealt into a
    // deck — pushed into the fight by the body that offers them.
    public static IReadOnlyList<CardData> BossCards() =>
        [.. ArchitectCards(), .. LadySealCards(), .. ScribeScrapeCards(), .. MotherWashCards()];

    public static EffectProgram<EnemyActionContext>? BossIntent(string enemyId, string intentId) =>
        PharaohIntent(enemyId, intentId)
        ?? WeigherIntent(enemyId, intentId)
        ?? ArchitectIntent(enemyId, intentId)
        ?? LadyIntent(enemyId, intentId)
        ?? ScribeIntent(enemyId, intentId)
        ?? MotherIntent(enemyId, intentId);

    public static readonly IReadOnlySet<string> EliteIdentities = new HashSet<string>(StringComparer.Ordinal)
    {
        SurveyorEliteEnemyId,
        ScarabHostEnemyId,
        RopeMasterEnemyId,
        StoneHaulerSummonEnemyId,
        CartoucheKeeperEnemyId,
        LinenOverseerEnemyId,
        TreasuryEnemyId,
        SphinxEnemyId,
        DecanKeeperEnemyId,
        ColossusEnemyId,
        PryBarVeteranEnemyId,
        LampThiefEnemyId,
        CurseBearerEnemyId,
    };

    public static IReadOnlyList<StatusData> EliteStatuses() =>
    [
        .. SurveyorEliteStatuses(),
        .. ScarabHostStatuses(),
        .. RopeMasterStatuses(),
        .. CartoucheKeeperStatuses(),
        .. LinenOverseerStatuses(),
        .. TreasuryStatuses(),
        .. SphinxStatuses(),
        .. TombbreakerStatuses(),
        .. DecanKeeperStatuses(),
        .. ColossusStatuses(),
    ];

    public static EffectProgram<EnemyActionContext>? EliteIntent(string enemyId, string intentId) =>
        SurveyorEliteIntent(enemyId, intentId)
        ?? ScarabHostIntent(enemyId, intentId)
        ?? RopeMasterIntent(enemyId, intentId)
        ?? CartoucheKeeperIntent(enemyId, intentId)
        ?? LinenOverseerIntent(enemyId, intentId)
        ?? TreasuryIntent(enemyId, intentId)
        ?? SphinxIntent(enemyId, intentId)
        ?? TombbreakerIntent(enemyId, intentId)
        ?? DecanKeeperIntent(enemyId, intentId)
        ?? ColossusIntent(enemyId, intentId);

    public static IReadOnlyList<CardData> EliteCards() =>
        [.. SurveyorOfferCards(), .. ScarabSealCards(), .. TreasuryCreditCards(), .. SphinxAnswerCards()];

    // ── §6.2, written once ────────────────────────────────────────────────────────────────────────────────

    // "Any elite-generated exact requirement must be checked against the deterministic current state. A
    // Weighed value greater than the player's realistically spendable Energy this turn is not offered."
    //
    // Realistically spendable is the pool itself: an exact measure is met by SPENDING, and what can be spent
    // is what is in the pool at the moment the demand is made. (A surcharge makes each card dearer, which
    // changes how many cards it takes to reach a figure, never whether the figure can be reached.)
    //
    // So this is the ceiling every elite demand passes through, and it is written here rather than in any one
    // elite because the Surveyor is only the first body to ask: the Sphinx, the Decans and the Treasury all
    // generate requirements, and a filter each of them re-derived would drift.
    public static ICombatExpression<TContext, int> Achievable<TContext>(
        ICombatExpression<TContext, int> demand) where TContext : class =>
        new MaxExpression<TContext>(
            new ConstantExpression<TContext>(1),
            new MinExpression<TContext>(demand, SpendableEnergy<TContext>()));

    public static ICombatExpression<TContext, int> Achievable<TContext>(int demand) where TContext : class =>
        Achievable<TContext>(new ConstantExpression<TContext>(demand));

    private static ICombatExpression<TContext, int> SpendableEnergy<TContext>() where TContext : class =>
        new CombatantCurrentResourceExpression<TContext>(Applicant, StandardCombatIds.EnergyResource);

    // ── counting the act's pressure ───────────────────────────────────────────────────────────────────────

    // "How many different Act-IV negative status TYPES is the player carrying?" — asked by the Sphinx and by
    // both of the last two elites, so it is written here. KINDS and not stacks: a player buried five deep in
    // one thing is answered more gently than one carrying a little of everything, which is the point.
    //
    // `min(stacks, 1)` is how "is this one present" is spelled as a number. ⚠ A measure is TAKEN at the end
    // of the turn it stands in and removes itself doing so, so an enemy acting after that turn never meets
    // one — the Weighed term is live only against a body that raises a measure on its own turn.
    public static ICombatExpression<TContext, int> NegativeKinds<TContext>() where TContext : class
    {
        ICombatExpression<TContext, int> Present(string statusId) =>
            new MinExpression<TContext>(
                new CombatantStatusStacksExpression<TContext>(Applicant, new StatusDefinitionId(statusId)),
                new ConstantExpression<TContext>(1));

        return new AddExpression<TContext>(
            Present(WeighedId), new AddExpression<TContext>(Present(BurdenedId), Present(EntombedId)));
    }

    // ── addressing an elite's own body ────────────────────────────────────────────────────────────────────

    // A turn rule that only means anything on the PLAYER's turn — an offer, a draw, a window. TurnStarted
    // fires for every body on the field, and an enemy's turn boundary is not the moment these describe.
    private static ICombatExpression<TContext, bool> PlayersTurn<TContext>() where TContext : class =>
        new TargetHasStatusExpression<TContext>(
            CombatantTargetSelectors.Source, new StatusDefinitionId(PassiveStatuses.ApplicantId));
}
