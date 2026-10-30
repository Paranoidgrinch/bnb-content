using RogueDeck.Core.Combat;
using RogueDeck.Run;
using RogueDeck.Scenario.Authoring;
using BnbContent.Converter.Relics;

namespace BnbContent.Converter.Events;

// Act IV's eight new doors (content cut 2026-10-30). Each one has a real price or a real risk; the road from
// life into the Judgment of the Dead does not hand anything over for nothing.
public static partial class ActFourEvents
{
    private static IEnumerable<BnbEvent> NewDoors(ConversionPools pools) =>
    [
        SenetWithTheDead(pools),
        TheWorkersBread(),
        TheInnerChamber(pools),
        TheOpeningOfTheMouth(),
        TheBookOfTheDeadVendor(pools),
        TheNegativeConfession(),
        TheBoatOfMillionsOfYears(),
        AmmitsShadow(),
    ];

    private static IRunEffectRequest TombCurse() =>
        new AddCardToDeckRunEffect(new CardDefinitionId(ActFour.TombCurseCardId));

    private static IRunEffectRequest RareCard(ConversionPools pools, string where) =>
        new OfferRewardRunEffect(new RewardId($"{where}:rare"), pools.CardRewardSource("rare", 3), 1)
            { Kind = RewardKinds.Card };

    private static RunCost Blood(int hp) =>
        new(RunExpr.GreaterThan(RunExpr.CurrentHealth, RunExpr.Const(hp)), [new ApplyRunDamageRunEffect(hp)]);

    private static IRunEffectRequest Feather() =>
        new IncrementCounterRunEffect(ActFour.FeathersCarried, 1);

    // A branch whose result is drawn by lot: each outcome is its own page, weighted.
    private static BnbEvent Wagered(
        string id, string name, Band band, string text,
        IReadOnlyList<(string Id, string Text, IReadOnlyList<RunCost> Costs,
            IReadOnlyList<(string Id, int Weight, string Text, IReadOnlyList<IRunEffectRequest> Effects)> Outcomes)> wagers,
        EventBranch? plain = null)
    {
        var choices = wagers.Select(w => new EventChoice(w.Id, [], TextKey: w.Text,
            Costs: w.Costs.Count > 0 ? w.Costs : null,
            Outcomes: [.. w.Outcomes.Select(o => new EventOutcome($"outcome:{w.Id}:{o.Id}", o.Weight))])).ToList();
        if (plain is not null)
            choices.Add(new EventChoice(plain.Id, plain.Effects, NextSituationId: $"result:{plain.Id}",
                TextKey: plain.Text, Costs: plain.Costs));
        var situations = new List<EventSituation> { new("start", text, choices) };
        situations.AddRange(wagers.SelectMany(w => w.Outcomes.Select(o => new EventSituation(
            $"outcome:{w.Id}:{o.Id}", o.Text, [new EventChoice("continue", o.Effects, TextKey: "Continue")]))));
        if (plain is not null)
            situations.Add(new EventSituation($"result:{plain.Id}", plain.Result,
                [new EventChoice("continue", [], TextKey: "Continue")]));
        return new BnbEvent(id, name, new EventScript("start", situations), EarliestDepthPercent: Depth(band),
            Tags: ["gamble"]);
    }

    // ── Senet with the Dead ───────────────────────────────────────────────────────────────────────────────

    private static BnbEvent SenetWithTheDead(ConversionPools pools) => Wagered(
        "senet_with_the_dead", "Senet with the Dead", Band.Mid,
        "A mummy sits at a board of thirty squares and has been waiting a long time for an opponent. It gestures "
        + "at the pieces. It does not say what happens to the one who loses.",
        [
            ("play_for_gold", "Play for Gold (40 Gold) — half the time 110 Gold, half the time nothing.",
                [Price(40)],
                [
                    ("won", 50, "Your last piece crosses the House of Waters. The mummy pays, slowly.", [Gold(110)]),
                    ("lost", 50, "It moves a piece you did not see. The Gold stays on its side of the board.", []),
                ]),
            ("play_for_a_card", "Play for a spell (8 HP) — half the time a Rare card, half the time a Tomb Curse.",
                [Blood(8)],
                [
                    ("won", 50, "It concedes with a bow, and hands you a page it has kept for a thousand years.",
                        [RareCard(pools, "senet_with_the_dead")]),
                    ("lost", 50, "It wins, and what it wins is a piece of your future. A curse follows you out.",
                        [TombCurse()]),
                ]),
        ],
        Branch("decline", "Decline the game.", "The mummy goes on waiting. It is good at it.", []));

