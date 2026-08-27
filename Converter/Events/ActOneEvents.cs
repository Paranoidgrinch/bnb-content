using RogueDeck.Core.Combat;
using RogueDeck.Run;
using RogueDeck.Scenario.Authoring;
using BnbContent.Converter.Relics;
using static BnbContent.Converter.Cards.CardAuthoring;
using static BnbContent.Converter.Relics.RelicAuthoring;

namespace BnbContent.Converter.Events;

// Act I's fifteen events, from `source-data/design/BnB_Final_Events_Master_PostAudit.md` §"ACT I".
//
// They are AUTHORED here rather than converted from the old event JSON — the ported v2 events wore the same
// names but did other things, and this file replaces them. Everything an event says is said in one of four
// ways, all of which already exist:
//
//   · a run effect that happens now — gold, a card removed, a relic taken, a transform;
//   · a MARKING written on one persistent card, which the next fight honours (ActOneEventObjects);
//   · a one-fight RULE, installed as the status the next fight opens with (ActOneEventObjects);
//   · a lasting PROMISE kept after the next fight, installed by name (ActOneEventPrograms).
//
// The prose is the city's own — the drawer that sorts by guilt, the clerk who almost helps — carried over from
// the shipped events, whose text was never the thing that had to change.
public static class ActOneEvents
{
    public const int Act = 1;

    public static IReadOnlyList<BnbEvent> All(ConversionPools pools, Random rng) =>
    [
        MisfilingCabinet(pools),
        CertifiedCopyDrawer(),
        SelfAmendingFeeTable(),
        LostAndFoundDesk(),
        LicensedVendor(pools, rng),
        ComplaintLedger(pools),
        WaitingTokenExchange(),
        AlmostHelpfulClerk(),
        WitnessQueue(pools),
        SealedBackDoor(),
        ClerksTeaBreak(),
        FriendlyFilingCabinet(pools),
        ReceiptOfPriorEffort(),
        ContradictoryMap(pools),
        ArchiveWindow(),
    ];

    // ── 1 ─────────────────────────────────────────────────────────────────────────────────────────────────

    private static BnbEvent MisfilingCabinet(ConversionPools pools) => Event(
        "misfiling_cabinet", "The Misfiling Cabinet",
        "A cabinet of blackened oak sorts papers by guilt, not alphabet. One drawer opens with a sound like a "
        + "verdict being reconsidered.",
        Branch("refile", "Let it refile the application.",
            "The application comes back under a title nobody chose, fifty Gold falls out of a fold in the "
            + "paper, and a second document quietly leaves the alphabet.",
            [
                new TransformCardsRunEffect(
                    Choose("choose a card for the cabinet to refile"), pools.TransformPool()),
                Gold(50),
                .. MarkForNextFight(ActOneEventObjects.Misfiled, "choose the card that will be Misfiled"),
            ]),
        Branch("pull", "Pull the entire file free.",
            "One obsolete procedure tears loose. Two of the cabinet's own forms come away with it.",
            [
                new RemoveCardsRunEffect(Choose("choose a card to pull out of the file")),
                Openings.NextCombat(
                    AddCard(ActOneEventObjects.MissingSignature.Id, CardZone.DrawPile),
                    AddCard(ActOneEventObjects.WrongForm.Id, CardZone.DrawPile)),
            ]));

    // ── 2 ─────────────────────────────────────────────────────────────────────────────────────────────────

    private static BnbEvent CertifiedCopyDrawer() => Event(
        "certified_copy_drawer", "The Certified Copy Drawer",
        "A brass drawer offers certified copies of anything placed inside. The copies are perfect. The drawer "
        + "is hungry.",
        Branch("duplicate", "Request a certified duplicate.",
            "The drawer returns two of what you gave it, and one duplicate notice for its trouble.",
            [
                new DuplicateCardsRunEffect(Choose("choose a card to have certified in duplicate")),
                new AddCardToDeckRunEffect(new CardDefinitionId("duplicate_copy")),
            ]),
        Branch("instrument", "Take the certified instrument.",
            "The stamp is yours. The drawer keeps one of your papers sealed until it is satisfied it is not a "
            + "copy.",
            [
                .. Grant(EventRelics.ActI, "originality_stamp"),
                .. MarkForNextFight(ActOneEventObjects.Sealed, "choose the card the drawer seals"),
            ]));

