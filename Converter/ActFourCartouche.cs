using System.Text.Json;
using RogueDeck.Core.Combat;
using RogueDeck.Sandbox.Composition;
using RogueDeck.Scenario.Authoring;

namespace BnbContent.Converter;

// Act IV, Stage 15 — The Cartouche Chambers. Two bodies that between them decide what a blessing of yours is
// FOR, and neither answer is "yours".
//
//   The Name-Erasing Chisel Spirit treats favour, blessing and identity as mistakes in stone. The first thing
//   that would go right for you each round is struck out — never gained, not taken back — and a doubt is cut
//   where it would have been.
//   The Royal Genealogy Wall claims every blessing as ancestral property. The first one you ACTUALLY get each
//   round is entered in the lineage as Royal Favor, and the Wall spends that later on its own defence and its
//   own retaliation. Your status is never stolen: you keep every stack. What the Wall takes is the fact of it.
//
// Put them in one room (Encounter 49) and §3.8 falls out of the two rules by itself: a gain the chisel erases
// raises no gain at all, so the Wall is fed nothing by it — and a second, later blessing that survives may
// still feed the Wall that round. The player can therefore expose a small buff on purpose to spend the
// chisel, and the ordering is deterministic rather than a priority table.
public static partial class ActFour
{
    public const string ChiselSpiritEnemyId = "name_erasing_chisel_spirit";
    public const string GenealogyWallEnemyId = "royal_genealogy_wall";

    public const string EraseTheFavorId = "erase_the_favor";
    public const string ChiselSetId = "chisel_set_against_your_name";
    public const string DynasticFavorId = "dynastic_favor";
    public const string RoyalFavorId = "royal_favor";

    // What the Wall's lineage will hold, and what a Favor is worth when it is spent.
    public const int RoyalFavorCap = 3;
    private const int FavorDamage = 3;
    private const int FavorBlock = 4;

    // One stack of the chisel refuses one WHOLE application, however large — "the first positive status gain
    // each round", not "the first stack". The engine spends a prohibition stack for stack and rounds the
    // spend up, so a number bigger than any blessing in the game is how "all of it" is spelled.
    private const int WholeBlessing = 99;

    public static CounterId FavorTakenThisRound => new("favor_taken_this_round");

    public static EffectProgram<EnemyActionContext>? CartoucheIntent(string enemyId, string intentId) =>
        $"{enemyId}.{intentId}" switch
        {
            "royal_genealogy_wall.ancestral_claim" => AncestralClaim(20),
            "royal_genealogy_wall.royal_line" => RoyalLine(32),
            _ => null,
        };

    // The chisel is set against the player from the first bell, because the opening round starts before the
    // bodies in it are dressed: a rule nobody wears yet does not fire, so the first round's chisel has to be
    // served with the fight rather than topped up into it.
    public static IReadOnlyList<StartingStatusSpec> CartoucheOpening(string enemyId) =>
        enemyId == ChiselSpiritEnemyId
            ? [new StartingStatusSpec(new StatusDefinitionId(ChiselSetId), 1)]
            : [];

    // ── the Name-Erasing Chisel Spirit ────────────────────────────────────────────────────────────────────

    // The erasure itself, worn by the player: a prohibition that refuses BLESSINGS and is spent doing it.
    //
    // Prohibition and not removal, and the difference is the whole identity. A status that landed and was
    // then stripped was still gained — every rule that answers a gain has already heard it, the Wall's
    // lineage included. The master is explicit that the erased status is "never gained", and a refusal is the
    // only shape in this engine that means that.
    public static StatusData ChiselSet() => new()
    {
        Id = ChiselSetId,
        NameKey = "Chisel Set Against Your Name",
        DescriptionKey =
            "The chisel is set: the next positive status you would gain this round is erased outright — "
            + "never gained — and a doubt is cut where it would have been. One a round.",
        Polarity = StatusPolarity.Debuff,
        StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
        UsesStacks = true,
        Tags = [],
        PassiveModifiers = [],
        Triggers = [],
        Prevention = new StatusPreventionData(StatusPreventionScope.Buffs, StacksPerStack: WholeBlessing),
    };

