using System.Text.Json;
using RogueDeck.Core.Combat;
using RogueDeck.Scenario.Authoring;

namespace BnbContent.Converter;

// Act III boss 1 — **The Ombudsman of Root and Road** (342 HP).
//
// > The player does not only decide whether a Claim exists; they shape which legal GROUND the complaint
// > belongs to.
//
// A large bent figure of grey fur, roots and braided road markers, carrying ford stones, animal teeth,
// broken boundary posts and carved traveller tokens. It hears both sides. That does not mean either side
// will like the settlement.
//
// Two laws, and a Claim remembers which one made it:
//
//   Right of the Road — the fourth real card of a turn.
//   Right of the Root — ending a turn with no Energy left.
//
// In Phase I exactly one Ground is being heard and it alternates every turn, so the player knows which law
// is live. What the player controls is the ammunition: **Counter-Petition** spends a licence to move one
// Claim from Road to Root or back, which is neither a creation nor a transfer — only a change in the legal
// theory the complaint will be heard under. Road Claims cost you money; Root Claims cost you blood.
//
// Hearing one of each brings the two jurisdictions together, and from then on both laws are live at once.
public static partial class ActThree
{
    public const string OmbudsmanEnemyId = "ombudsman_of_root_and_road";
    public const string OmbudsmanId = "grounds_of_complaint";

    public const string RoadClaimId = "road_claim";
    public const string RootClaimId = "root_claim";
    public const string RoadHeardId = "road_heard";
    public const string RootHeardId = "root_heard";
    public const string CombinedJurisdictionId = "combined_jurisdiction";
    public const string CrossPetitionId = "cross_petition";
    public const string BoundaryPendingId = "the_boundary_cannot_be_divided";
    public const string CounterPetitionFreeId = "counter_petition_free";
    public const string CounterPetitionUsedId = "counter_petition_used";
    public const string OmbudsmanNotedRoadId = "road_noted";
    public const string OmbudsmanNotedRootId = "root_noted";
    public const string CounterPetitionCardId = "counter_petition";
    public const string HearingsOpenedId = "separate_hearings_opened";

    public const int RightOfTheRoadLaw = 27;
    public const int RightOfTheRootLaw = 28;

    private const int OmbudsmanTransitionHealth = 171;
    private const int OmbudsmanSignatureHealth = 86;
    private const int OmbudsmanSettlementHealth = 6;

    public static readonly TagId CounterPetitionTag = new("counter_petition");

    // Which Ground is being heard while the hearings are separate: 0 Road, 1 Root.
    public static CounterId ActiveGroundCounter => new("active_ground");

    private static ICombatantTargetSelector Ombudsman { get; } = Elite(OmbudsmanId);

    private static IEnumerable<StatusData> OmbudsmanStatuses() =>
    [
        GroundsOfComplaint(),
        GroundClaim(RoadClaimId, "Road Claim",
            "Standing the Ombudsman holds under the Right of the Road. Heard, it becomes a demand for money."),
        GroundClaim(RootClaimId, "Root Claim",
            "Standing the Ombudsman holds under the Right of the Root. Heard, it becomes a blow."),
        Marker(RoadHeardId, "Road Heard", "A complaint has been heard on the Ground of the Road."),
        Marker(RootHeardId, "Root Heard", "A complaint has been heard on the Ground of the Root."),
        Marker(CombinedJurisdictionId, "Combined Jurisdiction",
            "The boundary cannot be divided: both the Right of the Road and the Right of the Root are law."),
        Marker(HearingsOpenedId, "Hearings Opened",
            "The Ombudsman has sat once. From the next bell, the hearing moves to the other Ground."),
        Marker(CrossPetitionId, "Cross-Petition",
            "One complaint of each Ground stands paired. The Ombudsman would rather settle them together."),
        Marker(BoundaryPendingId, "Boundary to Be Heard",
            "The Ombudsman's next action is the joining of the two jurisdictions, and not a blow."),
        Marker(CounterPetitionFreeId, "Petition Heard Freely",
            "Your next Counter-Petition this turn costs no licence."),
        Marker(CounterPetitionUsedId, "Petition Made",
            "You have already re-argued a complaint this turn."),
        Marker(OmbudsmanNotedRoadId, "Road Noted",
            "The Right of the Road has already been answered this turn."),
        Marker(OmbudsmanNotedRootId, "Root Noted",
            "The Right of the Root has already been answered this turn."),
    ];

