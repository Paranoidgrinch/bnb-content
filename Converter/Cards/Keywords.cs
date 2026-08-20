using System.Text.Json;
using RogueDeck.Core.Combat;
using RogueDeck.Sandbox.Composition;
using RogueDeck.Scenario.Authoring;

namespace BnbContent.Converter.Cards;

// The keyword substrate the final card pool stands on: the Bureaucrat's own statuses (Paperwork, Doubt,
// Seal, Ratified) and the five character-unspecific ones (Censure, Lien, Citation, Blood Ink, Ward Wax).
//
// Everything here is authored as a RAW EffectProgram against the engine types and serialized through the
// CombatJson converters — the same path PassiveStatuses.cs uses for the enemy passives, and the same path
// game.roguedeck.json is written on. The curated CombatNodeModel cannot reach the expressions these need
// (event deltas, "which status moved", status stacks read off a second combatant).
//
// Two rules govern the numbers here: what the design docs say wins over the older port, and every HP loss
// these statuses cause is authored as DamageOverTime — the design calls it "HP loss, not damage", and that
// kind is exactly what Strength, Doubt and every other Direct-restricted modifier leaves alone.
public static class Keywords
{
    public const string Paperwork = "paperwork";
    public const string Doubt = "doubt";
    public const string Seal = "seal";
    public const string Ratified = "ratified";

    // Archive is an ACTION, not a zone: an Archived card is in the Exhaust pile, but not every exhausted card
    // was Archived, and "whenever you Archive" must fire only for the deliberate act. The action therefore
    // leaves a mark — one stack of this on the archivist, per card — which is both the event a Rite listens
    // for and the running count the cards that scale on it read ("5 damage for each card you have Archived
    // this combat"). It only ever grows, which is what keeps it out of Blood Ink's way.
    public const string Archived = "archived";

    public const string Censure = "censure";
    public const string Lien = "lien";
    public const string Citation = "citation";
    public const string BloodInk = "blood_ink";
    public const string WardWax = "ward_wax";

    // Ratified lasts "until the end of the current player turn", so something has to notice that the PLAYER's
    // turn ended. Selectors are structural, not named, so the hero is found the way the rest of this converter
    // finds it: by the marker every encounter puts on the applicant.
    public const string ApplicantMarker = PassiveStatuses.ApplicantId;

    // Ward Wax decays faster when the enemy turn actually got through. "Got through" is counted on the bearer
    // as unblocked HP damage from an ordinary hit, and read (and cleared) when the round ends.
    public static readonly CounterId StruckThisRoundCounter = new("ward_wax_struck");

    public static IReadOnlyList<StatusData> All() =>
    [
        PaperworkStatus(),
        DoubtStatus(),
        SealStatus(),
        RatifiedStatus(),
        ArchivedStatus(),
        CensureStatus(),
        LienStatus(),
        CitationStatus(),
        BloodInkStatus(),
        WardWaxStatus(),
    ];

    // ── Bureaucrat ────────────────────────────────────────────────────────────────────────────────────────

    // "At the end of the affected enemy's turn, it loses HP equal to its current Paperwork. Paperwork ignores
    // Block and does not decay."
    //
    // The port used to tick this at the bearer's TURN START through the engine's damage-over-time automation,
    // because that was the only way to keep Doubt's attack penalty off it. Authoring the tick directly gets
    // the design's timing back AND keeps it out of the attack pipeline: the hit is DamageOverTime, which no
    // Direct-restricted modifier touches, and it ignores Block outright rather than relying on Block having
    // just been cleared.
    private static StatusData PaperworkStatus() => Status(
        Paperwork, "Paperwork", StatusPolarity.Debuff,
        "At the end of its turn, this character loses HP equal to its Paperwork. Ignores Block. Does not decay.",
        triggers:
        [
            Trigger(TurnEnded(HpLoss<TurnEndedTriggeredEffectContext>(
                CombatantTargetSelectors.Source, Stacks<TurnEndedTriggeredEffectContext>(Paperwork)))),
        ]);

