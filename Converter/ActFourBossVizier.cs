using System.Text.Json;
using RogueDeck.Core.Combat;
using RogueDeck.Sandbox.Composition;
using RogueDeck.Scenario.Authoring;

namespace BnbContent.Converter;

// Act IV, boss — The Vizier of the King's Mouth. The king is not present. That does not matter: the Vizier
// speaks with his voice, and three subordinate OFFICES stand with him.
//
// This is the act's hierarchy fight, and the whole of it is a KILL ORDER. Each office does two things at
// once — it acts on its own account, and while it lives it lends the Vizier a function:
//
//   ROYAL SEAL BEARER (110)      the first affliction to reach you each round lands one stack larger;
//   KEEPER OF TALLIES (116)      every measure you miss files a sheet of Paperwork and buys him 8 Block;
//   CAPTAIN OF THE INNER STAIR (124)  his blows land for 6 more.
//
// Only ONE office acts per enemy turn — the acting office rotates, and the rotation is written on their
// bodies where the telegraph is — so three subordinates never become three full enemies. And every office
// still standing when the Vizier crosses 295 is ABSORBED: its function follows him into the second half
// permanently, at a slightly lower price. Defeated offices grant nothing.
//
// So the player decides the second half of the fight by deciding what to kill in the first, and pays for
// each decision in the turns it costs. Below 100 he says THE KING IS NOT HERE — 32, and 4 more for every
// office he swallowed — and the one mercy in it is that the blow hands you the means to silence one of
// those inherited functions for exactly one of his actions.
public static partial class ActFour
{
    public const string VizierEnemyId = "vizier_of_the_kings_mouth";
    public const string SealBearerEnemyId = "royal_seal_bearer";
    public const string KeeperOfTalliesEnemyId = "keeper_of_tallies";
    public const string CaptainOfTheStairEnemyId = "captain_of_the_inner_stair";

    public const string KingsMouthId = "the_kings_mouth";
    public const string RoyalOfficeId = "royal_office";
    public const string ActingOfficeId = "the_acting_office";

    public const string OfficeSealId = "office_of_the_royal_seal";
    public const string OfficeTalliesId = "office_of_the_tallies";
    public const string OfficeStairId = "office_of_the_inner_stair";

    // The three lent functions, read off the VIZIER and never off the office: an office's death takes its
    // function away, and an office's absorption leaves it standing. Two of the three are the same rule in
    // both halves and are therefore one status apiece; the Captain's is a passive modifier and a passive
    // modifier cannot read a phase, so its two sizes are two statuses and the transition swaps them.
    public const string RoyalImpressionId = "the_royal_impression";
    public const string CountedFailureId = "counted_failure";
    public const string ArmedAuthorityId = "armed_authority";
    public const string ArmedAuthorityInheritedId = "armed_authority_inherited";

    // The seal itself, worn by the PLAYER: one stack of amplification, renewed each round while the
    // impression is in force.
    public const string RoyalSealImpressedId = "royal_seal_impressed";

    public const string MouthOpensNextId = "the_mouth_opens_next";
    public const string MouthHasOpenedId = "the_mouth_has_opened";
    public const string KingNotHereId = "the_king_is_not_here";

    public const string SilencedSealId = "the_seal_is_silenced";
    public const string SilencedTallyId = "the_tally_is_silenced";
    public const string SilencedStairId = "the_stair_is_silenced";
    public const string SilenceTag = "kings_mouth_silence";

    private const int AuthorityPerOffice = 6;
    private const int MouthOpensAt = 295;
    private const int MouthOpensBlock = 16;
    private const int KingNotHereAt = 100;
    private const int KingNotHereBase = 32;
    private const int KingNotHerePerOffice = 4;
    private const int KingNotHereCap = 44;
    private const int CountedFailureBlock = 8;
    private const int ArmedAuthorityBonus = 6;
    private const int ArmedAuthorityInheritedBonus = 5;
    private const int AppointBlock = 12;
    private const int OfficeBlockForVizier = 10;

    // Which of its own three intents an office does next. One id, because a counter lives on the body that
    // owns it: all three offices keep their own step in it and none of them can read another's.
    public static CounterId OfficeStep => new("royal_office_step");

    // The Vizier's bookmark in the player's missed measures — the ordering-free "once per resolution" the
    // act has used since the Hungry Grain Thief.
    public static CounterId VizierTalliesRead => new("vizier_tallies_read");
    public static CounterId MouthTaken => new("kings_mouth_taken");
    public static CounterId KingNotHereTaken => new("king_is_not_here_taken");