    private static StatusData GroundClaim(string id, string name, string description) => new()
    {
        Id = id,
        NameKey = name,
        DescriptionKey = description,
        Polarity = StatusPolarity.Buff,
        StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
        UsesStacks = true,
        Tags = [],
        PassiveModifiers = [],
        Triggers = [],
    };

    private static ICombatExpression<TContext, bool> Wears<TContext>(
        ICombatantTargetSelector who, string statusId)
        where TContext : class =>
        new ComparisonExpression<TContext>(
            new CombatantStatusStacksExpression<TContext>(who, new StatusDefinitionId(statusId)),
            ComparisonOperator.Greater, new ConstantExpression<TContext>(0));

    // A law is live while its Ground is the one being heard — or, once the boundary has been joined,
    // whatever the other one is doing.
    private static ICombatExpression<TContext, bool> GroundLive<TContext>(int ground)
        where TContext : class =>
        new OrExpression<TContext>(
            Wears<TContext>(Ombudsman, CombinedJurisdictionId),
            new ComparisonExpression<TContext>(
                new CombatantCounterExpression<TContext>(Ombudsman, ActiveGroundCounter),
                ComparisonOperator.Equal, new ConstantExpression<TContext>(ground)));

    // ── the two laws, and everything the Ombudsman remembers ──────────────────────────────────────────────
    private static StatusData GroundsOfComplaint()
    {
        var player = CombatantTargetSelectors.Source;

        // Right of the Road — the fourth real card of a turn.
        var road = new EffectProgram<CardPlayedTriggeredEffectContext>(
            new ConditionalEffectNode<CardPlayedTriggeredEffectContext>(
                new AndExpression<CardPlayedTriggeredEffectContext>(
                    GroundLive<CardPlayedTriggeredEffectContext>(0),
                    new ComparisonExpression<CardPlayedTriggeredEffectContext>(
                        NonJunkPlayedThisTurn(player), ComparisonOperator.Equal,
                        new ConstantExpression<CardPlayedTriggeredEffectContext>(4))),
                Violate<CardPlayedTriggeredEffectContext>(
                    Ombudsman, RightOfTheRoadLaw, OmbudsmanNotedRoadId)));

        // Right of the Root — ending a turn with nothing left to spend.
        var root = new EffectProgram<TurnEndedTriggeredEffectContext>(
            new ConditionalEffectNode<TurnEndedTriggeredEffectContext>(
                new AndExpression<TurnEndedTriggeredEffectContext>(
                    PlayersTurn<TurnEndedTriggeredEffectContext>(),
                    new AndExpression<TurnEndedTriggeredEffectContext>(
                        GroundLive<TurnEndedTriggeredEffectContext>(1),
                        new ComparisonExpression<TurnEndedTriggeredEffectContext>(
                            new CombatantCurrentResourceExpression<TurnEndedTriggeredEffectContext>(
                                Applicant, StandardCombatIds.EnergyResource),
                            ComparisonOperator.Equal,
                            new ConstantExpression<TurnEndedTriggeredEffectContext>(0)))),
                Violate<TurnEndedTriggeredEffectContext>(
                    Ombudsman, RightOfTheRootLaw, OmbudsmanNotedRootId)));

        // The player's bell: the latches clear, the hearing moves to the other Ground while the hearings are
        // still separate, and the means to re-argue a complaint is offered while there is one to re-argue.
        var bell = new EffectProgram<TurnStartedTriggeredEffectContext>(
            new ConditionalEffectNode<TurnStartedTriggeredEffectContext>(
                PlayersTurn<TurnStartedTriggeredEffectContext>(),
                new CausalSequenceEffectNode<TurnStartedTriggeredEffectContext>(
                [
                    new RemoveStatusNode<TurnStartedTriggeredEffectContext>(
                        Ombudsman, new StatusDefinitionId(OmbudsmanNotedRoadId)),
                    new RemoveStatusNode<TurnStartedTriggeredEffectContext>(
                        Ombudsman, new StatusDefinitionId(OmbudsmanNotedRootId)),
                    new RemoveStatusNode<TurnStartedTriggeredEffectContext>(
                        Applicant, new StatusDefinitionId(CounterPetitionUsedId)),
                    // The first bell opens the hearings on the Road rather than moving them: the complaint
                    // has to be heard on some Ground before it can be heard on the other.
                    new ConditionalEffectNode<TurnStartedTriggeredEffectContext>(
                        new NotExpression<TurnStartedTriggeredEffectContext>(
                            Wears<TurnStartedTriggeredEffectContext>(Ombudsman, HearingsOpenedId)),
                        new ApplyStatusNode<TurnStartedTriggeredEffectContext>(
                            Ombudsman, new StatusDefinitionId(HearingsOpenedId),
                            new ConstantExpression<TurnStartedTriggeredEffectContext>(1)),
                        new ConditionalEffectNode<TurnStartedTriggeredEffectContext>(
                            new NotExpression<TurnStartedTriggeredEffectContext>(
                                Wears<TurnStartedTriggeredEffectContext>(
                                    Ombudsman, CombinedJurisdictionId)),
                            new SetCombatantCounterNode<TurnStartedTriggeredEffectContext>(
                                Ombudsman, ActiveGroundCounter,
                                new SubtractExpression<TurnStartedTriggeredEffectContext>(
                                    new ConstantExpression<TurnStartedTriggeredEffectContext>(1),
                                    new CombatantCounterExpression<TurnStartedTriggeredEffectContext>(
                                        Ombudsman, ActiveGroundCounter)),
                                relative: false))),
                    OfferACounterPetition<TurnStartedTriggeredEffectContext>(),
                    QueueTheBoundary<TurnStartedTriggeredEffectContext>(),
                ])));

        // A Claim remembers which law made it. The act writes the law down as the violation goes past, and
        // the standing is made inside that same filing — so the Ground is simply read off the record.
        EffectProgram<TContext> ground<TContext>() where TContext : class =>
            new(new ConditionalEffectNode<TContext>(
                new TriggerEventStatusIsExpression<TContext>(new StatusDefinitionId(ClaimCreatedId)),
                new CausalSequenceEffectNode<TContext>(
                [
                    new ConditionalEffectNode<TContext>(
                        new ComparisonExpression<TContext>(
                            new CombatantCounterExpression<TContext>(Applicant, LawBeingFiledCounter),
                            ComparisonOperator.Equal, new ConstantExpression<TContext>(RightOfTheRootLaw)),
                        new ApplyStatusNode<TContext>(
                            Ombudsman, new StatusDefinitionId(RootClaimId),
                            new ConstantExpression<TContext>(1)),
                        // Everything else — the Road's own law, and a demand left owing — is heard as a
                        // matter of the road, which is where a traveller's complaints begin.
                        new ApplyStatusNode<TContext>(
                            Ombudsman, new StatusDefinitionId(RoadClaimId),
                            new ConstantExpression<TContext>(1))),
                    MarkACrossPetition<TContext>(),
                ])));

        return Rule(OmbudsmanId, "Grounds of Complaint",
            "Two rights, and only one of them is being heard at a time until the boundary is joined: the "
            + "Right of the Road (a fourth real card in a turn) and the Right of the Root (ending a turn "
            + "with no Energy). Standing remembers which right made it — a Road Claim becomes a demand for "
            + "money, a Root Claim becomes a blow — and a licence spent on a COUNTER-PETITION moves one "
            + "complaint from the one Ground to the other. Hear one of each and the two jurisdictions join.",
            [
                new StatusTriggerData("CardPlayed", JsonSerializer.SerializeToElement(
                    road, CombatJson.CreateOptions<CardPlayedTriggeredEffectContext>()),
                    StatusTriggerScope.Anywhere),
                new StatusTriggerData("TurnEnded", JsonSerializer.SerializeToElement(
                    root, CombatJson.CreateOptions<TurnEndedTriggeredEffectContext>()),
                    StatusTriggerScope.Anywhere),
                new StatusTriggerData("TurnStarted", JsonSerializer.SerializeToElement(
                    bell, CombatJson.CreateOptions<TurnStartedTriggeredEffectContext>()),
                    StatusTriggerScope.Anywhere),
                new StatusTriggerData("StatusApplied", JsonSerializer.SerializeToElement(
                    ground<StatusAppliedTriggeredEffectContext>(),
                    CombatJson.CreateOptions<StatusAppliedTriggeredEffectContext>())),
                new StatusTriggerData("StatusMerged", JsonSerializer.SerializeToElement(
                    ground<StatusMergedTriggeredEffectContext>(),
                    CombatJson.CreateOptions<StatusMergedTriggeredEffectContext>())),
            ]);
    }