    // The spirit's own rule: it sets the chisel again each round, and cuts its drawback into the space the
    // erased blessing left.
    public static StatusData EraseTheFavor() => new()
    {
        Id = EraseTheFavorId,
        NameKey = "Erase the Favor",
        DescriptionKey =
            "This chisel reads favour, blessing and identity as mistakes in the stone. Once each round it "
            + "strikes out the first one you would gain, and cuts 1 Doubt in its place.",
        Polarity = StatusPolarity.Neutral,
        StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
        UsesStacks = false,
        Tags = [],
        PassiveModifiers = [],
        Triggers =
        [
            Trigger(CutTheDoubt(), nameof(TriggerEvent.StatusApplicationPrevented), StatusTriggerScope.Anywhere),
            Trigger(SetTheChisel(), nameof(TriggerEvent.RoundStarted), StatusTriggerScope.Anywhere),
        ],
    };

    // "Then apply the Chisel's visible drawback." It answers ITS OWN refusal and nobody else's — which is a
    // question the engine can now be asked, and had to be: what was refused is one thing, which prohibition
    // did the refusing is another, and a rule about the chisel's work must not fire for a stranger's ward.
    private static EffectProgram<StatusApplicationBlockedTriggeredEffectContext> CutTheDoubt()
    {
        var spirit = Bearer(EraseTheFavorId);

        return new EffectProgram<StatusApplicationBlockedTriggeredEffectContext>(
            new ConditionalEffectNode<StatusApplicationBlockedTriggeredEffectContext>(
                new TriggerEventPreventerIsExpression<StatusApplicationBlockedTriggeredEffectContext>(
                    new StatusDefinitionId(ChiselSetId)),
                new ApplyStatusNode<StatusApplicationBlockedTriggeredEffectContext>(
                    Applicant, new StatusDefinitionId(Cards.Keywords.Doubt),
                    new ConstantExpression<StatusApplicationBlockedTriggeredEffectContext>(1),
                    sourceSelector: spirit)));
    }

    // One chisel a round, and only ever one: it is set again when the round turns, and a round in which
    // nothing was struck out finds it already there.
    private static EffectProgram<RoundStartedTriggeredEffectContext> SetTheChisel() =>
        new(new ConditionalEffectNode<RoundStartedTriggeredEffectContext>(
            new NotExpression<RoundStartedTriggeredEffectContext>(
                new TargetHasStatusExpression<RoundStartedTriggeredEffectContext>(
                    Applicant, new StatusDefinitionId(ChiselSetId))),
            new ApplyStatusNode<RoundStartedTriggeredEffectContext>(
                Applicant, new StatusDefinitionId(ChiselSetId),
                new ConstantExpression<RoundStartedTriggeredEffectContext>(1),
                sourceSelector: Bearer(EraseTheFavorId))));

    // ── the Royal Genealogy Wall ──────────────────────────────────────────────────────────────────────────

    // The lineage's own resource (§3.8). Not a copy of the player's status and not a theft of it: the old
    // "clone any positive player status" idea could not answer bespoke buffs, non-stacking ones, or the ones
    // that change a rule, so what the Wall keeps is a number of its own that it spends its own way.
    public static StatusData RoyalFavor() => new()
    {
        Id = RoyalFavorId,
        NameKey = "Royal Favor",
        DescriptionKey =
            "Blessings entered in the royal lineage, at most 3. The Wall spends them on its own defence "
            + "(+4 Block each) or on royal retaliation (+3 damage each).",
        Polarity = StatusPolarity.Buff,
        StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
        UsesStacks = true,
        Tags = [],
        PassiveModifiers = [],
        Triggers = [],
    };

    // "The first time each round the player ACTUALLY gains a positive status: the Wall gains Royal Favor
    // equal to the stacks gained, up to its cap."
    //
    // Actually is the whole of §3.8's priority rule, and it needs no priority table: an erased gain raises no
    // application at all, so the Wall simply never hears it, and a later blessing that survives that round is
    // still the first one it hears. The two bodies order themselves.
    public static StatusData DynasticFavor() => new()
    {
        Id = DynasticFavorId,
        NameKey = "Dynastic Favor",
        DescriptionKey =
            "Every blessing is ancestral property: the first positive status you actually gain each round is "
            + "entered in the lineage as that many Royal Favor. You keep your own.",
        Polarity = StatusPolarity.Neutral,
        StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
        UsesStacks = false,
        Tags = [],
        PassiveModifiers = [],
        Triggers =
        [
            Trigger(EnterInTheLineage<StatusAppliedTriggeredEffectContext>(),
                nameof(TriggerEvent.StatusApplied), StatusTriggerScope.Anywhere),
            Trigger(EnterInTheLineage<StatusMergedTriggeredEffectContext>(),
                nameof(TriggerEvent.StatusMerged), StatusTriggerScope.Anywhere),
            Trigger(ClearLatch<RoundStartedTriggeredEffectContext>(DynasticFavorId, FavorTakenThisRound),
                nameof(TriggerEvent.RoundStarted), StatusTriggerScope.Anywhere),
        ],
    };