    // The office list, in the order the rotation walks it: identity status, the function it lends, and the
    // sheet that silences that function once the Vizier has swallowed it.
    private static readonly (string Enemy, string Office, string Aura, string Silence, string Card, string Name)[]
        KingsOffices =
    [
        (SealBearerEnemyId, OfficeSealId, RoyalImpressionId, SilencedSealId,
            "silence_the_royal_seal", "Royal Seal"),
        (KeeperOfTalliesEnemyId, OfficeTalliesId, CountedFailureId, SilencedTallyId,
            "silence_the_tally", "Tally"),
        (CaptainOfTheStairEnemyId, OfficeStairId, ArmedAuthorityInheritedId, SilencedStairId,
            "silence_the_inner_stair", "Inner Stair"),
    ];

    public static EffectProgram<EnemyActionContext>? VizierIntent(string enemyId, string intentId) =>
        $"{enemyId}.{intentId}" switch
        {
            // ── the Vizier's rotating list. Slot six is the one that means two different things. ──────────
            "vizier_of_the_kings_mouth.speak_for_the_king" => ByMouth(
                I: Seq(Debuff(InscribedId, 2)),
                II: Seq(Debuff(InscribedId, 2), Hit(16))),
            "vizier_of_the_kings_mouth.no_petition_reaches_him" => ByMouth(
                I: Seq(Debuff(Cards.Keywords.Paperwork, 3), Guard(18)),
                II: Seq(Debuff(Cards.Keywords.Paperwork, 3), Guard(20))),
            "vizier_of_the_kings_mouth.return_to_your_place" => ByMouth(
                I: Seq(Debuff(BurdenedId, 2), Hit(15)),
                II: Seq(Debuff(BurdenedId, 2), Hit(18))),
            "vizier_of_the_kings_mouth.the_stair_is_forbidden" => ByMouth(
                I: Seq(Hit(26), Debuff(EntombedId, 1)),
                II: Seq(Hit(28), Debuff(EntombedId, 1))),
            "vizier_of_the_kings_mouth.royal_words_mortal_bones" => ByMouth(
                I: Seq(Hit(34)),
                II: Seq(Hit(35))),
            "vizier_of_the_kings_mouth.appoint_the_acting_office" => ByMouth(
                I: AppointTheActingOffice(),
                II: TheMouthRequiresNoKing()),

            // ── the two specials, reached only by his own intent rules ────────────────────────────────────
            "vizier_of_the_kings_mouth.the_kings_mouth_opens" => TheKingsMouthOpens(),
            "vizier_of_the_kings_mouth.the_king_is_not_here" => TheKingIsNotHere(),

            // ── the offices ───────────────────────────────────────────────────────────────────────────────
            "royal_seal_bearer.display_the_seal" => OfficeAct(Seq(Debuff(InscribedId, 1))),
            "royal_seal_bearer.impressed_edge" => OfficeAct(Seq(Hit(14))),
            "royal_seal_bearer.hold_the_royal_mark" => OfficeAct(ShieldTheMouth()),

            "keeper_of_tallies.count_again" => OfficeAct(
                Seq(new ApplyStatusNode<EnemyActionContext>(
                    Applicant, new StatusDefinitionId(WeighedId),
                    Achievable<EnemyActionContext>(2)))),
            "keeper_of_tallies.reed_tally_strike" => OfficeAct(Seq(Hit(12))),
            "keeper_of_tallies.enter_the_shortfall" => OfficeAct(
                Seq(Debuff(Cards.Keywords.Paperwork, 2))),

            "captain_of_the_inner_stair.hold_the_stair" => OfficeAct(Seq(Hit(18))),
            "captain_of_the_inner_stair.drive_them_down" => OfficeAct(
                Seq(Hit(14), Debuff(BurdenedId, 1))),
            "captain_of_the_inner_stair.shield_the_mouth" => OfficeAct(ShieldTheMouth()),

            _ => null,
        };

    public static IReadOnlyList<StatusData> VizierStatuses() =>
    [
        TheKingsMouth(),
        RoyalOffice(),
        TheActingOffice(),
        TheRoyalImpression(),
        RoyalSealImpressed(),
        CountedFailure(),
        ArmedAuthority(ArmedAuthorityId, "Armed Authority", ArmedAuthorityBonus,
            "The Captain of the Inner Stair is standing. The Vizier's blows land for 6 more."),
        ArmedAuthority(ArmedAuthorityInheritedId, "Armed Authority (Inherited)", ArmedAuthorityInheritedBonus,
            "The Captain was swallowed rather than beaten. The Vizier's blows land for 5 more, for good."),
        TheMouthOpensNext(),
        TheMouthHasOpened(),
        TheKingIsNotHereAnnounced(),
        .. KingsOffices.Select(o => Silenced(o.Silence, o.Name, o.Aura)),
        Office(OfficeSealId, "Office: Royal Seal", RoyalImpressionId,
            "The Royal Seal Bearer's warrant. While this office stands, the first affliction to reach you "
            + "each round lands one stack larger."),
        Office(OfficeTalliesId, "Office: Tallies", CountedFailureId,
            "The Keeper of Tallies' warrant. While this office stands, every measure you miss files a sheet "
            + "and buys the Vizier Block."),
        Office(OfficeStairId, "Office: Inner Stair", ArmedAuthorityId,
            "The Captain of the Inner Stair's warrant. While this office stands, the Vizier's blows land "
            + "for 6 more."),
    ];

