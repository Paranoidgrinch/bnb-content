using RogueDeck.Core.Combat;
using RogueDeck.Sandbox.Composition;

namespace BnbContent.Converter;

// THE HAND A TURN OPENS WITH — and how a rule says it means that one.
//
// Almost every relic, inscription and keyword in this game that "happens at the start of your turn" is
// written on the DRAW and not on the turn's start, and has to be: a turn-start trigger runs before the hand
// exists, so a promise about the hand can only be kept once the hand is dealt. But the engine has just one
// event for that, and it fires for EVERY draw — the turn's opening hand and every card any card or rule
// draws afterwards, all of them the same announcement.
//
// Read literally, that turns three kinds of rule into bugs at once:
//
//   • a rule that COUNTS TURNS at the draw counts draws instead — the Brass Service Bell, which rings "every
//     third turn", was ringing every third DRAW;
//   • a rule that PAYS at the draw and pays in CARDS feeds itself: its own card is a draw, which announces
//     itself, which pays again. Some clear their latch after the payment, so the payment sees the latch still
//     set and there is nothing to stop it at all;
//   • and every other "at the hand" rule — block, marks, discounts — quietly pays once per draw, so a deck
//     that draws is a deck that is paid several times a turn.
//
// The three together are what ended a real run in Act IV: one relic drew, its draw was another turn to the
// next relic, and nineteen draws deep the fight hit the engine's cycle limit and the run stopped.
//
// So the reading is written down once, here: a rule hung on the draw means the FIRST draw of the turn. It is
// applied by every file's own trigger helper, to every CardsDrawn trigger it installs. The rules that really
// do mean every draw — Nanshe's ration, Inanna's Claim of Hands, the Causeway envoy counting the hand as it
// changes — live in files that do not apply it, and say so where they are written.
public static class TheOpeningHand
{
    // The same program, refused unless this is the turn's first draw. `<= 1` rather than `== 1` so that a
    // fight assembled without the engine's draw counting behaves exactly as it did before rather than going
    // silent.
    public static EffectProgram<TContext> Only<TContext>(EffectProgram<TContext> program)
        where TContext : class
    {
        ArgumentNullException.ThrowIfNull(program);
        return new EffectProgram<TContext>(
            new ConditionalEffectNode<TContext>(
                new ComparisonExpression<TContext>(
                    new CardDrawsThisTurnExpression<TContext>(CombatantTargetSelectors.Source),
                    ComparisonOperator.LessOrEqual,
                    new ConstantExpression<TContext>(1)),
                program.Root));
    }

    // Applied by a trigger helper: the wrap for a CardsDrawn trigger, the program untouched for every other.
    public static EffectProgram<TContext> For<TContext>(EffectProgram<TContext> program, string trigger)
        where TContext : class =>
        string.Equals(trigger, nameof(TriggerEvent.CardsDrawn), StringComparison.Ordinal)
            ? Only(program)
            : program;
}
