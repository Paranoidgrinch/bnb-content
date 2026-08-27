using System.Text.Json;
using RogueDeck.Core.Combat;
using RogueDeck.Scenario.Authoring;

namespace BnbContent.Converter;

// Act III — The Green Docket: the shared vocabulary, and the Stage-1 identities that introduce it.
//
// Act II's pressure was source-bound DEBT. Act III's is source-bound STANDING, and the act says why in one
// line: **law exists because everyone remembers what everyone else did.** Four things carry the whole act.
//
//   Trespass    on the player, owed to whoever's law was broken. At 3 from ONE source those three are spent
//               and that source gains a Claim. Trespass never deals damage; it only accumulates into standing.
//   Safe-Conduct on the player, spent to refuse a Trespass — and to refuse NOTHING ELSE. That narrowness is
//               the engine's (a prohibition may name the one status it refuses); a safe conduct that also ate
//               Doubt and Panic would quietly be the best defensive status in the game.
//   Claim       on an enemy: recognised standing, at most 3. Deliberately not a damage multiplier. The
//               interesting question is what THIS party believes its Claim entitles it to.
//   Wergild     a demand owed by the player to one source, settled by Making Amends. (Stage 4.)
//
// **Newly created is not the same as transferred.** A Claim is newly created by three Trespass, by unpaid
// Wergild, or by an effect that says "create"; a transfer only changes owner. Effects that listen for a new
// Claim must not hear a transfer, because that is the loop the design spent a whole section closing. So the
// content keeps the two apart as two things: `claim` is the resource, and `claim_created` is the
// announcement that a claim was MADE. Only creation raises the announcement — the Act-II idiom of a status
// whose count only ever grows, which raises StatusApplied and then StatusMerged and never trips Blood Ink.
public static partial class ActThree
{
    // ── the vocabulary ────────────────────────────────────────────────────────────────────────────────────

    public const string TrespassId = "trespass";
    public const string SafeConductId = "safe_conduct";
    public const string ClaimId = "claim";
    public const string ClaimCreatedId = "claim_created";
    public const string GreenDocketCustomsId = "green_docket_customs";

    // At most three Claims to a party — the design's suggested ceiling, enforced where claims are made.
    public const int ClaimCeiling = 3;

    // How many Trespass from one source become that source's Claim.
    public const int TrespassThreshold = 3;

    // One instance per violation, each carrying the source whose law was broken. Merging would collapse the
    // whole point — the threshold is "3 from the SAME source", and merged stacks remember only the last one.
    public static StatusData Trespass() => new()
    {
        Id = TrespassId,
        NameKey = "Trespass",
        DescriptionKey =
            "A violation owed to whoever's law you broke. It deals no damage. At 3 from one source, those 3 "
            + "are spent and that source gains a Claim.",
        Polarity = StatusPolarity.Debuff,
        StackingBehavior = StatusStackingBehavior.CreateSeparateInstance,
        UsesStacks = true,
        Tags = [],
        PassiveModifiers = [],
        Triggers = [],
    };

    // The licence. Its Prevention names Trespass and refuses nothing else; the engine spends it stack for
    // stack and never lets it refuse itself. Separate instances, because Stage 5's Witchling cares WHOSE
    // hospitality a stack was — a merged stack remembers only the last giver.
    public static StatusData SafeConduct() => new()
    {
        Id = SafeConductId,
        NameKey = "Safe-Conduct",
        DescriptionKey = "Leave to pass. One is spent to refuse one Trespass, and it refuses nothing else.",
        Polarity = StatusPolarity.Buff,
        StackingBehavior = StatusStackingBehavior.CreateSeparateInstance,
        UsesStacks = true,
        Tags = [],
        PassiveModifiers = [],
        Triggers = [],
        Prevention = new StatusPreventionData(
            StatusPreventionScope.Debuffs, StacksPerStack: 1, Only: TrespassId),
    };

    // Standing. Merged, because a party's Claims are one fact about that party — how many it holds — and
    // every rule in the act asks exactly that.
    public static StatusData Claim() => new()
    {
        Id = ClaimId,
        NameKey = "Claim",
        DescriptionKey =
            "Recognised standing. It is not a damage bonus: each party reads its own Claims its own way. "
            + "At most 3 to a party.",
        Polarity = StatusPolarity.Buff,
        StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
        UsesStacks = true,
        Tags = [],
        PassiveModifiers = [],
        Triggers = [],
    };

