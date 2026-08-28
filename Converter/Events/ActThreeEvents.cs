using RogueDeck.Core.Combat;
using RogueDeck.Run;
using RogueDeck.Scenario.Authoring;
using BnbContent.Converter.Cards;
using BnbContent.Converter.Relics;
using static BnbContent.Converter.Relics.RelicAuthoring;
using static BnbContent.Converter.Cards.CardAuthoring;

namespace BnbContent.Converter.Events;

// Act III's fifteen doors, from `source-data/design/BnB_Final_Events_Master_PostAudit.md` §"ACT III".
//
// AUTHORED, exactly as Act I's and Act II's are. The Green Docket's doors are not offices; they are places
// on a road where somebody has a right and is willing to discuss it. So what they hand out is different in
// kind from the city's markings and the archives' inscriptions-about-filing: an INSCRIPTION here is a
// courtesy extended to one card and never withdrawn, and what a door writes on the FIGHT is usually a
// demand — a Wergild owed to nobody, which the act's own Make Amends settles like any other.
//
// Three doors ask the traveller to fight in a particular SHAPE and pay only if the shape held. Neither vow
// stops anything; the fight writes down whether the promise was kept and the road reads it afterwards.
//
// Every event carries the design's "Earliest Stage N" as a depth, so the map is not allowed to open the
// deepest rooms on the first step.
public static class ActThreeEvents
{
    public const int Act = 3;

    private const int Stages = 10;

    public static IReadOnlyList<BnbEvent> All(ConversionPools pools, Random rng)
    {
        _ = rng; // the Green Docket's doors are all authored; nothing here is dealt per run.
        return
        [
            AClearStream(pools),
            TheNoticeboundHedge(pools),
            TheWitchAtTheMilestone(pools),
            ThePublicFootpathDispute(pools),
            MoonlitMushrooms(pools),
            ASpidersClause(pools),
            TheAntQueue(pools),
            TheConceptualToll(pools, rng),
            RainBeneathTheRowan(pools),
            TheBuriedWaystone(pools),
            TheTravellingChandler(pools, rng),
            Stargazing(pools),
            TheQuietMeadow(pools),
            TheOmbudsmansWarning(pools),
            TheKindlyProcession(pools),
        ];
    }

    // ── 1 · Earliest Stage 1 ──────────────────────────────────────────────────────────────────────────────

    private static BnbEvent AClearStream(ConversionPools pools) => Event(
        "a_clear_stream", "A Clear Stream", stage: 1,
        "The water runs over pale stones and takes nothing with it that does not want to go. Somebody has "
        + "set a cup on the bank, upside down, for whoever comes next.",
        Branch("wash_away", "Wash away what clings.",
            "One thing goes downstream, and you feel a little better for having let it.",
            [Remove("choose what to wash away"), Heal(5)]),
        Branch("wash_one", "Wash one thing carefully.",
            "The rowan leans over the water. What you washed comes out blessed, and so do you.",
            [.. Inscribe(ActThreeEventObjects.RowanBlessed, "choose what to wash carefully"), Heal(10)]),
        Branch("bottle", "Bottle the water.",
            "It keeps. The first demand you settle in full on the next ordinary road will grant one more "
            + "leave to pass than it would have.",
            [Openings.NextCombat(Applies(ActThreeEventObjects.BottledWaterId))]));

    // ── 2 · Earliest Stage 2 ──────────────────────────────────────────────────────────────────────────────

