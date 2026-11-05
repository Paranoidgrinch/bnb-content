using RogueDeck.Core.Combat;
using RogueDeck.Run;
using RogueDeck.Sandbox.Composition;
using RogueDeck.Scenario.Authoring;
using static BnbContent.Converter.Gods.GodShapes;

namespace BnbContent.Converter.Gods;

// G1 — NISABA, LADY OF THE REED STYLUS, WHO WRITES WHAT MUST COME (rework of 2026-11-05). What she writes happens.
//
// Each sentence stands on the table as a body of its own, with its health and its countdown on it. Break it before
// the countdown runs out and it is erased; let it run out and it comes true and leaves. At most three stand at once,
// and she writes one she has not already written. From 65 % she writes in lapis (two at a time, 90 HP); at 30 % she
// wipes the tablet and writes the Final Line, which kills you on the fourth dawn and keeps her alive at 1 HP while it
// stands. Break it and the reed turns: 100 to her, and she can die.
//
// The countdown ticks at the start of her turn, so a sentence written on her turn gives you two turns to break it.
public static class Nisaba
{
    public const string EnemyId = "nisaba_keeper_of_the_first_tablet";
    public const string EncounterId = "act_5_nisaba_keeper_of_the_first_tablet";
    public const string SentenceEnemyId = "nisaba_sentence";

    public const string RuleId = "nisaba_what_she_writes";
    public const string SentenceId = "nisaba_sentence";
    public const string ComesTrueId = "nisaba_comes_true_in";
    public const string LapisRecordId = "nisaba_the_lapis_record";
    public const string FinalLineWrittenId = "nisaba_the_final_line_is_written";
    public const string FinalLineId = "nisaba_the_final_line";
    public const string IndelibleId = "nisaba_indelible";
    public const string NotYetWrittenId = "nisaba_not_yet_written";

    // What the sentences leave on you.
    public const string MeasureWithheldId = "nisaba_measure_withheld";
    public const string HandHoldsTwoId = "nisaba_hand_holds_two";
    public const string GuardAsNothingId = "nisaba_guard_as_nothing";

    public const int MaxHealth = 950;
    public const int SentenceHealth = 60;
    public const int LapisSentenceHealth = 90;
    public const int SentenceCountdown = 2;
    public const int MostStanding = 3;
    public const int FinalLineHealth = 250;
    public const int FinalLineCountdown = 4;
    public const int ReedTurns = 100;
    public const int LapisPercent = 65;
    public const int FinalPercent = 30;

    public const int InscribeBlock = 15;
    public const int StrikeDamage = 26;
    public const int DryHealth = 30;
    public const int ReadPerSentence = 14;

    public const int BearForty = 40;
    public const int WithheldEnergy = 2;
    public const int HandSize = 2;
    public const int WorksBroken = 3;
    public const int KeeperHeal = 80;

    public sealed record Sentence(string Key, string Name, string Text);

    public static readonly IReadOnlyList<Sentence> Sentences =
    [
        new("forty", "The Body Shall Bear Forty", "When it comes true: 40 damage to you. Block does not apply."),
        new("measure", "The Measure Shall Be Withheld", "When it comes true: 2 Energy less on your next turn."),
        new("hand", "The Hand Shall Hold Two", "When it comes true: you draw only 2 cards on your next turn."),
        new("works", "The Works Shall Be Broken", "When it comes true: 3 random cards of your draw pile are exhausted for this fight."),
        new("guard", "The Guard Shall Be As Nothing", "When it comes true: your Block is removed, and you gain no Block on your next turn."),
        new("keeper", "The Keeper Shall Be Restored", "When it comes true: she heals 80."),
    ];

    public static string SentenceKindId(string key) => $"nisaba_sentence_{key}";

    public const string FinalLineName = "On the Fourth Dawn the Name of the Supplicant Shall Be Erased";

    // Her counters: the slot of her four-move cycle this round and how many sentences stand (read by her intent
    // rules), and the dice of the sentence she writes.
    public static CounterId Slot => new("nisaba_slot");
    public static CounterId StandingCount => new("nisaba_standing");
    private static CounterId Roll => new("nisaba_roll");
    private static CounterId Seen => new("nisaba_seen");

    private static ICombatantTargetSelector Her => Bearer(RuleId);

    private static ICombatExpression<TContext, int> Standing<TContext>() where TContext : class => CountOf<TContext>(SentenceId);

