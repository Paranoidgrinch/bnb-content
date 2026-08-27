using System.Text.Json;
using RogueDeck.Core.Combat;
using RogueDeck.Scenario.Authoring;

namespace BnbContent.Converter;

// Act III, Stage 3 — The Meadow of Living Testimony. Neither of these two presses the player: they argue
// about what happened to them. The Foxglove says "I saw that too" and acquires standing of its own from a
// law it does not own; the Magpie says "no, I saw it otherwise" and takes the standing off whoever earned it.
//
// Both are support identities and both live INSIDE the moment a Trespass is filed, because that is where
// their questions can be answered: which law was broken (the Trespass does not carry it), and who the
// violation is going to be owed to (once it has landed it is too late to argue).
public static partial class ActThree
{
    public const string ISawThatTooId = "i_saw_that_too";
    public const string TestifiedThisTurnId = "testified_this_turn";
    public const string ContraryTestimonyId = "contrary_testimony";
    public const string ContestedThisTurnId = "contested_this_turn";

    // Which law the meadow has heard broken this turn, remembered by its number. 0 = nothing yet.
    public static CounterId RememberedLawCounter => new("remembered_law");

    // The Foxglove has two halves, and they answer two different moments.
    //
    // REMEMBERING watches the Trespass LAND, because a violation the player's Safe-Conduct refused is not
    // something a witness saw happen. What it reads at that moment is the law the filing wrote down on its
    // way past — the law, not the source, so the Magpie rewriting who owns the testimony does not change
    // what the Foxglove says it saw.
    //
    // TESTIFYING answers the VIOLATION and lives inside it (`ActThree.Violate`), because the second breach of
    // a law is exactly the one that law no longer punishes. That is why the Foxglove is put beside the Hedge:
    // the Hedge speaks once a turn, and the witness is what makes breaking its rule twice still cost you.
    public static StatusData ISawThatToo()
    {
        var foxglove = Lawgiver(ISawThatTooId);
        var law = new CombatantCounterExpression<StatusAppliedTriggeredEffectContext>(
            Applicant, LawBeingFiledCounter);

        var program = new EffectProgram<StatusAppliedTriggeredEffectContext>(
            new ConditionalEffectNode<StatusAppliedTriggeredEffectContext>(
                new AndExpression<StatusAppliedTriggeredEffectContext>(
                    new TriggerEventStatusIsExpression<StatusAppliedTriggeredEffectContext>(
                        new StatusDefinitionId(TrespassId)),
                    new AndExpression<StatusAppliedTriggeredEffectContext>(
                        // A LAW was broken. A pressure intent, or the Foxglove's own testimony, is not one.
                        new ComparisonExpression<StatusAppliedTriggeredEffectContext>(
                            law, ComparisonOperator.Greater,
                            new ConstantExpression<StatusAppliedTriggeredEffectContext>(0)),
                        // …and the meadow has not already settled on a law to listen for.
                        new ComparisonExpression<StatusAppliedTriggeredEffectContext>(
                            new CombatantCounterExpression<StatusAppliedTriggeredEffectContext>(
                                Applicant, RememberedLawCounter),
                            ComparisonOperator.Equal,
                            new ConstantExpression<StatusAppliedTriggeredEffectContext>(0)))),
                new SetCombatantCounterNode<StatusAppliedTriggeredEffectContext>(
                    Applicant, RememberedLawCounter, law, relative: false)));

        return Rule(ISawThatTooId, "I Saw That Too",
            "The Foxglove remembers the first law you are seen to break each turn. Break the same one again "
            + "that turn and it testifies: 1 Trespass owed to the Foxglove.",
            [
                new StatusTriggerData("StatusApplied", JsonSerializer.SerializeToElement(
                    program, CombatJson.CreateOptions<StatusAppliedTriggeredEffectContext>()),
                    StatusTriggerScope.Anywhere),
                new StatusTriggerData("TurnStarted", JsonSerializer.SerializeToElement(
                    ForgetTheTestimony(), CombatJson.CreateOptions<TurnStartedTriggeredEffectContext>()),
                    StatusTriggerScope.Anywhere),
            ]);
    }

