using RogueDeck.Core.Combat;
using RogueDeck.Sandbox.Composition;
using RogueDeck.Scenario.Authoring;
using static BnbContent.Converter.Gods.GodShapes;

namespace BnbContent.Converter.Gods;

// G5 — UTU-SHAMASH, JUDGE OF HEAVEN AND EARTH, WITNESS OF EVERY OATH (rework of 2026-11-05). He sees everything and
// returns it.
//
// The Oath: three free oath cards in hand each turn (20, 40, 60; Ethereal; one sworn removes the others), the Favor
// paid at once as Block (10, 20, 30). End the turn having dealt him less than you swore and he gains a Witness:
// +10 % to everything he does, for good. Chips on him show the oath sworn and the damage dealt to him so far.
// Retribution: the Saw of Judgment, every other move, deals 15 + 50 % of what you dealt him the turn before.
// No Shadow (60 %): 75 %, and a turn without an oath is witnessed too. Judge of the Dead (30 %): no more oaths, each
// Witness counts 20 %, and the Saw returns all of it.
public static class Utu
{
    public const string EnemyId = "utu_witness_of_every_oath";
    public const string EncounterId = "act_5_utu_witness_of_every_oath";

    public const string RuleId = "utu_the_oath_and_the_saw";
    public const string SwornId = "utu_sworn";
    public const string DealtId = "utu_dealt_to_him";
    public const string WitnessId = "utu_witness";
    public const string DeadWitnessId = "utu_witness_of_the_dead";
    public const string NoShadowId = "utu_no_shadow";
    public const string JudgeOfTheDeadId = "utu_judge_of_the_dead";
    public const string OathTag = "utu_oath";

    public const int MaxHealth = 950;
    public const int SawBase = 15;
    public const int SawPercent = 50;
    public const int NoShadowSawPercent = 75;
    public const int DeadSawPercent = 100;
    public const int WitnessPercent = 10;
    public const int DeadWitnessPercent = 20;
    public const int NoShadowPercentHealth = 60;
    public const int DeadPercentHealth = 30;

    public const int RaysDamage = 18;
    public const int RiseBlock = 30;
    public const int RiseStrength = 1;
    public const int DiskHits = 4;
    public const int DiskDamage = 7;

    public static readonly IReadOnlyList<(int Sworn, int Favor)> Oaths = [(20, 10), (40, 20), (60, 30)];

    public static string OathCardId(int sworn) => $"swear_by_the_sun_{sworn}";

    // On him: 1 while it is your turn, so only what you deal on your own turn is counted.
    private static CounterId YourTurn => new("utu_your_turn");

    private static ICombatantTargetSelector Him => Bearer(RuleId);

    // ── what the act hands to the rest of the converter ───────────────────────────────────────────────────