    // ── The Workers' Bread ────────────────────────────────────────────────────────────────────────────────

    private static BnbEvent TheWorkersBread() => Event(
        "the_workers_bread", "The Workers' Bread", Band.EarlyMid,
        "The ration for the pyramid gangs, stacked by the ramp: flat loaves and a jar of beer, counted out "
        + "to the man. One share is not accounted for.",
        Branch("eat", "Eat the share.",
            "It is very good bread. It is also somebody's, and the overseer of the next gang has your name.",
            [Heal(30), Openings.NextCombat(Applies(ActFour.BurdenedId, 2))]),
        Branch("give_it_away", "Give yours away.",
            "A labourer takes it without a word. Somewhere a scale notes it in your favour: a Feather.",
            [Feather()]),
        Branch("leave_it", "Leave it counted.", "The tally stays correct. Nobody thanks you for that.", []));

    // ── The Inner Chamber ─────────────────────────────────────────────────────────────────────────────────

    private static BnbEvent TheInnerChamber(ConversionPools pools) => Event(
        "the_inner_chamber", "The Inner Chamber", Band.MidLate,
        "The robbers' tunnel stops one chamber short of the burial. The last seal is still whole, and you "
        + "can see gold through the crack beside it.",
        Branch("go_on", "Go on.",
            "Inside is something worth the whole descent — and the curse that was written over the door.",
            [
                NormalRelic(pools, "the_inner_chamber", RelicAuthoring.Rarity.Rare),
                TombCurse(),
            ]),
        Branch("turn_back", "Turn back.",
            "You leave the seal for the next fool. The climb out is quiet, and you rest on the way.",
            [Heal(10)]));

    // ── The Opening of the Mouth ──────────────────────────────────────────────────────────────────────────

    private static BnbEvent TheOpeningOfTheMouth() => Event(
        "the_opening_of_the_mouth", "The Opening of the Mouth", Band.Mid,
        "A priest with an adze of meteoric iron offers to open the mouth of one of your procedures, so that "
        + "it may speak in the hall. The rite is for the dead, and it treats you as one.",
        Branch("open_one", "Open one mouth.",
            "One card comes back able to speak — and for the next three fights you are a little more buried.",
            [Upgrade(1, "choose the mouth to open"), Openings.ForFights(Openings.EventFights, Applies(ActFour.EntombedId, 1))]),
        Branch("open_two", "Open two.",
            "Two procedures, and twice the burial: the next three fights start Entombed 2.",
            [Upgrade(2, "choose the mouths to open"), Openings.ForFights(Openings.EventFights, Applies(ActFour.EntombedId, 2))]),
        Branch("refuse", "Refuse the rite.", "The priest puts the adze away. You are not dead yet.", []));

    // ── The Book of the Dead Vendor ───────────────────────────────────────────────────────────────────────

    private static BnbEvent TheBookOfTheDeadVendor(ConversionPools pools) => Event(
        "the_book_of_the_dead_vendor", "The Book of the Dead Vendor", Band.All,
        "A stall of papyrus rolls, each one a spell for the road ahead. The vendor has two prices, and is "
        + "frank about the cheaper one.",
        Branch("buy", "Buy a spell (75 Gold).",
            "You choose a roll and it is yours, properly blessed.",
            [RareCard(pools, "the_book_of_the_dead_vendor")], costs: [Price(75)]),
        Branch("buy_cursed", "Buy at the curse price (30 Gold).",
            "The cheap copy was written by somebody's enemy. The spell works; so does the curse in the margin.",
            [RareCard(pools, "the_book_of_the_dead_vendor_cursed"), TombCurse()], costs: [Price(30)]),
        Branch("browse", "Only browse.", "The vendor rolls the spells back up. There is always another reign.", []));