    public static IReadOnlyList<CardData> VizierSilenceCards() =>
        [.. KingsOffices.Select(o => SilenceCard(o.Card, o.Name, o.Silence, o.Aura))];

    // ── the offices, as warrants ──────────────────────────────────────────────────────────────────────────

    // An office's identity, and the only place its death can be heard. The function it lends is worn by the
    // VIZIER; what the warrant does when its holder falls is take that function off him. Absorption is the
    // deliberate opposite: a body set aside is REMOVED and not downed, so this never fires for it and the
    // function stays — which is the whole of §12.6 in one sentence.
    private static StatusData Office(string id, string name, string auraId, string description) => new()
    {
        Id = id,
        NameKey = name,
        DescriptionKey = description + " Kill it and the Vizier loses it; leave it standing when the mouth "
            + "opens and he keeps it for good.",
        Polarity = StatusPolarity.Neutral,
        StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
        UsesStacks = false,
        Tags = [],
        PassiveModifiers = [],
        Triggers =
        [
            Trigger(new EffectProgram<CombatantDownedTriggeredEffectContext>(
                new RemoveStatusNode<CombatantDownedTriggeredEffectContext>(
                    Bearer(KingsMouthId), new StatusDefinitionId(auraId))),
                nameof(TriggerEvent.Downed)),
        ],
    };

    private static StatusData RoyalOffice() => new()
    {
        Id = RoyalOfficeId,
        NameKey = "Royal Office",
        DescriptionKey =
            "A subordinate of the King's Mouth. The Vizier gains 6 Block at the start of your turn for each "
            + "office still standing, and only one office acts per turn.",
        Polarity = StatusPolarity.Neutral,
        StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
        UsesStacks = false,
        Tags = [],
        PassiveModifiers = [],
        Triggers = [],
    };

    // The rotation, written where the telegraph is. It is not a phase, but it is the same kind of fact and
    // it is read at the same moment: it says which of three bodies the coming enemy turn belongs to.
    private static StatusData TheActingOffice() => new()
    {
        Id = ActingOfficeId,
        NameKey = "Acting Office",
        DescriptionKey = "This office acts on the coming enemy turn. The others stand at the Vizier's "
            + "shoulder and do nothing.",
        Polarity = StatusPolarity.Neutral,
        StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
        UsesStacks = false,
        Tags = [],
        PassiveModifiers = [],
        Triggers = [],
    };

    // ── the three lent functions ──────────────────────────────────────────────────────────────────────────

    private static StatusData TheRoyalImpression() => new()
    {
        Id = RoyalImpressionId,
        NameKey = "Royal Impression",
        DescriptionKey =
            "The seal is in force: once each round, the next affliction to reach you lands one stack larger.",
        Polarity = StatusPolarity.Neutral,
        StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
        UsesStacks = false,
        Tags = [],
        PassiveModifiers = [],
        Triggers = [],
    };

    // The seal as the player wears it. Deliberately the act's OWN amplification machinery rather than a
    // bespoke rule: Inscribed already means "the next application to you is larger", and a seal stamped on a
    // petitioner is the same sentence said by somebody else. One stack, renewed at the top of each of the
    // player's turns while the impression stands — which is what "once per round" comes to.
    private static StatusData RoyalSealImpressed() => new()
    {
        Id = RoyalSealImpressedId,
        NameKey = "Royal Seal",
        DescriptionKey = "Stamped. The next affliction applied to you lands one stack larger, and the seal "
            + "is spent doing it.",
        Polarity = StatusPolarity.Debuff,
        StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
        UsesStacks = true,
        Tags = [],
        PassiveModifiers = [],
        Triggers = [],
        Amplification = new StatusAmplificationData(
            StatusAmplificationScope.Debuffs, AddStacks: 1, StacksSpent: 1),
    };

    private static StatusData CountedFailure() => new()
    {
        Id = CountedFailureId,
        NameKey = "Counted Failure",
        DescriptionKey =
            "Every measure you miss is entered against you: 1 Paperwork, and 8 Block for the Vizier while "
            + "the Keeper still stands to write it down.",
        Polarity = StatusPolarity.Neutral,
        StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
        UsesStacks = false,
        Tags = [],
        PassiveModifiers = [],
        Triggers = [],
    };

