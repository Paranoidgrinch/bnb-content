using RogueDeck.Core.Combat;
using RogueDeck.Sandbox.Composition;
using RogueDeck.Scenario.Authoring;

namespace BnbContent.Converter;

// ACT V, the fifth god — UTU, WITNESS OF EVERY OATH. He witnesses, and that is the whole of what he does.
//
// The other four gods DO something to the fight: Nisaba writes your future, Inanna takes your cards, Nanshe
// rations your turn, Nanna-Sin makes it come round again. Utu takes nothing and forbids nothing. He asks you
// to promise something, pays you for the promise ON THE SPOT, and then watches. His fight asks (§10 core
// fantasy):
//
//     What are you willing to promise when you know the world itself will hold you to your word?
//
// ── THE SUN DOES NOT FORBID (§10.2) ──────────────────────────────────────────────────────────────────────
// This is his defining rule and the one thing about him that must never be softened. A card that breaks an
// Oath REMAINS PLAYABLE. It resolves, in full, exactly as it would have. Then Witness rises by one and he
// says "Seen." Nothing in this file refuses a play, raises a cost, or removes a card from a hand — because a
// god who stops you is a god who decided for you, and then there was no promise to keep.
//
// What the engine cannot do is warn on the card face before the click. So the warning is a CHIP, restated
// after every single play: THE NEXT ONE BREAKS IT stands on the player the moment the allowance is spent,
// and THE OATH IS NOT YET KEPT stands whenever ending the turn now would be a breach. That is §10.2's
// "THIS BREAKS: Oath of Restraint" in the only place this game has to write it, and it is written every time
// the state changes rather than once when the vow is sworn.
//
// ── WITNESS (§10.3) ──────────────────────────────────────────────────────────────────────────────────────
// One counter, on him, that only ever grows. It is never spent, never consumed, never removed. Every one of
// his moves reads it, so a breach is not a slap for that turn — it is a permanently louder fight. That is
// the whole answer to "swear the biggest Favor and break it immediately", and it is why the Favor can be
// paid up front without the fight collapsing.
//
// ── THE THREE SKIES ──────────────────────────────────────────────────────────────────────────────────────
// His phases are the sun's height, and they are read off his own blood (§10.8 / §10.9 / §10.12):
//
//     100–70 %   Beneath the Morning Sun   one-turn Oaths        he learns nothing, you learn the rules
//      70–35 %   The Sun at Zenith         two-turn Oaths        + NO SHADOW: a card more, every turn
//        ≤ 35 %  The Final Oath            ONE Oath, until death the Favor is enormous, the vow is forever
//
// Each threshold is crossed once and answers with one move of its own, queued on a counter his intent rules
// read — Cross the Open Land at the Zenith, Nothing Is Hidden at the Final Oath — so the sky changing is an
// event on the table and not a number that quietly moved.
//
// ── THE ORACLE DIRECTOR (§10.4), AND WHY IT IS ONE CONDITION ─────────────────────────────────────────────
// "Oaths must be technically achievable. Utu never wins through semantic trickery." Two of the nine vows ask
// the player to DO something — play a Deed, deal damage — and a hand with no Deed in it can do neither. So
// the middle of the three offers is guarded: with a Deed in hand he asks for the deed, and without one he
// asks for the Straight Path instead, which any hand can keep by not playing. One condition, read at the
// moment of the offer, and the labels never say a thing the hand cannot do.
//
// ── WHAT THIS FIGHT DOES NOT HAVE ────────────────────────────────────────────────────────────────────────
// §10.11's No Shadow shows the top three cards of the draw pile. The engine can mark them and the frontend
// draws no draw pile, so the sight would be a sight nobody is given — the exact failure the phase-marker work
// was done to stop. It is honoured as what it is FOR (the harder Oath is matched by more planning material):
// a card more at the start of every turn from the Zenith on, which turns an unknown card into a known one.
// Written down in ADAPTATIONS.md rather than quietly dropped.
public static partial class ActFive
{
    public const string UtuEnemyId = "utu_witness_of_every_oath";
    public const string UtuEncounterId = "act_5_utu_witness_of_every_oath";

    // His rule, worn from the first bell. Every trigger below hangs off it.
    public const string OathsBeneathTheSunId = "oaths_beneath_the_sun";

    // What he keeps, and it is the only thing he keeps.
    public const string WitnessId = "witness";

    // The three skies.
    public const string MorningSunId = "beneath_the_morning_sun";
    public const string ZenithId = "the_sun_at_zenith";
    public const string FinalOathId = "the_final_oath";

    // What the player wears while a vow stands, and what the sun says about it.
    public const string OathStandsId = "the_oath_stands";
    public const string FavorOfTheSunId = "the_favor_of_the_sun";
    public const string NoShadowId = "no_shadow";
    public const string AtTheEdgeId = "the_next_one_breaks_it";
    public const string NotYetKeptId = "the_oath_is_not_yet_kept";
    public const string SeenId = "seen";

    // What he wears when a Great Oath has been broken: his next move is Judgment, and everyone can see it.
    public const string JudgmentId = "what_was_witnessed";

    // ── the nine vows, and the three fallbacks ────────────────────────────────────────────────────────────
    public const string RestraintId = "oath_of_restraint";
    public const string ResolveId = "oath_of_resolve";
    public const string OathOpenHandId = "oath_of_the_open_hand";
    public const string StraightPathId = "oath_of_the_straight_path";

    public const string LongRestraintId = "oath_of_long_restraint";
    public const string CompletionId = "oath_of_completion";
    public const string OathOpenHandsId = "oath_of_the_open_hands";
    public const string LongPathId = "oath_of_the_long_path";

