using System.Text.Json;
using RogueDeck.Core.Combat;
using RogueDeck.Sandbox.Composition;
using RogueDeck.Scenario.Authoring;

namespace BnbContent.Converter;

// Act IV, boss — The Pharaoh of the Sealed Name. An undead ruler bearing three mutually incompatible royal
// names, each of them true because enough stone says so.
//
// Every name is a phase, and every phase opens behind a CARTOUCHE WARD of 36: while it stands the Pharaoh
// takes a fifth less from everything, and it is the only percentage reduction in the fight. The Ward is not
// a Block bar and cannot be beaten down — it is legitimacy, and legitimacy is taken away by RITUAL.
//
// So after your draw the reigning name issues one ROYAL COMMAND, always something this turn can actually do:
//
//   MEASURE THE THRONE        spend exactly 2.
//   LEAVE TRIBUTE             end the turn with exactly 1 Energy unspent.
//   SOUTHERN / NORTHERN       lead with a Deed / lead with a Working.
//   PURIFY THE ROYAL RECORD   end with less register and burial on you than you started with.
//
// Obey and the Ward drops 18. Take it to nothing and the NAME IS EXPOSED for a whole player turn: no Ward may
// re-form and everything you land goes a quarter further. Refuse, and the Ward heals 9 while the Pharaoh's
// AUTHORITY grows — up to four, two damage each, wiped at every name change.
//
// The player is therefore never obeying because they must. They are deciding, every turn, whether this
// particular command is worth less than the authority refusing it hands over.
public static partial class ActFour
{
    public const string PharaohEnemyId = "pharaoh_of_the_sealed_name";

    public const string SealedNameId = "the_sealed_name";
    public const string CartoucheWardId = "cartouche_ward";
    public const string AuthorityId = "royal_authority";
    public const string NameExposedId = "name_exposed";

    // The three names. Phase I is the absence of the other two — the throne name is simply who he is.
    public const string TwoLandsNameId = "the_name_of_the_two_lands";
    public const string EternalNameId = "the_eternal_name";

    // The commands, each a face on the player saying what this turn is asked to be.
    public const string CommandMeasureId = "command_measure_the_throne";
    public const string CommandTributeId = "command_leave_tribute";
    public const string CommandSouthId = "command_southern_precedence";
    public const string CommandNorthId = "command_northern_precedence";
    public const string CommandPurifyId = "command_purify_the_record";

    public const int WardFull = 36;
    public const int WardReformed = 18;
    public const int AuthorityCap = 4;
    private const int WardStripped = 18;
    private const int WardHealed = 9;
    private const int TwoLandsAt = 420;
    private const int EternalAt = 210;
    private const int SignatureAt = 90;

    // Which command is standing, how many the record has seen, and the register-and-burial total the player
    // began the turn with — the only way "lower than at the beginning of the turn" is answerable at its end.
    public static CounterId CommandStep => new("royal_command_step");
    public static CounterId RecordAtDawn => new("record_at_dawn");
    public static CounterId ExposedTurns => new("exposed_turns");
    public static CounterId NamesTaken => new("names_taken");

    private static readonly string[] Commands =
        [CommandMeasureId, CommandTributeId, CommandSouthId, CommandNorthId, CommandPurifyId];

    public static EffectProgram<EnemyActionContext>? PharaohIntent(string enemyId, string intentId) =>
        $"{enemyId}.{intentId}" switch
        {
            "pharaoh_of_the_sealed_name.speak_the_throne_name" => ByName(
                I: Seq(Debuff(InscribedId, 2)),
                II: Seq(Hit(29)),
                III: Seq(Debuff(EmbalmedId, 2), Guard(18))),
            "pharaoh_of_the_sealed_name.staff_of_the_two_lands" => ByName(
                I: Seq(Hit(26), Guard(18)),
                II: Seq(Guard(28), Debuff(WeighedId, 3)),
                III: Seq(Debuff(InscribedId, 2), Debuff(Cards.Keywords.Doubt, 1), Hit(16))),
            "pharaoh_of_the_sealed_name.those_not_counted_kneel" => ByName(
                I: Seq(Debuff(BurdenedId, 1), Debuff(Cards.Keywords.Paperwork, 2)),
                II: Seq(Debuff(EntombedId, 1), Debuff(BurdenedId, 1)),
                III: Seq(Debuff(EntombedId, 2))),
            "pharaoh_of_the_sealed_name.golden_sandal" => ByName(
                I: Seq(Hit(15), Hit(15)),
                II: Seq(Debuff(Cards.Keywords.Paperwork, 3), Hit(14)),
                III: Seq(Hit(36), Guard(20))),
            "pharaoh_of_the_sealed_name.royal_audience_ends" => ByName(
                I: Seq(Hit(32)),
                II: Seq(Hit(18), Hit(18)),
                III: Seq(Hit(32))),
            "pharaoh_of_the_sealed_name.all_three_names_are_mine" => AllThreeNames(),
            _ => null,
        };