    private static StatusData ArmedAuthority(string id, string name, int bonus, string description) => new()
    {
        Id = id,
        NameKey = name,
        DescriptionKey = description,
        Polarity = StatusPolarity.Buff,
        StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
        UsesStacks = false,
        Tags = [],
        PassiveModifiers =
        [
            new PassiveModifierData(
                PassiveModifierPipeline.DamageDealt, PassiveModifierOperation.AddFlat, bonus),
        ],
        Triggers = [],
    };

    // Silence, and the one shape a suppression can have when what it suppresses is a passive modifier: a
    // passive modifier cannot ask whether it is silenced, so the silence carries the opposite modifier and
    // the two cancel. The other two functions are programs and simply ask.
    //
    // It lasts until the end of the VIZIER's next turn, which is what "until after his next action" means on
    // a body that acts once a turn — and the trigger is on his own turn end, so the player's turn in between
    // does not spend it.
    private static StatusData Silenced(string id, string name, string auraId) => new()
    {
        Id = id,
        NameKey = $"Silenced: {name}",
        DescriptionKey = $"The {name} office says nothing until the Vizier's next action is over.",
        Polarity = StatusPolarity.Debuff,
        StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
        UsesStacks = false,
        Tags = [],
        PassiveModifiers = auraId == ArmedAuthorityInheritedId
            ?
            [
                new PassiveModifierData(
                    PassiveModifierPipeline.DamageDealt, PassiveModifierOperation.AddFlat,
                    -ArmedAuthorityInheritedBonus),
            ]
            : [],
        Triggers =
        [
            Trigger(new EffectProgram<TurnEndedTriggeredEffectContext>(
                new RemoveStatusNode<TurnEndedTriggeredEffectContext>(
                    CombatantTargetSelectors.Source, new StatusDefinitionId(id))),
                nameof(TriggerEvent.TurnEnded)),
        ],
    };

    // ── the phases ────────────────────────────────────────────────────────────────────────────────────────

    private static StatusData TheMouthOpensNext() => new()
    {
        Id = MouthOpensNextId,
        NameKey = "The King's Mouth Opens",
        DescriptionKey =
            "His next action is no attack at all: every office still standing is absorbed, and what it does "
            + "becomes his for the rest of the fight.",
        Polarity = StatusPolarity.Neutral,
        StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
        UsesStacks = false,
        Tags = [],
        PassiveModifiers = [],
        Triggers = [],
    };

    private static StatusData TheMouthHasOpened() => new()
    {
        Id = MouthHasOpenedId,
        NameKey = "The King's Mouth",
        DescriptionKey =
            "There are no offices left to kill. Everything he swallowed he keeps, and every one of his "
            + "sayings has grown a blow on the end of it.",
        Polarity = StatusPolarity.Neutral,
        StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
        UsesStacks = false,
        Tags = [],
        PassiveModifiers = [],
        Triggers = [],
    };

    private static StatusData TheKingIsNotHereAnnounced() => new()
    {
        Id = KingNotHereId,
        NameKey = "The King Is Not Here",
        DescriptionKey =
            "32, and 4 more for every office he swallowed. It hands you the sheets to silence one of them "
            + "for exactly one action afterwards.",
        Polarity = StatusPolarity.Neutral,
        StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
        UsesStacks = false,
        Tags = [],
        PassiveModifiers = [],
        Triggers = [],
    };

    // ── the rule ──────────────────────────────────────────────────────────────────────────────────────────

    public static StatusData TheKingsMouth() => new()
    {
        Id = KingsMouthId,
        NameKey = "The King's Mouth",
        DescriptionKey =
            "Three offices stand with him. Only one acts each turn, each lends him a function while it "
            + "lives, and every one still standing at 295 is absorbed for good.",
        Polarity = StatusPolarity.Neutral,
        StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
        UsesStacks = false,
        Tags = [],
        PassiveModifiers = [],
        Triggers =
        [
            // ONE turn-start program with both halves inside it. Two triggers on one event have no order
            // content may rely on, and these two do have an order: authority descends and the rotation turns
            // at the top of the player's turn, the tallies are read at the top of his own.
            Trigger(TheTurnTurns(), nameof(TriggerEvent.TurnStarted), StatusTriggerScope.Anywhere),
            Trigger(TheFailsafes(), nameof(TriggerEvent.DamageTaken)),
        ],
    };

