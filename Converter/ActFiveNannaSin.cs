using System.Text.Json;
using RogueDeck.Core.Combat;
using RogueDeck.Sandbox.Composition;
using RogueDeck.Scenario.Authoring;

namespace BnbContent.Converter;

// ACT V, the fourth god — NANNA-SIN, LORD OF THE COUNTED MOON. He counts, and he is in no hurry.
//
// He is the divine calendar, and the fight is not about speed. It is about RECURRENCE: what you do now comes
// back when the moon reaches the same number again, and his own moves come back with it. His fight asks one
// question and asks it every night (§9 core fantasy):
//
//     If one action from this turn will return later, WHICH action should it be?
//
// THE RING. Eight phases, one per round, and each carries a COUNT:
//
//     New Moon —  ·  Waxing Crescent I  ·  First Quarter II  ·  Waxing Gibbous III  ·  Full Moon IV
//                    Waning Gibbous III  ·  Last Quarter II  ·  Waning Crescent I  ·  then New Moon again
//
// which is the pattern `— I II III IV III II I`, or, as the arithmetic actually runs it, 4 − |phase − 5|.
//
// THE COUNT SAYS WHICH CARD. At `First Quarter — II`, the SECOND card played that turn becomes Counted. At
// `New Moon` nothing is counted and nothing returns, which is why the calendar opens empty.
//
// THE COUNT RETURNS (§9.3, the audit's own revision). A Counted card does not wait for the same NAMED phase —
// it returns at the next occurrence of the same COUNT. That is the whole timing system, and it is why the two
// halves of the ring are different investments:
//
//     III  Waxing Gibbous → Waning Gibbous       two nights     short-term preparation
//     II   First Quarter  → Last Quarter         four nights    medium-term
//     I    Waxing Crescent → Waning Crescent     six nights     long-term
//     IV   Full Moon      → the next Full Moon   a whole orbit  full-orbit investment
//
// and on the waning side the same counts point at the NEXT orbit, so some echoes already come back inside the
// first orbit and the rest arrive when the sky repeats.
//
// LUNAR ECHO (§9.4). When the count returns, a temporary copy of the Counted card is in hand: it costs 0, it
// is the card it was (a copy of the definition, so an upgraded card comes back upgraded), it may be played
// normally, and it is gone at the end of the turn. It is NOT cast for the player. He counts; he does not act
// on your behalf.
//
// HIS OWN MOVES ARE COUNTED TOO (§9.5), and this is the half that needed no machinery at all. The phase→move
// map is fixed, so the move recorded at a count is always the move of the phase 10 − p: Waning Gibbous returns
// Fill the Measure, Last Quarter returns Divide the Night, and the Full Moon returns the last Full Moon. His
// Returning Move resolves in addition to his current intent at 55 % (75 % once the Old Moon is up), and the
// number is on a chip beside him BEFORE it lands, because a telegraph that leaves out half the damage is the
// hidden gotcha §3 forbids.
//
// PERFECT REFLECTION (§9.6). The Full Moon is the apex and amplifies both sides of recurrence: his Returning
// Move comes back at 70 % rather than 55 %, and an Echo the player plays that night resolves a SECOND time at
// half — the engine's own card replay, at a scale, which is exactly what "a moderate numerical enhancement"
// means for a card nobody wrote a number for.
//
// THE INTERCALARY SEAL (§9.8). Once per orbit the player may HOLD THE MOON: the phase does not advance, and a
// second night of the same phase happens. He gets the extra night too, the extra night is recorded like any
// other, and — because the next occurrence of a count is now the very next night — what you counted comes
// straight back. You buy time now by making one more thing return.
//
// THE ORBITS ARE THE PHASES (§9.9), and they are not about hit points. Orbit I the calendar is mostly empty;
// Orbit II you have seen this sky before; Orbit III his Returning Moves get stronger; Orbit IV+ is the
// Unending Month, and each further orbit gives him one permanent RADIANCE, which is the soft enrage that
// stops the fight being outlasted.
//
// ── WHY THE PHASE IS A COUNTER AND NANSHE'S DAY WAS THE ROUND ─────────────────────────────────────────────
// Nanshe's calendar had to be pure arithmetic on the round, because her tablet shows three days in ADVANCE and
// the engine's forecast is a live projection: an intent chosen from hidden state answers the same thing three
// times over. Nanna-Sin forecasts nothing — the ring itself is the forecast, and it is on the wall — so his
// phase can be a counter, which is what lets one held night stop the calendar without stopping the fight.
// The counter is still WRITTEN from arithmetic (`round − 1 − nights held`) rather than stepped, so a replayed
// fight cannot drift.
public static partial class ActFive
{
    public const string NannaSinEnemyId = "nanna_sin_lord_of_the_counted_moon";
    public const string NannaSinEncounterId = "act_5_nanna_sin_lord_of_the_counted_moon";

    // His rule, worn from the first bell. Every trigger below hangs off it.
    public const string LunarCalendarId = "the_lunar_calendar";

    // What he wears: where the moon is, what that means, and how many times the sky has repeated.
    public const string LunarPhaseId = "the_lunar_phase";
    public const string LunarCountId = "the_lunar_count";
    public const string OrbitId = "the_orbit";
    public const string ReturningMoveId = "the_move_that_returns";
    public const string MoonHeldId = "the_moon_is_held";
    public const string RadianceId = "radiance";