    // ── The Negative Confession ───────────────────────────────────────────────────────────────────────────

    private static BnbEvent TheNegativeConfession() => Event(
        "the_negative_confession", "The Negative Confession", Band.MidLate,
        "Forty-two assessors in a row, and a list of things you have not done. Deny them, and what you deny "
        + "is struck out of you — but every denial is weighed, later.",
        Branch("deny_two", "Deny two things.",
            "Two cards are struck out. The next three fights start Weighed 3: the hall measures what is left.",
            [Remove("choose the first denial"), Remove("choose the second denial"),
             Openings.ForFights(Openings.EventFights, Applies(ActFour.WeighedId, 3))]),
        Branch("deny_one", "Deny one.",
            "One card is struck out, and the next three fights start Weighed 2.",
            [Remove("choose what you deny"), Openings.ForFights(Openings.EventFights, Applies(ActFour.WeighedId, 2))]),
        Branch("confess", "Confess nothing.", "The assessors write nothing. That, too, is noted.", []));

    // ── The Boat of Millions of Years ─────────────────────────────────────────────────────────────────────

    private static BnbEvent TheBoatOfMillionsOfYears() => Event(
        "the_boat_of_millions_of_years", "The Boat of Millions of Years", Band.All,
        "The sun's boat is moored in a corridor with no river, and the boatman will take one passenger past "
        + "the next room without stopping. The fare is what it always is.",
        Branch("pay_gold", "Pay in Gold (60).",
            "The boat carries you past the next ordinary fight: it is won before it starts.",
            [Install(ActFourEventPrograms.BoatRide)], costs: [Price(60)]),
        Branch("pay_blood", "Pay in blood (10 HP).",
            "The boatman takes his fare from you, and the next ordinary fight is passed in the dark.",
            [Install(ActFourEventPrograms.BoatRide)], costs: [Blood(10)]),
        Branch("walk", "Walk.", "The boat casts off without you. The corridor goes on.", []));

    // ── Ammit's Shadow ────────────────────────────────────────────────────────────────────────────────────

    private static BnbEvent AmmitsShadow() =>
        new("ammits_shadow", "Ammit's Shadow", AmmitScript(), EarliestDepthPercent: Depth(Band.MidLate));

    private static EventScript AmmitScript()
    {
        var feed = new EventChoice("feed", [new ChangeMaxHealthRunEffect(-5), Feather(), Feather()],
            NextSituationId: "result:feed", TextKey: "Feed it (5 Max HP) — it leaves you 2 Feathers.");
        var sneak = new EventChoice("sneak", [],
            TextKey: "Sneak past — half the time unseen, half the time the next three fights start Entombed 1.",
            Outcomes: [new EventOutcome("outcome:sneak:unseen", 50), new EventOutcome("outcome:sneak:seen", 50)]);
        var challenge = new EventChoice("challenge", [Install(ActFourEventPrograms.AmmitChallenged)],
            NextSituationId: "result:challenge",
            TextKey: "Challenge it — its shadow joins the next ordinary fight; win for an elite's reward.");
        EventChoice Next() => new("continue", [], TextKey: "Continue");
        return new EventScript("start",
        [
            new("start", "Something with a crocodile's head and a lion's shoulders lies across the corridor, "
                + "asleep or pretending to be. It has eaten hearts here before. It is hungry now.",
                [feed, sneak, challenge]),
            new("result:feed", "It eats what you give it, and looks at you as if you were lighter now.", [Next()]),
            new("outcome:sneak:unseen", "Its nostrils flare as you pass, and settle. You are through.", [Next()]),
            new("outcome:sneak:seen", "One eye opens. It does not move, but it remembers you.",
                [new EventChoice("continue",
                    [Openings.ForFights(Openings.EventFights, Applies(ActFour.EntombedId, 1))], TextKey: "Continue")]),
            new("result:challenge", "It rises. Not here, it seems to say. Further on, where you will have to fight.",
                [Next()]),
        ]);
    }
}
