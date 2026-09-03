using System.Text.Json;
using RogueDeck.Core.Combat;
using RogueDeck.Sandbox.Composition;
using RogueDeck.Scenario.Authoring;

namespace BnbContent.Converter;

// Act IV, elite — the Keeper of the Living Cartouche. The act's third word read at boss grade: this is the
// body that finds out what the register is FOR.
//
// Inscribed enlarges the next thing that happens to you, in either direction, and the player has spent the
// whole act deciding which. The Keeper writes that decision down. Every application the register actually
// enlarges is a glyph in a living name:
//
//   BLACK GLYPH  — the register enlarged an affliction. The name is being written against you.
//   GOLDEN GLYPH — the register enlarged a blessing. You spent it on yourself, and the name is thinner for it.
//
// At three glyphs the name is read, and what it says is what you wrote: 14, six more for every black glyph
// and four less for every golden one, with the Keeper bracing 8 for each golden it had to swallow. BBB is 32
// to the face; GGG is 2 damage and 24 Block. The player authors their own worst turn three turns early.
//
// §6.4 exactly: the glyph is recorded off the COMPLETED amplification event, and it is written on the Keeper,
// so nothing it records can be an application that records another glyph.
public static partial class ActFour
{
    public const string CartoucheKeeperEnemyId = "keeper_of_the_living_cartouche";

    public const string LivingCartoucheId = "the_living_cartouche";
    public const string BlackGlyphId = "black_glyph";
    public const string GoldenGlyphId = "golden_glyph";

    public const int GlyphsToRead = 3;

    private const int ReadingBase = 14;
    private const int PerBlackGlyph = 6;
    private const int PerGoldenGlyph = 4;
    private const int GoldenGlyphBlock = 8;

    // What the intent rule reads. The two glyph statuses are the FACE — three slots the player watches fill —
    // and this is the same fact as a number, because a condition cannot add two statuses together.
    public static CounterId Glyphs => new("glyphs");

    public static EffectProgram<EnemyActionContext>? CartoucheKeeperIntent(string enemyId, string intentId) =>
        $"{enemyId}.{intentId}" switch
        {
            "keeper_of_the_living_cartouche.correct_the_name" => CorrectTheName(),
            "keeper_of_the_living_cartouche.read_the_living_name" => ReadTheLivingName(),
            _ => null,
        };

    public static IReadOnlyList<StatusData> CartoucheKeeperStatuses() =>
    [
        TheLivingCartouche(),
        Glyph(BlackGlyphId, "Black Glyph",
            "The register was spent enlarging something done TO you. The living name reads 6 harder for each."),
        Glyph(GoldenGlyphId, "Golden Glyph",
            "The register was spent enlarging something you did for yourself. The living name reads 4 softer "
            + "for each — and the Keeper braces 8."),
    ];

    // ── the cartouche ─────────────────────────────────────────────────────────────────────────────────────

    public static StatusData TheLivingCartouche() => new()
    {
        Id = LivingCartoucheId,
        NameKey = "The Living Cartouche",
        DescriptionKey =
            "Three slots, and you fill them. Every application the register actually enlarges is a glyph — "
            + "black for a curse, gold for a blessing — and at three the name is read back to you.",
        Polarity = StatusPolarity.Neutral,
        StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
        UsesStacks = false,
        Tags = [],
        PassiveModifiers = [],
        Triggers =
        [
            Trigger(RecordAGlyph(), nameof(TriggerEvent.StatusApplicationAmplified), StatusTriggerScope.Anywhere),
        ],
    };

    private static StatusData Glyph(string id, string name, string description) => new()
    {
        Id = id,
        NameKey = name,
        DescriptionKey = description,
        Polarity = StatusPolarity.Neutral,
        StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
        UsesStacks = true,
        Tags = [],
        PassiveModifiers = [],
        Triggers = [],
    };