    // "The next X enemy Attack actions each deal 25% less damage. After one full Attack action resolves,
    // remove 1 Doubt. Multi-hit Attacks consume only 1 Doubt for the entire Attack action."
    //
    // The reduction is a passive on every ordinary hit the bearer deals, so a multi-hit attack is softened on
    // each of its hits — which is what "the Attack action as a whole deals 25% less" comes to. The CONSUMPTION
    // is the part the old port got wrong: it spent a stack per damage event, so a three-hit attack ate three
    // Doubt. One stack is now claimed for the first hit of each ACTION and no more, which reads the same from
    // both sides of the fight: one enemy attack, or one card the player plays. Deliberately kept from the
    // design: a blocked attack still spends its Doubt, because the hit happened.
    private static StatusData DoubtStatus() => Status(
        Doubt, "Doubt", StatusPolarity.Debuff,
        "The next attacks this character makes deal 25% less damage. One stack is spent per attack.",
        passives:
        [
            new PassiveModifierData(PassiveModifierPipeline.DamageDealt,
                PassiveModifierOperation.ScalePercent, 75, RestrictDamageKind: DamageKind.Direct),
        ],
        triggers:
        [
            Trigger(new EffectProgram<DamageDealtTriggeredEffectContext>(
                new ConditionalEffectNode<DamageDealtTriggeredEffectContext>(
                    new ClaimOnceThisActionExpression<DamageDealtTriggeredEffectContext>("doubt.spent"),
                    Spend<DamageDealtTriggeredEffectContext>(Doubt, 1))),
                nameof(TriggerEvent.DamageDealt)),
        ]);

    // Seal is a plain counter of intent; the conversion to a Ratify event lives in the cards and relics that
    // apply it (CardAuthoring.ApplySeal), because a status cannot react to its own first application — the
    // engine deliberately keeps a status' StatusApplied trigger from seeing itself, so "you now hold 3" would
    // be invisible on the application that created the status.
    private static StatusData SealStatus() => Status(
        Seal, "Seal", StatusPolarity.Debuff,
        "At 3 Seal, 3 are spent and this character is Ratified. Excess Seal remains.");

    // "Until the end of the current player turn, each Deed targeting that enemy deals +3 total direct damage."
    //
    // Once per Deed PLAYED — not per hit, and not per internal repeat — which is what the engine's
    // OncePerCardPlay modifier means. A second Ratify in the same turn is still its own event for anything
    // watching, but it adds no second +3: the modifier is flat, so extra stacks change nothing.
    //
    // The window closes when the PLAYER's turn ends, which no bearer-scoped trigger on an enemy could see.
    // The trigger is therefore scoped to the whole fight and gated on the ending combatant being the applicant.
    private static StatusData RatifiedStatus() => Status(
        Ratified, "Ratified", StatusPolarity.Debuff,
        "Until the end of your turn, each Deed aimed at this character deals 3 more damage.",
        passives:
        [
            new PassiveModifierData(PassiveModifierPipeline.DamageReceived,
                PassiveModifierOperation.AddFlat, 3, RestrictDamageKind: DamageKind.Direct,
                RestrictSourceCardTag: CardAuthoring.DeedTag, OncePerAction: true),
        ],
        triggers:
        [
            new StatusTriggerData(
                nameof(TriggerEvent.TurnEnded),
                Serialize(new EffectProgram<TurnEndedTriggeredEffectContext>(
                    new ConditionalEffectNode<TurnEndedTriggeredEffectContext>(
                        Wears<TurnEndedTriggeredEffectContext>(CombatantTargetSelectors.Source, ApplicantMarker),
                        new ForEachTargetEffectNode<TurnEndedTriggeredEffectContext>(
                            CombatantTargetSelectors.WithStatus(
                                CombatantTargetSelectors.AllCombatants, new StatusDefinitionId(Ratified)),
                            new RemoveStatusNode<TurnEndedTriggeredEffectContext>(
                                CombatantTargetSelectors.IterationTarget, new StatusDefinitionId(Ratified)))))),
                StatusTriggerScope.Anywhere),
        ]);