    public const string ShallBeRestrainedId = "i_shall_be_restrained";
    public const string ShallFinishId = "i_shall_finish_what_i_begin";
    public const string ShallSpendId = "i_shall_leave_nothing_unspent";
    public const string ShallWalkStraightId = "i_shall_walk_straight";

    // 660. Below Nanna-Sin and Inanna, above Nanshe: a player who keeps his Oaths is being PAID every turn
    // to fight him, and a fight that pays the player is a fight that should end sooner than one that does not.
    public const int UtuMaxHealth = 660;

    private const int ZenithAt = 70;             // §10.9
    private const int FinalAt = 35;              // §10.12

    // WHAT THE RISING SUN ADDS, AND WHY IT IS WITNESS RATHER THAN A NUMBER. Every one of his moves is
    // "so much, and so much more for each Witness", and the telegraph says exactly that — so a flat bonus
    // for the later skies would be damage the intent line does not mention, which is the one thing this act
    // forbids. It is also the wrong fiction: the sun climbing is precisely the shadows going, so the sky
    // gives him what a sky can give him. He SEES more.
    private const int ZenithWitness = 2;
    private const int FinalWitness = 3;

    // Counters. The plays and the damage are the two things a vow can be MEASURED in across more than one
    // turn; the last type is what the Straight Path is read against; the rest are one-shot flags his intent
    // rules read, because a move that answers an event has to be chosen by the same state the event wrote.
    private static CounterId PlaysUnderOath => new("utu_plays");
    private static CounterId DamageUnderOath => new("utu_damage");
    private static CounterId LastType => new("utu_last_type");
    private static CounterId JudgmentDue => new("utu_judgment");
    private static CounterId ZenithDue => new("utu_zenith_due");
    private static CounterId NoonDue => new("utu_noon_due");
    private static CounterId ZenithTaken => new("utu_zenith_taken");
    private static CounterId NoonTaken => new("utu_noon_taken");
    private static CounterId Spoken => new("utu_spoken");

    // Which round his morning has already happened in. ⚠ CardsDrawn is not "the turn began" — it is "cards
    // arrived", and a card that draws cards fires it again in the middle of the turn. Without this, a Favor
    // would be paid twice for one promise, and a turn whose Oath had just been broken would be offered
    // another one on the spot. The offer is a MORNING, and there is one of those per round.
    private static CounterId Dawn => new("utu_dawn");

    private static ICombatantTargetSelector Sun => Bearer(OathsBeneathTheSunId);

    // What a vow asks of the turn it covers. Five kinds, and every one of them is measurable from state the
    // engine already keeps — which is why this god needed no engine purchase either.
    private enum Vow
    {
        Restraint,   // play no more than N
        Deed,        // play at least one Deed
        Spend,       // end the turn with exactly 0 Energy
        Damage,      // deal at least N
        Straight,    // never the same card type twice in a row
    }

    // What the Favor is. Everything but Power is paid EVERY turn the vow stands; Power is a standing status
    // and is paid once, at the swearing, or it would double on the second morning of a two-turn Oath.
    private enum Gift { Draw, Block, Energy, Power, EnergyAndDraw }

    // One vow. `Turns` is how many player turns it covers — 1 for a morning Oath, 2 under the Zenith, and
    // ZERO for a Great Oath, which is not a countdown at all: it stands until he dies, and it is measured
    // again from scratch every single turn.
    private sealed record Oath(
        string Id, string Name, int Turns, Vow Asks, int Number, Gift Gives, int Amount,
        string Text, string Label);

    private static bool Cumulative(Oath oath) => oath.Turns >= 2;
    private static bool Great(Oath oath) => oath.Turns == 0;