    // The three later orbits, which are his phases.
    public const string ReturningMoonId = "the_returning_moon";
    public const string OldMoonId = "the_old_moon";
    public const string UnendingMonthId = "the_unending_month";

    // What the player wears.
    public const string IntercalarySealId = "the_intercalary_seal";
    public const string PerfectReflectionId = "perfect_reflection";

    public const string HoldTheMoonCardId = "hold_the_moon";

    // His own paperwork wears a tag of its own, so the ring never counts it. See TheCountIsTaken.
    public const string MoonSheetTag = "moon_sheet";

    // The two stamps a card can carry in this fight. Both are read on the card face (bnb-godot's stamp table),
    // because the whole decision the fight asks — which card should come back — is made on the cards.
    public const string CountedMark = "moon_counted";
    public const string EchoMark = "lunar_echo";

    // 700, and the second highest of the four gods for Inanna's reason rather than Nisaba's: his mechanic
    // hands the player a free card most nights, so the deck fighting him is FASTER than it is anywhere else,
    // and a god whose third orbit arrives in the round he dies has an escalation nobody sees.
    public const int NannaSinMaxHealth = 700;

    private const int Phases = 8;
    private const int PeakPhase = 5;             // the Full Moon, where the count is IV
    private const int PeakCount = 4;
    private const int ReturnPercent = 55;        // §9.5's "around 50–65 % during earlier cycles"
    private const int OldMoonPercent = 75;       // Orbit III: "his Returning Moves become stronger"
    private const int FullMoonPercent = 70;      // §9.6: the Full Moon amplifies his side of recurrence too
    private const int FullMoonOldPercent = 85;
    private const int RadiancePerOrbit = 1;
    private const int RadianceDamage = 3;
    private const int UnendingOrbit = 4;
    private const int ReflectionNumerator = 1;   // an Echo played at the Full Moon happens again at a half
    private const int ReflectionDenominator = 2;

    // Counters, all on him. The phase is what his intent rules read; the held nights are what keep the phase
    // from being the round; the sealed orbit is the one whose Intercalary Seal has already been handed over.
    private static CounterId PhaseCounter => new("nanna_sin_phase");
    private static CounterId HeldNights => new("nanna_sin_held_nights");
    private static CounterId SealedOrbit => new("nanna_sin_sealed_orbit");
    private static CounterId NotKept => new("nanna_sin_not_kept");

    // …and one counter that lives on a CARD rather than on a body: which count that copy was taken at. One
    // mark plus one number beats four marks, and it is the same fact the stamp on the card face reads.
    private static CounterId CountedAt => new("nanna_sin_counted_at");

    private static ICombatantTargetSelector Moon => Bearer(LunarCalendarId);

    // The ring, in order. Each phase is a night, a name, one move, and what that move comes to before
    // anything returns on top of it.
    private static readonly (int Index, string Night, string IntentId, string Move, int Damage)[] Ring =
    [
        (1, "New Moon",        "count_the_unseen",     "Count the Unseen",    16),
        (2, "Waxing Crescent", "raise_the_horns",      "Raise the Horns",     20),
        (3, "First Quarter",   "divide_the_night",     "Divide the Night",    24),
        (4, "Waxing Gibbous",  "fill_the_measure",     "Fill the Measure",    26),
        (5, "Full Moon",       "crown_of_ur",          "Crown of Ur",         34),
        (6, "Waning Gibbous",  "pour_away_the_light",  "Pour Away the Light", 22),
        (7, "Last Quarter",    "cut_the_month",        "Cut the Month",       24),
        (8, "Waning Crescent", "close_the_reckoning",  "Close the Reckoning", 18),
    ];

    // The count a phase carries: `— I II III IV III II I`, which is one subtraction rather than a table.
    private static int CountOf(int phase) => PeakCount - Math.Abs(phase - PeakPhase);

    // Which phase's move comes back at this one. The counts are symmetric about the Full Moon, so the
    // partner of p is always 10 − p — and for the waxing half (and the Full Moon itself) that partner is in
    // the PREVIOUS orbit, which is why the first orbit is half empty.
    private static int RecordedAt(int phase) => 2 * PeakPhase - phase;
    private static bool FromLastOrbit(int phase) => phase <= PeakPhase;

    // ── what the act hands to the rest of the converter ───────────────────────────────────────────────────

    public static IReadOnlyList<StatusData> NannaSinStatuses() =>
    [
        TheLunarCalendar(), TheLunarPhase(), TheLunarCount(), TheOrbit(), TheReturningMove(),
        TheMoonIsHeld(), TheRadiance(),
        Face(ReturningMoonId, "The Returning Moon",
            "You have seen this sky before. Every count on the ring now points at a night that has already "
            + "happened, so his moves return as well as yours.", stacks: false),
        Face(OldMoonId, "The Old Moon",
            "Past, present and future overlap. What comes back off the calendar comes back harder.",
            stacks: false),
        Face(UnendingMonthId, "The Unending Month",
            "There is no last orbit. Every further turn of the sky leaves a little more light on him, and "
            + "waiting him out costs more than it did.", stacks: false),
        TheIntercalarySeal(), ThePerfectReflection(),
    ];

