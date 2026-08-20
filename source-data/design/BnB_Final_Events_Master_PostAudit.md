# Bureaucrats and Broomsticks
## FINAL EVENTS MASTER — Post-Audit Canon

**Status:** Content-finalized design specification pending implementation/playtesting  
**Canonical source base:** `Bureaucrats_and_Broomsticks_Master_Events_PrePruning_Snapshot.md` only  
**Total events:** 65 — Act I 15 · Act II 15 · Act III 15 · Act IV 20 · Act V 0  
**Event-exclusive relics:** 25

This file is the post-audit implementation master. It preserves the event identities from the Pre-Pruning Snapshot while incorporating the relic integration, naming audit, taxonomy cleanup, reward-pool migration and final mechanical fixes developed afterward.

## Global rules

- Player-card primary types are **Deed, Working, Rite, Junk**. `Form` is a tag, not a primary type. Enemy **Attack** remains an enemy-intent term.
- **Normal Relic** = one of the 50 global Common/Uncommon/Rare relics.
- **Shop Relic** = one of the 24 shop-exclusive relics.
- **Event Relic** = one of the 25 relics tied to a specific event branch.
- **Boss Relics never appear in Events.**
- Random relic rewards obey duplicate and character-eligibility rules.
- Event Relics do not enter Normal, Treasure, Shop or Boss pools unless explicitly overridden later.
- If multiple voluntary prevention/replacement effects react to the same status application, the player chooses resolution order and the remaining application is recalculated after each effect.

---

# ACT I — THE CITY

**Events:** 15 · **Event Relics:** 6

## Shared Act-I event objects

### Temporary cards
- **Missing Signature** — 1 Energy, Exhaust. If still in hand at end of turn: gain 1 Paperwork.
- **Notice of Delay** — 1 Energy, Retain, Exhaust. If still in hand at end of turn: gain 1 Fatigue.
- **Summons to Appear** — 1 Energy, Retain, Exhaust. If still in hand at end of turn: take 5 damage.
- **Fine Print** — Unplayable, Ethereal. When drawn, the next card played this turn costs +1 Energy.
- **Wrong Form** — 0 Energy, Exhaust. To play: discard another card.

### Temporary card markings
- **Misfiled** — starts next combat in discard.
- **Under Review** — unavailable next combat; returns upgraded afterward.
- **Sealed** — starts outside the deck; moves to hand at round 3.
- **Fast-Track** — guaranteed in opening hand next combat.

### Permanent marking — Certified Original
First time each combat the card is played: cost −1 for that play and gain Exhaust for that play. Later recovered plays behave normally.

### Shared next-combat rules
- **Witnessed Procedure** — playing the same primary card type twice consecutively gives 1 Doubt.
- **Restricted Public Hours** — Round 1: −1 Energy; Round 2: +1 Energy; then ends.
- **Audit Notice** — after combat lose 4 Gold per HP lost, max 80.
- **Garnished Reward** — combat grants no Gold.
- **Authorized Overtime** — once in combat, unused Energy carries to the next turn.
- **Priority Number** — turn 1: +1 Energy, draw +2.
- **Administrative Exemption** — prevent first Panic, Doubt, Paperwork or Fatigue application.
- **Witness Protection** — start with 10 Block; first direct HP-damage event is prevented.
- **Correct Window** — each round displays one eligible primary type among Deed/Working/Rite; first card of that type costs 1 less that round.
- **Expedited Route** — next eligible enemy has 30% less Max HP; combat grants no Gold.

## 1. The Misfiling Cabinet
**Let it refile the application:** Transform 1 card; gain 50 Gold; a different card is Misfiled next combat.  
**Pull the entire file free:** Remove 1 card; shuffle Missing Signature and Wrong Form into next combat.

## 2. The Certified Copy Drawer
**Request a certified duplicate:** Duplicate 1 card permanently; add 1 Duplicate Copy permanently.  
**Take the certified instrument:** Gain **Originality Stamp**; choose 1 card to be Sealed next combat.

**Originality Stamp:** Once per combat, when you play a non-Junk card that has another persistent copy with the same name in your deck, create a temporary copy in hand. It costs 1 less, minimum 0, and gains Exhaust.

## 3. The Self-Amending Fee Table
**Pay the comprehensive fee:** Pay 150 Gold; upgrade 2 cards.  
**Apply for a fee waiver:** Gain 75 Gold; next combat gains Audit Notice.

## 4. The Lost-and-Found Desk
**Leave a card for identification:** Give 1 card Under Review.  
**Claim an unlabelled parcel:** Gain **Unclaimed Property Tag**; shuffle Missing Signature into next combat.