    private static BnbEvent TheNoticeboundHedge(ConversionPools pools) => Event(
        "the_noticebound_hedge", "The Noticebound Hedge", stage: 2,
        "The hedge is grown through with notices — some nailed, some written straight onto the bark. Every "
        + "one of them says the same thing in a different hand: not this way.",
        Branch("lawful_gap", "Cut a lawful gap.",
            "Thirty-five Gold buys the paperwork, and the hedge opens exactly wide enough for one thing to "
            + "be left behind.",
            [Remove("choose what to leave in the gap")], costs: [Price(35)]),
        Branch("cross_first", "Cross first, explain later.",
            "Ninety Gold's worth of shortcut, and a hedge's demand waiting at the next ordinary fight. "
            + "Nobody will hold it against you afterwards; they will only take it out of you.",
            [Gold(90), Install(ActThreeEventPrograms.HedgeDemandWaits)]),
        Branch("mark_the_path", "Ask the hedge to mark the path.",
            "A knot is tied where the way changes. One of your procedures carries it now.",
            [.. Inscribe(ActThreeEventObjects.WayKnotted, "choose what the hedge will knot")]));

    // ── 3 · Earliest Stage 2 ──────────────────────────────────────────────────────────────────────────────

    private static BnbEvent TheWitchAtTheMilestone(ConversionPools pools) => Event(
        "the_witch_at_the_milestone", "The Witch at the Milestone", stage: 2,
        "She is sitting on the milestone rather than beside it, which is either rude or a legal position. "
        + "She does not ask where you are going. She asks what you would give to get there sooner.",
        Branch("knot", "Ask her for a knot.",
            "She ties it into one of your procedures and improves the knot while she is at it.",
            [
                .. Inscribe(ActThreeEventObjects.WayKnotted, "choose what she will knot"),
                Upgrade(1, "choose what she will improve"),
            ]),
        Branch("bad_memory", "Offer a bad memory.",
            "She takes it out of your file and out of you, and pays for it in old coin.",
            [Remove("choose the memory to give her"), new ChangeMaxHealthRunEffect(-4), Gold(70)]),
        Branch("shortest", "Ask which road is shortest.",
            "It is shorter. Whatever is waiting on it is diminished by the hurry, nobody is paying you for "
            + "the shortcut, and there is something at the end worth having.",
            [Install(ActThreeEventPrograms.ShortestRoadWaits)]));

    // ── 4 · Earliest Stage 3 ──────────────────────────────────────────────────────────────────────────────

    private static BnbEvent ThePublicFootpathDispute(ConversionPools pools) => Event(
        "the_public_footpath_dispute", "The Public Footpath Dispute", stage: 3,
        "Two hedges, one path, and four opinions about which of them is older. Everybody present is holding "
        + "a document, and none of the documents agree.",
        Branch("declare", "Declare a public right.",
            "You are covered — twice over — and everyone on the road afterwards has standing to object. "
            + "There is a purse in it if you get through them.",
            [
                Openings.NextCombat(
                    Applies(ActThree.SafeConductId), Applies(ActThree.SafeConductId),
                    EveryEnemyGainsAClaim()),
                Install(ActThreeEventPrograms.VictoryPurse80),
            ]),
        Branch("older_boundary", "Recognize the older boundary.",
            "You give way, and it costs a little of you and one of your own procedures.",
            [Remove("choose what to concede"), new ApplyRunDamageRunEffect(5)]),
        Branch("mediate", "Mediate the dispute.",
            "Two procedures come out of it better. Your own leave to pass went into the settlement.",
            [
                Upgrade(2, "choose two to improve in the mediation"),
                Openings.NextCombat(TakesALicence()),
            ]));

    // ── 5 · Earliest Stage 4 ──────────────────────────────────────────────────────────────────────────────

    private static BnbEvent MoonlitMushrooms(ConversionPools pools) => Event(
        "moonlit_mushrooms", "Moonlit Mushrooms", stage: 4,
        "The circle is perfect and nobody made it. Standing outside it you can hear a discussion; standing "
        + "inside it you would be part of one.",
        Branch("step_inside", "Step inside the circle.",
            "It costs you something to be a member. You come out wearing a cap that puts every third thing "
            + "you do to a vote.",
            [PercentDamage(8), .. Grant(EventRelics.ActIII, "mootcap")]),
        Branch("offer", "Offer something to the circle.",
            "One procedure goes into the ring. Two others come back improved, and nobody says by whom.",
            [Remove("choose what to offer the circle"), UpgradeRandom(2)]),
        Branch("quorum", "Wait for quorum.",
            "You will be counted at the end of every turn of the next ordinary fight: one real card, or "
            + "three. Get through it without failing the count and the ring will give you a voice.",
            [
                Openings.NextCombat(Applies(ActThreeEventObjects.QuorumVowId)),
                Install(ActThreeEventPrograms.QuorumKept),
                Install(ActThreeEventPrograms.QuorumLapsed),
            ]));

