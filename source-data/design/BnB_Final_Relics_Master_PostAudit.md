# Bureaucrats and Broomsticks
## FINAL RELICS MASTER — Post-Audit Canon

**Status:** Content-finalized design specification pending implementation/playtesting  
**Total designed relics:** **168**

| Pool | Count |
|---|---:|
| Normal Common/Uncommon/Rare | 50 |
| Boss | 69 |
| Shop-exclusive | 24 |
| Event-exclusive | 25 |
| **Total** | **168** |

Act V grants **no new relics**.

---

# 1. Acquisition Matrix

## Normal Relics — 50
- **18 Common · 18 Uncommon · 14 Rare**
- **38 General · 12 Bureaucrat-specific**
- Global pool for Acts I–IV; no Act-specific unlock layer is currently applied.
- Can appear from:
  - standard random relic rewards;
  - Treasure relic rewards;
  - normal relic slots in regular Shops;
  - Events that explicitly award a random Common/Uncommon/Rare **Normal Relic**.
- Character-specific relics are only eligible for the current character.
- Event-, Boss-, and Shop-exclusive relics never enter this pool.

## Boss Relics — 69
- Each Act I–IV Boss has exactly **3 associated Boss Relics**.
- Defeating that Boss awards **1 of its 3 relics at random**.
- **No choice screen.**
- Boss Relics are character-independent.
- Boss Relics never appear in Shops, Treasure, random Normal rewards, or Events.
- Act V awards no relic after its bosses.

## Shop Relics — 24
- **18 General · 6 Bureaucrat-specific** = exact **75/25** split.
- Eligible in standard Shop relic inventory during Acts I–IV.
- Can also appear in explicitly Shop-like event markets:
  - **The Licensed Vendor**
  - **The Conceptual Toll**
  - **The Travelling Chandler**
- Shop generation should target roughly **3/4 General and 1/4 current-character-specific relics**.
- Event- and Boss-exclusive relics cannot appear in Shop stock.

## Event Relics — 25
- Each is tied to a named Event branch.
- They do not appear in Normal, Shop, Treasure or Boss pools.
- Some Events have no Event Relic; **Moonlit Mushrooms** has two possible Event Relics.
- Act V contains no Events.

---

# 2. Global Shop Economy Rules

1. Percentage discounts apply to the **base price** first.
2. Flat discounts apply afterward.
3. Minimum price is **0 Gold**.
4. Shop Credit/Vouchers apply after discounts and before Gold payment.
5. Debt is created only after available Gold is used.
6. Purchase-specific refunds can never collectively exceed the **Gold actually paid** for that purchase.
7. `Gold spent` counts only actual Gold paid, not Vouchers/Shop Credit and not newly created Debt.
8. Cashback such as **Copper Receipt Roll** is not a refund and may create net economic value.
9. A Shop Relic bought during a Shop may affect remaining transactions in that same Shop unless its trigger explicitly requires entering the Shop.
10. Warranty return is resolved before the marked relic may create new Shop-entry effects.

---

# 3. Normal Relics — 50

## Pool summary

| Rarity | Count |
|---|---:|
| Common | 18 |
| Uncommon | 18 |
| Rare | 14 |

| Eligibility | Count |
|---|---:|
| General | 38 |
| Bureaucrat-specific | 12 |

## 1. Levy Stamp
**Rarity:** Common · **Eligibility:** General  
**Can appear:** Standard Normal Relic pool; standard reward/Treasure/Shop/Event sources only where that rarity is eligible.

**Effect:** When picked up, gain **30 Gold**. After every victorious combat, gain **4 additional Gold**.

## 2. Brass Bookmark
**Rarity:** Common · **Eligibility:** General  
**Can appear:** Standard Normal Relic pool; standard reward/Treasure/Shop/Event sources only where that rarity is eligible.

**Effect:** The first non-Junk card that enters your hand outside the normal draw step each turn gains **Retain until the start of your next turn**.

## 3. Conservator's Thread
**Rarity:** Common · **Eligibility:** General  
**Can appear:** Standard Normal Relic pool; standard reward/Treasure/Shop/Event sources only where that rarity is eligible.

**Effect:** The first time each turn a card leaves your hand **without being played**, gain **4 Block**. Post-play movement does not count.

## 4. Sun-Warmed Waystone
**Rarity:** Common · **Eligibility:** General  
**Can appear:** Standard Normal Relic pool; standard reward/Treasure/Shop/Event sources only where that rarity is eligible.

**Effect:** If you end your turn with at least **1 unspent Energy**, gain **5 Block**.

## 5. Five-Notch Bead
**Rarity:** Common · **Eligibility:** General  
**Can appear:** Standard Normal Relic pool; standard reward/Treasure/Shop/Event sources only where that rarity is eligible.

**Effect:** Every **fifth non-Junk card** played during combat deals **6 damage** to the living enemy with the lowest HP. Counter persists across turns and resets after combat.

## 6. Formkeeper's Signet
**Rarity:** Common · **Eligibility:** Bureaucrat  
**Can appear:** Normal pool while playing Bureaucrat; standard reward/Treasure/Shop/Event sources only where that rarity is eligible.

**Effect:** The first time each turn you play a **Form**, gain **2 Block**. If that Form targets an enemy, apply **1 additional Paperwork** to that enemy.

## 7. Rootbound Walking Staff
**Rarity:** Uncommon · **Eligibility:** General  
**Can appear:** Standard Normal Relic pool; standard reward/Treasure/Shop/Event sources only where that rarity is eligible.

**Effect:** After leaving a **non-combat map node**, charge this relic. At the start of your next combat, if charged, gain **1 Energy + 6 Block**, then discharge. Additional non-combat nodes do not stack charges.

## 8. Counterfeit Toll Writ
**Rarity:** Uncommon · **Eligibility:** General  
**Can appear:** Standard Normal Relic pool; standard reward/Treasure/Shop/Event sources only where that rarity is eligible.

**Effect:** When picked up, gain **30 Gold**. Shop prices are **15% lower** while owned.

## 9. Emergency Inkwell
**Rarity:** Uncommon · **Eligibility:** General  
**Can appear:** Standard Normal Relic pool; standard reward/Treasure/Shop/Event sources only where that rarity is eligible.

**Effect:** **Once per combat**, after playing a card, if you have exactly **0 Energy**, gain **1 Energy**.

## 10. Ashen Wax Knife
**Rarity:** Uncommon · **Eligibility:** General  
**Can appear:** Standard Normal Relic pool; standard reward/Treasure/Shop/Event sources only where that rarity is eligible.

**Effect:** The first time each turn you **Exhaust a non-Junk card**, draw **1 card**.

## 11. Quiet Reader's Cord
**Rarity:** Uncommon · **Eligibility:** General  
**Can appear:** Standard Normal Relic pool; standard reward/Treasure/Shop/Event sources only where that rarity is eligible.

**Effect:** If you end your turn after playing **2 or fewer non-Junk cards**, draw **1 additional card** at the start of your next turn.