**Unclaimed Property Tag:** At combat start mark 1 random non-Junk card in the draw pile Unclaimed. The first time it enters hand it costs 1 less that turn, minimum 0, then loses Unclaimed.

## 5. The Licensed Vendor
**Browse the licensed stock:** Open the regular vendor with 5 cards, 6 relic offers and 1 removal. Relics may be eligible Normal or Shop Relics under standard Shop eligibility.  
**Accept the sealed sample:** Choose 1 of 3 vendor-legal Normal/Shop Relics; next combat has Garnished Reward and Fine Print. Event/Boss Relics are excluded.

## 6. The Complaint Ledger
**File a formal complaint:** Remove 1 card; next combat begins with Administrative Exemption.  
**Sign as a supporting witness:** Normal card reward; next combat uses Witnessed Procedure.

## 7. The Waiting Token Exchange
**Exchange three hours of waiting:** Gain **Uncalled Ticket**; next combat uses Restricted Public Hours.  
**Exchange your place in line:** Upgrade 1 card; next combat gains Priority Number; Notice of Delay begins in opening hand.

**Uncalled Ticket:** Once per combat, if you end a turn with a non-Junk card whose base cost exceeds remaining Energy, choose one and place it on top of the draw pile instead of discarding it. At the start of next turn gain 1 Energy and draw +1.

## 8. The Almost-Helpful Clerk
**Accept the helpful stamp:** Choose 1 card; next combat opens with a temporary 0-cost Exhaust copy of it plus Missing Signature.  
**Accept the corrected route:** Next combat uses Expedited Route.  
**Narrative:** the Clerk can recognize the player when encountered again in Act II; no extra mechanical reward.

## 9. The Witness Queue
**Trust the first witness:** Gain 1 random eligible Normal Relic; add 2 Duplicate Copies permanently.  
**Trust the second witness:** Remove 1 card; Summons to Appear begins in hand next combat.  
**Cross-examine all three:** Normal card reward; next combat gains Witness Protection and Witnessed Procedure.

## 10. The Sealed Back Door
**Break the seal:** Next combat grants one additional normal card reward; shuffle Summons to Appear into it.  
**Respect the seal:** Gain **Threshold Ward**; all enemies next combat begin with 4 Strength.

**Threshold Ward:** Start each combat with 6 Block. The first time an enemy gains a positive status each combat, gain 1 Energy and 6 Block.

## 11. The Clerk's Tea Break
**Drink the lukewarm tea:** Heal 20% Max HP.  
**Read the abandoned notes:** Upgrade 1 card; next combat gains Authorized Overtime.

## 12. The Friendly Filing Cabinet
**Let it alphabetize the deck:** Remove 1 card.  
**Let it find a better form:** Transform 1 card; transformed card gains Fast-Track next combat.

## 13. Receipt of Prior Effort
**Redeem the receipt:** Gain 75 Gold.  
**Submit a performance claim:** Next combat visibly pays 125 Gold if won by end of round 3, otherwise 25 Gold.

## 14. The Contradictory Map
**Follow the direct corridor:** Next combat uses Expedited Route.  
**Follow the annotated corridor:** Normal card reward; next combat uses Correct Window; shuffle Wrong Form into it.  
**Fold the map incorrectly:** Gain **Crossed-Out Map**; shuffle Wrong Form into each of the next 2 combats.

**Crossed-Out Map:** Once per Act when choosing the next node, ignore path connections and move to any visible legal node in the immediately following row. Cannot move backward, bypass Boss gates, enter locked/scripted/ineligible nodes or override Act-level restrictions.

## 15. The Archive Window
**Take the old tool:** Gain **Inherited Bone Folder**; shuffle Fine Print into next combat.  
**Submit a method for preservation:** Give 1 card Under Review; after next combat it returns upgraded and gains Certified Original.

**Inherited Bone Folder:** At combat start mark 1 random unupgraded non-Junk card Restored. First time it enters hand that combat, temporarily upgrade it and reduce its cost by 1 for that turn. If no eligible unupgraded card exists, draw +1 on turn 1.


---

# ACT II — THE ENDLESS ARCHIVES

**Events:** 15 · **Event Relics:** 5 · **Temporary event cards:** 3 · **Permanent inscriptions:** 5

## Removed History
When a persistent card is permanently removed, store identity, permanent upgrade state, inscriptions and explicitly restorable persistent modifications. Combat-only changes and temporary copies are not stored. Restoring recreates the card and deletes the history entry. Transforming creates a new persistent instance.

