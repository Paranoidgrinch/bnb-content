using System.Text.Json;
using RogueDeck.Core.Combat;
using RogueDeck.Sandbox.Composition;
using RogueDeck.Scenario.Authoring;

namespace BnbContent.Converter;

// Act IV, Stages 13 and 14 — The Necropolis Warrens and The Chamber of Fixed Days. Two stages about what a
// procedure looks like when it is honest about being a calendar.
//
//   The False-Door Finder certifies the wrong entrance, and does it under LAW: Stage 13 brings Act III's
//   Safe-Conduct, Trespass and Claim back for exactly as long as the Finder is standing (§3.9). A passage
//   check met earns another licence; one missed is a violation owed to the Finder, and three of those are a
//   Claim — which is what makes its false threshold a threshold at all.
//   The Cursed Loot Bearer carries objects that generate their own paperwork as they get harder to lift.
//   Every card whose Burdened surcharge is actually PAID is one more form.
//   The Star-Table Scribe keeps the astronomical table: the measure for each appointed day, 1 → 2 → 3, in
//   that order and no other. A day measured wrong is a day written into the register.
//   The Moon-Cycle Ibis remembers the last rite it managed to perform and returns to it at its cycle point —
//   one stack, never the whole original amount (§3.7) — and sets the rite it has NOT lately done.
//   The Eclipse Scarab's procession contains a scheduled absence of noon. It is visible three turns out.
//
// Nothing here introduces a universal status. Stage 13's law is borrowed whole from Act III — the same
// statuses, the same customs, the same threshold — because a localized return that FORKED the vocabulary
// would be a sixth word for the act, and the point of §3.9 is that it is not one.
public static partial class ActFour
{
    public const string FalseDoorFinderEnemyId = "false_door_finder";
    public const string CursedLootBearerEnemyId = "cursed_loot_bearer";
    public const string StarTableScribeEnemyId = "star_table_scribe";
    public const string MoonCycleIbisEnemyId = "moon_cycle_ibis";
    public const string EclipseScarabEnemyId = "eclipse_scarab";

    public const string NecropolisPassageId = "necropolis_passage";
    public const string EveryObjectRequiresAFormId = "every_object_requires_a_form";
    public const string LunarReturnId = "lunar_return";
    public const string LastRiteBurdenedId = "last_rite_burdened";
    public const string LastRiteEntombedId = "last_rite_entombed";
    public const string ApproachOfNoonId = "approach_of_noon";

    // What the Finder's passage check asks for, and how many licences a body may hold at once. The measure is
    // the same small, payable number the Reed-Cord Surveyor opens the act with — a check nobody can meet is
    // elite machinery (§6.2), not a standard's. The cap is two: the fight opens you with one, compliance can
    // put one more in your pocket, and nothing stockpiles a way out of the stage.
    private const int PassageMeasure = 2;
    private const int SafeConductCap = 2;

    // How many approaches the Scarab makes before noon goes missing. Three of them plus the black turn itself
    // is the master's "every fourth own turn".
    public const int EclipseSteps = 3;

    // The Finder's two bookmarks — one in each of the act's running tallies, because "was it met?" and "was
    // it missed?" are two different answers to one resolution, and the Finder gives a different thing for
    // each. The Loot Bearer's bookmark sits in the surcharge tally instead.
    public static CounterId PassagesMetRead => new("passages_met_read");
    public static CounterId PassagesFailedRead => new("passages_failed_read");
    public static CounterId SurchargesRead => new("surcharges_read");

    // The Scribe's day, and nothing else: the table is fixed, so which day it is on is the whole of its state.
    public static CounterId DecanStep => new("decan_step");

    public static EffectProgram<EnemyActionContext>? WarrenIntent(string enemyId, string intentId) =>
        $"{enemyId}.{intentId}" switch
        {
            "false_door_finder.certify_passage" => SetTheMeasure(13, Const(PassageMeasure)),
            "star_table_scribe.fixed_decan_measure" => FixedDecanMeasure(14),
            "star_table_scribe.table_cover" => TableCover(27),
            "moon_cycle_ibis.set_the_rite" => SetTheRite(15),
            "moon_cycle_ibis.wing_shelter" => WingShelter(25),
            "eclipse_scarab.black_noon" => BlackNoon(12),
            _ => null,
        };

