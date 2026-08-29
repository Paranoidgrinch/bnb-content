using System.Collections;
using System.Reflection;

using BnbContent.Converter;
using RogueDeck.Run;

namespace BnbContent.Tests;

// A TRIGGER'S CONDITION MAY READ THE EVENT; ITS EFFECTS MAY NOT.
//
// A declarative run program (RunPrograms.On/When) is evaluated in two moments, not one. The condition — and
// any effect TEMPLATE — is evaluated at dispatch, while the event that woke it is still in scope. A plain
// effect passed to the same trigger is wrapped as a literal, queued, and drained afterwards, when the event
// is gone; anything inside it that asks the event a question throws
// "Event field '…' was evaluated without a matching event in context".
//
// That is a trap with no shape to it: the relic reads correctly, compiles, serializes, and only dies on the
// run where somebody is wearing it and the event actually fires. Bounty Hook — "gain 20 Gold after an Elite,
// or 35 if you finished below half HP" — carried it for a week, invisible only because no shop could sell it
// (see ADAPTATIONS, "The shop is a fixed shape"). It is written as two triggers now, and this test is the
// reason it cannot come back anywhere else.
public class QueuedEffectTests
{
    private static readonly RunBlueprint Game =
        BlueprintAssembler.Build(BabData.Load(TestData.Directory), seed: 20260829);

    [Fact]
    public void No_queued_effect_asks_the_event_a_question()
    {
        var offenders = new List<string>();

        foreach (var relic in Game.Relics)
            foreach (var program in relic.RunPrograms)
                Check($"relic '{relic.Id}'", program, offenders);

        foreach (var (id, program) in Game.Programs ?? new Dictionary<string, ITriggeredRunEffectDefinition>())
            Check($"program '{id}'", program, offenders);

        Assert.True(offenders.Count == 0, string.Join("\n", offenders));
    }

    // The detector, shown on a known offender: exactly the shape Bounty Hook had. A net nobody has seen catch
    // anything is not evidence that the water is empty — which is the lesson the Warden left (ADAPTATIONS).
    [Fact]
    public void The_check_catches_the_shape_that_was_wrong()
    {
        var offenders = new List<string>();

        Check("the probe", RunPrograms.When<CombatResolvedRunEvent>(
            RunExpr.True,
            new ConditionalRunEffect(
                RunExpr.LessThan(RunEventValues.CombatHeroHpRemaining, RunExpr.MaxHealth),
                [new ChangeResourceRunEffect(StandardRunIds.Gold, 35)],
                [new ChangeResourceRunEffect(StandardRunIds.Gold, 20)])),
            offenders);

        var offender = Assert.Single(offenders);
        Assert.Contains("EventIntValueExpression", offender);
    }

    // The trigger's queued effects are its LITERAL templates: everything else is built at dispatch and may
    // read whatever it likes.
    private static void Check(string where, ITriggeredRunEffectDefinition program, List<string> offenders)
    {
        foreach (var literal in Templates(program).OfType<LiteralEffectTemplate>())
            foreach (var reader in EventReadersIn(literal.Effect))
                offenders.Add($"{where}: a queued {literal.Effect.GetType().Name} reads {reader}");
    }

    private static IEnumerable<IRunEffectTemplate> Templates(ITriggeredRunEffectDefinition program)
    {
        // DataTriggeredRunEffect<TEvent> is generic in its event, so the templates are reached by name.
        var property = program.GetType().GetProperty("Templates", BindingFlags.Public | BindingFlags.Instance);
        return property?.GetValue(program) as IEnumerable<IRunEffectTemplate> ?? [];
    }

    // Every event-reading expression buried anywhere in an effect's object graph.
    private static IReadOnlyList<string> EventReadersIn(object root)
    {
        var found = new List<string>();
        var seen = new HashSet<object>(ReferenceEqualityComparer.Instance);
        var pending = new Stack<object>();
        pending.Push(root);

        while (pending.Count > 0)
        {
            var current = pending.Pop();
            if (current is null || !seen.Add(current))
                continue;

            var type = current.GetType();
            if (ReadsTheEvent(type))
                found.Add(type.Name);

            if (current is string || type.IsPrimitive)
                continue;

            if (current is IEnumerable items and not string)
            {
                foreach (var item in items)
                    if (item is not null)
                        pending.Push(item);
                continue;
            }

            if (type.Assembly != typeof(RunBlueprint).Assembly && type.Assembly != typeof(BabData).Assembly)
                continue;

            foreach (var member in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
                if (member.CanRead && member.GetIndexParameters().Length == 0)
                    Push(member.GetValue(current));
            foreach (var member in type.GetFields(BindingFlags.Public | BindingFlags.Instance))
                Push(member.GetValue(current));

            void Push(object? value)
            {
                if (value is not null && value is not string && !value.GetType().IsPrimitive)
                    pending.Push(value);
            }
        }

        return found;
    }

    // The engine's event readers are its `Event…Expression` types, and they are named that because that is
    // what they are — there is no interface to ask, since reading the event is a property of what Evaluate
    // does, not of its signature. That makes this a NAME test, which is why the fact above proves it catches
    // the real shape rather than trusting the pattern.
    private static bool ReadsTheEvent(Type type) =>
        type.Assembly == typeof(RunBlueprint).Assembly
        && type.Name.StartsWith("Event", StringComparison.Ordinal)
        && type.Name.Contains("Expression", StringComparison.Ordinal);
}