## Temporary event cards
- **Unfinished Citation** — 1 Energy, Retain, Exhaust. Remove one Referenced mark from another card. If left in hand at end of turn: gain 1 Paperwork.
- **Redacted Leaf** — Unplayable, Retain. Next non-Junk playable card becomes Redacted immediately before resolution; then Exhaust the Leaf.
- **Borrower's Claim** — 0 Energy, Retain, Exhaust. Put another non-Junk hand card on bottom of draw pile; draw 1. If left in hand: gain 1 Paperwork.

## Permanent inscriptions
- **Authorized Revision** — first play each combat costs +1; if payable, positive numerical effects +50%; then spent for combat.
- **Illuminated Initial** — first play each combat draws 1 and gains 3 Block.
- **Concordant Pair** — link two persistent cards. First partner played each combat moves the other from Draw/Discard to top of Draw; if already in hand gain 3 Block; inaccessible zones do nothing. Removing/transforming either dissolves the pair.
- **True Name** — first enemy Misfiled/Referenced/Redacted marker aimed at this card each combat is prevented.
- **Late-Bound** — first time each combat card ends turn unplayed in hand: Retain. Next turn cost −1 and positive numerical effects +25%; then Ready expires.

## 1. Misfiled Prophecy — Earliest Stage 2
**Correct the filing code:** Transform 1 card; a different card begins next combat Misfiled.  
**Accept the prophecy as written:** Give 1 card Authorized Revision; add Unfinished Citation to next combat's discard pile.

## 2. The Self-Correcting Index — Earliest Stage 6
**Allow the correction:** Upgrade 2 cards; one of them, shown immediately, begins next combat Redacted.  
**Correct the index yourself:** Remove 1 card; up to 2 remaining non-Junk cards begin next combat Misfiled.

## 3. The Locked Reading Room — Earliest Stage 4
**Read under supervision:** Rare Card Reward; next combat the first fourth card each turn Redacts one random remaining valid non-Junk hand card.  
**Copy a single illuminated passage:** Pay 40 Gold; give 1 card Illuminated Initial.  
**Wait outside in silence:** Heal 20% Max HP.

## 4. The Perpetual Borrower — Earliest Stage 7
**Lend one of your own volumes:** Choose eligible non-max-upgraded non-Junk card. Next combat it starts in Borrower's Keeping; at round 2 it returns to hand, Retains, is combat-upgraded and costs 0 that turn. Victory permanently upgrades the original if possible.  
**Accept the borrower's old notes:** Choose 1 of 3 Uncommon cards; Borrower's Claim enters next combat.  
**Settle the account:** Pay 60 Gold; heal 15% Max HP; upgrade 1 card.  
**Pocket the borrower's library card:** Gain **Unreturned Library Card**; lose 8% Max HP; Borrower's Claim enters next combat.

**Unreturned Library Card:** Once per combat, the first non-Junk card entering discard without being played is returned to hand at the start of your next turn, costs 0 that turn and gains Exhaust for that play.

## 5. The Reciprocal Shelf — Earliest Stage 2
**Submit the unwanted entry:** Transform 1 card; gain 50 Gold.  
**Argue with the classification:** Normal card reward; a different card begins next combat Misfiled + Redacted. If no eligible card exists, begin with 1 Paperwork instead.  
**Take the loose shelf label:** Gain **Reversible Shelf Label**; one random non-Junk card begins each of the next 2 combats Misfiled.

**Reversible Shelf Label:** Once per combat, when a non-Junk card leaves hand without being played, remember its name. The next time a card with that name enters hand, draw 1 and reduce that returned card's cost by 1 for the turn.

## 6. The Margin Notes — Earliest Stage 3
**Follow both arguments:** Give 2 cards Concordant Pair.  
**Add an illuminated reply:** Give 1 card Illuminated Initial.  
**Scrape the margin clean:** Upgrade 1 card; shuffle Redacted Leaf into next combat.

## 7. Unclaimed Reservation — Earliest Stage 7
**Claim the reserved volume:** Choose 1 of 3 Uncommon cards. Next combat that card starts in a Reservation Zone; round 3 it returns to hand with Retain, cost 0 for that turn and a combat-only upgrade.  
**Claim the empty seat:** Heal 25% Max HP.  
**Enter another name in the register:** Gain 70 Gold. Next combat, one random valid non-Junk opening-hand card cannot be played until another card is played; if no other legal play exists, access is automatic; if left unplayed, it becomes Misfiled.

## 8. The Infinite Return Slot — Earliest Stage 7
**Return a bad idea:** Remove 1 persistent card; gain 40 Gold; store it in Removed History.  
**Reach for a lost page:** Restore 1 eligible Removed-History entry and delete that entry; shuffle Borrower's Claim into next combat.

