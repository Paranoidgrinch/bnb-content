using RogueDeck.Core.Combat;
using RogueDeck.Sandbox.Composition;
using RogueDeck.Scenario.Authoring;
using static BnbContent.Converter.Gods.GodShapes;

namespace BnbContent.Converter.Gods;

// G2 — INANNA, QUEEN OF HEAVEN, WHO DESCENDED TO THE GREAT BELOW (rework of 2026-11-05). The seven gates of the
// Descent.
//
// She comes down wearing the seven regalia of the myth, each a status of its own on her. Every 130 HP she loses she
// passes a gate, and YOU choose which of the regalia she leaves there: a prompt names each one that is left and what
// it does. A blow that takes her past several gates asks once for each. With the seventh gone she is the Queen of
// Battle: every move twice, and 3 Strength more each turn.
//
// Her cycle opens with Splendor and Lioness is 20 (2026-11-08): with every regalium on, a 32 Lioness as her first
// move landed 37 twice, and the bot's Lieutenant died in two rounds in 12 fights of 12.
public static class Inanna
{
    public const string EnemyId = "inanna_mistress_of_the_eanna_ledger";
    public const string EncounterId = "act_5_inanna_mistress_of_the_eanna_ledger";

    public const string RuleId = "inanna_the_seven_gates";
    public const string GatesPassedId = "inanna_gates_passed";
    public const string QueenOfBattleId = "inanna_queen_of_battle";
    public const string InfatuationId = "inanna_infatuation";

    public const int MaxHealth = 1050;
    public const int HealthPerGate = 130;
    public const int CrownStrength = 5;
    public const int RodBlock = 25;
    public const int NecklaceHeal = 15;
    public const int RobePercent = 75;
    public const int RingWeak = 1;
    public const int Thorns = 4;
    public const int QueenStrength = 3;

    public const int LionessDamage = 20;
    public const int ArrowHits = 5;
    public const int ArrowDamage = 6;
    public const int SplendorBlock = 35;
    public const int SplendorStrength = 2;
    public const int InfatuationTurns = 2;
    public const int InfatuationPercent = 75;
    public const int InfatuationStrength = 2;

    public sealed record Regalium(string Key, string Name, string Effect);

    // In the order of the myth's gates.
    public static readonly IReadOnlyList<Regalium> Regalia =
    [
        new("crown", "Shugurra, Crown of the Steppe", "+5 Strength: her attacks deal 5 more."),
        new("rod", "Rod and Line of Lapis", "+25 Block at the end of each of her turns."),
        new("necklace", "Necklace of Small Lapis Beads", "She heals 15 at the start of each of her turns."),
        new("beads", "Twin Egg-Shaped Beads", "Her first attack each turn hits twice."),
        new("robe", "Pala, Robe of Ladyship", "She takes 25 % less damage."),
        new("ring", "Golden Ring", "You gain 1 Weak at the start of each of your turns."),
        new("breastplate", "Breastplate \"Come, Man, Come\"", "Thorns 4: each of your hits that wounds her deals 4 damage to you."),
    ];

    public static string RegaliumId(string key) => $"inanna_regalium_{key}";

    public static string OptionLabel(Regalium r) => $"{r.Name} — {r.Effect}";

    public const string GatePurpose = "Inanna passes a gate. Which of her regalia does she leave there?";

    private static ICombatantTargetSelector Her => Bearer(RuleId);

    private static ICombatExpression<TContext, int> Remaining<TContext>(ICombatantTargetSelector her) where TContext : class =>
        Regalia.Select(r => StacksOn<TContext>(her, RegaliumId(r.Key))).Aggregate(Plus);

    // ── what the act hands to the rest of the converter ───────────────────────────────────────────────────

    public static IEnumerable<StatusData> Statuses()
    {
        var her = Her;
        var self = CombatantTargetSelectors.Source;
        var hurt = CombatantTargetSelectors.EventTarget;

        // A blow: every 130 HP lost from her full health is a gate; each gate not yet passed asks its question.
        var lost = Minus(MaxHealthOf<DamageReceivedTriggeredEffectContext>(hurt), HealthOf<DamageReceivedTriggeredEffectContext>(hurt));
        var owed = Max(N<DamageReceivedTriggeredEffectContext>(0), Min(
            Minus(Over(lost, HealthPerGate), StacksOn<DamageReceivedTriggeredEffectContext>(hurt, GatesPassedId)),
            Remaining<DamageReceivedTriggeredEffectContext>(hurt)));
        var gates = Do(
            new RepeatEffectNode<DamageReceivedTriggeredEffectContext>(owed, Do(
                Mark<DamageReceivedTriggeredEffectContext>(hurt, GatesPassedId),
                TheGate(hurt, 0, []))),
            If(AllOf(Eq(Remaining<DamageReceivedTriggeredEffectContext>(hurt), 0),
                    Not(Wears<DamageReceivedTriggeredEffectContext>(hurt, QueenOfBattleId))),
                Mark<DamageReceivedTriggeredEffectContext>(hurt, QueenOfBattleId)));

        yield return Face(RuleId, "The Seven Gates",
            "Every 130 HP she loses she passes a gate, and you choose which of her regalia she leaves there. With all 7 gone she is the Queen of Battle.",
            triggers: [Own("DamageTaken", gates)]);
        yield return Face(GatesPassedId, "Gates Passed", "The gates of the Descent she has passed. One every 130 HP she loses; there are 7.",
            stacks: true);
        yield return Face(QueenOfBattleId, "Queen of Battle",
            "All 7 regalia are gone: every move of hers is made twice, and she gains 3 Strength at the end of each of her turns.",
            StatusPolarity.Buff,
            triggers: [Own("TurnEnded", Mark<TurnEndedTriggeredEffectContext>(self, "strength", QueenStrength))]);
        yield return Face(InfatuationId, "Infatuation", "Your attacks deal 25 % less. One stack wears off at the end of your turn.",
            StatusPolarity.Debuff, stacks: true,
            modifiers: [Modifier(PassiveModifierPipeline.DamageDealt, PassiveModifierOperation.ScalePercent, InfatuationPercent, DamageKind.Direct)],
            triggers: [Own("TurnEnded", AddStacks<TurnEndedTriggeredEffectContext>(self, InfatuationId, -1))]);

        foreach (var r in Regalia)
            yield return RegaliumFace(r);
    }

