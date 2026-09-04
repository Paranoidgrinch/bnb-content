using RogueDeck.Core.Combat;
using RogueDeck.Run;
using RogueDeck.Sandbox.Composition;
using RogueDeck.Scenario.Authoring;
using BnbContent.Converter.Relics;

namespace BnbContent.Converter.Events;

// Act IV's doors, from `source-data/design/BnB_Final_Events_Master_PostAudit.md` §"ACT IV" — all twenty.
//
// The Licensing Labyrinth's doors are OFFICES, and an office does not bargain: it measures you, files the
// measurement, and the filing follows you down the corridor. That is why so many of these promise a STRETCH
// of road rather than a single fight — "the next three combats start Inscribed 1" — and why nearly every one
// of them is written in the act's own five words. The exceptions are the four doors that offer a fight, and
// what a door can do about a fight is set one on the road ahead (see ActFourEventPrograms).
//
// Nine of the twenty hand over an Event relic, and each of the nine is the ONLY place that relic can be got.
//
// The design's §4.6 "shop-like event markets" are all in earlier acts (the Licensed Vendor, the Conceptual
// Toll, the Travelling Chandler); the Labyrinth has none. Its offices do not sell — they assess.
public static class ActFourEvents
{
    public const int Act = 4;

    public static IReadOnlyList<BnbEvent> All(ConversionPools pools, Random rng)
    {
        _ = rng; // the Labyrinth's doors are all authored; nothing here is dealt per run.
        return
        [
            TheDryNilometer(),
            TheBlackGranary(pools),
            TheRedLinenProcession(),
            TheNamelessCartouche(),
            TheForewrittenTablet(pools),
            TheTombRobbersFire(pools),
            TheTripleCountedDonkey(pools),
            TheFourCanopicJars(),
            TheChamberOfFalseMeasures(),
            TheCrocodileAtTheWeighingPlace(pools),
            TheWallOfOldComplaints(),
            TheCopperTithe(pools),
            TheUnnamedThrone(),
            TheFixedDayFestival(),
            TheBrokenSluice(),
            TheUnfinishedBurial(pools),
            TheSurveyOfTheDead(),
            TheHouseOfLifeAtNight(pools),
            TheMercifulBalance(),
            TheCartoucheRepairBench(pools),
        ];
    }

    // ── 1 · Early–Mid ─────────────────────────────────────────────────────────────────────────────────────

    private static BnbEvent TheDryNilometer() => Event(
        "the_dry_nilometer", "The Dry Nilometer", Band.EarlyMid,
        "A stair cut down into a shaft that has not held water in living memory. The marks are still there, "
        + "and so is the official who reads them, and he is reading them.",
        Branch("accept_the_true_level", "Accept the True Level.",
            "He writes down the lowest mark, which is the true one, and it costs you six of yourself to be "
            + "measured honestly. He gives you the cup he measures with.",
            [new ChangeMaxHealthRunEffect(-6), .. Grant(ActFourEventRelicRules.CupId)]),
        Branch("move_the_marker", "Move the Marker.",
            "Ninety Gold for a level that suits everybody. It is entered in the register, and the register "
            + "follows you for two rooms.",
            [Gold(90), .. Stretch(ActFourEventPrograms.Inscribed1, 2)]),
        Branch("leave_unmeasured", "Leave Unmeasured.",
            "Nothing is written down at all, which is restful. The next office you meet has to start from "
            + "the beginning, and starts with you.",
            [Heal(25), Openings.NextCombat(Applies("paperwork", 2), Applies(ActFour.WeighedId, 3))]));

    // ── 2 · Early–Mid ─────────────────────────────────────────────────────────────────────────────────────

    private static BnbEvent TheBlackGranary(ConversionPools pools) => Event(
        "the_black_granary", "The Black Granary", Band.EarlyMid,
        "Sealed since a year nobody can name, and full — you can hear it. The seal is a clay disc the size "
        + "of a hand, and it is the only thing between the grain and everyone who is hungry.",
        Branch("break_the_seal", "Break the Seal.",
            "A hundred and thirty Gold's worth of grain and something that was buried with it. You carry it "
            + "out yourself, and you carry it for two rooms.",
            [
                Gold(130),
                NormalRelic(pools, "the_black_granary", RelicAuthoring.Rarity.Common),
                .. Stretch(ActFourEventPrograms.Burdened2, 2),
            ]),
        Branch("accept_the_share", "Accept the Allotted Share.",
            "What is allotted is allotted. It is a great deal of food and slightly less of you afterwards.",
            [Heal(35), new ChangeMaxHealthRunEffect(-5)]),
        Branch("restore_the_record", "Restore the Record.",
            "You put the tally back in order. Two of your procedures come out of it better, and the granary "
            + "enters your name for having done it.",
            [
                Upgrade(2, "choose two to put in order"),
                .. Stretch(ActFourEventPrograms.Inscribed1, 1),
            ]));