## 9. The Redacted Portrait — Earliest Stage 5
**Restore the missing face:** Pay 100 Gold; gain **Blank Cameo**.  
**Take the absent name:** Give 1 card True Name.  
**Leave the portrait untouched:** Heal 15% Max HP.

**Blank Cameo:** After opening draw choose 1 non-Junk card. Until played it has Retain, costs 1 less and cannot be specifically selected by enemy card-targeting or negative-marker effects. If it is the only legal target of a mandatory effect, protection is ignored for that effect.

## 10. The Lost-Hour Bottle — Earliest Stage 8
**Drink the lost hour:** Next combat Round 1 +1 Energy; Round 2 +1 Energy; Round 3 −2 available Energy, minimum 0; then ends.  
**Bind the hour into a card:** Give 1 card Late-Bound.

## 11. The Necrology Window — Earliest Stage 9
**Borrow an unfinished life:** Heal 35% Max HP. Next eligible normal combat: primary enemy's first lethal event instead returns it once at 30% Max HP; after victory gain 75 bonus Gold. Effect waits through ineligible Elite/Boss/scripted nodes.  
**Close an abandoned account:** Lose 8 current HP; remove 1 card; upgrade a different card. Unavailable if lethal.

## 12. The Almost-Helpful Clerk, Reassigned — Earliest Stage 1
**Accept the whispered amendment:** Choose non-max-upgraded card; it begins next combat Redacted. If successfully played while still Redacted, permanently upgrade it after victory.  
**Accept the temporary reader's pass:** Next combat prevent the first enemy attempt to apply Misfiled/Referenced/Redacted to any player card; gain 35 Gold.  
**Ask how the Clerk has been:** Heal 20% Max HP.  
**Narrative:** recognition flag from Act I; no extra reward.

## 13. The Last Quiet Table — Earliest Stage 4
**Take the Vow of Silent Scholarship:** Next eligible combat, never play more than 3 non-Junk cards in any player turn. Breaking the Vow only forfeits the bonus. Win without breaking it → gain **Vow Bead**.  
**Read the forbidden volume:** Rare Card Reward; Redacted Leaf starts in opening hand; next combat the first fourth card each turn Redacts a random remaining valid non-Junk hand card.  
**Rest without reading:** Heal 25% Max HP.

**Vow Bead:** At the start of each turn you may Observe the Vow, capping the turn at 3 non-Junk cards. If exactly 3 are played, next turn gain 1 Energy and draw +1.

## 14. The Inward Seal — Earliest Stage 7
**Break the seal outward:** Gain **Inverted Sealstone**; two eligible non-Junk cards begin next combat Misfiled + Redacted.  
**Turn the seal inward:** Upgrade 2 cards; one begins next combat Redacted and the other Misfiled.  
**Press the seal into your skin:** Gain 8 Max HP; next combat begins with 2 Paperwork + 1 Doubt.

**Inverted Sealstone:** After opening draw choose 1 Deed or Working. The first time it is played, after resolving return that exact card to hand instead of its normal post-play destination. The second play resolves normally. Does not override explicitly non-replaceable Banished/scripted/boss-event removal.

## 15. The Librarian at the End of the Aisle — Earliest Stage 8 · Rare
**Ask for a forgotten book:** If Removed History has entries, restore one with stored state, one extra permanent upgrade if possible and True Name; delete the entry. If none exist, Rare Card Reward.  
**Ask the Librarian to forget a volume:** Remove 1 card; heal 15% Max HP; store it in Removed History.  
**Ask for the shortest path:** Next eligible normal combat enemies have 25% less Max HP, grants no Gold, and victory gives one additional normal card reward. Effect waits through ineligible nodes.


---

# ACT III — THE GREEN DOCKET

**Events:** 15 · **Event Relics:** 5 · **Permanent inscriptions:** 5

## Permanent inscriptions
- **Rowan-Blessed** — first time each combat card is first card played in a turn: gain 5 Block.
- **Way-Knotted** — first time each combat card follows a card with a different base cost: gain 1 Energy.
- **Hearth-Kept** — first time each combat card remains in hand at turn end: Retain; next-turn cost −1.
- **Stone-Witnessed** — first time each combat card targets an enemy already targeted by another player card that turn: positive numerical effects +25%.
- **Old Right** — first time each combat card would enter Exhaust because of its own normal post-play Exhaust: put it in Discard instead.

## 1. A Clear Stream — Earliest Stage 1
**Wash away what clings:** Remove 1 card; heal 5% Max HP.  
**Wash one thing carefully:** Give 1 card Rowan-Blessed; heal 10% Max HP.  
**Bottle the water:** Next normal combat, first full Wergild payment grants 1 extra Safe-Conduct.