    // The announcement, not the thing: a count that only ever grows, so a rule can hear that a Claim was MADE
    // without hearing every Claim that merely changed hands.
    public static StatusData ClaimCreated() => new()
    {
        Id = ClaimCreatedId,
        NameKey = "Claim Lodged",
        DescriptionKey = "How many Claims this party has been granted outright, as opposed to handed.",
        Polarity = StatusPolarity.Neutral,
        StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
        UsesStacks = true,
        Tags = [],
        PassiveModifiers = [],
        Triggers = [],
    };

    // ── the act's customs ─────────────────────────────────────────────────────────────────────────────────

    // The rule every Green Docket fight puts on the player: three Trespass owed to one party become that
    // party's Claim. It lives on the player because it is about the PLAYER's record — and the trigger's own
    // acting source is the party that just filed, which is what makes "from the same source" answerable at
    // all: the stacks spent are provably that party's and never a neighbour's.
    //
    // The count is its own gate. It only ever reaches the threshold on the application that completes it,
    // because that application is immediately spent back down to nothing.
    public static StatusData GreenDocketCustoms()
    {
        var player = CombatantTargetSelectors.EventTarget;
        var filer = CombatantTargetSelectors.Source;

        var steps = new List<IEffectNode<StatusAppliedTriggeredEffectContext>>();
        for (var i = 0; i < TrespassThreshold; i++)
            steps.Add(SpendOneTrespass());
        steps.Add(CreateClaim<StatusAppliedTriggeredEffectContext>(filer));

        var program = new EffectProgram<StatusAppliedTriggeredEffectContext>(
            new ConditionalEffectNode<StatusAppliedTriggeredEffectContext>(
                new ComparisonExpression<StatusAppliedTriggeredEffectContext>(
                    new CombatantStatusStacksFromSourceExpression<StatusAppliedTriggeredEffectContext>(
                        player, new StatusDefinitionId(TrespassId), filer),
                    ComparisonOperator.GreaterOrEqual,
                    new ConstantExpression<StatusAppliedTriggeredEffectContext>(TrespassThreshold)),
                new CausalSequenceEffectNode<StatusAppliedTriggeredEffectContext>(steps)));

        return Rule(GreenDocketCustomsId, "The Green Docket",
            "Three Trespass owed to one party are spent, and that party gains a Claim.",
            [new StatusTriggerData("StatusApplied", JsonSerializer.SerializeToElement(
                program, CombatJson.CreateOptions<StatusAppliedTriggeredEffectContext>()))]);
    }

    // One instance of one stack at a time, and only the filer's own — three separate picks, because a single
    // −3 would empty one violation and leave the other two standing.
    private static IEffectNode<StatusAppliedTriggeredEffectContext> SpendOneTrespass() =>
        new ModifySelectedStatusStacksNode<StatusAppliedTriggeredEffectContext>(
            CombatantTargetSelectors.EventTarget,
            new StatusSelectionSpec(StatusPolarityFilter.Debuff)
            {
                Definition = new StatusDefinitionId(TrespassId),
                FromActingSource = true,
            },
            new ConstantExpression<StatusAppliedTriggeredEffectContext>(-1));

    // ── making and moving Claims ──────────────────────────────────────────────────────────────────────────

    // A Claim is CREATED: the resource and the announcement together, and neither happens once the party is
    // already at the ceiling. Everything in the act that says "gains 1 newly created Claim" goes through here,
    // so a rule listening for a new Claim hears every one of them and no transfer at all.
    //
    // Both are filed in the HOLDER's own name rather than in the name of whoever's rule made them, because
    // standing belongs to the party that holds it — and because a rule woken by the announcement then finds
    // itself looking at the fight from the holder's side, which is the only side its question makes sense on.
    public static IEffectNode<TContext> CreateClaim<TContext>(ICombatantTargetSelector holder)
        where TContext : class =>
        new ConditionalEffectNode<TContext>(
            new ComparisonExpression<TContext>(
                new CombatantStatusStacksExpression<TContext>(holder, new StatusDefinitionId(ClaimId)),
                ComparisonOperator.Less,
                new ConstantExpression<TContext>(ClaimCeiling)),
            new CausalSequenceEffectNode<TContext>(
            [
                new ApplyStatusNode<TContext>(
                    holder, new StatusDefinitionId(ClaimId), new ConstantExpression<TContext>(1),
                    sourceSelector: holder),
                new ApplyStatusNode<TContext>(
                    holder, new StatusDefinitionId(ClaimCreatedId), new ConstantExpression<TContext>(1),
                    sourceSelector: holder),
            ]));