    // ── 3 · All ───────────────────────────────────────────────────────────────────────────────────────────

    private static BnbEvent TheRedLinenProcession() => Event(
        "the_red_linen_procession", "The Red Linen Procession", Band.All,
        "Red linen, carried the length of the corridor by people who do not look at it. There is no body in "
        + "it. There is a very great deal of paperwork in it.",
        Branch("join_the_procession", "Join the Procession.",
            "You walk with them as far as the turn, and one of your own procedures goes into the wrapping. "
            + "The linen keeps what it is given.",
            [
                Remove("choose what goes into the wrapping"),
                Heal(15),
                Openings.NextCombat(Applies(ActFour.EmbalmedId, 2)),
            ]),
        Branch("cut_the_linen", "Cut the Linen.",
            "It parts, and what is inside is a filing you can improve twice. The corridor closes behind you "
            + "for having done it.",
            [
                Upgrade(2, "choose two to correct from the wrapping"),
                Openings.NextCombat(Applies(ActFour.EntombedId, 2)),
            ]),
        Branch("follow_to_the_last_gate", "Follow Until the Last Gate.",
            "It is much farther than it looked and it costs twelve of you to arrive. At the gate they cut "
            + "you a knot of the linen, which is not a small thing to be given.",
            [new ApplyRunDamageRunEffect(12), .. Grant(ActFourEventRelicRules.KnotId)]));

    // ── 4 · All ───────────────────────────────────────────────────────────────────────────────────────────

    private static BnbEvent TheNamelessCartouche() => Event(
        "the_nameless_cartouche", "The Nameless Cartouche", Band.All,
        "An oval cut deep into the wall with nothing inside it. Whoever it belonged to has been removed from "
        + "the record so thoroughly that the hole itself is now the record.",
        Branch("inscribe_your_name", "Inscribe Your Name.",
            "It fits. Two of your procedures are the better for having a name over them, and every office "
            + "for the next three rooms has a name to enter.",
            [
                Upgrade(2, "choose two to sign"),
                .. Stretch(ActFourEventPrograms.Inscribed1, 3),
            ]),
        Branch("scrape_it_deeper", "Scrape It Deeper.",
            "You take the hole down to clean stone. Something of yours goes with it and so does seven of you.",
            [Remove("choose what is scraped out with it"), new ChangeMaxHealthRunEffect(-7)]),
        Branch("take_the_fragment", "Take the Fragment.",
            "A chip of the empty oval, pocket-sized. It is a cartouche with nobody in it, which turns out to "
            + "be a useful thing to be carrying in a building like this.",
            [.. Grant(ActFourEventRelicRules.CartoucheId)]));

    // ── 5 · Mid–Late ──────────────────────────────────────────────────────────────────────────────────────

    private static BnbEvent TheForewrittenTablet(ConversionPools pools) => Event(
        "the_forewritten_tablet", "The Forewritten Tablet", Band.MidLate,
        "The tablet describes, in the past tense, everything you are about to do in this corridor. Three "
        + "scribes stand around it. One of them is already writing the next line.",
        Branch("correct_one_line", "Correct One Line.",
            "One line, and the thing it described is a different thing now — a better one — and there is "
            + "fifty Gold in the correction fee for having found the error yourself.",
            [
                new TransformCardsRunEffect(Choose("choose the line to correct"), pools.TransformPool()),
                new UpgradeCardsRunEffect(RunSelectors.LastAddedCard),
                Gold(50),
            ]),
        Branch("demand_the_tablet", "Demand the Tablet.",
            "They will not give it up here. They will be waiting on the next ordinary stretch of corridor, "
            + "all three of them, writing about whoever is standing there — and there is a relic in it if "
            + "you take the tablet off them.",
            [Install(ActFourEventPrograms.TabletDemanded)]),
        Branch("sign_beneath_it", "Sign Beneath It.",
            "Signing makes it true, and two of your own procedures are struck out to make room for it. The "
            + "paperwork that follows follows you for two rooms.",
            [
                Remove("choose the first to be struck out"),
                Remove("choose the second to be struck out"),
                .. Stretch(ActFourEventPrograms.Paperwork3, 2),
            ]));