    public static IReadOnlyList<StatusData> PharaohStatuses() =>
    [
        TheSealedName(),
        CartoucheWard(),
        RoyalAuthority(),
        NameExposed(),
        NameMarker(TwoLandsNameId, "The Name of the Two Lands",
            "The second of his names. South and north are both his, and he says so with both hands."),
        NameMarker(EternalNameId, "The Eternal Name",
            "The last of his names, and the one no stone was needed for."),
        Command(CommandMeasureId, "Royal Command: Measure the Throne",
            "Spend exactly 2 Energy this turn."),
        Command(CommandTributeId, "Royal Command: Leave Tribute",
            "End your turn with exactly 1 Energy unspent."),
        Command(CommandSouthId, "Royal Command: Southern Precedence",
            "Lead with a Deed — the first card you play this turn."),
        Command(CommandNorthId, "Royal Command: Northern Precedence",
            "Lead with a Working — the first card you play this turn."),
        Command(CommandPurifyId, "Royal Command: Purify the Royal Record",
            "End the turn carrying less Inscribed and Entombed together than you began it with."),
    ];

    // ── the ward, the authority, the exposure ─────────────────────────────────────────────────────────────

    // Legitimacy, not armour. It cannot be beaten off — only obeying strips it — and while any of it stands
    // the king is a fifth harder to hurt.
    public static StatusData CartoucheWard() => new()
    {
        Id = CartoucheWardId,
        NameKey = "Cartouche Ward",
        DescriptionKey =
            "The name in its ring. While any of it stands this king takes 20% less from you — and no blow "
            + "takes it off. Obeying a Royal Command strips 18.",
        Polarity = StatusPolarity.Buff,
        StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
        UsesStacks = true,
        Tags = [],
        PassiveModifiers =
        [
            new PassiveModifierData(PassiveModifierPipeline.DamageReceived,
                PassiveModifierOperation.ScalePercent, 80, RestrictDamageKind: null),
        ],
        Triggers = [],
    };

    public static StatusData RoyalAuthority() => new()
    {
        Id = AuthorityId,
        NameKey = "Authority",
        DescriptionKey =
            "Every command you refused. 2 more damage on each of his own blows, at most four — and gone the "
            + "moment he takes another name.",
        Polarity = StatusPolarity.Buff,
        StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
        UsesStacks = true,
        Tags = [],
        PassiveModifiers =
        [
            new PassiveModifierData(PassiveModifierPipeline.DamageDealt,
                PassiveModifierOperation.AddPerStack, 2, RestrictDamageKind: DamageKind.Direct),
        ],
        Triggers = [],
    };

    public static StatusData NameExposed() => new()
    {
        Id = NameExposedId,
        NameKey = "The Name Is Exposed",
        DescriptionKey =
            "The ring is empty. No Ward can form while this stands, and everything you land goes 25% further.",
        Polarity = StatusPolarity.Debuff,
        StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
        UsesStacks = false,
        Tags = [],
        PassiveModifiers =
        [
            new PassiveModifierData(PassiveModifierPipeline.DamageReceived,
                PassiveModifierOperation.ScalePercent, 125, RestrictDamageKind: null),
        ],
        Triggers = [],
    };

    private static StatusData NameMarker(string id, string name, string description) => new()
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

    private static StatusData Command(string id, string name, string description) =>
        NameMarker(id, name, description);

    // ── the rule ──────────────────────────────────────────────────────────────────────────────────────────