    // ── 3 ─────────────────────────────────────────────────────────────────────────────────────────────────

    private static BnbEvent SelfAmendingFeeTable() => Event(
        "self_amending_fee_table", "The Self-Amending Fee Table",
        "A slate fee table updates while you read it. Each new line looks older and more legally binding than "
        + "the last.",
        Branch("pay", "Pay the comprehensive fee. (150 Gold)",
            "The table accepts the ruinous sum and stamps two procedures as properly improved.",
            [
                new UpgradeCardsRunEffect(
                    RunSelectors.DeckCards.Upgradable().ChooseByPlayer(2, "improve a card")),
            ],
            costs: [Price(150)]),
        Branch("waiver", "Apply for a fee waiver.",
            "Seventy-five Gold is refunded on the spot. The refund is provisional, and the audit is already "
            + "scheduled.",
            [
                Gold(75),
                NextFightRule(ActOneEventObjects.AuditNotice),
                Install(ActOneEventPrograms.AuditNotice),
            ]));

    // ── 4 ─────────────────────────────────────────────────────────────────────────────────────────────────

    private static BnbEvent LostAndFoundDesk() => Event(
        "lost_and_found_desk", "The Lost-and-Found Desk",
        "Behind a dusty desk lie lost seals, receipt spikes, wax knives, and forms nobody wants to admit they "
        + "misplaced.",
        Branch("identify", "Leave a card for identification.",
            "The desk takes it away for examination. Whatever comes back will have been improved by the "
            + "attention.",
            SendUnderReview("choose a card to leave for identification")),
        Branch("claim", "Claim an unlabelled parcel.",
            "The tag is yours, and so is the unsigned form somebody left inside the parcel.",
            [
                .. Grant(EventRelics.ActI, "unclaimed_property_tag"),
                Openings.NextCombat(AddCard(ActOneEventObjects.MissingSignature.Id, CardZone.DrawPile)),
            ]));

    // ── 6 ─────────────────────────────────────────────────────────────────────────────────────────────────

    private static BnbEvent ComplaintLedger(ConversionPools pools) => Event(
        "complaint_ledger", "The Complaint Ledger",
        "A heavy ledger invites formal complaints. Its previous entries are complaining about the complaints "
        + "below them.",
        Branch("complain", "File a formal complaint.",
            "One useless procedure is struck from your file, and the next office is instructed to let the "
            + "first filing against you go.",
            [
                new RemoveCardsRunEffect(Choose("choose a card to complain about")),
                NextFightRule(ActOneEventObjects.AdministrativeExemption),
            ]),
        Branch("witness", "Sign as a supporting witness.",
            "Your signature buys you a procedure of your own — and a witness who will be watching how you "
            + "file it.",
            [
                CardReward(pools, "complaint_ledger"),
                NextFightRule(ActOneEventObjects.WitnessedProcedure),
            ]));

    // ── 7 ─────────────────────────────────────────────────────────────────────────────────────────────────

    private static BnbEvent WaitingTokenExchange() => Event(
        "waiting_token_exchange", "The Waiting Token Exchange",
        "At the end of a bench sits a counter that trades lost waiting time for small official advantages.",
        Branch("three_hours", "Exchange three hours of waiting.",
            "The ticket is yours. The counter, in return, opens late tomorrow.",
            [
                .. Grant(EventRelics.ActI, "uncalled_ticket"),
                NextFightRule(ActOneEventObjects.RestrictedPublicHours),
            ]),
        Branch("place_in_line", "Exchange your place in line.",
            "A procedure improves while you wait. A notice of delay takes your former place.",
            [
                new UpgradeCardsRunEffect(
                    RunSelectors.DeckCards.Upgradable().ChooseByPlayer(1, "improve a card")),
                Openings.NextCombat(
                    Applies(ActOneEventObjects.PriorityNumber),
                    AddCard(ActOneEventObjects.NoticeOfDelay.Id, CardZone.Hand)),
            ]));

