using RogueDeck.Core.Combat;
using RogueDeck.Run;
using RogueDeck.Scenario.Authoring;
using BnbContent.Converter.Cards;
using BnbContent.Converter.Relics;
using static BnbContent.Converter.Cards.CardAuthoring;
using static BnbContent.Converter.Relics.RelicAuthoring;

namespace BnbContent.Converter.Events;

// Act II's fifteen events, from `source-data/design/BnB_Final_Events_Master_PostAudit.md` §"ACT II".
//
// AUTHORED, exactly as Act I's are: the ported v2 archives events wore these names and did other things, and
// this file replaces them. The vocabulary is the one B-2a/b/c built — ActTwoEventObjects for what a fight has
// to honour, ActTwoEventPrograms for what the run owes afterwards, EventRelics.ActII for the five relics, and
// RunState.RemovedCards for the two doors that give a card back.
//
// The archives speak differently from the city. The city's doors wrote a marking on a card and the next fight
// filed it somewhere; the archives write on the card ITSELF — a misfiling, a redaction, or an INSCRIPTION that
// never comes off — and half of what they promise is not about the next fight at all but about a fight further
// down, whichever one turns out to be an ordinary one.
//
// Every event carries the design's "Earliest Stage N" as a depth: the map is not allowed to open the deepest
// rooms on the first step (MapGenerationSpec.NodeRefMinimumDepthPercent).
public static class ActTwoEvents
{
    public const int Act = 2;

    // The design's stage ladder for this act — what "Earliest Stage 8" is eight of.
    private const int Stages = 10;

    public static IReadOnlyList<BnbEvent> All(ConversionPools pools, Random rng)
    {
        _ = rng; // the archives' doors are all authored; nothing here is dealt per run.
        return
        [
            MisfiledProphecy(pools),
            SelfCorrectingIndex(),
            LockedReadingRoom(pools),
            PerpetualBorrower(pools),
            ReciprocalShelf(pools),
            MarginNotes(),
            UnclaimedReservation(pools),
            InfiniteReturnSlot(),
            RedactedPortrait(),
            LostHourBottle(),
            NecrologyWindow(),
            AlmostHelpfulClerkReassigned(),
            LastQuietTable(pools),
            InwardSeal(),
            LibrarianAtTheEndOfTheAisle(pools),
        ];
    }

    // ── 1 · Earliest Stage 2 ──────────────────────────────────────────────────────────────────────────────

    private static BnbEvent MisfiledProphecy(ConversionPools pools) => Event(
        "misfiled_prophecy", "Misfiled Prophecy", stage: 2,
        "A prophecy about you has been filed under a classification that does not exist. It is accurate, "
        + "which is the part nobody can explain, and it is in the wrong drawer, which is the part everyone can.",
        Branch("correct", "Correct the filing code.",
            "The prophecy is refiled as something else entirely. In exchange the archive takes one of your own "
            + "procedures and files it just as badly.",
            [
                new TransformCardsRunEffect(
                    Choose("choose a card to be refiled as something else"), pools.TransformPool()),
                .. MarkForNextFight("choose the card the archive will misfile", ActTwo.MisfiledMark),
            ]),
        Branch("accept", "Accept the prophecy as written.",
            "The wrong code becomes the right one. One of your procedures is authorized to revise itself, and "
            + "an unfinished citation is filed behind you.",
            [
                .. Inscribe(ActTwoEventObjects.AuthorizedRevision,
                    "choose the card the revision is authorized for"),
                Openings.NextCombat(
                    AddCard(ActTwoEventObjects.UnfinishedCitation.Id, CardZone.DiscardPile)),
            ]));

    // ── 2 · Earliest Stage 6 ──────────────────────────────────────────────────────────────────────────────

    private static BnbEvent SelfCorrectingIndex() => Event(
        "self_correcting_index", "The Self-Correcting Index", stage: 6,
        "An index corrects itself while you watch. Two of its corrections are improvements. One of them is a "
        + "black rectangle where a word used to be.",
        Branch("allow", "Allow the correction.",
            "Two procedures come back better. One of them comes back with half of itself missing.",
            [
                Upgrade(2, "choose two cards for the index to correct"),
                .. MarkChosen(Improved("choose which correction the index redacted"), ActTwo.RedactedMark),
            ]),
        Branch("correct_yourself", "Correct the index yourself.",
            "One entry leaves your file for good. The index, offended, misfiles two others out of spite.",
            [
                Remove("choose the entry to strike from the index"),
                .. MarkEach(2, "choose up to two cards the index will misfile", ActTwo.MisfiledMark),
            ]));

