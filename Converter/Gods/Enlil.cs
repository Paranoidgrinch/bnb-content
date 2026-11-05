using RogueDeck.Core.Combat;
using RogueDeck.Run;
using RogueDeck.Sandbox.Composition;
using RogueDeck.Scenario.Authoring;
using static BnbContent.Converter.Gods.GodShapes;

namespace BnbContent.Converter.Gods;

// G6 — ENLIL, LORD OF THE WIND, WHOSE WORD CANNOT BE UNSPOKEN (rework of 2026-11-05).
//
// A visible ring of eight decrees. Exactly one is in force each round; the ring turns one step as his turn ends, and
// the next decree is always announced on him. Every decree binds both sides — both wear it — and nothing removes,
// shortens or changes one. The Noise of Mankind (60 %): every card you play is a Noise; end your turn on 12 and he
// sends Namtar's Plague. The Flood (30 %): the ring stops, the Noise ends, and the water rises a step every round,
// drowning both of you for water x 6 as his turn ends; Atrahasis' Boat halves your share.
public static class Enlil
{
    public const string EnemyId = "enlil_voice_of_the_unalterable_decree";
    public const string EncounterId = "act_5_enlil_voice_of_the_unalterable_decree";

    public const string RuleId = "enlil_the_ring_of_decrees";
    public const string NoiseOfMankindId = "enlil_the_noise_of_mankind";
    public const string NoiseId = "enlil_noise";
    public const string PlagueSentId = "enlil_namtars_plague";
    public const string FloodId = "enlil_the_flood";
    public const string WaterId = "enlil_water";
    public const string InTheBoatId = "enlil_in_the_boat";
    public const string BoatCardId = "atrahasis_boat";

    public const int MaxHealth = 1100;
    public const int WallCap = 20;
    public const int FourthWork = 4;
    public const int LeastPrice = 1;
    public const int ReturnPercent = 25;
    public const int HalfPercent = 50;
    public const int HandOfFour = 4;
    public const int NoisePercent = 60;
    public const int FloodPercent = 30;
    public const int NoiseLimit = 12;
    public const int PlagueDamage = 25;
    public const int PlagueWeak = 2;
    public const int PlagueFrail = 2;
    public const int FloodPerWater = 6;
    public const int BoatCost = 2;

    public const int CommandDamage = 20;
    public const int WindHits = 4;
    public const int WindDamage = 8;
    public const int EkurBlock = 40;
    public const int KingshipDamage = 35;

    public sealed record Decree(int Index, string Key, string Name, string Text);

    public static readonly IReadOnlyList<Decree> Ring =
    [
        new(0, "wall", "No Wall Shall Rise Above Twenty", "Block is capped at 20, for you and for him."),
        new(1, "fourth", "The Fourth Work Shall Be the Last", "You can play at most 4 cards this round."),
        new(2, "price", "No Word Shall Be Without Price", "No card costs less than 1 this round."),
        new(3, "blow", "The Blow Returns to the Striker", "Whoever deals damage loses 25 % of it as HP, you and him."),
        new(4, "half", "Half Shall Be the Measure of Every Wound", "All damage is halved, yours and his."),
        new(5, "wound", "No Wound Shall Close", "No healing, for you or for him."),
        new(6, "first", "The First Work Shall Be Done Twice", "Your first card this turn is played twice, and his first hit lands twice."),
        new(7, "hand", "The Hand Shall Hold Four", "You draw only 4 cards."),
    ];

    public static string DecreeId(string key) => $"enlil_decree_{key}";
    public static string NextId(string key) => $"enlil_next_{key}";

    // On him: which step of the ring is in force (0–7).
    public static CounterId RingStep => new("enlil_ring_step");

    private static ICombatantTargetSelector Him => Bearer(RuleId);

    // Both sides, for a decree that binds both.
    private static ICombatantTargetSelector Both { get; } = CombatantTargetSelectors.Union(Applicant, Bearer(RuleId));

    // ── what the act hands to the rest of the converter ───────────────────────────────────────────────────

    // The first decree is in force from the first bell, on you as on him.
    public static IReadOnlyList<StartingStatusSpec> HeroOpening() => [new StartingStatusSpec(new StatusDefinitionId(DecreeId(Ring[0].Key)), 1)];