    private static StatusData ArchivedStatus() => Status(
        Archived, "Archived", StatusPolarity.Neutral,
        "How many cards you have Archived this combat.");

    // ── general ───────────────────────────────────────────────────────────────────────────────────────────

    // "Censure X: when a Status the bearer would not want is applied, prevent up to X stacks and reduce
    // Censure by the number prevented." The whole rule is the engine's prohibition, including the side
    // relativity (debuffs on the player, buffs on an enemy) and the refusal to prevent itself.
    //
    // Neutral polarity on purpose: Censure must not read as a positive Status, or an enemy's Censure would be
    // counted by the cards that pay attention to buffs (Blacklisted) and eaten by a second Censure.
    private static StatusData CensureStatus() => Status(
        Censure, "Censure", StatusPolarity.Neutral,
        "Prevents statuses this character would not want, one stack per prevented stack.",
        prevention: new StatusPreventionData(StatusPreventionScope.UnwantedByBearer));

    // "Lien X: at the end of the holder's turn, remove up to X remaining Block. The holder loses the same
    // amount of HP. Reduce Lien by the amount resolved. If the holder has no remaining Block, Lien does not
    // decay."
    //
    // min(Block, Lien) without a scratch value is the Bookworm problem again: whichever side is removed first
    // changes what the second read sees. Branching on which is smaller keeps every read on a value that has
    // not been touched yet.
    private static StatusData LienStatus()
    {
        var block = new CombatantDefensivePoolExpression<TurnEndedTriggeredEffectContext>(
            CombatantTargetSelectors.Source, StandardCombatIds.BlockDefensivePool);
        var lien = Stacks<TurnEndedTriggeredEffectContext>(Lien);

        IEffectNode<TurnEndedTriggeredEffectContext> Resolve(
            ICombatExpression<TurnEndedTriggeredEffectContext, int> amount) =>
            new SequenceEffectNode<TurnEndedTriggeredEffectContext>(
            [
                // Block first: the HP loss and the Lien spend both read a value the removal has not touched.
                new ModifyDefensivePoolNode<TurnEndedTriggeredEffectContext>(
                    CombatantTargetSelectors.Source, StandardCombatIds.BlockDefensivePool, Negate(amount)),
                HpLoss<TurnEndedTriggeredEffectContext>(CombatantTargetSelectors.Source, amount),
                new ModifyStatusStacksNode<TurnEndedTriggeredEffectContext>(
                    CombatantTargetSelectors.Source, new StatusDefinitionId(Lien), Negate(amount)),
            ]);

        return Status(
            Lien, "Lien", StatusPolarity.Debuff,
            "At the end of its turn, this character loses up to X remaining Block and the same amount of HP. " +
            "Lien is reduced by what it took. No Block, no decay.",
            triggers:
            [
                Trigger(TurnEnded(new ConditionalEffectNode<TurnEndedTriggeredEffectContext>(
                    new ComparisonExpression<TurnEndedTriggeredEffectContext>(
                        block, ComparisonOperator.GreaterOrEqual, lien),
                    // Block covers the whole claim: the claim is what is taken, and Lien clears.
                    Resolve(lien),
                    // Less Block than claim: only what there is, and the rest of the Lien stays outstanding.
                    Resolve(block)))),
            ]);
    }