    // A Claim CHANGES HANDS: one leaves the holder and one arrives with the new one, and the announcement is
    // never touched. The design spends a section on this distinction because it is what keeps the Boundary
    // Stone, the Ditch Lamprey and the Bracken Moot from feeding each other forever.
    public static IEffectNode<TContext> TransferClaim<TContext>(
        ICombatantTargetSelector from, ICombatantTargetSelector to)
        where TContext : class =>
        new CausalSequenceEffectNode<TContext>(
        [
            new ModifySelectedStatusStacksNode<TContext>(
                from,
                new StatusSelectionSpec(StatusPolarityFilter.Any) { Definition = new StatusDefinitionId(ClaimId) },
                new ConstantExpression<TContext>(-1)),
            new ApplyStatusNode<TContext>(
                to, new StatusDefinitionId(ClaimId), new ConstantExpression<TContext>(1), sourceSelector: to),
        ]);

    // ── the pressure intents ──────────────────────────────────────────────────────────────────────────────

    // An Act-III body's pressure intent is a blow and a violation, and the violation has to go through the
    // act's one filing point like every other — otherwise the Contrary Magpie standing beside it could not
    // contest a Trespass that arrives from an intent, which is most of them.
    //
    // The JSON entry keeps its own actions: those are what the TELEGRAPH is written from, so the player still
    // reads "10 dmg · Trespass +1" while the program that actually runs is this one.
    public static EffectProgram<EnemyActionContext>? Intent(string enemyId, string intentId) =>
        $"{enemyId}.{intentId}" switch
        {
            "permit_hare.check_the_permit" => Pressure(10),
            "mossbound_clerk.record_custom" => Pressure(10),
            "reckoning_hedge.measure_back" => Pressure(10),
            "errant_boundary_stone.move_the_marker" => Pressure(11),
            "hawthorn_tenant.enforce_the_plot" => Pressure(12),
            "foxglove_witness.testify" => Pressure(10),
            "contrary_magpie.contrary_cry" => Pressure(10),
            _ => null,
        };

    // A blow, and then a violation owed to whoever struck — no Local Law was broken, so nothing that asks
    // about laws answers this one.
    private static EffectProgram<EnemyActionContext> Pressure(int damage) =>
        new(new CausalSequenceEffectNode<EnemyActionContext>(
        [
            new DealDamageNode<EnemyActionContext>(
                Applicant, new ConstantExpression<EnemyActionContext>(damage)),
            FileTrespass<EnemyActionContext>(CombatantTargetSelectors.Source),
        ]));

    // ── shapes ────────────────────────────────────────────────────────────────────────────────────────────

    // Every Green Docket identity, and the rule the player carries into a fight with any of them: the act's
    // customs, plus the one Safe-Conduct a normal Act-III combat opens with.
    public static IReadOnlyList<StatusData> All() =>
    [
        Trespass(),
        SafeConduct(),
        Claim(),
        ClaimCreated(),
        GreenDocketCustoms(),
        NoHastyPassage(),
        FirstUseBecameCustom(),
        EveryDetourLeavesAStone(),
        DetourStone(),
        DetourLatch(),
        CurrentSurvey(),
        SurveyedThisTurn(),
        WanderingTitle(),
        PriorDispute(),
        RespectTheOccupiedPlot(),
        PlotEnforcedThisTurn(),
        PriorPossession(),
        ISawThatToo(),
        TestifiedThisTurn(),
        ContraryTestimony(),
        ContestedThisTurn(),
    ];

    // The standard roster, stage by stage. Anything in here is a Green Docket body, which is how a fight
    // knows to open under the act's customs.
    public static readonly IReadOnlySet<string> Identities = new HashSet<string>(StringComparer.Ordinal)
    {
        // Stage 1 — the Road of Permitted Turns
        "permit_hare", "mossbound_clerk", "cairn_of_stray_paths",
        // Stage 2 — the Surveyed Hedgerows
        "reckoning_hedge", "errant_boundary_stone", "hawthorn_tenant",
        // Stage 3 — the Meadow of Living Testimony
        "foxglove_witness", "contrary_magpie",
    };