    // ── 3 · Earliest Stage 4 ──────────────────────────────────────────────────────────────────────────────

    private static BnbEvent LockedReadingRoom(ConversionPools pools) => Event(
        "locked_reading_room", "The Locked Reading Room", stage: 4,
        "A reading room stands locked behind glass. Inside, a single volume is open at a page you can almost "
        + "read. A supervisor's chair faces it, empty, and recently warm.",
        Branch("supervised", "Read under supervision.",
            "You are allowed the rare volume. Somebody stands behind you the whole time, and every fourth "
            + "thing you write down is taken away and blacked out.",
            [
                RareCardReward(pools, "locked_reading_room"),
                NextFightRule(ActTwoEventObjects.FourthCard),
            ]),
        Branch("copy", "Copy a single illuminated passage. (40 Gold)",
            "The gold buys an hour and a good pen. The initial you copy is worth more than the passage it "
            + "opens.",
            [.. Inscribe(ActTwoEventObjects.IlluminatedInitial, "choose the card to be illuminated")],
            costs: [Price(40)]),
        Branch("wait", "Wait outside in silence.",
            "Nothing is read. The quiet does what quiet does, and a fifth of what the archives have taken from "
            + "you comes back.",
            [Heal(20)]));

    // ── 4 · Earliest Stage 7 ──────────────────────────────────────────────────────────────────────────────

    private static BnbEvent PerpetualBorrower(ConversionPools pools) => Event(
        "perpetual_borrower", "The Perpetual Borrower", stage: 7,
        "Someone has been borrowing from this archive for longer than the archive has existed. They are "
        + "unfailingly polite, they return everything, and everything they return is better than it was.",
        Branch("lend", "Lend one of your own volumes.",
            "The borrower takes it with both hands. You will not have it for the next piece of work — but if "
            + "that work goes well, what comes back is better than what went out.",
            [
                new TagCardsRunEffect(
                    Choose("choose a volume to lend the borrower"),
                    new RunCardTagId(ActTwoEventObjects.BorrowersKeeping), true),
                ArchiveOpening(),
                Install(ActTwoEventPrograms.LentVolumeReturns),
            ]),
        Branch("notes", "Accept the borrower's old notes.",
            "The notes are somebody else's, and good. The claim slip that comes stapled to them is yours now.",
            [
                UncommonChoice(pools, "perpetual_borrower"),
                Openings.NextCombat(AddCard(ActTwoEventObjects.BorrowersClaim.Id, CardZone.DrawPile)),
            ]),
        Branch("settle", "Settle the account. (60 Gold)",
            "The account is closed, courteously. You are given tea, an hour's rest, and one correction you "
            + "did not ask for.",
            [Heal(15), Upgrade(1, "choose a card the borrower corrects")],
            costs: [Price(60)]),
        Branch("pocket", "Pocket the borrower's library card.",
            "It is warm. It is also, in some sense nobody wants to define, still in use — and the part of you "
            + "that is now lending it out is not getting it back.",
            [
                .. Grant(EventRelics.ActII, "unreturned_library_card"),
                new ChangeMaxHealthRunEffect(-6),
                Openings.NextCombat(AddCard(ActTwoEventObjects.BorrowersClaim.Id, CardZone.DrawPile)),
            ]));

    // ── 5 · Earliest Stage 2 ──────────────────────────────────────────────────────────────────────────────

