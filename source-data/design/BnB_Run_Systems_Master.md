# Bureaucrats and Broomsticks
## RUN SYSTEMS MASTER — Treasure, Shops, Campfires & Global Node Rules

**Status:** Canonical structural run-system specification  
**Scope:** Acts I–V  
**Balance status:** Structural rules locked; numerical economy values, Mimic percentages, Rest percentages and price curves remain subject to the dedicated balancing pass.

---

# 1. Purpose

This document defines the global rules for the recurring non-combat run systems that connect the finalized card, relic, event, enemy, Elite and Boss content.

It covers:

- Treasure nodes
- Mimic checks
- Standard Shops
- Campfires
- Relic-pool access through these systems
- Card-pool access through Shops
- Act V restrictions
- The boundary between fixed content rules and later numerical balancing

The exact **map-generation structure** is maintained separately in the Map Master.

The exact **economy values and curves** are intentionally not finalized here and will be handled in a dedicated global balancing pass after the complete Bureaucrat run is implemented.

---

# 2. Global Pool Terminology

## 2.1 General Cards

**General Cards** are the character-unspecific reward cards from the finalized General Card Pool.

- They follow their normal rarity rules.
- They follow their normal Act gates.
- They are available to every current and future playable character unless explicitly excluded.

For Shop composition, these are the **3 standard cards**.

## 2.2 Character Cards

**Character Cards** are reward cards belonging to the currently played character.

For the current Bureaucrat-only implementation:

- Character Cards come from the finalized Bureaucrat reward pool.
- Their normal rarity rules apply.
- Their normal Act gates apply.

For Shop composition, these are the **4 character-specific cards**.

Future characters will use the same Shop slot structure with their own character-specific pools.

## 2.3 Normal Relics

**Normal Relics** are the finalized global Common / Uncommon / Rare relic pool.

Current finalized pool:

- 50 Normal Relics
- global pool across Acts I–IV
- character eligibility still applies to character-specific Normal Relics

Normal Relics may appear through standard relic-reward sources such as Treasure and Normal Relic Shop slots.

## 2.4 Shop Relics

**Shop Relics** are the finalized Shop-exclusive relic pool.

Current finalized pool:

- 24 Shop Relics
- 18 General
- 6 Bureaucrat-specific

They do not enter the Normal Relic pool.

## 2.5 Event Relics

Event Relics remain tied to their explicitly defined Event branches.

They do not enter Treasure or normal Shop generation.

## 2.6 Boss Relics

Boss Relics remain exclusive to their associated Boss victories.

They do not enter Treasure or Shop generation.

---

# 3. Treasure Nodes

## 3.1 Opening a Treasure

When the player opens a Treasure node:

1. Perform the **Act-dependent Mimic check**.
2. If the Treasure is **not a Mimic**, resolve the standard Treasure reward.
3. If the Treasure **is a Mimic**, replace the normal Treasure opening with the appropriate Mimic encounter.

The Mimic check occurs before the normal Treasure reward is granted.

---

## 3.2 Mimic Chance

Treasure has an **Act-dependent Mimic chance**.

- Act I uses its own Mimic probability.
- Act II uses its own Mimic probability.
- Act III uses its own Mimic probability.
- Act IV uses its own Mimic probability.
- Exact percentages are part of the later balance pass.

The structural rule that the chance depends on the current Act is fixed.

Mimic encounter identities, combat tuning and any Mimic-specific victory rewards are handled by the relevant encounter specification rather than by this document.

---

## 3.3 Successful Treasure Reward

If the Treasure is not a Mimic, the player receives:

- **Gold**
- **1 random eligible Normal Relic**

The relic is drawn from the finalized **Normal Relic pool**.

Normal relic-generation rules apply:

- character eligibility is respected;
- Event Relics are excluded;
- Shop Relics are excluded;
- Boss Relics are excluded;
- duplicate restrictions follow the global relic rules.

The exact Gold amount is part of the later economy-balancing pass.

---

# 4. Standard Shops

## 4.1 Fixed Shop Inventory

Every regular Shop contains:

### Cards
- **3 General Cards**
- **4 Character Cards**

### Relics
- **2 Shop Relics**
- **2 Normal Relics**

This gives every regular Shop a fixed inventory of:

- **7 cards**
- **4 relics**

before any relic, event or future system explicitly modifies Shop generation.

---

## 4.2 Card Generation

### General Card Slots

The 3 General Card slots are generated from the finalized General Card Pool.

They obey:

- current Act gates;
- normal rarity generation rules;
- normal eligibility rules.

### Character Card Slots

The 4 Character Card slots are generated from the currently played character's reward pool.

For the Bureaucrat they obey:

- Bureaucrat Act gates;
- Bureaucrat rarity generation rules;
- all normal card eligibility restrictions.

Future characters replace only these 4 character-specific slots; the 3 General Card slots remain unchanged.

---

## 4.3 Relic Generation

### Shop Relic Slots

The 2 Shop Relic slots draw only from the finalized Shop-exclusive Relic pool.

Character-specific Shop Relics are only eligible for the current character.

### Normal Relic Slots

The 2 Normal Relic slots draw only from the finalized Normal Relic pool.

They do not draw Event or Boss Relics.

---

## 4.4 Card Removal

Regular Shops provide the standard **card-removal service**.

The exact:

- base removal price;
- price scaling;
- Act scaling, if any;
- interaction with the wider Gold curve

are part of the dedicated economy-balancing pass.

Existing relics that modify purchased card removal interact with this service according to their finalized relic text.

---

## 4.5 Shop Economy

The structural Shop inventory is content-final.

The following remain balance variables rather than content variables:

- card prices;
- relic prices;
- card-removal prices;
- rarity-based price differences;
- Act-dependent price curves;
- Gold income relative to Shop prices.

All finalized Shop Relic economy rules continue to apply, including discount ordering, refund limits, Shop Credit, Vouchers, Debt and Gold-spent tracking.

---

## 4.6 Special Event Markets

Explicit Shop-like Events may use different inventory sizes or modifiers as defined by their own Event text.

Currently finalized examples include:

- **The Licensed Vendor**
- **The Conceptual Toll**
- **The Travelling Chandler**

Their event-specific inventories override the normal Shop inventory for that Event only.

---

# 5. Campfires

## 5.1 Campfire Identity

Campfires currently offer exactly **two actions**:

1. **Take Authorized Leave**
2. **Submit an Amendment**

No additional Campfire actions are part of the initial content-complete Bureaucrat version.

Internally, these retain the global mechanical action names:

- **Rest**
- **Smith**

This preserves compatibility with relics and other effects that explicitly trigger from Rest or Smith.

---

# 6. Take Authorized Leave

**System action:** Rest

The player takes officially sanctioned leave and recovers HP.

## Effect

- Heal a percentage of **Max HP**.
- The percentage depends on the current Act.
- Later Acts heal a **smaller percentage** than earlier Acts.

Exact percentages are intentionally left for the balance pass.

The intended progression is therefore:

**Act I Rest > Act II Rest > Act III Rest > Act IV Rest**

in percentage of Max HP restored.

This represents the increasingly hostile run structure while preserving Rest as a meaningful recovery option.

---

# 7. Submit an Amendment

**System action:** Smith

The player formally submits a change to an existing document rather than physically forging or smithing an object.

## Effect

- Choose **1 eligible card** in the persistent deck.
- Upgrade that card.

Normal upgrade restrictions apply.

Cards that cannot be upgraded are not eligible targets.

The mechanical action remains **Smith** for trigger and implementation purposes.

---

# 8. Campfire Interaction Rules

Effects that refer to **Rest** trigger when the player chooses **Take Authorized Leave**.

Effects that refer to **Smith** trigger when the player chooses **Submit an Amendment**.

Example:

**Twin-Ember Brazier**
- Rest → upgrade 1 random unupgraded card.
- Smith → heal 7 HP.

The displayed flavor names therefore do not alter the mechanical event taxonomy.

---

# 9. Act V — The Divine Ledger