    private static readonly Oath[] Oaths =
    [
        // ── Beneath the Morning Sun: one turn, small numbers, and the player learns what a breach costs.
        new(RestraintId, "Oath of Restraint", 1, Vow.Restraint, 4, Gift.Draw, 2,
            "Sworn: no more than 4 cards this turn. The fifth is legal, and it is seen.",
            "Oath of Restraint — draw 2 now; play no more than 4 cards this turn"),
        new(ResolveId, "Oath of Resolve", 1, Vow.Deed, 1, Gift.Block, 18,
            "Sworn: at least one Deed before this turn ends. Ending it without one is seen.",
            "Oath of Resolve — 18 Block now; play at least one Deed this turn"),
        new(OathOpenHandId, "Oath of the Open Hand", 1, Vow.Spend, 0, Gift.Energy, 2,
            "Sworn: end this turn with exactly 0 Energy. A single Energy left standing is seen.",
            "Oath of the Open Hand — 2 Energy now; end the turn with exactly 0 Energy"),
        new(StraightPathId, "Oath of the Straight Path", 1, Vow.Straight, 0, Gift.Energy, 1,
            "Sworn: never the same kind of card twice in a row. Deed after Deed, Working after Working, "
            + "Rite after Rite — each is legal, and each is seen.",
            "Oath of the Straight Path — 1 Energy now; never play the same kind of card twice in a row"),

        // ── The Sun at Zenith: the vow outlives the turn, so what it counts has to as well.
        new(LongRestraintId, "Oath of Long Restraint", 2, Vow.Restraint, 8, Gift.Draw, 2,
            "Sworn: no more than 8 cards across these two turns, and two cards drawn on each of them. The "
            + "ninth is legal, and it is seen.",
            "Oath of Long Restraint — draw 2 each turn; play no more than 8 cards across two turns"),
        new(CompletionId, "Oath of Completion", 2, Vow.Damage, 60, Gift.Power, 4,
            "Sworn: 60 damage before these two turns are over, and everything you do hits for 4 more while "
            + "you try. Falling short is seen.",
            "Oath of Completion — +4 damage on everything; deal 60 damage across two turns"),
        new(OathOpenHandsId, "Oath of the Open Hands", 2, Vow.Spend, 0, Gift.Energy, 2,
            "Sworn: end EACH of these two turns with exactly 0 Energy, and take 2 Energy on each of them. "
            + "A single Energy left standing on either is seen.",
            "Oath of the Open Hands — 2 Energy each turn; end both turns with exactly 0 Energy"),
        new(LongPathId, "Oath of the Long Path", 2, Vow.Straight, 0, Gift.Energy, 1,
            "Sworn: never the same kind of card twice in a row for two turns — and the last card of the "
            + "first turn is still the last card when the second begins.",
            "Oath of the Long Path — 1 Energy each turn; never the same kind twice in a row, across both turns"),

        // ── The Final Oath: no countdown. It is measured again every turn until one of you is finished.
        new(ShallBeRestrainedId, "I Shall Be Restrained", 0, Vow.Restraint, 5, Gift.Energy, 2,
            "Spoken once, and standing until he falls: no more than 5 cards on any turn, and 2 Energy on "
            + "every one of them.",
            "I SHALL BE RESTRAINED — 2 Energy every turn; play no more than 5 cards each turn"),
        new(ShallFinishId, "I Shall Finish What I Begin", 0, Vow.Damage, 40, Gift.Power, 6,
            "Spoken once, and standing until he falls: 40 damage every turn, and everything you do hits for "
            + "6 more.",
            "I SHALL FINISH WHAT I BEGIN — +6 damage on everything; deal 40 damage every turn"),
        new(ShallSpendId, "I Shall Leave Nothing Unspent", 0, Vow.Spend, 0, Gift.EnergyAndDraw, 1,
            "Spoken once, and standing until he falls: an Energy and a card every turn, and every turn ends "
            + "with exactly 0 Energy.",
            "I SHALL LEAVE NOTHING UNSPENT — 1 Energy and 1 card every turn; end every turn with 0 Energy"),
        new(ShallWalkStraightId, "I Shall Walk Straight", 0, Vow.Straight, 0, Gift.EnergyAndDraw, 1,
            "Spoken once, and standing until he falls: an Energy and a card every turn, and never the same "
            + "kind of card twice in a row for the rest of the fight.",
            "I SHALL WALK STRAIGHT — 1 Energy and 1 card every turn; never the same kind twice in a row"),
    ];

    private static Oath Vowed(string id) => Oaths.First(o => o.Id == id);

    // The three that are offered under each sky. The middle slot is the one the Director guards: with a Deed
    // in hand it asks for the deed, without one it asks for the Straight Path, which any hand can keep.
    private static (Oath Restraint, Oath Asked, Oath Fallback, Oath Spend) OfferedUnder(string sky) => sky switch
    {
        ZenithId => (Vowed(LongRestraintId), Vowed(CompletionId), Vowed(LongPathId), Vowed(OathOpenHandsId)),
        FinalOathId => (Vowed(ShallBeRestrainedId), Vowed(ShallFinishId), Vowed(ShallWalkStraightId),
            Vowed(ShallSpendId)),
        _ => (Vowed(RestraintId), Vowed(ResolveId), Vowed(StraightPathId), Vowed(OathOpenHandId)),
    };

    // ── what the act hands to the rest of the converter ───────────────────────────────────────────────────

    public static IReadOnlyList<StatusData> UtuStatuses() =>
    [
        TheOathsBeneathTheSun(),
        Face(WitnessId, "Witness",
            "What he has seen. It is never spent, never removed and never forgiven, and every move he makes "
            + "reads it. One breach, one Witness — for the rest of the fight, and the climbing sun sees "
            + "more of its own accord.", stacks: true),
        Face(MorningSunId, "Beneath the Morning Sun",
            "The first sky. Each Oath covers one turn, and he asks again every morning.", stacks: false),
        Face(ZenithId, "The Sun at Zenith",
            "The second sky. One moment is easy to govern — so an Oath now spans two turns, and nothing is "
            + "hidden while it does.", stacks: false),
        Face(FinalOathId, "The Final Oath",
            "The last sky. He has heard enough small promises: you speak once, and what you speak stands "
            + "until he falls.", stacks: false),
        Face(OathStandsId, "The Oath Stands",
            "Turns this vow still covers. When the last one ends, what you swore is measured.", stacks: true),
        Face(FavorOfTheSunId, "The Favor of the Sun",
            "Paid in advance, on the strength of your word. Everything you do hits for this much more while "
            + "the Oath stands — and it goes the moment the Oath does.", stacks: true) with
        {
            PassiveModifiers =
            [
                new PassiveModifierData(
                    PassiveModifierPipeline.DamageDealt, PassiveModifierOperation.AddPerStack, 1,
                    RestrictDamageKind: null),
            ],
        },
        Face(NoShadowId, "No Shadow",
            "Nothing relevant to a promise is hidden from you. One card more at the start of every turn, "
            + "for the rest of the fight.", stacks: false) with
        {
            PassiveModifiers =
            [
                new PassiveModifierData(
                    PassiveModifierPipeline.TurnStartDraw, PassiveModifierOperation.AddPerStack, 1,
                    RestrictDamageKind: null),
            ],
        },
        Face(AtTheEdgeId, "The Next One Breaks It",
            "You have spent the whole of what you swore. The next card is legal — and it is seen.",
            stacks: false),
        Face(NotYetKeptId, "The Oath Is Not Yet Kept",
            "What you swore has not yet been done. End the turn now and he sees it.", stacks: false),
        Face(SeenId, "Seen",
            "He saw it. The Witness beside him is one higher, and it stays there.", stacks: false),
        Face(JudgmentId, "What Was Witnessed",
            "The Final Oath is broken. His next move is Pronounce What Was Witnessed, and it is read off "
            + "the Witness — he is not invulnerable while it is coming.", stacks: false),
        .. Oaths.Select(o => Face(o.Id, o.Name, o.Text, stacks: false)),
    ];