## 12. Archive Key
**Rarity:** Uncommon · **Eligibility:** Bureaucrat  
**Can appear:** Normal pool while playing Bureaucrat; standard reward/Treasure/Shop/Event sources only where that rarity is eligible.

**Effect:** The first time each turn you **Archive a Junk card**, gain **5 Block** and draw **1 card**.

## 13. Index Volvelle
**Rarity:** Rare · **Eligibility:** General  
**Can appear:** Standard Normal Relic pool; standard reward/Treasure/Shop/Event sources only where that rarity is eligible.

**Effect:** Before drawing your opening hand, reveal **4 random non-Junk cards** from your draw pile. Choose 1 to place on top. The chosen card costs **1 less Energy the first time it is played that combat**, minimum 0.

## 14. Withheld Hourglass
**Rarity:** Rare · **Eligibility:** General  
**Can appear:** Standard Normal Relic pool; standard reward/Treasure/Shop/Event sources only where that rarity is eligible.

**Effect:** **Once per combat**, after a non-Junk card resolves and would normally enter Discard, you may place it beneath the Hourglass. At the start of your next turn return it to hand; it costs **0** that turn and gains **Exhaust**. Naturally Exhausting cards and temporary copies cannot be stored.

## 15. Road-Claim Token
**Rarity:** Rare · **Eligibility:** General  
**Can appear:** Standard Normal Relic pool; standard reward/Treasure/Shop/Event sources only where that rarity is eligible.

**Effect:** When picked up, upgrade **1 card**. After every Elite victory, heal **5 HP**; if already at full HP, gain **20 Gold** instead.

## 16. Concordance Medallion
**Rarity:** Rare · **Eligibility:** Bureaucrat  
**Can appear:** Normal pool while playing Bureaucrat; standard reward/Treasure/Shop/Event sources only where that rarity is eligible.

**Effect:** The first time each turn a **single-target card** directly applies Paperwork and/or Doubt, apply half of each directly applied amount to every other living enemy, rounded down; if rounding would produce 0 but the status was applied, apply 1. If no second enemy exists, gain **5 Block** instead. Relic propagation cannot recursively propagate.

## 17. Chancery Ribbon
**Rarity:** Rare · **Eligibility:** Bureaucrat  
**Can appear:** Normal pool while playing Bureaucrat; standard reward/Treasure/Shop/Event sources only where that rarity is eligible.

**Effect:** The first **Form** you play each turn costs **1 less Energy**. When it resolves, Paperwork and Doubt directly applied by that Form are increased by **50%**, rounded up. If it directly applies neither, draw **1 card**.

## 18. Moss Salve
**Rarity:** Common · **Eligibility:** General  
**Can appear:** Standard Normal Relic pool; standard reward/Treasure/Shop/Event sources only where that rarity is eligible.

**Effect:** After winning a combat in which you lost HP, heal **up to 2 HP**, but never more HP than you lost in that combat.

## 19. Lead Counterweight
**Rarity:** Common · **Eligibility:** General  
**Can appear:** Standard Normal Relic pool; standard reward/Treasure/Shop/Event sources only where that rarity is eligible.

**Effect:** The first time each turn you play a non-Junk card with **base cost 2+**, gain **4 Block**.

## 20. Hollow Wax Bead
**Rarity:** Common · **Eligibility:** General  
**Can appear:** Standard Normal Relic pool; standard reward/Treasure/Shop/Event sources only where that rarity is eligible.

**Effect:** Every **third 0-cost non-Junk card** played during combat draws **1 card**. Counter persists across turns and resets after combat; can trigger at most once per turn.

## 21. Binder's Awl
**Rarity:** Common · **Eligibility:** General  
**Can appear:** Standard Normal Relic pool; standard reward/Treasure/Shop/Event sources only where that rarity is eligible.

**Effect:** The first time your draw pile is shuffled each combat, gain **1 Energy** and draw **1 card**.

## 22. Carved Bone Buckle
**Rarity:** Common · **Eligibility:** General  
**Can appear:** Standard Normal Relic pool; standard reward/Treasure/Shop/Event sources only where that rarity is eligible.

**Effect:** When picked up, gain **4 Max HP** and heal **4 HP**.

## 23. Petitioner's Token
**Rarity:** Common · **Eligibility:** Bureaucrat  
**Can appear:** Normal pool while playing Bureaucrat; standard reward/Treasure/Shop/Event sources only where that rarity is eligible.

**Effect:** The first time each combat a **Queued card successfully resolves**, gain **1 Energy** and draw **1 card**.

## 24. Redaction Knife
**Rarity:** Uncommon · **Eligibility:** General  
**Can appear:** Standard Normal Relic pool; standard reward/Treasure/Shop/Event sources only where that rarity is eligible.

**Effect:** **Once per turn**, after your normal draw, you may discard **1 card**. If you do, draw **1 card**.

## 25. Alms Basin
**Rarity:** Uncommon · **Eligibility:** General  
**Can appear:** Standard Normal Relic pool; standard reward/Treasure/Shop/Event sources only where that rarity is eligible.

**Effect:** During each Shop visit, the first time Gold spent after entering reaches **75**, heal **8 HP**. Once per Shop visit.

## 26. Index Bone
**Rarity:** Uncommon · **Eligibility:** General  
**Can appear:** Standard Normal Relic pool; standard reward/Treasure/Shop/Event sources only where that rarity is eligible.

**Effect:** At the start of your turn, look at the top **2 cards** of your draw pile and return them in either order. If fewer remain, inspect all remaining cards.

## 27. Refusal Rosary
**Rarity:** Uncommon · **Eligibility:** General  
**Can appear:** Standard Normal Relic pool; standard reward/Treasure/Shop/Event sources only where that rarity is eligible.

**Effect:** Whenever you leave a normal card reward without taking a card, gain **10 Gold** and heal **1 HP**.

## 28. Archive Censer
**Rarity:** Uncommon · **Eligibility:** Bureaucrat  
**Can appear:** Normal pool while playing Bureaucrat; standard reward/Treasure/Shop/Event sources only where that rarity is eligible.

**Effect:** The first time each turn you **Archive a card**, gain **1 Energy**.

## 29. Seal-Maker's Die
**Rarity:** Uncommon · **Eligibility:** Bureaucrat  
**Can appear:** Normal pool while playing Bureaucrat; standard reward/Treasure/Shop/Event sources only where that rarity is eligible.

**Effect:** The first time each turn you **Ratify**, draw **1 card** and gain **5 Block**.

## 30. Iron Astrolabe
**Rarity:** Rare · **Eligibility:** General  
**Can appear:** Standard Normal Relic pool; standard reward/Treasure/Shop/Event sources only where that rarity is eligible.

**Effect:** The first time each turn you draw a non-Junk card with **base cost 2+**, gain **1 Energy**.

## 31. Twin-Ember Brazier
**Rarity:** Rare · **Eligibility:** General  
**Can appear:** Standard Normal Relic pool; standard reward/Treasure/Shop/Event sources only where that rarity is eligible.