    // ── 6 · Earliest Stage 4 ──────────────────────────────────────────────────────────────────────────────

    private static BnbEvent ASpidersClause(ConversionPools pools) => Event(
        "a_spiders_clause", "A Spider's Clause", stage: 4,
        "The web is between two thorn branches and it is written on. Not marked — written, in a hand that "
        + "runs clockwise and gets smaller towards the middle.",
        Branch("read_exception", "Read the exception.",
            "It costs you something to read that far in. One of your procedures acquires an older right "
            + "than the one that burns it.",
            [
                PercentDamage(6),
                .. Inscribe(ActThreeEventObjects.OldRightInscription, "choose what the exception covers"),
            ]),
        Branch("cut_through", "Cut through the clause.",
            "One procedure goes with it, and something on the next road will remember that you cut.",
            [Remove("choose what the cut costs you"), Openings.NextCombat(Applies("doubt"))]),
        Branch("sign", "Sign beneath the web.",
            "A hundred Gold for a signature nobody read. Somebody on the next road will have standing "
            + "because of it.",
            [Gold(100), Openings.NextCombat(OneEnemyGainsAClaim())]));

    // ── 7 · Earliest Stage 5 ──────────────────────────────────────────────────────────────────────────────

    private static BnbEvent TheAntQueue(ConversionPools pools) => Event(
        "the_ant_queue", "The Ant Queue", stage: 5,
        "The line crosses the road and has been crossing it, by the look of the grass, for longer than the "
        + "road has been here. Nothing in it hurries. Nothing in it stops.",
        Branch("wait", "Wait your turn.",
            "It takes most of the afternoon. Two procedures are properly filed by the end of it, and you "
            + "are rested.",
            [Upgrade(2, "choose two to put in order while you wait"), Heal(10)]),
        Branch("step_over", "Step over the line.",
            "Something bites. There is a rare thing lying where the line was, and coin beside it.",
            [PercentCurrentDamage(10), RareCardReward(pools, "ant_queue"), Gold(60)]),
        Branch("walk_with", "Walk with the proper line.",
            "You are in the procession now, and it has an order: nothing cheaper after something dear, "
            + "within a turn. Win an ordinary fight without stepping out of it and they will mark you.",
            [
                Openings.NextCombat(Applies(ActThreeEventObjects.AntLineVowId)),
                Install(ActThreeEventPrograms.AntLineKept),
                Install(ActThreeEventPrograms.AntLineLapsed),
            ]));

    // ── 8 · Earliest Stage 4 ──────────────────────────────────────────────────────────────────────────────

    private static BnbEvent TheConceptualToll(ConversionPools pools, Random rng) => Market(
        "the_conceptual_toll", "The Conceptual Toll", stage: 4,
        "There is no bridge. There is a toll, and a keeper, and a very old argument about whether the "
        + "absence of a bridge is a reason not to charge for crossing it.",
        pools, rng, entry: 45, cards: 4, relics: 3, removal: true, discount: 15,
        browse: "Pay the conceptual toll.",
        others:
        [
            Branch("dispute", "Dispute the crossing.",
                "Eighty-five Gold's worth of being right, and a crossing's demand waiting at the next "
                + "ordinary fight.",
                [Gold(85), Install(ActThreeEventPrograms.ConceptualDemandWaits)]),
            Branch("use_anyway", "Use the bridge anyway.",
                "Twice covered, unpaid, and something worth having at the end of the next ordinary fight.",
                [
                    Openings.NextCombat(
                        Applies(ActThree.SafeConductId), Applies(ActThree.SafeConductId)),
                    Install(ActThreeEventPrograms.GarnishedReward),
                    Install(ActThreeEventPrograms.ExtraCardReward),
                ]),
        ]);