    // One glyph per completed application, and only for the register's own work: an amplification event names
    // both what grew and what paid for it, so "was it Inscribed?" is one question and "was the thing that grew
    // a curse or a blessing?" is the other. Both were bought when the register was ratified.
    private static EffectProgram<StatusApplicationAmplifiedTriggeredEffectContext> RecordAGlyph()
    {
        var keeper = Bearer(LivingCartoucheId);

        IEffectNode<StatusApplicationAmplifiedTriggeredEffectContext> Record(
            StatusPolarity polarity, string glyph) =>
            new ConditionalEffectNode<StatusApplicationAmplifiedTriggeredEffectContext>(
                new TriggerEventStatusPolarityIsExpression<StatusApplicationAmplifiedTriggeredEffectContext>(
                    polarity),
                new CausalSequenceEffectNode<StatusApplicationAmplifiedTriggeredEffectContext>(
                [
                    new ApplyStatusNode<StatusApplicationAmplifiedTriggeredEffectContext>(
                        keeper, new StatusDefinitionId(glyph),
                        new ConstantExpression<StatusApplicationAmplifiedTriggeredEffectContext>(1),
                        sourceSelector: keeper),
                    new SetCombatantCounterNode<StatusApplicationAmplifiedTriggeredEffectContext>(
                        keeper, Glyphs,
                        new ConstantExpression<StatusApplicationAmplifiedTriggeredEffectContext>(1),
                        relative: true),
                ]));

        return new EffectProgram<StatusApplicationAmplifiedTriggeredEffectContext>(
            new ConditionalEffectNode<StatusApplicationAmplifiedTriggeredEffectContext>(
                new AndExpression<StatusApplicationAmplifiedTriggeredEffectContext>(
                    // It was the register that paid …
                    new TriggerEventAmplifierIsExpression<StatusApplicationAmplifiedTriggeredEffectContext>(
                        new StatusDefinitionId(InscribedId)),
                    // … and it grew on the PLAYER. An amplification reads the other way round from every
                    // other status event in this engine: "source" is the body the enlarged status landed on
                    // — the one wearing the register — and "eventTarget" is whoever applied it, so that a
                    // rule can answer the applier. Asking the event target here would have meant "did the
                    // player apply it", which is true of a blessing they cast on themselves and false of
                    // every curse the Keeper writes.
                    new TargetHasStatusExpression<StatusApplicationAmplifiedTriggeredEffectContext>(
                        CombatantTargetSelectors.Source,
                        new StatusDefinitionId(PassiveStatuses.ApplicantId))),
                new CausalSequenceEffectNode<StatusApplicationAmplifiedTriggeredEffectContext>(
                [
                    Record(StatusPolarity.Debuff, BlackGlyphId),
                    Record(StatusPolarity.Buff, GoldenGlyphId),
                ])));
    }

    // ── reading the name ──────────────────────────────────────────────────────────────────────────────────

    // What the player wrote, read back. The damage floor is zero rather than negative, and the bracing is the
    // Keeper swallowing what you spent the register on yourself.
    private static EffectProgram<EnemyActionContext> ReadTheLivingName()
    {
        var keeper = CombatantTargetSelectors.Source;
        var black = new CombatantStatusStacksExpression<EnemyActionContext>(
            keeper, new StatusDefinitionId(BlackGlyphId));
        var gold = new CombatantStatusStacksExpression<EnemyActionContext>(
            keeper, new StatusDefinitionId(GoldenGlyphId));

        return new EffectProgram<EnemyActionContext>(
            new CausalSequenceEffectNode<EnemyActionContext>(
            [
                new DealDamageNode<EnemyActionContext>(
                    Applicant,
                    new MaxExpression<EnemyActionContext>(
                        Const(0),
                        new SubtractExpression<EnemyActionContext>(
                            new AddExpression<EnemyActionContext>(
                                Const(ReadingBase),
                                new MultiplyExpression<EnemyActionContext>(Const(PerBlackGlyph), black)),
                            new MultiplyExpression<EnemyActionContext>(Const(PerGoldenGlyph), gold)))),

                new GainBlockNode<EnemyActionContext>(
                    keeper, new MultiplyExpression<EnemyActionContext>(Const(GoldenGlyphBlock), gold)),

                new RemoveStatusNode<EnemyActionContext>(keeper, new StatusDefinitionId(BlackGlyphId)),
                new RemoveStatusNode<EnemyActionContext>(keeper, new StatusDefinitionId(GoldenGlyphId)),
                new SetCombatantCounterNode<EnemyActionContext>(keeper, Glyphs, Const(0), relative: false),
            ]));
    }

    // "Remove one existing Glyph; apply Inscribed 1." Which one is not stated, and the Keeper's own interest
    // decides it: a golden glyph is the player's defensive future, so that is the one it corrects out of the
    // name — and it hands over a fresh register while it is at it, which is the offer the whole body is.
    //
    // Its cooldown is its place in the cycle: six intents round, which is twice the three the master asks for.
    private static EffectProgram<EnemyActionContext> CorrectTheName()
    {
        var keeper = CombatantTargetSelectors.Source;

        IEffectNode<EnemyActionContext> Erase(string glyph) =>
            new CausalSequenceEffectNode<EnemyActionContext>(
            [
                new ModifyStatusStacksNode<EnemyActionContext>(
                    keeper, new StatusDefinitionId(glyph), Const(-1)),
                new SetCombatantCounterNode<EnemyActionContext>(keeper, Glyphs, Const(-1), relative: true),
            ]);

        return new EffectProgram<EnemyActionContext>(
            new CausalSequenceEffectNode<EnemyActionContext>(
            [
                new ConditionalEffectNode<EnemyActionContext>(
                    new ComparisonExpression<EnemyActionContext>(
                        new CombatantStatusStacksExpression<EnemyActionContext>(
                            keeper, new StatusDefinitionId(GoldenGlyphId)),
                        ComparisonOperator.Greater, Const(0)),
                    Erase(GoldenGlyphId),
                    new ConditionalEffectNode<EnemyActionContext>(
                        new ComparisonExpression<EnemyActionContext>(
                            new CombatantStatusStacksExpression<EnemyActionContext>(
                                keeper, new StatusDefinitionId(BlackGlyphId)),
                            ComparisonOperator.Greater, Const(0)),
                        Erase(BlackGlyphId))),

                new ApplyStatusNode<EnemyActionContext>(
                    Applicant, new StatusDefinitionId(InscribedId), Const(1)),
            ]));
    }
}
