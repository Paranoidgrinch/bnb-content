using RogueDeck.Core.Combat;
using RogueDeck.Run;
using RogueDeck.Scenario.Authoring;
using static BnbContent.Converter.Cards.CardAuthoring;

namespace BnbContent.Converter.Events;

// What Act II's events promise for AFTER the door closes.
//
// The archives keep more of their promises late than the city did: a marking that has to be cleared once the
// fight that honoured it is over, an inscription's rule that has to be in every fight from now on, a lent
// volume that comes back improved, a vow that is only worth something if the fight is won without breaking it,
// and two promises that WAIT — through the shop, the rest, the elite — until an ordinary fight is walked into.
//
// Each is an authored RUN PROGRAM: the body lives once in the document (RunBlueprint.Programs) and an event
// installs it by name (fx.installProgramById). The instance id IS the name, so a promise made twice is still
// one promise, and a one-shot body names itself to step down once it has been kept.
public static class ActTwoEventPrograms
{
    public const string MarkingsExpire = "act_two_markings_expire";
    public const string Inscriptions = "act_two_inscriptions";
    public const string LentVolumeReturns = "act_two_lent_volume_returns";
    public const string AmendmentUpgrade = "act_two_amendment_upgrade";
    public const string AmendmentLapsed = "act_two_amendment_lapsed";
    public const string VowKept = "act_two_vow_kept";
    public const string VowLapsed = "act_two_vow_lapsed";
    public const string ShelfLabelAgain = "act_two_shelf_label_again";
    public const string GarnishedReward = "act_two_garnished_reward";
    public const string GarnishThePurse = "act_two_garnish_the_purse";
    public const string ExtraCardReward = "act_two_extra_card_reward";
    public const string ShortestPathWaits = "act_two_shortest_path_waits";
    public const string UnfinishedLifeWaits = "act_two_unfinished_life_waits";
    public const string NecrologyBounty = "act_two_necrology_bounty";

    // The run tag a lent volume wears while it is away — the same tag the fight reads as "Borrower's Keeping",
    // which is why the program that upgrades it afterwards can find it by name.
    public static IReadOnlyDictionary<string, ITriggeredRunEffectDefinition> All(ConversionPools pools) =>
        new Dictionary<string, ITriggeredRunEffectDefinition>
        {
            [MarkingsExpire] = MarkingsExpireBody(),
            [Inscriptions] = InscriptionsBody(),
            [LentVolumeReturns] = LentVolumeReturnsBody(),
            [AmendmentUpgrade] = AmendmentUpgradeBody(),
            [AmendmentLapsed] = AmendmentLapsedBody(),
            [VowKept] = VowKeptBody(),
            [VowLapsed] = VowLapsedBody(),
            [ShelfLabelAgain] = ShelfLabelAgainBody(),
            [GarnishedReward] = GarnishedRewardBody(),
            [GarnishThePurse] = GarnishThePurseBody(),
            [ExtraCardReward] = ExtraCardRewardBody(pools),
            [ShortestPathWaits] = ShortestPathWaitsBody(pools),
            [UnfinishedLifeWaits] = UnfinishedLifeWaitsBody(),
            [NecrologyBounty] = NecrologyBountyBody(),
        };

    // "Next combat" means exactly one combat: what the archives wrote between fights stops being true in the
    // fight that read it.
    private static ITriggeredRunEffectDefinition MarkingsExpireBody() =>
        RunPrograms.On<CombatResolvedRunEvent>(
        [
            .. ActTwoEventObjects.SpentAfterOneFight().Select(Untag),
            Done(MarkingsExpire),
        ]);

    // The five inscriptions are permanent, so their rules have to be in EVERY later fight — this is the one
    // program here that never steps down. One program rather than five: a card without the tag makes each rule
    // a no-op, so a run that has been given one inscription pays for all five in dead statuses and nothing else.
    private static ITriggeredRunEffectDefinition InscriptionsBody() =>
        Openings.EveryCombat(
        [
            .. ActTwoEventObjects.Inscriptions().Select(inscription =>
                new CombatNodeModel("applyStatus", "source",
                    CombatAmountSpec.FromConst(1), StatusId: inscription)),
        ]);