    // ── 9 · Earliest Stage 3 ──────────────────────────────────────────────────────────────────────────────

    private static BnbEvent RainBeneathTheRowan(ConversionPools pools) => Event(
        "rain_beneath_the_rowan", "Rain Beneath the Rowan", stage: 3,
        "The rain arrives the way rain does on this road: all at once, and with the air of having been "
        + "waiting. The rowan is the only thing for a mile that is not getting wet underneath.",
        Branch("wait", "Wait beneath the branches.",
            "It takes an hour and it is worth an hour.",
            [Heal(30)]),
        Branch("shelter", "Ask the tree for shelter.",
            "It says yes the way trees do. One of your procedures is under the branches now, permanently.",
            [.. Inscribe(ActThreeEventObjects.RowanBlessed, "choose what the rowan shelters"), Heal(10)]),
        Branch("keep_walking", "Keep walking through the rain.",
            "You are wet and shorter of breath, and for the next two ordinary fights you start with more in "
            + "hand than you would have.",
            [
                new ApplyRunDamageRunEffect(6),
                Openings.NextCombat(new CombatNodeModel("drawCards", "source", CombatAmountSpec.FromConst(1))),
                Install(ActThreeEventPrograms.ShelterAgain),
            ]));

    // ── 10 · Earliest Stage 6 ─────────────────────────────────────────────────────────────────────────────

    private static BnbEvent TheBuriedWaystone(ConversionPools pools) => Event(
        "the_buried_waystone", "The Buried Waystone", stage: 6,
        "Most of it is under the turf. What is above says a distance and half a name, and the distance is "
        + "wrong for anywhere that still exists.",
        Branch("clean", "Clean the old inscription.",
            "The stones watched, which is a thing they can be made to do again. One of your procedures "
            + "carries their attention now.",
            [.. Inscribe(ActThreeEventObjects.StoneWitnessed, "choose what the stones will witness")]),
        Branch("follow", "Follow the forgotten name.",
            "It goes somewhere smaller than it should. Nobody pays you for the detour, and there is "
            + "something rare at the end of it.",
            [Install(ActThreeEventPrograms.ForgottenNameWaits)]),
        Branch("bury", "Bury one of your own marks beside it.",
            "It goes under the turf with the rest. It costs you a little of yourself, and a hundred Gold "
            + "comes up where it went down.",
            [Remove("choose the mark to bury"), new ChangeMaxHealthRunEffect(-5), Gold(100)]));

    // ── 11 · Earliest Stage 3 ─────────────────────────────────────────────────────────────────────────────

    private static BnbEvent TheTravellingChandler(ConversionPools pools, Random rng) => Market(
        "the_travelling_chandler", "The Travelling Chandler", stage: 3,
        "The cart smells of tallow and beeswax and something underneath that is neither. He sells light, "
        + "and light on this road is a legal commodity.",
        pools, rng, entry: 0, cards: 3, relics: 2, removal: false, discount: 20,
        browse: "Browse the cart.",
        others:
        [
            Branch("flame", "Buy a traveller's flame.",
                "Fifty Gold for a light that lasts one fight, and lights it well: you begin with more to "
                + "spend and leave to spend it.",
                [
                    // HELD, not gained: the opening bell arrives on a full pool, and a clamped gain is
                    // silently nothing (HeldEnergy). The point waits and lands the moment you run dry.
                    Openings.NextCombat(
                        Applies(HeldEnergy.Id),
                        Applies(ActThree.SafeConductId)),
                ],
                costs: [Price(50)]),
            Branch("trade", "Trade something old for wax.",
                "One procedure goes onto the cart. Thirty-five Gold comes off it.",
                [Remove("choose what to trade for wax"), Gold(35)]),
        ]);