    public static EffectProgram<EnemyActionContext>? UtuIntent(string enemyId, string intentId) =>
        enemyId != UtuEnemyId ? null : intentId switch
        {
            "rise_between_the_mountains" => Sunlight(Strike(Radiant(24, 2))),
            "raise_the_saw" => Sunlight(Seq(Strike(Radiant(13, 1)), Strike(Radiant(13, 1)))),
            "call_the_witness" => Sunlight(Seq(Guard(24), Strike(Radiant(10, 5)))),
            // The Zenith's own move, fired once, on the turn the sky changes.
            "cross_the_open_land" => Sunlight(Seq(
                Spend(ZenithDue), Strike(Radiant(34, 3)), Debuff(Cards.Keywords.Doubt, 2))),
            // The Final Oath's own move.
            //
            // ⚠ IT USED TO TAKE THE PLAYER'S BLOCK WITH IT — "nothing is hidden, and Block is where a player
            // hides", which is the best line in the fight and had to go. The intent label is built from the
            // authored effect list, and that DSL has no word for taking Block away; so the strip would have
            // been damage the telegraph never mentioned, in the one act whose own rules forbid exactly that.
            // A plainer, larger number that the label CAN say is worth more than a flourish it cannot.
            "nothing_is_hidden" => Sunlight(Seq(
                Spend(NoonDue), Strike(Radiant(30, 2)), Debuff(Cards.Keywords.Paperwork, 3))),
            // §10.14. Fully telegraphed, enormous, and read off everything he has ever seen.
            "pronounce_what_was_witnessed" => Sunlight(Seq(
                Spend(JudgmentDue),
                new RemoveStatusNode<EnemyActionContext>(Sun, new StatusDefinitionId(JudgmentId)),
                Strike(Radiant(25, 10)))),
            _ => null,
        };

    private static EffectProgram<EnemyActionContext> Sunlight(IEffectNode<EnemyActionContext> move) => new(move);

    private static IEffectNode<EnemyActionContext> Strike(ICombatExpression<EnemyActionContext, int> damage) =>
        new DealDamageNode<EnemyActionContext>(Applicant, damage);

    // Every number he deals, and there are only two terms in it: what the move is worth, and what he has
    // seen. This is the same sum the telegraph prints — "24 dmg +2 per own Witness" — which is the whole
    // reason there is nothing else in it.
    private static ICombatExpression<EnemyActionContext, int> Radiant(int damage, int perWitness) =>
        new AddExpression<EnemyActionContext>(
            Const<EnemyActionContext>(damage),
            new MultiplyExpression<EnemyActionContext>(
                Const<EnemyActionContext>(perWitness), Stacks<EnemyActionContext>(Sun, WitnessId)));

    private static IEffectNode<EnemyActionContext> Spend(CounterId flag) =>
        new SetCombatantCounterNode<EnemyActionContext>(
            Sun, flag, Const<EnemyActionContext>(0), relative: false);

    // ── his rule ──────────────────────────────────────────────────────────────────────────────────────────

    private static StatusData TheOathsBeneathTheSun() => new()
    {
        Id = OathsBeneathTheSunId,
        NameKey = "Oaths Beneath the Sun",
        DescriptionKey =
            "He asks for a promise at the start of every turn and pays for it at once. Nothing he asks is "
            + "ever forbidden to you — a card that breaks your Oath is played, and resolves, and then he has "
            + "seen it. What he has seen he keeps, and every move he makes is read off it.",
        Polarity = StatusPolarity.Neutral,
        StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
        UsesStacks = false,
        Tags = [],
        PassiveModifiers = [],
        Triggers =
        [
            Trigger(TheSunAsks(), nameof(TriggerEvent.CardsDrawn), StatusTriggerScope.Anywhere),
            Trigger(TheSunSees(), nameof(TriggerEvent.CardPlayed), StatusTriggerScope.Anywhere),
            Trigger(TheSunSets(), nameof(TriggerEvent.TurnEnded), StatusTriggerScope.Anywhere),
            Trigger(TheSunClimbs(), nameof(TriggerEvent.DamageTaken)),
        ],
    };

    // ── the sky climbs ────────────────────────────────────────────────────────────────────────────────────

    // Read off his own blood, exactly once per threshold, and each crossing queues the move that announces
    // it. The "taken" counter is what makes it once: DamageTaken fires on every hit, and a marker applied
    // again while its condition holds reads ×9 — the lesson three gods have now paid for.
    private static EffectProgram<DamageReceivedTriggeredEffectContext> TheSunClimbs()
    {
        var height = new CombatantHealthPercentageExpression<DamageReceivedTriggeredEffectContext>(Sun);

        IEffectNode<DamageReceivedTriggeredEffectContext> Cross(
            int band, CounterId taken, CounterId due, string sky, string leaving, int sees,
            IEffectNode<DamageReceivedTriggeredEffectContext>? andAlso = null) =>
            new ConditionalEffectNode<DamageReceivedTriggeredEffectContext>(
                new AndExpression<DamageReceivedTriggeredEffectContext>(
                    At(height, ComparisonOperator.LessOrEqual, band),
                    At(new CombatantCounterExpression<DamageReceivedTriggeredEffectContext>(Sun, taken),
                        ComparisonOperator.Equal, 0)),
                Steps(
                [
                    new SetCombatantCounterNode<DamageReceivedTriggeredEffectContext>(
                        Sun, taken, Const<DamageReceivedTriggeredEffectContext>(1), relative: false),
                    new SetCombatantCounterNode<DamageReceivedTriggeredEffectContext>(
                        Sun, due, Const<DamageReceivedTriggeredEffectContext>(1), relative: false),
                    Drop<DamageReceivedTriggeredEffectContext>(Sun, leaving),
                    Wear<DamageReceivedTriggeredEffectContext>(Sun, sky, 1),
                    Wear<DamageReceivedTriggeredEffectContext>(Sun, WitnessId, sees),
                    andAlso ?? new NoOpEffectNode<DamageReceivedTriggeredEffectContext>(),
                ]));

        return new EffectProgram<DamageReceivedTriggeredEffectContext>(Steps(
        [
            Cross(ZenithAt, ZenithTaken, ZenithDue, ZenithId, MorningSunId, ZenithWitness,
                // §10.11 — the harder Oath is paid for in planning material, and it never goes away again.
                Wear<DamageReceivedTriggeredEffectContext>(Applicant, NoShadowId, 1)),
            Cross(FinalAt, NoonTaken, NoonDue, FinalOathId, ZenithId, FinalWitness),
        ]));
    }