    private static EffectProgram<TurnStartedTriggeredEffectContext> TheTurnTurns()
    {
        var vizier = Bearer(KingsMouthId);
        var officesStanding = new CountTargetsExpression<TurnStartedTriggeredEffectContext>(
            CombatantTargetSelectors.WithStatus(
                CombatantTargetSelectors.AllAliveCombatants, new StatusDefinitionId(RoyalOfficeId)));

        // §12.3 Authority Descends, the rotation, and the seal — all three are things the player must see
        // BEFORE they spend a turn, so all three happen at the top of theirs.
        var theirTurn = new CausalSequenceEffectNode<TurnStartedTriggeredEffectContext>(
        [
            new GainBlockNode<TurnStartedTriggeredEffectContext>(
                vizier,
                new MultiplyExpression<TurnStartedTriggeredEffectContext>(
                    new ConstantExpression<TurnStartedTriggeredEffectContext>(AuthorityPerOffice),
                    officesStanding)),

            AdvanceTheRotation<TurnStartedTriggeredEffectContext>(),

            // The impression is renewed, never stacked: one seal a round is the whole of §12.1.
            new ConditionalEffectNode<TurnStartedTriggeredEffectContext>(
                new AndExpression<TurnStartedTriggeredEffectContext>(
                    new TargetHasStatusExpression<TurnStartedTriggeredEffectContext>(
                        vizier, new StatusDefinitionId(RoyalImpressionId)),
                    new AndExpression<TurnStartedTriggeredEffectContext>(
                        new NotExpression<TurnStartedTriggeredEffectContext>(
                            new TargetHasStatusExpression<TurnStartedTriggeredEffectContext>(
                                vizier, new StatusDefinitionId(SilencedSealId))),
                        new NotExpression<TurnStartedTriggeredEffectContext>(
                            new TargetHasStatusExpression<TurnStartedTriggeredEffectContext>(
                                Applicant, new StatusDefinitionId(RoyalSealImpressedId))))),
                new ApplyStatusNode<TurnStartedTriggeredEffectContext>(
                    Applicant, new StatusDefinitionId(RoyalSealImpressedId),
                    new ConstantExpression<TurnStartedTriggeredEffectContext>(1), sourceSelector: vizier)),
        ]);

        // §12.1 Counted Failure. The measure resolves at the end of the player's turn and he acts first in
        // the enemy phase, so his turn start is the first moment that can answer it — and the bookmark makes
        // it exactly once per resolution however many turns he was busy.
        var missed = SinceLastLooked<TurnStartedTriggeredEffectContext>(vizier, MeasuresFailed, VizierTalliesRead);

        var hisTurn = new CausalSequenceEffectNode<TurnStartedTriggeredEffectContext>(
        [
            new ConditionalEffectNode<TurnStartedTriggeredEffectContext>(
                new AndExpression<TurnStartedTriggeredEffectContext>(
                    new TargetHasStatusExpression<TurnStartedTriggeredEffectContext>(
                        vizier, new StatusDefinitionId(CountedFailureId)),
                    new AndExpression<TurnStartedTriggeredEffectContext>(
                        new NotExpression<TurnStartedTriggeredEffectContext>(
                            new TargetHasStatusExpression<TurnStartedTriggeredEffectContext>(
                                vizier, new StatusDefinitionId(SilencedTallyId))),
                        new ComparisonExpression<TurnStartedTriggeredEffectContext>(
                            missed, ComparisonOperator.Greater,
                            new ConstantExpression<TurnStartedTriggeredEffectContext>(0)))),
                new CausalSequenceEffectNode<TurnStartedTriggeredEffectContext>(
                [
                    new ApplyStatusNode<TurnStartedTriggeredEffectContext>(
                        Applicant, new StatusDefinitionId(Cards.Keywords.Paperwork),
                        new ConstantExpression<TurnStartedTriggeredEffectContext>(1), sourceSelector: vizier),

                    // The Keeper's Block half is the office's, not the inheritance's: once he has swallowed
                    // it he files the sheet and nothing more (§12.6).
                    new ConditionalEffectNode<TurnStartedTriggeredEffectContext>(
                        new NotExpression<TurnStartedTriggeredEffectContext>(
                            new TargetHasStatusExpression<TurnStartedTriggeredEffectContext>(
                                vizier, new StatusDefinitionId(MouthHasOpenedId))),
                        new GainBlockNode<TurnStartedTriggeredEffectContext>(
                            vizier,
                            new ConstantExpression<TurnStartedTriggeredEffectContext>(CountedFailureBlock))),
                ])),

            // The bookmark moves whether or not anything was filed: what has already been looked at is
            // looked at once.
            MoveTheBookmark<TurnStartedTriggeredEffectContext>(vizier, MeasuresFailed, VizierTalliesRead),
        ]);

        return new EffectProgram<TurnStartedTriggeredEffectContext>(
            new ConditionalEffectNode<TurnStartedTriggeredEffectContext>(
                PlayersTurn<TurnStartedTriggeredEffectContext>(), theirTurn, hisTurn));
    }

