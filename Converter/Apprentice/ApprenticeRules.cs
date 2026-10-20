using RogueDeck.Core.Combat;
using RogueDeck.Sandbox.Composition;
using RogueDeck.Scenario.Authoring;
using static BnbContent.Converter.Apprentice.ApprenticeKeywords;

namespace BnbContent.Converter.Apprentice;

// The statuses the Apprentice's cards and relics leave behind: rules that outlive the card that made them, and the
// marks his relics put on him at every fight's start, which the wand's own machinery reads (WandRules.Hooks).
public static partial class ApprenticeRules
{
    // Chalk Circle: "if you do not cast this turn, gain 1 Grit at the end of it" — one Grit per circle drawn.
    public const string QuietCircle = "chalk_circle_quiet";

    // ── his relics' marks, and the wand rule each switches on ────────────────────────────────────────────
    public const string SootyFerrule = "sooty_ferrule_rule";
    public const string KnottedWristcord = "knotted_wristcord_rule";
    public const string BlueInkstone = "blue_inkstone_rule";
    public const string BentNib = "bent_nib_rule";
    public const string BrassDividers = "brass_dividers_rule";
    public const string AshBookcord = "ash_bookcord_rule";
    public const string FeatherCatcher = "feather_catcher_rule";
    public const string ChalkedThumbstall = "chalked_thumbstall_rule";
    public const string BoneSpacer = "bone_spacer_rule";

    private static readonly (WandHook Hook, string Status, string Name)[] RelicMarks =
    [
        (WandHook.SootyFerrule, SootyFerrule, "Sooty Ferrule"),
        (WandHook.KnottedWristcord, KnottedWristcord, "Knotted Wristcord"),
        (WandHook.BlueInkstone, BlueInkstone, "Blue Inkstone"),
        (WandHook.BentNib, BentNib, "Bent Nib"),
        (WandHook.BrassDividers, BrassDividers, "Brass Dividers"),
        (WandHook.AshBookcord, AshBookcord, "Ash Bookcord"),
        (WandHook.FeatherCatcher, FeatherCatcher, "Feather Catcher"),
        (WandHook.ChalkedThumbstall, ChalkedThumbstall, "Chalked Thumbstall"),
        (WandHook.BoneSpacer, BoneSpacer, "Bone Spacer"),
    ];

    public static IReadOnlyDictionary<WandHook, StatusDefinitionId> Hooks =>
        RelicMarks.ToDictionary(m => m.Hook, m => new StatusDefinitionId(m.Status));

    // Glass Rod: the next cast deals and gives more, and strains the wand (the master's +3 Exhaustion).
    public const string GlassRodPrimed = "glass_rod_primed";
    public const string GlassRodPrimedPlus = "glass_rod_primed_plus";

    public static IReadOnlyList<WandCastBoost> CastBoosts =>
    [
        new(new StatusDefinitionId(GlassRodPrimed), 50, 3),
        new(new StatusDefinitionId(GlassRodPrimedPlus), 65, 3),
    ];

    public static IReadOnlyList<StatusData> All() =>
    [
        QuietCircleStatus(), ChilledStatus(),
        Status(GlassRodPrimed, "Glass Rod", StatusPolarity.Buff,
            "Your next cast deals and gives 50% more, and adds 3 Exhaustion."),
        Status(GlassRodPrimedPlus, "Glass Rod+", StatusPolarity.Buff,
            "Your next cast deals and gives 65% more, and adds 3 Exhaustion."),
        .. RelicMarks.Select(m => Status(m.Status, m.Name, StatusPolarity.Neutral, $"{m.Name}'s rule for the wand.")),
        .. Growth(),
    ];

    // Cold Ash: "its next attack deals N less" — once, on the first hit of its next action, and gone at the end of
    // its turn whether it attacked or not.
    private static StatusData ChilledStatus()
    {
        var self = CombatantTargetSelectors.Source;
        var program = new EffectProgram<TurnEndedTriggeredEffectContext>(
            new RemoveStatusNode<TurnEndedTriggeredEffectContext>(self, new StatusDefinitionId(ApprenticeCards.Chilled)));
        return Status(ApprenticeCards.Chilled, "Cold Ash", StatusPolarity.Debuff,
            "Its next attack deals this much less. Fades at the end of its turn.",
            [Trigger(program, nameof(TriggerEvent.TurnEnded))],
            [new PassiveModifierData(PassiveModifierPipeline.DamageDealt, PassiveModifierOperation.AddPerStack, -1, OncePerAction: true)]);
    }

    private static StatusData QuietCircleStatus()
    {
        var self = CombatantTargetSelectors.Source;
        var circles = new CombatantStatusStacksExpression<TurnEndedTriggeredEffectContext>(self, new StatusDefinitionId(QuietCircle));
        var program = new EffectProgram<TurnEndedTriggeredEffectContext>(new CausalSequenceEffectNode<TurnEndedTriggeredEffectContext>(
        [
            new ConditionalEffectNode<TurnEndedTriggeredEffectContext>(
                new ComparisonExpression<TurnEndedTriggeredEffectContext>(
                    new WandValueExpression<TurnEndedTriggeredEffectContext>(WandField.CastsThisTurn),
                    ComparisonOperator.Equal, new ConstantExpression<TurnEndedTriggeredEffectContext>(0)),
                new ApplyStatusNode<TurnEndedTriggeredEffectContext>(self, new StatusDefinitionId(Grit), circles)),
            new RemoveStatusNode<TurnEndedTriggeredEffectContext>(self, new StatusDefinitionId(QuietCircle)),
        ]));
        return Status(QuietCircle, "Chalk Circle", StatusPolarity.Neutral,
            "If you do not cast this turn, gain 1 Grit for each at the end of it.",
            [Trigger(program, nameof(TriggerEvent.TurnEnded))]);
    }
}