**Effect:** Whenever you **Rest** at a Campfire, upgrade **1 random unupgraded card**. Whenever you **Smith**, heal **7 HP**.

## 32. Gilded Tithe Chain
**Rarity:** Rare · **Eligibility:** General  
**Can appear:** Standard Normal Relic pool; standard reward/Treasure/Shop/Event sources only where that rarity is eligible.

**Effect:** When picked up, gain **4 Max HP** and heal 4. After acquisition, for every **100 actual Gold spent in Shops**, gain **2 Max HP** and heal 2. Excess spending carries forward.

## 33. Rebinding Spindle
**Rarity:** Rare · **Eligibility:** General  
**Can appear:** Standard Normal Relic pool; standard reward/Treasure/Shop/Event sources only where that rarity is eligible.

**Effect:** The first time your draw pile would shuffle each combat, choose up to **2 non-Junk cards** from Discard, set them aside, shuffle the rest, then place the chosen cards on top in any order. The first time each chosen card enters hand after that shuffle, it costs **1 less Energy that turn**.

## 34. Deferred Signet
**Rarity:** Rare · **Eligibility:** Bureaucrat  
**Can appear:** Normal pool while playing Bureaucrat; standard reward/Treasure/Shop/Event sources only where that rarity is eligible.

**Effect:** The first card you **Queue** each turn costs **1 less Energy to Queue**, minimum 0. When it later resolves, apply **1 Seal** to its living enemy target; if it has none, apply 1 Seal to the living enemy with the highest HP.

## 35. Iron Prayer Bead
**Rarity:** Common · **Eligibility:** General  
**Can appear:** Standard Normal Relic pool; standard reward/Treasure/Shop/Event sources only where that rarity is eligible.

**Effect:** The first **Deed** you play each turn targeting an enemy currently intending to **Attack** deals **4 additional total damage**.

## 36. Black Salt Charm
**Rarity:** Common · **Eligibility:** General  
**Can appear:** Standard Normal Relic pool; standard reward/Treasure/Shop/Event sources only where that rarity is eligible.

**Effect:** At combat start gain **4 Block**. If you begin below **50% Max HP**, gain **8 Block instead** and gain **1 Energy on turn 1**.

## 37. Tarnished Bell
**Rarity:** Common · **Eligibility:** General  
**Can appear:** Standard Normal Relic pool; standard reward/Treasure/Shop/Event sources only where that rarity is eligible.

**Effect:** The first time each turn you apply a negative status to an enemy, deal **4 damage** to that enemy.

## 38. Grave Coin
**Rarity:** Common · **Eligibility:** General  
**Can appear:** Standard Normal Relic pool; standard reward/Treasure/Shop/Event sources only where that rarity is eligible.

**Effect:** Whenever an enemy dies while affected by at least one negative status, gain **4 Gold**, maximum **12 Gold per combat**.

## 39. Bruise Cup
**Rarity:** Common · **Eligibility:** General  
**Can appear:** Standard Normal Relic pool; standard reward/Treasure/Shop/Event sources only where that rarity is eligible.

**Effect:** The first time each turn an enemy causes you to lose HP, gain **4 Block immediately**.

## 40. Votive Candle
**Rarity:** Common · **Eligibility:** Bureaucrat  
**Can appear:** Normal pool while playing Bureaucrat; standard reward/Treasure/Shop/Event sources only where that rarity is eligible.

**Effect:** The first **Rite** you play each combat costs **1 less Energy**, minimum 0, and grants **3 Block** when played.

## 41. Blood-Price Token
**Rarity:** Uncommon · **Eligibility:** General  
**Can appear:** Standard Normal Relic pool; standard reward/Treasure/Shop/Event sources only where that rarity is eligible.

**Effect:** **Once per turn** as a free relic action, lose **3 HP**; your next non-Junk card this turn costs **1 less Energy**, minimum 0. Cannot reduce you below 1 HP.

## 42. Blackthorn Brooch
**Rarity:** Uncommon · **Eligibility:** General  
**Can appear:** Standard Normal Relic pool; standard reward/Treasure/Shop/Event sources only where that rarity is eligible.

**Effect:** The first time each turn a single card grants at least **10 Block**, deal **6 damage to all enemies**.

## 43. Executioner's Measure
**Rarity:** Uncommon · **Eligibility:** General  
**Can appear:** Standard Normal Relic pool; standard reward/Treasure/Shop/Event sources only where that rarity is eligible.

**Effect:** When you kill a non-summoned enemy with direct card damage, record overkill up to **15 Excess**. At the start of the next combat, your first damaging card deals additional damage equal to stored Excess, then reset.

## 44. Sootglass Lens
**Rarity:** Uncommon · **Eligibility:** General  
**Can appear:** Standard Normal Relic pool; standard reward/Treasure/Shop/Event sources only where that rarity is eligible.

**Effect:** The first time each turn you apply a negative status to an enemy that was already affected by any negative status, draw **1 card**.

## 45. Rubric Tablet
**Rarity:** Uncommon · **Eligibility:** Bureaucrat  
**Can appear:** Normal pool while playing Bureaucrat; standard reward/Treasure/Shop/Event sources only where that rarity is eligible.

**Effect:** The first time each turn you play a **Rite**, the next non-Rite non-Junk card that turn costs **1 less Energy**, minimum 0.

## 46. Refuse Docket
**Rarity:** Uncommon · **Eligibility:** Bureaucrat  
**Can appear:** Normal pool while playing Bureaucrat; standard reward/Treasure/Shop/Event sources only where that rarity is eligible.

**Effect:** The first time each turn a **Junk card enters your hand**, you may immediately Archive it. If you do, choose an enemy and apply **1 Seal**.

## 47. Blood-Stamped Bond
**Rarity:** Rare · **Eligibility:** General  
**Can appear:** Standard Normal Relic pool; standard reward/Treasure/Shop/Event sources only where that rarity is eligible.

**Effect:** At combat start you may sign: lose **6 HP**; on turns 1–3 gain **+1 Energy and +1 Draw**. If you decline, gain **10 Block on turn 1** instead. Signing cannot reduce you below 1 HP.

## 48. Thorn-Crowned Reliquary
**Rarity:** Rare · **Eligibility:** General  
**Can appear:** Standard Normal Relic pool; standard reward/Treasure/Shop/Event sources only where that rarity is eligible.

**Effect:** Whenever a card grants Block, deal damage to the enemy with the highest current HP equal to **25% of Block gained**, rounded down, max **10 per card** and **20 total per turn**. Cannot trigger itself.

## 49. Blank Folio
**Rarity:** Rare · **Eligibility:** General  
**Can appear:** Standard Normal Relic pool; standard reward/Treasure/Shop/Event sources only where that rarity is eligible.

**Effect:** When picked up, remove **1 card** from your deck. Every **third normal card reward skipped** after acquisition removes **1 additional card**, then resets the counter.

## 50. Chancery Scale
**Rarity:** Rare · **Eligibility:** Bureaucrat  
**Can appear:** Normal pool while playing Bureaucrat; standard reward/Treasure/Shop/Event sources only where that rarity is eligible.