    // "If a newly created Claim of one Ground appears while at least one Claim of the other exists, mark one
    // pair as a Cross-Petition. Only one pair at a time." Combined jurisdiction only: while the hearings are
    // separate there is nothing to cross.
    private static IEffectNode<TContext> MarkACrossPetition<TContext>()
        where TContext : class =>
        new ConditionalEffectNode<TContext>(
            new AndExpression<TContext>(
                Wears<TContext>(Ombudsman, CombinedJurisdictionId),
                new AndExpression<TContext>(
                    new NotExpression<TContext>(Wears<TContext>(Ombudsman, CrossPetitionId)),
                    new AndExpression<TContext>(
                        Wears<TContext>(Ombudsman, RoadClaimId),
                        Wears<TContext>(Ombudsman, RootClaimId)))),
            new ApplyStatusNode<TContext>(
                Ombudsman, new StatusDefinitionId(CrossPetitionId), new ConstantExpression<TContext>(1)));

    // "Once both Grounds have been Heard, queue the transition — or at 171 HP if they have not." Queued, not
    // taken: what it replaces is the Ombudsman's next action, so the joining costs the player nothing.
    private static IEffectNode<TContext> QueueTheBoundary<TContext>()
        where TContext : class =>
        new ConditionalEffectNode<TContext>(
            new AndExpression<TContext>(
                new NotExpression<TContext>(Wears<TContext>(Ombudsman, CombinedJurisdictionId)),
                new AndExpression<TContext>(
                    new NotExpression<TContext>(Wears<TContext>(Ombudsman, BoundaryPendingId)),
                    new OrExpression<TContext>(
                        new AndExpression<TContext>(
                            Wears<TContext>(Ombudsman, RoadHeardId),
                            Wears<TContext>(Ombudsman, RootHeardId)),
                        new ComparisonExpression<TContext>(
                            new CombatantCurrentHealthExpression<TContext>(Ombudsman),
                            ComparisonOperator.LessOrEqual,
                            new ConstantExpression<TContext>(OmbudsmanTransitionHealth))))),
            new ApplyStatusNode<TContext>(
                Ombudsman, new StatusDefinitionId(BoundaryPendingId), new ConstantExpression<TContext>(1)));