    private static IEffectNode<TContext> Recount<TContext>(ICombatantTargetSelector her) where TContext : class =>
        Counter(her, StandingCount, Standing<TContext>());

    private static IEffectNode<TContext> Leave<TContext>(ICombatantTargetSelector body) where TContext : class =>
        new SetCombatantLifecycleStateNode<TContext>(body, CombatantLifecycleState.Removed);

    // ── what the act hands to the rest of the converter ───────────────────────────────────────────────────

    public static IEnumerable<StatusData> Statuses()
    {
        var her = Her;
        var self = CombatantTargetSelectors.Source;
        var body = CombatantTargetSelectors.IterationTarget;

        // Your turn opens: the slot of her cycle is read off the round, and the measure withheld is taken.
        var morning = If(PlayersTurnAgainst<TurnStartedTriggeredEffectContext>(her), Do(
            Counter(her, Slot, new RemainderExpression<TurnStartedTriggeredEffectContext>(
                Minus(Round<TurnStartedTriggeredEffectContext>(), N<TurnStartedTriggeredEffectContext>(1)),
                N<TurnStartedTriggeredEffectContext>(4))),
            Recount<TurnStartedTriggeredEffectContext>(her)));

        // Her turn opens: every sentence comes one dawn nearer; one that reaches it comes true and leaves.
        IEffectNode<TurnStartedTriggeredEffectContext> ComesTrue(string key) => key switch
        {
            "forty" => HitPlayer<TurnStartedTriggeredEffectContext>(BearForty, ignoresBlock: true),
            "measure" => Afflict<TurnStartedTriggeredEffectContext>(MeasureWithheldId, WithheldEnergy),
            "hand" => Afflict<TurnStartedTriggeredEffectContext>(HandHoldsTwoId, 1),
            "works" => new RepeatEffectNode<TurnStartedTriggeredEffectContext>(N<TurnStartedTriggeredEffectContext>(WorksBroken),
                new MoveCardToZoneNode<TurnStartedTriggeredEffectContext>(Applicant,
                    new RandomCardInOwnerZoneExpression<TurnStartedTriggeredEffectContext>(Applicant, CardZone.DrawPile),
                    CardZone.ExhaustPile)),
            "guard" => Do(ClearBlock<TurnStartedTriggeredEffectContext>(Applicant),
                Afflict<TurnStartedTriggeredEffectContext>(GuardAsNothingId, 1)),
            _ => new HealNode<TurnStartedTriggeredEffectContext>(self, N<TurnStartedTriggeredEffectContext>(KeeperHeal)),
        };
        var dawn = Do(
            new ForEachTargetEffectNode<TurnStartedTriggeredEffectContext>(Living(SentenceId),
                CountDown(body, ComesTrueId, Do(
                [
                    .. Sentences.Select(s => If(Wears<TurnStartedTriggeredEffectContext>(body, SentenceKindId(s.Key)), ComesTrue(s.Key))),
                    Leave<TurnStartedTriggeredEffectContext>(body),
                ]))),
            // The Final Line: on the fourth dawn the name is erased.
            new ForEachTargetEffectNode<TurnStartedTriggeredEffectContext>(Living(FinalLineId),
                CountDown(body, ComesTrueId, new SetHealthNode<TurnStartedTriggeredEffectContext>(Applicant, N<TurnStartedTriggeredEffectContext>(0)))),
            Recount<TurnStartedTriggeredEffectContext>(self));

        // A blow takes her to 65 %, then to 30 %.
        var hurt = CombatantTargetSelectors.EventTarget;
        var phases = Do(
            If(AllOf(Not(Wears<DamageReceivedTriggeredEffectContext>(hurt, LapisRecordId)),
                    AtOrBelow<DamageReceivedTriggeredEffectContext>(hurt, LapisPercent)),
                Mark<DamageReceivedTriggeredEffectContext>(hurt, LapisRecordId)),
            If(AllOf(Not(Wears<DamageReceivedTriggeredEffectContext>(hurt, FinalLineWrittenId)),
                    AtOrBelow<DamageReceivedTriggeredEffectContext>(hurt, FinalPercent)),
                Do(
                    Mark<DamageReceivedTriggeredEffectContext>(hurt, FinalLineWrittenId),
                    Mark<DamageReceivedTriggeredEffectContext>(hurt, IndelibleId),
                    Unmark<DamageReceivedTriggeredEffectContext>(hurt, NotYetWrittenId),
                    new ForEachTargetEffectNode<DamageReceivedTriggeredEffectContext>(Living(SentenceId),
                        Leave<DamageReceivedTriggeredEffectContext>(body)),
                    new SummonCombatantNode<DamageReceivedTriggeredEffectContext>(
                        StandardCombatIds.EnemyTeam, N<DamageReceivedTriggeredEffectContext>(FinalLineHealth),
                        new CombatantDefinitionId(SentenceEnemyId), FinalLineName,
                        startingStatuses:
                        [
                            new StatusGrant(new StatusDefinitionId(FinalLineId), 1),
                            new StatusGrant(new StatusDefinitionId(ComesTrueId), FinalLineCountdown),
                        ]),
                    Recount<DamageReceivedTriggeredEffectContext>(hurt))));

        yield return Face(RuleId, "What She Writes",
            "Each sentence stands as its own body with a countdown. Break it in time and it is erased; otherwise it comes true. At most 3 stand.",
            triggers:
            [
                On("TurnStarted", morning),
                Own("TurnStarted", dawn),
                Own("DamageTaken", phases),
            ]);
        yield return Face(SentenceId, "A Sentence", "Written by Nisaba. Break it before its countdown runs out and it is erased.");
        foreach (var s in Sentences)
            yield return Face(SentenceKindId(s.Key), s.Name, s.Text);
        yield return Face(ComesTrueId, "Comes True In",
            "At the start of Nisaba's turn this counts down. At 0 the sentence comes true.", stacks: true) with { Tags = [BreakFirstTag] };
        yield return Face(LapisRecordId, "The Lapis Record",
            "From 65 % she writes two sentences at a time, and each has 90 HP.");
        yield return Face(FinalLineWrittenId, "The Final Line",
            "From 30 % only the Final Line stands. She strikes and reads it aloud in turn.");
        yield return Face(FinalLineId, FinalLineName,
            "When its countdown runs out you die. Break it: she takes 100 damage, and Indelible is gone.");
        yield return Indelible(IndelibleId, "Indelible", "While the Final Line stands she cannot fall below 1 HP.");
        yield return Indelible(NotYetWrittenId, "The Line Not Yet Written",
            "She cannot fall below 1 HP before the Final Line is written (at 30 %).");

        // The marks the sentences leave on you.
        yield return Face(MeasureWithheldId, "The Measure Withheld", "At the start of your turn you lose this much Energy.",
            StatusPolarity.Debuff, stacks: true,
            triggers:
            [
                Own("TurnStarted", Do(
                    new LoseResourceNode<TurnStartedTriggeredEffectContext>(self, StandardCombatIds.EnergyResource,
                        StacksOn<TurnStartedTriggeredEffectContext>(self, MeasureWithheldId)),
                    Unmark<TurnStartedTriggeredEffectContext>(self, MeasureWithheldId))),
            ]);
        yield return Face(HandHoldsTwoId, "The Hand Holds Two", "This turn you draw only 2 cards.", StatusPolarity.Debuff,
            modifiers: [Modifier(PassiveModifierPipeline.TurnStartDraw, PassiveModifierOperation.ClampMax, HandSize)],
            triggers: [Own("TurnEnded", Unmark<TurnEndedTriggeredEffectContext>(self, HandHoldsTwoId))]);
        yield return Face(GuardAsNothingId, "The Guard As Nothing", "Until the end of your turn you gain no Block.", StatusPolarity.Debuff,
            modifiers: [Modifier(PassiveModifierPipeline.BlockGain, PassiveModifierOperation.ClampMax, 0)],
            triggers: [Own("TurnEnded", If(IsPlayersTurn<TurnEndedTriggeredEffectContext>(),
                Unmark<TurnEndedTriggeredEffectContext>(self, GuardAsNothingId)))]);
    }