    public static IReadOnlyList<CardData> NannaSinCards() => [HoldTheMoon()];

    public static EffectProgram<EnemyActionContext>? NannaSinIntent(string enemyId, string intentId) =>
        enemyId != NannaSinEnemyId ? null : Ring.Where(p => p.IntentId == intentId)
            .Select(p => new EffectProgram<EnemyActionContext>(Seq(TheNight(p.Index), TheMoveThatReturns(p.Index))))
            .FirstOrDefault();

    // ── the ring, as faces ────────────────────────────────────────────────────────────────────────────────

    public static StatusData TheLunarCalendar() => new()
    {
        Id = LunarCalendarId,
        NameKey = "The Lunar Calendar",
        DescriptionKey =
            "Eight phases, one a round, carrying the counts — · I · II · III · IV · III · II · I. The card "
            + "you play on the count is Counted, and comes back the next time that count comes round. So do "
            + "his own moves.",
        Polarity = StatusPolarity.Neutral,
        StackingBehavior = StatusStackingBehavior.MergeWithExistingInstance,
        UsesStacks = false,
        Tags = [],
        PassiveModifiers = [],
        Triggers =
        [
            Trigger(TheCalendarTurns(), nameof(TriggerEvent.TurnStarted), StatusTriggerScope.Anywhere),
            Trigger(TheNightEnds(), nameof(TriggerEvent.TurnEnded), StatusTriggerScope.Anywhere),
            Trigger(TheCountIsTaken(), nameof(TriggerEvent.CardPlayed), StatusTriggerScope.Anywhere),
        ],
    };

    private static StatusData TheLunarPhase() => Face(
        LunarPhaseId, "The Lunar Phase",
        "Where the moon stands on the ring: 1 New Moon · 2 Waxing Crescent · 3 First Quarter · 4 Waxing "
        + "Gibbous · 5 Full Moon · 6 Waning Gibbous · 7 Last Quarter · 8 Waning Crescent.", stacks: true);

    private static StatusData TheLunarCount() => Face(
        LunarCountId, "The Lunar Count",
        "Which card played this turn is Counted — at II it is the second. A Counted card returns as a free "
        + "copy the next time this count comes round, and is gone at the end of that turn. A card that will "
        + "not last the night — an Echo, or his own sheet — is not counted and does not take the count.",
        stacks: true);

    private static StatusData TheOrbit() => Face(
        OrbitId, "The Orbit",
        "How many times the sky has come round. From the second orbit every count on the ring has a night "
        + "behind it, so everything returns.", stacks: true);

    private static StatusData TheReturningMove() => Face(
        ReturningMoveId, "The Move That Returns",
        "What the count brings back of his own, on top of the intent beside it. It is the move he made at "
        + "this count last time, weaker — and the Full Moon returns it stronger than the rest.", stacks: true);

    private static StatusData TheMoonIsHeld() => Face(
        MoonHeldId, "The Moon Is Held",
        "You asked for another night. The phase does not advance: the same count comes again, and what you "
        + "counted comes straight back — as does what he counted.", stacks: false);

    private static StatusData TheRadiance() => Face(
        RadianceId, "Radiance",
        $"Light he has kept. Each stack adds {RadianceDamage} to everything he does, and nothing takes it "
        + "off again.", stacks: true) with
    {
        PassiveModifiers =
        [
            new PassiveModifierData(
                PassiveModifierPipeline.DamageDealt, PassiveModifierOperation.AddPerStack, RadianceDamage,
                RestrictDamageKind: null),
        ],
    };

    private static StatusData TheIntercalarySeal() => Face(
        IntercalarySealId, "Intercalary Seal",
        "One per orbit. Hold the Moon and the phase does not advance — a second night of this same phase, "
        + "for you and for him.", stacks: true);

    private static StatusData ThePerfectReflection() => Face(
        PerfectReflectionId, "Perfect Reflection",
        "The Full Moon. A Lunar Echo you play tonight happens a second time at half strength — and his "
        + "Returning Move is stronger tonight for the same reason.", stacks: false);

    // ── the calendar turns ────────────────────────────────────────────────────────────────────────────────

    // Everything the ring does happens at the player's turn start, in this order and for a reason: a held
    // night has to be spent BEFORE the phase is worked out, the phase before the count, and the count before
    // anything is returned by it. Every number here is an expression over live state read where it stands —
    // the standing hazard of this whole authoring style, and the one that has cost this project three days.
    private static EffectProgram<TurnStartedTriggeredEffectContext> TheCalendarTurns()
    {
        var phase = Phase<TurnStartedTriggeredEffectContext>();
        var orbit = Orbit<TurnStartedTriggeredEffectContext>();

        return new EffectProgram<TurnStartedTriggeredEffectContext>(
            new ConditionalEffectNode<TurnStartedTriggeredEffectContext>(
                PlayersTurn<TurnStartedTriggeredEffectContext>(),
                new CausalSequenceEffectNode<TurnStartedTriggeredEffectContext>(
                [
                    SpendTheHeldNight(),
                    new SetCombatantCounterNode<TurnStartedTriggeredEffectContext>(
                        Moon, NotKept, Const<TurnStartedTriggeredEffectContext>(0), relative: false),
                    // The phase is written to a COUNTER as well as to a chip, because the counter is what his
                    // intent rules read: the move and the night it belongs to can never disagree.
                    new SetCombatantCounterNode<TurnStartedTriggeredEffectContext>(
                        Moon, PhaseCounter, phase, relative: false),
                    Restate(Moon, LunarPhaseId, phase),
                    Restate(Moon, LunarCountId, Count<TurnStartedTriggeredEffectContext>()),
                    Restate(Moon, OrbitId, orbit),
                    TheSkyRepeats(orbit),
                    TheSeal(orbit),
                    TheFullMoonRises(phase),
                    WhatHeWillBringBack(phase, orbit),
                    TheCountReturns(),
                    OfferTheSeal(),
                ])));
    }