    // ── the sun asks ──────────────────────────────────────────────────────────────────────────────────────

    // Everything that happens at the start of a player turn, and it hangs off CardsDrawn rather than
    // TurnStarted for one reason: an Oath is answered by a PROMPT, and a prompt asked before the hand is
    // dealt asks the player to promise something about cards they have not seen. The draw has happened when
    // this runs, so the hand on the table is the hand the vow is sworn against — which is also what makes
    // the Director's "is there a Deed in hand" question a fair one.
    private static EffectProgram<CardsDrawnTriggeredEffectContext> TheSunAsks() =>
        new(new ConditionalEffectNode<CardsDrawnTriggeredEffectContext>(
            PlayersTurn<CardsDrawnTriggeredEffectContext>(),
            Steps(
            [
                new ConditionalEffectNode<CardsDrawnTriggeredEffectContext>(
                    new ComparisonExpression<CardsDrawnTriggeredEffectContext>(
                        new CombatantCounterExpression<CardsDrawnTriggeredEffectContext>(Sun, Dawn),
                        ComparisonOperator.NotEqual,
                        new RoundNumberExpression<CardsDrawnTriggeredEffectContext>()),
                    Steps(
                    [
                        new SetCombatantCounterNode<CardsDrawnTriggeredEffectContext>(
                            Sun, Dawn, new RoundNumberExpression<CardsDrawnTriggeredEffectContext>(),
                            relative: false),
                        // Yesterday's news comes off first: "Seen" is about the turn it was said on.
                        Drop<CardsDrawnTriggeredEffectContext>(Applicant, SeenId),
                        // A vow already standing is renewed rather than replaced — this is the second
                        // morning of a long Oath, or any morning at all under the Final one.
                        new ConditionalEffectNode<CardsDrawnTriggeredEffectContext>(
                            Standing<CardsDrawnTriggeredEffectContext>(),
                            Renew(),
                            @else: Ask()),
                    ])),
                // …and this one is right to run on every draw: it says what the table looks like NOW.
                Warn<CardsDrawnTriggeredEffectContext>(),
            ])));

    // Is any vow at all on the table?
    private static ICombatExpression<TContext, bool> Standing<TContext>() where TContext : class =>
        AnyOf([.. Oaths.Select(o => Has<TContext>(Applicant, o.Id))]);

    // The per-turn half of a Favor, paid again on every morning the vow survives to see. Power is not here:
    // it is a status that is still standing from the swearing, and paying it twice would double it.
    private static IEffectNode<CardsDrawnTriggeredEffectContext> Renew() =>
        Steps(
        [
            .. Oaths.Where(o => o.Gives != Gift.Power).Select(o =>
                (IEffectNode<CardsDrawnTriggeredEffectContext>)
                new ConditionalEffectNode<CardsDrawnTriggeredEffectContext>(
                    Has<CardsDrawnTriggeredEffectContext>(Applicant, o.Id),
                    Paid<CardsDrawnTriggeredEffectContext>(o))),
        ]);

    // "Speak." Three offers, under whichever sky he is in, with the middle one guarded by what is in hand.
    private static IEffectNode<CardsDrawnTriggeredEffectContext> Ask()
    {
        IEffectNode<CardsDrawnTriggeredEffectContext> Under(string sky)
        {
            var (restraint, asked, fallback, spend) = OfferedUnder(sky);

            ChooseOptionsNode<CardsDrawnTriggeredEffectContext> Three(Oath middle) =>
                new(
                    [Swear(restraint), Swear(middle), Swear(spend)],
                    [restraint.Label, middle.Label, spend.Label],
                    count: 1, purpose: "speak");

            return new ConditionalEffectNode<CardsDrawnTriggeredEffectContext>(
                ADeedInHand<CardsDrawnTriggeredEffectContext>(), Three(asked), @else: Three(fallback));
        }

        // The Final Oath is spoken ONCE (§10.12, "Speak once."). Broken, it is not offered again — the
        // Favor is gone, the Judgment is coming, and there is nothing left to promise.
        return new ConditionalEffectNode<CardsDrawnTriggeredEffectContext>(
            Has<CardsDrawnTriggeredEffectContext>(Sun, FinalOathId),
            new ConditionalEffectNode<CardsDrawnTriggeredEffectContext>(
                At(new CombatantCounterExpression<CardsDrawnTriggeredEffectContext>(Sun, Spoken),
                    ComparisonOperator.Equal, 0),
                Under(FinalOathId)),
            @else: new ConditionalEffectNode<CardsDrawnTriggeredEffectContext>(
                Has<CardsDrawnTriggeredEffectContext>(Sun, ZenithId),
                Under(ZenithId),
                @else: Under(MorningSunId)));
    }

