using RogueDeck.Core.Combat;
using RogueDeck.Run;
using RogueDeck.Scenario.Authoring;
using static BnbContent.Converter.Cards.CardAuthoring;

namespace BnbContent.Converter.Events;

// What Act III's events promise for AFTER the door closes.
//
// The Green Docket's doors promise fewer things about ONE card than the archives did and more about the next
// stretch of road: a demand waiting at the next ordinary fight, a shape you have to fight in if you want the
// prize, a stretch of two fights rather than one, and the road looking after you at the next hard room.
//
// Each is an authored RUN PROGRAM: the body lives once in the document (RunBlueprint.Programs) and an event
// installs it by name. The instance id IS the name, so a promise made twice is still one promise, and a
// one-shot body names itself to step down once it has been kept.
public static class ActThreeEventPrograms
{
    public const string Inscriptions = "act_three_inscriptions";
    public const string ExtraCardReward = "act_three_extra_card_reward";
    public const string RareCardReward = "act_three_rare_card_reward";
    public const string GarnishedReward = "act_three_garnished_reward";
    public const string GarnishThePurse = "act_three_garnish_the_purse";
    public const string ShortestRoadWaits = "act_three_shortest_road_waits";
    public const string ForgottenNameWaits = "act_three_forgotten_name_waits";
    public const string HedgeDemandWaits = "act_three_hedge_demand_waits";
    public const string ConceptualDemandWaits = "act_three_conceptual_demand_waits";
    public const string QuorumKept = "act_three_quorum_kept";
    public const string QuorumLapsed = "act_three_quorum_lapsed";
    public const string AntLineKept = "act_three_ant_line_kept";
    public const string AntLineLapsed = "act_three_ant_line_lapsed";
    public const string ShelterAgain = "act_three_shelter_again";
    public const string RoadStarAgain = "act_three_road_star_again";
    public const string PreparedResponse = "act_three_prepared_response";
    public const string VictoryPurse80 = "act_three_victory_purse_80";
    public const string VictoryPurse60 = "act_three_victory_purse_60";

    public static IReadOnlyDictionary<string, ITriggeredRunEffectDefinition> All(ConversionPools pools) =>
        new Dictionary<string, ITriggeredRunEffectDefinition>
        {
            [Inscriptions] = InscriptionsBody(),
            [ExtraCardReward] = ExtraCardRewardBody(pools),
            [RareCardReward] = RareCardRewardBody(pools),
            [GarnishedReward] = GarnishedRewardBody(),
            [GarnishThePurse] = GarnishThePurseBody(),
            [ShortestRoadWaits] = ShortestRoadWaitsBody(),
            [ForgottenNameWaits] = ForgottenNameWaitsBody(),
            [HedgeDemandWaits] = DemandWaitsBody(HedgeDemandWaits),
            [ConceptualDemandWaits] = DemandWaitsBody(ConceptualDemandWaits),
            [QuorumKept] = QuorumKeptBody(),
            [QuorumLapsed] = QuorumLapsedBody(),
            [AntLineKept] = AntLineKeptBody(),
            [AntLineLapsed] = AntLineLapsedBody(),
            [ShelterAgain] = ShelterAgainBody(),
            [RoadStarAgain] = RoadStarAgainBody(),
            [PreparedResponse] = PreparedResponseBody(),
            [VictoryPurse80] = VictoryPurseBody(VictoryPurse80, 80),
            [VictoryPurse60] = VictoryPurseBody(VictoryPurse60, 60),
        };

    // The five inscriptions are permanent, so their rules have to be in EVERY later fight. One program
    // rather than five: a card without the tag makes each rule a no-op, so a run that has been given one
    // inscription pays for all five in dead statuses and nothing else.
    private static ITriggeredRunEffectDefinition InscriptionsBody() =>
        Openings.EveryCombat(
            [.. ActThreeEventObjects.Inscriptions().Select(Applies)]);

    // "Victory grants one additional card reward."
    private static ITriggeredRunEffectDefinition ExtraCardRewardBody(ConversionPools pools) =>
        RunPrograms.When<CombatResolvedRunEvent>(RunEventValues.CombatWasVictory,
        [
            Literal(new OfferRewardRunEffect(
                new RewardId("event:green_docket:extra"), pools.CardRewardSource(), 1)),
            Done(ExtraCardReward),
        ]);