    // A night bought last turn is spent now: one more night held, and the phase arithmetic below therefore
    // lands on the same number it landed on yesterday.
    private static IEffectNode<TurnStartedTriggeredEffectContext> SpendTheHeldNight() =>
        new ConditionalEffectNode<TurnStartedTriggeredEffectContext>(
            Has<TurnStartedTriggeredEffectContext>(Moon, MoonHeldId),
            new CausalSequenceEffectNode<TurnStartedTriggeredEffectContext>(
            [
                new SetCombatantCounterNode<TurnStartedTriggeredEffectContext>(
                    Moon, HeldNights, Const<TurnStartedTriggeredEffectContext>(1), relative: true),
                new RemoveStatusNode<TurnStartedTriggeredEffectContext>(
                    Moon, new StatusDefinitionId(MoonHeldId)),
            ]));

    // Orbit II, III and IV+ — his phases, and the only escalation he has. They are markers rather than
    // rewrites: every rule in the fight is already written in terms of the ring.
    //
    // ⚠ STRUCK OFF AND WRITTEN AGAIN, every night, rather than applied while the orbit lasts. A status merges
    // by ADDING, so a marker re-applied on each of an orbit's eight nights reads "The Old Moon ×8" — which is
    // a chip that lies about a thing that is either true or not.
    private static IEffectNode<TurnStartedTriggeredEffectContext> TheSkyRepeats(
        ICombatExpression<TurnStartedTriggeredEffectContext, int> orbit)
    {
        IEffectNode<TurnStartedTriggeredEffectContext> Mark(string id, int from, bool last) =>
            new CausalSequenceEffectNode<TurnStartedTriggeredEffectContext>(
            [
                new RemoveStatusNode<TurnStartedTriggeredEffectContext>(Moon, new StatusDefinitionId(id)),
                new ConditionalEffectNode<TurnStartedTriggeredEffectContext>(
                    last
                        ? At(orbit, ComparisonOperator.GreaterOrEqual, from)
                        : At(orbit, ComparisonOperator.Equal, from),
                    new ApplyStatusNode<TurnStartedTriggeredEffectContext>(
                        Moon, new StatusDefinitionId(id), Const<TurnStartedTriggeredEffectContext>(1),
                        sourceSelector: Moon)),
            ]);

        return new CausalSequenceEffectNode<TurnStartedTriggeredEffectContext>(
        [
            Mark(ReturningMoonId, 2, last: false),
            Mark(OldMoonId, 3, last: false),
            Mark(UnendingMonthId, UnendingOrbit, last: true),
        ]);
    }

    // ONE SEAL PER ORBIT, and the orbit it was granted for is written down — otherwise a held New Moon would
    // hand out a second seal for the same sky, which is the one thing §9.8 forbids outright.
    //
    // The same window carries the soft enrage, because it is the same fact: a new orbit has begun, and from
    // the fourth that costs him nothing and the player one more Radiance.
    private static IEffectNode<TurnStartedTriggeredEffectContext> TheSeal(
        ICombatExpression<TurnStartedTriggeredEffectContext, int> orbit) =>
        new ConditionalEffectNode<TurnStartedTriggeredEffectContext>(
            new ComparisonExpression<TurnStartedTriggeredEffectContext>(
                orbit, ComparisonOperator.Greater,
                new CombatantCounterExpression<TurnStartedTriggeredEffectContext>(Moon, SealedOrbit)),
            new CausalSequenceEffectNode<TurnStartedTriggeredEffectContext>(
            [
                new SetCombatantCounterNode<TurnStartedTriggeredEffectContext>(
                    Moon, SealedOrbit, orbit, relative: false),
                // ONE, and not one MORE: a seal the player did not spend does not carry into the next sky.
                // §9.8 grants a seal per orbit and permits one hold per orbit, which is the same sentence
                // said twice only if the grant cannot stack.
                new RemoveStatusNode<TurnStartedTriggeredEffectContext>(
                    Applicant, new StatusDefinitionId(IntercalarySealId)),
                new ApplyStatusNode<TurnStartedTriggeredEffectContext>(
                    Applicant, new StatusDefinitionId(IntercalarySealId),
                    Const<TurnStartedTriggeredEffectContext>(1), sourceSelector: Moon),
                new ConditionalEffectNode<TurnStartedTriggeredEffectContext>(
                    At(orbit, ComparisonOperator.GreaterOrEqual, UnendingOrbit),
                    new ApplyStatusNode<TurnStartedTriggeredEffectContext>(
                        Moon, new StatusDefinitionId(RadianceId),
                        Const<TurnStartedTriggeredEffectContext>(RadiancePerOrbit), sourceSelector: Moon)),
            ]));

