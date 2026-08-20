# General / Character-Unspecific — Final Card Pool

> **Status:** Final post-audit design specification for the character-unspecific reward pool. Structural design, naming, Act gates, status rules and upgrades are locked for implementation. Numerical values remain subject only to combat simulation and live balance passes.

## 1. Pool Summary

The general pool contains **50 character-unspecific reward cards**. It is deliberately smaller than a character pool and contains **no Commons**: general cards should bend or enrich a run rather than replace a character's basic identity.

| Rarity | Act I | Act II | Act III | Act IV | Total |
|---|---:|---:|---:|---:|---:|
| Uncommon | 16 | 6 | 5 | 4 | 31 |
| Rare | 3 | 4 | 5 | 7 | 19 |
| **New cards unlocked** | **19** | **10** | **10** | **11** | **50** |

| Minimum Act reached | Available general reward pool |
|---|---:|
| Act I | 19 |
| Act II | 29 |
| Act III | 39 |
| Act IV | 50 |

- **Act I:** flexible universal tools and readable introductions to the five general statuses.
- **Act II:** stronger status manipulation, copying, conversion and document/curse logic.
- **Act III:** meaningful engines, defensive commitments and larger status payoffs.
- **Act IV:** deliberately powerful endgame cards, finishers and rule-changing effects suitable for the late enemy, elite and boss pools.
- **Act V:** no new general reward cards. The completed Act-IV pool is the final deckbuilding state before the boss gauntlet.

## 2. Card-Type Taxonomy

The general pool uses the same BnB-wide card types as the Bureaucrat pool.

| Type | Definition |
|---|---|
| **Deed** | A one-shot offensive action. Direct damage is usually central. |
| **Working** | A one-shot defensive, manipulative, administrative or magical action. |
| **Rite** | A persistent combat effect that remains active and changes rules, engines or recurring behavior. |

Character-specific concepts remain tags or mechanics rather than general card types.

## 3. General Status Rules

### 3.1 Censure

**Censure X** is a context-sensitive status of prohibition.

- **On the player:** when a negative Status would be applied, prevent up to X stacks and reduce Censure by the number of stacks prevented.
- **On an enemy:** when a positive Status would be applied, prevent up to X stacks and reduce Censure by the number of stacks prevented.
- Censure cannot prevent an application of **Censure itself**.
- Censure does not prevent Block, Healing, Energy, summons, form/phase changes, intent-table changes, encounter rules or other non-Status mechanics.
- A Status explicitly paid as a **cost** cannot be prevented by Censure. This prevents Censure from deleting the price of future character cards that intentionally pay a negative Status as a cost.

### 3.2 Lien

**Lien X:** At the end of the holder's turn, remove up to X remaining Block. The holder loses the same amount of HP. Reduce Lien by the amount resolved.

- If the holder has no remaining Block, Lien does not decay.
- Lien HP loss is **HP loss**, not ordinary damage.
- A **Lien resolution** is one complete instance of this end-of-turn processing or an effect that explicitly says to resolve Lien immediately.

### 3.3 Citation

**Citation X:** After the holder resolves a **non-damaging action**, it loses X HP. Then remove 1 Citation.

- A damaging action is an action that resolves at least one direct-damage effect against an opposing combatant, even if Block absorbs that damage.
- An action containing Block, Buff, Debuff, Heal, Summon or other utility but no direct-damage effect is non-damaging.
- A mixed action that includes direct damage is treated as damaging and does not trigger Citation.
- One action causes at most one Citation trigger, regardless of how many non-damaging subeffects it contains.
- Status-based HP loss such as Paperwork is not direct damage from the action that originally applied that Status.

### 3.4 Blood Ink

**Blood Ink X:** Whenever another Status on the holder loses one or more stacks in a single Status-change event, the holder loses X HP. Then remove 1 Blood Ink.

- Losing 5 stacks of one Status in one event produces **one** Blood Ink trigger, not five.
- If several different Statuses lose stacks in separate Status-change events, each event may trigger Blood Ink.
- Blood Ink never triggers from the loss of its own stacks.
- When another Status loses stacks, Blood Ink's intrinsic trigger resolves before Rites that react to that Status loss unless a card explicitly says otherwise.
- Blood Ink HP loss is HP loss, not ordinary damage.

### 3.5 Ward Wax

**Ward Wax X:** At the start of your turn, gain X Block.

After the enemy turn:

- if you took **no unblocked Attack damage**, lose 1 Ward Wax;
- if you took **any unblocked Attack damage**, lose 2 Ward Wax.