    private static StatusData RegaliumFace(Regalium r)
    {
        var self = CombatantTargetSelectors.Source;
        var face = Face(RegaliumId(r.Key), r.Name, r.Effect, StatusPolarity.Buff);
        return r.Key switch
        {
            "crown" => face with
            {
                PassiveModifiers = [Modifier(PassiveModifierPipeline.DamageDealt, PassiveModifierOperation.AddFlat, CrownStrength, DamageKind.Direct)],
            },
            "rod" => face with { Triggers = [Own("TurnEnded", Guard<TurnEndedTriggeredEffectContext>(self, RodBlock))] },
            "necklace" => face with
            {
                Triggers = [Own("TurnStarted", new HealNode<TurnStartedTriggeredEffectContext>(self, N<TurnStartedTriggeredEffectContext>(NecklaceHeal)))],
            },
            "robe" => face with
            {
                PassiveModifiers = [Modifier(PassiveModifierPipeline.DamageReceived, PassiveModifierOperation.ScalePercent, RobePercent)],
            },
            // Your turn opens: the ring's Weak.
            "ring" => face with
            {
                Triggers = [On("TurnStarted", If(PlayersTurnAgainst<TurnStartedTriggeredEffectContext>(Bearer(RegaliumId("ring"))),
                    Afflict<TurnStartedTriggeredEffectContext>(WeakId, RingWeak)))],
            },
            // Thorns, the engine's own: a hit that wounds her wounds its striker for the stacks (4).
            "breastplate" => face with { UsesStacks = true, Tags = [StandardCombatIds.TriggeredDamageTag.value] },
            _ => face,
        };
    }

    // The question at a gate. The engine's prompt carries fixed labels, so the tree walks which regalia she still
    // wears and asks, at its leaf, about exactly those.
    private static IEffectNode<DamageReceivedTriggeredEffectContext> TheGate(
        ICombatantTargetSelector her, int index, IReadOnlyList<Regalium> worn)
    {
        if (index == Regalia.Count)
            return worn.Count == 0
                ? Nothing<DamageReceivedTriggeredEffectContext>()
                : new ChooseOptionsNode<DamageReceivedTriggeredEffectContext>(
                    [.. worn.Select(r => Unmark<DamageReceivedTriggeredEffectContext>(her, RegaliumId(r.Key)))],
                    [.. worn.Select(OptionLabel)], 1, GatePurpose);
        var r = Regalia[index];
        return If(Wears<DamageReceivedTriggeredEffectContext>(her, RegaliumId(r.Key)),
            TheGate(her, index + 1, [.. worn, r]),
            TheGate(her, index + 1, worn));
    }

    // ── her moves ─────────────────────────────────────────────────────────────────────────────────────────

    public static EffectProgram<EnemyActionContext>? Intent(string enemyId, string intentId)
    {
        if (enemyId != EnemyId)
            return null;
        var self = CombatantTargetSelectors.Source;
        // The Twin Beads: the first attack of her turn lands twice.
        IEffectNode<EnemyActionContext> First(int damage) =>
            If(Wears<EnemyActionContext>(self, RegaliumId("beads")),
                Do(HitPlayer<EnemyActionContext>(damage), HitPlayer<EnemyActionContext>(damage)),
                HitPlayer<EnemyActionContext>(damage));
        IEffectNode<EnemyActionContext>? move = intentId switch
        {
            "lioness_of_battle" => First(LionessDamage),
            "arrows_of_the_morning_star" => Do([First(ArrowDamage), .. Enumerable.Repeat(HitPlayer<EnemyActionContext>(ArrowDamage), ArrowHits - 1)]),
            "splendor_of_eanna" => Do(Guard<EnemyActionContext>(self, SplendorBlock), Mark<EnemyActionContext>(self, "strength", SplendorStrength)),
            "infatuation" => Do(Afflict<EnemyActionContext>(InfatuationId, InfatuationTurns), Mark<EnemyActionContext>(self, "strength", InfatuationStrength)),
            _ => null,
        };
        // The Queen of Battle makes every move twice.
        return move is null ? null : new(If(Wears<EnemyActionContext>(self, QueenOfBattleId), Do(move, move), move));
    }
}