    // ── 6 · Mid–Late ──────────────────────────────────────────────────────────────────────────────────────

    private static BnbEvent TheTombRobbersFire(ConversionPools pools) => Event(
        "the_tomb_robbers_fire", "The Tomb Robbers' Fire", Band.MidLate,
        "Three of them, a small fire, and a great deal that is not theirs laid out on a cloth. They are not "
        + "hiding. Nobody licensed comes down this far.",
        Branch("trade", "Trade.",
            "Seventy Gold, and they hand over something they clearly want rid of. The next corridor knows "
            + "where you got it.",
            [
                NormalRelic(pools, "the_tomb_robbers_fire", RelicAuthoring.Rarity.Uncommon),
                Openings.NextCombat(Applies(ActThree.TrespassId, 1)),
            ],
            costs: [Price(70)]),
        Branch("join_the_opening", "Join the Opening.",
            "You are in on it now, which means you are in on it when they are found. It will be the next "
            + "ordinary stretch of corridor, the lamp will go out first, and the take is a hundred and "
            + "twenty Gold and a piece of what was in there.",
            [Install(ActFourEventPrograms.RobbersJoined)]),
        Branch("steal_from_the_thieves", "Steal from the Thieves.",
            "A hundred Gold off the cloth while they are arguing. They come looking, and they are still "
            + "looking two rooms later.",
            [Gold(100), .. Stretch(ActFourEventPrograms.PanicAndBurden, 2)]));

    // ── 7 · Early–Mid ─────────────────────────────────────────────────────────────────────────────────────

    private static BnbEvent TheTripleCountedDonkey(ConversionPools pools) => Event(
        "the_triple_counted_donkey", "The Triple-Counted Donkey", Band.EarlyMid,
        "One donkey, three tokens, three offices — each of which counted it, and each of which is correct. "
        + "The donkey is standing in the corridor waiting to be resolved.",
        Branch("honor_the_first_tally", "Honor the First Tally.",
            "The oldest count wins, which is the tidiest answer and pays seventy-five Gold. You carry the "
            + "other two tokens out with you.",
            [Gold(75), Openings.NextCombat(Applies(ActFour.BurdenedId, 1))]),
        Branch("break_all_three", "Break All Three Tokens.",
            "There is no donkey now, officially. It costs you a little blood and one of your own procedures, "
            + "and you come out of it more solid than you went in.",
            [
                Remove("choose what is broken with them"),
                new ApplyRunDamageRunEffect(5),
                new ChangeMaxHealthRunEffect(5),
            ]),
        Branch("follow_the_donkey", "Follow the Donkey.",
            "It knows a way through. Where it stops there is water and something somebody left behind.",
            [
                NormalRelic(pools, "the_triple_counted_donkey", RelicAuthoring.Rarity.Common),
                Heal(10),
            ]));

    // ── 8 · Mid–Late ──────────────────────────────────────────────────────────────────────────────────────

    private static BnbEvent TheFourCanopicJars() => Event(
        "the_four_canopic_jars", "The Four Canopic Jars", Band.MidLate,
        "Four jars on a shelf, each labelled with what it holds and each label in a different hand. Only one "
        + "of them is what it says. You may open one.",
        Branch("jar_of_breath", "Jar of Breath.",
            "It is empty, and the emptiness is the point: it is a jar for holding a breath somebody else has "
            + "finished with.",
            [.. Grant(ActFourEventRelicRules.JarId)]),
        Branch("jar_of_blood", "Jar of Blood.",
            "Twelve more of you, all at once, and a great deal of it in your mouth for the next room.",
            [new ChangeMaxHealthRunEffect(12), Openings.NextCombat(Applies("poison", 5))]),
        Branch("jar_of_hunger", "Jar of Hunger.",
            "A hundred and fifty Gold in old coin, and every bit of it wants carrying.",
            [Gold(150), .. Stretch(ActFourEventPrograms.Burdened2, 1)]),
        Branch("jar_of_the_name", "Jar of the Name.",
            "The name in it is not yours, but three of your procedures are entered under it, and the next "
            + "office finds you very thoroughly on the register.",
            [
                Upgrade(3, "choose three to enter under the name"),
                Openings.NextCombat(Applies(ActFour.InscribedId, 2)),
            ]));