Additional rules:

- The accelerated loss happens only once per enemy turn, regardless of number of hits or attackers.
- Ward Wax is primarily a player-positive Status. Enemies receive it only when content explicitly says so.
- Damage prevention from effects such as **Wax Indemnity** occurs before determining whether unblocked Attack damage was actually taken for Ward-Wax decay.

## 4. General Status Design Roles

| Status | Primary role |
|---|---|
| **Censure** | Deny hostile debuffs on the player or beneficial buffs on enemies. |
| **Lien** | Turn unused Block into an outstanding claim and HP loss. |
| **Citation** | Punish non-damaging actions and utility-heavy enemies. |
| **Blood Ink** | Make Status decay, consumption and removal dangerous. |
| **Ward Wax** | Multi-turn defense whose durability depends on whether protection is breached. |

These are horizontal, character-unspecific tools. They must not become mandatory archetypes or replace character-specific mechanics.

## 5. Uncommon Cards — 31

| # | Card | Act | Type | Cost | Base | Upgrade | Tags |
|---:|---|:---:|---|---:|---|---|---|
| 1 | **Malediction Review** | I | Working | 1 | Gain 6 Block. Choose one: gain 2 Censure; or apply 2 Censure to an enemy. | Gain 8 Block. | Block, Censure, Choice |
| 2 | **Grave Lien** | I | Deed | 1 | Deal 7 damage. Apply 5 Lien. | Deal 9 damage. Apply 6 Lien. | Damage, Lien |
| 3 | **Witchmark Citation** | I | Working | 1 | Apply 3 Citation. If the target currently intends a non-damaging action, draw 1 card. | Apply 4 Citation. | Citation, Intent, Draw |
| 4 | **Blood Marginalia** | I | Working | 1 | Apply 3 Citation and 2 Blood Ink. | Apply 3 Citation and 3 Blood Ink. | Citation, Blood Ink |
| 5 | **Waxen Surety** | I | Working | 1 | Gain 4 Ward Wax. | Gain 5 Ward Wax. | Ward Wax |
| 6 | **Blacklisted** | II | Working | 1 | Apply 2 Censure. For each different positive Status already on the target, apply 1 additional Censure, maximum +3. | Apply 3 Censure initially; the conditional maximum remains +3. | Censure, Anti-Buff |
| 7 | **Foreclosure** | I | Deed | 1 | Deal 6 damage. Then immediately resolve up to 5 Lien on the target. | Deal 8 damage. | Damage, Lien, Conversion |
| 8 | **Contempt Finding** | I | Working | 1 | Remove all Citation from an enemy. Gain 2 Block per Citation removed. | Gain 3 Block per Citation removed. | Citation, Block, Conversion |
| 9 | **Sanguine Errata** | II | Working | 1 | Apply 2 Blood Ink. Then choose and remove 1 stack of another Status from the target. | Apply 3 Blood Ink. | Blood Ink, Status Manipulation |
| 10 | **Tallow Reserve** | I | Working | 0 | Requires at least 6 Block. Lose 6 Block. Gain 3 Ward Wax. Exhaust. | Requires at least 5 Block and loses only 5 Block. | Block, Ward Wax, Conversion, Exhaust |
| 11 | **Countermanded Grace** | II | Rite | 1 | The first time each turn Censure prevents any Status stack, gain 2 Ward Wax. This may trigger from Censure on you or on an enemy. | Gain 3 Ward Wax instead. | Censure, Ward Wax, Engine |
| 12 | **Mortgage Sigil** | I | Working | 1 | Apply 3 Lien. The next time the target gains Block before the end of its next turn, apply 3 additional Lien. | Apply 4 Lien initially and 4 additional Lien. | Lien, Block, Setup |
| 13 | **Silent Hearing** | I | Working | 1 | Apply 2 Citation. Until your next turn, if the target performs a damaging action, gain 7 Block. | Apply 3 Citation. | Citation, Block, Intent |
| 14 | **Vein Register** | II | Rite | 1 | The first time each turn another Status on an enemy loses a stack, apply 1 Blood Ink to it. | Costs 0 Energy. | Blood Ink, Status Loss, Engine |
| 15 | **Sealed Mantle** | I | Working | 1 | Gain 8 Block. If at least one enemy attacks during this enemy turn and you take no unblocked Attack damage, gain 2 Ward Wax. | Gain 10 Block. | Block, Ward Wax |
| 16 | **Borrowed Candle** | I | Working | 0 | Draw 2 cards. Put one card from your hand on top of your draw pile. Exhaust. | You may put the chosen card on top or bottom of your draw pile. | Draw, Hand Control, Exhaust |
| 17 | **Notary Beetle** | I | Rite | 1 | The first time each turn you apply a negative Status to an enemy that does not already have that Status, apply 1 additional stack of it. | Costs 0 Energy. | Status, Seed, Engine |
| 18 | **Crossed Sigil** | II | Working | 1 | Remove 1 stack of a negative Status from yourself. Then apply 1 Censure to an enemy. If you had no negative Status to remove, gain 1 Censure instead. | The Censure amount becomes 2. | Cleanse, Censure |
| 19 | **Blood Tithe** | III | Deed | 1 | Deal 8 damage. If the target has Blood Ink, trigger Blood Ink immediately for twice its current value, then remove 1 Blood Ink. | Deal 11 damage. | Damage, Blood Ink, Burst |
| 20 | **Wax Reliquary** | III | Working | 1 | Gain 4 Ward Wax. Until your next turn, Ward Wax cannot suffer its additional decay for taking unblocked Attack damage. | Gain 5 Ward Wax. | Ward Wax, Preservation |
| 21 | **Sanctioned Charm** | I | Working | 1 | Gain 5 Block. Until your next turn, the first time your Censure prevents a negative Status, the Censure used to prevent it is not consumed. | Gain 7 Block. | Block, Censure, Preservation |
| 22 | **Forfeit Seal** | I | Deed | 1 | Deal 7 damage. If the target still has Block after this attack, apply 4 Lien. | Deal 10 damage. | Damage, Block, Lien |
| 23 | **Proxy Curse** | II | Working | 1 | Choose one negative Status on yourself. Remove up to 3 stacks of it. Apply 1 Blood Ink to an enemy per stack removed. | Up to 4 stacks. | Cleanse, Blood Ink, Conversion |
| 24 | **Consecrated Testament** | III | Rite | 1 | The first 3 times each turn an enemy loses HP because of a Status effect, gain 1 Ward Wax. | The first 4 times each turn. | Status, Ward Wax, Engine |
| 25 | **Black Tribunal** | IV | Deed | 2 | Deal 14 damage, plus 8 damage for each different negative Status on the target. Count at most 5 different Statuses. | Base damage becomes 18. | Damage, Multi-Status, Burst |
| 26 | **False Signature** | I | Working | 0 | Choose a card in your hand. It costs 1 less Energy this turn. After that card is played, the next card you play this combat costs 1 additional Energy. Exhaust. | The chosen card costs 2 less Energy instead. | Cost Manipulation, Exhaust |
| 27 | **Mortgaged Aegis** | III | Working | 1 | Gain 18 Block. At the start of your next turn, gain 8 Lien. | Gain 22 Block. | Block, Lien, Deferred Cost |
| 28 | **Sovereign Prohibition** | IV | Working | 2 | Gain 3 Censure. Apply 3 Censure to ALL enemies. | Costs 1 Energy. | Censure, AoE, Defense, Control |
| 29 | **Candle Cathedral** | IV | Rite | 2 | Whenever Ward Wax grants Block at the start of your turn, gain additional Block equal to half your Ward Wax, rounded up. Ward Wax no longer suffers its additional decay when you take unblocked Attack damage. | Costs 1 Energy. | Ward Wax, Block, Rule Change |
| 30 | **Grand Citation** | IV | Deed | 2 | Deal 14 damage to ALL enemies. Each enemy with Citation additionally loses HP equal to 3 × its current Citation, then loses 1 Citation. | Base damage becomes 18 to ALL enemies. | Damage, AoE, Citation, Burst |
| 31 | **Vital Census** | III | Deed | 2 | Deal 8 damage to ALL enemies. Trigger Blood Ink once on every enemy that has Blood Ink. | Deal 11 damage to ALL enemies. | Damage, AoE, Blood Ink |