    private static EffectProgram<TContext> EnterInTheLineage<TContext>() where TContext : class
    {
        var wall = Bearer(DynasticFavorId);

        var room = new SubtractExpression<TContext>(
            new ConstantExpression<TContext>(RoyalFavorCap),
            new CombatantStatusStacksExpression<TContext>(wall, new StatusDefinitionId(RoyalFavorId)));

        return new EffectProgram<TContext>(
            new ConditionalEffectNode<TContext>(
                new AndExpression<TContext>(
                    new AndExpression<TContext>(
                        // A blessing, on the player, that is not itself a copy — the act's own gate, read
                        // for the other polarity.
                        new TargetHasStatusExpression<TContext>(
                            CombatantTargetSelectors.EventTarget,
                            new StatusDefinitionId(PassiveStatuses.ApplicantId)),
                        new AndExpression<TContext>(
                            new TriggerEventStatusPolarityIsExpression<TContext>(StatusPolarity.Buff),
                            new NotExpression<TContext>(new TriggerEventIsReplicatedExpression<TContext>()))),
                    new AndExpression<TContext>(
                        NotYetThisRound<TContext>(wall, FavorTakenThisRound),
                        new ComparisonExpression<TContext>(
                            room, ComparisonOperator.Greater, new ConstantExpression<TContext>(0)))),
                new CausalSequenceEffectNode<TContext>(
                [
                    new SetCombatantCounterNode<TContext>(
                        wall, FavorTakenThisRound, new ConstantExpression<TContext>(1), relative: false),

                    // "Equal to stacks gained" — the size of THIS application, which is what the engine now
                    // reports for a merge as well as for a first application; the instance's new total would
                    // read a one-stack blessing on top of three as four.
                    new ApplyStatusNode<TContext>(
                        wall, new StatusDefinitionId(RoyalFavorId),
                        new MinExpression<TContext>(new EventAmountExpression<TContext>(), room),
                        sourceSelector: wall),
                ])));
    }

    // Royal retaliation: the lineage is cashed in, three damage a Favor, and the wall is a plain wall again
    // until the next blessing feeds it.
    private static EffectProgram<EnemyActionContext> AncestralClaim(int damage) =>
        new(new CausalSequenceEffectNode<EnemyActionContext>(
        [
            new DealDamageNode<EnemyActionContext>(
                Applicant,
                new AddExpression<EnemyActionContext>(
                    Const(damage),
                    new MultiplyExpression<EnemyActionContext>(
                        Const(FavorDamage),
                        new CombatantStatusStacksExpression<EnemyActionContext>(
                            CombatantTargetSelectors.Source, new StatusDefinitionId(RoyalFavorId))))),

            SpendTheLineage(),
        ]));

    // …and the same lineage spent the other way, on the wall the dynasty puts between you and it.
    private static EffectProgram<EnemyActionContext> RoyalLine(int block) =>
        new(new CausalSequenceEffectNode<EnemyActionContext>(
        [
            new GainBlockNode<EnemyActionContext>(
                CombatantTargetSelectors.Source,
                new AddExpression<EnemyActionContext>(
                    Const(block),
                    new MultiplyExpression<EnemyActionContext>(
                        Const(FavorBlock),
                        new CombatantStatusStacksExpression<EnemyActionContext>(
                            CombatantTargetSelectors.Source, new StatusDefinitionId(RoyalFavorId))))),

            SpendTheLineage(),
        ]));

    // Spent, not merely used: the cap is 3, and a Favor that stayed on the wall after being cashed would make
    // the ceiling a floor.
    private static RemoveStatusNode<EnemyActionContext> SpendTheLineage() =>
        new(CombatantTargetSelectors.Source, new StatusDefinitionId(RoyalFavorId));
}