    // The apex is a faculty of the one who plays the card, so it is worn by the PLAYER — which is also where
    // it is read, on their own row, on the night it matters.
    private static IEffectNode<TurnStartedTriggeredEffectContext> TheFullMoonRises(
        ICombatExpression<TurnStartedTriggeredEffectContext, int> phase) =>
        new ConditionalEffectNode<TurnStartedTriggeredEffectContext>(
            At(phase, ComparisonOperator.Equal, PeakPhase),
            new ApplyStatusNode<TurnStartedTriggeredEffectContext>(
                Applicant, new StatusDefinitionId(PerfectReflectionId),
                Const<TurnStartedTriggeredEffectContext>(1), sourceSelector: Moon),
            new RemoveStatusNode<TurnStartedTriggeredEffectContext>(
                Applicant, new StatusDefinitionId(PerfectReflectionId)));

    // WHAT HE WILL BRING BACK, WRITTEN DOWN BEFORE IT LANDS. The telegraph is built from the authored intent
    // and can only ever name tonight's move; the Returning Move is the other half of the damage, and a boss
    // that deals half its damage off the telegraph is exactly the hidden rule this act forbids. So the number
    // goes on a chip beside him, computed by the same arithmetic that will deal it.
    private static IEffectNode<TurnStartedTriggeredEffectContext> WhatHeWillBringBack(
        ICombatExpression<TurnStartedTriggeredEffectContext, int> phase,
        ICombatExpression<TurnStartedTriggeredEffectContext, int> orbit) =>
        new CausalSequenceEffectNode<TurnStartedTriggeredEffectContext>(
        [
            new RemoveStatusNode<TurnStartedTriggeredEffectContext>(
                Moon, new StatusDefinitionId(ReturningMoveId)),
            .. Ring.Where(p => CountOf(p.Index) >= 1).Select(p =>
                (IEffectNode<TurnStartedTriggeredEffectContext>)
                new ConditionalEffectNode<TurnStartedTriggeredEffectContext>(
                    FromLastOrbit(p.Index)
                        ? new AndExpression<TurnStartedTriggeredEffectContext>(
                            At(phase, ComparisonOperator.Equal, p.Index),
                            At(orbit, ComparisonOperator.GreaterOrEqual, 2))
                        : At(phase, ComparisonOperator.Equal, p.Index),
                    new ConditionalEffectNode<TurnStartedTriggeredEffectContext>(
                        Strengthened<TurnStartedTriggeredEffectContext>(),
                        Written<TurnStartedTriggeredEffectContext>(Returning(p.Index, strong: true)),
                        Written<TurnStartedTriggeredEffectContext>(Returning(p.Index, strong: false))))),
        ]);

    private static IEffectNode<TContext> Written<TContext>(int stacks) where TContext : class =>
        new ApplyStatusNode<TContext>(
            Moon, new StatusDefinitionId(ReturningMoveId), Const<TContext>(stacks), sourceSelector: Moon);

    // ── the count returns ─────────────────────────────────────────────────────────────────────────────────

    // §9.4. Whatever wears tonight's count comes back as a temporary copy in hand: free, itself (a copy of the
    // definition, so an upgraded card returns upgraded), and gone at the end of the turn.
    //
    // The card is looked for in all four ordinary zones because a Counted card is wherever the fight left it —
    // discarded, shuffled back into the draw, exhausted, or still in hand behind a cost that could not be
    // paid. ONE mark and a number on it beats four marks: the mark says "this was counted", the number says
    // at which count, and the search is the same four nodes whatever the count is.
    private static IEffectNode<TurnStartedTriggeredEffectContext> TheCountReturns()
    {
        var count = Count<TurnStartedTriggeredEffectContext>();
        var iterated = new IteratedCardExpression<TurnStartedTriggeredEffectContext>();
        var copied = Echoed;

        IEffectNode<TurnStartedTriggeredEffectContext> Search(CardZone zone) =>
            new ForEachCardInZoneNode<TurnStartedTriggeredEffectContext>(
                Applicant, zone,
                new ConditionalEffectNode<TurnStartedTriggeredEffectContext>(
                    new ComparisonExpression<TurnStartedTriggeredEffectContext>(
                        new CardInstanceMarkCounterExpression<TurnStartedTriggeredEffectContext>(
                            iterated, CountedAt),
                        ComparisonOperator.Equal, count),
                    new CausalSequenceEffectNode<TurnStartedTriggeredEffectContext>(
                    [
                        new CreateCardCopyNode<TurnStartedTriggeredEffectContext>(
                            Applicant, iterated, CardZone.Hand,
                            Const<TurnStartedTriggeredEffectContext>(1), copied),
                        new MarkCardInstanceNode<TurnStartedTriggeredEffectContext>(
                            Applicant, Copy, new TagId(EchoMark)),
                        // Free, by taking the whole printed cost off this one copy — the engine's own
                        // per-instance discount, which is spent by the play that uses it.
                        new SetCardInstanceMarkCounterNode<TurnStartedTriggeredEffectContext>(
                            Applicant, Copy, StandardCombatIds.CardCostDeltaCounter,
                            new NegateExpression<TurnStartedTriggeredEffectContext>(
                                new CardInstanceBaseCostExpression<TurnStartedTriggeredEffectContext>(
                                    Copy, StandardCombatIds.EnergyResource)),
                            relative: false),
                        // The record is spent by being answered. The same count will be taken again tonight,
                        // by whichever card is played on it.
                        new MarkCardInstanceNode<TurnStartedTriggeredEffectContext>(
                            Applicant, iterated, new TagId(CountedMark), remove: true),
                    ])),
                maxIterations: WholeDeck, markFilter: new TagId(CountedMark));

        return new ConditionalEffectNode<TurnStartedTriggeredEffectContext>(
            new ComparisonExpression<TurnStartedTriggeredEffectContext>(
                count, ComparisonOperator.GreaterOrEqual, Const<TurnStartedTriggeredEffectContext>(1)),
            new CausalSequenceEffectNode<TurnStartedTriggeredEffectContext>(
            [
                Search(CardZone.Hand), Search(CardZone.DrawPile),
                Search(CardZone.DiscardPile), Search(CardZone.ExhaustPile),
            ]));
    }