## 6. Rare Cards — 19

| # | Card | Act | Type | Cost | Base | Upgrade | Tags |
|---:|---|:---:|---|---:|---|---|---|
| 1 | **Dawn Summons** | I | Deed | 2 | Deal 16 damage. If this is the first card you play this turn, deal 10 additional damage. | Base damage becomes 20. | Damage, Sequencing |
| 2 | **Reciprocal Edict** | I | Rite | 2 | The first time each turn your Censure prevents a negative Status applied by an enemy, apply 2 Censure to that enemy. The first time each turn Censure prevents a positive Status on an enemy, gain 1 Censure. | Costs 1 Energy. | Censure, Engine |
| 3 | **Usurer's Moon** | I | Rite | 1 | Whenever Lien removes Block from an enemy, apply 1 Citation for every 3 Block removed, maximum 3 Citation per Lien resolution. | Apply 1 Citation for every 2 Block removed instead; maximum remains 3. | Lien, Citation, Engine |
| 4 | **Blood Redaction** | II | Working | 1 | Choose one negative Status on an enemy other than Blood Ink. Remove up to 6 stacks of it. Apply the same number of Blood Ink. Exhaust. | Up to 8 stacks. | Blood Ink, Status Conversion, Exhaust |
| 5 | **Votive Covenant** | III | Rite | 2 | If you take no unblocked Attack damage during an enemy turn, Ward Wax does not decay after that turn. If you do take unblocked Attack damage, Ward Wax loses 3 stacks instead of 2. | Costs 1 Energy. | Ward Wax, Rule Change, Risk |
| 6 | **Moonlit Counterfeit** | II | Working | 1 | Choose a non-Rite card in your hand. Create a Temporary copy of it that costs 0 this turn. Exhaust the original. Moonlit Counterfeit Exhausts. | Put the original into your discard pile instead of Exhausting it. | Copy, Temporary, Exhaust |
| 7 | **Seizure Writ** | II | Deed | 2 | Deal 12 damage. Then remove all remaining Block from the target. For every 3 Block removed this way, apply 1 Lien, maximum 6 Lien. | Deal 15 damage and require only 2 Block removed per Lien. | Damage, Block, Lien |
| 8 | **Standing Citation** | II | Rite | 2 | The first time each turn Citation triggers on each enemy, that trigger does not remove a Citation stack. | Costs 1 Energy. | Citation, Rule Change |
| 9 | **Exemplary Sentence** | III | Deed | 2 | Choose an enemy. Remove up to 5 Citation from it. For each Citation removed, ALL enemies lose 4 HP. Then deal 12 damage to the chosen enemy. | Deal 15 damage to the chosen enemy and 5 HP loss to ALL enemies per Citation removed. | Citation, AoE, Burst |
| 10 | **Wax Indemnity** | III | Working | 1 | Until your next turn, whenever you would take unblocked Attack damage, you may consume up to 4 Ward Wax. Reduce that damage by 3 per Ward Wax consumed. | Reduce damage by 4 per Ward Wax consumed. | Ward Wax, Damage Prevention, Conversion |
| 11 | **Oath of Refusal** | III | Rite | 2 | The first 2 times each turn Censure prevents one or more Status stacks, record 1 Refusal. At the start of your next turn, draw 1 card per recorded Refusal, maximum 2; if at least 1 Refusal was recorded, gain 1 Energy. Then clear all recorded Refusal. | Costs 1 Energy. | Censure, Draw, Energy, Engine |
| 12 | **Debt Ouroboros** | III | Rite | 2 | Whenever Lien resolves, after that resolution apply Lien equal to half the amount of Lien consumed, rounded down, maximum 4 Lien reapplied per resolution. | Costs 1 Energy. | Lien, Scaling, Rule Change |
| 13 | **Crown Repossession** | IV | Deed | 3 | Deal 22 damage. Remove all remaining Block from the target. It then loses HP equal to the Block removed this way, maximum 40 HP. Apply 6 Lien. | Deal 27 damage; maximum HP loss becomes 50. | Damage, Block, Lien, Burst |
| 14 | **Absolute Interdict** | IV | Rite | 2 | The first time each turn Censure on a combatant would prevent Status stacks, 1 Censure prevents the entire Status application instead, regardless of stack count, and only 1 Censure is consumed. This applies independently to you and to each enemy. | Costs 1 Energy. | Censure, Rule Change |
| 15 | **Tallow Judgment** | IV | Deed | 2 | Consume up to 8 Ward Wax. Deal 10 damage plus 7 damage per Ward Wax consumed. | Deal 14 base damage plus 8 damage per Ward Wax consumed. | Damage, Ward Wax, Conversion, Burst |
| 16 | **Hemal Audit** | IV | Deed | 2 | Deal 18 damage. Then trigger Blood Ink repeatedly, up to 6 times or until no Blood Ink remains. | Deal 22 damage; maximum becomes 8 Blood Ink triggers. | Damage, Blood Ink, Burst |
| 17 | **Compound Indictment** | IV | Working | 1 | Requires at least 3 different negative Statuses on the target. Choose up to 5 different stackable negative Statuses on it and add 2 stacks to each. Exhaust. | Add 3 stacks to each chosen Status instead. | Multi-Status, Scaling, Exhaust |
| 18 | **Grand Dispensation** | IV | Working | 2 | Choose 2 different options: deal 24 damage to an enemy; gain 24 Block; draw 3 cards; gain 2 Energy. Exhaust. | Choose 3 different options. | Choice, Damage, Block, Draw, Energy, Exhaust |
| 19 | **Last Office** | IV | Working | 2 | Choose an enemy. Count the number of different stackable Statuses that have reached 0 stacks on any combatant during this combat, maximum 5. For each counted Status, deal 8 damage to the chosen enemy and gain 3 Block. Exhaust. | Deal 10 damage and gain 4 Block per counted Status. | Status History, Damage, Block, Exhaust |