    // ── 8 ─────────────────────────────────────────────────────────────────────────────────────────────────

    private static BnbEvent AlmostHelpfulClerk() => Event(
        "almost_helpful_clerk", "The Almost-Helpful Clerk",
        "A clerk looks up, understands almost everything, and reaches for a stamp. This is either a miracle or "
        + "a trap.",
        Branch("stamp", "Accept the helpful stamp.",
            "The stamp lands slightly off-centre, which is apparently what makes it free. The clerk files a "
            + "form that was never signed.",
            [
                .. MarkForNextFight(ActOneEventObjects.Stamped, "choose the card to have stamped"),
                Openings.NextCombat(AddCard(ActOneEventObjects.MissingSignature.Id, CardZone.Hand)),
            ]),
        Branch("route", "Accept the corrected route.",
            "You arrive by the short corridor. Whoever was waiting for you had to run, and nobody is paying "
            + "for the paperwork.",
            ExpeditedRoute()));

    // ── 9 ─────────────────────────────────────────────────────────────────────────────────────────────────

    private static BnbEvent WitnessQueue(ConversionPools pools) => Event(
        "witness_queue", "The Witness Queue",
        "Three witnesses swear you were here before. Unfortunately, none of them agree on what you were doing.",
        Branch("first", "Trust the first witness.",
            "The witness produces something official from an inside pocket, and two duplicate notices from "
            + "the other one.",
            [
                RandomRelic(pools, "witness_queue"),
                new AddCardToDeckRunEffect(new CardDefinitionId("duplicate_copy")),
                new AddCardToDeckRunEffect(new CardDefinitionId("duplicate_copy")),
            ]),
        Branch("second", "Trust the second witness.",
            "One false statement leaves your file. A summons arrives to replace it.",
            [
                new RemoveCardsRunEffect(Choose("choose the statement to withdraw")),
                Openings.NextCombat(AddCard(ActOneEventObjects.SummonsToAppear.Id, CardZone.Hand)),
            ]),
        Branch("cross_examine", "Cross-examine all three.",
            "The contradictions are useful. So is the protection they buy you — and so, unfortunately, is the "
            + "witness now attached to your filing.",
            [
                CardReward(pools, "witness_queue"),
                Openings.NextCombat(
                    Applies(ActOneEventObjects.WitnessProtection),
                    Applies(ActOneEventObjects.WitnessedProcedure)),
            ]));

    // ── 10 ────────────────────────────────────────────────────────────────────────────────────────────────

    private static BnbEvent SealedBackDoor() => Event(
        "sealed_back_door", "The Sealed Back Door",
        "A sealed side door claims to lead directly to the right corridor. The seal disagrees in very small "
        + "print.",
        Branch("break", "Break the seal.",
            "The corridor is exactly as advertised, and so is the summons that was pinned to the other side "
            + "of the door.",
            [
                Install(ActOneEventPrograms.ExtraCardReward),
                Openings.NextCombat(AddCard(ActOneEventObjects.SummonsToAppear.Id, CardZone.DrawPile)),
            ]),
        Branch("respect", "Respect the seal.",
            "The ward is yours. Word travels: whatever is waiting has been told to take you seriously.",
            [
                .. Grant(EventRelics.ActI, "threshold_ward"),
                Openings.NextCombat(new CombatNodeModel("applyStatus", "allEnemies",
                    CombatAmountSpec.FromConst(4), StatusId: "strength")),
            ]));

    // ── 11 ────────────────────────────────────────────────────────────────────────────────────────────────