    // 295 opens the mouth and 100 announces the signature. Neither happens here — both are written on him as
    // a telegraph, and his own intent rules pick the action up next turn. §5.4: a transition the player
    // cannot see coming is the same problem one turn earlier.
    private static EffectProgram<DamageReceivedTriggeredEffectContext> TheFailsafes()
    {
        var vizier = Bearer(KingsMouthId);
        var health = new CombatantCurrentHealthExpression<DamageReceivedTriggeredEffectContext>(vizier);

        ICombatExpression<DamageReceivedTriggeredEffectContext, bool> At(int band) =>
            new ComparisonExpression<DamageReceivedTriggeredEffectContext>(
                health, ComparisonOperator.LessOrEqual,
                new ConstantExpression<DamageReceivedTriggeredEffectContext>(band));

        ICombatExpression<DamageReceivedTriggeredEffectContext, bool> NotYet(CounterId taken) =>
            new ComparisonExpression<DamageReceivedTriggeredEffectContext>(
                new CombatantCounterExpression<DamageReceivedTriggeredEffectContext>(vizier, taken),
                ComparisonOperator.Equal,
                new ConstantExpression<DamageReceivedTriggeredEffectContext>(0));

        IEffectNode<DamageReceivedTriggeredEffectContext> Announce(CounterId taken, string markerId) =>
            new CausalSequenceEffectNode<DamageReceivedTriggeredEffectContext>(
            [
                new SetCombatantCounterNode<DamageReceivedTriggeredEffectContext>(
                    vizier, taken, new ConstantExpression<DamageReceivedTriggeredEffectContext>(1),
                    relative: false),
                new ApplyStatusNode<DamageReceivedTriggeredEffectContext>(
                    vizier, new StatusDefinitionId(markerId),
                    new ConstantExpression<DamageReceivedTriggeredEffectContext>(1), sourceSelector: vizier),
            ]);

        return new EffectProgram<DamageReceivedTriggeredEffectContext>(
            new CausalSequenceEffectNode<DamageReceivedTriggeredEffectContext>(
            [
                new ConditionalEffectNode<DamageReceivedTriggeredEffectContext>(
                    new AndExpression<DamageReceivedTriggeredEffectContext>(
                        At(MouthOpensAt), NotYet(MouthTaken)),
                    Announce(MouthTaken, MouthOpensNextId)),

                new ConditionalEffectNode<DamageReceivedTriggeredEffectContext>(
                    new AndExpression<DamageReceivedTriggeredEffectContext>(
                        At(KingNotHereAt), NotYet(KingNotHereTaken)),
                    Announce(KingNotHereTaken, KingNotHereId)),
            ]));
    }

    // ── the rotation ──────────────────────────────────────────────────────────────────────────────────────

    // "The acting Office rotates through the LIVING Office list." Written as what it is: the token is taken
    // off whoever holds it and handed to the first office still standing after them, wrapping round. Three
    // orders, one per holder, and the fourth reading — nobody holds it, which is how the fight opens — takes
    // the same order as the last. An office that died holding it simply falls through to that default, which
    // is the same answer by a shorter road.
    private static IEffectNode<TContext> AdvanceTheRotation<TContext>() where TContext : class
    {
        ICombatantTargetSelector Standing(string officeId) =>
            CombatantTargetSelectors.FirstTarget(
                CombatantTargetSelectors.WithStatus(
                    CombatantTargetSelectors.AllAliveCombatants, new StatusDefinitionId(officeId)));

        ICombatExpression<TContext, bool> Holds(string officeId) =>
            new TargetExistsExpression<TContext>(
                CombatantTargetSelectors.WithStatus(
                    CombatantTargetSelectors.WithStatus(
                        CombatantTargetSelectors.AllAliveCombatants,
                        new StatusDefinitionId(ActingOfficeId)),
                    new StatusDefinitionId(officeId)));

        IEffectNode<TContext> HandTo(int index, params string[] order) =>
            index >= order.Length
                ? new NoOpEffectNode<TContext>()
                : new ConditionalEffectNode<TContext>(
                    new TargetExistsExpression<TContext>(Standing(order[index])),
                    new ApplyStatusNode<TContext>(
                        Standing(order[index]), new StatusDefinitionId(ActingOfficeId),
                        new ConstantExpression<TContext>(1)),
                    HandTo(index + 1, order));

        IEffectNode<TContext> GiveTo(params string[] order) =>
            new CausalSequenceEffectNode<TContext>(
            [
                new ForEachTargetEffectNode<TContext>(
                    CombatantTargetSelectors.WithStatus(
                        CombatantTargetSelectors.AllAliveCombatants,
                        new StatusDefinitionId(ActingOfficeId)),
                    new RemoveStatusNode<TContext>(
                        CombatantTargetSelectors.IterationTarget,
                        new StatusDefinitionId(ActingOfficeId))),
                HandTo(0, order),
            ]);

        return new ConditionalEffectNode<TContext>(
            Holds(OfficeSealId),
            GiveTo(OfficeTalliesId, OfficeStairId, OfficeSealId),
            new ConditionalEffectNode<TContext>(
                Holds(OfficeTalliesId),
                GiveTo(OfficeStairId, OfficeSealId, OfficeTalliesId),
                GiveTo(OfficeSealId, OfficeTalliesId, OfficeStairId)));
    }