## 7. Temporary Copies

The general pool uses the same BnB-wide **Temporary** rule as the Bureaucrat pool:

- a Temporary card is a combat-only generated instance;
- it cannot itself become the source of another copy, restore, history or record operation unless an effect explicitly overrides this;
- it is not a persistent deck instance;
- where a creating card says the copy Exhausts, it Exhausts after resolving.

This rule is especially important for **Moonlit Counterfeit** and for future character interactions.

## 8. Cross-Pool Guardrails

The general pool must remain horizontal rather than replacing character identities.

- General cards may interact with any normal Status, but should not become strictly superior versions of character-specific enablers or finishers.
- General recursion/copy effects must not appropriate a character's defining recursion identity. Strong multi-card Exhaust recursion is therefore reserved for character-specific content.
- General early-game cards must not simply be upgraded versions of starter damage/Block cards.
- Status amplification must be capped where future characters could otherwise create unbounded scaling. `Compound Indictment` therefore affects at most five different Statuses.
- **Censure** can interfere with Status applications but not with boss phases, transformations, summons or encounter-rule transitions.
- **Notary Beetle** only amplifies the first application of a negative Status that is not already present, preventing it from becoming a universal recurring +1 Status engine.
- **Grand Dispensation** Exhausts because repeated Draw + Energy selection would otherwise be a near-universal auto-pick.