    private static BnbEvent ReciprocalShelf(ConversionPools pools) => Event(
        "reciprocal_shelf", "The Reciprocal Shelf", stage: 2,
        "A shelf that gives exactly as much as it is given, and classifies exactly as well as it is argued "
        + "with. A loose label hangs from one end, printed on both sides.",
        Branch("submit", "Submit the unwanted entry.",
            "The shelf takes what you no longer want, returns something you might, and pays fifty Gold for "
            + "the inconvenience.",
            [
                new TransformCardsRunEffect(
                    Choose("choose an entry to submit to the shelf"), pools.TransformPool()),
                Gold(50),
            ]),
        Branch("argue", "Argue with the classification.",
            "You win the argument. The shelf reclassifies a different card of yours out of pure spite, and "
            + "blacks out half of it on the way.",
            [
                CardReward(pools, "reciprocal_shelf"),
                .. MarkForNextFight(
                    "choose the card the shelf reclassifies", ActTwo.MisfiledMark, ActTwo.RedactedMark),
            ]),
        Branch("label", "Take the loose shelf label.",
            "Reversible, as promised. So is the shelf's memory of where your things belong — for the next two "
            + "pieces of work, something of yours starts in the wrong place.",
            [
                .. Grant(EventRelics.ActII, "reversible_shelf_label"),
                new TagCardsRunEffect(
                    RunSelectors.DeckCards.Random(1), new RunCardTagId(ActTwo.MisfiledMark), true),
                ArchiveOpening(),
                Install(ActTwoEventPrograms.ShelfLabelAgain),
            ]));

    // ── 6 · Earliest Stage 3 ──────────────────────────────────────────────────────────────────────────────

    private static BnbEvent MarginNotes() => Event(
        "margin_notes", "The Margin Notes", stage: 3,
        "Two hands have been arguing in the margins of this volume for four hundred years. Both are right. "
        + "Neither has noticed the other is dead.",
        Branch("both", "Follow both arguments.",
            "You copy the pair of them onto two of your own procedures. From now on, playing either fetches "
            + "the other.",
            [
                new ForEachCardRunEffect(
                    RunSelectors.DeckCards.ChooseByPlayer(2, "choose two cards to bind into a pair"),
                    [RunEffectTemplates.TagThisCard(new RunCardTagId(ActTwoEventObjects.ConcordantPair))]),
                Install(ActTwoEventPrograms.Inscriptions),
            ]),
        Branch("reply", "Add an illuminated reply.",
            "You settle the argument in gold leaf. Whichever of your procedures carries the reply opens "
            + "every fight worth a card and a little cover.",
            [.. Inscribe(ActTwoEventObjects.IlluminatedInitial, "choose the card to carry the reply")]),
        Branch("scrape", "Scrape the margin clean.",
            "The vellum comes up beautifully. One of your own procedures is improved by the practice, and a "
            + "leaf of the scraped ink follows you into the next room.",
            [
                Upgrade(1, "choose a card the practice improves"),
                Openings.NextCombat(
                    AddCard(ActTwoEventObjects.RedactedLeaf.Id, CardZone.DrawPile),
                    Applies(ActTwoEventObjects.RedactedLeafRule.Id)),
            ]));

    // ── 7 · Earliest Stage 7 ──────────────────────────────────────────────────────────────────────────────

    private static BnbEvent UnclaimedReservation(ConversionPools pools) => Event(
        "unclaimed_reservation", "Unclaimed Reservation", stage: 7,
        "A volume has been reserved for a reader who never came. The reservation is still valid. The register "
        + "beside it has a blank line, and a pen.",
        Branch("claim_volume", "Claim the reserved volume.",
            "The volume is yours, and it stays reserved a while longer — it is not on the table for the "
            + "opening of the next piece of work, and when it arrives it arrives free.",
            [
                UncommonChoice(pools, "unclaimed_reservation", ActTwoEventObjects.Reservation),
                ArchiveOpening(),
                Install(ActTwoEventPrograms.MarkingsExpire),
            ]),
        Branch("empty_seat", "Claim the empty seat.",
            "You sit in the reader's chair for as long as nobody comes. Nobody comes.",
            [Heal(25)]),
        Branch("register", "Enter another name in the register.",
            "Seventy Gold for a signature that is not yours. One of your own cards is now registered to "
            + "somebody else, and cannot be filed until something else has been.",
            [Gold(70), NextFightRule(ActTwoEventObjects.RegisteredElsewhere)]));

    // ── 8 · Earliest Stage 7 ──────────────────────────────────────────────────────────────────────────────

    private static BnbEvent InfiniteReturnSlot() => Event(
        "infinite_return_slot", "The Infinite Return Slot", stage: 7,
        "A brass slot in the wall, worn smooth. Everything the archives have ever lost went in here, and the "
        + "archives have never once admitted that anything comes back out.",
        Branch("return", "Return a bad idea.",
            "It goes in without a sound. Forty Gold comes back, which is more than the idea was worth, and "
            + "the slot remembers it — the archives never truly lose anything.",
            [Remove("choose a bad idea to return"), Gold(40)]),
        Branch("reach", "Reach for a lost page.",
            "Your arm goes in further than the wall is thick. What comes out is something you gave up on, "
            + "exactly as you gave it up — along with somebody else's claim slip.",
            [
                new RestoreRemovedCardRunEffect(Purpose: "choose a card to reach back for"),
                Openings.NextCombat(AddCard(ActTwoEventObjects.BorrowersClaim.Id, CardZone.DrawPile)),
            ]));