    private static BnbEvent ClerksTeaBreak() => Event(
        "clerks_tea_break", "The Clerk's Tea Break",
        "For one impossible minute, no one is responsible for anything. A cup of tea steams beside an "
        + "abandoned ledger.",
        Branch("tea", "Drink the lukewarm tea.",
            "It tastes like dust, mercy, and exactly a fifth of your maximum health.",
            [
                new ComputedHealRunEffect(RunExpr.Divide(
                    RunExpr.Add(RunExpr.Multiply(RunExpr.MaxHealth, RunExpr.Const(20)), RunExpr.Const(99)),
                    RunExpr.Const(100))),
            ]),
        Branch("notes", "Read the abandoned notes.",
            "One genuinely useful correction, and one clerk's private arrangement about hours worked after "
            + "the counter closes.",
            [
                new UpgradeCardsRunEffect(
                    RunSelectors.DeckCards.Upgradable().ChooseByPlayer(1, "improve a card")),
                NextFightRule(ActOneEventObjects.AuthorizedOvertime),
            ]));

    // ── 12 ────────────────────────────────────────────────────────────────────────────────────────────────

    private static BnbEvent FriendlyFilingCabinet(ConversionPools pools) => Event(
        "friendly_filing_cabinet", "The Friendly Filing Cabinet",
        "A filing cabinet opens helpfully. The other furniture watches it with professional suspicion.",
        Branch("alphabetize", "Let it alphabetize the deck.",
            "One unnecessary item is filed somewhere you will never have to visit.",
            [new RemoveCardsRunEffect(Choose("choose a card to file away for good"))]),
        Branch("better_form", "Let it find a better form.",
            "The cabinet knows a form that does what you meant. It puts the new one where you will find it "
            + "first.",
            [
                new TransformCardsRunEffect(
                    Choose("choose a card to be replaced with a better form"), pools.TransformPool()),
                // The FRESH copy, not the one that was handed in: a transform removes the old instance and adds
                // a new one, so "the transformed card" is whatever the deck just gained.
                new TagCardsRunEffect(
                    RunSelectors.LastAddedCard, new RunCardTagId(ActOneEventObjects.FastTrack), true),
                Applies(ActOneEventObjects.MarkingsRule.Id).AsOpening(),
                Install(ActOneEventPrograms.MarkingsExpire),
            ]));

    // ── 13 ────────────────────────────────────────────────────────────────────────────────────────────────

    private static BnbEvent ReceiptOfPriorEffort() => Event(
        "receipt_of_prior_effort", "Receipt of Prior Effort",
        "An old receipt proves that you once nearly did everything correctly. The ink is faded, but the "
        + "implication is useful.",
        Branch("redeem", "Redeem the receipt.",
            "A clerk grudgingly pays out an administrative refund of seventy-five Gold.",
            [Gold(75)]),
        Branch("claim", "Submit a performance claim.",
            "The claim is accepted, at a rate that depends entirely on how quickly you finish the next piece "
            + "of work.",
            [
                NextFightRule(ActOneEventObjects.ReceiptOfPriorEffort),
                Install(ActOneEventPrograms.ReceiptOfPriorEffort),
            ]));

    // ── 14 ────────────────────────────────────────────────────────────────────────────────────────────────

    private static BnbEvent ContradictoryMap(ConversionPools pools) => Event(
        "contradictory_map", "The Contradictory Map",
        "A wall map shows three routes to the same office. Two exist only on Tuesdays, and one is arguing "
        + "with itself.",
        Branch("direct", "Follow the direct corridor.",
            "You arrive early and unannounced. Nobody has authorised payment for work done off the route.",
            ExpeditedRoute()),
        Branch("annotated", "Follow the annotated corridor.",
            "The annotations are useful and terrifying in equal measure. One of them turns out to be the "
            + "wrong form entirely.",
            [
                CardReward(pools, "contradictory_map"),
                Openings.NextCombat(
                    Applies(ActOneEventObjects.CorrectWindow),
                    AddCard(ActOneEventObjects.WrongForm.Id, CardZone.DrawPile)),
            ]),
        Branch("fold", "Fold the map incorrectly.",
            "The creases cross out a corridor that was never there. You keep the map. The map keeps handing "
            + "you the wrong form.",
            [
                .. Grant(EventRelics.ActI, "crossed_out_map"),
                Openings.NextCombat(AddCard(ActOneEventObjects.WrongForm.Id, CardZone.DrawPile)),
                Install(ActOneEventPrograms.WrongFormAgain),
            ]));