## 2. The Noticebound Hedge — Earliest Stage 2
**Cut a lawful gap:** Pay 35 Gold; remove 1 card.  
**Cross first, explain later:** Gain 90 Gold; next normal combat begins with environmental Hedge Wergild 2; nonpayment causes normal Wergild HP loss but no Claim.  
**Ask the hedge to mark the path:** Give 1 card Way-Knotted.

## 3. The Witch at the Milestone — Earliest Stage 2
**Ask her for a knot:** Give 1 card Way-Knotted and upgrade it if possible.  
**Offer a bad memory:** Remove 1 card; lose 4 Max HP; gain 70 Gold.  
**Ask which road is shortest:** Next normal combat grants no Gold and enemies have 20% less Max HP; victory grants one additional card reward.

## 4. The Public Footpath Dispute — Earliest Stage 3
**Declare a public right:** Next normal combat starts with +2 Safe-Conduct; every enemy starts with 1 Claim; after victory gain 80 Gold.  
**Recognize the older boundary:** Remove 1 card; lose 5 current HP.  
**Mediate the dispute:** Upgrade 2 cards; next combat starts with 1 less Safe-Conduct, minimum 0.

## 5. Moonlit Mushrooms — Earliest Stage 4
**Step inside the circle:** Lose 8% Max HP; gain **Mootcap**.  
**Offer something to the circle:** Remove 1 card; upgrade 2 random other upgradeable cards.  
**Wait for quorum:** Next normal combat must end every player turn with exactly 1 or 3 non-Junk cards played; 0 is failure. Win without violating → gain **Dissenting Spore**.

**Mootcap:** First time each turn you play the third non-Junk card, choose: gain 10 Block; draw 1; or deal 7 damage to all enemies. Once per turn.  
**Dissenting Spore:** End turn with odd non-Junk count → +1 Spore, max 3; even → −1 Spore. Start turn at 3 Spores: consume all, gain 1 Energy, draw +1, gain 6 Block.

## 6. A Spider's Clause — Earliest Stage 4
**Read the exception:** Lose 6% Max HP; give one compatible Exhaust card Old Right.  
**Cut through the clause:** Remove 1 card; next normal combat starts with 1 Doubt.  
**Sign beneath the web:** Gain 100 Gold; a random enemy next normal combat starts with 1 Claim.

## 7. The Ant Queue — Earliest Stage 5
**Wait your turn:** Upgrade 2 cards; heal 10% Max HP.  
**Step over the line:** Lose 10% current HP; Rare Card Reward; gain 60 Gold.  
**Walk with the proper line:** Next normal combat, played non-Junk base costs may never decrease within a turn. Violation only forfeits bonus. Win cleanly → gain **Antway Marker**.

**Antway Marker:** If the first 3 non-Junk cards of a turn form a non-decreasing base-cost sequence, after card 3 gain 1 Energy and draw 1. If sequence decreases before card 3, no trigger that turn. Once per turn.

## 8. The Conceptual Toll — Earliest Stage 4
**Pay the conceptual toll:** Pay 45 Gold; open special market with 4 cards, 3 eligible Shop Relics, 1 removal, all prices 15% lower.  
**Dispute the crossing:** Gain 85 Gold; next normal combat starts with Conceptual Wergild 2.  
**Use the bridge anyway:** Next normal combat starts with +2 Safe-Conduct, grants no Gold, and victory grants one additional card reward.

## 9. Rain Beneath the Rowan — Earliest Stage 3
**Wait beneath the branches:** Heal 30% Max HP.  
**Ask the tree for shelter:** Give 1 card Rowan-Blessed; heal 10% Max HP.  
**Keep walking through the rain:** Lose 6 current HP; next 2 normal combats draw +1 on turn 1.

## 10. The Buried Waystone — Earliest Stage 6
**Clean the old inscription:** Give 1 card Stone-Witnessed.  
**Follow the forgotten name:** Next normal combat enemies have 20% less Max HP, grants no Gold; victory gives Rare Card Reward.  
**Bury one of your own marks beside it:** Remove 1 card; lose 5 Max HP; gain 100 Gold.

## 11. The Travelling Chandler — Earliest Stage 3
**Browse the cart:** Small market: 3 cards, 2 eligible Shop Relics, no removal, prices 20% lower.  
**Buy a traveller's flame:** Pay 50 Gold; next combat turn 1 +1 Energy and +1 Safe-Conduct.  
**Trade something old for wax:** Remove 1 card; gain 35 Gold.

