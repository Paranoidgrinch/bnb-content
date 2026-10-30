using System.Text.Json;
using RogueDeck.Core.Combat;
using RogueDeck.Scenario.Authoring;

namespace BnbContent.Converter;

// Act III, Stage 3 — Tollwater, where the act stops asking what you did and starts naming a price for it. Since
// the content cut of 2026-10-29: a shell that names the only coin it takes, a fish that leaves once its oath is
// kept twice, and a ford whose every right is a bill — and whose other bank resents being paid second.
public static partial class ActThree
{
    public const string PaymentAccordingToCharterId = "payment_according_to_charter";
    public const string ShellEnergyId = "shell_names_energy";
    public const string ShellDeedId = "shell_names_a_deed";
    public const string ShellWorkingId = "shell_names_a_working";
    public const string OathKeptId = "oath_kept";
    public const string TollOnBothBanksId = "toll_on_both_banks";

    public const int OathsToLeave = 2;
    public const int FordWergildPerClaim = 2;
    public const int AngeredBankStrength = 2;

    // ── Charter-Shell Snail ───────────────────────────────────────────────────────────────────────────────
    //
    // "Its shell names the only accepted payment, rotating each turn between Energy, a Deed and a Working."
    // The shell is one of three chips; the payment itself (`ActThree.OfferACard`, `CanPayInCoin`) reads it
    // while the Snail is owed anything.
    public static StatusData PaymentAccordingToCharter()
    {
        var snail = Lawgiver(PaymentAccordingToCharterId);

        IEffectNode<TurnStartedTriggeredEffectContext> Turn(string from, string to) =>
            new CausalSequenceEffectNode<TurnStartedTriggeredEffectContext>(
            [
                new RemoveStatusNode<TurnStartedTriggeredEffectContext>(snail, new StatusDefinitionId(from)),
                new ApplyStatusNode<TurnStartedTriggeredEffectContext>(
                    snail, new StatusDefinitionId(to), new ConstantExpression<TurnStartedTriggeredEffectContext>(1)),
            ]);

        var rotate = new EffectProgram<TurnStartedTriggeredEffectContext>(
            new ConditionalEffectNode<TurnStartedTriggeredEffectContext>(
                new AndExpression<TurnStartedTriggeredEffectContext>(
                    PlayersTurn<TurnStartedTriggeredEffectContext>(),
                    Alive<TurnStartedTriggeredEffectContext>(snail)),
                new ConditionalEffectNode<TurnStartedTriggeredEffectContext>(
                    Wears<TurnStartedTriggeredEffectContext>(snail, ShellEnergyId),
                    Turn(ShellEnergyId, ShellDeedId),
                    new ConditionalEffectNode<TurnStartedTriggeredEffectContext>(
                        Wears<TurnStartedTriggeredEffectContext>(snail, ShellDeedId),
                        Turn(ShellDeedId, ShellWorkingId),
                        Turn(ShellWorkingId, ShellEnergyId)))));

        return Rule(PaymentAccordingToCharterId, "Payment According to Charter",
            "While you owe the Snail, its shell names the only payment Make Amends accepts: Energy, a Deed or "
            + "a Working. It turns each turn.",
            [new StatusTriggerData("TurnStarted", JsonSerializer.SerializeToElement(
                rotate, CombatJson.CreateOptions<TurnStartedTriggeredEffectContext>()),
                StatusTriggerScope.Anywhere)]);
    }

    public static StatusData ShellEnergy() =>
        Marker(ShellEnergyId, "Shell: Energy", "While you owe the Snail, only coin pays: Make Amends with Energy.");

    public static StatusData ShellDeed() =>
        Marker(ShellDeedId, "Shell: a Deed", "While you owe the Snail, only an offered Deed pays.");

    public static StatusData ShellWorking() =>
        Marker(ShellWorkingId, "Shell: a Working", "While you owe the Snail, only an offered Working pays.");

    // While the Snail is owed, does its shell accept this means of payment?
    private static ICombatExpression<CardPlayContext, bool> SnailOwed() =>
        new ComparisonExpression<CardPlayContext>(
            new CombatantStatusStacksFromSourceExpression<CardPlayContext>(
                CombatantTargetSelectors.Source, new StatusDefinitionId(WergildId), Lawgiver(PaymentAccordingToCharterId)),
            ComparisonOperator.Greater,
            new ConstantExpression<CardPlayContext>(0));

    private static ICombatExpression<CardPlayContext, bool> CharterAcceptsCoin() =>
        new OrExpression<CardPlayContext>(
            new NotExpression<CardPlayContext>(SnailOwed()),
            Wears<CardPlayContext>(Lawgiver(PaymentAccordingToCharterId), ShellEnergyId));