    // What the act serves on the player when a Finder is in the room: the customs that turn three Trespass
    // into a Claim, and the one Safe-Conduct §3.9 insists the player is never expected to arrive with.
    //
    // Asked of the whole roster, like Act III's own opening and for the same reason — Safe-Conduct is kept as
    // per-grant instances, so asking twice would hand out two licences rather than merging one. The
    // Tombbreakers Three bring the same law with them for the same reason the Finder does: their Lamp Thief
    // files Trespass, and the audit is emphatic that the player never arrives with unexplained Act-III
    // resources.
    public static IReadOnlyList<StartingStatusSpec> NecropolisOpening(IEnumerable<string> enemyIds) =>
        enemyIds.Any(id => id == FalseDoorFinderEnemyId || Tombbreakers.Contains(id))
            ?
            [
                new StartingStatusSpec(new StatusDefinitionId(ActThree.GreenDocketCustomsId), 1),
                new StartingStatusSpec(new StatusDefinitionId(ActThree.SafeConductId), 1),
            ]
            : [];

    // ── the False-Door Finder ─────────────────────────────────────────────────────────────────────────────

    // The passage procedure. The check itself is `Certify Passage` — the act's one Primary Measure, raised
    // only when none stands (§3.1) — and this rule is what ANSWERS it: a licence for compliance, a violation
    // for failure, from the Finder and owed to the Finder.
    //
    // It answers on the Finder's OWN turn start, which is the first moment after the player's turn ended in
    // which the result exists, and it takes each resolution once by keeping a bookmark in each tally. That is
    // what makes the answer independent of how many other bodies also watched the same measure resolve.
    public static StatusData NecropolisPassage() => new()
    {
        Id = NecropolisPassageId,
        NameKey = "Necropolis Passage",
        DescriptionKey =
            "This entrance is legally valid. A passage check you meet earns 1 Safe-Conduct (up to 2); one "
            + "you miss is 1 Trespass owed to the Finder — and three Trespass are a Claim.",
        Polarity = StatusPolarity.Neutral,
        StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
        UsesStacks = false,
        Tags = [],
        PassiveModifiers = [],
        Triggers = [Trigger(CertifyTheResult(), nameof(TriggerEvent.TurnStarted))],
    };

    private static EffectProgram<TurnStartedTriggeredEffectContext> CertifyTheResult()
    {
        var finder = CombatantTargetSelectors.Source;
        var missed = SinceLastLooked<TurnStartedTriggeredEffectContext>(finder, MeasuresFailed, PassagesFailedRead);
        var met = SinceLastLooked<TurnStartedTriggeredEffectContext>(finder, MeasuresMet, PassagesMetRead);

        return new EffectProgram<TurnStartedTriggeredEffectContext>(
            new CausalSequenceEffectNode<TurnStartedTriggeredEffectContext>(
            [
                // A failure is filed in the Finder's own name — which is the whole reason the law is worth
                // borrowing: three violations owed to ONE party are that party's Claim, and a Safe-Conduct
                // refuses the filing outright.
                new ConditionalEffectNode<TurnStartedTriggeredEffectContext>(
                    new ComparisonExpression<TurnStartedTriggeredEffectContext>(
                        missed, ComparisonOperator.Greater,
                        new ConstantExpression<TurnStartedTriggeredEffectContext>(0)),
                    new ApplyStatusNode<TurnStartedTriggeredEffectContext>(
                        Applicant, new StatusDefinitionId(ActThree.TrespassId),
                        new ConstantExpression<TurnStartedTriggeredEffectContext>(1), sourceSelector: finder)),

                // Compliance is worth a licence, and the cap is local: this stage is the only place in the
                // act that hands them out at all.
                new ConditionalEffectNode<TurnStartedTriggeredEffectContext>(
                    new AndExpression<TurnStartedTriggeredEffectContext>(
                        new ComparisonExpression<TurnStartedTriggeredEffectContext>(
                            met, ComparisonOperator.Greater,
                            new ConstantExpression<TurnStartedTriggeredEffectContext>(0)),
                        new ComparisonExpression<TurnStartedTriggeredEffectContext>(
                            new CombatantStatusStacksExpression<TurnStartedTriggeredEffectContext>(
                                Applicant, new StatusDefinitionId(ActThree.SafeConductId)),
                            ComparisonOperator.Less,
                            new ConstantExpression<TurnStartedTriggeredEffectContext>(SafeConductCap))),
                    new ApplyStatusNode<TurnStartedTriggeredEffectContext>(
                        Applicant, new StatusDefinitionId(ActThree.SafeConductId),
                        new ConstantExpression<TurnStartedTriggeredEffectContext>(1), sourceSelector: finder)),

                MoveTheBookmark<TurnStartedTriggeredEffectContext>(finder, MeasuresFailed, PassagesFailedRead),
                MoveTheBookmark<TurnStartedTriggeredEffectContext>(finder, MeasuresMet, PassagesMetRead),
            ]));
    }