    // "Citation X: after the holder resolves a NON-DAMAGING action, it loses X HP. Then remove 1 Citation."
    //
    // What counts as damaging is the design's wording and the engine's answer both: at least one ordinary hit
    // landed on the other side, whether or not Block soaked it. Utility, guarding, healing and summoning are
    // not; nor is a status ticking, which is not an action at all. One action asks the question once, however
    // many sub-effects it contained — which is the whole reason the engine now has an action to ask about.
    private static StatusData CitationStatus() => Status(
        Citation, "Citation", StatusPolarity.Debuff,
        "After this character takes a non-damaging action, it loses HP equal to its Citation, then loses 1 Citation.",
        triggers:
        [
            Trigger(new EffectProgram<ActionResolvedTriggeredEffectContext>(
                new ConditionalEffectNode<ActionResolvedTriggeredEffectContext>(
                    new NotExpression<ActionResolvedTriggeredEffectContext>(
                        new ActionDealtDamageExpression<ActionResolvedTriggeredEffectContext>()),
                    new SequenceEffectNode<ActionResolvedTriggeredEffectContext>(
                    [
                        HpLoss<ActionResolvedTriggeredEffectContext>(
                            CombatantTargetSelectors.Source, Stacks<ActionResolvedTriggeredEffectContext>(Citation)),
                        Spend<ActionResolvedTriggeredEffectContext>(Citation, 1),
                    ]))),
                nameof(TriggerEvent.ActionResolved)),
        ]);

    // "Blood Ink X: whenever another Status on the holder loses one or more stacks in a single Status-change
    // event, the holder loses X HP. Then remove 1 Blood Ink."
    //
    // Three separate readings had to be expressible, and all three are now: the event's DELTA (only a loss
    // counts, so the sign matters), WHICH status moved (never its own — an expression, not a filter, because a
    // trigger filter that excluded itself would change every status already authored), and the last-stack
    // case. A status whose final stack is spent raises StatusExpired, not StatusStacksChanged, so both events
    // carry the same body; expiry is unconditionally a loss.
    private static StatusData BloodInkStatus()
    {
        IEffectNode<TContext> Bleed<TContext>(ICombatantTargetSelector holder) where TContext : class =>
            new SequenceEffectNode<TContext>(
            [
                HpLoss<TContext>(holder, StacksOn<TContext>(holder, BloodInk)),
                new ModifyStatusStacksNode<TContext>(holder, new StatusDefinitionId(BloodInk),
                    new ConstantExpression<TContext>(-1)),
            ]);

        return Status(
            BloodInk, "Blood Ink", StatusPolarity.Debuff,
            "Whenever another status on this character loses stacks, it loses HP equal to Blood Ink, then loses " +
            "1 Blood Ink.",
            triggers:
            [
                Trigger(new EffectProgram<StatusStacksChangedTriggeredEffectContext>(
                    new ConditionalEffectNode<StatusStacksChangedTriggeredEffectContext>(
                        new AndExpression<StatusStacksChangedTriggeredEffectContext>(
                            new NotExpression<StatusStacksChangedTriggeredEffectContext>(
                                new TriggerEventStatusIsExpression<StatusStacksChangedTriggeredEffectContext>(
                                    new StatusDefinitionId(BloodInk))),
                            new ComparisonExpression<StatusStacksChangedTriggeredEffectContext>(
                                new EventAmountExpression<StatusStacksChangedTriggeredEffectContext>(),
                                ComparisonOperator.Less,
                                new ConstantExpression<StatusStacksChangedTriggeredEffectContext>(0))),
                        Bleed<StatusStacksChangedTriggeredEffectContext>(CombatantTargetSelectors.Source))),
                    nameof(TriggerEvent.StatusStacksChanged)),

                // The expiry branch has to watch the whole fight. A bearer-scoped StatusExpired trigger asks
                // whether the status that expired IS this one — which is the opposite question: Blood Ink
                // answers every OTHER status running out. So the rule is fight-scoped and re-states its own
                // gate: the combatant it expired on must be wearing Blood Ink.
                new StatusTriggerData(
                    nameof(TriggerEvent.StatusExpired),
                    Serialize(new EffectProgram<StatusExpiredTriggeredEffectContext>(
                        new ConditionalEffectNode<StatusExpiredTriggeredEffectContext>(
                            new AndExpression<StatusExpiredTriggeredEffectContext>(
                                new NotExpression<StatusExpiredTriggeredEffectContext>(
                                    new TriggerEventStatusIsExpression<StatusExpiredTriggeredEffectContext>(
                                        new StatusDefinitionId(BloodInk))),
                                Wears<StatusExpiredTriggeredEffectContext>(
                                    CombatantTargetSelectors.EventTarget, BloodInk)),
                            Bleed<StatusExpiredTriggeredEffectContext>(CombatantTargetSelectors.EventTarget)))),
                    StatusTriggerScope.Anywhere),
            ]);
    }