    public static StatusData TheSealedName() => new()
    {
        Id = SealedNameId,
        NameKey = "The Sealed Name",
        DescriptionKey =
            "Three names, three wards, and a command every turn. Obey and his legitimacy is stripped 18 at a "
            + "time; refuse and it heals 9 while his Authority grows. Take the ward to nothing and the name "
            + "stands exposed for a whole turn.",
        Polarity = StatusPolarity.Neutral,
        StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
        UsesStacks = false,
        Tags = [],
        PassiveModifiers = [],
        Triggers =
        [
            Trigger(IssueTheCommand(), nameof(TriggerEvent.TurnStarted), StatusTriggerScope.Anywhere),
            Trigger(JudgeTheCommand(), nameof(TriggerEvent.TurnEnded), StatusTriggerScope.Anywhere),
            Trigger(TakeTheNextName(), nameof(TriggerEvent.DamageTaken)),
        ],
    };

    // After the draw: the reigning name issues one command it can see the player can actually obey (§5.2),
    // and the exposure window is counted here too — one WHOLE player turn, then the ward re-forms at 18 if
    // the name has not changed in the meantime.
    private static EffectProgram<TurnStartedTriggeredEffectContext> IssueTheCommand()
    {
        var pharaoh = Bearer(SealedNameId);
        var step = new CombatantCounterExpression<TurnStartedTriggeredEffectContext>(pharaoh, CommandStep);

        ICombatExpression<TurnStartedTriggeredEffectContext, int> Carried(string statusId) =>
            new CombatantStatusStacksExpression<TurnStartedTriggeredEffectContext>(
                Applicant, new StatusDefinitionId(statusId));

        var record = new AddExpression<TurnStartedTriggeredEffectContext>(
            Carried(InscribedId), Carried(EntombedId));

        var energy = new CombatantCurrentResourceExpression<TurnStartedTriggeredEffectContext>(
            Applicant, StandardCombatIds.EnergyResource);

        // The command stands on HIM, not on the player: a command is the KING asking, and — see ADAPTATIONS —
        // a neutral rule-marker applied to the player is an application like any other, so the register would
        // enlarge it and spend an Inscribed doing it.
        IEffectNode<TurnStartedTriggeredEffectContext> Issue(string commandId) =>
            new ApplyStatusNode<TurnStartedTriggeredEffectContext>(
                pharaoh, new StatusDefinitionId(commandId),
                new ConstantExpression<TurnStartedTriggeredEffectContext>(1), sourceSelector: pharaoh);

        // Solvability, §5.2: a command is only issued when this turn's deterministic state can meet it.
        // Measure the Throne is the fallback everywhere because two Energy is what a turn opens with.
        var measurable = new ComparisonExpression<TurnStartedTriggeredEffectContext>(
            energy, ComparisonOperator.GreaterOrEqual,
            new ConstantExpression<TurnStartedTriggeredEffectContext>(2));

        var phaseThree = new ConditionalEffectNode<TurnStartedTriggeredEffectContext>(
            // Nothing to purify is nothing to ask for.
            new ComparisonExpression<TurnStartedTriggeredEffectContext>(
                record, ComparisonOperator.Greater,
                new ConstantExpression<TurnStartedTriggeredEffectContext>(0)),
            Issue(CommandPurifyId),
            Issue(CommandMeasureId));

        var phaseTwo = new ConditionalEffectNode<TurnStartedTriggeredEffectContext>(
            new ComparisonExpression<TurnStartedTriggeredEffectContext>(
                new RemainderExpression<TurnStartedTriggeredEffectContext>(
                    step, new ConstantExpression<TurnStartedTriggeredEffectContext>(2)),
                ComparisonOperator.Equal,
                new ConstantExpression<TurnStartedTriggeredEffectContext>(0)),
            Issue(CommandSouthId),
            Issue(CommandNorthId));

        var phaseOne = new ConditionalEffectNode<TurnStartedTriggeredEffectContext>(
            new AndExpression<TurnStartedTriggeredEffectContext>(
                measurable,
                new ComparisonExpression<TurnStartedTriggeredEffectContext>(
                    new RemainderExpression<TurnStartedTriggeredEffectContext>(
                        step, new ConstantExpression<TurnStartedTriggeredEffectContext>(2)),
                    ComparisonOperator.Equal,
                    new ConstantExpression<TurnStartedTriggeredEffectContext>(0))),
            Issue(CommandMeasureId),
            Issue(CommandTributeId));

        return new EffectProgram<TurnStartedTriggeredEffectContext>(
            new ConditionalEffectNode<TurnStartedTriggeredEffectContext>(
                PlayersTurn<TurnStartedTriggeredEffectContext>(),
                new CausalSequenceEffectNode<TurnStartedTriggeredEffectContext>(
                [
                    // The exposure is one WHOLE player turn. It is counted at the start of the turn it covers
                    // and closed at the start of the next, and the ward re-forms behind it.
                    new ConditionalEffectNode<TurnStartedTriggeredEffectContext>(
                        new TargetHasStatusExpression<TurnStartedTriggeredEffectContext>(
                            pharaoh, new StatusDefinitionId(NameExposedId)),
                        new ConditionalEffectNode<TurnStartedTriggeredEffectContext>(
                            new ComparisonExpression<TurnStartedTriggeredEffectContext>(
                                new CombatantCounterExpression<TurnStartedTriggeredEffectContext>(
                                    pharaoh, ExposedTurns),
                                ComparisonOperator.Greater,
                                new ConstantExpression<TurnStartedTriggeredEffectContext>(0)),
                            new CausalSequenceEffectNode<TurnStartedTriggeredEffectContext>(
                            [
                                new RemoveStatusNode<TurnStartedTriggeredEffectContext>(
                                    pharaoh, new StatusDefinitionId(NameExposedId)),
                                new SetCombatantCounterNode<TurnStartedTriggeredEffectContext>(
                                    pharaoh, ExposedTurns,
                                    new ConstantExpression<TurnStartedTriggeredEffectContext>(0),
                                    relative: false),
                                new ApplyStatusNode<TurnStartedTriggeredEffectContext>(
                                    pharaoh, new StatusDefinitionId(CartoucheWardId),
                                    new ConstantExpression<TurnStartedTriggeredEffectContext>(WardReformed),
                                    sourceSelector: pharaoh),
                            ]),
                            new SetCombatantCounterNode<TurnStartedTriggeredEffectContext>(
                                pharaoh, ExposedTurns,
                                new ConstantExpression<TurnStartedTriggeredEffectContext>(1),
                                relative: false))),

                    // Last turn's command is spent whether it was obeyed or not.
                    .. Commands.Select(id =>
                        (IEffectNode<TurnStartedTriggeredEffectContext>)
                        new RemoveStatusNode<TurnStartedTriggeredEffectContext>(
                            pharaoh, new StatusDefinitionId(id))),

                    // What the record stood at when the turn opened — the only way "lower than you began
                    // with" can be asked at its end.
                    new SetCombatantCounterNode<TurnStartedTriggeredEffectContext>(
                        pharaoh, RecordAtDawn, record, relative: false),

                    new ConditionalEffectNode<TurnStartedTriggeredEffectContext>(
                        new TargetHasStatusExpression<TurnStartedTriggeredEffectContext>(
                            pharaoh, new StatusDefinitionId(EternalNameId)),
                        phaseThree,
                        new ConditionalEffectNode<TurnStartedTriggeredEffectContext>(
                            new TargetHasStatusExpression<TurnStartedTriggeredEffectContext>(
                                pharaoh, new StatusDefinitionId(TwoLandsNameId)),
                            phaseTwo,
                            phaseOne)),

                    new SetCombatantCounterNode<TurnStartedTriggeredEffectContext>(
                        pharaoh, CommandStep,
                        new ConstantExpression<TurnStartedTriggeredEffectContext>(1), relative: true),
                ])));
    }