    // ── the intents ───────────────────────────────────────────────────────────────────────────────────────

    private static EffectProgram<EnemyActionContext> ByMouth(
        IEffectNode<EnemyActionContext> I, IEffectNode<EnemyActionContext> II) =>
        new(new ConditionalEffectNode<EnemyActionContext>(
            new TargetHasStatusExpression<EnemyActionContext>(
                CombatantTargetSelectors.Source, new StatusDefinitionId(MouthHasOpenedId)),
            II, I));

    // An office does its thing and then turns its own page. The step is a counter on the acting body, so
    // three offices keep three steps in one id and none of them can read another's — and an office standing
    // at the Vizier's shoulder never reaches this, which is what keeps its list in order.
    private static EffectProgram<EnemyActionContext> OfficeAct(IEffectNode<EnemyActionContext> act) =>
        new(new CausalSequenceEffectNode<EnemyActionContext>(
        [
            act,
            new SetCombatantCounterNode<EnemyActionContext>(
                CombatantTargetSelectors.Source, OfficeStep,
                new RemainderExpression<EnemyActionContext>(
                    new AddExpression<EnemyActionContext>(
                        new CombatantCounterExpression<EnemyActionContext>(
                            CombatantTargetSelectors.Source, OfficeStep),
                        Const(1)),
                    Const(3)),
                relative: false),
        ]));

    private static IEffectNode<EnemyActionContext> ShieldTheMouth() =>
        new GainBlockNode<EnemyActionContext>(Bearer(KingsMouthId), Const(OfficeBlockForVizier));

    // Phase I, slot six: the rotation is turned by hand and whoever it lands on is armoured for it. The
    // Vizier acts first in the enemy phase, so the office he appoints is the office that acts next.
    private static IEffectNode<EnemyActionContext> AppointTheActingOffice() =>
        new CausalSequenceEffectNode<EnemyActionContext>(
        [
            AdvanceTheRotation<EnemyActionContext>(),
            new GainBlockNode<EnemyActionContext>(
                CombatantTargetSelectors.FirstTarget(
                    CombatantTargetSelectors.WithStatus(
                        CombatantTargetSelectors.AllAliveCombatants,
                        new StatusDefinitionId(ActingOfficeId))),
                Const(AppointBlock)),
        ]);

    // Phase II, the same slot: there is nobody left to appoint, so he says so.
    private static IEffectNode<EnemyActionContext> TheMouthRequiresNoKing() =>
        new CausalSequenceEffectNode<EnemyActionContext>(
        [
            new ApplyStatusNode<EnemyActionContext>(
                Applicant, new StatusDefinitionId(WeighedId), Achievable<EnemyActionContext>(3)),
            Debuff(Cards.Keywords.Doubt, 1),
            Guard(16),
        ]);

    // §12.5. Every office still standing is set aside rather than killed — REMOVED and not downed, so no
    // warrant hears a death and no function is taken off him. What the Captain lent shrinks by one on the
    // way in, because a swallowed office is worth slightly less than a living one.
    private static EffectProgram<EnemyActionContext> TheKingsMouthOpens()
    {
        var vizier = CombatantTargetSelectors.Source;
        var standing = CombatantTargetSelectors.WithStatus(
            CombatantTargetSelectors.AllAliveCombatants, new StatusDefinitionId(RoyalOfficeId));

        return new EffectProgram<EnemyActionContext>(
            new CausalSequenceEffectNode<EnemyActionContext>(
            [
                new ConditionalEffectNode<EnemyActionContext>(
                    new TargetHasStatusExpression<EnemyActionContext>(
                        vizier, new StatusDefinitionId(ArmedAuthorityId)),
                    new CausalSequenceEffectNode<EnemyActionContext>(
                    [
                        new RemoveStatusNode<EnemyActionContext>(
                            vizier, new StatusDefinitionId(ArmedAuthorityId)),
                        new ApplyStatusNode<EnemyActionContext>(
                            vizier, new StatusDefinitionId(ArmedAuthorityInheritedId), Const(1),
                            sourceSelector: vizier),
                    ])),

                new ApplyStatusNode<EnemyActionContext>(
                    vizier, new StatusDefinitionId(MouthHasOpenedId), Const(1), sourceSelector: vizier),
                new RemoveStatusNode<EnemyActionContext>(
                    vizier, new StatusDefinitionId(MouthOpensNextId)),
                new GainBlockNode<EnemyActionContext>(vizier, Const(MouthOpensBlock)),

                new ForEachTargetEffectNode<EnemyActionContext>(
                    standing,
                    new RemoveStatusNode<EnemyActionContext>(
                        CombatantTargetSelectors.IterationTarget,
                        new StatusDefinitionId(ActingOfficeId))),
                new SetCombatantLifecycleStateNode<EnemyActionContext>(
                    standing, CombatantLifecycleState.Removed),
            ]));
    }