    // ── 9 · Mid ───────────────────────────────────────────────────────────────────────────────────────────

    private static BnbEvent TheChamberOfFalseMeasures() => Event(
        "the_chamber_of_false_measures", "The Chamber of False Measures", Band.Mid,
        "Every weight in the room is stamped with the royal cartouche and no two of them agree. The scale in "
        + "the middle is the only honest object here, and it is waiting.",
        Branch("heavy_weight", "Heavy Weight.",
            "You take the heaviest, which makes you heavier, and every measure for the next two rooms is "
            + "taken against it.",
            [new ChangeMaxHealthRunEffect(10), .. Stretch(ActFourEventPrograms.Weighed3, 2)]),
        Branch("light_weight", "Light Weight.",
            "The lightest weighs almost nothing, and so, briefly, do you. You come out whole and eight "
            + "short of what you were.",
            [Heal(100), new ChangeMaxHealthRunEffect(-8)]),
        Branch("break_the_scale", "Break the Scale.",
            "Fifteen of you and one royal weight, broken in half on the floor. Half a royal weight still "
            + "weighs whatever it is told to weigh, which is the whole of the trick.",
            [new ApplyRunDamageRunEffect(15), .. Grant(ActFourEventRelicRules.WeightId)]));

    // ── 10 · Mid–Late ─────────────────────────────────────────────────────────────────────────────────────

    private static BnbEvent TheCrocodileAtTheWeighingPlace(ConversionPools pools) => Event(
        "the_crocodile_at_the_weighing_place", "The Crocodile at the Weighing Place", Band.MidLate,
        "The scale is set up properly, the offerings are stacked beside it, and the thing waiting under the "
        + "scale for whatever fails the weighing has been waiting a very long time.",
        Branch("offer_gold", "Offer Gold.",
            "Sixty Gold onto the pan. It is accepted, which is not the same as being weighed, and you are "
            + "the better for the arrangement.",
            [new ChangeMaxHealthRunEffect(6), Heal(15)],
            costs: [Price(60)]),
        Branch("place_yourself", "Place Yourself on the Scale.",
            "You are weighed, and what you are found to be worth is handed to you. The finding is entered, "
            + "and the corridor has the entry before you do.",
            [
                NormalRelic(pools, "the_crocodile", RelicAuthoring.Rarity.Uncommon),
                Openings.NextCombat(Applies(ActFour.WeighedId, 3), Applies(ActFour.EntombedId, 1)),
            ]),
        Branch("take_the_offerings", "Take the Offerings.",
            "A hundred and twenty Gold that was meant for somebody who is not coming. It is noted. It is "
            + "noted for three rooms.",
            [Gold(120), .. Stretch(ActFourEventPrograms.Inscribed1, 3)]));

    // ── 11 · All ──────────────────────────────────────────────────────────────────────────────────────────

    private static BnbEvent TheWallOfOldComplaints() => Event(
        "the_wall_of_old_complaints", "The Wall of Old Complaints", Band.All,
        "Every hand's breadth of it is a grievance somebody scratched in and nobody answered, going back so "
        + "far that the oldest are underneath the newest and the wall is thicker for it.",
        Branch("add_your_own", "Add Your Own.",
            "You scratch in your two corrections, which is two of your procedures improved and one more "
            + "complaint on a wall of them. The next office has read it.",
            [Upgrade(2, "choose two complaints worth making properly"), .. Stretch(ActFourEventPrograms.Paperwork3, 1)]),
        Branch("erase_one", "Erase One.",
            "You take one grievance off the wall — yours — and it costs six of you to have never had it.",
            [Remove("choose the complaint to withdraw"), new ChangeMaxHealthRunEffect(-6)]),
        Branch("read_them_all", "Read Them All.",
            "All of them, in order, which takes as long as it takes. What you carry away is the chisel "
            + "somebody left in the wall, and two rooms' worth of doubt about everything.",
            [.. Grant(ActFourEventRelicRules.ChiselId), .. Stretch(ActFourEventPrograms.Doubt1, 2)]));

    // ── 12 · All ──────────────────────────────────────────────────────────────────────────────────────────

