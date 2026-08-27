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
public static class ActThree
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
                    holder, new StatusDefinitionId(ClaimId), new ConstantExpression<TContext>(1)),
                new ApplyStatusNode<TContext>(
                    holder, new StatusDefinitionId(ClaimCreatedId), new ConstantExpression<TContext>(1)),
            ]));

    // ── Stage 1 — The Road of Permitted Turns ─────────────────────────────────────────────────────────────
    //
    // Each identity's Local Law is ONE status, carried by the party whose law it is, scoped Anywhere — so the
    // status is not the event's subject but only the rule's licence, and the rule fires on what the PLAYER
    // does. The status is also how the program finds its own author again: the acting source of a rule that
    // answers a card play is the player, and a Trespass filed in the player's name would never mature into
    // anybody's Claim, so every application names its lawgiver explicitly.

    public const string NoHastyPassageId = "no_hasty_passage";
    public const string FirstUseBecameCustomId = "first_use_became_custom";
    public const string EveryDetourLeavesAStoneId = "every_detour_leaves_a_stone";
    public const string DetourStoneId = "detour_stone";
    public const string DetourLatchId = "detour_recorded_this_turn";

    // Which card type the combat's first non-Junk play made customary: 1 Deed, 2 Working, 3 Rite. Kept on the
    // player, the one combatant every part of the program can address with a single selector.
    public static CounterId CustomaryUseCounter => new("customary_use");

    private const int DetourStonesPerClaim = 2;

    // "If the player plays a third card during a player turn: 1 Trespass. Once per player turn." The count is
    // its own latch — a turn passes through exactly three played cards once.
    public static StatusData NoHastyPassage()
    {
        var player = CombatantTargetSelectors.Source;

        var program = new EffectProgram<CardPlayedTriggeredEffectContext>(
            new ConditionalEffectNode<CardPlayedTriggeredEffectContext>(
                new ComparisonExpression<CardPlayedTriggeredEffectContext>(
                    new CardsPlayedThisTurnExpression<CardPlayedTriggeredEffectContext>(player),
                    ComparisonOperator.Equal,
                    new ConstantExpression<CardPlayedTriggeredEffectContext>(3)),
                FileTrespass<CardPlayedTriggeredEffectContext>(player, EnemyLawgiver(NoHastyPassageId))));

        return Rule(NoHastyPassageId, "No Hasty Passage",
            "The third card you play in a turn is a hasty passage: 1 Trespass owed to the Permit Hare.",
            [new StatusTriggerData("CardPlayed", JsonSerializer.SerializeToElement(
                program, CombatJson.CreateOptions<CardPlayedTriggeredEffectContext>()),
                StatusTriggerScope.Anywhere)]);
    }

    // "The first non-Junk card played in the combat establishes its card type as Customary Use. From then on,
    // a turn whose first non-Junk card is of another type is a Trespass."
    //
    // Once per turn without a latch: the rule only ever asks on the turn's FIRST non-Junk card, and a turn has
    // one of those. The same reading is why no turn gate is needed for "beginning with the next player turn" —
    // the card that sets the custom is itself the first non-Junk card of its turn, and it takes the other
    // branch.
    public static StatusData FirstUseBecameCustom()
    {
        var player = CombatantTargetSelectors.Source;
        var clerk = EnemyLawgiver(FirstUseBecameCustomId);

        // The custom has not been established yet: whatever type this card is becomes the procedure.
        IEffectNode<CardPlayedTriggeredEffectContext> Record(string tag, int type) =>
            new ConditionalEffectNode<CardPlayedTriggeredEffectContext>(
                new TriggerEventSourceCardHasTagExpression<CardPlayedTriggeredEffectContext>(new TagId(tag)),
                new SetCombatantCounterNode<CardPlayedTriggeredEffectContext>(
                    player, CustomaryUseCounter,
                    new ConstantExpression<CardPlayedTriggeredEffectContext>(type), relative: false));

        // The custom stands and this card departs from it.
        ICombatExpression<CardPlayedTriggeredEffectContext, bool> Departs(string tag, int type) =>
            new AndExpression<CardPlayedTriggeredEffectContext>(
                new ComparisonExpression<CardPlayedTriggeredEffectContext>(
                    new CombatantCounterExpression<CardPlayedTriggeredEffectContext>(player, CustomaryUseCounter),
                    ComparisonOperator.Equal,
                    new ConstantExpression<CardPlayedTriggeredEffectContext>(type)),
                new NotExpression<CardPlayedTriggeredEffectContext>(
                    new TriggerEventSourceCardHasTagExpression<CardPlayedTriggeredEffectContext>(new TagId(tag))));

        var body = new ConditionalEffectNode<CardPlayedTriggeredEffectContext>(
            new ComparisonExpression<CardPlayedTriggeredEffectContext>(
                new CombatantCounterExpression<CardPlayedTriggeredEffectContext>(player, CustomaryUseCounter),
                ComparisonOperator.Equal,
                new ConstantExpression<CardPlayedTriggeredEffectContext>(0)),
            new CausalSequenceEffectNode<CardPlayedTriggeredEffectContext>(
            [
                Record(Cards.CardAuthoring.DeedTag, 1),
                Record(Cards.CardAuthoring.WorkingTag, 2),
                Record(Cards.CardAuthoring.RiteTag, 3),
            ]),
            new ConditionalEffectNode<CardPlayedTriggeredEffectContext>(
                new OrExpression<CardPlayedTriggeredEffectContext>(
                    Departs(Cards.CardAuthoring.DeedTag, 1),
                    new OrExpression<CardPlayedTriggeredEffectContext>(
                        Departs(Cards.CardAuthoring.WorkingTag, 2),
                        Departs(Cards.CardAuthoring.RiteTag, 3))),
                FileTrespass<CardPlayedTriggeredEffectContext>(player, clerk)));

        // …and none of it is asked of a Junk card, or of any card after the turn's first real one.
        var program = new EffectProgram<CardPlayedTriggeredEffectContext>(
            new ConditionalEffectNode<CardPlayedTriggeredEffectContext>(
                new AndExpression<CardPlayedTriggeredEffectContext>(
                    new NotExpression<CardPlayedTriggeredEffectContext>(
                        new TriggerEventSourceCardHasTagExpression<CardPlayedTriggeredEffectContext>(
                            new TagId(Cards.CardAuthoring.JunkTag))),
                    new ComparisonExpression<CardPlayedTriggeredEffectContext>(
                        NonJunkPlayedThisTurn(player),
                        ComparisonOperator.Equal,
                        new ConstantExpression<CardPlayedTriggeredEffectContext>(1))),
                body));

        return Rule(FirstUseBecameCustomId, "The First Use Became Custom",
            "However this combat's first real card was played, that is the procedure. Open a later turn with "
            + "another kind of card and you owe the Mossbound Clerk 1 Trespass.",
            [new StatusTriggerData("CardPlayed", JsonSerializer.SerializeToElement(
                program, CombatJson.CreateOptions<CardPlayedTriggeredEffectContext>()),
                StatusTriggerScope.Anywhere)]);
    }

    // How many real cards have been played this turn — the Junk the fight hands you does not count as a use.
    private static ICombatExpression<CardPlayedTriggeredEffectContext, int> NonJunkPlayedThisTurn(
        ICombatantTargetSelector player) =>
        new SubtractExpression<CardPlayedTriggeredEffectContext>(
            new CardsPlayedThisTurnExpression<CardPlayedTriggeredEffectContext>(player),
            new CardsPlayedThisTurnWithTagExpression<CardPlayedTriggeredEffectContext>(
                player, new TagId(Cards.CardAuthoring.JunkTag)));

    // The Cairn is a support identity and reads the fight rather than pressing it: the first Trespass the
    // player ACTUALLY receives each turn from somebody else leaves a stone, and two stones become somebody
    // else's standing. Prevented Trespass leaves no stone, which falls out of watching StatusApplied — a
    // refused application never lands.
    public static StatusData EveryDetourLeavesAStone()
    {
        // A Trespass application: the source is the party that filed it, the event target is the player.
        var filer = CombatantTargetSelectors.Source;
        var cairn = AllyLawgiver(EveryDetourLeavesAStoneId);
        var somebodyElse = CombatantTargetSelectors.FirstTarget(
            CombatantTargetSelectors.Except(
                CombatantTargetSelectors.AllAlliesOfSource,
                CombatantTargetSelectors.AllAlliesOfSourceWithStatus(
                    new StatusDefinitionId(EveryDetourLeavesAStoneId))));

        var record = new CausalSequenceEffectNode<StatusAppliedTriggeredEffectContext>(
        [
            new ApplyStatusNode<StatusAppliedTriggeredEffectContext>(
                cairn, new StatusDefinitionId(DetourLatchId),
                new ConstantExpression<StatusAppliedTriggeredEffectContext>(1)),
            new ApplyStatusNode<StatusAppliedTriggeredEffectContext>(
                cairn, new StatusDefinitionId(DetourStoneId),
                new ConstantExpression<StatusAppliedTriggeredEffectContext>(1)),
            new ConditionalEffectNode<StatusAppliedTriggeredEffectContext>(
                new ComparisonExpression<StatusAppliedTriggeredEffectContext>(
                    new CombatantStatusStacksExpression<StatusAppliedTriggeredEffectContext>(
                        cairn, new StatusDefinitionId(DetourStoneId)),
                    ComparisonOperator.GreaterOrEqual,
                    new ConstantExpression<StatusAppliedTriggeredEffectContext>(DetourStonesPerClaim)),
                new CausalSequenceEffectNode<StatusAppliedTriggeredEffectContext>(
                [
                    new RemoveStatusNode<StatusAppliedTriggeredEffectContext>(
                        cairn, new StatusDefinitionId(DetourStoneId)),
                    CreateClaim<StatusAppliedTriggeredEffectContext>(somebodyElse),
                ])),
        ]);

        var program = new EffectProgram<StatusAppliedTriggeredEffectContext>(
            new ConditionalEffectNode<StatusAppliedTriggeredEffectContext>(
                new AndExpression<StatusAppliedTriggeredEffectContext>(
                    new TriggerEventStatusIsExpression<StatusAppliedTriggeredEffectContext>(
                        new StatusDefinitionId(TrespassId)),
                    new AndExpression<StatusAppliedTriggeredEffectContext>(
                        // …filed by somebody OTHER than the Cairn…
                        new ComparisonExpression<StatusAppliedTriggeredEffectContext>(
                            new CombatantStatusStacksExpression<StatusAppliedTriggeredEffectContext>(
                                filer, new StatusDefinitionId(EveryDetourLeavesAStoneId)),
                            ComparisonOperator.Equal,
                            new ConstantExpression<StatusAppliedTriggeredEffectContext>(0)),
                        // …and not already recorded this turn.
                        new ComparisonExpression<StatusAppliedTriggeredEffectContext>(
                            new CombatantStatusStacksExpression<StatusAppliedTriggeredEffectContext>(
                                cairn, new StatusDefinitionId(DetourLatchId)),
                            ComparisonOperator.Equal,
                            new ConstantExpression<StatusAppliedTriggeredEffectContext>(0)))),
                record));

        // The latch is released when the PLAYER's turn starts, so "the first time each player turn" means the
        // player's turns and not the Cairn's.
        var clear = new EffectProgram<TurnStartedTriggeredEffectContext>(
            new ConditionalEffectNode<TurnStartedTriggeredEffectContext>(
                new ComparisonExpression<TurnStartedTriggeredEffectContext>(
                    new CombatantStatusStacksExpression<TurnStartedTriggeredEffectContext>(
                        CombatantTargetSelectors.Source,
                        new StatusDefinitionId(PassiveStatuses.ApplicantId)),
                    ComparisonOperator.Greater,
                    new ConstantExpression<TurnStartedTriggeredEffectContext>(0)),
                new RemoveStatusNode<TurnStartedTriggeredEffectContext>(
                    EnemyLawgiver(EveryDetourLeavesAStoneId), new StatusDefinitionId(DetourLatchId))));

        return Rule(EveryDetourLeavesAStoneId, "Every Detour Leaves a Stone",
            "The first Trespass you actually take from somebody else each turn leaves the Cairn a stone. At 2 "
            + "stones the Cairn spends them, and another party gains a Claim.",
            [
                new StatusTriggerData("StatusApplied", JsonSerializer.SerializeToElement(
                    program, CombatJson.CreateOptions<StatusAppliedTriggeredEffectContext>()),
                    StatusTriggerScope.Anywhere),
                new StatusTriggerData("TurnStarted", JsonSerializer.SerializeToElement(
                    clear, CombatJson.CreateOptions<TurnStartedTriggeredEffectContext>()),
                    StatusTriggerScope.Anywhere),
            ]);
    }

    public static StatusData DetourStone() => new()
    {
        Id = DetourStoneId,
        NameKey = "Detour Stone",
        DescriptionKey = "One remembered departure from the sanctioned path. Two of them become somebody's Claim.",
        Polarity = StatusPolarity.Buff,
        StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
        UsesStacks = true,
        Tags = [],
        PassiveModifiers = [],
        Triggers = [],
    };

    public static StatusData DetourLatch() => new()
    {
        Id = DetourLatchId,
        NameKey = "Detour Recorded",
        DescriptionKey = "This turn's departure has already been written down.",
        Polarity = StatusPolarity.Neutral,
        StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
        UsesStacks = false,
        Tags = [],
        PassiveModifiers = [],
        Triggers = [],
    };

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
    ];

    // The standard roster, stage by stage. Anything in here is a Green Docket body, which is how a fight
    // knows to open under the act's customs.
    public static readonly IReadOnlySet<string> Identities = new HashSet<string>(StringComparer.Ordinal)
    {
        "permit_hare", "mossbound_clerk", "cairn_of_stray_paths",
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

    // "The party whose law this is", read from the player's side of the fight — a rule that answers a card
    // play has the player as its acting source. FirstTarget because a scalar read needs one combatant, and
    // because two bodies never carry the same law.
    private static ICombatantTargetSelector EnemyLawgiver(string lawId) =>
        CombatantTargetSelectors.FirstTarget(
            CombatantTargetSelectors.AllEnemiesOfSourceWithStatus(new StatusDefinitionId(lawId)));

    // The same party read from an ENEMY's side — a rule that answers something an enemy did.
    private static ICombatantTargetSelector AllyLawgiver(string lawId) =>
        CombatantTargetSelectors.FirstTarget(
            CombatantTargetSelectors.AllAlliesOfSourceWithStatus(new StatusDefinitionId(lawId)));

    // One Trespass, owed to the party whose law was broken and to nobody else.
    private static IEffectNode<TContext> FileTrespass<TContext>(
        ICombatantTargetSelector player, ICombatantTargetSelector lawgiver)
        where TContext : class =>
        new ApplyStatusNode<TContext>(
            player, new StatusDefinitionId(TrespassId), new ConstantExpression<TContext>(1),
            sourceSelector: lawgiver);

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