    // ⚠ A PROPERTY, for the reason the boss-relic file writes down: a static field declared below the program
    // that names it is still null when that program is built, and a create-node handed a null result key
    // records nothing — the copy is made, and then nothing can find it to stamp or to make free.
    private static EffectResultKey<OrderedTargetOutcomes<CreateCardInstanceOutcome>> Echoed =>
        new("nanna_sin.echoed");

    private static ICardInstanceExpression<TurnStartedTriggeredEffectContext> Copy =>
        new CreateCardOutcomeExpression<TurnStartedTriggeredEffectContext>(Echoed);

    // The sheet, laid in hand every morning the seal is unspent — one lever, offered while it can be used,
    // and consumed by using it. (A lever that hands itself back for ever is an infinite turn: Nanshe's.)
    private static IEffectNode<TurnStartedTriggeredEffectContext> OfferTheSeal() =>
        new ConditionalEffectNode<TurnStartedTriggeredEffectContext>(
            new AndExpression<TurnStartedTriggeredEffectContext>(
                new ComparisonExpression<TurnStartedTriggeredEffectContext>(
                    Stacks<TurnStartedTriggeredEffectContext>(Applicant, IntercalarySealId),
                    ComparisonOperator.GreaterOrEqual, Const<TurnStartedTriggeredEffectContext>(1)),
                new NotExpression<TurnStartedTriggeredEffectContext>(
                    Has<TurnStartedTriggeredEffectContext>(Moon, MoonHeldId))),
            new CreateCardInstanceNode<TurnStartedTriggeredEffectContext>(
                Applicant, new CardDefinitionId(HoldTheMoonCardId), CardZone.Hand,
                Const<TurnStartedTriggeredEffectContext>(1)));

    // ── the night ends ────────────────────────────────────────────────────────────────────────────────────

    // An Echo is a copy of tonight and belongs to tonight. It is swept from wherever it ended up, because the
    // turn's end may have discarded it before this rule ran — and a copy that survived the night would be a
    // permanent addition to a deck that never bought it.
    private static EffectProgram<TurnEndedTriggeredEffectContext> TheNightEnds()
    {
        IEffectNode<TurnEndedTriggeredEffectContext> Sweep(CardZone zone) =>
            new ForEachCardInZoneNode<TurnEndedTriggeredEffectContext>(
                Applicant, zone,
                new MoveCardToZoneNode<TurnEndedTriggeredEffectContext>(
                    Applicant, new IteratedCardExpression<TurnEndedTriggeredEffectContext>(),
                    CardZone.BanishedPile),
                maxIterations: WholeDeck, markFilter: new TagId(EchoMark));

        return new EffectProgram<TurnEndedTriggeredEffectContext>(
            new ConditionalEffectNode<TurnEndedTriggeredEffectContext>(
                PlayersTurn<TurnEndedTriggeredEffectContext>(),
                new CausalSequenceEffectNode<TurnEndedTriggeredEffectContext>(
                [
                    Sweep(CardZone.Hand), Sweep(CardZone.DiscardPile), Sweep(CardZone.ExhaustPile),
                ])));
    }

    // ── the count is taken ────────────────────────────────────────────────────────────────────────────────