    private static BnbEvent TheCopperTithe(ConversionPools pools) => Event(
        "the_copper_tithe", "The Copper Tithe", Band.All,
        "A table across the corridor, a copper bowl on the table, and a bearer beside the bowl who has been "
        + "told what the tithe is and has no authority to hear anything else.",
        Branch("pay_the_tithe", "Pay the Tithe.",
            "A part of what you carry into the bowl, and the receipt is worth having: two of your procedures "
            + "come back stamped.",
            [LosePercentGold(15), Upgrade(2, "choose two the receipt covers")]),
        Branch("give_more_than_required", "Give More Than Required.",
            "More than a third, which nobody asks twice about. What comes back out of the bowl was not put "
            + "there by you.",
            [
                LosePercentGold(35),
                new OfferRewardRunEffect(
                    new RewardId("event:copper_tithe:relic"),
                    pools.NormalRelicOfRarity("the Copper Tithe's overpayment",
                        (RelicAuthoring.Rarity.Uncommon, 50), (RelicAuthoring.Rarity.Rare, 50)),
                    1),
            ]),
        Branch("give_nothing", "Give Nothing.",
            "You keep what you carry, which is the whole of the point. The bearer writes nothing down and "
            + "sends word ahead, and the word is waiting on the next ordinary road.",
            [Install(ActFourEventPrograms.TitheRefused)]));

    // ── 13 · Late · Rare ──────────────────────────────────────────────────────────────────────────────────

    private static BnbEvent TheUnnamedThrone() => Event(
        "the_unnamed_throne", "The Unnamed Throne", Band.Late,
        "A throne with the name cut out of it — not worn away, cut, carefully, by somebody who was paid to "
        + "and knew exactly how much had to go. The gold leaf around the hole is untouched.",
        Branch("restore_the_name", "Restore the Name.",
            "You cut a name back into it, which is eight of yourself and a very great deal of nerve. The "
            + "tablet they used for the erasure is still under the seat, and it works both ways.",
            [new ChangeMaxHealthRunEffect(-8), .. Grant(ActFourEventRelicRules.TabletId)]),
        Branch("erase_it_completely", "Erase It Completely.",
            "The rest of it goes, and two of your own procedures go with it because the chisel does not "
            + "distinguish. The corridor afterwards is not a restful place.",
            [
                new RemoveCardsRunEffect(
                    RunSelectors.DeckCards.ChooseByPlayer(2, "choose the two that go with the name")),
                .. Stretch(ActFourEventPrograms.Panic2, 1),
            ]),
        Branch("take_the_gold_leaf", "Take the Gold Leaf.",
            "A hundred and fifty Gold's worth of leaf off a throne nobody sits on, and three rooms of "
            + "paperwork about where it went.",
            [Gold(150), .. Stretch(ActFourEventPrograms.Paperwork2, 3)]));

    // ── 14 · Mid ──────────────────────────────────────────────────────────────────────────────────────────

    private static BnbEvent TheFixedDayFestival() => Event(
        "the_fixed_day_festival", "The Fixed-Day Festival", Band.Mid,
        "The festival is held on a fixed day, which was fixed by a calendar that has since been corrected "
        + "twice. It is being held anyway, today, by people who are not going to be told otherwise.",
        Branch("carry_the_standard", "Carry the Standard.",
            "You carry it the length of the hall, which is heavier than it looks and improves one deed and "
            + "one working of yours in the eyes of everyone watching.",
            [
                UpgradeOfKind(Cards.CardAuthoring.DeedTag, "choose the deed the standard honours"),
                UpgradeOfKind(Cards.CardAuthoring.WorkingTag, "choose the working the standard honours"),
                .. Stretch(ActFourEventPrograms.Burdened1, 1),
            ]),
        Branch("beat_the_drum", "Beat the Drum.",
            "You beat it badly and much too fast, and for two rooms afterwards everything begins in a hurry "
            + "— more in hand at once, and less sense in it.",
            [.. Stretch(ActFourEventPrograms.EnergyAndPanic, 2)]),
        Branch("wait_for_the_correct_star", "Wait for the Correct Star.",
            "You wait for the star the festival was meant for. It comes up, eventually, and you are a great "
            + "deal better for the rest and forty Gold poorer for the wait.",
            [Heal(40), Gold(-40)]));

    // ── 15 · Early–Mid ────────────────────────────────────────────────────────────────────────────────────