## 12. Stargazing — Earliest Stage 6
**Road Star:** Lose 4 Max HP; next 2 normal combats +1 Safe-Conduct.  
**Root Star:** Upgrade 2 cards; one randomly gains Hearth-Kept.  
**Hill Star:** Rare Card Reward; highest-Max-HP enemy next normal combat starts with 1 Claim.

## 13. The Quiet Meadow — Earliest Stage 1
**Practice where nobody watches:** Upgrade 2 cards.  
**Leave something behind:** Remove 1 card; heal 10% Max HP.  
**Lie in the grass:** Heal 35% Max HP.  
**No hidden reward or relic.**

## 14. The Ombudsman's Warning — Earliest Stage 7 · Rare
**Prepare a response:** Upgrade 2 cards; next Elite/Boss combat starts with +1 Safe-Conduct. If the Act Boss is the Ombudsman of Root and Road, remove his first generated Claim once.  
**Submit your own complaint:** Next normal combat every enemy starts with 1 Paperwork + 1 Doubt; highest-Max-HP enemy also gets 1 Claim; victory gives 60 Gold.  
**Keep the leaf:** Lose 6 Max HP; gain **Complaint Leaf**.

**Complaint Leaf:** First enemy each combat that causes HP loss or directly applies a negative status becomes the Respondent. While alive, the first non-Junk card each turn targeting it costs 1 less.

## 15. The Kindly Procession — Earliest Stage 8 · Rare
**Bow and let them pass:** Heal 25% Max HP; gain 3 Max HP.  
**Walk three steps with them:** Lose 7 Max HP; gain **Guest-Right Brooch**; next normal combat +2 Safe-Conduct.  
**Give the procession something of yours:** Remove 1 card; gain 100 Gold; upgrade a different card.  
**Follow them farther than three steps (Stage 9+):** Lose 12 Max HP; gain Guest-Right Brooch; Rare Card Reward; full heal; next combat every enemy starts with 1 Claim.

**Guest-Right Brooch:** Once per Event, reduce one immediate explicit Gold/current-HP/Max-HP option cost by 25%, rounded up. Does not reduce Shop purchases, combat damage, delayed penalties, statuses, card sacrifices/removals or non-numerical costs.


---

# ACT IV — THE LICENSING LABYRINTH

**Events:** 20 · **Event Relics:** 9

## 1. The Dry Nilometer — Early–Mid
**Accept the True Level:** Lose 6 Max HP; gain **Cup of the Lowest Mark**.  
**Move the Marker:** Gain 90 Gold; next 2 combats start with Inscribed 1.  
**Leave Unmeasured:** Heal 25% Max HP; next combat starts with Paperwork 2 + Weighed 3.  
**Cup of the Lowest Mark:** First time each combat you end a turn with exactly 1 unspent Energy, heal 4 HP and draw +1 next turn.

## 2. The Black Granary — Early–Mid
**Break the Seal:** Gain 130 Gold; random eligible Common Normal Relic; next 2 combats Burdened 2.  
**Accept the Allotted Share:** Heal 35% Max HP; lose 5 Max HP.  
**Restore the Record:** Upgrade 2 cards; next combat Inscribed 1.

## 3. The Red Linen Procession — All
**Join the Procession:** Remove 1 card; heal 15% Max HP; next combat Embalmed 2.  
**Cut the Linen:** Upgrade 2 cards; next combat Entombed 2.  
**Follow Until the Last Gate:** Lose 12 current HP; gain **Red Linen Knot**.  
**Red Linen Knot:** Start combat with 8 Block. First time each combat a positive Status would naturally lose stacks/duration, prevent 1 stack or 1 turn of duration from being lost and gain 8 Block. Does not prevent consumption, enemy removal or explicit cleansing.

## 4. The Nameless Cartouche — All
**Inscribe Your Name:** Upgrade 2 cards; next 3 combats Inscribed 1.  
**Scrape It Deeper:** Remove 1 card; lose 7 Max HP.  
**Take the Fragment:** Gain **Blank Cartouche**.  
**Blank Cartouche:** Draw +1 on turn 1. First time each combat you gain Inscribed, remove 1 Inscribed.

## 5. The Forewritten Tablet — Mid–Late
**Correct One Line:** Transform 1 card; upgrade it; gain 50 Gold.  
**Demand the Tablet:** Fight Reed-Pen Scribe + Cartouche Recarver + Palette-Bearing Apprentice. Victory: random eligible Normal Relic, 60% Uncommon / 40% Rare.  
**Sign Beneath It:** Remove 2 cards; next 2 combats start with Paperwork 3.