    // Two rules, both about the card that was just played, and both about the same night.
    //
    // The count: at `II`, the SECOND card played this turn is Counted. The engine counts the played card
    // BEFORE the trigger goes out (the tracker is registered ahead of the adapter), so the comparison is an
    // equality and not an off-by-one.
    //
    // ⚠ EXCEPT THAT ONLY A CARD THAT WILL STILL EXIST TOMORROW CAN BE COUNTED, and there are two of those in
    // this fight that will not: the Hold the Moon sheet he lays in hand every morning, and an Echo, which is
    // banished at the end of the night it came back on. Counting either is a promise the calendar cannot
    // keep — a COUNTED stamp on a card that is gone before the count comes round is a lie on the card face,
    // which is exactly the hidden rule §3 forbids.
    //
    // They are tallied OUT of the index rather than merely skipped, and that matters: the engine counts every
    // card played, so a player who held the moon and then played the card they meant to count would have
    // played "the second card" and counted nothing at all, silently. Only these two — a temporary card a
    // relic made is a play the player chose to make, and it takes the count like any other.
    //
    // Perfect Reflection: an Echo played under the Full Moon resolves a second time at half — the card's own
    // program, replayed at a scale, which is the only honest way to enhance a card nobody wrote a number for.
    private static EffectProgram<CardPlayedTriggeredEffectContext> TheCountIsTaken()
    {
        var played = new TriggerEventCardInstanceExpression<CardPlayedTriggeredEffectContext>();
        var count = Stacks<CardPlayedTriggeredEffectContext>(Moon, LunarCountId);
        var passing = new OrExpression<CardPlayedTriggeredEffectContext>(
            new CardInstanceHasTagExpression<CardPlayedTriggeredEffectContext>(
                played, new TagId(MoonSheetTag)),
            new CardInstanceHasMarkExpression<CardPlayedTriggeredEffectContext>(
                played, new TagId(EchoMark)));

        // Which play of the player's own this was: everything the engine counted, less what cannot be kept.
        var index = new SubtractExpression<CardPlayedTriggeredEffectContext>(
            new CardsPlayedThisTurnExpression<CardPlayedTriggeredEffectContext>(Applicant),
            new CombatantCounterExpression<CardPlayedTriggeredEffectContext>(Moon, NotKept));

        var counted = new ConditionalEffectNode<CardPlayedTriggeredEffectContext>(
            passing,
            new SetCombatantCounterNode<CardPlayedTriggeredEffectContext>(
                Moon, NotKept, Const<CardPlayedTriggeredEffectContext>(1), relative: true),
            new ConditionalEffectNode<CardPlayedTriggeredEffectContext>(
                new AndExpression<CardPlayedTriggeredEffectContext>(
                    new ComparisonExpression<CardPlayedTriggeredEffectContext>(
                        count, ComparisonOperator.GreaterOrEqual, Const<CardPlayedTriggeredEffectContext>(1)),
                    new ComparisonExpression<CardPlayedTriggeredEffectContext>(
                        index, ComparisonOperator.Equal, count)),
                new CausalSequenceEffectNode<CardPlayedTriggeredEffectContext>(
                [
                    new MarkCardInstanceNode<CardPlayedTriggeredEffectContext>(
                        Applicant, played, new TagId(CountedMark)),
                    new SetCardInstanceMarkCounterNode<CardPlayedTriggeredEffectContext>(
                        Applicant, played, CountedAt, count, relative: false),
                ])));

        var reflected = new ConditionalEffectNode<CardPlayedTriggeredEffectContext>(
            new AndExpression<CardPlayedTriggeredEffectContext>(
                Has<CardPlayedTriggeredEffectContext>(Applicant, PerfectReflectionId),
                new CardInstanceHasMarkExpression<CardPlayedTriggeredEffectContext>(
                    played, new TagId(EchoMark))),
            new ReplayCardProgramNode<CardPlayedTriggeredEffectContext>(
                played, Moon, ReflectionNumerator, ReflectionDenominator));

        return new EffectProgram<CardPlayedTriggeredEffectContext>(
            new ConditionalEffectNode<CardPlayedTriggeredEffectContext>(
                PlayersTurn<CardPlayedTriggeredEffectContext>(),
                new CausalSequenceEffectNode<CardPlayedTriggeredEffectContext>([counted, reflected])));
    }

    // ── his nights ────────────────────────────────────────────────────────────────────────────────────────

    // Tonight's move, exactly as the telegraph reads it.
    private static IEffectNode<EnemyActionContext> TheNight(int phase) => phase switch
    {
        1 => Seq(Hit(Ring[0].Damage), Debuff(Cards.Keywords.Paperwork, 2)),
        3 => Seq(Hit(Ring[2].Damage / 2), Hit(Ring[2].Damage / 2)),
        6 => Seq(Hit(Ring[5].Damage), Debuff(Cards.Keywords.Doubt, 2)),
        8 => Seq(Hit(Ring[7].Damage), Guard(20)),
        _ => Hit(Ring[phase - 1].Damage),
    };

    // §9.5, and it needed no recording at all: the ring is fixed, so the move recorded at this count is
    // always the move of phase 10 − p. On the waxing half that night belongs to the previous orbit, which is
    // why the first orbit is only half full — and which is exactly the "the calendar is mostly empty"
    // §9.9 asks for, arrived at by arithmetic rather than by a rule that hides it.
    private static IEffectNode<EnemyActionContext> TheMoveThatReturns(int phase)
    {
        if (CountOf(phase) < 1)
            return new NoOpEffectNode<EnemyActionContext>();

        var comes = new ConditionalEffectNode<EnemyActionContext>(
            Strengthened<EnemyActionContext>(),
            Hit(Returning(phase, strong: true)),
            Hit(Returning(phase, strong: false)));

        return FromLastOrbit(phase)
            ? new ConditionalEffectNode<EnemyActionContext>(
                At(new CombatantStatusStacksExpression<EnemyActionContext>(
                       Moon, new StatusDefinitionId(OrbitId)), ComparisonOperator.GreaterOrEqual, 2),
                comes)
            : comes;
    }