    public static IEnumerable<StatusData> Statuses()
    {
        var him = Him;
        var self = CombatantTargetSelectors.Source;
        var dead = Wears<TurnStartedTriggeredEffectContext>(him, JudgeOfTheDeadId);

        // Your turn opens: the slate is wiped, and (until he judges the dead) the three oaths are in your hand.
        var morning = If(PlayersTurnAgainst<TurnStartedTriggeredEffectContext>(him), Do(
            Unmark<TurnStartedTriggeredEffectContext>(him, SwornId),
            Unmark<TurnStartedTriggeredEffectContext>(him, DealtId),
            Counter(him, YourTurn, N<TurnStartedTriggeredEffectContext>(1)),
            If(Not(dead), Do([.. Oaths.Select(o => Give<TurnStartedTriggeredEffectContext>(OathCardId(o.Sworn), CardZone.Hand))]))));

        // A blow on your turn is counted on him.
        var hurt = CombatantTargetSelectors.EventTarget;
        var counted = If(AllOf(Eq(CounterOf<DamageReceivedTriggeredEffectContext>(hurt, YourTurn), 1), Above(HitSize, 0)),
            MarkBy(hurt, DealtId, HitSize));

        // Your turn ends: the oath is judged. In No Shadow a turn sworn to nothing is witnessed too.
        var sworn = StacksOn<TurnEndedTriggeredEffectContext>(him, SwornId);
        var dealt = StacksOn<TurnEndedTriggeredEffectContext>(him, DealtId);
        var judged = If(PlayersTurnAgainst<TurnEndedTriggeredEffectContext>(him), Do(
            Counter(him, YourTurn, N<TurnEndedTriggeredEffectContext>(0)),
            If(Not(Wears<TurnEndedTriggeredEffectContext>(him, JudgeOfTheDeadId)),
                If(Above(sworn, 0),
                    If(Less(dealt, sworn), Mark<TurnEndedTriggeredEffectContext>(him, WitnessId)),
                    If(Wears<TurnEndedTriggeredEffectContext>(him, NoShadowId), Mark<TurnEndedTriggeredEffectContext>(him, WitnessId))))));

        // His health: No Shadow at 60 %, the Judge of the Dead at 30 % (no more oaths; each Witness now counts 20 %).
        var phases = Do(
            If(AllOf(Not(Wears<DamageReceivedTriggeredEffectContext>(hurt, NoShadowId)), AtOrBelow<DamageReceivedTriggeredEffectContext>(hurt, NoShadowPercentHealth)),
                Mark<DamageReceivedTriggeredEffectContext>(hurt, NoShadowId)),
            If(AllOf(Not(Wears<DamageReceivedTriggeredEffectContext>(hurt, JudgeOfTheDeadId)), AtOrBelow<DamageReceivedTriggeredEffectContext>(hurt, DeadPercentHealth)),
                Do(
                    Mark<DamageReceivedTriggeredEffectContext>(hurt, JudgeOfTheDeadId),
                    If(Above(StacksOn<DamageReceivedTriggeredEffectContext>(hurt, WitnessId), 0),
                        MarkBy(hurt, DeadWitnessId, StacksOn<DamageReceivedTriggeredEffectContext>(hurt, WitnessId))),
                    Unmark<DamageReceivedTriggeredEffectContext>(hurt, WitnessId),
                    new ForEachCardInZoneNode<DamageReceivedTriggeredEffectContext>(Applicant, CardZone.Hand,
                        new MoveCardToZoneNode<DamageReceivedTriggeredEffectContext>(Applicant,
                            new IteratedCardExpression<DamageReceivedTriggeredEffectContext>(), CardZone.ExhaustPile),
                        tagFilter: new TagId(OathTag)))));

        yield return Face(RuleId, "The Oath and the Saw",
            "Swear an oath each turn; deal him less than you swore and he gains a Witness. His Saw returns half of what you dealt him.",
            triggers:
            [
                On("TurnStarted", morning),
                Own("DamageTaken", counted),
                Own("DamageTaken", phases),
                On("TurnEnded", judged),
            ]);
        yield return Face(SwornId, "Sworn", "The oath you swore this turn. Deal him less by the end of it and he gains a Witness.", stacks: true);
        yield return Face(DealtId, "Dealt to Him", "The damage you have dealt him this turn. His Saw of Judgment returns part of it.", stacks: true);
        yield return Face(WitnessId, "Witness", "Every oath he saw broken: each makes his damage and Block 10 % greater, for good.",
            StatusPolarity.Buff, stacks: true,
            modifiers:
            [
                Modifier(PassiveModifierPipeline.DamageDealt, PassiveModifierOperation.AddPercentPerStack, WitnessPercent, DamageKind.Direct),
                Modifier(PassiveModifierPipeline.BlockGain, PassiveModifierOperation.AddPercentPerStack, WitnessPercent),
            ]);
        yield return Face(DeadWitnessId, "Witness Below", "His Witness in the underworld: each makes his damage and Block 20 % greater, for good.",
            StatusPolarity.Buff, stacks: true,
            modifiers:
            [
                Modifier(PassiveModifierPipeline.DamageDealt, PassiveModifierOperation.AddPercentPerStack, DeadWitnessPercent, DamageKind.Direct),
                Modifier(PassiveModifierPipeline.BlockGain, PassiveModifierOperation.AddPercentPerStack, DeadWitnessPercent),
            ]);
        yield return Face(NoShadowId, "No Shadow", "From 60 %: the Saw returns 75 % of what you dealt him, and a turn without an oath is witnessed too.");
        yield return Face(JudgeOfTheDeadId, "Judge of the Dead",
            "From 30 % the sun is below: no more oaths, each Witness counts 20 %, and the Saw returns all you dealt him.");
    }

    public static IEnumerable<CardData> Cards()
    {
        var you = CombatantTargetSelectors.Source;
        foreach (var (sworn, favor) in Oaths)
            yield return GodCard(OathCardId(sworn), $"Swear by the Sun: {sworn}",
                $"Ethereal. Gain {favor} Block now. Swear to deal Utu {sworn} damage this turn, or he gains a Witness. The other oaths are gone.",
                Do(
                    new GainBlockNode<CardPlayContext>(you, N<CardPlayContext>(favor)),
                    SetStacks(Him, SwornId, N<CardPlayContext>(sworn)),
                    new ForEachCardInZoneNode<CardPlayContext>(you, CardZone.Hand,
                        new MoveCardToZoneNode<CardPlayContext>(you, new IteratedCardExpression<CardPlayContext>(), CardZone.ExhaustPile),
                        tagFilter: new TagId(OathTag))),
                tags: OathTag);
    }

    // ── his moves ─────────────────────────────────────────────────────────────────────────────────────────

    public static EffectProgram<EnemyActionContext>? Intent(string enemyId, string intentId)
    {
        if (enemyId != EnemyId)
            return null;
        var self = CombatantTargetSelectors.Source;
        // 15 + the share of what you dealt him: 50, 75 under No Shadow, all of it below.
        var dealt = StacksOn<EnemyActionContext>(self, DealtId);
        var share = Plus(Plus(N<EnemyActionContext>(SawPercent),
                Times(StacksOn<EnemyActionContext>(self, NoShadowId), NoShadowSawPercent - SawPercent)),
            Times(StacksOn<EnemyActionContext>(self, JudgeOfTheDeadId), DeadSawPercent - NoShadowSawPercent));
        return intentId switch
        {
            // three slots of his six-move cycle (every other move) are the Saw
            "the_saw_of_judgment" or "the_saw_of_judgment_2" or "the_saw_of_judgment_3" => new(HitPlayer(Plus(N<EnemyActionContext>(SawBase),
                Over(new MultiplyExpression<EnemyActionContext>(dealt, share), 100)))),
            "rays_that_lay_bare" => new(Do(ClearBlock<EnemyActionContext>(Applicant), HitPlayer<EnemyActionContext>(RaysDamage))),
            "rise_between_the_mountains" => new(Do(Guard<EnemyActionContext>(self, RiseBlock), Mark<EnemyActionContext>(self, "strength", RiseStrength))),
            "the_sun_disk" => new(Do([.. Enumerable.Repeat(HitPlayer<EnemyActionContext>(DiskDamage), DiskHits)])),
            _ => null,
        };
    }
}