    // …and judged where every one of them is answerable: at the player's own turn end, off live state. None
    // of the five needs the measure's machinery, which is deliberate — a royal command is the KING asking,
    // and the act's measure belongs to the act.
    private static EffectProgram<TurnEndedTriggeredEffectContext> JudgeTheCommand()
    {
        var pharaoh = Bearer(SealedNameId);

        ICombatExpression<TurnEndedTriggeredEffectContext, int> Carried(string statusId) =>
            new CombatantStatusStacksExpression<TurnEndedTriggeredEffectContext>(
                Applicant, new StatusDefinitionId(statusId));

        ICombatExpression<TurnEndedTriggeredEffectContext, bool> Obeyed(
            string commandId, ICombatExpression<TurnEndedTriggeredEffectContext, bool> met) =>
            new AndExpression<TurnEndedTriggeredEffectContext>(
                new TargetHasStatusExpression<TurnEndedTriggeredEffectContext>(
                    pharaoh, new StatusDefinitionId(commandId)),
                met);

        var standing = new OrExpression<TurnEndedTriggeredEffectContext>(
            Obeyed(CommandMeasureId, new ComparisonExpression<TurnEndedTriggeredEffectContext>(
                new ResourceSpentThisTurnExpression<TurnEndedTriggeredEffectContext>(Applicant),
                ComparisonOperator.Equal, new ConstantExpression<TurnEndedTriggeredEffectContext>(2))),
            new OrExpression<TurnEndedTriggeredEffectContext>(
                Obeyed(CommandTributeId, new ComparisonExpression<TurnEndedTriggeredEffectContext>(
                    new CombatantCurrentResourceExpression<TurnEndedTriggeredEffectContext>(
                        Applicant, StandardCombatIds.EnergyResource),
                    ComparisonOperator.Equal, new ConstantExpression<TurnEndedTriggeredEffectContext>(1))),
                new OrExpression<TurnEndedTriggeredEffectContext>(
                    Obeyed(CommandSouthId,
                        new FirstCardPlayedThisTurnHasTagExpression<TurnEndedTriggeredEffectContext>(
                            Applicant, new TagId(Cards.CardAuthoring.DeedTag))),
                    new OrExpression<TurnEndedTriggeredEffectContext>(
                        Obeyed(CommandNorthId,
                            new FirstCardPlayedThisTurnHasTagExpression<TurnEndedTriggeredEffectContext>(
                                Applicant, new TagId(Cards.CardAuthoring.WorkingTag))),
                        Obeyed(CommandPurifyId, new ComparisonExpression<TurnEndedTriggeredEffectContext>(
                            new AddExpression<TurnEndedTriggeredEffectContext>(
                                Carried(InscribedId), Carried(EntombedId)),
                            ComparisonOperator.Less,
                            new CombatantCounterExpression<TurnEndedTriggeredEffectContext>(
                                pharaoh, RecordAtDawn)))))));

        var obeyed = new CausalSequenceEffectNode<TurnEndedTriggeredEffectContext>(
        [
            new ModifyStatusStacksNode<TurnEndedTriggeredEffectContext>(
                pharaoh, new StatusDefinitionId(CartoucheWardId),
                new ConstantExpression<TurnEndedTriggeredEffectContext>(-WardStripped)),

            // Stripped to nothing: the name stands exposed for the whole of the next player turn.
            new ConditionalEffectNode<TurnEndedTriggeredEffectContext>(
                new ComparisonExpression<TurnEndedTriggeredEffectContext>(
                    new CombatantStatusStacksExpression<TurnEndedTriggeredEffectContext>(
                        pharaoh, new StatusDefinitionId(CartoucheWardId)),
                    ComparisonOperator.LessOrEqual,
                    new ConstantExpression<TurnEndedTriggeredEffectContext>(0)),
                new CausalSequenceEffectNode<TurnEndedTriggeredEffectContext>(
                [
                    new ApplyStatusNode<TurnEndedTriggeredEffectContext>(
                        pharaoh, new StatusDefinitionId(NameExposedId),
                        new ConstantExpression<TurnEndedTriggeredEffectContext>(1), sourceSelector: pharaoh),
                    new SetCombatantCounterNode<TurnEndedTriggeredEffectContext>(
                        pharaoh, ExposedTurns,
                        new ConstantExpression<TurnEndedTriggeredEffectContext>(0), relative: false),
                ])),
        ]);

        var refused = new CausalSequenceEffectNode<TurnEndedTriggeredEffectContext>(
        [
            // The ward heals — but never while the name is exposed, and never past full.
            new ConditionalEffectNode<TurnEndedTriggeredEffectContext>(
                new AndExpression<TurnEndedTriggeredEffectContext>(
                    new NotExpression<TurnEndedTriggeredEffectContext>(
                        new TargetHasStatusExpression<TurnEndedTriggeredEffectContext>(
                            pharaoh, new StatusDefinitionId(NameExposedId))),
                    new ComparisonExpression<TurnEndedTriggeredEffectContext>(
                        new CombatantStatusStacksExpression<TurnEndedTriggeredEffectContext>(
                            pharaoh, new StatusDefinitionId(CartoucheWardId)),
                        ComparisonOperator.Less,
                        new ConstantExpression<TurnEndedTriggeredEffectContext>(WardFull))),
                new ApplyStatusNode<TurnEndedTriggeredEffectContext>(
                    pharaoh, new StatusDefinitionId(CartoucheWardId),
                    new MinExpression<TurnEndedTriggeredEffectContext>(
                        new ConstantExpression<TurnEndedTriggeredEffectContext>(WardHealed),
                        new SubtractExpression<TurnEndedTriggeredEffectContext>(
                            new ConstantExpression<TurnEndedTriggeredEffectContext>(WardFull),
                            new CombatantStatusStacksExpression<TurnEndedTriggeredEffectContext>(
                                pharaoh, new StatusDefinitionId(CartoucheWardId)))),
                    sourceSelector: pharaoh)),

            new ConditionalEffectNode<TurnEndedTriggeredEffectContext>(
                new ComparisonExpression<TurnEndedTriggeredEffectContext>(
                    new CombatantStatusStacksExpression<TurnEndedTriggeredEffectContext>(
                        pharaoh, new StatusDefinitionId(AuthorityId)),
                    ComparisonOperator.Less,
                    new ConstantExpression<TurnEndedTriggeredEffectContext>(AuthorityCap)),
                new ApplyStatusNode<TurnEndedTriggeredEffectContext>(
                    pharaoh, new StatusDefinitionId(AuthorityId),
                    new ConstantExpression<TurnEndedTriggeredEffectContext>(1), sourceSelector: pharaoh)),
        ]);

        // A turn on which no command stood at all is neither obeyed nor refused.
        var anyCommand = Commands
            .Select(id => (ICombatExpression<TurnEndedTriggeredEffectContext, bool>)
                new TargetHasStatusExpression<TurnEndedTriggeredEffectContext>(
                    pharaoh, new StatusDefinitionId(id)))
            .Aggregate((a, b) => new OrExpression<TurnEndedTriggeredEffectContext>(a, b));

        return new EffectProgram<TurnEndedTriggeredEffectContext>(
            new ConditionalEffectNode<TurnEndedTriggeredEffectContext>(
                new AndExpression<TurnEndedTriggeredEffectContext>(
                    PlayersTurn<TurnEndedTriggeredEffectContext>(), anyCommand),
                new ConditionalEffectNode<TurnEndedTriggeredEffectContext>(standing, obeyed, refused)));
    }