    // What the player carries into a fight against any Green Docket body: the customs that turn three
    // Trespass into a Claim, and the one Safe-Conduct the act opens you with.
    //
    // Asked of the WHOLE roster and not of each body in turn, because the opening belongs to the act rather
    // than to any one identity — a duo of Green Docket bodies is still one road, and asking twice would hand
    // the player two safe conducts (Safe-Conduct is per-grant instances, so they would not even merge).
    public static IReadOnlyList<StartingStatusSpec> HeroOpening(IEnumerable<string> enemyIds) =>
        enemyIds.Any(Identities.Contains)
            ?
            [
                new StartingStatusSpec(new StatusDefinitionId(GreenDocketCustomsId), 1),
                new StartingStatusSpec(new StatusDefinitionId(SafeConductId), 1),
            ]
            : [];

    // ── addressing the parties ────────────────────────────────────────────────────────────────────────────
    //
    // Every one of these reads the whole field rather than "my allies" or "my enemies", and that is
    // deliberate. Which SIDE a selector means depends on whose action woke the rule — a Local Law fires on
    // the player's card play, the same law's consequences fire on an enemy's status application, and the
    // enemy team is "allies of the source" in one and "enemies of the source" in the other. A rule written
    // against the field is right in both, and the act has too many rules that fire from both sides to keep
    // two mirrored spellings of each.

    // The player: the one combatant every fight marks as the applicant.
    private static ICombatantTargetSelector Applicant { get; } =
        CombatantTargetSelectors.FirstTarget(
            CombatantTargetSelectors.WithStatus(
                CombatantTargetSelectors.AllAliveCombatants,
                new StatusDefinitionId(PassiveStatuses.ApplicantId)));

    // "The party whose law this is" — the living body carrying that law. FirstTarget because a scalar read
    // needs one combatant, and because two bodies never carry the same law.
    private static ICombatantTargetSelector Lawgiver(string lawId) =>
        CombatantTargetSelectors.FirstTarget(
            CombatantTargetSelectors.WithStatus(
                CombatantTargetSelectors.AllAliveCombatants, new StatusDefinitionId(lawId)));

    // ── filing a Trespass ─────────────────────────────────────────────────────────────────────────────────
    //
    // EVERY Trespass in the act is filed here — Local Laws, passives and the pressure intents alike — because
    // by Stage 3 two identities need the moment of filing itself rather than its result. The Contrary Magpie
    // rewrites who a violation is owed to BEFORE it lands (and only a landed source can be argued with, so it
    // cannot be a reaction), and the Foxglove Witness needs to know WHICH law was broken, which the Trespass
    // itself does not carry and the Magpie's rewriting would destroy in any case.
    //
    // So the filing writes down the law on the way past. `NoLaw` is a Trespass that is not a law violation at
    // all — a pressure intent, or a witness's own testimony — and nothing that asks about laws answers it.
    public const int NoLaw = 0;
    public const int HastyPassageLaw = 1;
    public const int CustomaryUseLaw = 2;
    public const int CurrentSurveyLaw = 3;
    public const int OccupiedPlotLaw = 4;

    // Which law is being broken by the violation currently being filed. Kept on the player because the player
    // is the one combatant every rule can address, and read again the instant the Trespass lands.
    public static CounterId LawBeingFiledCounter => new("law_being_filed");

    // One Trespass that is not a law violation — a pressure intent's blow, or a witness's own testimony.
    public static IEffectNode<TContext> FileTrespass<TContext>(ICombatantTargetSelector lawgiver)
        where TContext : class =>
        new CausalSequenceEffectNode<TContext>(
        [
            new SetCombatantCounterNode<TContext>(
                Applicant, LawBeingFiledCounter, new ConstantExpression<TContext>(NoLaw), relative: false),
            ContestedFiling<TContext>(lawgiver),
        ]);

    // A LAW is broken. Three separate things happen, and they are not the same thing:
    //
    //   the violation      — always, and it is what the witnesses in the meadow answer;
    //   the testimony      — a second violation of a law already heard this turn brings the Foxglove out,
    //                        BEFORE the law itself speaks, because the law's own filing is what teaches the
    //                        meadow which law it is listening for;
    //   the law's Trespass — once a turn per law, which is where `latch` comes in.
    //
    // That the last of those is capped and the first is not is the design's own reading: the Hedge punishes
    // one breach a turn, and the Foxglove is put beside it precisely so a SECOND breach of the same law still
    // costs the player something. A law that can only be broken once a turn to begin with (the Hare's third
    // card, the Clerk's opening card) passes no latch.
    public static IEffectNode<TContext> Violate<TContext>(
        ICombatantTargetSelector lawgiver, int law, string? latch = null)
        where TContext : class
    {
        IEffectNode<TContext> theLawSpeaks = latch is null
            ? ContestedFiling<TContext>(lawgiver)
            : new ConditionalEffectNode<TContext>(
                new ComparisonExpression<TContext>(
                    new CombatantStatusStacksExpression<TContext>(lawgiver, new StatusDefinitionId(latch)),
                    ComparisonOperator.Equal,
                    new ConstantExpression<TContext>(0)),
                new CausalSequenceEffectNode<TContext>(
                [
                    ContestedFiling<TContext>(lawgiver),
                    new ApplyStatusNode<TContext>(
                        lawgiver, new StatusDefinitionId(latch), new ConstantExpression<TContext>(1)),
                ]));

        return new CausalSequenceEffectNode<TContext>(
        [
            new SetCombatantCounterNode<TContext>(
                Applicant, LawBeingFiledCounter, new ConstantExpression<TContext>(law), relative: false),
            WitnessTestimony<TContext>(law),
            theLawSpeaks,
        ]);
    }