**Effect:** The first time each turn you apply Paperwork to an enemy already at **5+ Paperwork**, gain **1 Energy** and draw **1 card**. The first card each turn targeting an enemy at **8+ Paperwork** costs **1 less Energy**, minimum 0.

---
# 4. Shop-Exclusive Relics — 24

**Distribution:** 18 General · 6 Bureaucrat-specific.

## 1. Pawnbroker's Loupe
**Eligibility:** General  
**Can appear:** Regular Shops; Licensed Vendor; Conceptual Toll; Travelling Chandler.

**Effect:** Whenever a normal card reward is generated, one random card is **Appraised**. Take it → gain **12 Gold**. Skip the entire reward → gain **6 Gold**.

## 2. Copper Receipt Roll
**Eligibility:** General  
**Can appear:** Shop Relic pool.

**Effect:** After every **third purchase** made in Shops after acquisition, gain **35 Gold**. Counter persists between Shops; card removal counts as a purchase.

## 3. Secondhand Reliquary
**Eligibility:** General  
**Can appear:** Shop Relic pool.

**Effect:** The first time each Act you enter a Shop, one Normal Relic for sale is marked **Secondhand** and costs **30% less**. Purchasing it causes **5 HP loss**.

## 4. Bounty Hook
**Eligibility:** General  
**Can appear:** Shop Relic pool.

**Effect:** After defeating an Elite, gain **20 additional Gold**. If you finish that Elite combat below 50% Max HP, gain **35 instead**.

## 5. Witchmarket Purse
**Eligibility:** General  
**Can appear:** Shop Relic pool.

**Effect:** At the end of each Act, for every full **100 Gold** owned, gain **20 Gold**, maximum **60 Gold**.

## 6. Bent Auction Gavel
**Eligibility:** General  
**Can appear:** Shop Relic pool.

**Effect:** Whenever you would receive a **random Normal Relic** as a combat, Treasure or standard relic reward, you may reject it before acquisition and gain **65 Gold** instead. Boss/Event/purchased/guaranteed named rewards are excluded.

## 7. Wastebroker's Permit
**Eligibility:** Bureaucrat  
**Can appear:** Shop Relic pool while playing Bureaucrat.

**Effect:** Whenever you **Archive a Junk card**, record 1 Salvage, max **3 per combat**. After victory gain **5 Gold per Salvage**.

## 8. Filing-Fee Stamp
**Eligibility:** Bureaucrat  
**Can appear:** Shop Relic pool while playing Bureaucrat.

**Effect:** At combat end, each enemy that died with **5+ Paperwork** grants 6 Gold; if it died with **10+**, grant 4 additional Gold. Maximum **20 Gold per combat**.

## 9. Scrivener's Shears
**Eligibility:** General  
**Can appear:** Shop Relic pool.

**Effect:** The first card removal purchased in each Act costs **50% less**. After using that discount, the next card removal that Act costs **25% more** than normal.

## 10. Apprentice's Whetstone
**Eligibility:** General  
**Can appear:** Shop Relic pool.

**Effect:** Whenever you purchase a card, you may pay **20 additional Gold** to upgrade it immediately.

## 11. Backroom Kettle
**Eligibility:** General  
**Can appear:** Shop Relic pool.

**Effect:** **Once per Shop visit**, pay **25 Gold** to heal **8 HP**. Does not occupy inventory; usable in the Shop where it is bought.

## 12. Crooked Display Case
**Eligibility:** General  
**Can appear:** Shop Relic pool.

**Effect:** When purchased, immediately add **1 additional Normal Relic** to the current Shop. Every future Shop also offers one additional Normal Relic. That extra relic costs **20% more**.

## 13. Turnover Bell
**Eligibility:** General  
**Can appear:** Shop Relic pool.

**Effect:** **Once per Shop visit**, pay **30 Gold** to replace all unsold cards with new cards. Relics/services are unaffected. Usable in the Shop where bought.

## 14. Debtor's Signet
**Eligibility:** General  
**Can appear:** Shop Relic pool.

**Effect:** You may buy items you cannot fully afford. Spend all available Gold and add the remainder as **Debt**, max 100. While in Debt, **50% of Gold gained**, rounded up, repays Debt. Debt is not negative Gold, not Gold spent, and cannot be spent.

## 15. Notary's Waiver
**Eligibility:** Bureaucrat  
**Can appear:** Shop Relic pool while playing Bureaucrat.

**Effect:** Whenever you **Ratify**, gain 1 Waiver, max 4. When purchasing a normal card removal, each Waiver reduces the price by **10 Gold**; all stored Waivers are consumed.

## 16. Priority Window Pass
**Eligibility:** Bureaucrat  
**Can appear:** Shop Relic pool while playing Bureaucrat.

**Effect:** The first Form or Queue card shown in each Shop receives an additional **10% discount**. Whenever you purchase a Form or Queue card, refund **up to 15 Gold actually paid**, subject to the global refund cap.

## 17. Twin-Lock Chest Key
**Eligibility:** General  
**Can appear:** Shop Relic pool.

**Effect:** Whenever a Treasure node would grant a Normal Relic, reveal **2 eligible Normal Relics** instead and choose 1.

## 18. Appraiser's Chalk
**Eligibility:** General  
**Can appear:** Shop Relic pool.

**Effect:** Whenever a normal card reward is generated, one random eligible unupgraded card in it is offered **upgraded**.

## 19. Guest-Favor Token
**Eligibility:** General  
**Can appear:** Shop Relic pool.

**Effect:** The first time each Act you complete an Event **without entering combat**, after resolving it choose: gain **25 Gold**, or receive a special **2-card normal reward** and take up to 1.

## 20. Merchant Punchcard
**Eligibility:** General  
**Can appear:** Shop Relic pool.

**Effect:** Whenever you enter a future Shop, gain **1 Punch**, max 3. Before the first purchase in a Shop, redeem any number; each reduces that purchase by **20 Gold**.

## 21. Warranty Tag
**Eligibility:** General  
**Can appear:** Shop Relic pool.

**Effect:** The first eligible Relic purchased each Act may be marked Under Warranty. At the next Shop, before another purchase, you may return it for **50% of Gold actually paid**. If you leave without returning it, warranty expires. Relics with immediate one-time acquisition effects cannot be warranted.

## 22. Indemnity Stamp
**Eligibility:** General  
**Can appear:** Shop Relic pool.

**Effect:** When picked up, gain **20 Gold**. Whenever you lose Gold outside a Shop, after the node resolves recover **50%**, max **50 Gold per node**. Voluntary spending does not count.

## 23. Archive Voucher Roll
**Eligibility:** Bureaucrat  
**Can appear:** Shop Relic pool while playing Bureaucrat.

**Effect:** After winning a combat in which you **Archived at least 2 cards**, gain **1 Archive Voucher**, max 5. Each Voucher is **10 Gold Shop Credit**; Vouchers persist, are not Gold, cannot be lost and do not count as Gold spent.