    // ── 9 · Earliest Stage 5 ──────────────────────────────────────────────────────────────────────────────

    private static BnbEvent RedactedPortrait() => Event(
        "redacted_portrait", "The Redacted Portrait", stage: 5,
        "A portrait of an archivist whose face has been officially removed. The frame, the hands and the "
        + "signature are immaculate. Somebody went to the trouble of blacking out only the face.",
        Branch("restore", "Restore the missing face. (100 Gold)",
            "The restorer takes the money and returns a cameo with nothing on it. It is the face. It is also, "
            + "from now on, whichever of your cards the archive would rather not see.",
            [.. Grant(EventRelics.ActII, "blank_cameo")],
            costs: [Price(100)]),
        Branch("absent_name", "Take the absent name.",
            "You do not learn the name. You learn that it HAS one — and you write that on one of your own "
            + "cards, which will not be written over so easily again.",
            [.. Inscribe(ActTwoEventObjects.TrueName, "choose the card that learns its own name")]),
        Branch("leave", "Leave the portrait untouched.",
            "Some faces were removed for a reason. Walking away is worth a little of what the archives have "
            + "cost you.",
            [Heal(15)]));

    // ── 10 · Earliest Stage 8 ─────────────────────────────────────────────────────────────────────────────

    private static BnbEvent LostHourBottle() => Event(
        "lost_hour_bottle", "The Lost-Hour Bottle", stage: 8,
        "A stoppered bottle labelled with a date, a place, and one hour that nobody accounted for. It is warm "
        + "and it is faintly ticking.",
        Branch("drink", "Drink the lost hour.",
            "Two rounds of somebody else's afternoon. It is glorious. The third round is when the hour is "
            + "noticed missing, and the archives take it back out of yours.",
            [NextFightRule(ActTwoEventObjects.LostHour)]),
        Branch("bind", "Bind the hour into a card.",
            "The hour goes into the paper. Whatever you write on it is never discarded, and the second turn "
            + "you are still holding it, it is worth more.",
            [.. Inscribe(ActTwoEventObjects.LateBound, "choose the card to bind the hour into")]));

    // ── 11 · Earliest Stage 9 ─────────────────────────────────────────────────────────────────────────────

    private static BnbEvent NecrologyWindow() => Event(
        "necrology_window", "The Necrology Window", stage: 9,
        "A window onto the department that files the dead. Two ledgers lie open: one of lives that stopped "
        + "mid-sentence, one of accounts nobody ever closed.",
        Branch("borrow", "Borrow an unfinished life.",
            "It fits badly and it heals a great deal. The clerk notes the loan; somewhere ahead of you, "
            + "something now has an hour of it too — and the department pays well for the paperwork.",
            [Heal(35), Install(ActTwoEventPrograms.UnfinishedLifeWaits)],
            requirement: null),
        Branch("close", "Close an abandoned account.",
            "It costs eight of your own to close somebody else's. One entry leaves your file entirely; "
            + "another is finished properly, at last.",
            [
                new ApplyRunDamageRunEffect(8),
                Remove("choose the entry to close"),
                Upgrade(1, "choose the entry to finish properly"),
            ],
            // "Unavailable if lethal": the door is not offered when eight would end the run.
            requirement: RunExpr.GreaterThan(RunExpr.CurrentHealth, RunExpr.Const(8))));

    // ── 12 · Earliest Stage 1 ─────────────────────────────────────────────────────────────────────────────