## 6. The Tomb Robbers' Fire — Mid–Late
**Trade:** Pay 70 Gold; random eligible Uncommon Normal Relic; next combat Trespass.  
**Join the Opening:** Fight Grave-Cut Robber + Lamp Thief + Cursed Loot Bearer. Victory: 120 Gold + random eligible Common Normal Relic.  
**Steal from the Thieves:** Gain 100 Gold; next 2 combats Panic 1 + Burdened 1.

## 7. The Triple-Counted Donkey — Early–Mid
**Honor the First Tally:** Gain 75 Gold; next combat Burdened 1.  
**Break All Three Tokens:** Remove 1 card; lose 5 current HP; gain 5 Max HP.  
**Follow the Donkey:** Random eligible Common Normal Relic; heal 10% Max HP.

## 8. The Four Canopic Jars — Mid–Late
**Jar of Breath:** Gain **Jar of Borrowed Breath**.  
**Jar of Blood:** Gain 12 Max HP; next combat 5 Poison.  
**Jar of Hunger:** Gain 150 Gold; next combat Burdened 2.  
**Jar of the Name:** Upgrade 3 cards; next combat Inscribed 2.  
**Jar of Borrowed Breath:** First time each combat a temporary negative status leaves you completely, draw 1 and heal 3. Expiration/decay-to-zero/cleanse qualify; partial stack loss does not.

## 9. The Chamber of False Measures — Mid
**Heavy Weight:** Gain 10 Max HP; next 2 combats Weighed 3.  
**Light Weight:** Full heal; lose 8 Max HP.  
**Break the Scale:** Lose 15 current HP; gain **Broken Royal Weight**.  
**Broken Royal Weight:** Start combat with 10 Block. Once per combat when Weighed is failed, prevent the direct HP loss and gain Burdened 1 instead.

## 10. The Crocodile at the Weighing Place — Mid–Late
**Offer Gold:** Lose 60 Gold; gain 6 Max HP; heal 15% Max HP.  
**Place Yourself on the Scale:** Random eligible Uncommon Normal Relic; next combat Weighed 3 + Entombed 1.  
**Take the Offerings:** Gain 120 Gold; next 3 combats Inscribed 1.

## 11. The Wall of Old Complaints — All
**Add Your Own:** Upgrade 2 cards; next combat Paperwork 3.  
**Erase One:** Remove 1 card; lose 6 Max HP.  
**Read Them All:** Gain **Petition Chisel**; next 2 combats Doubt 1.  
**Petition Chisel:** Each enemy action that directly applies one or more negative statuses records 1 Grievance, max 3. At start of turn with 3: consume all, draw 2, gain 1 Energy, remove 1 stack of one negative status if possible.

## 12. The Copper Tithe — All
**Pay the Tithe:** Lose 15% current Gold; upgrade 2 cards.  
**Give More Than Required:** Lose 35% current Gold; random eligible Normal Relic, 50% Uncommon / 50% Rare.  
**Give Nothing:** Fight Copper Tribute Bearer + Ivory-Weight Jackal; victory keeps current Gold and adds 70 Gold.

## 13. The Unnamed Throne — Late · Rare
**Restore the Name:** Lose 8 Max HP; gain **Tablet of the Missing Name**.  
**Erase It Completely:** Remove 2 cards; next combat Panic 2.  
**Take the Gold Leaf:** Gain 150 Gold; next 3 combats Paperwork 2.  
**Tablet of the Missing Name:** Start combat with 1 Nameless Authority. First positive-status gain each combat consumes it and increases that gain by 50%, rounded up, minimum +1 stack. If you have Inscribed afterward, remove 1 Inscribed.

## 14. The Fixed-Day Festival — Mid
**Carry the Standard:** Upgrade 1 Deed and 1 Working; if a category is absent, use another eligible card; next combat Burdened 1.  
**Beat the Drum:** Next 2 combats: turn 1 +1 Energy and start with Panic 1.  
**Wait for the Correct Star:** Heal 40% Max HP; lose 40 Gold.

## 15. The Broken Sluice — Early–Mid
**Open It:** Heal 25% Max HP; lose up to 50 Gold.  
**Close It Properly:** Gain 8 Max HP; next combat Burdened 1.  
**Reroute the Channel:** Upgrade 2 cards; next 2 combats Weighed 2.

## 16. The Unfinished Burial — Mid–Late
**Finish the Wrapping:** Remove 1 card; gain **Funerary Linen Coil**.  
**Take the Amulet:** Random eligible Uncommon Normal Relic; next combat Embalmed 3 + Entombed 1.  
**Unwrap the Name:** Transform 2 cards; next 2 combats Inscribed 1.  
**Funerary Linen Coil:** Once per combat, the first non-Junk card deliberately Exhausted, Archived or player-Banished without being played normally heals 4 HP and draws 1.