## 9. Act-Level Balance Intent

### Act I

The pool is deliberately small: **16 Uncommons + 3 Rares**. Cards are flexible and understandable, and the three Rares can meaningfully shape a run without providing late-game engine power prematurely.

### Act II

The pool gains **6 Uncommons + 4 Rares**. Cards become more technical: copying, conversion, stronger Status manipulation and engine pieces appear, but raw endgame damage remains limited.

### Act III

The pool gains **5 Uncommons + 5 Rares**. Defensive engines and meaningful cash-out cards become viable. Cards are expected to compete against substantially more dangerous multi-enemy and elite encounters.

### Act IV

The pool gains **4 Uncommons + 7 Rares**. These cards are intentionally powerful and include 40–80+ damage ceilings, major rule changes and high-impact universal utility. This is necessary because the deck must handle the Act-IV endgame and then enter Act V without receiving another new card tier.

## 10. Playtest Watchpoints

The following are not structural blockers but should receive focused simulation/live-play testing:

- **Censure:** whether repeated denial trivializes specific buff/debuff-heavy encounters despite the boss-mechanic exclusion rules.
- **Blood Ink:** interaction density with large Status-conversion cards, especially `Blood Redaction → Compound Indictment → Hemal Audit`.
- **Ward Wax:** whether preservation engines become effectively permanent Block loops, especially with `Votive Covenant` and `Candle Cathedral`.
- **Lien:** whether enemy Block patterns make Lien too binary across the encounter roster.
- **Citation:** whether pure-attack enemies make too many Citation cards dead picks, and whether Utility-heavy bosses are punished disproportionately.
- **Moonlit Counterfeit:** universal scaling with future high-cost character cards.
- **Grand Dispensation:** universal pick rate despite Exhaust.
- **Last Office:** tracking complexity and whether the combat-history condition is sufficiently readable in UI.

## 11. Current Content Status

- [x] 50 character-unspecific reward cards
- [x] 31 Uncommons
- [x] 19 Rares
- [x] No general Commons
- [x] Every card has a direct upgraded version
- [x] Act gates assigned to all cards
- [x] 5 general Status Effects defined
- [x] Censure player/enemy symmetry defined
- [x] Lien timing defined
- [x] Citation action classification defined
- [x] Blood Ink event semantics defined
- [x] Ward Wax decay and mitigation timing defined
- [x] Temporary-copy safety rule defined
- [x] Final duplication / naming / cross-character audit applied
- [ ] Numerical combat simulation and live playtest balance pass