    // Swearing: the vow goes on, its clock is set, what it will be measured in is zeroed, and the Favor is
    // paid AT ONCE — before a single card of the turn has been played, and whether or not the vow survives
    // the turn. That is the trade the whole fight is made of.
    private static IEffectNode<CardsDrawnTriggeredEffectContext> Swear(Oath oath) =>
        Steps(
        [
            Wear<CardsDrawnTriggeredEffectContext>(Applicant, oath.Id, 1),
            Great(oath)
                ? new SetCombatantCounterNode<CardsDrawnTriggeredEffectContext>(
                    Sun, Spoken, Const<CardsDrawnTriggeredEffectContext>(1), relative: false)
                : Wear<CardsDrawnTriggeredEffectContext>(Applicant, OathStandsId, oath.Turns),
            new SetCombatantCounterNode<CardsDrawnTriggeredEffectContext>(
                Sun, PlaysUnderOath, Const<CardsDrawnTriggeredEffectContext>(0), relative: false),
            new SetCombatantCounterNode<CardsDrawnTriggeredEffectContext>(
                Sun, DamageUnderOath, Const<CardsDrawnTriggeredEffectContext>(0), relative: false),
            // Nothing was played before the vow, so nothing can repeat it: the first card of a Straight Path
            // is always legal.
            new SetCombatantCounterNode<CardsDrawnTriggeredEffectContext>(
                Sun, LastType, Const<CardsDrawnTriggeredEffectContext>(0), relative: false),
            Paid<CardsDrawnTriggeredEffectContext>(oath),
        ]);

    private static IEffectNode<TContext> Paid<TContext>(Oath oath) where TContext : class => oath.Gives switch
    {
        Gift.Draw => new DrawCardsNode<TContext>(Applicant, Const<TContext>(oath.Amount)),
        Gift.Block => new GainBlockNode<TContext>(Applicant, Const<TContext>(oath.Amount)),
        Gift.Energy => new GainResourceNode<TContext>(
            Applicant, StandardCombatIds.EnergyResource, Const<TContext>(oath.Amount)),
        Gift.EnergyAndDraw => Steps<TContext>(
        [
            new GainResourceNode<TContext>(
                Applicant, StandardCombatIds.EnergyResource, Const<TContext>(oath.Amount)),
            new DrawCardsNode<TContext>(Applicant, Const<TContext>(oath.Amount)),
        ]),
        _ => Wear<TContext>(Applicant, FavorOfTheSunId, oath.Amount),
    };

    // §10.4. Twelve indices is the whole hand and then some; a card that is not there answers false, so the
    // question is exact for any hand rather than for the hand somebody imagined.
    private static ICombatExpression<TContext, bool> ADeedInHand<TContext>() where TContext : class =>
        AnyOf([.. Enumerable.Range(0, 12).Select(i =>
            (ICombatExpression<TContext, bool>)new CardInstanceHasTagExpression<TContext>(
                new CardInZoneExpression<TContext>(CardZone.Hand, i),
                new TagId(Cards.CardAuthoring.DeedTag)))]);

    // ── the sun sees ──────────────────────────────────────────────────────────────────────────────────────

    // Every card the player plays, in this order: it is counted, it is measured against the two vows that can
    // be broken WHILE a turn is being played, and then it becomes the card the next one is read against.
    //
    // ⚠ THE ORDER OF THE LAST TWO IS THE RULE. The Straight Path is broken by a card matching the one before
    // it, so the comparison has to happen before this card becomes "the one before it" — otherwise every card
    // matches itself and the vow is broken by the first thing played under it.
    private static EffectProgram<CardPlayedTriggeredEffectContext> TheSunSees()
    {
        var played = new TriggerEventCardInstanceExpression<CardPlayedTriggeredEffectContext>();

        IEffectNode<CardPlayedTriggeredEffectContext> Remember(string tag, int kind) =>
            new ConditionalEffectNode<CardPlayedTriggeredEffectContext>(
                new CardInstanceHasTagExpression<CardPlayedTriggeredEffectContext>(played, new TagId(tag)),
                new SetCombatantCounterNode<CardPlayedTriggeredEffectContext>(
                    Sun, LastType, Const<CardPlayedTriggeredEffectContext>(kind), relative: false));

        var kinds = new[]
        {
            (Cards.CardAuthoring.DeedTag, 1), (Cards.CardAuthoring.WorkingTag, 2),
            (Cards.CardAuthoring.RiteTag, 3), (Cards.CardAuthoring.JunkTag, 4),
        };

        // Which kind this card is, as a number: the same ladder Remember writes, read as an expression so the
        // comparison and the record can never disagree about what a Working is.
        var isKind = (int kind) => (ICombatExpression<CardPlayedTriggeredEffectContext, bool>)
            new CardInstanceHasTagExpression<CardPlayedTriggeredEffectContext>(
                played, new TagId(kinds.First(k => k.Item2 == kind).Item1));

        var last = new CombatantCounterExpression<CardPlayedTriggeredEffectContext>(Sun, LastType);
        var repeats = AnyOf([.. kinds.Select(k =>
            new AndExpression<CardPlayedTriggeredEffectContext>(
                isKind(k.Item2), At(last, ComparisonOperator.Equal, k.Item2)))]);

        return new EffectProgram<CardPlayedTriggeredEffectContext>(
            new ConditionalEffectNode<CardPlayedTriggeredEffectContext>(
                PlayersTurn<CardPlayedTriggeredEffectContext>(),
                Steps(
                [
                    new SetCombatantCounterNode<CardPlayedTriggeredEffectContext>(
                        Sun, PlaysUnderOath, Const<CardPlayedTriggeredEffectContext>(1), relative: true),

                    // "No more than N" — broken by the card that has just resolved, never by refusing it.
                    .. Oaths.Where(o => o.Asks == Vow.Restraint).Select(o =>
                        (IEffectNode<CardPlayedTriggeredEffectContext>)
                        new ConditionalEffectNode<CardPlayedTriggeredEffectContext>(
                            new AndExpression<CardPlayedTriggeredEffectContext>(
                                Has<CardPlayedTriggeredEffectContext>(Applicant, o.Id),
                                At(Played<CardPlayedTriggeredEffectContext>(o),
                                    ComparisonOperator.Greater, o.Number)),
                            Breach<CardPlayedTriggeredEffectContext>(o))),

                    // "Never twice in a row" — and a card with no kind at all (a god's own sheet) repeats
                    // nothing, because LastType only ever holds a kind that exists.
                    .. Oaths.Where(o => o.Asks == Vow.Straight).Select(o =>
                        (IEffectNode<CardPlayedTriggeredEffectContext>)
                        new ConditionalEffectNode<CardPlayedTriggeredEffectContext>(
                            new AndExpression<CardPlayedTriggeredEffectContext>(
                                Has<CardPlayedTriggeredEffectContext>(Applicant, o.Id), repeats),
                            Breach<CardPlayedTriggeredEffectContext>(o))),

                    .. kinds.Select(k => Remember(k.Item1, k.Item2)),
                    Warn<CardPlayedTriggeredEffectContext>(),
                ])));
    }