    // ── 12 · Earliest Stage 6 ─────────────────────────────────────────────────────────────────────────────

    private static BnbEvent Stargazing(ConversionPools pools) => Event(
        "stargazing", "Stargazing", stage: 6,
        "Three of them are low and bright and have been argued over. The road star, the root star, and the "
        + "one over the hill that nobody around here will name out loud.",
        Branch("road_star", "Read the Road Star.",
            "It costs a little of you to look that long. The next two ordinary fights open with leave to "
            + "pass.",
            [
                new ChangeMaxHealthRunEffect(-4),
                Openings.NextCombat(Applies(ActThree.SafeConductId)),
                Install(ActThreeEventPrograms.RoadStarAgain),
            ]),
        Branch("root_star", "Read the Root Star.",
            "Two procedures come out of it better, and one of them is kept warm afterwards.",
            [
                Upgrade(2, "choose two the root star improves"),
                .. InscribeAtRandom(ActThreeEventObjects.HearthKept),
            ]),
        Branch("hill_star", "Read the Hill Star.",
            "There is something rare in it, and something under the hill now knows your face.",
            [
                RareCardReward(pools, "stargazing"),
                Openings.NextCombat(BiggestEnemyGainsAClaim()),
            ]));

    // ── 13 · Earliest Stage 1 ─────────────────────────────────────────────────────────────────────────────

    private static BnbEvent TheQuietMeadow(ConversionPools pools) => Event(
        "the_quiet_meadow", "The Quiet Meadow", stage: 1,
        "Nothing here has a right to anything. The grass is long, the light is level, and for the length of "
        + "a field there is nobody to be answerable to.",
        Branch("practice", "Practice where nobody watches.",
            "Two procedures are better for the hour, and nobody has to be told.",
            [Upgrade(2, "choose two to practise")]),
        Branch("leave_behind", "Leave something behind.",
            "It stays in the grass. You go on lighter.",
            [Remove("choose what to leave in the meadow"), Heal(10)]),
        Branch("lie_down", "Lie in the grass.",
            "Nothing happens for a long time, which is the whole of what the meadow has to offer, and it "
            + "is a great deal.",
            [Heal(35)]));

    // ── 14 · Earliest Stage 7 · Rare ──────────────────────────────────────────────────────────────────────

    private static BnbEvent TheOmbudsmansWarning(ConversionPools pools) => Event(
        "the_ombudsmans_warning", "The Ombudsman's Warning", stage: 7,
        "A leaf the size of a hand, pressed flat and written on both sides. It is addressed to you, it is "
        + "dated tomorrow, and it is signed with a paw print and a seal.",
        Branch("prepare", "Prepare a response.",
            "Two procedures are put in order for it, and the next hard room finds you already covered.",
            [
                Upgrade(2, "choose two to put in order"),
                Install(ActThreeEventPrograms.PreparedResponse),
            ]),
        Branch("complain", "Submit your own complaint.",
            "It is filed. Everybody on the next ordinary road is answering paperwork instead of you, and "
            + "the largest of them has standing to be annoyed about it.",
            [
                Openings.NextCombat(
                    EveryEnemyGains("paperwork"), EveryEnemyGains("doubt"), BiggestEnemyGainsAClaim()),
                Install(ActThreeEventPrograms.VictoryPurse60),
            ]),
        Branch("keep", "Keep the leaf.",
            "It costs you something to carry a complaint about yourself. The first party to lay a hand on "
            + "you will be answering it.",
            [new ChangeMaxHealthRunEffect(-6), .. Grant(EventRelics.ActIII, "complaint_leaf")]));

    // ── 15 · Earliest Stage 8 · Rare ──────────────────────────────────────────────────────────────────────