    // ── Counter-Petition ──────────────────────────────────────────────────────────────────────────────────
    //
    // A combat here has no free actions, only cards, so re-arguing a complaint is a card the fight hands
    // over while there is a complaint to re-argue.
    public static CardData CounterPetition() => new()
    {
        Id = CounterPetitionCardId,
        NameKey = "Counter-Petition",
        DescriptionKey =
            "Once a turn, spend 1 Safe-Conduct to argue one of the Ombudsman's complaints under the other "
            + "Ground — Road becomes Root, or Root becomes Road. It creates nothing, moves nothing, and "
            + "nobody's standing changes hands.",
        Costs = [],
        Tags = [CounterPetitionTag, new TagId("form")],
        Program = new EffectProgram<CardPlayContext>(
            new CausalSequenceEffectNode<CardPlayContext>(
            [
                new ConditionalEffectNode<CardPlayContext>(
                    new AndExpression<CardPlayContext>(
                        new NotExpression<CardPlayContext>(
                            Wears<CardPlayContext>(Applicant, CounterPetitionUsedId)),
                        new OrExpression<CardPlayContext>(
                            Wears<CardPlayContext>(Applicant, CounterPetitionFreeId),
                            new ComparisonExpression<CardPlayContext>(
                                new CombatantStatusStacksExpression<CardPlayContext>(
                                    Applicant, new StatusDefinitionId(SafeConductId)),
                                ComparisonOperator.GreaterOrEqual,
                                new ConstantExpression<CardPlayContext>(1)))),
                    new CausalSequenceEffectNode<CardPlayContext>(
                    [
                        // The free hearing is spent before the licence is.
                        new ConditionalEffectNode<CardPlayContext>(
                            Wears<CardPlayContext>(Applicant, CounterPetitionFreeId),
                            new RemoveStatusNode<CardPlayContext>(
                                Applicant, new StatusDefinitionId(CounterPetitionFreeId)),
                            new ModifySelectedStatusStacksNode<CardPlayContext>(
                                Applicant,
                                new StatusSelectionSpec(StatusPolarityFilter.Any)
                                {
                                    Definition = new StatusDefinitionId(SafeConductId),
                                },
                                new ConstantExpression<CardPlayContext>(-1))),
                        new ApplyStatusNode<CardPlayContext>(
                            Applicant, new StatusDefinitionId(CounterPetitionUsedId),
                            new ConstantExpression<CardPlayContext>(1)),
                        // Neither a creation nor a transfer: only which theory the complaint is heard under.
                        new ChooseOptionsNode<CardPlayContext>(
                        [
                            Reargue(RoadClaimId, RootClaimId),
                            Reargue(RootClaimId, RoadClaimId),
                        ],
                        ["argue it as a matter of the root", "argue it as a matter of the road"],
                        count: 1, purpose: "under which ground"),
                    ])),
                AnotherCounterPetition(),
            ])),
        PlayedCardDestinationZone = CardZone.ExhaustPile,
        TurnEndHandDestinationZone = CardZone.Hand,
    };