    // The signature, and the counterplay the audit asked for: the blow is what hands the player the sheets.
    // Only the offices he actually swallowed are worth silencing, so only those sheets are laid.
    private static EffectProgram<EnemyActionContext> TheKingIsNotHere()
    {
        var vizier = CombatantTargetSelectors.Source;

        ICombatExpression<EnemyActionContext, int> Inherited(string auraId) =>
            new MinExpression<EnemyActionContext>(
                new CombatantStatusStacksExpression<EnemyActionContext>(
                    vizier, new StatusDefinitionId(auraId)),
                Const(1));

        // A function is worn without stacks, so "is it there" has to be asked as a presence and turned into
        // a number: 1 per office, capped at 44 for all three.
        var swallowed = new AddExpression<EnemyActionContext>(
            Inherited(RoyalImpressionId),
            new AddExpression<EnemyActionContext>(
                Inherited(CountedFailureId),
                Inherited(ArmedAuthorityInheritedId)));

        IEffectNode<EnemyActionContext> Offer(string auraId, string cardId) =>
            new ConditionalEffectNode<EnemyActionContext>(
                new TargetHasStatusExpression<EnemyActionContext>(
                    vizier, new StatusDefinitionId(auraId)),
                new CreateCardInstanceNode<EnemyActionContext>(
                    Applicant, new CardDefinitionId(cardId), CardZone.Hand, Const(1)));

        return new EffectProgram<EnemyActionContext>(
            new CausalSequenceEffectNode<EnemyActionContext>(
            [
                new DealDamageNode<EnemyActionContext>(
                    Applicant,
                    new MinExpression<EnemyActionContext>(
                        Const(KingNotHereCap),
                        new AddExpression<EnemyActionContext>(
                            Const(KingNotHereBase),
                            new MultiplyExpression<EnemyActionContext>(
                                Const(KingNotHerePerOffice), swallowed)))),

                .. KingsOffices.Select(o => Offer(o.Aura, o.Card)),

                new RemoveStatusNode<EnemyActionContext>(
                    vizier, new StatusDefinitionId(KingNotHereId)),
            ]));
    }

    // ── silence, as cards ─────────────────────────────────────────────────────────────────────────────────

    private static CardData SilenceCard(string id, string name, string silenceId, string auraId)
    {
        var vizier = Bearer(KingsMouthId);

        return new CardData
        {
            Id = id,
            NameKey = $"Silence the {name}",
            DescriptionKey =
                $"The {name} office says nothing until the Vizier's next action is over. One office only — "
                + "the sheets are gone at the end of the turn either way.",
            Costs = [],
            Tags = [new TagId(SilenceTag), new TagId(Cards.CardAuthoring.TemporaryTag)],
            Program = new EffectProgram<CardPlayContext>(
                new ConditionalEffectNode<CardPlayContext>(
                    new AndExpression<CardPlayContext>(
                        new TargetHasStatusExpression<CardPlayContext>(
                            vizier, new StatusDefinitionId(auraId)),
                        // One silence at a time: a second sheet in the same turn is a dead sheet.
                        new NotExpression<CardPlayContext>(
                            new OrExpression<CardPlayContext>(
                                new TargetHasStatusExpression<CardPlayContext>(
                                    vizier, new StatusDefinitionId(SilencedSealId)),
                                new OrExpression<CardPlayContext>(
                                    new TargetHasStatusExpression<CardPlayContext>(
                                        vizier, new StatusDefinitionId(SilencedTallyId)),
                                    new TargetHasStatusExpression<CardPlayContext>(
                                        vizier, new StatusDefinitionId(SilencedStairId)))))),
                    new ApplyStatusNode<CardPlayContext>(
                        vizier, new StatusDefinitionId(silenceId),
                        new ConstantExpression<CardPlayContext>(1),
                        sourceSelector: CombatantTargetSelectors.Source))),
            PlayedCardDestinationZone = CardZone.ExhaustPile,
            TurnEndHandDestinationZone = CardZone.ExhaustPile,
        };
    }
}