    // What the returning move comes to. The Full Moon returns harder than the rest of the ring because §9.6
    // says the apex amplifies BOTH sides of recurrence, and the Old Moon raises the whole ring once.
    private static int Returning(int phase, bool strong)
    {
        var full = phase == PeakPhase;
        var percent = (full, strong) switch
        {
            (true, true) => FullMoonOldPercent,
            (true, false) => FullMoonPercent,
            (false, true) => OldMoonPercent,
            (false, false) => ReturnPercent,
        };
        return Ring[RecordedAt(phase) - 1].Damage * percent / 100;
    }

    private static ICombatExpression<TContext, bool> Strengthened<TContext>() where TContext : class =>
        new OrExpression<TContext>(
            Has<TContext>(Moon, OldMoonId), Has<TContext>(Moon, UnendingMonthId));

    // ── the player's one lever ────────────────────────────────────────────────────────────────────────────

    // HOLD THE MOON (§9.8). It buys a night — and pays for it by making one more thing return, his as well as
    // yours. The seal is spent here rather than at the turn start that honours it, so the player can see the
    // decision has been taken.
    private static CardData HoldTheMoon() => new()
    {
        Id = HoldTheMoonCardId,
        NameKey = "Hold the Moon",
        DescriptionKey =
            "The phase does not advance: a second night of this same phase, for you and for him. What you "
            + "counted comes back at once — and so does what he counted. Once an orbit.",
        Costs = [],
        Tags = [new TagId(Cards.CardAuthoring.TemporaryTag), new TagId(MoonSheetTag)],
        Program = new EffectProgram<CardPlayContext>(
            new ConditionalEffectNode<CardPlayContext>(
                new ComparisonExpression<CardPlayContext>(
                    Stacks<CardPlayContext>(Applicant, IntercalarySealId),
                    ComparisonOperator.GreaterOrEqual, Const<CardPlayContext>(1)),
                new CausalSequenceEffectNode<CardPlayContext>(
                [
                    new ModifyStatusStacksNode<CardPlayContext>(
                        Applicant, new StatusDefinitionId(IntercalarySealId), Const<CardPlayContext>(-1)),
                    new ApplyStatusNode<CardPlayContext>(
                        Moon, new StatusDefinitionId(MoonHeldId), Const<CardPlayContext>(1),
                        sourceSelector: Moon),
                ]))),
        PlayedCardDestinationZone = CardZone.ExhaustPile,
        TurnEndHandDestinationZone = CardZone.ExhaustPile,
    };

    // ── shared idioms ─────────────────────────────────────────────────────────────────────────────────────

    // The night the calendar is on, counted from zero: the rounds that have passed, less the ones that were
    // held. Written as arithmetic rather than stepped, so a fight rebuilt from its checkpoint lands on the
    // same sky it left.
    private static ICombatExpression<TContext, int> Nights<TContext>() where TContext : class =>
        new SubtractExpression<TContext>(
            new SubtractExpression<TContext>(
                new RoundNumberExpression<TContext>(), Const<TContext>(1)),
            new CombatantCounterExpression<TContext>(Moon, HeldNights));

    private static ICombatExpression<TContext, int> Phase<TContext>() where TContext : class =>
        new AddExpression<TContext>(
            new RemainderExpression<TContext>(Nights<TContext>(), Const<TContext>(Phases)),
            Const<TContext>(1));

    private static ICombatExpression<TContext, int> Orbit<TContext>() where TContext : class =>
        new AddExpression<TContext>(
            new DivideExpression<TContext>(Nights<TContext>(), Const<TContext>(Phases)),
            Const<TContext>(1));

    // `— I II III IV III II I`, as 4 − |phase − 5|. The absolute value is the larger of a number and its
    // negation, which is the whole of the ring's symmetry in one line.
    private static ICombatExpression<TContext, int> Count<TContext>() where TContext : class
    {
        var offset = new SubtractExpression<TContext>(Phase<TContext>(), Const<TContext>(PeakPhase));
        return new SubtractExpression<TContext>(
            Const<TContext>(PeakCount),
            new MaxExpression<TContext>(offset, new NegateExpression<TContext>(offset)));
    }

    private static ICombatExpression<TContext, bool> At<TContext>(
        ICombatExpression<TContext, int> value, ComparisonOperator op, int number) where TContext : class =>
        new ComparisonExpression<TContext>(value, op, Const<TContext>(number));

    // A face that carries a NUMBER the fight keeps rewriting, on a named body: struck off and written again,
    // because a status merges by adding and every one of these is a total rather than a contribution. Zero is
    // written as absence, which is what "no count tonight" looks like on the New Moon.
    private static IEffectNode<TContext> Restate<TContext>(
        ICombatantTargetSelector body, string statusId, ICombatExpression<TContext, int> value)
        where TContext : class =>
        new CausalSequenceEffectNode<TContext>(
        [
            new RemoveStatusNode<TContext>(body, new StatusDefinitionId(statusId)),
            new ConditionalEffectNode<TContext>(
                new ComparisonExpression<TContext>(value, ComparisonOperator.GreaterOrEqual, Const<TContext>(1)),
                new ApplyStatusNode<TContext>(
                    body, new StatusDefinitionId(statusId), value, sourceSelector: Moon)),
        ]);
}