    private static IEffectNode<CardPlayContext> Reargue(string from, string to) =>
        new ConditionalEffectNode<CardPlayContext>(
            Wears<CardPlayContext>(Ombudsman, from),
            new CausalSequenceEffectNode<CardPlayContext>(
            [
                new ModifyStatusStacksNode<CardPlayContext>(
                    Ombudsman, new StatusDefinitionId(from), new ConstantExpression<CardPlayContext>(-1)),
                new ApplyStatusNode<CardPlayContext>(
                    Ombudsman, new StatusDefinitionId(to), new ConstantExpression<CardPlayContext>(1)),
            ]));

    private static IEffectNode<TContext> OfferACounterPetition<TContext>()
        where TContext : class =>
        new ConditionalEffectNode<TContext>(
            new AndExpression<TContext>(
                new ComparisonExpression<TContext>(
                    new CombatantStatusStacksExpression<TContext>(Ombudsman, new StatusDefinitionId(ClaimId)),
                    ComparisonOperator.Greater, new ConstantExpression<TContext>(0)),
                new ComparisonExpression<TContext>(
                    new CombatantZoneCardCountExpression<TContext>(
                        Applicant, CardZone.Hand, CounterPetitionTag),
                    ComparisonOperator.Equal, new ConstantExpression<TContext>(0))),
            new CreateCardInstanceNode<TContext>(
                Applicant, new CardDefinitionId(CounterPetitionCardId), CardZone.Hand,
                new ConstantExpression<TContext>(1)));

    // The copy being played is still counted in hand, so the threshold is one rather than none.
    private static IEffectNode<CardPlayContext> AnotherCounterPetition() =>
        new ConditionalEffectNode<CardPlayContext>(
            new AndExpression<CardPlayContext>(
                new ComparisonExpression<CardPlayContext>(
                    new CombatantStatusStacksExpression<CardPlayContext>(
                        Ombudsman, new StatusDefinitionId(ClaimId)),
                    ComparisonOperator.Greater, new ConstantExpression<CardPlayContext>(0)),
                new ComparisonExpression<CardPlayContext>(
                    new CombatantZoneCardCountExpression<CardPlayContext>(
                        Applicant, CardZone.Hand, CounterPetitionTag),
                    ComparisonOperator.LessOrEqual, new ConstantExpression<CardPlayContext>(1))),
            new CreateCardInstanceNode<CardPlayContext>(
                Applicant, new CardDefinitionId(CounterPetitionCardId), CardZone.Hand,
                new ConstantExpression<CardPlayContext>(1)));

