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

    // Two Bureaucrat cards change what a Paperwork tick does, and both are answered inside the tick rather
    // than by watching it from outside — the tick is the only place that knows the HP loss was PAPERWORK'S
    // and not some other lingering effect's.
    //
    // Stay of Execution sits on the enemy and is spent by the tick it skips. Red Ink Doctrine is a Rite the
    // player carries; the tick looks for it on whoever holds it, which is what makes it a rule of the fight
    // rather than a rule of one combatant.
    public const string StayOfExecution = "stay_of_execution";
    public const string RedInkDoctrine = "red_ink_doctrine";
    public const int RedInkPaperwork = 2;

    // A Queue card's program runs ONLY when it resolves, never when it is played — so the pulse it leaves is
    // exactly "a queued card has just resolved", which Pending Matters and Petitioner's Token wait for.
    public const string QueueResolved = "queue_resolved";

    // Junk being FILED, as an event: the cards that create Junk leave this behind so Clerk's Familiar can
    // answer it. A running count that only grows, which is what keeps it clear of Blood Ink.
    public const string JunkFiled = "junk_filed";

    // How much a Lien resolution actually took, written on the holder as it happens. A scratch value, and the
    // only way to do this at all: the three steps of a resolution each change something the next one would
    // otherwise re-read, so the amount is computed ONCE, stored, and spent from storage. It is also the
    // number Usurer's Moon needs ("1 Citation for every 3 Block removed").
    public static CounterId LienResolvedCounter => new("lien_resolved");

    // Usurer's Moon, a Rite the player carries: the Lien resolution looks for it, because only the resolution
    // knows how much Block it took.
    public const string UsurersMoon = "usurers_moon";
    public const string UsurersMoonPlus = "usurers_moon+";

    // Standing Citation, a Rite the player carries: the Citation trigger looks for it, because only that
    // trigger knows it is about to spend a stack.
    public const string StandingCitation = "standing_citation";
    public static CounterId StandingCitationSpared => new("standing_citation_spared");

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
    public static CounterId StruckThisRoundCounter => new("ward_wax_struck");

    // What the round that just ended did to you, kept for the cards that ask about "the previous enemy turn"
    // (Restitution Writ, Blood Testimony). Snapshotted when the round closes, before this round's count is
    // cleared, so it is available all through the turn that follows.
    public static CounterId StruckLastRoundCounter => new("struck_last_round");

    public static IReadOnlyList<StatusData> All() =>
    [
        PaperworkStatus(),
        DoubtStatus(),
        SealStatus(),
        RatifiedStatus(),
        ArchivedStatus(),
        ApplicantStatus(),
        Tally(StayOfExecution, "Stay of Execution",
            "This character's Paperwork does not resolve at the end of its next turn."),
        Tally(RedInkDoctrine, "Red Ink Doctrine", "Paperwork that draws blood writes itself deeper."),
        Tally(QueueResolved, "Resolved from the Queue", "How many queued cards have resolved this combat."),
        Tally(JunkFiled, "Junk Filed", "How much rubbish you have generated this combat."),
        // Usurer's Moon has no rules of its own: the Lien resolution looks for it, because only the
        // resolution knows how much Block it took.
        Tally(UsurersMoon, "Usurer's Moon", "Lien that takes Block also files Citation."),
        Tally(UsurersMoonPlus, "Usurer's Moon+", "Lien that takes Block also files Citation."),
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
            Trigger(TurnEnded(new ConditionalEffectNode<TurnEndedTriggeredEffectContext>(
                // A stay spends itself on the tick it holds off, so the reprieve is exactly one turn long.
                Wears<TurnEndedTriggeredEffectContext>(CombatantTargetSelectors.Source, StayOfExecution),
                new RemoveStatusNode<TurnEndedTriggeredEffectContext>(
                    CombatantTargetSelectors.Source, new StatusDefinitionId(StayOfExecution)),
                @else: new SequenceEffectNode<TurnEndedTriggeredEffectContext>(
                [
                    HpLoss<TurnEndedTriggeredEffectContext>(
                        CombatantTargetSelectors.Source, Stacks<TurnEndedTriggeredEffectContext>(Paperwork)),

                    // "After an enemy takes HP loss from its Paperwork trigger, if it survives, apply 2
                    // Paperwork to it." Asked here because only the tick knows the loss was Paperwork's;
                    // watching the damage from outside could not tell it from any other lingering effect.
                    new ConditionalEffectNode<TurnEndedTriggeredEffectContext>(
                        new AndExpression<TurnEndedTriggeredEffectContext>(
                            new TargetIsAliveExpression<TurnEndedTriggeredEffectContext>(CombatantTargetSelectors.Source),
                            new ComparisonExpression<TurnEndedTriggeredEffectContext>(
                                new CountTargetsExpression<TurnEndedTriggeredEffectContext>(
                                    CombatantTargetSelectors.WithStatus(
                                        CombatantTargetSelectors.AllCombatants,
                                        new StatusDefinitionId(RedInkDoctrine))),
                                ComparisonOperator.Greater,
                                new ConstantExpression<TurnEndedTriggeredEffectContext>(0))),
                        new ApplyStatusNode<TurnEndedTriggeredEffectContext>(
                            CombatantTargetSelectors.Source, new StatusDefinitionId(Paperwork),
                            new ConstantExpression<TurnEndedTriggeredEffectContext>(RedInkPaperwork))),
                ])))),
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
                    new CausalSequenceEffectNode<DamageDealtTriggeredEffectContext>(
                    [
                        // Hedge Covenant: the doubted attack's softening is paid to the player as Block. The
                        // hit that landed is three quarters of what was aimed, so a quarter was prevented —
                        // which is a third of what landed, rounded up. The engine reports what landed, not
                        // what was averted, so that is how the figure is worked back.
                        Held<DamageDealtTriggeredEffectContext>(BureaucratHistory.HedgeCovenant,
                            new ForEachTargetEffectNode<DamageDealtTriggeredEffectContext>(
                                CombatantTargetSelectors.WithStatus(CombatantTargetSelectors.AllCombatants,
                                    new StatusDefinitionId(ApplicantMarker)),
                                new GainBlockNode<DamageDealtTriggeredEffectContext>(
                                    CombatantTargetSelectors.IterationTarget,
                                    new DivideExpression<DamageDealtTriggeredEffectContext>(
                                        new AddExpression<DamageDealtTriggeredEffectContext>(
                                            new EventAmountExpression<DamageDealtTriggeredEffectContext>(),
                                            new ConstantExpression<DamageDealtTriggeredEffectContext>(2)),
                                        new ConstantExpression<DamageDealtTriggeredEffectContext>(3))))),

                        // Hearth Compact: an attack that got nothing through costs no Doubt.
                        new ConditionalEffectNode<DamageDealtTriggeredEffectContext>(
                            new AndExpression<DamageDealtTriggeredEffectContext>(
                                new OrExpression<DamageDealtTriggeredEffectContext>(
                                    Present<DamageDealtTriggeredEffectContext>(BureaucratHistory.HearthCompact),
                                    Present<DamageDealtTriggeredEffectContext>(BureaucratHistory.HearthCompact + "+")),
                                new ComparisonExpression<DamageDealtTriggeredEffectContext>(
                                    new EventAmountExpression<DamageDealtTriggeredEffectContext>(),
                                    ComparisonOperator.Equal,
                                    new ConstantExpression<DamageDealtTriggeredEffectContext>(0))),
                            new NoOpEffectNode<DamageDealtTriggeredEffectContext>(),
                            @else: Spend<DamageDealtTriggeredEffectContext>(Doubt, 1)),
                    ]))),
                nameof(TriggerEvent.DamageDealt)),
        ]);

    // Guest Right: "once per turn, when an enemy with at least 3 Doubt would deal unblocked damage, remove
    // 3 Doubt and reduce that remaining damage to 0."
    //
    // Nothing can stop a hit that is already landing, so the hit is taken and immediately given back — the
    // player ends the exchange exactly where the card says they should. Recorded in ADAPTATIONS.
    private static IEffectNode<TContext> GuestRight<TContext>() where TContext : class
    {
        var attacker = CombatantTargetSelectors.Source;
        var guest = CombatantTargetSelectors.EventTarget;

        return new ConditionalEffectNode<TContext>(
            new AndExpression<TContext>(
                new OrExpression<TContext>(
                    Present<TContext>(BureaucratHistory.GuestRight),
                    Present<TContext>(BureaucratHistory.GuestRight + "+")),
                new AndExpression<TContext>(
                    new ComparisonExpression<TContext>(
                        new CombatantStatusStacksExpression<TContext>(attacker, new StatusDefinitionId(Doubt)),
                        ComparisonOperator.GreaterOrEqual, new ConstantExpression<TContext>(3)),
                    new ComparisonExpression<TContext>(
                        new CombatantCounterExpression<TContext>(guest, BureaucratHistory.GuestRightUsed),
                        ComparisonOperator.Equal, new ConstantExpression<TContext>(0)))),
            new CausalSequenceEffectNode<TContext>(
            [
                new HealNode<TContext>(guest, new EventAmountExpression<TContext>()),
                new ModifyStatusStacksNode<TContext>(attacker, new StatusDefinitionId(Doubt),
                    new ConstantExpression<TContext>(-3)),
                new SetCombatantCounterNode<TContext>(guest, BureaucratHistory.GuestRightUsed,
                    new ConstantExpression<TContext>(1), relative: false),
            ]));
    }

    // Wax Indemnity: "whenever you would take unblocked Attack damage, you may consume up to 4 Ward Wax;
    // reduce that damage by 3 per Wax consumed." Nothing can soften a hit that is already landing, so the Wax
    // buys it back afterwards — the player ends the exchange where the card says they should, at the cost of
    // the healing being visible as healing. Recorded in ADAPTATIONS.
    private const int IndemnityPerWax = 3;

    private static IEffectNode<TContext> WaxIndemnity<TContext>() where TContext : class
    {
        var guest = CombatantTargetSelectors.EventTarget;
        // As much Wax as the hit is worth, never more than four and never more than is worn.
        var spend = new MinExpression<TContext>(
            new MinExpression<TContext>(
                new CombatantStatusStacksExpression<TContext>(guest, new StatusDefinitionId(WardWax)),
                new ConstantExpression<TContext>(4)),
            new DivideExpression<TContext>(
                new AddExpression<TContext>(
                    new EventAmountExpression<TContext>(),
                    new ConstantExpression<TContext>(IndemnityPerWax - 1)),
                new ConstantExpression<TContext>(IndemnityPerWax)));

        return new ConditionalEffectNode<TContext>(
            new AndExpression<TContext>(
                Present<TContext>(GeneralWax.WaxIndemnity),
                new ComparisonExpression<TContext>(
                    new CombatantStatusStacksExpression<TContext>(guest, new StatusDefinitionId(WardWax)),
                    ComparisonOperator.Greater, new ConstantExpression<TContext>(0))),
            new CausalSequenceEffectNode<TContext>(
            [
                new SetCombatantCounterNode<TContext>(guest, IndemnitySpent, spend, relative: false),
                new ModifyStatusStacksNode<TContext>(guest, new StatusDefinitionId(WardWax),
                    new SubtractExpression<TContext>(new ConstantExpression<TContext>(0),
                        new CombatantCounterExpression<TContext>(guest, IndemnitySpent))),
                new HealNode<TContext>(guest,
                    new MinExpression<TContext>(
                        new EventAmountExpression<TContext>(),
                        new MultiplyExpression<TContext>(
                            new CombatantCounterExpression<TContext>(guest, IndemnitySpent),
                            new ConstantExpression<TContext>(IndemnityPerWax)))),
            ]));
    }

    private static CounterId IndemnitySpent => new("wax_indemnity_spent");

    // "Is this rule in force?" — a Rite the player carries is found by looking for it on anybody, since a
    // program run from the enemy's seat cannot address the player directly.
    private static ICombatExpression<TContext, bool> Present<TContext>(string rite) where TContext : class =>
        new ComparisonExpression<TContext>(
            new CountTargetsExpression<TContext>(
                CombatantTargetSelectors.WithStatus(CombatantTargetSelectors.AllCombatants,
                    new StatusDefinitionId(rite))),
            ComparisonOperator.Greater, new ConstantExpression<TContext>(0));

    // The same, for both a rule and its upgrade, running the body when either is in force.
    private static IEffectNode<TContext> Held<TContext>(string rite, IEffectNode<TContext> body)
        where TContext : class =>
        new ConditionalEffectNode<TContext>(
            new OrExpression<TContext>(Present<TContext>(rite), Present<TContext>(rite + "+")),
            body);

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

    private static StatusData ArchivedStatus() =>
        Tally(Archived, "Archived", "How many cards you have Archived this combat.");

    // A plain running count or marker: no rules of its own, only something for other rules to read. Neutral
    // so a cleanse cannot take it and a prohibition does not refuse it.
    private static StatusData Tally(string id, string name, string description) =>
        Status(id, name, StatusPolarity.Neutral, description);

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
    private static StatusData LienStatus() => Status(
        Lien, "Lien", StatusPolarity.Debuff,
        "At the end of its turn, this character loses up to X remaining Block and the same amount of HP. " +
        "Lien is reduced by what it took. No Block, no decay.",
        triggers: [Trigger(TurnEnded(ResolveLien<TurnEndedTriggeredEffectContext>(
            CombatantTargetSelectors.Source, cap: null)))]);

    // One complete Lien resolution on a holder: take up to `cap` (or all of it) of what the Lien can claim,
    // in Block and the same in HP, and reduce the Lien by what it took.
    //
    // The claim is worked out first and written to a counter, because each of the three steps changes a value
    // the next would otherwise read — remove the Block and the claim shrinks under you. The counter is the
    // scratch value the effect language does not otherwise have, and it is what Usurer's Moon reads afterwards.
    public static IEffectNode<TContext> ResolveLien<TContext>(ICombatantTargetSelector holder, int? cap)
        where TContext : class
    {
        ICombatExpression<TContext, int> claim = new MinExpression<TContext>(
            new CombatantDefensivePoolExpression<TContext>(holder, StandardCombatIds.BlockDefensivePool),
            new CombatantStatusStacksExpression<TContext>(holder, new StatusDefinitionId(Lien)));

        if (cap is { } ceiling)
            claim = new MinExpression<TContext>(claim, new ConstantExpression<TContext>(ceiling));

        var taken = new CombatantCounterExpression<TContext>(holder, LienResolvedCounter);

        return new CausalSequenceEffectNode<TContext>(
        [
            new SetCombatantCounterNode<TContext>(holder, LienResolvedCounter, claim, relative: false),
            new ModifyDefensivePoolNode<TContext>(holder, StandardCombatIds.BlockDefensivePool, Negate(taken)),
            HpLoss<TContext>(holder, taken),
            new ModifyStatusStacksNode<TContext>(holder, new StatusDefinitionId(Lien), Negate(taken)),
            UsurersMoonCitation<TContext>(holder, taken, perCitation: 3, moon: UsurersMoon),
            UsurersMoonCitation<TContext>(holder, taken, perCitation: 2, moon: UsurersMoonPlus),

            // Debt Ouroboros: a resolved claim renews itself at half, rounded down, at most 4.
            new ConditionalEffectNode<TContext>(
                new OrExpression<TContext>(
                    Present<TContext>(GeneralWax.DebtOuroboros),
                    Present<TContext>(GeneralWax.DebtOuroboros + "+")),
                new ApplyStatusNode<TContext>(holder, new StatusDefinitionId(Lien),
                    new MinExpression<TContext>(
                        new DivideExpression<TContext>(taken, new ConstantExpression<TContext>(2)),
                        new ConstantExpression<TContext>(4)))),
        ]);
    }

    // "Whenever Lien removes Block from an enemy, apply 1 Citation for every N Block removed, maximum 3 per
    // resolution." Asked inside the resolution, because only the resolution knows how much it took.
    private static IEffectNode<TContext> UsurersMoonCitation<TContext>(
        ICombatantTargetSelector holder, ICombatExpression<TContext, int> taken, int perCitation, string moon)
        where TContext : class =>
        new ConditionalEffectNode<TContext>(
            new AndExpression<TContext>(
                new ComparisonExpression<TContext>(
                    new CountTargetsExpression<TContext>(
                        CombatantTargetSelectors.WithStatus(
                            CombatantTargetSelectors.AllCombatants, new StatusDefinitionId(moon))),
                    ComparisonOperator.Greater, new ConstantExpression<TContext>(0)),
                // Only what the Lien took off an ENEMY counts; the applicant's own Liens pay nothing.
                new NotExpression<TContext>(
                    new TargetHasStatusExpression<TContext>(holder, new StatusDefinitionId(ApplicantMarker)))),
            new ApplyStatusNode<TContext>(holder, new StatusDefinitionId(Citation),
                new MinExpression<TContext>(
                    new DivideExpression<TContext>(taken, new ConstantExpression<TContext>(perCitation)),
                    new ConstantExpression<TContext>(3))));

    // "Citation X: after the holder resolves a NON-DAMAGING action, it loses X HP. Then remove 1 Citation."
    //
    // What counts as damaging is the design's wording and the engine's answer both: at least one ordinary hit
    // landed on the other side, whether or not Block soaked it. Utility, guarding, healing and summoning are
    // not; nor is a status ticking, which is not an action at all. One action asks the question once, however
    // many sub-effects it contained.
    private static StatusData CitationStatus() => Status(
        Citation, "Citation", StatusPolarity.Debuff,
        "After this character takes a non-damaging action, it loses HP equal to its Citation, then loses 1 Citation.",
        triggers:
        [
            Trigger(new EffectProgram<ActionResolvedTriggeredEffectContext>(
                new ConditionalEffectNode<ActionResolvedTriggeredEffectContext>(
                    new NotExpression<ActionResolvedTriggeredEffectContext>(
                        new ActionDealtDamageExpression<ActionResolvedTriggeredEffectContext>()),
                    new CausalSequenceEffectNode<ActionResolvedTriggeredEffectContext>(
                    [
                        HpLoss<ActionResolvedTriggeredEffectContext>(
                            CombatantTargetSelectors.Source, Stacks<ActionResolvedTriggeredEffectContext>(Citation)),

                        // Standing Citation: the first trigger on each enemy every turn costs no stack. Asked
                        // here because only this trigger knows it is about to spend one; the latch is per
                        // bearer, so "each enemy" is per enemy, and it is cleared at that enemy's turn start.
                        new ConditionalEffectNode<ActionResolvedTriggeredEffectContext>(
                            new AndExpression<ActionResolvedTriggeredEffectContext>(
                                new ComparisonExpression<ActionResolvedTriggeredEffectContext>(
                                    new CountTargetsExpression<ActionResolvedTriggeredEffectContext>(
                                        CombatantTargetSelectors.WithStatus(
                                            CombatantTargetSelectors.AllCombatants,
                                            new StatusDefinitionId(StandingCitation))),
                                    ComparisonOperator.Greater,
                                    new ConstantExpression<ActionResolvedTriggeredEffectContext>(0)),
                                new ComparisonExpression<ActionResolvedTriggeredEffectContext>(
                                    new CombatantCounterExpression<ActionResolvedTriggeredEffectContext>(
                                        CombatantTargetSelectors.Source, StandingCitationSpared),
                                    ComparisonOperator.Equal,
                                    new ConstantExpression<ActionResolvedTriggeredEffectContext>(0))),
                            new SetCombatantCounterNode<ActionResolvedTriggeredEffectContext>(
                                CombatantTargetSelectors.Source, StandingCitationSpared,
                                new ConstantExpression<ActionResolvedTriggeredEffectContext>(1), relative: false),
                            @else: Spend<ActionResolvedTriggeredEffectContext>(Citation, 1)),
                    ]))),
                nameof(TriggerEvent.ActionResolved)),

            // The sparing is per turn, per bearer.
            Trigger(new EffectProgram<TurnStartedTriggeredEffectContext>(
                new SetCombatantCounterNode<TurnStartedTriggeredEffectContext>(
                    CombatantTargetSelectors.Source, StandingCitationSpared,
                    new ConstantExpression<TurnStartedTriggeredEffectContext>(0), relative: false)),
                nameof(TriggerEvent.TurnStarted)),
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
                    new CausalSequenceEffectNode<CardsDrawnTriggeredEffectContext>(
                    [
                        new GainBlockNode<CardsDrawnTriggeredEffectContext>(
                            CombatantTargetSelectors.Source, Stacks<CardsDrawnTriggeredEffectContext>(WardWax)),

                        // Candle Cathedral: the wax pays half again, rounded up.
                        Held<CardsDrawnTriggeredEffectContext>(ActIVRites.CandleCathedral,
                            new GainBlockNode<CardsDrawnTriggeredEffectContext>(
                                CombatantTargetSelectors.Source,
                                new DivideExpression<CardsDrawnTriggeredEffectContext>(
                                    new AddExpression<CardsDrawnTriggeredEffectContext>(
                                        Stacks<CardsDrawnTriggeredEffectContext>(WardWax),
                                        new ConstantExpression<CardsDrawnTriggeredEffectContext>(1)),
                                    new ConstantExpression<CardsDrawnTriggeredEffectContext>(2)))),
                    ])),
                    nameof(TriggerEvent.CardsDrawn)),

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
                                    new AndExpression<RoundEndedTriggeredEffectContext>(
                                        new ComparisonExpression<RoundEndedTriggeredEffectContext>(
                                            struck, ComparisonOperator.Greater,
                                            new ConstantExpression<RoundEndedTriggeredEffectContext>(0)),
                                        // Candle Cathedral and Wax Reliquary suspend the accelerated loss.
                                        new NotExpression<RoundEndedTriggeredEffectContext>(
                                            new OrExpression<RoundEndedTriggeredEffectContext>(
                                                new OrExpression<RoundEndedTriggeredEffectContext>(
                                                    Present<RoundEndedTriggeredEffectContext>(ActIVRites.CandleCathedral),
                                                    Present<RoundEndedTriggeredEffectContext>(ActIVRites.CandleCathedral + "+")),
                                                Present<RoundEndedTriggeredEffectContext>(GeneralWax.WaxReliquary)))),
                                    Decay<RoundEndedTriggeredEffectContext>(2),
                                    Decay<RoundEndedTriggeredEffectContext>(1)),
                            ])))),
                    StatusTriggerScope.Anywhere),
            ]);
    }

    // The applicant marker: it says which combatant is the player — selectors are structural, so this is the
    // only way a rule can ask "did that happen to the player?" — and it keeps the record of what got through.
    //
    // The record lives here rather than on Ward Wax because several cards ask about the enemy turn just past
    // (Restitution Writ, Blood Testimony, Sealed Mantle) and must be able to ask whether or not the player
    // happens to be wearing anything. Counted as it happens, and rolled over when the round closes so the
    // figure is still there all through the turn that follows.
    private static StatusData ApplicantStatus() => new()
    {
        Id = ApplicantMarker,
        NameKey = "The Applicant",
        DescriptionKey = "You are the one this is all happening to.",
        Polarity = StatusPolarity.Neutral,
        StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
        UsesStacks = true,
        Triggers =
        [
            // An ordinary hit that actually cost HP. The RECEIVER, not the source: in a damage event
            // "source" is whoever swung.
            Trigger(new EffectProgram<DamageReceivedTriggeredEffectContext>(
                new ConditionalEffectNode<DamageReceivedTriggeredEffectContext>(
                    new ComparisonExpression<DamageReceivedTriggeredEffectContext>(
                        new EventAmountExpression<DamageReceivedTriggeredEffectContext>(),
                        ComparisonOperator.Greater,
                        new ConstantExpression<DamageReceivedTriggeredEffectContext>(0)),
                    new CausalSequenceEffectNode<DamageReceivedTriggeredEffectContext>(
                    [
                        new SetCombatantCounterNode<DamageReceivedTriggeredEffectContext>(
                            CombatantTargetSelectors.EventTarget, StruckThisRoundCounter,
                            new EventAmountExpression<DamageReceivedTriggeredEffectContext>(), relative: true),
                        GuestRight<DamageReceivedTriggeredEffectContext>(),
                        WaxIndemnity<DamageReceivedTriggeredEffectContext>(),
                    ]))),
                nameof(TriggerEvent.DamageTaken)),

            // Guest Right's once-per-turn licence.
            Trigger(new EffectProgram<TurnStartedTriggeredEffectContext>(
                new SetCombatantCounterNode<TurnStartedTriggeredEffectContext>(
                    CombatantTargetSelectors.Source, BureaucratHistory.GuestRightUsed,
                    new ConstantExpression<TurnStartedTriggeredEffectContext>(0), relative: false)),
                nameof(TriggerEvent.TurnStarted)),

            // Who struck, and how often they have. Written on the ATTACKER, so "each time this enemy has
            // attacked" is per enemy; the applicant only does the writing, because it is the one status
            // present in every encounter and the record has to exist whether or not a card is asking yet.
            new StatusTriggerData(
                nameof(TriggerEvent.ActionResolved),
                Serialize(new EffectProgram<ActionResolvedTriggeredEffectContext>(
                    new ConditionalEffectNode<ActionResolvedTriggeredEffectContext>(
                        new AndExpression<ActionResolvedTriggeredEffectContext>(
                            new ActionDealtDamageExpression<ActionResolvedTriggeredEffectContext>(),
                            new NotExpression<ActionResolvedTriggeredEffectContext>(
                                new TargetHasStatusExpression<ActionResolvedTriggeredEffectContext>(
                                    CombatantTargetSelectors.Source, new StatusDefinitionId(ApplicantMarker)))),
                        new CausalSequenceEffectNode<ActionResolvedTriggeredEffectContext>(
                        [
                            new SetCombatantCounterNode<ActionResolvedTriggeredEffectContext>(
                                CombatantTargetSelectors.Source, BureaucratHistory.AttacksCounter,
                                new ConstantExpression<ActionResolvedTriggeredEffectContext>(1), relative: true),
                            new ApplyStatusNode<ActionResolvedTriggeredEffectContext>(
                                CombatantTargetSelectors.Source,
                                new StatusDefinitionId(BureaucratHistory.AttackedThisRound),
                                new ConstantExpression<ActionResolvedTriggeredEffectContext>(1)),
                        ])))),
                StatusTriggerScope.Anywhere),

            // The round is over: keep what it did, then start counting again. Fight-scoped, because a round
            // ending is nobody's own event.
            new StatusTriggerData(
                nameof(TriggerEvent.RoundEnded),
                Serialize(new EffectProgram<RoundEndedTriggeredEffectContext>(
                    new ForEachTargetEffectNode<RoundEndedTriggeredEffectContext>(
                        CombatantTargetSelectors.WithStatus(
                            CombatantTargetSelectors.AllCombatants, new StatusDefinitionId(ApplicantMarker)),
                        new CausalSequenceEffectNode<RoundEndedTriggeredEffectContext>(
                        [
                            new SetCombatantCounterNode<RoundEndedTriggeredEffectContext>(
                                CombatantTargetSelectors.IterationTarget, StruckLastRoundCounter,
                                new CombatantCounterExpression<RoundEndedTriggeredEffectContext>(
                                    CombatantTargetSelectors.IterationTarget, StruckThisRoundCounter),
                                relative: false),
                            new SetCombatantCounterNode<RoundEndedTriggeredEffectContext>(
                                CombatantTargetSelectors.IterationTarget, StruckThisRoundCounter,
                                new ConstantExpression<RoundEndedTriggeredEffectContext>(0), relative: false),
                        ])))),
                StatusTriggerScope.Anywhere),

            // …and the same rollover for who attacked: last round's marks are cleared, this round's become
            // last round's. Two passes, because a combatant may be in either set or both.
            new StatusTriggerData(
                nameof(TriggerEvent.RoundEnded),
                Serialize(new EffectProgram<RoundEndedTriggeredEffectContext>(
                    new CausalSequenceEffectNode<RoundEndedTriggeredEffectContext>(
                    [
                        new ForEachTargetEffectNode<RoundEndedTriggeredEffectContext>(
                            CombatantTargetSelectors.WithStatus(CombatantTargetSelectors.AllCombatants,
                                new StatusDefinitionId(BureaucratHistory.AttackedLastRound)),
                            new RemoveStatusNode<RoundEndedTriggeredEffectContext>(
                                CombatantTargetSelectors.IterationTarget,
                                new StatusDefinitionId(BureaucratHistory.AttackedLastRound))),
                        new ForEachTargetEffectNode<RoundEndedTriggeredEffectContext>(
                            CombatantTargetSelectors.WithStatus(CombatantTargetSelectors.AllCombatants,
                                new StatusDefinitionId(BureaucratHistory.AttackedThisRound)),
                            new CausalSequenceEffectNode<RoundEndedTriggeredEffectContext>(
                            [
                                new ApplyStatusNode<RoundEndedTriggeredEffectContext>(
                                    CombatantTargetSelectors.IterationTarget,
                                    new StatusDefinitionId(BureaucratHistory.AttackedLastRound),
                                    new ConstantExpression<RoundEndedTriggeredEffectContext>(1)),
                                new RemoveStatusNode<RoundEndedTriggeredEffectContext>(
                                    CombatantTargetSelectors.IterationTarget,
                                    new StatusDefinitionId(BureaucratHistory.AttackedThisRound)),
                            ])),
                    ]))),
                StatusTriggerScope.Anywhere),
        ],
    };

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