    private static BnbEvent AlmostHelpfulClerkReassigned() => Event(
        "almost_helpful_clerk_reassigned", "The Almost-Helpful Clerk, Reassigned", stage: 1,
        "The clerk from the city offices is here, three floors below where anyone can find them, and very "
        + "pleased to see you. They have been reassigned. Nobody has told them what to.",
        Branch("amendment", "Accept the whispered amendment.",
            "They tell you the correct wording with their hand over their mouth. Half of it is blacked out "
            + "before you get it down — file it anyway and the correction sticks for good.",
            [
                new ForEachCardRunEffect(Choose("choose the card to be amended"),
                [
                    RunEffectTemplates.TagThisCard(
                        new RunCardTagId(ActTwoEventObjects.WhisperedAmendment)),
                    RunEffectTemplates.TagThisCard(new RunCardTagId(ActTwo.RedactedMark)),
                ]),
                Openings.NextCombat(
                    Applies(ActTwoEventObjects.ArchiveMarkings),
                    Applies(ActTwo.ArchiveRegulationsId),
                    Applies(ActTwoEventObjects.AmendmentWatch)),
                Install(ActTwoEventPrograms.AmendmentUpgrade),
                Install(ActTwoEventPrograms.AmendmentLapsed),
            ]),
        Branch("pass", "Accept the temporary reader's pass.",
            "It is laminated, it is expired, and it works. Thirty-five Gold falls out of the sleeve, which "
            + "the clerk pretends not to notice.",
            [NextFightRule(ActTwoEventObjects.ReadersPass), Gold(35)]),
        Branch("ask", "Ask how the Clerk has been.",
            "Badly, thank you for asking. Nobody has, in some time. It costs them nothing to say so and it "
            + "does you a surprising amount of good.",
            [Heal(20)]));

    // ── 13 · Earliest Stage 4 ─────────────────────────────────────────────────────────────────────────────

    private static BnbEvent LastQuietTable(ConversionPools pools) => Event(
        "last_quiet_table", "The Last Quiet Table", stage: 4,
        "One table in the whole archive where nothing is happening. A vow is carved into it, a forbidden "
        + "volume is chained to it, and the chair is still warm from whoever chose neither.",
        Branch("vow", "Take the Vow of Silent Scholarship.",
            "Three filings a turn, no more, until the next piece of work is done. Nothing enforces it. That "
            + "is rather the point.",
            [
                NextFightRule(ActTwoEventObjects.Vow),
                Install(ActTwoEventPrograms.VowKept),
                Install(ActTwoEventPrograms.VowLapsed),
            ]),
        Branch("forbidden", "Read the forbidden volume.",
            "It is worth it. It is also watched — you carry a leaf of its ink into the next room, and every "
            + "fourth thing you file there is read over your shoulder.",
            [
                RareCardReward(pools, "last_quiet_table"),
                Openings.NextCombat(
                    AddCard(ActTwoEventObjects.RedactedLeaf.Id, CardZone.Hand),
                    Applies(ActTwoEventObjects.RedactedLeafRule.Id),
                    Applies(ActTwoEventObjects.FourthCard)),
            ]),
        Branch("rest", "Rest without reading.",
            "You sit at the quiet table and read nothing at all. It is the first quarter-hour of the run "
            + "that belongs to you.",
            [Heal(25)]));

    // ── 14 · Earliest Stage 7 ─────────────────────────────────────────────────────────────────────────────

    private static BnbEvent InwardSeal() => Event(
        "inward_seal", "The Inward Seal", stage: 7,
        "A wax seal pressed into the wall itself, facing inward. Whatever it was sealing, it was sealing it "
        + "away from the archive, not from you.",
        Branch("outward", "Break the seal outward.",
            "The wax comes away in one piece and inverts in your hand. Two of your cards are misfiled and "
            + "blacked out in the confusion; the sealstone is worth both of them.",
            [
                .. Grant(EventRelics.ActII, "inverted_sealstone"),
                new ForEachCardRunEffect(
                    RunSelectors.DeckCards.ChooseByPlayer(2, "choose two cards the breaking seal catches"),
                    [
                        RunEffectTemplates.TagThisCard(new RunCardTagId(ActTwo.MisfiledMark)),
                        RunEffectTemplates.TagThisCard(new RunCardTagId(ActTwo.RedactedMark)),
                    ]),
                ArchiveOpening(),
                Install(ActTwoEventPrograms.MarkingsExpire),
            ]),
        Branch("inward", "Turn the seal inward.",
            "You press it back the way it was facing. Two procedures are improved by the discipline; one "
            + "comes out half-erased and the other in the wrong drawer.",
            [
                Upgrade(2, "choose two cards the seal improves"),
                new TagCardsRunEffect(
                    Improved("choose which of them the seal blacked out"),
                    new RunCardTagId(ActTwo.RedactedMark), true),
                new TagCardsRunEffect(
                    Improved("choose which of them the seal misfiled"),
                    new RunCardTagId(ActTwo.MisfiledMark), true),
                ArchiveOpening(),
                Install(ActTwoEventPrograms.MarkingsExpire),
            ]),
        Branch("skin", "Press the seal into your skin.",
            "It does not hurt and it does not come off. You are, from now on, marginally more of a document — "
            + "and the archives have already started filing you.",
            [
                new ChangeMaxHealthRunEffect(8),
                Openings.NextCombat(
                    new CombatNodeModel("applyStatus", You,
                        CombatAmountSpec.FromConst(2), StatusId: Keywords.Paperwork),
                    new CombatNodeModel("applyStatus", You,
                        CombatAmountSpec.FromConst(1), StatusId: Keywords.Doubt)),
            ]));