    private static ITriggeredRunEffectDefinition RareCardRewardBody(ConversionPools pools) =>
        RunPrograms.When<CombatResolvedRunEvent>(RunEventValues.CombatWasVictory,
        [
            Literal(new OfferRewardRunEffect(
                new RewardId("event:green_docket:rare"), pools.CardRewardSource("rare"), 1)),
            Done(RareCardReward),
        ]);

    // "Grants no Gold." The purse is the map's, not the fight's, and it is paid out AFTER the resolved
    // event — so the fight ending arms a bailiff and the bailiff takes the very next Gold that arrives.
    private static ITriggeredRunEffectDefinition GarnishedRewardBody() =>
        RunPrograms.On<CombatResolvedRunEvent>(
        [
            Literal(new InstallProgramByIdRunEffect(new RunProgramSourceId(GarnishThePurse))),
            Done(GarnishedReward),
        ]);

    private static ITriggeredRunEffectDefinition GarnishThePurseBody() =>
        RunPrograms.When<ResourceChangedRunEvent>(
            RunExpr.And(
                RunEventValues.ResourceIs(StandardRunIds.Gold),
                RunExpr.GreaterThan(RunEventValues.ResourceDelta, RunExpr.Const(0))),
            [
                RunEffectTemplates.GainResource(
                    StandardRunIds.Gold, RunExpr.Negate(RunEventValues.ResourceDelta)),
                Done(GarnishThePurse),
            ]);

    // "Ask which road is shortest": the next ORDINARY fight is smaller, pays nothing, and pays a card.
    private static ITriggeredRunEffectDefinition ShortestRoadWaitsBody() =>
        RunPrograms.When<NodeEnteredRunEvent>(NormalCombat(),
        [
            Literal(Openings.NextCombat(Diminish(20))),
            Literal(new InstallProgramByIdRunEffect(new RunProgramSourceId(GarnishedReward))),
            Literal(new InstallProgramByIdRunEffect(new RunProgramSourceId(ExtraCardReward))),
            Done(ShortestRoadWaits),
        ]);

    // "Follow the forgotten name": the same road, and a rarer card at the end of it.
    private static ITriggeredRunEffectDefinition ForgottenNameWaitsBody() =>
        RunPrograms.When<NodeEnteredRunEvent>(NormalCombat(),
        [
            Literal(Openings.NextCombat(Diminish(20))),
            Literal(new InstallProgramByIdRunEffect(new RunProgramSourceId(GarnishedReward))),
            Literal(new InstallProgramByIdRunEffect(new RunProgramSourceId(RareCardReward))),
            Done(ForgottenNameWaits),
        ]);

    // A demand of the road, waiting for an ordinary fight to be walked into.
    private static ITriggeredRunEffectDefinition DemandWaitsBody(string name) =>
        RunPrograms.When<NodeEnteredRunEvent>(NormalCombat(),
        [
            Literal(Openings.NextCombat(
                new CombatNodeModel("applyStatus", "source", CombatAmountSpec.FromConst(2),
                    StatusId: ActThreeEventObjects.EnvironmentalWergildId))),
            Done(name),
        ]);

    // "Win without violating the quorum → gain the Dissenting Spore." The fight could not stop a second or a
    // fourth card and did not try; it only wrote down that it happened.
    //
    // ★ A branch decided by the fight that just ended has to be a program CONDITION, not a conditional
    // effect, so the two outcomes are two programs over the same event, each ruling the other out and each
    // cleaning up after both.
    private static ITriggeredRunEffectDefinition QuorumKeptBody() =>
        RunPrograms.When<CombatResolvedRunEvent>(
            RunExpr.And(RunEventValues.CombatWasVictory, RunExpr.Equal(QuorumHeld, RunExpr.Const(1))),
            [Literal(new AddRelicByIdRunEffect(new RelicId("dissenting_spore"))), .. LetTheQuorumGo()]);

    private static ITriggeredRunEffectDefinition QuorumLapsedBody() =>
        RunPrograms.When<CombatResolvedRunEvent>(
            RunExpr.Or(RunEventValues.CombatWasDefeat, RunExpr.Equal(QuorumHeld, RunExpr.Const(0))),
            [.. LetTheQuorumGo()]);

    private static IReadOnlyList<IRunEffectTemplate> LetTheQuorumGo() =>
        [Done(QuorumKept), Done(QuorumLapsed)];

    private static ITriggeredRunEffectDefinition AntLineKeptBody() =>
        RunPrograms.When<CombatResolvedRunEvent>(
            RunExpr.And(RunEventValues.CombatWasVictory, RunExpr.Equal(AntLineHeld, RunExpr.Const(1))),
            [Literal(new AddRelicByIdRunEffect(new RelicId("antway_marker"))), .. LetTheLineGo()]);

