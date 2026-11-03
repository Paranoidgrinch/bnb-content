using System.Text.Json;
using RogueDeck.Core.Combat;
using RogueDeck.Run;
using RogueDeck.Sandbox.Composition;
using RogueDeck.Scenario.Authoring;
using BnbContent.Converter.Cards;

namespace BnbContent.Converter;

// ── The Testator (Act II, Necrology) ─────────────────────────────────────────────────────────────────────────
//
// New in the content cut of 2026-10-29. The Testator keeps one testament at a time, on a chip, and rewrites it at
// the end of each of its turns: to its heirs (every other enemy grows stronger), to you (Block and cards), or of
// its debts (Paperwork for you). When it dies, the testament standing at that moment is executed — so the
// question it asks is not whether to kill it but WHEN.
//
// It also keeps the act's books: what it reads aloud is Overdue owed to it, collected like any other source.
public static class Testator
{
    public const string EnemyId = "the_testator";
    public const string RuleId = "the_testament";
    public const string DelinquencyId = "testator_delinquency";
    public const string HeirsId = "testament_heirs";
    public const string ClaimantId = "testament_claimant";
    public const string DebtsId = "testament_debts";

    // The order the will is rewritten in, round and round.
    public static readonly string[] Testaments = [HeirsId, ClaimantId, DebtsId];

    public const int HeirsStrength = 2;
    public const int HeirsBlock = 10;
    public const int ClaimantBlock = 12;
    public const int ClaimantCards = 2;
    public const int DebtsPaperwork = 3;

    private static ICombatantTargetSelector Applicant { get; } =
        CombatantTargetSelectors.FirstTarget(
            CombatantTargetSelectors.WithStatus(
                CombatantTargetSelectors.AllAliveCombatants, new StatusDefinitionId(PassiveStatuses.ApplicantId)));

    public static IEnumerable<StatusData> Statuses() =>
    [
        Rewriting(),
        ActTwo.Delinquency(DelinquencyId, "Probate Fees",
            "At 2 Overdue owed to the Testator, it collects them: 7 damage."),
        Marker(HeirsId, "Testament: To My Heirs",
            $"If it dies now, every other enemy gains {HeirsStrength} Strength and {HeirsBlock} Block."),
        Marker(ClaimantId, "Testament: To the Claimant",
            $"If it dies now, you gain {ClaimantBlock} Block and draw {ClaimantCards} cards."),
        Marker(DebtsId, "Testament: Of My Debts",
            $"If it dies now, its debts are yours: {DebtsPaperwork} Paperwork."),
    ];

    public static IReadOnlyList<EncounterTriggerData> Triggers() => [TheWillIsRead()];

    // The rule's own chip, and the rewriting at the end of each Testator turn: the next testament in the order.
    private static StatusData Rewriting()
    {
        var self = CombatantTargetSelectors.Source;
        var steps = new List<IEffectNode<TurnEndedTriggeredEffectContext>>();
        // Read the standing testament ONCE, as a chain of branches, so a rewrite cannot be rewritten again in the
        // same pass.
        IEffectNode<TurnEndedTriggeredEffectContext> Rewrite(int index) =>
            index == Testaments.Length
                ? new SequenceEffectNode<TurnEndedTriggeredEffectContext>([])
                : new ConditionalEffectNode<TurnEndedTriggeredEffectContext>(
                    new TargetHasStatusExpression<TurnEndedTriggeredEffectContext>(self, new StatusDefinitionId(Testaments[index])),
                    new CausalSequenceEffectNode<TurnEndedTriggeredEffectContext>(
                    [
                        new RemoveStatusNode<TurnEndedTriggeredEffectContext>(self, new StatusDefinitionId(Testaments[index])),
                        new ApplyStatusNode<TurnEndedTriggeredEffectContext>(
                            self, new StatusDefinitionId(Testaments[(index + 1) % Testaments.Length]),
                            new ConstantExpression<TurnEndedTriggeredEffectContext>(1)),
                    ]),
                    Rewrite(index + 1));

        var program = new EffectProgram<TurnEndedTriggeredEffectContext>(Rewrite(0));
        return Marker(RuleId, "The Testament",
            "Each of its turns it rewrites its testament. When it dies, the testament standing then is carried out.")
            with
            {
                Triggers =
                [
                    new StatusTriggerData("TurnEnded", JsonSerializer.SerializeToElement(
                        program, CombatJson.CreateOptions<TurnEndedTriggeredEffectContext>())),
                ],
            };
    }