    private static BnbEvent TheBrokenSluice() => Event(
        "the_broken_sluice", "The Broken Sluice", Band.EarlyMid,
        "A sluice gate jammed half open, and behind it water that has been standing long enough to be "
        + "somebody's responsibility. There is a form for opening it and a form for closing it properly.",
        Branch("open_it", "Open It.",
            "It goes with a noise, and what comes through is clean and cold and takes a purse's worth of "
            + "everything loose with it.",
            [Heal(25), Gold(-50)]),
        Branch("close_it_properly", "Close It Properly.",
            "Properly means the form, the seal and an hour on your knees in the channel. You come out of it "
            + "eight the sturdier and carrying the hour into the next room.",
            [new ChangeMaxHealthRunEffect(8), .. Stretch(ActFourEventPrograms.Burdened1, 1)]),
        Branch("reroute_the_channel", "Reroute the Channel.",
            "You send the water the way it should have gone in the first place. Two of your procedures are "
            + "the better for the survey, and two rooms of it are measured against what you drew.",
            [Upgrade(2, "choose two the survey corrects"), .. Stretch(ActFourEventPrograms.Weighed2, 2)]));

    // ── 16 · Mid–Late ─────────────────────────────────────────────────────────────────────────────────────

    private static BnbEvent TheUnfinishedBurial(ConversionPools pools) => Event(
        "the_unfinished_burial", "The Unfinished Burial", Band.MidLate,
        "Wrapped to the shoulders and left. The linen is still soft, the tools are still laid out in order, "
        + "and whoever was doing it put them down mid-turn and did not come back.",
        Branch("finish_the_wrapping", "Finish the Wrapping.",
            "You finish it the way it was being done, and something of yours goes into the last turn of the "
            + "linen. What is left over on the spool is yours.",
            [Remove("choose what goes into the last turn"), .. Grant(ActFourEventRelicRules.CoilId)]),
        Branch("take_the_amulet", "Take the Amulet.",
            "It is under the third layer, where they always are. Taking it costs the wrapping, and the "
            + "wrapping notices: the next fight begins preserved and half buried.",
            [
                NormalRelic(pools, "the_unfinished_burial", RelicAuthoring.Rarity.Uncommon),
                Openings.NextCombat(Applies(ActFour.EmbalmedId, 3), Applies(ActFour.EntombedId, 1)),
            ]),
        Branch("unwrap_the_name", "Unwrap the Name.",
            "You go back to the collar for the name, and two of your own procedures come away as something "
            + "else entirely. Two rooms of the register have you now.",
            [Transform(2, pools, "choose two to unwrap"), .. Stretch(ActFourEventPrograms.Inscribed1, 2)]));

    // ── 17 · Late ─────────────────────────────────────────────────────────────────────────────────────────

    private static BnbEvent TheSurveyOfTheDead() => Event(
        "the_survey_of_the_dead", "The Survey of the Dead", Band.Late,
        "A table, three clerks and a queue that does not move, because the survey counts the dead and the "
        + "living in one column and settles which you are at the table.",
        Branch("be_counted_among_the_living", "Be Counted Among the Living.",
            "You are entered as living, and the entry is worth everything: you come away whole. It costs "
            + "eight off the ceiling, because a living body is a body that can be counted again.",
            [Heal(100), new ChangeMaxHealthRunEffect(-8)]),
        Branch("be_counted_among_the_dead", "Be Counted Among the Dead.",
            "Entered as dead, which nobody argues with. The dead are wrapped properly, and the wrapping is "
            + "on you for three rooms.",
            [new ChangeMaxHealthRunEffect(12), .. Stretch(ActFourEventPrograms.Embalmed1, 3)]),
        Branch("refuse_the_count", "Refuse the Count.",
            "You will be one or the other and not on their say-so. Three clerks leave the table at once, and "
            + "they are not going to catch up with you until the next ordinary road.",
            [Install(ActFourEventPrograms.CountRefused)]));

    // ── 18 · Late · Rare ──────────────────────────────────────────────────────────────────────────────────