    // "Ward Wax X: at the start of your turn, gain X Block. After the enemy turn, lose 1 Ward Wax if you took
    // no unblocked Attack damage, or 2 if you took any."
    //
    // "Unblocked Attack damage" is counted on the bearer as it happens — the damage event reports what reached
    // HP, and the ordinary-hit kind is what separates an attack from a Paperwork tick. The count is read and
    // cleared at the END OF THE ROUND, which is the first moment after the enemy turn that every combatant has
    // acted; the accelerated loss therefore happens once per enemy turn however many hits landed.
    private static StatusData WardWaxStatus()
    {
        IEffectNode<TContext> Decay<TContext>(int amount) where TContext : class =>
            new ModifyStatusStacksNode<TContext>(
                CombatantTargetSelectors.IterationTarget, new StatusDefinitionId(WardWax),
                new ConstantExpression<TContext>(-amount));

        var struck = new CombatantCounterExpression<RoundEndedTriggeredEffectContext>(
            CombatantTargetSelectors.IterationTarget, StruckThisRoundCounter);

        return Status(
            WardWax, "Ward Wax", StatusPolarity.Buff,
            "At the start of your turn, gain Block equal to Ward Wax. After the enemy turn it loses 1 stack, " +
            "or 2 if any attack got through.",
            triggers:
            [
                // AFTER the draw, not at the turn start: a combatant's Block is cleared at its own turn start
                // once its triggers have run, so a guard granted there would be swept away before it could be
                // used. CardsDrawn is the first moment of the turn that survives. (Consequence, recorded in
                // ADAPTATIONS: Ward Wax pays nothing to a bearer that does not draw, which suits a status the
                // design calls player-facing.)
                Trigger(new EffectProgram<CardsDrawnTriggeredEffectContext>(
                    new GainBlockNode<CardsDrawnTriggeredEffectContext>(
                        CombatantTargetSelectors.Source, Stacks<CardsDrawnTriggeredEffectContext>(WardWax))),
                    nameof(TriggerEvent.CardsDrawn)),

                // Remember a hit that got through: an ordinary hit that actually cost HP.
                Trigger(new EffectProgram<DamageReceivedTriggeredEffectContext>(
                    new ConditionalEffectNode<DamageReceivedTriggeredEffectContext>(
                        new ComparisonExpression<DamageReceivedTriggeredEffectContext>(
                            new EventAmountExpression<DamageReceivedTriggeredEffectContext>(),
                            ComparisonOperator.Greater,
                            new ConstantExpression<DamageReceivedTriggeredEffectContext>(0)),
                        // The RECEIVER, not the source: in a damage event "source" is whoever swung.
                        new SetCombatantCounterNode<DamageReceivedTriggeredEffectContext>(
                            CombatantTargetSelectors.EventTarget, StruckThisRoundCounter,
                            new ConstantExpression<DamageReceivedTriggeredEffectContext>(1), relative: true))),
                    nameof(TriggerEvent.DamageTaken)),

                // The round is over: pay the decay and forget the round's hits. Scoped to the whole fight,
                // because a round ending is nobody's own event; the loop finds every wearer.
                new StatusTriggerData(
                    nameof(TriggerEvent.RoundEnded),
                    Serialize(new EffectProgram<RoundEndedTriggeredEffectContext>(
                        new ForEachTargetEffectNode<RoundEndedTriggeredEffectContext>(
                            CombatantTargetSelectors.WithStatus(
                                CombatantTargetSelectors.AllCombatants, new StatusDefinitionId(WardWax)),
                            new SequenceEffectNode<RoundEndedTriggeredEffectContext>(
                            [
                                new ConditionalEffectNode<RoundEndedTriggeredEffectContext>(
                                    new ComparisonExpression<RoundEndedTriggeredEffectContext>(
                                        struck, ComparisonOperator.Greater,
                                        new ConstantExpression<RoundEndedTriggeredEffectContext>(0)),
                                    Decay<RoundEndedTriggeredEffectContext>(2),
                                    Decay<RoundEndedTriggeredEffectContext>(1)),
                                new SetCombatantCounterNode<RoundEndedTriggeredEffectContext>(
                                    CombatantTargetSelectors.IterationTarget, StruckThisRoundCounter,
                                    new ConstantExpression<RoundEndedTriggeredEffectContext>(0), relative: false),
                            ])))),
                    StatusTriggerScope.Anywhere),
            ]);
    }