    private static BnbEvent TheKindlyProcession(ConversionPools pools) => Event(
        "the_kindly_procession", "The Kindly Procession", stage: 8,
        "They are going the other way, unhurried, carrying nothing you can name. Every one of them looks at "
        + "you as they pass, and every one of them is kind about it.",
        Branch("bow", "Bow and let them pass.",
            "It takes a moment and it is the right moment. You are the better for it in a way that lasts.",
            [Heal(25), new ChangeMaxHealthRunEffect(3)]),
        Branch("three_steps", "Walk three steps with them.",
            "Three steps is guest-right. It costs you seven of yourself, and the road looks after its "
            + "guests from here on.",
            [
                new ChangeMaxHealthRunEffect(-7),
                .. Grant(EventRelics.ActIII, "guest_right_brooch"),
                Openings.NextCombat(
                    Applies(ActThree.SafeConductId), Applies(ActThree.SafeConductId)),
            ]),
        Branch("give", "Give the procession something of yours.",
            "They take it without breaking step. A hundred Gold is in your hand and another of your "
            + "procedures is better than it was.",
            [Remove("choose what to give them"), Gold(100), Upgrade(1, "choose what comes back improved")]),
        Branch("farther", "Follow them farther than three steps.",
            "You come back whole and twelve short, wearing their brooch and carrying something rare. "
            + "Everything on the next road has heard where you have been.",
            [
                new ChangeMaxHealthRunEffect(-12),
                .. Grant(EventRelics.ActIII, "guest_right_brooch"),
                RareCardReward(pools, "kindly_procession"),
                Heal(100),
                Openings.NextCombat(EveryEnemyGainsAClaim()),
            ]));

    // ── the shared shapes ─────────────────────────────────────────────────────────────────────────────────

    private static IRunSelector<RunCardInstance> Choose(string purpose) =>
        RunSelectors.DeckCards.ChooseByPlayer(1, purpose);

    private static IRunEffectRequest Gold(int amount) =>
        new ChangeResourceRunEffect(StandardRunIds.Gold, amount);

    private static RunCost Price(int gold) =>
        new(RunExpr.HasResource(StandardRunIds.Gold, gold), [Gold(-gold)]);

    // "Heal N% of Max HP", rounded up, exactly as the city's tea break does it.
    private static IRunEffectRequest Heal(int percent) =>
        new ComputedHealRunEffect(RunExpr.Divide(
            RunExpr.Add(RunExpr.Multiply(RunExpr.MaxHealth, RunExpr.Const(percent)), RunExpr.Const(99)),
            RunExpr.Const(100)));

    // "Lose N% of Max HP", the same rounding, taken out of you rather than put back.
    private static IRunEffectRequest PercentDamage(int percent) =>
        new ComputedDamageRunEffect(RunExpr.Divide(
            RunExpr.Add(RunExpr.Multiply(RunExpr.MaxHealth, RunExpr.Const(percent)), RunExpr.Const(99)),
            RunExpr.Const(100)));

    private static IRunEffectRequest PercentCurrentDamage(int percent) =>
        new ComputedDamageRunEffect(RunExpr.Divide(
            RunExpr.Add(RunExpr.Multiply(RunExpr.CurrentHealth, RunExpr.Const(percent)), RunExpr.Const(99)),
            RunExpr.Const(100)));

    private static IRunEffectRequest Upgrade(int count, string purpose) =>
        new UpgradeCardsRunEffect(RunSelectors.DeckCards.Upgradable().ChooseByPlayer(count, purpose));

    private static IRunEffectRequest UpgradeRandom(int count) =>
        new UpgradeCardsRunEffect(RunSelectors.DeckCards.Upgradable().Random(count));

    private static IRunEffectRequest Remove(string purpose) =>
        new RemoveCardsRunEffect(Choose(purpose));

    private static IRunEffectRequest RareCardReward(ConversionPools pools, string where) =>
        new OfferRewardRunEffect(new RewardId($"event:{where}:rare"), pools.CardRewardSource("rare"), 1);