## 24. Departmental Purchase Order
**Eligibility:** Bureaucrat  
**Can appear:** Shop Relic pool while playing Bureaucrat.

**Effect:** Each Act separately track purchased **Deed, Working, Rite**. The first purchase of each type that Act refunds **up to 15 Gold actually paid**. Categories reset next Act.

---
# 5. Event-Exclusive Relics — 25

## Originality Stamp
**Act:** I  
**Only source:** The Certified Copy Drawer — Take the certified instrument.

**Effect:** Once per combat, first played non-Junk card with another persistent same-name copy creates a temporary copy in hand; copy costs 1 less and Exhausts.

## Unclaimed Property Tag
**Act:** I  
**Only source:** The Lost-and-Found Desk — Claim an unlabelled parcel.

**Effect:** Combat start mark random non-Junk card; first time it enters hand it costs 1 less that turn.

## Uncalled Ticket
**Act:** I  
**Only source:** The Waiting Token Exchange — Exchange three hours of waiting.

**Effect:** Once per combat, end turn with unaffordable non-Junk card: place it on top of draw pile; next turn gain 1 Energy and draw +1.

## Threshold Ward
**Act:** I  
**Only source:** The Sealed Back Door — Respect the seal.

**Effect:** Start combat with 6 Block. First enemy positive-status gain each combat gives 1 Energy + 6 Block.

## Crossed-Out Map
**Act:** I  
**Only source:** The Contradictory Map — Fold the map incorrectly.

**Effect:** Once per Act ignore path connections to a legal node in the next row; cannot bypass gates/locks/scripted restrictions.

## Inherited Bone Folder
**Act:** I  
**Only source:** The Archive Window — Take the old tool.

**Effect:** Combat start mark random unupgraded non-Junk card; first time drawn that combat temporarily upgrade and cost −1 that turn. If none eligible, draw +1 on turn 1.

## Unreturned Library Card
**Act:** II  
**Only source:** The Perpetual Borrower — Pocket the library card.

**Effect:** Once per combat, first non-Junk card entering discard unplayed returns next turn, costs 0 that turn and Exhausts on play.

## Reversible Shelf Label
**Act:** II  
**Only source:** The Reciprocal Shelf — Take the loose shelf label.

**Effect:** Once per combat, remember first non-Junk card name moved from hand unplayed; next same-name card entering hand draws 1 and costs 1 less that turn.

## Blank Cameo
**Act:** II  
**Only source:** The Redacted Portrait — Restore the missing face.

**Effect:** After opening draw choose a non-Junk card: Retain, cost −1, protected from specific enemy card targeting/markers until played; mandatory-only-target effects can ignore protection.

## Vow Bead
**Act:** II  
**Only source:** The Last Quiet Table — complete Vow challenge.

**Effect:** At turn start optionally cap yourself at 3 non-Junk cards; playing exactly 3 grants next turn +1 Energy and +1 Draw.

## Inverted Sealstone
**Act:** II  
**Only source:** The Inward Seal — Break the seal outward.

**Effect:** After opening draw choose a Deed/Working; first play returns the exact card to hand after resolution instead of normal post-play destination; second play is normal.

## Mootcap
**Act:** III  
**Only source:** Moonlit Mushrooms — Step inside the circle.

**Effect:** First third non-Junk card each turn lets you choose 10 Block, draw 1, or 7 AoE damage.

## Dissenting Spore
**Act:** III  
**Only source:** Moonlit Mushrooms — complete quorum challenge.

**Effect:** Odd turn card count +1 Spore, even −1; at 3 next turn consume for +1 Energy, +1 Draw, +6 Block.

## Antway Marker
**Act:** III  
**Only source:** The Ant Queue — complete Single File Journey.

**Effect:** First 3 non-Junk cards in non-decreasing base-cost order → after third gain 1 Energy and draw 1; once/turn.

## Complaint Leaf
**Act:** III  
**Only source:** The Ombudsman's Warning — Keep the leaf.

**Effect:** First enemy each combat to cause HP loss or apply a negative status becomes Respondent; first targeted non-Junk card each turn against Respondent costs 1 less.

## Guest-Right Brooch
**Act:** III  
**Only source:** The Kindly Procession — Walk/Follow.

**Effect:** Once per Event reduce one immediate Gold/current-HP/Max-HP option cost by 25%; excludes Shops/combat/delayed/status/non-numeric/card-sacrifice costs.

## Cup of the Lowest Mark
**Act:** IV  
**Only source:** The Dry Nilometer — Accept the True Level.

**Effect:** First time each combat you end a turn with exactly 1 Energy: heal 4 and draw +1 next turn.

## Red Linen Knot
**Act:** IV  
**Only source:** The Red Linen Procession — Follow Until the Last Gate.

**Effect:** Start combat with 8 Block. Once/combat prevent 1 stack or 1 duration-turn of natural positive-status decay; gain 8 Block.

## Blank Cartouche
**Act:** IV  
**Only source:** The Nameless Cartouche — Take the Fragment.

**Effect:** Draw +1 on turn 1. First time each combat you gain Inscribed, remove 1 Inscribed.

## Jar of Borrowed Breath
**Act:** IV  
**Only source:** The Four Canopic Jars — Jar of Breath.

**Effect:** First time each combat a temporary negative status leaves you completely: draw 1 and heal 3.

## Broken Royal Weight
**Act:** IV  
**Only source:** The Chamber of False Measures — Break the Scale.

**Effect:** Start combat with 10 Block. Once/combat failed Weighed direct HP loss is prevented and replaced with Burdened 1.

## Petition Chisel
**Act:** IV  
**Only source:** The Wall of Old Complaints — Read Them All.

**Effect:** Each enemy action applying one or more negative statuses = 1 Grievance, max 3. At 3 next turn consume: draw 2, +1 Energy, remove 1 negative-status stack.

## Tablet of the Missing Name
**Act:** IV  
**Only source:** The Unnamed Throne — Restore the Name.

**Effect:** Start combat with Nameless Authority. First positive-status gain is increased by 50%, rounded up, min +1 stack; if Inscribed, remove 1 after amplification.

## Funerary Linen Coil
**Act:** IV  
**Only source:** The Unfinished Burial — Finish the Wrapping.

**Effect:** Once/combat first non-Junk card deliberately Exhausted/Archived/player-Banished without normal play: heal 4 and draw 1.

## Mercy Counterweight
**Act:** IV  
**Only source:** The Merciful Balance — Place Your Burden on the Pan.

**Effect:** First negative-status application each combat: choose reduce by 1 stack, or accept and gain next-turn +1 Energy +1 Draw.

---
# 6. Boss Relics — 69

**Acquisition:** after defeating the associated Act I–IV Boss, receive **1 of that Boss's 3 relics at random**. No choice screen. Character-independent. Never enters another relic pool.


# Act I Boss Relics


## The Deputy Undersecretary

### Unfinished Docket
**Only source:** defeat **The Deputy Undersecretary**; 1-in-3 associated random Boss Relic result.

**Effect:** At end of turn store up to 1 unspent Energy; gain stored Energy next turn.