    // ── the Cursed Loot Bearer ────────────────────────────────────────────────────────────────────────────

    // "Whenever Burdened actually increases the Energy cost paid for a card: apply Paperwork. Max once per
    // card." The act already writes that moment down — the tally Burdened keeps when a surcharge is PAID
    // rather than cleansed away — so the Bearer needs no rule of its own beyond a bookmark in it, and the
    // "once per card" cap is structural: the tally only ever moves once per card.
    //
    // Nor does the total need a ceiling. Every payment works a stack off, so a turn can only pay as many
    // surcharges as there was Burdened to pay them with.
    public static StatusData EveryObjectRequiresAForm() => new()
    {
        Id = EveryObjectRequiresAFormId,
        NameKey = "Every Object Requires a Form",
        DescriptionKey =
            "These goods generate their own paperwork as they get harder to carry: 1 Paperwork for every "
            + "card whose Burdened surcharge you actually paid.",
        Polarity = StatusPolarity.Neutral,
        StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
        UsesStacks = false,
        Tags = [],
        PassiveModifiers = [],
        Triggers = [Trigger(FileForTheGoods(), nameof(TriggerEvent.TurnStarted))],
    };

    private static EffectProgram<TurnStartedTriggeredEffectContext> FileForTheGoods()
    {
        var bearer = CombatantTargetSelectors.Source;
        var unfiled = SinceLastLooked<TurnStartedTriggeredEffectContext>(bearer, BurdenPaid, SurchargesRead);

        return new EffectProgram<TurnStartedTriggeredEffectContext>(
            new CausalSequenceEffectNode<TurnStartedTriggeredEffectContext>(
            [
                new ConditionalEffectNode<TurnStartedTriggeredEffectContext>(
                    new ComparisonExpression<TurnStartedTriggeredEffectContext>(
                        unfiled, ComparisonOperator.Greater,
                        new ConstantExpression<TurnStartedTriggeredEffectContext>(0)),
                    new ApplyStatusNode<TurnStartedTriggeredEffectContext>(
                        Applicant, new StatusDefinitionId(Cards.Keywords.Paperwork), unfiled,
                        sourceSelector: bearer)),

                MoveTheBookmark<TurnStartedTriggeredEffectContext>(bearer, BurdenPaid, SurchargesRead),
            ]));
    }

    // ── the Star-Table Scribe ─────────────────────────────────────────────────────────────────────────────

    // The appointed day's measure: 1, then 2, then 3, then 1 again. No random order — the table is the whole
    // identity, and a player who has read it knows what tomorrow asks for.
    //
    // The day advances only when a measure was actually raised, so a turn spent measuring against somebody
    // else's standard (§3.1) does not silently skip a day out of the table.
    private static EffectProgram<EnemyActionContext> FixedDecanMeasure(int damage) =>
        new(new CausalSequenceEffectNode<EnemyActionContext>(
        [
            new DealDamageNode<EnemyActionContext>(Applicant, Const(damage)),
            new ConditionalEffectNode<EnemyActionContext>(
                new NotExpression<EnemyActionContext>(
                    new TargetHasStatusExpression<EnemyActionContext>(
                        Applicant, new StatusDefinitionId(WeighedId))),
                new CausalSequenceEffectNode<EnemyActionContext>(
                [
                    new ApplyStatusNode<EnemyActionContext>(
                        Applicant, new StatusDefinitionId(WeighedId),
                        new AddExpression<EnemyActionContext>(
                            Const(1),
                            new RemainderExpression<EnemyActionContext>(
                                new CombatantCounterExpression<EnemyActionContext>(
                                    CombatantTargetSelectors.Source, DecanStep),
                                Const(3)))),

                    new SetCombatantCounterNode<EnemyActionContext>(
                        CombatantTargetSelectors.Source, DecanStep, Const(1), relative: true),
                ])),
        ]));