    // ── 15 ────────────────────────────────────────────────────────────────────────────────────────────────

    private static BnbEvent ArchiveWindow() => Event(
        "archive_window", "The Archive Window",
        "A narrow window opens into an archive older than the city ordinance. A hand made of dust offers two "
        + "kinds of help.",
        Branch("tool", "Take the old tool.",
            "The folder still remembers how to be useful. The terms under which it was lent are printed very "
            + "small.",
            [
                .. Grant(EventRelics.ActI, "inherited_bone_folder"),
                Openings.NextCombat(
                    AddCard(ActOneEventObjects.FinePrint.Id, CardZone.DrawPile),
                    Applies(ActOneEventObjects.FinePrintTax.Id)),
            ]),
        Branch("method", "Submit a method for preservation.",
            "The archive takes the method away to copy it properly. What comes back is better, and it is "
            + "stamped as the original.",
            SendUnderReviewAsOriginal("choose a method to submit for preservation")));

    // ── 5 ─────────────────────────────────────────────────────────────────────────────────────────────────
    //
    // ADAPTATION: an event cannot open a shop NODE, and the shop node is where the engine keeps a shop's live
    // state (a display drawn per visit, a paid reroll). So the vendor is a counter built INSIDE the event: an
    // authored stock of five cards, six relic offers and one removal, each bought at most once, at the same
    // prices the city shop charges. What it costs the port is the reroll and the per-run redraw — this
    // vendor's shelf is the same shelf every run, whereas a shop node's is dealt fresh.
    private static BnbEvent LicensedVendor(ConversionPools pools, Random rng)
    {
        var cardPrices = new Dictionary<string, int> { ["common"] = 55, ["uncommon"] = 85, ["rare"] = 130 };
        var relicPrices = new Dictionary<string, int> { ["common"] = 130, ["uncommon"] = 190, ["rare"] = 260 };

        var cards = pools.RewardCards.OrderBy(_ => rng.Next()).Take(5).ToList();
        // "Relics may be eligible Normal or Shop Relics under standard Shop eligibility. Event/Boss Relics are
        // excluded" — which is exactly what the act's own relic pool already is.
        var relics = pools.Relics.OrderBy(_ => rng.Next()).Take(6).ToList();

        var stock = new List<EventChoice>();
        foreach (var (card, index) in cards.Select((c, i) => (c, i)))
        {
            var price = cardPrices.GetValueOrDefault(card.Rarity, 85);
            stock.Add(Stall($"card-{index}", $"{card.Name} — {price} Gold", price,
                [new AddCardToDeckRunEffect(new CardDefinitionId(card.Id))]));
        }
        foreach (var (relic, index) in relics.Select((r, i) => (r, i)))
        {
            var price = relicPrices.GetValueOrDefault(relic.Source.Rarity ?? "common", 190);
            stock.Add(Stall($"relic-{index}", $"{relic.Source.Name} — {price} Gold", price,
                ConversionPools.RelicOffer(relic).Grant));
        }
        stock.Add(Stall("removal", "Have a card struck from your file — 75 Gold", 75,
            [new RemoveCardsRunEffect(Choose("choose a card to have struck from your file"))]));
        stock.Add(new EventChoice("leave", [], TextKey: "Thank the vendor and move on."));

        return new BnbEvent("licensed_vendor", "The Licensed Vendor",
            new EventScript("start",
            [
                new EventSituation("start",
                    "A vendor unfolds a counter covered in seals, receipts, and legally glowing merchandise. "
                    + "Every item has been licensed by someone who is currently denying responsibility.",
                    [
                        new EventChoice("browse", [], NextSituationId: "stock",
                            TextKey: "Browse the licensed stock."),
                        new EventChoice("sample",
                            [
                                RandomRelic(pools, "licensed_vendor"),
                                NextFightRule(ActOneEventObjects.GarnishedReward),
                                Install(ActOneEventPrograms.GarnishedReward),
                                Openings.NextCombat(
                                    AddCard(ActOneEventObjects.FinePrint.Id, CardZone.DrawPile),
                                    Applies(ActOneEventObjects.FinePrintTax.Id)),
                            ],
                            NextSituationId: "sample",
                            TextKey: "Accept the sealed sample."),
                    ]),
                new EventSituation("stock",
                    "The prices are official, which somehow makes them worse.", stock),
                new EventSituation("sample",
                    "The vendor hands over something wrapped in a receipt. The receipt continues on the back, "
                    + "where it explains who is collecting your next fee.",
                    [new EventChoice("continue", [], TextKey: "Continue")]),
            ]));
    }