Act V is not a normal exploration Act.

It is the final Boss gauntlet.

Act V provides:

- no normal Treasure nodes;
- no regular Shops;
- no Campfires;
- no normal Events;
- no new card-reward tier;
- no new relic acquisition;
- no Boss Relic rewards.

The deck and relic loadout entering Act V are therefore the player's final constructed run state.

Act V tests the build assembled across Acts I–IV rather than offering another layer of progression.

---

# 10. Systems Explicitly Defined Elsewhere

## 10.1 Map Structure

The following belong to the separate **Map Master**:

- number of rows;
- node counts;
- branching structure;
- node placement;
- minimum / maximum node quotas;
- Duo / Trio encounter requirements;
- Elite placement;
- Treasure placement;
- Shop placement;
- Campfire placement;
- Event placement;
- Boss routing;
- path-generation safeguards.

This Run Systems Master defines what a node does once entered, not how the map generates it.

## 10.2 Encounter Content

Normal enemies, Duo / Trio encounters, Elites, Bosses and Mimic encounters are maintained in their respective finalized encounter masters.

## 10.3 Events

All Event branches, costs, temporary effects and Event Relic acquisition are maintained in:

`BnB_Final_Events_Master_PostAudit.md`

## 10.4 Relics

All relic identities, eligibility rules, acquisition pools and Shop-economy interaction rules are maintained in:

`BnB_Final_Relics_Master_PostAudit.md`

## 10.5 Cards

General Cards are maintained in:

`general_final_cards.md`

Bureaucrat Cards are maintained in:

`bureaucrat_final_cards.md`

---

# 11. Dedicated Balance Pass — Intentionally Deferred

The following values are **not missing content**. They are deliberately deferred balancing parameters:

### Treasure
- Mimic chance per Act
- Treasure Gold amount

### Shops
- card prices
- Normal Relic prices
- Shop Relic prices
- card-removal prices
- price scaling
- rarity price modifiers
- Act-based economy scaling

### Campfires
- Rest healing percentage per Act

### Global Run Economy
- combat Gold rewards
- Elite Gold rewards
- card reward rarity probabilities
- overall Gold income
- average number of affordable Shop purchases
- removal accessibility
- relic acquisition rate
- reward skip value
- economy/relic synergy ceilings

These should be tuned only after the complete Bureaucrat run is playable and measurable.

---

# 12. Current Structural Status

- [x] Normal enemy content finalized
- [x] Encounter compositions finalized
- [x] Elite content finalized
- [x] Boss content finalized
- [x] Event content finalized
- [x] Normal Relic pool finalized
- [x] Shop Relic pool finalized
- [x] Event Relic pool finalized
- [x] Boss Relic pools finalized
- [x] General Card pool finalized
- [x] Bureaucrat Card pool finalized
- [x] Treasure reward structure defined
- [x] Act-dependent Mimic system defined structurally
- [x] Standard Shop inventory defined
- [x] Campfire action set defined
- [x] Campfire flavor identity defined
- [x] Act V progression restrictions defined
- [ ] Map Master finalized separately
- [ ] Numerical economy balance pass
- [ ] Mimic percentages finalized
- [ ] Treasure Gold finalized
- [ ] Rest percentages finalized
- [ ] Shop prices finalized
- [ ] Full-run simulation and live playtesting

---

# 13. Canonical Run-System Summary

## Treasure
**Open Treasure → Act-dependent Mimic check.**

If no Mimic:

**Gold + 1 random eligible Normal Relic**

## Standard Shop
**3 General Cards**  
**4 Character Cards**  
**2 Shop Relics**  
**2 Normal Relics**  
**Card Removal service**

## Campfire
**Take Authorized Leave** — Rest; heal an Act-dependent percentage of Max HP. Healing percentage decreases in later Acts.

**Submit an Amendment** — Smith; upgrade 1 eligible card.

## Act V
**Boss gauntlet only. No normal progression nodes or new rewards.**

---

This file is the canonical structural reference for recurring run-node systems. Numerical values intentionally remain outside the content lock until the dedicated balancing phase.