    // ── 15 · Earliest Stage 8 ─────────────────────────────────────────────────────────────────────────────

    private static BnbEvent LibrarianAtTheEndOfTheAisle(ConversionPools pools) => Event(
        "librarian_at_the_end_of_the_aisle", "The Librarian at the End of the Aisle", stage: 8,
        "The aisle ends in a desk that was not there a moment ago, and a librarian who has been waiting at it "
        + "for a very long time. They know what you have thrown away. They are not judging. They have a list.",
        Branch("forgotten_book", "Ask for a forgotten book.",
            "They do not have to look it up. What comes back is what you gave up on, one improvement further "
            + "along, and it knows its own name now.",
            [
                new ConditionalRunEffect(
                    RunExpr.GreaterThan(new RemovedCardCountExpression(), RunExpr.Const(0)),
                    [
                        new RestoreRemovedCardRunEffect(
                            Purpose: "choose the book the Librarian has been keeping",
                            ExtraUpgrades: 1,
                            Tags: [ActTwoEventObjects.TrueName]),
                        Install(ActTwoEventPrograms.Inscriptions),
                    ],
                    [RareCardReward(pools, "librarian")]),
            ]),
        Branch("forget", "Ask the Librarian to forget a volume.",
            "They take it off the list, which is a different and much quieter thing than destroying it. "
            + "Whatever it was costing you stops.",
            [Remove("choose the volume to be forgotten"), Heal(15)]),
        Branch("shortest", "Ask for the shortest path.",
            "They point down an aisle that was not there either. Whatever is at the end of it arrived in a "
            + "hurry and diminished, nobody is paying you for the shortcut, and there is a procedure waiting.",
            [Install(ActTwoEventPrograms.ShortestPathWaits)]));

    // ── the shared shapes ─────────────────────────────────────────────────────────────────────────────────

    private static IRunSelector<RunCardInstance> Choose(string purpose) =>
        RunSelectors.DeckCards.ChooseByPlayer(1, purpose);

    // A card the player has already had improved — how "one of THEM" is asked for when the two upgrades were
    // one prompt. It reaches every improved card, not only the two just picked; the archives are not fussy.
    private static IRunSelector<RunCardInstance> Improved(string purpose) =>
        RunSelectors.DeckCards.Matching(CardValue.Upgraded).ChooseByPlayer(1, purpose);

    private static IRunEffectRequest Gold(int amount) =>
        new ChangeResourceRunEffect(StandardRunIds.Gold, amount);

    private static RunCost Price(int gold) =>
        new(RunExpr.HasResource(StandardRunIds.Gold, gold), [Gold(-gold)]);

    // "Heal N% of Max HP", rounded up, exactly as the city's tea break does it.
    private static IRunEffectRequest Heal(int percent) =>
        new ComputedHealRunEffect(RunExpr.Divide(
            RunExpr.Add(RunExpr.Multiply(RunExpr.MaxHealth, RunExpr.Const(percent)), RunExpr.Const(99)),
            RunExpr.Const(100)));

    private static IRunEffectRequest Upgrade(int count, string purpose) =>
        new UpgradeCardsRunEffect(RunSelectors.DeckCards.Upgradable().ChooseByPlayer(count, purpose));

    private static IRunEffectRequest Remove(string purpose) =>
        new RemoveCardsRunEffect(Choose(purpose));

    private static IRunEffectRequest CardReward(ConversionPools pools, string where) =>
        new OfferRewardRunEffect(new RewardId($"event:{where}"), pools.CardRewardSource(), 1);