    private static BnbEvent TheHouseOfLifeAtNight(ConversionPools pools) => Event(
        "the_house_of_life_at_night", "The House of Life at Night", Band.Late,
        "The scriptorium after hours, which is when the real work is done: copying, erasing and replacing "
        + "lines in formulae nobody is going to check until it is far too late.",
        Branch("copy_a_formula", "Copy a Formula.",
            "One of yours, copied out fair, twice as much of it as you had. There is paperwork about the "
            + "second copy for exactly one room.",
            [
                new DuplicateCardsRunEffect(Choose("choose the formula to copy out fair")),
                .. Stretch(ActFourEventPrograms.Paperwork2, 1),
            ]),
        Branch("erase_a_formula", "Erase a Formula.",
            "A line taken out of the world entirely. You are five the sturdier for its absence, which is how "
            + "these things usually go.",
            [Remove("choose the formula to erase"), new ChangeMaxHealthRunEffect(5)]),
        Branch("replace_a_line", "Replace a Line.",
            "Two of yours become two others, and both are written in the improved hand. The register does "
            + "not miss a substitution of that size.",
            [
                Transform(2, pools, "choose two lines to replace"),
                Upgrade(2, "choose two to write out in the better hand"),
                .. Stretch(ActFourEventPrograms.Inscribed2, 1),
            ]));

    // ── 19 · All ──────────────────────────────────────────────────────────────────────────────────────────

    private static BnbEvent TheMercifulBalance() => Event(
        "the_merciful_balance", "The Merciful Balance", Band.All,
        "A scale kept by an office that grants relief, which means the pans are the same as everywhere else "
        + "and the difference is entirely in what they will let you put on them.",
        Branch("place_gold_on_the_pan", "Place Gold on the Pan.",
            "Seventy-five Gold, weighed and accepted, and a thing struck from your file for it.",
            [Remove("choose what the relief strikes out")],
            costs: [Price(75)]),
        Branch("place_blood_on_the_pan", "Place Blood on the Pan.",
            "Ten off the ceiling and onto the pan. Relief granted: two of your procedures come back in "
            + "better order than you left them.",
            [new ChangeMaxHealthRunEffect(-10), Upgrade(2, "choose two the relief puts in order")]),
        Branch("place_your_burden_on_the_pan", "Place Your Burden on the Pan.",
            "What you put on the pan is what you have been carrying, and they take it, and they give you "
            + "back the counterweight. It is heavy, and the next room is heavier.",
            [
                .. Grant(ActFourEventRelicRules.MercyId),
                Openings.NextCombat(Applies(ActFour.BurdenedId, 2), Applies(ActFour.EntombedId, 1)),
            ]));

    // ── 20 · All ──────────────────────────────────────────────────────────────────────────────────────────

    private static BnbEvent TheCartoucheRepairBench(ConversionPools pools) => Event(
        "the_cartouche_repair_bench", "Cartouche Repair Bench", Band.All,
        "A bench, a rack of chisels by size, and a row of names in various stages of being put right. The "
        + "one at the near end has been at the near end for some time.",
        Branch("restore_the_name", "Restore the Name.",
            "You put one of them right, which is slow work and good work, and the bench is a restful place "
            + "to have spent an afternoon.",
            [Upgrade(1, "choose the name to put right"), Heal(15)]),
        Branch("replace_the_name", "Replace the Name.",
            "Out with one and in with another, and fifty Gold from a drawer for the trouble. Nobody at the "
            + "bench asks whose it was.",
            [Transform(1, pools, "choose the name to replace"), Gold(50)]),
        Branch("leave_no_name", "Leave No Name.",
            "You take a name out and put nothing in, which is the one thing the bench is not for. The "
            + "register spends the next room looking for what should have been there.",
            [Remove("choose the name to leave out"), .. Stretch(ActFourEventPrograms.Inscribed2, 1)]));

    // ── the shared shapes ─────────────────────────────────────────────────────────────────────────────────

    private static IRunSelector<RunCardInstance> Choose(string purpose) =>
        RunSelectors.DeckCards.ChooseByPlayer(1, purpose);

    private static IRunEffectRequest Gold(int amount) =>
        new ChangeResourceRunEffect(StandardRunIds.Gold, amount);

    private static RunCost Price(int gold) =>
        new(RunExpr.HasResource(StandardRunIds.Gold, gold), [Gold(-gold)]);

    // "Heal N% of Max HP", rounded up, as every act before this one does it.
    private static IRunEffectRequest Heal(int percent) =>
        new ComputedHealRunEffect(RunExpr.Divide(
            RunExpr.Add(RunExpr.Multiply(RunExpr.MaxHealth, RunExpr.Const(percent)), RunExpr.Const(99)),
            RunExpr.Const(100)));

    private static IRunEffectRequest Upgrade(int count, string purpose) =>
        new UpgradeCardsRunEffect(RunSelectors.DeckCards.Upgradable().ChooseByPlayer(count, purpose));

    private static IRunEffectRequest Remove(string purpose) =>
        new RemoveCardsRunEffect(Choose(purpose));