    public static IEnumerable<StatusData> Statuses()
    {
        var him = Him;
        var self = CombatantTargetSelectors.Source;
        var step = CounterOf<TurnEndedTriggeredEffectContext>(self, RingStep);

        // His turn ends: in the Flood the water rises and drowns you both; otherwise the ring turns a step.
        IEffectNode<TurnEndedTriggeredEffectContext> Proclaim(ICombatExpression<TurnEndedTriggeredEffectContext, int> at) => Do(
        [
            .. Ring.Select(d => Unmark<TurnEndedTriggeredEffectContext>(Both, DecreeId(d.Key))),
            .. Ring.Select(d => Unmark<TurnEndedTriggeredEffectContext>(self, NextId(d.Key))),
            .. Ring.Select(d => If(Eq(at, d.Index), Do(
                new ForEachTargetEffectNode<TurnEndedTriggeredEffectContext>(Both,
                    Mark<TurnEndedTriggeredEffectContext>(CombatantTargetSelectors.IterationTarget, DecreeId(d.Key))),
                Mark<TurnEndedTriggeredEffectContext>(self, NextId(Ring[(d.Index + 1) % Ring.Count].Key))))),
        ]);
        var water = StacksOn<TurnEndedTriggeredEffectContext>(self, WaterId);
        // In the Boat you take half: water x 6, less water x 3 while you wear it.
        var yourShare = Minus(Times(water, FloodPerWater),
            new MultiplyExpression<TurnEndedTriggeredEffectContext>(Times(water, FloodPerWater / 2),
                Min(N<TurnEndedTriggeredEffectContext>(1), StacksOn<TurnEndedTriggeredEffectContext>(Applicant, InTheBoatId))));
        var dusk = If(Wears<TurnEndedTriggeredEffectContext>(self, FloodId),
            Do(
                Mark<TurnEndedTriggeredEffectContext>(self, WaterId),
                new DealDamageNode<TurnEndedTriggeredEffectContext>(Applicant, yourShare,
                    ignoresBlock: true, kind: DamageKind.DamageOverTime),
                new DealDamageNode<TurnEndedTriggeredEffectContext>(self, Times(water, FloodPerWater),
                    ignoresBlock: true, kind: DamageKind.DamageOverTime),
                Unmark<TurnEndedTriggeredEffectContext>(Applicant, InTheBoatId)),
            Do(
                Counter(self, RingStep, new RemainderExpression<TurnEndedTriggeredEffectContext>(
                    Plus(step, N<TurnEndedTriggeredEffectContext>(1)), N<TurnEndedTriggeredEffectContext>(Ring.Count))),
                Proclaim(CounterOf<TurnEndedTriggeredEffectContext>(self, RingStep))));

        // The Noise of Mankind: every card you play is a Noise; at 12 as your turn ends the Plague is sent, and it
        // comes at the start of his turn.
        var noise = If(AllOf(PlayersTurnAgainst<CardPlayedTriggeredEffectContext>(him),
                Wears<CardPlayedTriggeredEffectContext>(him, NoiseOfMankindId), Not(Wears<CardPlayedTriggeredEffectContext>(him, FloodId))),
            Mark<CardPlayedTriggeredEffectContext>(him, NoiseId));
        var silence = If(AllOf(PlayersTurnAgainst<TurnEndedTriggeredEffectContext>(him),
                Ge(StacksOn<TurnEndedTriggeredEffectContext>(him, NoiseId), NoiseLimit)),
            Do(Unmark<TurnEndedTriggeredEffectContext>(him, NoiseId), Mark<TurnEndedTriggeredEffectContext>(him, PlagueSentId)));
        var plague = If(Wears<TurnStartedTriggeredEffectContext>(self, PlagueSentId), Do(
            Unmark<TurnStartedTriggeredEffectContext>(self, PlagueSentId),
            HitPlayer<TurnStartedTriggeredEffectContext>(PlagueDamage),
            Afflict<TurnStartedTriggeredEffectContext>(WeakId, PlagueWeak),
            Afflict<TurnStartedTriggeredEffectContext>(FrailId, PlagueFrail)));

        var hurt = CombatantTargetSelectors.EventTarget;
        var phases = Do(
            If(AllOf(Not(Wears<DamageReceivedTriggeredEffectContext>(hurt, NoiseOfMankindId)), AtOrBelow<DamageReceivedTriggeredEffectContext>(hurt, NoisePercent)),
                Mark<DamageReceivedTriggeredEffectContext>(hurt, NoiseOfMankindId)),
            If(AllOf(Not(Wears<DamageReceivedTriggeredEffectContext>(hurt, FloodId)), AtOrBelow<DamageReceivedTriggeredEffectContext>(hurt, FloodPercent)),
                Do(
                    Mark<DamageReceivedTriggeredEffectContext>(hurt, FloodId),
                    Unmark<DamageReceivedTriggeredEffectContext>(hurt, NoiseId),
                    Unmark<DamageReceivedTriggeredEffectContext>(hurt, PlagueSentId),
                    Do([.. Ring.Select(d => Unmark<DamageReceivedTriggeredEffectContext>(hurt, NextId(d.Key)))]),
                    Give<DamageReceivedTriggeredEffectContext>(BoatCardId, CardZone.Hand))));

        yield return Face(RuleId, "The Ring of Decrees",
            "One decree a round binds you both; the ring turns a step as his turn ends, and the next is shown. Nothing removes a decree.",
            triggers:
            [
                Own("TurnEnded", dusk),
                Own("TurnStarted", plague),
                On("CardPlayed", noise),
                On("TurnEnded", silence),
                Own("DamageTaken", phases),
            ]);
        foreach (var d in Ring)
        {
            yield return DecreeFace(d);
            yield return Face(NextId(d.Key), $"Next: {d.Name}", $"The decree of the next round. {d.Text}");
        }
        yield return Face(NoiseOfMankindId, "The Noise of Mankind",
            "From 60 %: every card you play is a Noise. End your turn on 12 and he sends Namtar's Plague: 25, 2 Weak, 2 Frail.");
        yield return Face(NoiseId, "Noise", "The cards you played this turn and before. At 12 as your turn ends he sends Namtar's Plague, and it is silent again.",
            stacks: true);
        yield return Face(PlagueSentId, "Namtar's Plague", "At the start of his turn: 25 damage to you, 2 Weak and 2 Frail.", StatusPolarity.Debuff);
        yield return Face(FloodId, "The Flood",
            "From 30 % the ring stops on the decree in force and the Noise ends. Every round the water rises; it drowns you both as his turn ends.");
        yield return Face(WaterId, "Water", "As his turn ends the water rises a step, then you and he each take water x 6. Block does not apply.",
            stacks: true);
        yield return Face(InTheBoatId, "In the Boat", "This round the flood deals you half.", StatusPolarity.Buff);
    }