    private static IRunEffectRequest RareCardReward(ConversionPools pools, string where) =>
        new OfferRewardRunEffect(new RewardId($"event:{where}:rare"), pools.CardRewardSource("rare"), 1);

    // "Choose 1 of 3 Uncommon cards" — and, where the door promised it, whatever the chosen card is to carry.
    // The tag rides inside the OFFER rather than following it, so a declined reward writes nothing.
    private static IRunEffectRequest UncommonChoice(
        ConversionPools pools, string where, params string[] tags) =>
        new OfferRewardRunEffect(
            new RewardId($"event:{where}:uncommon"), pools.CardRewardSource("uncommon", 3, tags), 1);

    private static IRunEffectRequest Install(string program) =>
        new InstallProgramByIdRunEffect(new RunProgramSourceId(program));

    private static IRunEffectRequest NextFightRule(string statusId) => Applies(statusId).AsOpening();

    private static CombatNodeModel Applies(string statusId) =>
        new("applyStatus", You, CombatAmountSpec.FromConst(1), StatusId: statusId);

    private static IRunEffectRequest AsOpening(this CombatNodeModel node) => Openings.NextCombat(node);

    // What a fight has to be carrying to honour anything the archives wrote between fights. The regulations are
    // idempotent by construction, so installing them beside the markings costs nothing when nothing is
    // misfiled — and leaving them out when something IS would make the misfiling silently mean nothing.
    private static IRunEffectRequest ArchiveOpening() =>
        Openings.NextCombat(
            Applies(ActTwoEventObjects.ArchiveMarkings), Applies(ActTwo.ArchiveRegulationsId));

    // One card the player picks, wearing everything the archive wrote on it, honoured by the next fight only.
    private static IReadOnlyList<IRunEffectRequest> MarkForNextFight(string purpose, params string[] marks) =>
        MarkChosen(Choose(purpose), marks);

    private static IReadOnlyList<IRunEffectRequest> MarkChosen(
        IRunSelector<RunCardInstance> selector, params string[] marks) =>
    [
        new ForEachCardRunEffect(selector,
            [.. marks.Select(m => RunEffectTemplates.TagThisCard(new RunCardTagId(m)))]),
        ArchiveOpening(),
        Install(ActTwoEventPrograms.MarkingsExpire),
    ];

    // …and the same over several cards at once ("up to 2 remaining cards begin next combat Misfiled").
    private static IReadOnlyList<IRunEffectRequest> MarkEach(int count, string purpose, params string[] marks) =>
        MarkChosen(RunSelectors.DeckCards.ChooseByPlayer(count, purpose), marks);

    // An INSCRIPTION: written on one card and never cleared, so its rule has to be in every later fight.
    private static IReadOnlyList<IRunEffectRequest> Inscribe(string inscription, string purpose) =>
    [
        new TagCardsRunEffect(Choose(purpose), new RunCardTagId(inscription), true),
        Install(ActTwoEventPrograms.Inscriptions),
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
        IReadOnlyList<RunCost>? Costs, IRunExpression<bool>? Requirement);

    private static EventBranch Branch(
        string id, string text, string result, IReadOnlyList<IRunEffectRequest> effects,
        IReadOnlyList<RunCost>? costs = null, IRunExpression<bool>? requirement = null) =>
        new(id, text, result, effects, costs, requirement);

    // Choose, then read what it did to you, then leave — the same three beats the city's doors use.
    private static BnbEvent Event(
        string id, string name, int stage, string text, params EventBranch[] branches)
    {
        var situations = new List<EventSituation>
        {
            new("start", text, branches
                .Select(b => new EventChoice(b.Id, b.Effects, NextSituationId: $"result:{b.Id}",
                    Requirement: b.Requirement, TextKey: b.Text, Costs: b.Costs))
                .ToList()),
        };
        situations.AddRange(branches.Select(b => new EventSituation($"result:{b.Id}", b.Result,
            [new EventChoice("continue", [], TextKey: "Continue")])));

        return new BnbEvent(id, name, new EventScript("start", situations),
            EarliestDepthPercent: Depth(stage));
    }

    // The design's "Earliest Stage N" as a share of the act's own depth: stage 1 is the doorstep, the last
    // stage is the far end. The generated map is taller than the act's stage ladder, so the gate is expressed
    // as a percentage and the generator measures its own rows against it.
    private static int Depth(int stage) => (stage - 1) * 100 / (Stages - 1);
}