    private static StatusData Indelible(string id, string name, string text) =>
        Face(id, name, text, StatusPolarity.Buff) with
        {
            DeathPrevention = new StatusDeathPreventionData(1, [], Repeating: true),
            Tags = [CannotFallTag],
        };

    // What falls with what: the reed turns when the Final Line breaks; her sentences leave with her.
    public static IReadOnlyList<EncounterTriggerData> Triggers() => Downed(Fallen());

    private static IEffectNode<CombatantDownedTriggeredEffectContext> Fallen()
    {
        var downed = CombatantTargetSelectors.SourceIncludingDowned;
        var her = Her;
        return Do(
            If(Wears<CombatantDownedTriggeredEffectContext>(downed, FinalLineId),
                Do(Unmark<CombatantDownedTriggeredEffectContext>(her, IndelibleId),
                    LoseHealth(her, N<CombatantDownedTriggeredEffectContext>(ReedTurns)))),
            If(Wears<CombatantDownedTriggeredEffectContext>(downed, RuleId),
                Do(KillAll<CombatantDownedTriggeredEffectContext>(SentenceId), KillAll<CombatantDownedTriggeredEffectContext>(FinalLineId))),
            Recount<CombatantDownedTriggeredEffectContext>(her));
    }

    // ── her moves ─────────────────────────────────────────────────────────────────────────────────────────