    private static IRunEffectRequest Install(string program) =>
        new InstallProgramByIdRunEffect(new RunProgramSourceId(program));

    // A licence taken back rather than granted. A stack removed from a status nobody has is not removed,
    // which is the design's "minimum 0" without anything having to say so.
    private static CombatNodeModel TakesALicence() =>
        new("modifyStatusStacks", "source", CombatAmountSpec.FromConst(-1),
            StatusId: ActThree.SafeConductId);

    private static CombatNodeModel Applies(string statusId) =>
        new("applyStatus", "source", CombatAmountSpec.FromConst(1), StatusId: statusId);

    private static CombatNodeModel EveryEnemyGains(string statusId) =>
        CombatNodeModel.ForEach("allEnemies",
            new CombatNodeModel("applyStatus", "iterationTarget",
                CombatAmountSpec.FromConst(1), StatusId: statusId));

    // Standing GRANTED, so the announcement goes up with it: a party that starts a fight with a Claim was
    // given one, and everything in the act that listens for a grant is entitled to hear this one.
    private static CombatNodeModel EveryEnemyGainsAClaim() =>
        CombatNodeModel.ForEach("allEnemies", CombatNodeModel.Sequence(
        [
            new CombatNodeModel("applyStatus", "iterationTarget",
                CombatAmountSpec.FromConst(1), StatusId: ActThree.ClaimId),
            new CombatNodeModel("applyStatus", "iterationTarget",
                CombatAmountSpec.FromConst(1), StatusId: ActThree.ClaimCreatedId),
        ]));

    private static CombatNodeModel OneEnemyGainsAClaim() =>
        new("randomTargets", "allEnemies", CombatAmountSpec.FromConst(1),
            Children:
            [
                CombatNodeModel.Sequence(
                [
                    new CombatNodeModel("applyStatus", "iterationTarget",
                        CombatAmountSpec.FromConst(1), StatusId: ActThree.ClaimId),
                    new CombatNodeModel("applyStatus", "iterationTarget",
                        CombatAmountSpec.FromConst(1), StatusId: ActThree.ClaimCreatedId),
                ]),
            ]);

    private static CombatNodeModel BiggestEnemyGainsAClaim() =>
        CombatNodeModel.Sequence(
        [
            new CombatNodeModel("applyStatus", "highestHealthEnemy",
                CombatAmountSpec.FromConst(1), StatusId: ActThree.ClaimId),
            new CombatNodeModel("applyStatus", "highestHealthEnemy",
                CombatAmountSpec.FromConst(1), StatusId: ActThree.ClaimCreatedId),
        ]);

    // An INSCRIPTION: written on one card and never cleared, so its rule has to be in every later fight.
    private static IReadOnlyList<IRunEffectRequest> Inscribe(string inscription, string purpose) =>
    [
        new TagCardsRunEffect(Choose(purpose), new RunCardTagId(inscription), true),
        Install(ActThreeEventPrograms.Inscriptions),
    ];

    private static IReadOnlyList<IRunEffectRequest> InscribeAtRandom(string inscription) =>
    [
        new TagCardsRunEffect(
            RunSelectors.DeckCards.Random(1), new RunCardTagId(inscription), true),
        Install(ActThreeEventPrograms.Inscriptions),
    ];

    // A named Event relic, plus whatever it does the moment it is taken.
    private static IReadOnlyList<IRunEffectRequest> Grant(IReadOnlyList<BnbRelic> pool, string id)
    {
        var relic = pool.FirstOrDefault(r => r.Id == id)
            ?? throw new ConversionException($"event relic '{id}'", "no relic with this id is authored");
        return [new AddRelicByIdRunEffect(new RelicId(relic.Id)), .. relic.Pickup ?? []];
    }

    // ── the event's own shape ─────────────────────────────────────────────────────────────────────────────

    private sealed record EventBranch(
        string Id, string Text, string Result, IReadOnlyList<IRunEffectRequest> Effects,
        IReadOnlyList<RunCost>? Costs);