    // ── Settlement Has Weight ─────────────────────────────────────────────────────────────────────────────
    private static IEffectNode<TurnEndedTriggeredEffectContext> SettlementHasWeight()
    {
        var creditor = CombatantTargetSelectors.IterationTarget;

        var owed = new CombatantStatusStacksFromSourceExpression<TurnEndedTriggeredEffectContext>(
            Applicant, new StatusDefinitionId(TrespassId), creditor);

        return new ConditionalEffectNode<TurnEndedTriggeredEffectContext>(
            new ComparisonExpression<TurnEndedTriggeredEffectContext>(
                new CombatantStatusStacksExpression<TurnEndedTriggeredEffectContext>(
                    creditor, new StatusDefinitionId(OmbudsmanId)),
                ComparisonOperator.Greater,
                new ConstantExpression<TurnEndedTriggeredEffectContext>(0)),
            new ConditionalEffectNode<TurnEndedTriggeredEffectContext>(
                new ComparisonExpression<TurnEndedTriggeredEffectContext>(
                    owed, ComparisonOperator.Greater,
                    new ConstantExpression<TurnEndedTriggeredEffectContext>(0)),
                // A record cleared is worth more to the traveller than a wound is to the Ombudsman.
                new ModifySelectedStatusStacksNode<TurnEndedTriggeredEffectContext>(
                    Applicant,
                    new StatusSelectionSpec(StatusPolarityFilter.Debuff)
                    {
                        Definition = new StatusDefinitionId(TrespassId),
                        FromActingSource = true,
                    },
                    new ConstantExpression<TurnEndedTriggeredEffectContext>(-1),
                    sourceSelector: creditor),
                new SetHealthNode<TurnEndedTriggeredEffectContext>(
                    creditor,
                    new SubtractExpression<TurnEndedTriggeredEffectContext>(
                        new CombatantCurrentHealthExpression<TurnEndedTriggeredEffectContext>(creditor),
                        new ConstantExpression<TurnEndedTriggeredEffectContext>(
                            OmbudsmanSettlementHealth)))));
    }