## 17. The Survey of the Dead — Late
**Be Counted Among the Living:** Full heal; lose 8 Max HP.  
**Be Counted Among the Dead:** Gain 12 Max HP; next 3 combats Embalmed 1.  
**Refuse the Count:** Fight Gate Tally Scribe + Uncounted Pilgrim + Ancestral Witness. Victory: 90 Gold, upgrade 1 card, remove 1 card.

## 18. The House of Life at Night — Late · Rare
**Copy a Formula:** Duplicate 1 card; next combat Paperwork 2.  
**Erase a Formula:** Remove 1 card; gain 5 Max HP.  
**Replace a Line:** Transform 2 cards; upgrade both; next combat Inscribed 2.

## 19. The Merciful Balance — All
**Place Gold on the Pan:** Pay 75 Gold; remove 1 card.  
**Place Blood on the Pan:** Lose 10 Max HP; upgrade 2 cards.  
**Place Your Burden on the Pan:** Gain **Mercy Counterweight**; next combat Burdened 2 + Entombed 1.  
**Mercy Counterweight:** First time each combat you would gain a negative status, choose: reduce the application by 1 stack; or accept it normally and next turn gain 1 Energy and draw +1. Then inactive for the combat.

## 20. Cartouche Repair Bench — All
**Restore the Name:** Upgrade 1 card; heal 15% Max HP.  
**Replace the Name:** Transform 1 card; gain 50 Gold.  
**Leave No Name:** Remove 1 card; next combat Inscribed 2.

---

# ACT V — THE DIVINE LEDGER

**Normal Events:** 0  
**Event Relics:** 0  
**New relic acquisition:** none.

Act V is the final boss gauntlet.

---

# Event Relic Index

| Act | Source Event | Relic |
|---|---|---|
| I | The Certified Copy Drawer | Originality Stamp |
| I | The Lost-and-Found Desk | Unclaimed Property Tag |
| I | The Waiting Token Exchange | Uncalled Ticket |
| I | The Sealed Back Door | Threshold Ward |
| I | The Contradictory Map | Crossed-Out Map |
| I | The Archive Window | Inherited Bone Folder |
| II | The Perpetual Borrower | Unreturned Library Card |
| II | The Reciprocal Shelf | Reversible Shelf Label |
| II | The Redacted Portrait | Blank Cameo |
| II | The Last Quiet Table | Vow Bead |
| II | The Inward Seal | Inverted Sealstone |
| III | Moonlit Mushrooms | Mootcap |
| III | Moonlit Mushrooms | Dissenting Spore |
| III | The Ant Queue | Antway Marker |
| III | The Ombudsman's Warning | Complaint Leaf |
| III | The Kindly Procession | Guest-Right Brooch |
| IV | The Dry Nilometer | Cup of the Lowest Mark |
| IV | The Red Linen Procession | Red Linen Knot |
| IV | The Nameless Cartouche | Blank Cartouche |
| IV | The Four Canopic Jars | Jar of Borrowed Breath |
| IV | The Chamber of False Measures | Broken Royal Weight |
| IV | The Wall of Old Complaints | Petition Chisel |
| IV | The Unnamed Throne | Tablet of the Missing Name |
| IV | The Unfinished Burial | Funerary Linen Coil |
| IV | The Merciful Balance | Mercy Counterweight |

**Total:** 25.

# Final naming migration

| Snapshot name | Final name |
|---|---|
| The Fee Table Updates Itself | The Self-Amending Fee Table |
| The Clerk Who Almost Helps | The Almost-Helpful Clerk |
| The Borrower Who Never Returned | The Perpetual Borrower |
| The Shelf That Shelves Back | The Reciprocal Shelf |
| The Lost Hour in a Bottle | The Lost-Hour Bottle |
| The Clerk Who Almost Helps, Again | The Almost-Helpful Clerk, Reassigned |
| The Seal That Opens Inward | The Inward Seal |
| The Hedge That Requires Notice | The Noticebound Hedge |
| The Toll Without a Bridge | The Conceptual Toll |
| Rain Under One Tree | Rain Beneath the Rowan |
| The Nilometer Without Water | The Dry Nilometer |
| The Scribe Who Has Written You Already | The Forewritten Tablet |
| The Donkey Counted Three Times | The Triple-Counted Donkey |
| The Empty Throne Name | The Unnamed Throne |
| The Festival of the Fixed Day | The Fixed-Day Festival |
| Weighing Room Mercy | The Merciful Balance |

## Final verdict
The 65-event structure is retained. Numerical tuning remains playtest-adjustable; event identity, branch structure, naming, relic integration and acquisition roles are content-finalized.