    private static ITriggeredRunEffectDefinition AntLineLapsedBody() =>
        RunPrograms.When<CombatResolvedRunEvent>(
            RunExpr.Or(RunEventValues.CombatWasDefeat, RunExpr.Equal(AntLineHeld, RunExpr.Const(0))),
            [.. LetTheLineGo()]);

    private static IReadOnlyList<IRunEffectTemplate> LetTheLineGo() =>
        [Done(AntLineKept), Done(AntLineLapsed)];

    // "The next 2 normal combats draw +1 on turn 1." An opening is consumed by one fight, so the second is
    // written the moment the first is over.
    private static ITriggeredRunEffectDefinition ShelterAgainBody() =>
        RunPrograms.On<CombatResolvedRunEvent>(
        [
            Literal(Openings.NextCombat(new CombatNodeModel("drawCards", "source", CombatAmountSpec.FromConst(1)))),
            Done(ShelterAgain),
        ]);

    // …and the road star's two licences, the same way.
    private static ITriggeredRunEffectDefinition RoadStarAgainBody() =>
        RunPrograms.On<CombatResolvedRunEvent>(
        [
            Literal(Openings.NextCombat(Applies(ActThree.SafeConductId))),
            Done(RoadStarAgain),
        ]);

    // "Next Elite or Boss combat starts with +1 Safe-Conduct" — it waits through every ordinary room.
    private static ITriggeredRunEffectDefinition PreparedResponseBody() =>
        RunPrograms.When<NodeEnteredRunEvent>(
            RunExpr.And(
                new EventBoolValueExpression(RunEventFields.NodeIsCombat),
                RunExpr.Or(
                    RunEventValues.NodeHasTag(MapNodeTags.Elite),
                    RunEventValues.NodeHasTag(MapNodeTags.Boss))),
            [
                Literal(Openings.NextCombat(Applies(ActThree.SafeConductId))),
                Done(PreparedResponse),
            ]);

    // "After victory gain N Gold" — the door's own promise, paid by the fight it was made about, and paid
    // whether or not something else has garnished that fight's purse.
    private static ITriggeredRunEffectDefinition VictoryPurseBody(string name, int gold) =>
        RunPrograms.When<CombatResolvedRunEvent>(RunEventValues.CombatWasVictory,
        [
            RunEffectTemplates.Literal(
                new ChangeResourceRunEffect(StandardRunIds.Gold, gold)),
            Done(name),
        ]);

    // ── shorthands ────────────────────────────────────────────────────────────────────────────────────────

    // An ordinary fight: a combat node the map generated as Combat or MultiCombat. An Elite, a Boss and a
    // mimic are all combat nodes too, and none of them is what the design means by "normal combat".
    private static IRunExpression<bool> NormalCombat() =>
        RunExpr.And(
            new EventBoolValueExpression(RunEventFields.NodeIsCombat),
            RunExpr.Or(
                RunEventValues.NodeHasTag(MapNodeTags.Combat),
                RunEventValues.NodeHasTag(MapNodeTags.MultiCombat)));

    // "Enemies have N% less Max HP." Max health cannot be lowered from outside a fight, so the shortfall is
    // paid as unblockable damage at the opening bell, read per body off its own maximum.
    private static CombatNodeModel Diminish(int percent) =>
        CombatNodeModel.ForEach("allEnemies",
            new CombatNodeModel("dealDamage", "iterationTarget",
                CombatAmountSpec.Binary("div",
                    CombatAmountSpec.Binary("mul",
                        new CombatAmountSpec("maxHealth", SelectorKey: "iterationTarget"),
                        CombatAmountSpec.FromConst(percent)),
                    CombatAmountSpec.FromConst(100)),
                IgnoresBlock: true));

    private static CombatNodeModel Applies(string statusId) =>
        new("applyStatus", "source", CombatAmountSpec.FromConst(1), StatusId: statusId);

    private static IRunExpression<int> QuorumHeld =>
        RunEventValues.CombatCounter(ActThreeEventObjects.QuorumHeld.ToString());

    private static IRunExpression<int> AntLineHeld =>
        RunEventValues.CombatCounter(ActThreeEventObjects.AntLineHeld.ToString());

    private static IRunEffectTemplate Literal(IRunEffectRequest effect) => RunEffectTemplates.Literal(effect);

    private static IRunEffectTemplate Done(string program) =>
        Literal(new UninstallRunProgramRunEffect(new RunProgramId(program)));
}
