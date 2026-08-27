using System.Text.Json;
using RogueDeck.Core.Combat;
using RogueDeck.Scenario.Authoring;

namespace BnbContent.Converter;

// Act III, Stage 4 — The Tollwater Crossings, where the act stops asking what you did and starts naming a
// price for it. Three parties, three readings of the same demand: one writes the payment procedure on its
// own shell, one treats settlement as sacred, and one holds that a recognised right and an active demand are
// two separate legal facts and collects both.
public static partial class ActThree
{
    public const string PaymentAccordingToCharterId = "payment_according_to_charter";
    public const string TollOnBothBanksId = "toll_on_both_banks";

    // "Cards with Base Cost 0 cannot be used as Offerings to pay Wergild owed to Charter-Shell Snail."
    //
    // A rule about what does not count as payment cannot live where payments are refused — nothing asks the
    // creditor's permission — so it lives in the payment itself (`ActThree.OfferACard`) and this is the
    // licence that payment looks for. The Snail carries it; while the Snail is owed anything, a free card
    // buys nothing.
    public static StatusData PaymentAccordingToCharter() =>
        Marker(PaymentAccordingToCharterId, "Payment According to Charter",
            "While the Charter-Shell Snail is owed Wergild, a card that costs nothing is not an offering.");

    // "Whenever the Ford gains a newly created Claim, immediately create Wergild 1 from Ford. The Claim
    // remains." A recognised right and an active demand are separate legal facts, and the Ford is the body
    // that says so: it does not spend its standing to make a demand, it exercises it.
    public static StatusData TollOnBothBanks()
    {
        var ford = CombatantTargetSelectors.EventTarget;

        EffectProgram<TContext> Program<TContext>() where TContext : class =>
            new(new ConditionalEffectNode<TContext>(
                new TriggerEventStatusIsExpression<TContext>(new StatusDefinitionId(ClaimCreatedId)),
                DemandWergild<TContext>(ford, 1)));

        return Rule(TollOnBothBanksId, "Toll on Both Banks",
            "Every Claim the Two-Bank Toll Ford is granted becomes a demand for 1 Wergild as well. The Claim "
            + "stays where it is: a right and a price are two different things.",
            [
                // A merged status raises StatusApplied for the first grant and StatusMerged for every one
                // after, so the toll has to be collected at both.
                new StatusTriggerData("StatusApplied", JsonSerializer.SerializeToElement(
                    Program<StatusAppliedTriggeredEffectContext>(),
                    CombatJson.CreateOptions<StatusAppliedTriggeredEffectContext>())),
                new StatusTriggerData("StatusMerged", JsonSerializer.SerializeToElement(
                    Program<StatusMergedTriggeredEffectContext>(),
                    CombatJson.CreateOptions<StatusMergedTriggeredEffectContext>())),
            ]);
    }
}