### Red-Ribboned Matter
**Only source:** defeat **The Deputy Undersecretary**; 1-in-3 associated random Boss Relic result.

**Effect:** At end of turn choose 1 non-Junk card to Retain; it costs 1 less next turn.

### Backlog Counterseal
**Only source:** defeat **The Deputy Undersecretary**; 1-in-3 associated random Boss Relic result.

**Effect:** At end of turn gain 4 Block per unplayed non-Junk card, max 8.


## The Queue Commissioner

### Brass Service Bell
**Only source:** defeat **The Queue Commissioner**; 1-in-3 associated random Boss Relic result.

**Effect:** At start of every third player turn: gain 1 Energy and draw 1.

### Priority Sash
**Only source:** defeat **The Queue Commissioner**; 1-in-3 associated random Boss Relic result.

**Effect:** First time each turn total player-caused HP damage reaches 15+: gain 8 Block.

### Ivory Number Disc
**Only source:** defeat **The Queue Commissioner**; 1-in-3 associated random Boss Relic result.

**Effect:** Enemy turn ends with no HP loss → advance. At 2, reset; next turn gain 1 Energy and draw 1.


## The Lord Sealkeeper

### Access Seal-Shard
**Only source:** defeat **The Lord Sealkeeper**; 1-in-3 associated random Boss Relic result.

**Effect:** At combat start gain 1 Energy and draw +1.

### Testimony Seal-Shard
**Only source:** defeat **The Lord Sealkeeper**; 1-in-3 associated random Boss Relic result.

**Effect:** At combat start gain 8 Block; prevent first negative-status application each combat.

### Execution Seal-Shard
**Only source:** defeat **The Lord Sealkeeper**; 1-in-3 associated random Boss Relic result.

**Effect:** First Attack/Deed-style damaging play each turn deals 4 additional total damage; multi-hit receives it once.


## The Municipal Dragon

### Stamped Expedition Writ
**Only source:** defeat **The Municipal Dragon**; 1-in-3 associated random Boss Relic result.

**Effect:** Once per combat, free action: gain 2 Energy this turn.

### Civic Entry Warrant
**Only source:** defeat **The Municipal Dragon**; 1-in-3 associated random Boss Relic result.

**Effect:** Once per combat, free action: gain 1 Energy; your Attacks/Deeds ignore enemy Block for the rest of the turn.

### Inspector's Brass Charter
**Only source:** defeat **The Municipal Dragon**; 1-in-3 associated random Boss Relic result.

**Effect:** At combat start gain 8 Block; enemies reveal their following intent in addition to current intent when possible.


## The Living Charter

### Continuance Fragment
**Only source:** defeat **The Living Charter**; 1-in-3 associated random Boss Relic result.

**Effect:** At end of turn retain up to 8 remaining Block for next turn.

### Right of Redress
**Only source:** defeat **The Living Charter**; 1-in-3 associated random Boss Relic result.

**Effect:** First time cumulative HP loss in a combat reaches 12: next turn gain 15 Block and draw 2.

### Margin of Appeal
**Only source:** defeat **The Living Charter**; 1-in-3 associated random Boss Relic result.

**Effect:** Once per combat after an enemy intent is revealed, replace it with another legal non-identical intent; mandatory phase/death/scripted actions excluded.


# Act II Boss Relics


## The Whispering Catalogue

### Errata Ribbon
**Only source:** defeat **The Whispering Catalogue**; 1-in-3 associated random Boss Relic result.

**Effect:** End turn classify Sparse (0–2 non-Junk) or Busy (3+). From turn 2: changed classification → next turn +1 Energy; same → next turn +6 Block.

### Index of Contradictions
**Only source:** defeat **The Whispering Catalogue**; 1-in-3 associated random Boss Relic result.

**Effect:** First time each turn a non-Junk card differs in type from previous non-Junk card: draw 1 and gain 3 Block.

### Registry Tab
**Only source:** defeat **The Whispering Catalogue**; 1-in-3 associated random Boss Relic result.

**Effect:** End turn 1 determine most-played non-Junk card type (choose on tie). From turn 2, first card of that Registered Type each turn costs 1 less.


## The Warden of Sealed Volumes

### Custody Shackle
**Only source:** defeat **The Warden of Sealed Volumes**; 1-in-3 associated random Boss Relic result.

**Effect:** End turn with ≤2 non-Junk cards played: Seal highest-base-cost non-Junk card remaining in hand instead of discarding. Next turn return it; cost 0 that turn.

### Master Release Key
**Only source:** defeat **The Warden of Sealed Volumes**; 1-in-3 associated random Boss Relic result.

**Effect:** After opening draw choose 1 non-Junk card and Seal it; turn 2 return it to hand costing 0 that turn.

### Release Tag
**Only source:** defeat **The Warden of Sealed Volumes**; 1-in-3 associated random Boss Relic result.

**Effect:** After normal draw mark random playable base-cost-1+ non-Junk as Evidence: cost −1 that turn and gain 4 Block when played. If none, mark another playable card for Block only.


## The Curator of Misplaced Hours

### Misdated Pocket Watch
**Only source:** defeat **The Curator of Misplaced Hours**; 1-in-3 associated random Boss Relic result.

**Effect:** Start turn based on previous turn: 0 non-Junk → 8 Block; 1–2 → +1 Energy; 3+ → draw 1. No turn-1 effect.

### Borrowed Minute
**Only source:** defeat **The Curator of Misplaced Hours**; 1-in-3 associated random Boss Relic result.

**Effect:** Once/turn with no outstanding debt: gain 1 Energy now. Next turn start with 1 less Energy, minimum 0, and gain 4 Block; debt then clears.

### Deferred Appointment Book
**Only source:** defeat **The Curator of Misplaced Hours**; 1-in-3 associated random Boss Relic result.

**Effect:** Turn 2 draw +2; Turn 3 gain 2 Energy; Turn 4 gain 15 Block.


## The Auditor of Returned Lives

### Identity Writ
**Only source:** defeat **The Auditor of Returned Lives**; 1-in-3 associated random Boss Relic result.

**Effect:** First time each turn you play a card name already played earlier this combat: draw 1. If not triggered that turn, gain 5 Block at end.

### Settled Ledger
**Only source:** defeat **The Auditor of Returned Lives**; 1-in-3 associated random Boss Relic result.

**Effect:** Track Energy actually spent on cards. Every 4 spent → gain 1 Energy; excess carries within combat.

### Closure Writ
**Only source:** defeat **The Auditor of Returned Lives**; 1-in-3 associated random Boss Relic result.

**Effect:** After winning combat heal 25% of HP lost in it, max 10 HP.


## The Grand Cross-Reference

### Premise Slip
**Only source:** defeat **The Grand Cross-Reference**; 1-in-3 associated random Boss Relic result.

**Effect:** First non-Junk card each turn is Premise. Next non-Junk: different type → costs 1 less; same type → gain 6 Block when played. Then Premise expires.

### Concordance Thread
**Only source:** defeat **The Grand Cross-Reference**; 1-in-3 associated random Boss Relic result.