    private static StatusData DecreeFace(Decree d)
    {
        var self = CombatantTargetSelectors.Source;
        var gainer = CombatantTargetSelectors.EventTarget;
        var face = Face(DecreeId(d.Key), d.Name, d.Text);
        return d.Key switch
        {
            // a gain past 20 is cut back to 20
            "wall" => face with
            {
                Triggers = [Own("BlockGained", If(Above(BlockOf<BlockGainedTriggeredEffectContext>(gainer), WallCap),
                    new ModifyDefensivePoolNode<BlockGainedTriggeredEffectContext>(gainer, StandardCombatIds.BlockDefensivePool,
                        Minus(N<BlockGainedTriggeredEffectContext>(WallCap), BlockOf<BlockGainedTriggeredEffectContext>(gainer)))))],
            },
            "fourth" => face with { CombatRules = [new CombatRuleData(CombatRule.MaxCardsPerTurn, FourthWork)] },
            "price" => face with { CombatRules = [new CombatRuleData(CombatRule.MinimumCardCost, LeastPrice)] },
            // the striker loses a quarter of the blow as HP (health lost is not damage, so it does not return again)
            "blow" => face with
            {
                Triggers = [Own("DamageTaken", If(Above(HitSize, 3),
                    LoseHealth(CombatantTargetSelectors.Source, Over(Times(HitSize, ReturnPercent), 100))))],
            },
            "half" => face with { PassiveModifiers = [Modifier(PassiveModifierPipeline.DamageReceived, PassiveModifierOperation.ScalePercent, HalfPercent)] },
            // what is healed is taken straight back
            "wound" => face with
            {
                Triggers = [Own("Healed", If(Above(new EventAmountExpression<HealedTriggeredEffectContext>(), 0),
                    LoseHealth(CombatantTargetSelectors.EventTarget, new EventAmountExpression<HealedTriggeredEffectContext>())))],
            },
            "first" => face with
            {
                Triggers = [Own("CardPlayed", If(AllOf(IsPlayersTurn<CardPlayedTriggeredEffectContext>(),
                        Eq(new CardsPlayedThisTurnExpression<CardPlayedTriggeredEffectContext>(self), 1)),
                    new ReplayCardProgramNode<CardPlayedTriggeredEffectContext>(
                        new TriggerEventCardInstanceExpression<CardPlayedTriggeredEffectContext>(), Bearer(RuleId))))],
            },
            "hand" => face with { PassiveModifiers = [Modifier(PassiveModifierPipeline.TurnStartDraw, PassiveModifierOperation.ClampMax, HandOfFour)] },
            _ => face,
        };
    }

    public static IEnumerable<CardData> Cards()
    {
        var you = CombatantTargetSelectors.Source;
        yield return GodCard(BoatCardId, "Atrahasis' Boat",
            "Retain. This round the flood deals you half. Returns to your hand.",
            Mark<CardPlayContext>(you, InTheBoatId), cost: BoatCost, retain: true, returns: true);
    }

    // ── his moves ─────────────────────────────────────────────────────────────────────────────────────────

    public static EffectProgram<EnemyActionContext>? Intent(string enemyId, string intentId)
    {
        if (enemyId != EnemyId)
            return null;
        var self = CombatantTargetSelectors.Source;
        // Under the seventh decree his first hit lands twice.
        IEffectNode<EnemyActionContext> First(int damage) =>
            If(Wears<EnemyActionContext>(self, DecreeId("first")),
                Do(HitPlayer<EnemyActionContext>(damage), HitPlayer<EnemyActionContext>(damage)),
                HitPlayer<EnemyActionContext>(damage));
        return intentId switch
        {
            "the_command_goes_forth" => new(First(CommandDamage)),
            "wind_over_the_plain" => new(Do([First(WindDamage), .. Enumerable.Repeat(HitPlayer<EnemyActionContext>(WindDamage), WindHits - 1)])),
            "raise_the_e_kur" => new(Guard<EnemyActionContext>(self, EkurBlock)),
            "weight_of_kingship" => new(First(KingshipDamage)),
            _ => null,
        };
    }
}
