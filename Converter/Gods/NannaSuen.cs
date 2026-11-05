using RogueDeck.Core.Combat;
using RogueDeck.Sandbox.Composition;
using RogueDeck.Scenario.Authoring;
using static BnbContent.Converter.Gods.GodShapes;

namespace BnbContent.Converter.Gods;

// G4 — NANNA-SUEN, LORD OF THE COUNTED MOON, FATHER OF THE SUN AND THE MORNING STAR (rework of 2026-11-05). He is
// the father of Utu and Inanna.
//
// A fixed ring of four phases, one a round, starting at New Moon. Each phase has exactly one move and one rule, and
// the phase he is in and the next one are chips on him:
//   New Moon    Count the Unseen: +30 Block, 2 Moonless into your draw pile; he takes 50 % less damage.
//   Waxing      Raise the Horns: 2x14, +3 Strength for good.
//   Full Moon   Crown of Ur: 45. The first card you play this round is Counted; at the next Full Moon a free
//               temporary copy of it is in your hand (Ethereal).
//   Waning      Pour Away the Light: 3x8; he takes double damage.
// Each orbit completed (four rounds) gives him a Radiance: +15 % to his damage and Block, for good.
public static class NannaSuen
{
    public const string EnemyId = "nanna_sin_lord_of_the_counted_moon";
    public const string EncounterId = "act_5_nanna_sin_lord_of_the_counted_moon";

    public const string RuleId = "nanna_the_counted_moon";
    public const string RadianceId = "nanna_radiance";
    public const string MoonlessCardId = "moonless";
    public const string CountedMark = "nanna_counted";
    public const string EchoMark = "nanna_echo";

    public const int MaxHealth = 1000;
    public const int NewMoonBlock = 30;
    public const int Moonless = 2;
    public const int NewMoonPercent = 50;
    public const int HornHits = 2;
    public const int HornDamage = 14;
    public const int HornStrength = 3;
    public const int CrownDamage = 45;
    public const int PourHits = 3;
    public const int PourDamage = 8;
    public const int WaningPercent = 200;
    public const int RadiancePercent = 15;

    public sealed record Phase(int Index, string Key, string Name, string Move, string Rule);

    public static readonly IReadOnlyList<Phase> Ring =
    [
        new(0, "new", "New Moon", "count_the_unseen", "He takes 50 % less damage. Count the Unseen: +30 Block, 2 Moonless into your draw pile."),
        new(1, "waxing", "Waxing", "raise_the_horns", "Raise the Horns: 2x14 and +3 Strength, for good."),
        new(2, "full", "Full Moon", "crown_of_ur", "Crown of Ur: 45. The first card you play this round is Counted and returns free at the next Full Moon."),
        new(3, "waning", "Waning", "pour_away_the_light", "He takes double damage. Pour Away the Light: 3x8."),
    ];

    public static string PhaseId(string key) => $"nanna_phase_{key}";
    public static string NextId(string key) => $"nanna_next_{key}";

    private const int WholeDeck = 256;

    private static ICombatantTargetSelector Him => Bearer(RuleId);

    private static EffectResultKey<OrderedTargetOutcomes<CreateCardInstanceOutcome>> Echoed => new("nanna_suen.echoed");

    // ── what the act hands to the rest of the converter ───────────────────────────────────────────────────