**Effect:** After normal draw link random playable non-Junk hand card to next non-Junk draw-pile card. Playing linked hand card draws referenced card; it costs 1 less that turn. One link/turn.

### Conclusion Leaf
**Only source:** defeat **The Grand Cross-Reference**; 1-in-3 associated random Boss Relic result.

**Effect:** End turn record last non-Junk type. Next turn: Deed/Attack-type → first damaging Deed +8 total damage; Working/Skill-type → +8 Block; Rite/Other → draw +1. No card → no bonus.


# Act III Boss Relics


## The Ombudsman of Root and Road

### Boundary Tally
**Only source:** defeat **The Ombudsman of Root and Road**; 1-in-3 associated random Boss Relic result.

**Effect:** At combat start choose Road or Root; alternate each turn. Road: first non-Junk card costs 1 less. Root: start turn gain 10 Block.

### Counter-Petition Twine
**Only source:** defeat **The Ombudsman of Root and Road**; 1-in-3 associated random Boss Relic result.

**Effect:** Once/turn free action: discard 1 non-Junk card; draw 1 and gain 1 Energy.

### Signed Settlement
**Only source:** defeat **The Ombudsman of Root and Road**; 1-in-3 associated random Boss Relic result.

**Effect:** Start turn: if no HP lost previous enemy turn, gain 1 Energy and draw 1; otherwise gain 8 Block. No turn-1 effect.


## The Notary of Old Growth

### Countersealed Ring of Passage
**Only source:** defeat **The Notary of Old Growth**; 1-in-3 associated random Boss Relic result.

**Effect:** First non-Junk card each turn establishes base cost; next same-base-cost non-Junk card costs 0. If no match played, gain 5 Block at end.

### Countersealed Ring of Restraint
**Only source:** defeat **The Notary of Old Growth**; 1-in-3 associated random Boss Relic result.

**Effect:** After third non-Junk card in a turn, next non-Junk costs 0 and draws 1 on play. If turn ends with fewer than four played, Retain 1 chosen non-Junk and reduce its next-turn cost by 1.

### Countersealed Ring of Keeping
**Only source:** defeat **The Notary of Old Growth**; 1-in-3 associated random Boss Relic result.

**Effect:** End turn with no non-Junk cards in hand → next turn +1 Energy and +2 Draw; otherwise Retain 1 chosen non-Junk and reduce next-turn cost by 1.


## Grandmother Clause

### Honey Spoon
**Only source:** defeat **Grandmother Clause**; 1-in-3 associated random Boss Relic result.

**Effect:** Once/turn voluntarily gain 2 Energy this turn. Clause: end turn with at least 1 Energy; breach loses 6 HP.

### Better Chair Cushion
**Only source:** defeat **Grandmother Clause**; 1-in-3 associated random Boss Relic result.

**Effect:** Once/turn voluntarily gain 14 Block. Clause: end turn with at least 1 non-Junk card in hand; breach loses 6 HP.

### Last-Slice Tin
**Only source:** defeat **Grandmother Clause**; 1-in-3 associated random Boss Relic result.

**Effect:** Once/turn voluntarily draw 2. Clause: play no more than 4 non-Junk cards that turn; breach loses 6 HP.


## The Hill That Answers

### Surveyed Milestone
**Only source:** defeat **The Hill That Answers**; 1-in-3 associated random Boss Relic result.

**Effect:** Mark highest-Max-HP enemy as Landmark. First crossing of 75%, 50%, 25% HP each grants 1 Energy and draw 1; multiple crossed at once all resolve.

### Survey Cairn
**Only source:** defeat **The Hill That Answers**; 1-in-3 associated random Boss Relic result.

**Effect:** End turn with ≥12 Block: may bury 12. Next turn gain 1 Energy and draw 1. Once/turn.

### Loadstone Cairn
**Only source:** defeat **The Hill That Answers**; 1-in-3 associated random Boss Relic result.

**Effect:** Enemy-caused HP loss records Weight, max 12. Next turn gain Block equal Weight and first Deed/Attack gets +Weight total damage; then reset.


## The Queen Under the Hill

### Royal Grace Cup
**Only source:** defeat **The Queen Under the Hill**; 1-in-3 associated random Boss Relic result.

**Effect:** Start turn may accept one Grace: +1 Energy, draw 1, or +10 Block. Accepting causes all enemies to gain 6 Block.

### Hollow-Court Token
**Only source:** defeat **The Queen Under the Hill**; 1-in-3 associated random Boss Relic result.

**Effect:** First time each turn you spend last Energy by playing a card: +1 Favor, max 3. Start turn at 3: consume all, +1 Energy, +2 Draw, +8 Block.

### Silver Name-Tally
**Only source:** defeat **The Queen Under the Hill**; 1-in-3 associated random Boss Relic result.

**Effect:** Once/combat free action choose enemy: remove all its Block; its next Attack deals 10 less total damage; next card you play that turn costs 0.


# Act IV Boss Relics


## The Pharaoh of the Sealed Name

### Crown of the Three Names
**Only source:** defeat **The Pharaoh of the Sealed Name**; 1-in-3 associated random Boss Relic result.

**Effect:** At start of each turn gain 1 Energy.

### Edict of the Open Audience
**Only source:** defeat **The Pharaoh of the Sealed Name**; 1-in-3 associated random Boss Relic result.

**Effect:** Once/combat after drawing hand: all cards currently in hand cost 0 for the rest of that turn; later-drawn cards retain normal cost.

### Eternal Cartouche
**Only source:** defeat **The Pharaoh of the Sealed Name**; 1-in-3 associated random Boss Relic result.

**Effect:** Once after acquisition, if damage would reduce HP to 0: prevent it, set HP to 25% Max HP rounded up, remove all negative statuses, permanently destroy this relic.


## The Weigher of the Unspoken Heart

### Feather of Perfect Measure
**Only source:** defeat **The Weigher of the Unspoken Heart**; 1-in-3 associated random Boss Relic result.

**Effect:** First Deed/Attack or Working/Skill-equivalent card each turn costs 1 less. First later play of the opposite category draws 1 and gains 8 Block.

### Acquittal Scarab
**Only source:** defeat **The Weigher of the Unspoken Heart**; 1-in-3 associated random Boss Relic result.

**Effect:** Every third player turn remove all enemy Block; enemies take 30% increased player-caused HP damage for rest of turn. Upcoming judgment is shown one turn ahead.

### Balance of the Two Pans
**Only source:** defeat **The Weigher of the Unspoken Heart**; 1-in-3 associated random Boss Relic result.

**Effect:** End turn with equal number of offensive Deeds and defensive/manipulative Workings, at least one each: heal 2 and gain 1 Energy next turn; healing max 10/combat. Otherwise gain 12 Block.


## The Architect of the Impossible Pyramid

### Impossible Capstone
**Only source:** defeat **The Architect of the Impossible Pyramid**; 1-in-3 associated random Boss Relic result.

**Effect:** At end of turn retain 50% of remaining Block, rounded down, no cap.