    // The Perpetual Borrower's own volume: away for the fight, and if the fight is won, the borrower returns it
    // improved. Upgrade first, then let the marking go — in that order, or the upgrade finds nothing to look for.
    private static ITriggeredRunEffectDefinition LentVolumeReturnsBody() =>
        RunPrograms.When<CombatResolvedRunEvent>(RunEventValues.CombatWasVictory,
        [
            Literal(new UpgradeCardsRunEffect(Tagged(ActTwoEventObjects.BorrowersKeeping))),
            Untag(ActTwoEventObjects.BorrowersKeeping),
            Done(LentVolumeReturns),
        ]);

    // "If successfully played while still Redacted, permanently upgrade it after victory." The fight wrote down
    // whether that happened; this reads it off the result. Either way the amendment stops being an amendment —
    // the redaction was one fight's, and the card is nobody's business afterwards.
    // ★ A branch decided by the fight that just ended has to be a program CONDITION, not a conditional effect:
    // a `ConditionalRunEffect` is enqueued and evaluated LATER, when the resolved combat is no longer the event
    // in context — and a run program is DATA, so there is no escape to decide it in code either. So the two
    // outcomes are two programs over the same event, each ruling the other out, and each cleaning up after
    // both. Exactly one of them fires after the fight the amendment was written for; a fight it was never in
    // reads the counter as zero, which is the lapse.
    private static ITriggeredRunEffectDefinition AmendmentUpgradeBody() =>
        RunPrograms.When<CombatResolvedRunEvent>(
            RunExpr.GreaterThan(AmendmentPlayed, RunExpr.Const(0)),
            [
                Literal(new UpgradeCardsRunEffect(Tagged(ActTwoEventObjects.WhisperedAmendment))),
                .. ForgetTheAmendment(),
            ]);

    private static ITriggeredRunEffectDefinition AmendmentLapsedBody() =>
        RunPrograms.When<CombatResolvedRunEvent>(
            RunExpr.Equal(AmendmentPlayed, RunExpr.Const(0)), [.. ForgetTheAmendment()]);

    // Either way the amendment stops being one: the redaction was that fight's business, and the card is
    // nobody's afterwards.
    private static IReadOnlyList<IRunEffectTemplate> ForgetTheAmendment() =>
    [
        Untag(ActTwoEventObjects.WhisperedAmendment),
        Untag(ActTwo.RedactedMark),
        Done(AmendmentUpgrade),
        Done(AmendmentLapsed),
    ];

    // "Win a combat without ever playing more than 3 non-Junk cards in a turn → gain the Vow Bead." The fight
    // could not stop the fourth card and did not try; it only wrote down that it happened.
    private static ITriggeredRunEffectDefinition VowKeptBody() =>
        RunPrograms.When<CombatResolvedRunEvent>(
            RunExpr.And(RunEventValues.CombatWasVictory, RunExpr.Equal(VowHeld, RunExpr.Const(1))),
            [Literal(new AddRelicByIdRunEffect(new RelicId("vow_bead"))), .. LetTheVowGo()]);

    // The fourth card, or a fight lost: the vow is over either way, and so are both halves of the promise.
    private static ITriggeredRunEffectDefinition VowLapsedBody() =>
        RunPrograms.When<CombatResolvedRunEvent>(
            RunExpr.Or(
                RunEventValues.CombatWasDefeat, RunExpr.Equal(VowHeld, RunExpr.Const(0))),
            [.. LetTheVowGo()]);

    private static IReadOnlyList<IRunEffectTemplate> LetTheVowGo() => [Done(VowKept), Done(VowLapsed)];

    // "One random card begins EACH of the next 2 combats Misfiled." A combat opening is consumed by one fight,
    // so the second misfiling is written the moment the first fight is over — and this program, not the shared
    // expiry, is what clears the first one, because the two would otherwise race on the same event.
    private static ITriggeredRunEffectDefinition ShelfLabelAgainBody() =>
        RunPrograms.On<CombatResolvedRunEvent>(
        [
            Untag(ActTwo.MisfiledMark),
            Literal(new TagCardsRunEffect(
                RunSelectors.DeckCards.Random(1), new RunCardTagId(ActTwo.MisfiledMark), true)),
            Literal(Openings.NextCombat(
                Applies(ActTwoEventObjects.ArchiveMarkings), Applies(ActTwo.ArchiveRegulationsId))),
            Literal(new InstallProgramByIdRunEffect(new RunProgramSourceId(MarkingsExpire))),
            Done(ShelfLabelAgain),
        ]);

    // "Grants no Gold." The purse is the map's, not the fight's, and it is paid out AFTER the resolved event —
    // so the fight ending arms a bailiff and the bailiff takes the very next Gold that arrives.
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