    // One shelf entry: paid for once, then it is sold out for the rest of the visit, and the counter stays open.
    private static EventChoice Stall(
        string id, string text, int price, IReadOnlyList<IRunEffectRequest> payload)
    {
        var sold = new RunFlagId($"licensed_vendor.{id}");
        return new EventChoice(id,
            [.. payload, new SetFlagRunEffect(sold)],
            NextSituationId: "stock",
            Requirement: RunExpr.Not(RunExpr.Flag(sold)),
            TextKey: text,
            Costs: [Price(price)]);
    }

    // ── the shared shapes ─────────────────────────────────────────────────────────────────────────────────

    // A card the player picks out of their own deck.
    private static IRunSelector<RunCardInstance> Choose(string purpose) =>
        RunSelectors.DeckCards.ChooseByPlayer(1, purpose);

    private static IRunEffectRequest Gold(int amount) =>
        new ChangeResourceRunEffect(StandardRunIds.Gold, amount);

    private static RunCost Price(int gold) =>
        new(RunExpr.HasResource(StandardRunIds.Gold, gold), [Gold(-gold)]);

    private static IRunEffectRequest CardReward(ConversionPools pools, string where) =>
        new OfferRewardRunEffect(new RewardId($"event:{where}"), pools.CardRewardSource(), 1);

    private static IRunEffectRequest RandomRelic(ConversionPools pools, string where) =>
        new OfferRewardRunEffect(
            new RewardId($"event:{where}:relic"), pools.RelicGrantSource(null, $"event '{where}'"), 1);

    // A named Event relic, plus whatever it does the moment it is taken — the engine has no per-relic pickup
    // hook, so a grant site carries them (the Crossed-Out Map's free step is one of these).
    private static IReadOnlyList<IRunEffectRequest> Grant(IReadOnlyList<BnbRelic> pool, string id)
    {
        var relic = pool.FirstOrDefault(r => r.Id == id)
            ?? throw new ConversionException($"event relic '{id}'", "no relic with this id is authored");
        return [new AddRelicByIdRunEffect(new RelicId(relic.Id)), .. relic.Pickup ?? []];
    }

    private static IRunEffectRequest Install(string program) =>
        new InstallProgramByIdRunEffect(new RunProgramSourceId(program));

    // "Next combat uses X" — the rule as the status the fight opens with.
    private static IRunEffectRequest NextFightRule(string statusId) => Applies(statusId).AsOpening();

    private static CombatNodeModel Applies(string statusId) =>
        new("applyStatus", "source", CombatAmountSpec.FromConst(1), StatusId: statusId);

    private static IRunEffectRequest AsOpening(this CombatNodeModel node) => Openings.NextCombat(node);