    private static ICombatExpression<CardPlayContext, bool> CharterAcceptsOffering(
        ICardInstanceExpression<CardPlayContext> offering) =>
        new OrExpression<CardPlayContext>(
            new NotExpression<CardPlayContext>(SnailOwed()),
            new OrExpression<CardPlayContext>(
                new AndExpression<CardPlayContext>(
                    Wears<CardPlayContext>(Lawgiver(PaymentAccordingToCharterId), ShellDeedId),
                    new CardInstanceHasTagExpression<CardPlayContext>(offering, new TagId(Cards.CardAuthoring.DeedTag))),
                new AndExpression<CardPlayContext>(
                    Wears<CardPlayContext>(Lawgiver(PaymentAccordingToCharterId), ShellWorkingId),
                    new CardInstanceHasTagExpression<CardPlayContext>(offering, new TagId(Cards.CardAuthoring.WorkingTag)))));

    // ── Streamside Oath-Fish ──────────────────────────────────────────────────────────────────────────────
    //
    // "Pay its Wergild in full twice and it leaves peacefully, counting as defeated." Spliced into the one
    // settlement in `ActThreeWergild`, which is the only place that knows a demand was settled in full.
    public static IEffectNode<TurnEndedTriggeredEffectContext> AnOathKept()
    {
        var creditor = CombatantTargetSelectors.IterationTarget;
        return new ConditionalEffectNode<TurnEndedTriggeredEffectContext>(
            Wears<TurnEndedTriggeredEffectContext>(creditor, OathAcceptedId),
            new CausalSequenceEffectNode<TurnEndedTriggeredEffectContext>(
            [
                new ApplyStatusNode<TurnEndedTriggeredEffectContext>(
                    creditor, new StatusDefinitionId(OathKeptId), new ConstantExpression<TurnEndedTriggeredEffectContext>(1)),
                new ConditionalEffectNode<TurnEndedTriggeredEffectContext>(
                    new ComparisonExpression<TurnEndedTriggeredEffectContext>(
                        new CombatantStatusStacksExpression<TurnEndedTriggeredEffectContext>(
                            creditor, new StatusDefinitionId(OathKeptId)),
                        ComparisonOperator.GreaterOrEqual,
                        new ConstantExpression<TurnEndedTriggeredEffectContext>(OathsToLeave)),
                    new SetHealthNode<TurnEndedTriggeredEffectContext>(
                        creditor, new ConstantExpression<TurnEndedTriggeredEffectContext>(0))),
            ]));
    }

    public static StatusData OathKept() => new()
    {
        Id = OathKeptId,
        NameKey = "Oath Kept",
        DescriptionKey = "Demands you settled with it in full. At 2 it leaves in peace, counted as defeated.",
        Polarity = StatusPolarity.Neutral,
        StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
        UsesStacks = true,
        Tags = [],
        PassiveModifiers = [],
        Triggers = [],
    };

    // ── Two-Bank Toll Ford ────────────────────────────────────────────────────────────────────────────────
    //
    // "Every Claim instantly becomes Wergild; paying one bank angers the other." A Claim made for the Ford is
    // spent at once on a demand for 2; a demand settled in full gives the Ford 2 Strength.
    public static StatusData TollOnBothBanks()
    {
        var ford = CombatantTargetSelectors.EventTarget;

        EffectProgram<TContext> Program<TContext>() where TContext : class =>
            new(new ConditionalEffectNode<TContext>(
                new TriggerEventStatusIsExpression<TContext>(new StatusDefinitionId(ClaimCreatedId)),
                new CausalSequenceEffectNode<TContext>(
                [
                    ConsumeClaim<TContext>(ford),
                    DemandWergild<TContext>(ford, FordWergildPerClaim),
                ])));

        return Rule(TollOnBothBanksId, "Toll on Both Banks",
            "Every Claim the Ford gains becomes 2 Wergild at once. Settle a demand in full and the other bank "
            + "is angered: the Ford gains 2 Strength.",
            [
                new StatusTriggerData("StatusApplied", JsonSerializer.SerializeToElement(
                    Program<StatusAppliedTriggeredEffectContext>(),
                    CombatJson.CreateOptions<StatusAppliedTriggeredEffectContext>())),
                new StatusTriggerData("StatusMerged", JsonSerializer.SerializeToElement(
                    Program<StatusMergedTriggeredEffectContext>(),
                    CombatJson.CreateOptions<StatusMergedTriggeredEffectContext>())),
            ]);
    }

    public static IEffectNode<TurnEndedTriggeredEffectContext> TheOtherBankAngered()
    {
        var creditor = CombatantTargetSelectors.IterationTarget;
        return new ConditionalEffectNode<TurnEndedTriggeredEffectContext>(
            Wears<TurnEndedTriggeredEffectContext>(creditor, TollOnBothBanksId),
            new ApplyStatusNode<TurnEndedTriggeredEffectContext>(
                creditor, new StatusDefinitionId("strength"),
                new ConstantExpression<TurnEndedTriggeredEffectContext>(AngeredBankStrength)));
    }
}