    // ── taking the next name ──────────────────────────────────────────────────────────────────────────────

    // The bands, read where a body's health changes. Each name is taken once, and taking one is not an
    // attack: the fight pauses, the ward re-forms whole, the authority he built under the old name is gone,
    // and the player is handed something to carry into the new one.
    private static EffectProgram<DamageReceivedTriggeredEffectContext> TakeTheNextName()
    {
        var pharaoh = Bearer(SealedNameId);
        var health = new CombatantCurrentHealthExpression<DamageReceivedTriggeredEffectContext>(pharaoh);

        IEffectNode<DamageReceivedTriggeredEffectContext> Take(
            int at, int taken, string nameId,
            IReadOnlyList<IEffectNode<DamageReceivedTriggeredEffectContext>> gifts) =>
            new ConditionalEffectNode<DamageReceivedTriggeredEffectContext>(
                new AndExpression<DamageReceivedTriggeredEffectContext>(
                    new ComparisonExpression<DamageReceivedTriggeredEffectContext>(
                        health, ComparisonOperator.LessOrEqual,
                        new ConstantExpression<DamageReceivedTriggeredEffectContext>(at)),
                    new ComparisonExpression<DamageReceivedTriggeredEffectContext>(
                        new CombatantCounterExpression<DamageReceivedTriggeredEffectContext>(
                            pharaoh, NamesTaken),
                        ComparisonOperator.Less,
                        new ConstantExpression<DamageReceivedTriggeredEffectContext>(taken))),
                new CausalSequenceEffectNode<DamageReceivedTriggeredEffectContext>(
                [
                    new SetCombatantCounterNode<DamageReceivedTriggeredEffectContext>(
                        pharaoh, NamesTaken,
                        new ConstantExpression<DamageReceivedTriggeredEffectContext>(taken), relative: false),

                    new RemoveStatusNode<DamageReceivedTriggeredEffectContext>(
                        pharaoh, new StatusDefinitionId(AuthorityId)),
                    new RemoveStatusNode<DamageReceivedTriggeredEffectContext>(
                        pharaoh, new StatusDefinitionId(NameExposedId)),
                    new RemoveStatusNode<DamageReceivedTriggeredEffectContext>(
                        pharaoh, new StatusDefinitionId(CartoucheWardId)),
                    new ApplyStatusNode<DamageReceivedTriggeredEffectContext>(
                        pharaoh, new StatusDefinitionId(CartoucheWardId),
                        new ConstantExpression<DamageReceivedTriggeredEffectContext>(WardFull),
                        sourceSelector: pharaoh),

                    new ApplyStatusNode<DamageReceivedTriggeredEffectContext>(
                        pharaoh, new StatusDefinitionId(nameId),
                        new ConstantExpression<DamageReceivedTriggeredEffectContext>(1),
                        sourceSelector: pharaoh),

                    .. gifts,
                ]));

        return new EffectProgram<DamageReceivedTriggeredEffectContext>(
            new CausalSequenceEffectNode<DamageReceivedTriggeredEffectContext>(
            [
                Take(TwoLandsAt, 1, TwoLandsNameId,
                [
                    new ApplyStatusNode<DamageReceivedTriggeredEffectContext>(
                        Applicant, new StatusDefinitionId(InscribedId),
                        new ConstantExpression<DamageReceivedTriggeredEffectContext>(1),
                        sourceSelector: pharaoh),
                    new GainBlockNode<DamageReceivedTriggeredEffectContext>(
                        pharaoh, new ConstantExpression<DamageReceivedTriggeredEffectContext>(14)),
                ]),

                Take(EternalAt, 2, EternalNameId,
                [
                    new ApplyStatusNode<DamageReceivedTriggeredEffectContext>(
                        Applicant, new StatusDefinitionId(EmbalmedId),
                        new ConstantExpression<DamageReceivedTriggeredEffectContext>(2),
                        sourceSelector: pharaoh),
                    new ApplyStatusNode<DamageReceivedTriggeredEffectContext>(
                        Applicant, new StatusDefinitionId(InscribedId),
                        new ConstantExpression<DamageReceivedTriggeredEffectContext>(1),
                        sourceSelector: pharaoh),
                ]),
            ]));
    }

