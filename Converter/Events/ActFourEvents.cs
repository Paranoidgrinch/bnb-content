using RogueDeck.Core.Combat;
using RogueDeck.Run;
using RogueDeck.Sandbox.Composition;
using RogueDeck.Scenario.Authoring;
using BnbContent.Converter.Relics;

namespace BnbContent.Converter.Events;

// Act IV's doors, from `source-data/design/BnB_Final_Events_Master_PostAudit.md` §"ACT IV" — the first ten of
// twenty (events 11–20 arrive with IV-23).
//
// The Licensing Labyrinth's doors are OFFICES, and an office does not bargain: it measures you, files the
// measurement, and the filing follows you down the corridor. That is why so many of these promise a STRETCH
// of road rather than a single fight — "the next three combats start Inscribed 1" — and why nearly every one
// of them is written in the act's own five words. The two exceptions are the doors that offer a fight, and
// what a door can do about a fight is set one on the road ahead (see ActFourEventPrograms).
//
// Five of the ten hand over an Event relic. Those five are built with this step rather than with the other
// four at IV-23, because a branch that hands over nothing is a branch nobody can test.
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