    // How many plays this vow has seen: its own tally across two turns, or simply the turn's, which is the
    // only difference between an Oath of Restraint and an Oath of LONG Restraint.
    private static ICombatExpression<TContext, int> Played<TContext>(Oath oath) where TContext : class =>
        Cumulative(oath)
            ? new CombatantCounterExpression<TContext>(Sun, PlaysUnderOath)
            : new CardsPlayedThisTurnExpression<TContext>(Applicant);

    private static ICombatExpression<TContext, int> Dealt<TContext>(Oath oath) where TContext : class =>
        Cumulative(oath)
            ? new CombatantCounterExpression<TContext>(Sun, DamageUnderOath)
            : new DamageDealtThisTurnExpression<TContext>(Applicant);

    // ── the sun sets ──────────────────────────────────────────────────────────────────────────────────────

    // What can only be judged when the turn is over: the Energy that was not spent, the damage that was not
    // dealt, the Deed that was never played. Then the clock moves, and an Oath whose last turn this was is
    // taken off the table — kept or broken, it is finished.
    private static EffectProgram<TurnEndedTriggeredEffectContext> TheSunSets()
    {
        var energy = new CombatantCurrentResourceExpression<TurnEndedTriggeredEffectContext>(
            Applicant, StandardCombatIds.EnergyResource);
        var stands = Stacks<TurnEndedTriggeredEffectContext>(Applicant, OathStandsId);

        IEffectNode<TurnEndedTriggeredEffectContext> Judge(
            Oath oath, ICombatExpression<TurnEndedTriggeredEffectContext, bool> broken) =>
            new ConditionalEffectNode<TurnEndedTriggeredEffectContext>(
                new AndExpression<TurnEndedTriggeredEffectContext>(
                    Has<TurnEndedTriggeredEffectContext>(Applicant, oath.Id), broken),
                Breach<TurnEndedTriggeredEffectContext>(oath));

        // The turn's damage joins the vow's tally before anything is judged against it — a two-turn Oath is
        // measured on the sum of both, and the second turn's has only just finished happening.
        var tally = new SetCombatantCounterNode<TurnEndedTriggeredEffectContext>(
            Sun, DamageUnderOath,
            new DamageDealtThisTurnExpression<TurnEndedTriggeredEffectContext>(Applicant), relative: true);

        // Judged EVERY turn: what the vow says about the turn that has just ended, whatever else it covers.
        var everyTurn = Oaths
            .Where(o => o.Asks == Vow.Spend || (o.Asks == Vow.Damage && Great(o)))
            .Select(o => o.Asks == Vow.Spend
                ? Judge(o, At(energy, ComparisonOperator.NotEqual, 0))
                : Judge(o, At(Dealt<TurnEndedTriggeredEffectContext>(o), ComparisonOperator.Less, o.Number)));

        // Judged only when the vow's last turn ends: what it asked of the WHOLE of the time it covered.
        var atTheEnd = Steps<TurnEndedTriggeredEffectContext>(
        [
            .. Oaths.Where(o => o.Asks == Vow.Deed).Select(o => Judge(o,
                At(new CardsPlayedThisTurnWithTagExpression<TurnEndedTriggeredEffectContext>(
                        Applicant, new TagId(Cards.CardAuthoring.DeedTag)),
                    ComparisonOperator.Less, o.Number))),
            .. Oaths.Where(o => o.Asks == Vow.Damage && !Great(o)).Select(o => Judge(o,
                At(Dealt<TurnEndedTriggeredEffectContext>(o), ComparisonOperator.Less, o.Number))),
            // Kept or broken, a timed Oath ends here. The Favor ends with it: it was paid for a promise
            // whose term is over.
            .. Oaths.Where(o => !Great(o)).Select(o =>
                (IEffectNode<TurnEndedTriggeredEffectContext>)
                Drop<TurnEndedTriggeredEffectContext>(Applicant, o.Id)),
            Drop<TurnEndedTriggeredEffectContext>(Applicant, FavorOfTheSunId),
            Drop<TurnEndedTriggeredEffectContext>(Applicant, OathStandsId),
        ]);

        return new EffectProgram<TurnEndedTriggeredEffectContext>(
            new ConditionalEffectNode<TurnEndedTriggeredEffectContext>(
                PlayersTurn<TurnEndedTriggeredEffectContext>(),
                Steps([
                    tally,
                    .. everyTurn,
                    new ConditionalEffectNode<TurnEndedTriggeredEffectContext>(
                        At(stands, ComparisonOperator.Equal, 1),
                        atTheEnd,
                        @else: new ConditionalEffectNode<TurnEndedTriggeredEffectContext>(
                            At(stands, ComparisonOperator.GreaterOrEqual, 2),
                            new ModifyStatusStacksNode<TurnEndedTriggeredEffectContext>(
                                Applicant, new StatusDefinitionId(OathStandsId),
                                Const<TurnEndedTriggeredEffectContext>(-1)))),
                ])));
    }