    // The will is read when the Testator goes down. An encounter trigger, because it is the falling of THIS body
    // that matters and an encounter trigger hears every downing: in a Downed program the Source is the downed body,
    // asked about through `SourceIncludingDowned` (the Spare-Life Jar's lesson — a plain selector will not resolve
    // to a body that is no longer living).
    private static EncounterTriggerData TheWillIsRead()
    {
        var fallen = CombatantTargetSelectors.SourceIncludingDowned;

        ICombatExpression<CombatantDownedTriggeredEffectContext, bool> Left(string testament) =>
            new TargetHasStatusExpression<CombatantDownedTriggeredEffectContext>(fallen, new StatusDefinitionId(testament));

        var heirs = CombatantTargetSelectors.AllAlliesOfSource;
        // The claimant, from the fallen body's side: the one enemy of it wearing the applicant's marker.
        var claimant = CombatantTargetSelectors.AllEnemiesOfSourceWithStatus(new StatusDefinitionId(PassiveStatuses.ApplicantId));
        var program = new EffectProgram<CombatantDownedTriggeredEffectContext>(
            new ConditionalEffectNode<CombatantDownedTriggeredEffectContext>(
                Left(RuleId),
                new CausalSequenceEffectNode<CombatantDownedTriggeredEffectContext>(
                [
                    new ConditionalEffectNode<CombatantDownedTriggeredEffectContext>(
                        Left(HeirsId),
                        new CausalSequenceEffectNode<CombatantDownedTriggeredEffectContext>(
                        [
                            new ApplyStatusNode<CombatantDownedTriggeredEffectContext>(
                                heirs, new StatusDefinitionId("strength"),
                                new ConstantExpression<CombatantDownedTriggeredEffectContext>(HeirsStrength)),
                            new GainBlockNode<CombatantDownedTriggeredEffectContext>(
                                heirs, new ConstantExpression<CombatantDownedTriggeredEffectContext>(HeirsBlock)),
                        ])),
                    new ConditionalEffectNode<CombatantDownedTriggeredEffectContext>(
                        Left(ClaimantId),
                        new CausalSequenceEffectNode<CombatantDownedTriggeredEffectContext>(
                        [
                            new GainBlockNode<CombatantDownedTriggeredEffectContext>(
                                claimant, new ConstantExpression<CombatantDownedTriggeredEffectContext>(ClaimantBlock)),
                            new DrawCardsNode<CombatantDownedTriggeredEffectContext>(
                                claimant, new ConstantExpression<CombatantDownedTriggeredEffectContext>(ClaimantCards)),
                        ])),
                    new ConditionalEffectNode<CombatantDownedTriggeredEffectContext>(
                        Left(DebtsId),
                        new ApplyStatusNode<CombatantDownedTriggeredEffectContext>(
                            claimant, new StatusDefinitionId(Keywords.Paperwork),
                            new ConstantExpression<CombatantDownedTriggeredEffectContext>(DebtsPaperwork))),
                ])));

        return new EncounterTriggerData("Downed",
            JsonSerializer.SerializeToElement(program, CombatJson.CreateOptions<CombatantDownedTriggeredEffectContext>()));
    }

    private static StatusData Marker(string id, string name, string description) => new()
    {
        Id = id,
        NameKey = name,
        DescriptionKey = description,
        Polarity = StatusPolarity.Neutral,
        StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
        UsesStacks = false,
        Tags = [],
        PassiveModifiers = [],
        Triggers = [],
    };
}