    // ── Intents ───────────────────────────────────────────────────────────────────────────────────────────
    private static EffectProgram<EnemyActionContext>? OmbudsmanIntent(string enemyId, string intentId)
    {
        if (enemyId != OmbudsmanEnemyId)
            return null;

        var self = CombatantTargetSelectors.Source;

        IEffectNode<EnemyActionContext> ordinary = intentId switch
        {
            "hear_the_road" => HearTheGround(RoadClaimId, RoadHeardId,
                new CausalSequenceEffectNode<EnemyActionContext>(
                [
                    DemandWergild<EnemyActionContext>(self, 2),
                    Blow(10),
                ])),
            "hear_the_root" => HearTheGround(RootClaimId, RootHeardId,
                new CausalSequenceEffectNode<EnemyActionContext>(
                [
                    Blow(20),
                    new GainBlockNode<EnemyActionContext>(self, new ConstantExpression<EnemyActionContext>(10)),
                ])),
            // Walk the Boundary → Merge the Findings.
            "walk_the_boundary" => Jurisdictions(
                new CausalSequenceEffectNode<EnemyActionContext>(
                [
                    Blow(14),
                    new SetCombatantCounterNode<EnemyActionContext>(
                        self, ActiveGroundCounter,
                        new SubtractExpression<EnemyActionContext>(
                            new ConstantExpression<EnemyActionContext>(1),
                            new CombatantCounterExpression<EnemyActionContext>(self, ActiveGroundCounter)),
                        relative: false),
                ]),
                new DealDamageNode<EnemyActionContext>(
                    Applicant,
                    new AddExpression<EnemyActionContext>(
                        new ConstantExpression<EnemyActionContext>(18),
                        new MultiplyExpression<EnemyActionContext>(
                            new ConstantExpression<EnemyActionContext>(6),
                            new MinExpression<EnemyActionContext>(
                                new MinExpression<EnemyActionContext>(
                                    new ConstantExpression<EnemyActionContext>(1),
                                    new CombatantStatusStacksExpression<EnemyActionContext>(
                                        self, new StatusDefinitionId(RoadClaimId))),
                                new MinExpression<EnemyActionContext>(
                                    new ConstantExpression<EnemyActionContext>(1),
                                    new CombatantStatusStacksExpression<EnemyActionContext>(
                                        self, new StatusDefinitionId(RootClaimId)))))))),
            // Recommend Amends → Settle the Cross-Petition where a pair stands.
            "recommend_amends" => Jurisdictions(RecommendAmends(), SettleTheCrossPetition()),
            // Hear Both Parties → Provisional Settlement. Both make the next petition cheaper.
            "hear_both_parties" => Jurisdictions(
                new CausalSequenceEffectNode<EnemyActionContext>(
                [
                    new GainBlockNode<EnemyActionContext>(self, new ConstantExpression<EnemyActionContext>(18)),
                    new ApplyStatusNode<EnemyActionContext>(
                        Applicant, new StatusDefinitionId(CounterPetitionFreeId),
                        new ConstantExpression<EnemyActionContext>(1)),
                ]),
                new CausalSequenceEffectNode<EnemyActionContext>(
                [
                    new GainBlockNode<EnemyActionContext>(self, new ConstantExpression<EnemyActionContext>(20)),
                    new ApplyStatusNode<EnemyActionContext>(
                        Applicant, new StatusDefinitionId(CounterPetitionFreeId),
                        new ConstantExpression<EnemyActionContext>(1)),
                ])),
            "hear_every_complaint" => new ConditionalEffectNode<EnemyActionContext>(
                new OrExpression<EnemyActionContext>(
                    new ComparisonExpression<EnemyActionContext>(
                        new CombatantCurrentHealthExpression<EnemyActionContext>(self),
                        ComparisonOperator.LessOrEqual,
                        new ConstantExpression<EnemyActionContext>(OmbudsmanSignatureHealth)),
                    new ComparisonExpression<EnemyActionContext>(
                        new CombatantStatusStacksExpression<EnemyActionContext>(self, new StatusDefinitionId(ClaimId)),
                        ComparisonOperator.GreaterOrEqual,
                        new ConstantExpression<EnemyActionContext>(ClaimCeiling))),
                HearEveryComplaint(),
                Blow(16)),
            _ => new NoOpEffectNode<EnemyActionContext>(),
        };

        if (intentId is not ("hear_the_road" or "hear_the_root" or "walk_the_boundary"
            or "recommend_amends" or "hear_both_parties" or "hear_every_complaint"))
            return null;

        // "Queue the transition as the next legal boss action. No direct attack occurs." Whatever was coming
        // is replaced by the joining of the two jurisdictions.
        return new EffectProgram<EnemyActionContext>(
            new ConditionalEffectNode<EnemyActionContext>(
                Wears<EnemyActionContext>(self, BoundaryPendingId),
                TheBoundaryCannotBeDivided(), ordinary));
    }

    private static IEffectNode<EnemyActionContext> Jurisdictions(
        IEffectNode<EnemyActionContext> separate, IEffectNode<EnemyActionContext> combined) =>
        new ConditionalEffectNode<EnemyActionContext>(
            Wears<EnemyActionContext>(CombatantTargetSelectors.Source, CombinedJurisdictionId),
            combined, separate);

    // A hearing on one Ground: with a complaint of that Ground it is heard, and without one the Ombudsman
    // simply hits you.
    private static IEffectNode<EnemyActionContext> HearTheGround(
        string groundId, string heardId, IEffectNode<EnemyActionContext> heard)
    {
        var self = CombatantTargetSelectors.Source;

        return new ConditionalEffectNode<EnemyActionContext>(
            Wears<EnemyActionContext>(self, groundId),
            new CausalSequenceEffectNode<EnemyActionContext>(
            [
                ConsumeClaim<EnemyActionContext>(self),
                new ModifyStatusStacksNode<EnemyActionContext>(
                    self, new StatusDefinitionId(groundId), new ConstantExpression<EnemyActionContext>(-1)),
                heard,
                new ApplyStatusNode<EnemyActionContext>(
                    self, new StatusDefinitionId(heardId), new ConstantExpression<EnemyActionContext>(1)),
            ]),
            Blow(16));
    }

    private static IEffectNode<EnemyActionContext> RecommendAmends()
    {
        var self = CombatantTargetSelectors.Source;

        return new CausalSequenceEffectNode<EnemyActionContext>(
        [
            DemandWergild<EnemyActionContext>(self, 1),
            new ApplyStatusNode<EnemyActionContext>(
                Applicant, new StatusDefinitionId(SafeConductId),
                new ConstantExpression<EnemyActionContext>(1), sourceSelector: self),
        ]);
    }