### Pyramidion of Repetition
**Only source:** defeat **The Architect of the Impossible Pyramid**; 1-in-3 associated random Boss Relic result.

**Effect:** Every sixth Deed/Working-equivalent one-shot card counted as Attack/Skill is played twice; repeated play costs no extra Energy. Counter resets each combat.

### Crooked Plumb Line
**Only source:** defeat **The Architect of the Impossible Pyramid**; 1-in-3 associated random Boss Relic result.

**Effect:** First time each turn two consecutive non-Junk cards have different types, refund up to 2 Energy actually spent on second. If never triggered that turn, gain 10 Block at end.


## The Lady of the Black Granaries

### Black Granary Key
**Only source:** defeat **The Lady of the Black Granaries**; 1-in-3 associated random Boss Relic result.

**Effect:** Unspent Energy is retained between turns with no cap.

### Granary Reserve Seal
**Only source:** defeat **The Lady of the Black Granaries**; 1-in-3 associated random Boss Relic result.

**Effect:** After winning combat heal 15 HP, not above Max HP.

### Ration Seal
**Only source:** defeat **The Lady of the Black Granaries**; 1-in-3 associated random Boss Relic result.

**Effect:** Fourth non-Junk card each turn costs 0 and draws 1 after resolving. If fewer than four are played, gain 10 Block at end.


## The First Scribe of the House of Life

### Palimpsest Reed
**Only source:** defeat **The First Scribe of the House of Life**; 1-in-3 associated random Boss Relic result.

**Effect:** First Deed or Working each turn is Recorded. Next turn add a temporary copy to hand; it costs 0 that turn and Exhausts. Only one Recorded card at a time.

### Erasure Tablet
**Only source:** defeat **The First Scribe of the House of Life**; 1-in-3 associated random Boss Relic result.

**Effect:** Once/combat after enemy intent revealed: erase it; enemy does not perform it and gains 20 Block instead. Mandatory phase/death actions excluded.

### Correction Reed
**Only source:** defeat **The First Scribe of the House of Life**; 1-in-3 associated random Boss Relic result.

**Effect:** Once/turn after normal draw swap 1 non-Junk hand card with 1 non-Junk Discard card. Retrieved card costs 1 less that turn; if no eligible Discard card, draw 1 instead.


## The Mother of Natron and Resin

### Canopic Cabinet
**Only source:** defeat **The Mother of Natron and Resin**; 1-in-3 associated random Boss Relic result.

**Effect:** At combat start gain 12 Block. First application of each distinct negative status to you each combat is prevented.

### Resin Shroud
**Only source:** defeat **The Mother of Natron and Resin**; 1-in-3 associated random Boss Relic result.

**Effect:** First time each combat an enemy turn ends while you are below 50% Max HP: remove all negative statuses and gain 25 Block.

### Basin of Black Natron
**Only source:** defeat **The Mother of Natron and Resin**; 1-in-3 associated random Boss Relic result.

**Effect:** Start turn: if you have a negative status, remove 1 stack of one of your choice; otherwise gain 12 Block.


## The Vizier of the King's Mouth

### Triune Office Seal
**Only source:** defeat **The Vizier of the King's Mouth**; 1-in-3 associated random Boss Relic result.

**Effect:** Each turn: first Deed/Attack +8 total damage; draw +1; first Working/Skill gains 8 Block. All three offices active.

### Staff of the King's Mouth
**Only source:** defeat **The Vizier of the King's Mouth**; 1-in-3 associated random Boss Relic result.

**Effect:** First non-Junk card each turn refunds Energy actually spent after resolving, maximum refund 2.

### Vacant-Throne Decree
**Only source:** defeat **The Vizier of the King's Mouth**; 1-in-3 associated random Boss Relic result.

**Effect:** Once/combat free action: gain 3 Energy, draw 3, gain 20 Block.


## The Queen of the Flood Reckoning

### Sluice Gate of the Two Lands
**Only source:** defeat **The Queen of the Flood Reckoning**; 1-in-3 associated random Boss Relic result.

**Effect:** Once/turn free action choose: OPEN — lose 12 Block, gain 1 Energy; CLOSE — spend 1 Energy, gain 12 Block. Must fully pay cost.

### Flood-Reckoning Crown
**Only source:** defeat **The Queen of the Flood Reckoning**; 1-in-3 associated random Boss Relic result.

**Effect:** Start turn based on previous end: ended at 0 Energy → +1 Energy and +1 Draw; ended with 1+ → gain 1 Energy and 15 Block. Turn 1 gain 10 Block instead.

### Black Flood Vessel
**Only source:** defeat **The Queen of the Flood Reckoning**; 1-in-3 associated random Boss Relic result.

**Effect:** Once/combat after normal draw discard entire hand, then draw 7 and gain 2 Energy. Discards trigger normal Discard effects.

---
# 7. Source Index

## Event-only sources
See the Event Relic section above: each of the 25 entries names its exact Event and branch.

## Special Shop-like Event sources
The following Events can expose Shop Relics in their market stock:
- **The Licensed Vendor** — Act I
- **The Conceptual Toll** — Act III
- **The Travelling Chandler** — Act III

## Events that award random Normal Relics
Not exhaustive of future tuning, but the current finalized Events explicitly include:
- **The Witness Queue** — Act I: random eligible Normal Relic
- **The Black Granary** — Act IV: Common
- **The Forewritten Tablet** — Act IV: 60% Uncommon / 40% Rare after event combat
- **The Tomb Robbers' Fire** — Act IV: Uncommon via trade; Common after event combat
- **The Triple-Counted Donkey** — Act IV: Common
- **The Crocodile at the Weighing Place** — Act IV: Uncommon
- **The Copper Tithe** — Act IV: 50% Uncommon / 50% Rare
- **The Unfinished Burial** — Act IV: Uncommon

## No-relic zone
**Act V — The Divine Ledger**
- no Normal relic rewards
- no Shop relics
- no Event relics
- no Boss relic rewards
- no normal Events

---

# 8. Naming / Taxonomy Notes

- `Seal` in relic names is retained only where it represents a concrete seal-object and does not create unacceptable confusion with the Bureaucrat's enemy **Seal** mechanic.
- Player card text should prefer **Deed / Working / Rite / Junk**, not legacy Slay-the-Spire-style Attack/Skill/Power categories.
- Some Boss Relic effects were originally phrased with Attack/Skill shorthand during design; implementation should map those effects to the current BnB player-card taxonomy while preserving the stated mechanical role.
- `Archive` is a distinct Bureaucrat action and must not be silently treated as generic Exhaust.
- Queue cards count as played at Queue time; later resolution is a separate event.
- Boss Relics are forced random rewards, so all three associated relics must remain broadly usable.
- Event Relics may be more situational because the player explicitly chooses the branch that grants them.

# 9. Final Count

| Category | Count |
|---|---:|
| Normal | 50 |
| Shop | 24 |
| Event | 25 |
| Boss | 69 |
| **Total** | **168** |

This file is the single relic-reference master for acquisition, eligibility and mechanical identity.