    // ── "Seen." ───────────────────────────────────────────────────────────────────────────────────────────

    // A breach, and every part of it is a consequence rather than a punishment: the Witness rises and stays
    // risen, the vow is over (so it cannot be broken twice by the same turn), the Favor it bought is gone,
    // and the player is told, on their own row, that he saw it.
    //
    // A GREAT Oath breaks differently, because it was a different promise: there is no next Oath to swear,
    // and his next move is Judgment — announced on a chip so the turn it is coming is a turn that can be
    // planned against. §10.14: he does not become invulnerable, and killing him before it lands is a real
    // and intended way to answer it.
    private static IEffectNode<TContext> Breach<TContext>(Oath oath) where TContext : class =>
        Steps<TContext>(
        [
            Wear<TContext>(Sun, WitnessId, 1),
            Wear<TContext>(Applicant, SeenId, 1),
            Drop<TContext>(Applicant, oath.Id),
            Drop<TContext>(Applicant, FavorOfTheSunId),
            Drop<TContext>(Applicant, OathStandsId),
            .. Great(oath)
                ? new IEffectNode<TContext>[]
                {
                    new SetCombatantCounterNode<TContext>(
                        Sun, JudgmentDue, Const<TContext>(1), relative: false),
                    Wear<TContext>(Sun, JudgmentId, 1),
                }
                : [],
        ]);

    // ── the warning ───────────────────────────────────────────────────────────────────────────────────────

    // §10.2 asks for a warning ON the action that would break the vow. The nearest true thing this game can
    // write is a chip that says the state the next action would be taken in, restated after every play and
    // every morning — THE NEXT ONE BREAKS IT when the allowance is spent, THE OATH IS NOT YET KEPT whenever
    // ending the turn where it stands would be a breach. Both are struck off and written again rather than
    // applied, for the reason the whole project now knows by heart.
    private static IEffectNode<TContext> Warn<TContext>() where TContext : class
    {
        var edge = AnyOf([.. Oaths.Where(o => o.Asks == Vow.Restraint).Select(o =>
            new AndExpression<TContext>(
                Has<TContext>(Applicant, o.Id),
                At(Played<TContext>(o), ComparisonOperator.GreaterOrEqual, o.Number)))]);

        var energy = new CombatantCurrentResourceExpression<TContext>(
            Applicant, StandardCombatIds.EnergyResource);

        var owed = AnyOf(
        [
            .. Oaths.Where(o => o.Asks == Vow.Spend).Select(o =>
                (ICombatExpression<TContext, bool>)new AndExpression<TContext>(
                    Has<TContext>(Applicant, o.Id), At(energy, ComparisonOperator.NotEqual, 0))),
            .. Oaths.Where(o => o.Asks == Vow.Damage).Select(o =>
                (ICombatExpression<TContext, bool>)new AndExpression<TContext>(
                    Has<TContext>(Applicant, o.Id),
                    At(Dealt<TContext>(o), ComparisonOperator.Less, o.Number))),
            .. Oaths.Where(o => o.Asks == Vow.Deed).Select(o =>
                (ICombatExpression<TContext, bool>)new AndExpression<TContext>(
                    Has<TContext>(Applicant, o.Id),
                    At(new CardsPlayedThisTurnWithTagExpression<TContext>(
                            Applicant, new TagId(Cards.CardAuthoring.DeedTag)),
                        ComparisonOperator.Less, o.Number))),
        ]);

        return Steps<TContext>(
        [
            Drop<TContext>(Applicant, AtTheEdgeId),
            Drop<TContext>(Applicant, NotYetKeptId),
            new ConditionalEffectNode<TContext>(edge, Wear<TContext>(Applicant, AtTheEdgeId, 1)),
            new ConditionalEffectNode<TContext>(owed, Wear<TContext>(Applicant, NotYetKeptId, 1)),
        ]);
    }

    // ── shared idioms ─────────────────────────────────────────────────────────────────────────────────────

    private static IEffectNode<TContext> Steps<TContext>(IReadOnlyList<IEffectNode<TContext>> steps)
        where TContext : class => new CausalSequenceEffectNode<TContext>(steps);

    private static IEffectNode<TContext> Wear<TContext>(
        ICombatantTargetSelector body, string statusId, int stacks) where TContext : class =>
        new ApplyStatusNode<TContext>(
            body, new StatusDefinitionId(statusId), Const<TContext>(stacks), sourceSelector: Sun);

    private static IEffectNode<TContext> Drop<TContext>(ICombatantTargetSelector body, string statusId)
        where TContext : class =>
        new RemoveStatusNode<TContext>(body, new StatusDefinitionId(statusId));

    // An OR over however many terms there are, folded left. An empty list is false, which is the right answer
    // to "is any of nothing true".
    private static ICombatExpression<TContext, bool> AnyOf<TContext>(
        IReadOnlyList<ICombatExpression<TContext, bool>> terms) where TContext : class =>
        terms.Count == 0
            ? new ComparisonExpression<TContext>(Const<TContext>(0), ComparisonOperator.Equal, Const<TContext>(1))
            : terms.Aggregate((a, b) => new OrExpression<TContext>(a, b));
}