    // "Victory gives one additional normal card reward" — the same three-card offer the fight itself pays, a
    // second time, out of the ARCHIVES' pool rather than the city's.
    private static ITriggeredRunEffectDefinition ExtraCardRewardBody(ConversionPools pools) =>
        RunPrograms.When<CombatResolvedRunEvent>(RunEventValues.CombatWasVictory,
        [
            Literal(new OfferRewardRunEffect(
                new RewardId("event:librarian"), pools.CardRewardSource(), 1)),
            Done(ExtraCardReward),
        ]);

    // "Next eligible NORMAL combat … effect waits through ineligible nodes." A node-entered reaction resolves
    // BEFORE the node itself does, so an opening installed here lands in the fight being walked into — and a
    // node that is not an ordinary fight simply does not match, which is exactly what "waits" means.
    private static ITriggeredRunEffectDefinition ShortestPathWaitsBody(ConversionPools pools) =>
        RunPrograms.When<NodeEnteredRunEvent>(NormalCombat(),
        [
            Literal(Openings.NextCombat(
                Applies(ActTwoEventObjects.ShortestPath),
                Diminish(25))),
            Literal(new InstallProgramByIdRunEffect(new RunProgramSourceId(GarnishedReward))),
            Literal(new InstallProgramByIdRunEffect(new RunProgramSourceId(ExtraCardReward))),
            Done(ShortestPathWaits),
        ]);

    // The Necrology Window's borrowed life, waiting for an ordinary fight the same way.
    private static ITriggeredRunEffectDefinition UnfinishedLifeWaitsBody() =>
        RunPrograms.When<NodeEnteredRunEvent>(NormalCombat(),
        [
            Literal(Openings.NextCombat(
                new CombatNodeModel("applyStatus", "highestHealthEnemy",
                    CombatAmountSpec.FromConst(1), StatusId: ActTwoEventObjects.UnfinishedLife),
                new CombatNodeModel("applyStatus", "highestHealthEnemy",
                    CombatAmountSpec.FromConst(1), StatusId: ActTwoEventObjects.UnfinishedReturn))),
            Literal(new InstallProgramByIdRunEffect(new RunProgramSourceId(NecrologyBounty))),
            Done(UnfinishedLifeWaits),
        ]);

    // "…after victory gain 75 bonus Gold."
    private static ITriggeredRunEffectDefinition NecrologyBountyBody() =>
        RunPrograms.When<CombatResolvedRunEvent>(RunEventValues.CombatWasVictory,
        [
            RunEffectTemplates.GainResource(StandardRunIds.Gold, RunExpr.Const(75)),
            Done(NecrologyBounty),
        ]);

    // ── shorthands ────────────────────────────────────────────────────────────────────────────────────────

    // An ordinary fight: a combat node the map generated as Combat or MultiCombat. An Elite, a Boss and a mimic
    // are all combat nodes too, and none of them is what the design means by "normal combat".
    private static IRunExpression<bool> NormalCombat() =>
        RunExpr.And(
            new EventBoolValueExpression(RunEventFields.NodeIsCombat),
            RunExpr.Or(
                RunEventValues.NodeHasTag(MapNodeTags.Combat),
                RunEventValues.NodeHasTag(MapNodeTags.MultiCombat)));

    // "Enemies have N% less Max HP." Max health cannot be lowered from outside a fight, so the shortfall is paid
    // as unblockable damage at the opening bell, read per body off its own maximum (Act I's Expedited Route).
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

    // What the finished fight wrote down about the two promises it was carrying.
    private static IRunExpression<int> AmendmentPlayed =>
        RunEventValues.CombatCounter(ActTwoEventObjects.AmendmentPlayed.ToString());

    private static IRunExpression<int> VowHeld =>
        RunEventValues.CombatCounter(ActTwoEventObjects.VowHeld.ToString());

    private static IRunSelector<RunCardInstance> Tagged(string marking) =>
        RunSelectors.DeckCards.WithTag(new RunCardTagId(marking));

    private static IRunEffectTemplate Untag(string marking) =>
        Literal(new TagCardsRunEffect(Tagged(marking), new RunCardTagId(marking), false));

    private static IRunEffectTemplate Literal(IRunEffectRequest effect) => RunEffectTemplates.Literal(effect);

    // The last thing a one-shot promise does: name itself, and stop being pending.
    private static IRunEffectTemplate Done(string program) =>
        Literal(new UninstallRunProgramRunEffect(new RunProgramId(program)));
}