    // The Contrary Magpie's whole identity: the argument is never about whether the thing happened, only
    // about who gets to say they saw it. It has to sit INSIDE the filing rather than react to it, because a
    // violation that has already landed is owed to somebody, and the design is explicit that Safe-Conduct is
    // only offered against the source the Magpie leaves behind.
    private static IEffectNode<TContext> ContestedFiling<TContext>(ICombatantTargetSelector lawgiver)
        where TContext : class
    {
        var magpie = Lawgiver(ContraryTestimonyId);

        IEffectNode<TContext> Trespass(ICombatantTargetSelector owed) =>
            new ApplyStatusNode<TContext>(
                Applicant, new StatusDefinitionId(TrespassId), new ConstantExpression<TContext>(1),
                sourceSelector: owed);

        var mayContest = new AndExpression<TContext>(
            // There has to be a Magpie standing, and it has to have kept quiet so far this turn. With no
            // Magpie on the field the health read is zero and the whole question is false.
            new AndExpression<TContext>(
                new ComparisonExpression<TContext>(
                    new CombatantCurrentHealthExpression<TContext>(magpie),
                    ComparisonOperator.Greater,
                    new ConstantExpression<TContext>(0)),
                new ComparisonExpression<TContext>(
                    new CombatantStatusStacksExpression<TContext>(
                        magpie, new StatusDefinitionId(ContestedThisTurnId)),
                    ComparisonOperator.Equal,
                    new ConstantExpression<TContext>(0))),
            new AndExpression<TContext>(
                // It contests ANOTHER party's filing, never its own.
                new ComparisonExpression<TContext>(
                    new CombatantStatusStacksExpression<TContext>(
                        lawgiver, new StatusDefinitionId(ContraryTestimonyId)),
                    ComparisonOperator.Equal,
                    new ConstantExpression<TContext>(0)),
                // …and only where it has less standing than the party it is contradicting.
                new ComparisonExpression<TContext>(
                    new CombatantStatusStacksExpression<TContext>(magpie, new StatusDefinitionId(ClaimId)),
                    ComparisonOperator.Less,
                    new CombatantStatusStacksExpression<TContext>(lawgiver, new StatusDefinitionId(ClaimId)))));

        return new ConditionalEffectNode<TContext>(
            mayContest,
            new CausalSequenceEffectNode<TContext>(
            [
                Trespass(magpie),
                new ApplyStatusNode<TContext>(
                    magpie, new StatusDefinitionId(ContestedThisTurnId), new ConstantExpression<TContext>(1)),
            ]),
            Trespass(lawgiver));
    }

    // "Whoever's turn just started is the player" — how a rule that counts per PLAYER turn tells the player's
    // turn boundary from an enemy's, turns here belonging to combatants rather than to the table.
    private static ICombatExpression<TContext, bool> PlayersTurn<TContext>()
        where TContext : class =>
        new ComparisonExpression<TContext>(
            new CombatantStatusStacksExpression<TContext>(
                CombatantTargetSelectors.Source, new StatusDefinitionId(PassiveStatuses.ApplicantId)),
            ComparisonOperator.Greater,
            new ConstantExpression<TContext>(0));

    // An inert status: something the fight can see and other rules can ask about, carrying nothing itself.
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

    // A rule, not a resource: a status that exists only to carry triggers.
    private static StatusData Rule(
        string id, string name, string description, IReadOnlyList<StatusTriggerData> triggers) => new()
        {
            Id = id,
            NameKey = name,
            DescriptionKey = description,
            Polarity = StatusPolarity.Neutral,
            StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
            UsesStacks = false,
            Tags = [],
            PassiveModifiers = [],
            Triggers = triggers,
        };
}