    // ── the intents ───────────────────────────────────────────────────────────────────────────────────────

    // One slot, three meanings. The engine rotates ONE list, so a slot keeps its Phase-I name all fight and
    // the phase marker beside the telegraph is what says the king has become somebody else (BossPhases).
    private static EffectProgram<EnemyActionContext> ByName(
        IEffectNode<EnemyActionContext> I, IEffectNode<EnemyActionContext> II, IEffectNode<EnemyActionContext> III)
    {
        var self = CombatantTargetSelectors.Source;

        return new EffectProgram<EnemyActionContext>(
            new ConditionalEffectNode<EnemyActionContext>(
                new TargetHasStatusExpression<EnemyActionContext>(
                    self, new StatusDefinitionId(EternalNameId)),
                III,
                new ConditionalEffectNode<EnemyActionContext>(
                    new TargetHasStatusExpression<EnemyActionContext>(
                        self, new StatusDefinitionId(TwoLandsNameId)),
                    II,
                    I)));
    }

    // Every command refused, all at once — and then he has nothing left to be owed.
    private static EffectProgram<EnemyActionContext> AllThreeNames() =>
        new(new CausalSequenceEffectNode<EnemyActionContext>(
        [
            new DealDamageNode<EnemyActionContext>(
                Applicant,
                new MinExpression<EnemyActionContext>(
                    Const(42),
                    new AddExpression<EnemyActionContext>(
                        Const(34),
                        new MultiplyExpression<EnemyActionContext>(
                            Const(2),
                            new CombatantStatusStacksExpression<EnemyActionContext>(
                                CombatantTargetSelectors.Source, new StatusDefinitionId(AuthorityId)))))),

            new RemoveStatusNode<EnemyActionContext>(
                CombatantTargetSelectors.Source, new StatusDefinitionId(AuthorityId)),
        ]));

    private static IEffectNode<EnemyActionContext> Seq(params IEffectNode<EnemyActionContext>[] steps) =>
        new CausalSequenceEffectNode<EnemyActionContext>(steps);

    private static IEffectNode<EnemyActionContext> Hit(int damage) =>
        new DealDamageNode<EnemyActionContext>(Applicant, Const(damage));

    private static IEffectNode<EnemyActionContext> Guard(int block) =>
        new GainBlockNode<EnemyActionContext>(CombatantTargetSelectors.Source, Const(block));

    private static IEffectNode<EnemyActionContext> Debuff(string statusId, int stacks) =>
        new ApplyStatusNode<EnemyActionContext>(Applicant, new StatusDefinitionId(statusId), Const(stacks));
}