    private static IRunEffectRequest Install(string program) =>
        new InstallProgramByIdRunEffect(new RunProgramSourceId(program));

    // "A random eligible Normal Relic" of one rarity, auto-taken — the Labyrinth's doors hand these over
    // rather than offering a choice.
    private static IRunEffectRequest NormalRelic(
        ConversionPools pools, string where, RelicAuthoring.Rarity rarity) =>
        new OfferRewardRunEffect(
            new RewardId($"event:{where}:relic"),
            pools.NormalRelicOfRarity($"event '{where}'", (rarity, 100)), 1);

    private static IReadOnlyList<IRunEffectRequest> Stretch(string key, int combats) =>
        ActFourEventPrograms.Stretch(key, combats);

    // "Lose N% of your current Gold", rounded up the way every other percentage in this act is rounded, and
    // read at the moment the branch resolves rather than when the door was written.
    private static IRunEffectRequest LosePercentGold(int percent) =>
        new ComputedResourceRunEffect(StandardRunIds.Gold, RunExpr.Negate(RunExpr.Divide(
            RunExpr.Add(
                RunExpr.Multiply(RunExpr.Resource(StandardRunIds.Gold), RunExpr.Const(percent)),
                RunExpr.Const(99)),
            RunExpr.Const(100))));

    // "Upgrade 1 <category>; if the category is absent, use another eligible card" — the festival's own
    // clause, which is why it is asked as a count and not assumed.
    private static IRunEffectRequest UpgradeOfKind(string tag, string purpose)
    {
        var ofKind = RunSelectors.DeckCards.Upgradable().WithTag(new RunCardTagId(tag));
        return new ConditionalRunEffect(
            RunExpr.GreaterThan(RunExpr.Count(ofKind), RunExpr.Const(0)),
            [new UpgradeCardsRunEffect(ofKind.ChooseByPlayer(1, purpose))],
            [Upgrade(1, purpose)]);
    }

    private static IRunEffectRequest Transform(int count, ConversionPools pools, string purpose) =>
        new TransformCardsRunEffect(
            RunSelectors.DeckCards.ChooseByPlayer(count, purpose), pools.TransformPool());

    private static CombatNodeModel Applies(string statusId, int stacks) =>
        new("applyStatus", "source", CombatAmountSpec.FromConst(stacks), StatusId: statusId);

    // A named Event relic, plus whatever it does the moment it is taken.
    private static IReadOnlyList<IRunEffectRequest> Grant(string id)
    {
        var relic = EventRelics.ActIV.FirstOrDefault(r => r.Id == id)
            ?? throw new ConversionException($"event relic '{id}'", "no Act-IV event relic with this id");
        return [new AddRelicByIdRunEffect(new RelicId(relic.Id)), .. relic.Pickup ?? []];
    }

    // ── the event's own shape ─────────────────────────────────────────────────────────────────────────────

    // The design's availability band, as a share of the act's depth. The Labyrinth's master gives bands and
    // not stages, so the bands are read straight rather than through a stage number that does not exist.
    private enum Band { All, EarlyMid, Mid, MidLate, Late }

    private static int Depth(Band band) => band switch
    {
        Band.All => 0,
        Band.EarlyMid => 0,
        Band.Mid => 40,
        Band.MidLate => 55,
        Band.Late => 75,
        _ => 0,
    };

    private sealed record EventBranch(
        string Id, string Text, string Result, IReadOnlyList<IRunEffectRequest> Effects,
        IReadOnlyList<RunCost>? Costs);

    private static EventBranch Branch(
        string id, string text, string result, IReadOnlyList<IRunEffectRequest> effects,
        IReadOnlyList<RunCost>? costs = null) =>
        new(id, text, result, effects, costs);

    private static BnbEvent Event(
        string id, string name, Band band, string text, params EventBranch[] branches)
    {
        var situations = new List<EventSituation>
        {
            new("start", text, branches
                .Select(b => new EventChoice(b.Id, b.Effects, NextSituationId: $"result:{b.Id}",
                    TextKey: b.Text, Costs: b.Costs))
                .ToList()),
        };
        situations.AddRange(branches.Select(b => new EventSituation($"result:{b.Id}", b.Result,
            [new EventChoice("continue", [], TextKey: "Continue")])));

        return new BnbEvent(id, name, new EventScript("start", situations),
            EarliestDepthPercent: Depth(band));
    }
}