    // Cover the table, and write the day up: a measure taken wrong goes into the register, where it will
    // make the next thing that happens to the player one stack worse. The Scribe does not care HOW wrong —
    // measuring error by band is the Reed-Cord Surveyor's office, not the astronomer's.
    private static EffectProgram<EnemyActionContext> TableCover(int block) =>
        new(new CausalSequenceEffectNode<EnemyActionContext>(
        [
            new GainBlockNode<EnemyActionContext>(CombatantTargetSelectors.Source, Const(block)),
            new ConditionalEffectNode<EnemyActionContext>(
                AtLeast(2),
                new ApplyStatusNode<EnemyActionContext>(
                    Applicant, new StatusDefinitionId(InscribedId), Const(1))),
        ]));

    // ── the Moon-Cycle Ibis ───────────────────────────────────────────────────────────────────────────────

    // The memory, and the two faces it wears. A program cannot hold a status id it only learns at fire time —
    // it can answer one in the same breath (that is what Replicated bought at IV-7), but it cannot put one in
    // its pocket for three turns — so the rite the Ibis last performed is remembered the way this act
    // remembers everything a player has to plan around: as a face on the body.
    public static StatusData LunarReturn() => new()
    {
        Id = LunarReturnId,
        NameKey = "Lunar Return",
        DescriptionKey =
            "This ibis repeats its rites by lunar return. Whatever affliction it last managed to lay on you "
            + "is remembered as the Last Rite — and the next rite it sets is the other one.",
        Polarity = StatusPolarity.Neutral,
        StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
        UsesStacks = false,
        Tags = [],
        PassiveModifiers = [],
        Triggers =
        [
            // Applied and merged both, because a rite laid on a player who already carries it is a merge and
            // not an application — and it is just as much a rite for that.
            Trigger(RememberTheRite<StatusAppliedTriggeredEffectContext>(),
                nameof(TriggerEvent.StatusApplied), StatusTriggerScope.Anywhere),
            Trigger(RememberTheRite<StatusMergedTriggeredEffectContext>(),
                nameof(TriggerEvent.StatusMerged), StatusTriggerScope.Anywhere),
        ],
    };

    public static IReadOnlyList<StatusData> LastRites() =>
    [
        LastRite(LastRiteBurdenedId, "Last Rite: Burdened",
            "The ibis last laid weight on you. Its cycle point repeats 1 Burdened — and its next rite is "
            + "burial."),
        LastRite(LastRiteEntombedId, "Last Rite: Entombed",
            "The ibis last packed you deeper. Its cycle point repeats 1 Entombed — and its next rite is "
            + "weight."),
    ];

    private static StatusData LastRite(string id, string name, string description) => new()
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

    // "Whenever the Ibis SUCCESSFULLY applies a negative status, remember its type." Successfully is the
    // operative word — a refused filing raises no application at all, so nothing is remembered — and the
    // gate is the act's own: an original affliction on the player, by this body.
    private static EffectProgram<TContext> RememberTheRite<TContext>() where TContext : class
    {
        var ibis = CombatantTargetSelectors.Source;

        // "That rite was just laid" → wear its face, and take the other one off.
        IEffectNode<TContext> Remember(string riteId, string faceId, string otherFaceId) =>
            new ConditionalEffectNode<TContext>(
                new TriggerEventStatusIsExpression<TContext>(new StatusDefinitionId(riteId)),
                new CausalSequenceEffectNode<TContext>(
                [
                    new RemoveStatusNode<TContext>(ibis, new StatusDefinitionId(otherFaceId)),
                    new ApplyStatusNode<TContext>(
                        ibis, new StatusDefinitionId(faceId), new ConstantExpression<TContext>(1),
                        sourceSelector: ibis),
                ]));

        return new EffectProgram<TContext>(
            new ConditionalEffectNode<TContext>(
                new AndExpression<TContext>(
                    OriginalAfflictionOnThePlayer<TContext>(),
                    new TargetHasStatusExpression<TContext>(ibis, new StatusDefinitionId(LunarReturnId))),
                new CausalSequenceEffectNode<TContext>(
                [
                    Remember(BurdenedId, LastRiteBurdenedId, LastRiteEntombedId),
                    Remember(EntombedId, LastRiteEntombedId, LastRiteBurdenedId),
                ])));
    }