    private static IEffectNode<EnemyActionContext> SettleTheCrossPetition()
    {
        var self = CombatantTargetSelectors.Source;

        return new ConditionalEffectNode<EnemyActionContext>(
            Wears<EnemyActionContext>(self, CrossPetitionId),
            new CausalSequenceEffectNode<EnemyActionContext>(
            [
                ConsumeClaim<EnemyActionContext>(self),
                ConsumeClaim<EnemyActionContext>(self),
                new ModifyStatusStacksNode<EnemyActionContext>(
                    self, new StatusDefinitionId(RoadClaimId), new ConstantExpression<EnemyActionContext>(-1)),
                new ModifyStatusStacksNode<EnemyActionContext>(
                    self, new StatusDefinitionId(RootClaimId), new ConstantExpression<EnemyActionContext>(-1)),
                Blow(16),
                DemandWergild<EnemyActionContext>(self, 2),
                new ApplyStatusNode<EnemyActionContext>(
                    Applicant, new StatusDefinitionId(SafeConductId),
                    new ConstantExpression<EnemyActionContext>(1), sourceSelector: self),
                new RemoveStatusNode<EnemyActionContext>(self, new StatusDefinitionId(CrossPetitionId)),
            ]),
            RecommendAmends());
    }

    // "Preserve Claims and their Grounds; remove current Block; the player gains 1 Safe-Conduct; both
    // Grounds become active; no direct attack occurs."
    private static IEffectNode<EnemyActionContext> TheBoundaryCannotBeDivided()
    {
        var self = CombatantTargetSelectors.Source;

        return new CausalSequenceEffectNode<EnemyActionContext>(
        [
            new ModifyDefensivePoolNode<EnemyActionContext>(
                self, StandardCombatIds.BlockDefensivePool,
                new NegateExpression<EnemyActionContext>(
                    new CombatantDefensivePoolExpression<EnemyActionContext>(
                        self, StandardCombatIds.BlockDefensivePool))),
            new ApplyStatusNode<EnemyActionContext>(
                Applicant, new StatusDefinitionId(SafeConductId),
                new ConstantExpression<EnemyActionContext>(1), sourceSelector: self),
            new ApplyStatusNode<EnemyActionContext>(
                self, new StatusDefinitionId(CombinedJurisdictionId),
                new ConstantExpression<EnemyActionContext>(1)),
            new RemoveStatusNode<EnemyActionContext>(self, new StatusDefinitionId(BoundaryPendingId)),
        ]);
    }

    // "18 damage, +5 per Root Claim; a demand for 1 per Road Claim; then every complaint is struck out."
    private static IEffectNode<EnemyActionContext> HearEveryComplaint()
    {
        var self = CombatantTargetSelectors.Source;

        var roots = new MinExpression<EnemyActionContext>(
            new ConstantExpression<EnemyActionContext>(ClaimCeiling),
            new CombatantStatusStacksExpression<EnemyActionContext>(self, new StatusDefinitionId(RootClaimId)));
        var roads = new MinExpression<EnemyActionContext>(
            new ConstantExpression<EnemyActionContext>(ClaimCeiling),
            new CombatantStatusStacksExpression<EnemyActionContext>(self, new StatusDefinitionId(RoadClaimId)));

        return new CausalSequenceEffectNode<EnemyActionContext>(
        [
            new DealDamageNode<EnemyActionContext>(
                Applicant,
                new AddExpression<EnemyActionContext>(
                    new ConstantExpression<EnemyActionContext>(18),
                    new MultiplyExpression<EnemyActionContext>(
                        new ConstantExpression<EnemyActionContext>(5), roots))),
            new RepeatEffectNode<EnemyActionContext>(roads, DemandWergild<EnemyActionContext>(self, 1)),
            new RemoveStatusNode<EnemyActionContext>(self, new StatusDefinitionId(ClaimId)),
            new RemoveStatusNode<EnemyActionContext>(self, new StatusDefinitionId(RoadClaimId)),
            new RemoveStatusNode<EnemyActionContext>(self, new StatusDefinitionId(RootClaimId)),
            new RemoveStatusNode<EnemyActionContext>(self, new StatusDefinitionId(CrossPetitionId)),
        ]);
    }
}