    // A marking: written on one card the player picks, honoured by the next fight, and spent there.
    private static IReadOnlyList<IRunEffectRequest> MarkForNextFight(string marking, string purpose) =>
    [
        new TagCardsRunEffect(Choose(purpose), new RunCardTagId(marking), true),
        Applies(ActOneEventObjects.MarkingsRule.Id).AsOpening(),
        Install(ActOneEventPrograms.MarkingsExpire),
    ];

    // Under Review is the marking the next fight does not spend: the card is held out of it and comes back
    // upgraded once it is over.
    private static IReadOnlyList<IRunEffectRequest> SendUnderReview(string purpose) =>
    [
        new TagCardsRunEffect(Choose(purpose), new RunCardTagId(ActOneEventObjects.UnderReview), true),
        Applies(ActOneEventObjects.MarkingsRule.Id).AsOpening(),
        Install(ActOneEventPrograms.UnderReviewReturns),
    ];

    // …and the Archive Window's version, where the same card also comes back stamped as an original — one
    // choice, two markings, so the player is asked once.
    private static IReadOnlyList<IRunEffectRequest> SendUnderReviewAsOriginal(string purpose) =>
    [
        new ForEachCardRunEffect(Choose(purpose),
        [
            RunEffectTemplates.TagThisCard(new RunCardTagId(ActOneEventObjects.UnderReview)),
            RunEffectTemplates.TagThisCard(new RunCardTagId(ActOneEventObjects.CertifiedOriginal)),
        ]),
        Applies(ActOneEventObjects.MarkingsRule.Id).AsOpening(),
        Install(ActOneEventPrograms.UnderReviewReturns),
        Install(ActOneEventPrograms.CertifiedOriginal),
    ];

    // "The next eligible enemy has 30% less Max HP; combat grants no Gold."
    //
    // ADAPTATION: max health cannot be lowered from outside a fight, so the shortfall is paid as unblockable
    // damage at the opening bell — 30% of each enemy's own maximum, read per body rather than as one flat
    // number. A body that would have started at 70% of its HP starts there.
    private static IReadOnlyList<IRunEffectRequest> ExpeditedRoute() =>
    [
        Openings.NextCombat(
            Applies(ActOneEventObjects.ExpeditedRoute),
            CombatNodeModel.ForEach("allEnemies",
                new CombatNodeModel("dealDamage", "iterationTarget",
                    CombatAmountSpec.Binary("div",
                        CombatAmountSpec.Binary("mul",
                            new CombatAmountSpec("maxHealth", SelectorKey: "iterationTarget"),
                            CombatAmountSpec.FromConst(30)),
                        CombatAmountSpec.FromConst(100)),
                    IgnoresBlock: true))),
        Install(ActOneEventPrograms.GarnishedReward),
    ];

    // ── the event's own shape ─────────────────────────────────────────────────────────────────────────────

    private sealed record EventBranch(
        string Id, string Text, string Result, IReadOnlyList<IRunEffectRequest> Effects,
        IReadOnlyList<RunCost>? Costs);

    private static EventBranch Branch(
        string id, string text, string result, IReadOnlyList<IRunEffectRequest> effects,
        IReadOnlyList<RunCost>? costs = null) => new(id, text, result, effects, costs);

    // Every branch reads the same way the ported events did: choose, then read what it did to you, then leave.
    private static BnbEvent Event(string id, string name, string text, params EventBranch[] branches)
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

        return new BnbEvent(id, name, new EventScript("start", situations));
    }
}

// An authored event: its identity and the flavour a frontend shows, plus the script the engine walks. The
// ported events carry the same three things on BabEvent; this is what they look like when nobody has to
// convert them.
// `EarliestDepthPercent` is the design's "Earliest Stage N", already converted to the act's own depth: the
// map may not put this door in the first N% of the act (MapGenerationSpec.NodeRefMinimumDepthPercent). Act I
// gates nothing, so it leaves it at 0.
public sealed record BnbEvent(
    string Id, string Name, EventScript Script, IReadOnlyList<string>? Tags = null,
    int EarliestDepthPercent = 0);