    // The other half, run from inside the violation itself: this law has been broken before this turn, and
    // the Foxglove has not yet said so. It is asked BEFORE the law files its own Trespass, because that
    // filing is what teaches the meadow which law it is listening for — asked after, the first violation
    // would testify against itself.
    public static IEffectNode<TContext> WitnessTestimony<TContext>(int law)
        where TContext : class
    {
        var foxglove = Lawgiver(ISawThatTooId);

        return new ConditionalEffectNode<TContext>(
            new AndExpression<TContext>(
                // A Foxglove has to be standing, and to have kept quiet so far this turn.
                new AndExpression<TContext>(
                    new ComparisonExpression<TContext>(
                        new CombatantCurrentHealthExpression<TContext>(foxglove),
                        ComparisonOperator.Greater,
                        new ConstantExpression<TContext>(0)),
                    new ComparisonExpression<TContext>(
                        new CombatantStatusStacksExpression<TContext>(
                            foxglove, new StatusDefinitionId(TestifiedThisTurnId)),
                        ComparisonOperator.Equal,
                        new ConstantExpression<TContext>(0))),
                // …and this has to be the law it is listening for.
                new ComparisonExpression<TContext>(
                    new CombatantCounterExpression<TContext>(Applicant, RememberedLawCounter),
                    ComparisonOperator.Equal,
                    new ConstantExpression<TContext>(law))),
            new CausalSequenceEffectNode<TContext>(
            [
                // The latch is claimed BEFORE the filing, because the testimony lands as a Trespass of its
                // own and this rule would otherwise be asked about it again.
                new ApplyStatusNode<TContext>(
                    foxglove, new StatusDefinitionId(TestifiedThisTurnId), new ConstantExpression<TContext>(1)),
                FileTrespass<TContext>(foxglove),
            ]));
    }

    // A new turn has heard nothing yet.
    private static EffectProgram<TurnStartedTriggeredEffectContext> ForgetTheTestimony() =>
        new(new ConditionalEffectNode<TurnStartedTriggeredEffectContext>(
            PlayersTurn<TurnStartedTriggeredEffectContext>(),
            new CausalSequenceEffectNode<TurnStartedTriggeredEffectContext>(
            [
                new SetCombatantCounterNode<TurnStartedTriggeredEffectContext>(
                    Applicant, RememberedLawCounter,
                    new ConstantExpression<TurnStartedTriggeredEffectContext>(0), relative: false),
                new RemoveStatusNode<TurnStartedTriggeredEffectContext>(
                    Lawgiver(ISawThatTooId), new StatusDefinitionId(TestifiedThisTurnId)),
            ])));

    public static StatusData TestifiedThisTurn() =>
        Marker(TestifiedThisTurnId, "Testified",
            "The Foxglove has already given its account of this turn.");

    // "The first time each player turn another party would apply Trespass, and the Magpie holds fewer Claims
    // than they do, the Magpie may take ownership of that Trespass."
    //
    // The rule itself is not here — it lives in the filing (`ActThree.ContestedFiling`), because ownership
    // has to be decided before the violation lands. What is here is the Magpie's licence to be looked for,
    // and the per-turn latch that keeps it to one contradiction a turn.
    public static StatusData ContraryTestimony() =>
        Rule(ContraryTestimonyId, "Contrary Testimony",
            "Once a turn, a Trespass owed to a party holding more Claims than the Magpie is owed to the "
            + "Magpie instead. The argument is never about whether it happened.",
            [new StatusTriggerData("TurnStarted", JsonSerializer.SerializeToElement(
                ForgetTheContradiction(), CombatJson.CreateOptions<TurnStartedTriggeredEffectContext>()),
                StatusTriggerScope.Anywhere)]);

    private static EffectProgram<TurnStartedTriggeredEffectContext> ForgetTheContradiction() =>
        new(new ConditionalEffectNode<TurnStartedTriggeredEffectContext>(
            PlayersTurn<TurnStartedTriggeredEffectContext>(),
            new RemoveStatusNode<TurnStartedTriggeredEffectContext>(
                Lawgiver(ContraryTestimonyId), new StatusDefinitionId(ContestedThisTurnId))));

    public static StatusData ContestedThisTurn() =>
        Marker(ContestedThisTurnId, "Contested",
            "The Magpie has already contradicted somebody this turn.");
}