    public static IEnumerable<StatusData> Statuses()
    {
        var him = Him;
        var self = CombatantTargetSelectors.Source;
        var phase = new RemainderExpression<TurnStartedTriggeredEffectContext>(
            Minus(Round<TurnStartedTriggeredEffectContext>(), N<TurnStartedTriggeredEffectContext>(1)), N<TurnStartedTriggeredEffectContext>(Ring.Count));

        // Your turn opens: the moon is in tonight's phase, the next one is announced; at the Full Moon the Counted
        // card comes back.
        var iterated = new IteratedCardExpression<TurnStartedTriggeredEffectContext>();
        var copy = new CreateCardOutcomeExpression<TurnStartedTriggeredEffectContext>(Echoed);
        IEffectNode<TurnStartedTriggeredEffectContext> Return(CardZone zone) =>
            new ForEachCardInZoneNode<TurnStartedTriggeredEffectContext>(Applicant, zone, Do(
                new CreateCardCopyNode<TurnStartedTriggeredEffectContext>(Applicant, iterated, CardZone.Hand,
                    N<TurnStartedTriggeredEffectContext>(1), Echoed, temporary: true),
                new MarkCardInstanceNode<TurnStartedTriggeredEffectContext>(Applicant, copy, new TagId(EchoMark)),
                new SetCardInstanceMarkCounterNode<TurnStartedTriggeredEffectContext>(Applicant, copy, StandardCombatIds.CardCostDeltaCounter,
                    new NegateExpression<TurnStartedTriggeredEffectContext>(
                        new CardInstanceBaseCostExpression<TurnStartedTriggeredEffectContext>(copy, StandardCombatIds.EnergyResource)),
                    relative: false),
                new MarkCardInstanceNode<TurnStartedTriggeredEffectContext>(Applicant, iterated, new TagId(CountedMark), remove: true)),
                maxIterations: WholeDeck, markFilter: new TagId(CountedMark));
        var dusk = If(PlayersTurnAgainst<TurnStartedTriggeredEffectContext>(him), Do(
        [
            .. Ring.Select(p => Unmark<TurnStartedTriggeredEffectContext>(him, PhaseId(p.Key))),
            .. Ring.Select(p => Unmark<TurnStartedTriggeredEffectContext>(him, NextId(p.Key))),
            .. Ring.Select(p => If(Eq(phase, p.Index), Do(
                Mark<TurnStartedTriggeredEffectContext>(him, PhaseId(p.Key)),
                Mark<TurnStartedTriggeredEffectContext>(him, NextId(Ring[(p.Index + 1) % Ring.Count].Key))))),
            If(Eq(phase, 2), Do(Return(CardZone.Hand), Return(CardZone.DrawPile), Return(CardZone.DiscardPile), Return(CardZone.ExhaustPile))),
        ]));

        // The first card you play under the Full Moon is Counted.
        var played = new TriggerEventCardInstanceExpression<CardPlayedTriggeredEffectContext>();
        var count = If(AllOf(
                PlayersTurnAgainst<CardPlayedTriggeredEffectContext>(him),
                Wears<CardPlayedTriggeredEffectContext>(him, PhaseId("full")),
                Eq(new CardsPlayedThisTurnExpression<CardPlayedTriggeredEffectContext>(Applicant), 1),
                Not(new CardInstanceHasMarkExpression<CardPlayedTriggeredEffectContext>(played, new TagId(EchoMark)))),
            new MarkCardInstanceNode<CardPlayedTriggeredEffectContext>(Applicant, played, new TagId(CountedMark)));

        // Your turn ends: an echo belongs to its night, and leaves (Ethereal).
        IEffectNode<TurnEndedTriggeredEffectContext> Sweep(CardZone zone) =>
            new ForEachCardInZoneNode<TurnEndedTriggeredEffectContext>(Applicant, zone,
                new MoveCardToZoneNode<TurnEndedTriggeredEffectContext>(Applicant,
                    new IteratedCardExpression<TurnEndedTriggeredEffectContext>(), CardZone.ExhaustPile),
                maxIterations: WholeDeck, markFilter: new TagId(EchoMark));
        var nightEnds = If(PlayersTurnAgainst<TurnEndedTriggeredEffectContext>(him), Do(Sweep(CardZone.Hand), Sweep(CardZone.DiscardPile)));

        // His turn ends on the Waning: an orbit is complete.
        var orbit = If(Wears<TurnEndedTriggeredEffectContext>(self, PhaseId("waning")), Mark<TurnEndedTriggeredEffectContext>(self, RadianceId));

        yield return Face(RuleId, "The Counted Moon",
            "One phase a round in a fixed ring: New Moon, Waxing, Full Moon, Waning. Each has one move and one rule. Every orbit he grows brighter.",
            triggers:
            [
                On("TurnStarted", dusk),
                On("CardPlayed", count),
                On("TurnEnded", nightEnds),
                Own("TurnEnded", orbit),
            ]);
        foreach (var p in Ring)
        {
            var face = Face(PhaseId(p.Key), $"Now: {p.Name}", p.Rule);
            yield return p.Key switch
            {
                "new" => face with { PassiveModifiers = [Modifier(PassiveModifierPipeline.DamageReceived, PassiveModifierOperation.ScalePercent, NewMoonPercent)] },
                "waning" => face with { PassiveModifiers = [Modifier(PassiveModifierPipeline.DamageReceived, PassiveModifierOperation.ScalePercent, WaningPercent)] },
                _ => face,
            };
            yield return Face(NextId(p.Key), $"Next: {p.Name}", $"Next round the moon is {p.Name}. {p.Rule}");
        }
        yield return Face(RadianceId, "Radiance", "One for every orbit he has completed: each makes his damage and Block 15 % greater, for good.",
            StatusPolarity.Buff, stacks: true,
            modifiers:
            [
                Modifier(PassiveModifierPipeline.DamageDealt, PassiveModifierOperation.AddPercentPerStack, RadiancePercent, DamageKind.Direct),
                Modifier(PassiveModifierPipeline.BlockGain, PassiveModifierOperation.AddPercentPerStack, RadiancePercent),
            ]);
    }

    public static IEnumerable<CardData> Cards()
    {
        yield return new CardData
        {
            Id = MoonlessCardId,
            NameKey = "Moonless",
            DescriptionKey = "Unplayable. Ethereal: at the end of your turn it is exhausted.",
            Costs = [],
            Tags = [new TagId(BnbContent.Converter.Cards.CardAuthoring.UnplayableTag), new TagId(BnbContent.Converter.Cards.CardAuthoring.JunkTag), new TagId(MoonlessCardId)],
            Program = new EffectProgram<CardPlayContext>(Nothing<CardPlayContext>()),
            TurnEndHandDestinationZone = CardZone.ExhaustPile,
        };
    }

    // ── his moves ─────────────────────────────────────────────────────────────────────────────────────────

    public static EffectProgram<EnemyActionContext>? Intent(string enemyId, string intentId)
    {
        if (enemyId != EnemyId)
            return null;
        var self = CombatantTargetSelectors.Source;
        return intentId switch
        {
            "count_the_unseen" => new(Do(Guard<EnemyActionContext>(self, NewMoonBlock), Give<EnemyActionContext>(MoonlessCardId, CardZone.DrawPile, Moonless))),
            "raise_the_horns" => new(Do([.. Enumerable.Repeat(HitPlayer<EnemyActionContext>(HornDamage), HornHits),
                Mark<EnemyActionContext>(self, "strength", HornStrength)])),
            "crown_of_ur" => new(HitPlayer<EnemyActionContext>(CrownDamage)),
            "pour_away_the_light" => new(Do([.. Enumerable.Repeat(HitPlayer<EnemyActionContext>(PourDamage), PourHits)])),
            _ => null,
        };
    }
}