    // ── shared authoring helpers ──────────────────────────────────────────────────────────────────────────

    // HP loss, not damage: DamageOverTime so no Direct-restricted modifier reshapes it, ignoring Block because
    // every status in this file says it does.
    private static IEffectNode<TContext> HpLoss<TContext>(
        ICombatantTargetSelector who, ICombatExpression<TContext, int> amount) where TContext : class =>
        new DealDamageNode<TContext>(who, amount, ignoresBlock: true, kind: DamageKind.DamageOverTime);

    private static CombatantStatusStacksExpression<TContext> Stacks<TContext>(string statusId)
        where TContext : class =>
        StacksOn<TContext>(CombatantTargetSelectors.Source, statusId);

    private static CombatantStatusStacksExpression<TContext> StacksOn<TContext>(
        ICombatantTargetSelector who, string statusId) where TContext : class =>
        new(who, new StatusDefinitionId(statusId));

    private static ICombatExpression<TContext, int> Negate<TContext>(ICombatExpression<TContext, int> amount)
        where TContext : class =>
        new SubtractExpression<TContext>(new ConstantExpression<TContext>(0), amount);

    private static IEffectNode<TContext> Spend<TContext>(string statusId, int amount) where TContext : class =>
        new ModifyStatusStacksNode<TContext>(
            CombatantTargetSelectors.Source, new StatusDefinitionId(statusId),
            new ConstantExpression<TContext>(-amount));

    private static ICombatExpression<TContext, bool> Wears<TContext>(ICombatantTargetSelector who, string statusId)
        where TContext : class =>
        new TargetHasStatusExpression<TContext>(who, new StatusDefinitionId(statusId));

    private static EffectProgram<TurnEndedTriggeredEffectContext> TurnEnded(
        IEffectNode<TurnEndedTriggeredEffectContext> body) => new(body);

    private static StatusTriggerData Trigger(
        EffectProgram<TurnEndedTriggeredEffectContext> program) =>
        new(nameof(TriggerEvent.TurnEnded), Serialize(program));

    private static StatusTriggerData Trigger<TContext>(EffectProgram<TContext> program, string trigger)
        where TContext : class =>
        new(trigger, Serialize(program));

    private static JsonElement Serialize<TContext>(EffectProgram<TContext> program) where TContext : class =>
        JsonSerializer.SerializeToElement(program, CombatJson.CreateOptions<TContext>());

    private static StatusData Status(
        string id, string name, StatusPolarity polarity, string description,
        IReadOnlyList<PassiveModifierData>? passives = null,
        IReadOnlyList<StatusTriggerData>? triggers = null,
        StatusPreventionData? prevention = null) => new()
        {
            Id = id,
            NameKey = name,
            DescriptionKey = description,
            Polarity = polarity,
            StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
            UsesStacks = true,
            Tags = [],
            PassiveModifiers = passives ?? [],
            Triggers = triggers ?? [],
            Prevention = prevention,
        };
}