    public static EffectProgram<EnemyActionContext>? Intent(string enemyId, string intentId) =>
        enemyId != EnemyId ? null : intentId switch
        {
            "inscribe" => new(Do(
                Write(),
                If(Wears<EnemyActionContext>(CombatantTargetSelectors.Source, LapisRecordId), Write()),
                Guard<EnemyActionContext>(CombatantTargetSelectors.Source, InscribeBlock),
                Recount<EnemyActionContext>(CombatantTargetSelectors.Source))),
            "strike_of_the_reed" => new(HitPlayer<EnemyActionContext>(StrikeDamage)),
            "dry_the_clay" => new(new ForEachTargetEffectNode<EnemyActionContext>(Living(SentenceId), Do(
                new ModifyMaxHealthNode<EnemyActionContext>(CombatantTargetSelectors.IterationTarget, Const(DryHealth)),
                new HealNode<EnemyActionContext>(CombatantTargetSelectors.IterationTarget, Const(DryHealth))))),
            "read_aloud" => new(new RepeatEffectNode<EnemyActionContext>(
                Plus(Standing<EnemyActionContext>(), CountOf<EnemyActionContext>(FinalLineId)),
                HitPlayer<EnemyActionContext>(ReadPerSentence))),
            _ => null,
        };

    // One sentence she has not yet written, at random — and none at all with three already standing.
    private static IEffectNode<EnemyActionContext> Write()
    {
        var self = CombatantTargetSelectors.Source;
        ICombatExpression<EnemyActionContext, bool> Written(string key) => Above(CountOf<EnemyActionContext>(SentenceKindId(key)), 0);
        // 1 for a sentence not standing, 0 for one that does.
        var unwritten = Sentences.Select(s => Minus(N<EnemyActionContext>(1),
                Min(CountOf<EnemyActionContext>(SentenceKindId(s.Key)), N<EnemyActionContext>(1))))
            .Aggregate(Plus);
        // 60 is divisible by every count from 1 to 6, so the remainder is an even draw among the unwritten.
        var pick = CounterOf<EnemyActionContext>(self, Roll);
        var health = Plus(N<EnemyActionContext>(SentenceHealth),
            Times(StacksOn<EnemyActionContext>(self, LapisRecordId), LapisSentenceHealth - SentenceHealth));
        return If(AllOf(Lt(Standing<EnemyActionContext>(), MostStanding), Above(unwritten, 0)), Do(
        [
            // the roll is fixed first: what is unwritten changes as soon as the sentence is
            Counter(self, Roll, new RemainderExpression<EnemyActionContext>(new RandomBelowExpression<EnemyActionContext>(60), unwritten)),
            Counter(self, Seen, Const(0)),
            .. Sentences.Select(s => If(Not(Written(s.Key)), Do(
                If(new ComparisonExpression<EnemyActionContext>(CounterOf<EnemyActionContext>(self, Seen), ComparisonOperator.Equal, pick),
                    new SummonCombatantNode<EnemyActionContext>(
                        StandardCombatIds.EnemyTeam, health, new CombatantDefinitionId(SentenceEnemyId), s.Name,
                        startingStatuses:
                        [
                            new StatusGrant(new StatusDefinitionId(SentenceId), 1),
                            new StatusGrant(new StatusDefinitionId(SentenceKindId(s.Key)), 1),
                            new StatusGrant(new StatusDefinitionId(ComesTrueId), SentenceCountdown),
                        ])),
                Bump<EnemyActionContext>(self, Seen)))),
        ]));
    }
}