    private static EventBranch Branch(
        string id, string text, string result, IReadOnlyList<IRunEffectRequest> effects,
        IReadOnlyList<RunCost>? costs = null) =>
        new(id, text, result, effects, costs);

    private static BnbEvent Event(
        string id, string name, int stage, string text, params EventBranch[] branches)
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
            EarliestDepthPercent: Depth(stage));
    }

    // ADAPTATION: an event cannot open a shop NODE, so a door that opens a market is a counter built INSIDE
    // the event — an authored stock, each item bought at most once, at the act's prices less the discount
    // the design names. Same trade the city's Licensed Vendor made: no reroll, and the same shelf each run.
    private static BnbEvent Market(
        string id, string name, int stage, string text, ConversionPools pools, Random rng,
        int entry, int cards, int relics, bool removal, int discount, string browse,
        IReadOnlyList<EventBranch> others)
    {
        int Less(int price) => price * (100 - discount) / 100;

        var cardPrices = new Dictionary<string, int> { ["common"] = 55, ["uncommon"] = 85, ["rare"] = 130 };
        var relicPrices = new Dictionary<string, int> { ["common"] = 130, ["uncommon"] = 190, ["rare"] = 260 };

        var stock = new List<EventChoice>();
        foreach (var (card, index) in pools.RewardCards.OrderBy(_ => rng.Next()).Take(cards)
            .Select((c, i) => (c, i)))
        {
            var price = Less(cardPrices.GetValueOrDefault(card.Rarity, 85));
            stock.Add(Stall(id, $"card-{index}", $"{card.Name} — {price} Gold", price,
                [new AddCardToDeckRunEffect(new CardDefinitionId(card.Id))]));
        }
        foreach (var (relic, index) in pools.Relics.OrderBy(_ => rng.Next()).Take(relics)
            .Select((r, i) => (r, i)))
        {
            var price = Less(relicPrices.GetValueOrDefault(relic.Source.Rarity ?? "common", 190));
            stock.Add(Stall(id, $"relic-{index}", $"{relic.Source.Name} — {price} Gold", price,
                ConversionPools.RelicOffer(relic).Grant));
        }
        if (removal)
        {
            var price = Less(75);
            stock.Add(Stall(id, "removal", $"Have something struck from your file — {price} Gold", price,
                [new RemoveCardsRunEffect(Choose("choose what to have struck from your file"))]));
        }
        stock.Add(new EventChoice("leave", [], TextKey: "Take your leave."));

        var opening = new List<EventChoice>
        {
            new("browse", [], NextSituationId: "stock", TextKey: browse,
                Costs: entry > 0 ? [Price(entry)] : null),
        };
        opening.AddRange(others.Select(b => new EventChoice(b.Id, b.Effects,
            NextSituationId: $"result:{b.Id}", TextKey: b.Text, Costs: b.Costs)));

        var situations = new List<EventSituation>
        {
            new("start", text, opening),
            new("stock", "What is laid out is laid out; there will be no more of it.", stock),
        };
        situations.AddRange(others.Select(b => new EventSituation($"result:{b.Id}", b.Result,
            [new EventChoice("continue", [], TextKey: "Continue")])));

        return new BnbEvent(id, name, new EventScript("start", situations),
            EarliestDepthPercent: Depth(stage));
    }

    private static EventChoice Stall(
        string eventId, string id, string text, int price, IReadOnlyList<IRunEffectRequest> payload)
    {
        var sold = new RunFlagId($"{eventId}.{id}");
        return new EventChoice(id,
            [.. payload, new SetFlagRunEffect(sold)],
            NextSituationId: "stock",
            Requirement: RunExpr.Not(RunExpr.Flag(sold)),
            TextKey: text,
            Costs: [Price(price)]);
    }

    // The design's "Earliest Stage N" as a share of the act's own depth.
    private static int Depth(int stage) => (stage - 1) * 100 / (Stages - 1);
}