    // Set the rite the moon has NOT lately returned to: weight after burial, burial after weight, and weight
    // on a night with no record behind it. The memory decides, so the player reads what is coming off the
    // face the ibis is already wearing.
    private static EffectProgram<EnemyActionContext> SetTheRite(int damage) =>
        new(new CausalSequenceEffectNode<EnemyActionContext>(
        [
            new DealDamageNode<EnemyActionContext>(Applicant, Const(damage)),
            new ConditionalEffectNode<EnemyActionContext>(
                new TargetHasStatusExpression<EnemyActionContext>(
                    CombatantTargetSelectors.Source, new StatusDefinitionId(LastRiteBurdenedId)),
                new ApplyStatusNode<EnemyActionContext>(
                    Applicant, new StatusDefinitionId(EntombedId), Const(1)),
                new ApplyStatusNode<EnemyActionContext>(
                    Applicant, new StatusDefinitionId(BurdenedId), Const(1))),
        ]));

    // The cycle point: shelter, and one stack of the Last Rite. ONE — §3.7 is explicit that the return is not
    // a reproduction of the original amount, which is what keeps a predictable body from being a multiplying
    // one. A fight in which the ibis has never landed a rite repeats nothing.
    private static EffectProgram<EnemyActionContext> WingShelter(int block) =>
        new(new CausalSequenceEffectNode<EnemyActionContext>(
        [
            new GainBlockNode<EnemyActionContext>(CombatantTargetSelectors.Source, Const(block)),
            new ConditionalEffectNode<EnemyActionContext>(
                new TargetHasStatusExpression<EnemyActionContext>(
                    CombatantTargetSelectors.Source, new StatusDefinitionId(LastRiteBurdenedId)),
                new ApplyStatusNode<EnemyActionContext>(
                    Applicant, new StatusDefinitionId(BurdenedId), Const(1)),
                new ConditionalEffectNode<EnemyActionContext>(
                    new TargetHasStatusExpression<EnemyActionContext>(
                        CombatantTargetSelectors.Source, new StatusDefinitionId(LastRiteEntombedId)),
                    new ApplyStatusNode<EnemyActionContext>(
                        Applicant, new StatusDefinitionId(EntombedId), Const(1)))),
        ]));

    // ── the Eclipse Scarab ────────────────────────────────────────────────────────────────────────────────

    // How far the procession has got towards the hour that is not there. Visible from the first turn, which
    // is the entire point: the master calls the schedule "visible well in advance", and a catastrophe the
    // player cannot count down to is just a big number.
    public static StatusData ApproachOfNoon() => new()
    {
        Id = ApproachOfNoonId,
        NameKey = "Approach of Noon",
        DescriptionKey =
            "How far the scarab's procession has come. At 3 the next turn is Black Noon, and the count "
            + "starts again.",
        Polarity = StatusPolarity.Neutral,
        StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
        UsesStacks = true,
        Tags = [],
        PassiveModifiers = [],
        Triggers = [],
    };

    // The scheduled absence of noon: a fixed combined threat, and the procession begins again — a calendar
    // does not stop having fourth days.
    private static EffectProgram<EnemyActionContext> BlackNoon(int damage) =>
        new(new CausalSequenceEffectNode<EnemyActionContext>(
        [
            new DealDamageNode<EnemyActionContext>(Applicant, Const(damage)),
            new ApplyStatusNode<EnemyActionContext>(
                Applicant, new StatusDefinitionId("panic"), Const(2)),
            new ApplyStatusNode<EnemyActionContext>(
                Applicant, new StatusDefinitionId(EntombedId), Const(1)),

            new RemoveStatusNode<EnemyActionContext>(
                CombatantTargetSelectors.Source, new StatusDefinitionId(ApproachOfNoonId)),
        ]));
}
