# Bureaucrat — Final Card Pool

> **Status:** Final post-audit card design specification. Structural design, naming, Act gates, upgrades and rules are locked for implementation. Numerical values remain subject only to combat simulation and live balance passes.

## 1. Pool Summary

The Bureaucrat has **80 regular reward cards**, each with a direct upgraded version. Starter cards and generated Junk cards are separate from the 80-card reward pool.

| Rarity | Act I | Act II | Act III | Act IV | Total |
|---|---:|---:|---:|---:|---:|
| Common | 15 | 3 | 1 | 1 | 20 |
| Uncommon | 20 | 7 | 5 | 3 | 35 |
| Rare | 11 | 4 | 6 | 4 | 25 |
| **New cards unlocked** | **46** | **14** | **12** | **8** | **80** |

| Minimum Act reached | Available Bureaucrat reward pool |
|---|---:|
| Act I | 46 |
| Act II | 60 |
| Act III | 72 |
| Act IV | 80 |

### Design intent by Act

- **Act I:** readable foundations, municipal/clerical absurdity, early Paperwork/Doubt/Junk/Queue/Seal seeds, reliable damage and Block.
- **Act II:** archival logic, recursion, redaction, indexing, Junk classification and deliberate deck manipulation.
- **Act III:** custom, testimony, hospitality, restitution, grievances and effects that care about what happened in previous turns.
- **Act IV:** sacred/monumental administration, tallying, measures, processions, cartouches, thresholds, large conversions and endgame finishers.
- **Act V:** no new Bureaucrat reward cards; the completed Act-IV deck is tested against the final boss gauntlet.

## 2. Card-Type Taxonomy

BnB does **not** use Slay-the-Spire-style `Attack / Skill / Power` card types.

| Type | Definition |
|---|---|
| **Deed** | A one-shot offensive action. Direct damage is usually central, though a Deed may also apply statuses or convert resources. |
| **Working** | A one-shot defensive, manipulative, administrative or magical action. |
| **Rite** | A persistent combat effect that remains active and changes rules, engines or recurring behavior. |
| **Junk** | A generated administrative nuisance card. Junk is not part of the normal reward pool. |

`Form`, `Argument`, `Permit` and similar concepts are **tags/subtypes**, not primary card types. Enemy **Attack intents** remain an enemy-system term and are unrelated to the card type `Deed`.

## 3. Core Bureaucrat Rules

### 3.1 Paperwork

**Paperwork X:** At the end of the affected enemy's turn, it loses HP equal to its current Paperwork. Paperwork ignores Block and does not decay.

- Paperwork loss is treated as **HP loss**, not ordinary damage, unless an effect explicitly says otherwise.
- A **Paperwork trigger** means one resolution of this HP-loss event.
- Effects that trigger Paperwork immediately use the target's current Paperwork value at resolution time.

### 3.2 Doubt

**Doubt X:** The next `X` enemy Attack actions each deal **25% less damage**. After one full Attack action resolves, remove **1 Doubt**.

- Multi-hit Attacks consume only **1 Doubt** for the entire Attack action.
- The 25% modifier applies to the Attack action as a whole according to the Core's normal damage-rounding rules.
- Doubt is normally consumed even if Block prevents all resulting damage, unless a card explicitly overrides that rule.

### 3.3 Seal and Ratified

**Seal:** Whenever an enemy reaches 3 Seal, remove exactly 3 Seal and trigger a **Ratify event**. Excess Seal remains.

**Ratified:** Until the end of the current player turn, each **Deed** targeting that enemy deals **+3 total direct damage**.

- The +3 applies **once per Deed card played**, regardless of hit count or internal repeats.
- Additional Ratify events in the same player turn do not stack another +3 damage modifier, but they still count as separate Ratify events for triggered effects.
- Seal overflow remains after Ratification (for example, 5 Seal → Ratify + 2 Seal remaining).

### 3.4 Archive vs. Exhaust

**Archive** is a specific action: a card is moved to the Exhaust pile by an effect that explicitly says `Archive`.

- Every Archived card is in the Exhaust pile, but not every Exhausted card was Archived.
- Effects that say `when/whenever you Archive` trigger only from Archive actions.
- Effects that inspect the Exhaust pile may see both Archived cards and cards Exhausted by other means.
- If an effect Archives multiple cards, each card produces its own Archive event.

### 3.5 Queue

To **Queue** a card:

1. Pay its Energy cost immediately (after any Queue-specific discounts).
2. Choose targets and modes immediately.
3. Remove it from the hand and place it in the Queue.
4. The card counts as **played** now, but its queued effect has not yet resolved.

Normal player-turn order:

1. Energy refresh.
2. Start-of-turn effects.
3. Resolve cards already in the Queue, **oldest first (FIFO)**.
4. Normal draw.
5. Player action phase.

Additional Queue rules:

- Queue resolution does **not** count as playing the card a second time.
- A target is locked when the card is Queued. If that target no longer exists at resolution, target-bound parts fizzle; there is no automatic retargeting.
- Combat-state values such as current Paperwork are evaluated when the card resolves unless the card explicitly snapshots them.
- Cards newly Queued during a normal Queue-resolution window wait until the next Queue-resolution window.
- Effects such as **Night Docket** can explicitly resolve a Queued card outside the normal window.
- End-of-turn Rite effects resolve before temporary end-of-player-turn states such as Ratified expire.

### 3.6 Temporary copies

A **Temporary** copy is a combat-only generated card instance.

- Temporary copies created by Bureaucrat copy effects Exhaust after resolving when their creating effect specifies this.
- Temporary cards cannot themselves be used as the source of another copy, restore, history or record operation unless a card explicitly overrides this.
- Temporary cards are not persistent deck instances.

## 4. Generated Junk Cards

| Junk | Cost | Effect | Mechanical identity |
|---|---:|---|---|
| **Red Tape** | — | **Unplayable.** | Hard hand clog; must be removed by another effect. |
| **Duplicate Copy** | 0 | Exhaust. No other effect. | Dead draw that can at least be disposed of for free. |
| **Misfiled Paper** | 1 | Draw 1 card. Exhaust. | Cycling taxed by Energy. |
| **Unsigned Form** | 0 | Exhaust. Add a fresh Unsigned Form to your discard pile. | Recurring administrative nuisance; only Archive truly disposes of it. |

## 5. Starter Deck

Starting resources: **70 HP, 3 Energy**.

| Card | Qty. | Type | Cost | Base | Upgrade | Tags |
|---|---:|---|---:|---|---|---|
| **Paper Cut** | 4 | Deed | 1 | Deal 6 damage. | Deal 8 damage. | Damage |
| **Cower Behind a Desk** | 4 | Working | 1 | Gain 5 Block. | Gain 7 Block. | Block |
| **Strong Binder** | 1 | Working | 1 | Gain 7 Block. Apply 1 Doubt. | Gain 9 Block. Apply 2 Doubt. | Block, Doubt, Argument |
| **Permit A38** | 1 | Working | 2 | Apply 5 Paperwork. | Costs 1 Energy. | Paperwork, Form, Permit |

## 6. Regular Reward Cards

### Common — 20 cards

| # | Card | Act | Type | Cost | Base | Upgrade | Tags |
|---:|---|:---:|---|---:|---|---|---|
| 1 | **Waxing Authority** | I | Deed | 1 | Deal 5 damage. Apply 1 Seal. | Deal 7 damage. Apply 1 Seal. | Damage, Seal |
| 2 | **Form of Ill Intent** | I | Working | 1 | Apply 3 Paperwork. If the target intends to Attack, also apply 1 Doubt. | Apply 4 Paperwork. If the target intends to Attack, also apply 1 Doubt. | Paperwork, Doubt, Intent |
| 3 | **Protective Adjournment** | I | Working | 1 | Queue: Gain 11 Block. | Queue: Gain 14 Block. | Queue, Block |
| 4 | **Secure Misfiling** | I | Working | 0 | Add 1 Misfiled Paper to your discard pile. Draw 1 card. | Add 1 Misfiled Paper to your discard pile. Draw 2 cards. | Junk, Draw |
| 5 | **Cauldron Copy** | I | Deed | 1 | Deal 9 damage. Add 1 Duplicate Copy to your discard pile. | Deal 12 damage. Add 1 Duplicate Copy to your discard pile. | Damage, Junk |
| 6 | **Occult Precedent** | I | Working | 1 | Gain 7 Block. If any enemy has Paperwork, gain 2 additional Block. | Gain 9 Block. If any enemy has Paperwork, gain 2 additional Block. | Block, Paperwork |
| 7 | **Fine-Print Hex** | I | Deed | 1 | Deal 7 damage. If the target has Doubt, apply 1 Seal. | Deal 9 damage. If the target has Doubt, apply 1 Seal. | Damage, Doubt, Seal |
| 8 | **Certified Kindling** | I | Working | 1 | Archive a card from your hand. Gain 4 Block. If it was Junk, gain 4 additional Block. | Archive a card from your hand. Gain 6 Block. If it was Junk, gain 4 additional Block. | Archive, Junk, Block |
| 9 | **Broom Dispatch** | II | Working | 1 | Apply 2 Paperwork to ALL enemies. | Apply 3 Paperwork to ALL enemies. | Paperwork, AoE |
| 10 | **Notarial Press** | I | Working | 1 | Apply 2 Seal. If this Ratifies the target, gain 5 Block. | Apply 2 Seal. If this Ratifies the target, gain 7 Block. | Seal, Ratify, Block |
| 11 | **Inkblot Verdict** | I | Deed | 1 | Deal 8 damage. If the target has Paperwork, deal 2 additional damage. | Deal 10 damage. If the target has Paperwork, deal 2 additional damage. | Damage, Paperwork |
| 12 | **Deskward** | I | Working | 1 | Gain 8 Block. Add 1 Red Tape to your discard pile. | Gain 11 Block. Add 1 Red Tape to your discard pile. | Block, Junk |
| 13 | **Deferred Hex** | I | Deed | 1 | Queue: Deal 13 damage. | Queue: Deal 16 damage. | Queue, Damage |
| 14 | **Errata Furnace** | II | Working | 1 | Archive a Junk card from your hand. Apply 4 Paperwork to a random enemy. | Archive a Junk card from your hand. Apply 5 Paperwork to a random enemy. | Archive, Junk, Paperwork |
| 15 | **Seal of Concern** | I | Working | 1 | Apply 1 Seal and 1 Doubt. | Apply 2 Seal and 1 Doubt. | Seal, Doubt |
| 16 | **Petty Objection** | I | Working | 1 | Gain 5 Block. Apply 1 Doubt. | Gain 7 Block. Apply 1 Doubt. | Block, Doubt |
| 17 | **Cursed Addendum** | I | Deed | 1 | Deal 6 damage. Apply 2 Paperwork. | Deal 8 damage. Apply 2 Paperwork. | Damage, Paperwork |
| 18 | **Cross-Filing** | II | Working | 1 | Apply 4 Paperwork to an enemy. If another enemy is present, you may move 2 of that newly applied Paperwork to it. | Apply 5 Paperwork to an enemy. If another enemy is present, you may move 3 of that newly applied Paperwork to it. | Paperwork, Multi-Enemy |
| 19 | **Priority Docket** | III | Working | 1 | Choose another card in your hand and Queue it, paying 1 less Energy (minimum 0). | Choose another card in your hand and Queue it, paying 2 less Energy (minimum 0). | Queue, Energy |
| 20 | **Final Attestation** | IV | Deed | 1 | Deal 8 damage. If the target is Ratified, gain 1 Energy. | Deal 11 damage. If the target is Ratified, gain 1 Energy. | Damage, Ratify, Energy |

### Uncommon — 35 cards

| # | Card | Act | Type | Cost | Base | Upgrade | Tags |
|---:|---|:---:|---|---:|---|---|---|
| 21 | **Black Ledger** | I | Rite | 1 | At the start of your turn, if any enemy has at least 8 Paperwork, draw 1 card. | Threshold becomes 6 Paperwork. | Paperwork, Draw |
| 22 | **Ash Register** | I | Rite | 1 | The first time each turn you Archive a card, draw 1 card. | Costs 0 Energy. | Archive, Draw |
| 23 | **Night Docket** | I | Working | 0 | Resolve your oldest Queued card immediately. Add 1 Red Tape to your discard pile. Exhaust. | Do not add Red Tape. | Queue, Junk, Exhaust |
| 24 | **Seal Dividend** | I | Rite | 1 | The first time each turn you Ratify an enemy, draw 1 card. | Costs 0 Energy. | Seal, Ratify, Draw |
| 25 | **Dubious Authority** | I | Rite | 1 | Whenever Doubt is consumed after an enemy attacks, apply 2 Paperwork to that enemy. | Apply 3 Paperwork instead. | Doubt, Paperwork |
| 26 | **Counter Ward** | I | Working | 1 | Gain 6 Block. The next card you Queue this turn costs 1 less Energy (minimum 0). | Gain 8 Block. | Block, Queue, Energy |
| 27 | **Palimpsest Order** | II | Working | 1 | Archive a card from your hand. Return a non-Junk card from your discard pile to your hand. Exhaust. | Costs 0 Energy. | Archive, Recursion, Discard |
| 28 | **Redaction Veil** | II | Working | 1 | Remove up to 4 Paperwork from an enemy. Gain 3 Block for each Paperwork removed. | May remove up to 5 Paperwork. | Paperwork, Block, Conversion |
| 29 | **Restitution Writ** | III | Working | 0 | Apply Paperwork equal to half the unblocked Attack damage you took during the previous enemy turn, rounded down. Maximum 6 Paperwork. Exhaust. | Maximum becomes 9 Paperwork. | Paperwork, Retaliation, Exhaust |
| 30 | **Temple Tally** | IV | Rite | 1 | Whenever an enemy reaches a new multiple of 5 Paperwork for the first time this combat, apply 1 Seal to it for each new multiple crossed. | Costs 0 Energy. | Paperwork, Seal, Threshold |
| 31 | **Threefold Injunction** | I | Deed | 1 | Deal 3 damage 3 times. If the target is Ratified, each hit also applies 1 Paperwork. | Deal 4 damage 3 times. | Damage, Multi-Hit, Ratify, Paperwork |
| 32 | **Candle Allowance** | I | Working | 0 | Queue: Gain 1 Energy and draw 1 card. Exhaust. | Queue: Gain 1 Energy and draw 2 cards. Exhaust. | Queue, Energy, Draw, Exhaust |
| 33 | **Cinder Warrant** | I | Deed | 1 | Deal 7 damage. You may Archive a Junk card from your hand; if you do, repeat this attack. | Deal 8 damage. You may Archive a Junk card from your hand; if you do, repeat this attack. | Damage, Archive, Junk |
| 34 | **Smudged Index** | II | Working | 1 | Look at the top 4 cards of your draw pile. Archive one. Put the others back in any order. Gain 4 Block. | Look at the top 5 cards. Gain 6 Block. | Archive, Draw-Pile, Block, Control |
| 35 | **Hedge Hospitality** | III | Working | 1 | Gain 7 Block. Until your next turn, the first enemy that deals unblocked Attack damage to you gains 4 Paperwork. | Gain 9 Block; apply 5 Paperwork instead. | Block, Paperwork, Retaliation |
| 36 | **Clerk's Familiar** | I | Rite | 1 | The first time each turn you create a Junk card, gain 4 Block. | Gain 5 Block instead. | Junk, Block |
| 37 | **Presumption of Error** | I | Working | 1 | Apply 1 Doubt. The next time that enemy consumes Doubt by attacking, apply 1 Doubt to it after the Attack resolves. Exhaust. | Costs 0 Energy. | Doubt, Exhaust |
| 38 | **Tallow Budget** | I | Working | 0 | Gain 1 Energy. Add 1 Red Tape to your hand. Exhaust. | Add the Red Tape to your discard pile instead. | Energy, Junk, Exhaust |
| 39 | **Clutter Concordance** | II | Deed | 1 | Deal 5 damage, plus 2 damage for each different Junk type currently present across your discard and Exhaust piles. | Base damage becomes 7. | Damage, Junk, Collection |
| 40 | **Witness Knot** | III | Working | 1 | Apply 1 Doubt to an enemy. If it attacks before your next turn, apply 2 Paperwork to all other enemies. | Apply 3 Paperwork to all other enemies instead. | Doubt, Paperwork, Multi-Enemy |
| 41 | **Conditional Approval** | I | Deed | 1 | Deal 6 damage. If the target does not intend to Attack, apply 2 Seal; otherwise apply 1 Seal. | Deal 8 damage. | Damage, Seal, Intent |
| 42 | **Wastepaper Bastion** | I | Working | 1 | Gain 4 Block, plus 2 Block for each Junk card in your hand. | Gain 5 Block, plus 3 Block for each Junk card in your hand. | Block, Junk, Hand |
| 43 | **Formal Dissent** | I | Working | 0 | Remove 1 Doubt from an enemy. Gain 1 Energy. Exhaust. | Also draw 1 card. | Doubt, Energy, Exhaust |
| 44 | **Marginalia** | II | Working | 1 | Choose another non-Rite, non-Temporary persistent card from your Exhaust pile. Create a Temporary copy in your hand. It costs 1 more Energy this turn and Exhausts when played. Marginalia Exhausts. | The created copy does not cost 1 additional Energy. | Exhaust, Copy, Temporary, Recursion |
| 45 | **Processional Calendar** | IV | Rite | 1 | At the end of your turn, if you have at least 2 Queued cards, resolve your oldest Queued card. | Costs 0 Energy. | Queue, Tempo |
| 46 | **Hex Circular** | I | Deed | 2 | Deal 7 damage to ALL enemies. Apply 1 Doubt to ALL enemies. | Deal 9 damage to ALL enemies. | Damage, AoE, Doubt |
| 47 | **Pending Matters** | I | Rite | 1 | The first time each turn a Queued card resolves, gain 3 Block. | Gain 4 Block instead. | Queue, Block |
| 48 | **Notary's Tithe** | I | Working | 0 | Remove 1 Seal from an enemy. Draw 2 cards. Exhaust. | Draw 3 cards instead. | Seal, Draw, Exhaust |
| 49 | **Binding Fee** | II | Working | 1 | Archive a non-Junk card from your hand. Apply Paperwork equal to 3 plus its base Energy cost. | Apply Paperwork equal to 4 plus its base Energy cost. | Archive, Paperwork, Conversion |
| 50 | **Guestbook Oath** | III | Rite | 1 | At the end of your turn, if you have any Block, apply 1 Doubt to every enemy that intends to Attack. | Costs 0 Energy. | Block, Doubt, Intent, Multi-Enemy |
| 51 | **Backlog Charge** | I | Deed | 1 | Deal 6 damage, plus 3 damage for each card currently in your Queue. Count at most 3 Queued cards. | Base damage becomes 8. | Damage, Queue |
| 52 | **Clerical Discretion** | I | Working | 1 | Gain 5 Block. Choose one: apply 1 Doubt; or apply 1 Seal. | Gain 7 Block. | Block, Choice, Doubt, Seal |
| 53 | **Dead Letter Office** | II | Working | 1 | For each different Junk type you have Archived this combat, apply 1 Paperwork to ALL enemies. Exhaust. | After resolving the base effect, apply 1 additional Paperwork to ALL enemies. | Archive, Junk, Paperwork, AoE, Collection |
| 54 | **Customary Due** | III | Working | 0 | Choose a non-Rite, non-Temporary card that resolved during your previous turn. Create a Temporary copy and Queue it, paying its normal Energy cost. The copy Exhausts after resolving. Customary Due Exhausts. | The copied card costs 1 less Energy to Queue (minimum 0). | Queue, Copy, Temporary, History, Exhaust |
| 55 | **Hieratic Measure** | IV | Rite | 2 | Whenever you Ratify an enemy, immediately trigger its current Paperwork once, then remove 3 Paperwork from it. | Costs 1 Energy. | Ratify, Paperwork, Conversion |

### Rare — 25 cards

| # | Card | Act | Type | Cost | Base | Upgrade | Tags |
|---:|---|:---:|---|---:|---|---|---|
| 56 | **Red Ink Doctrine** | I | Rite | 2 | After an enemy takes HP loss from its Paperwork trigger, if it survives, apply 2 Paperwork to it. | Costs 1 Energy. | Paperwork, Scaling |
| 57 | **Licensed Disposal** | I | Rite | 2 | The first Junk card you draw each turn is automatically Archived; then draw 1 card. Archive triggers still occur. | Costs 1 Energy. | Junk, Archive, Draw |
| 58 | **Skeleton Staff** | I | Rite | 2 | At the end of your turn, you may Queue one non-Rite card from your hand with base Energy cost 2 or less for 0 Energy. Add 1 Red Tape to your discard pile. | May choose a non-Rite card with base Energy cost 3 or less. | Queue, Energy, Junk |
| 59 | **Ghost Register** | II | Rite | 2 | The first non-Junk, non-Temporary persistent card you Archive each turn is recorded. At the start of your next turn, add a Temporary copy of it to your hand. It costs 0 Energy and Exhausts when played. | Costs 1 Energy. | Archive, Copy, Temporary, Recursion |
| 60 | **Hearth Compact** | III | Rite | 2 | Whenever an enemy with Doubt attacks and deals no unblocked damage, the Doubt stack that would normally be consumed is retained. | Costs 1 Energy. | Doubt, Block, Rule Change |
| 61 | **Summary Judgment** | I | Deed | 2 | Deal 16 damage. If the target has at least 6 Paperwork, trigger its Paperwork immediately, then remove 3 Paperwork. | Deal 19 damage. | Damage, Paperwork, Burst |
| 62 | **Candle Tribunal** | I | Deed | 2 | Deal 5 damage 3 times. If the target is Ratified, repeat this attack. | Deal 6 damage 3 times. | Damage, Multi-Hit, Ratify, Burst |
| 63 | **Archive Pyre** | II | Deed | 2 | Archive all Junk cards in your hand. Deal 9 damage to ALL enemies, plus 5 damage for each Junk Archived this way. | Base damage becomes 12. | Archive, Junk, Damage, AoE |
| 64 | **Due Recompense** | III | Deed | 2 | Deal 14 damage, plus 5 damage for each Doubt on the target. Count at most 6 Doubt. Then remove all Doubt from the target. | Base damage becomes 18. | Damage, Doubt, Conversion, Burst |
| 65 | **Cartouche Reckoning** | IV | Deed | 3 | Deal 18 damage. Then, up to 3 times: if the target has at least 10 Paperwork, remove 10 Paperwork and repeat this attack. | Deal 21 damage per hit. | Damage, Paperwork, Conversion, Burst |
| 66 | **Rebuttal** | I | Deed | 1 | Deal 9 damage. Gain 4 Block per Doubt already on the target, maximum 12 Block. Then apply 1 Doubt. | Deal 12 damage. | Damage, Block, Doubt |
| 67 | **Privy Seal** | I | Working | 1 | Requires at least 1 Seal. Remove all Seals from an enemy and Ratify it immediately. Draw 1 card. Exhaust. | Does not Exhaust. | Seal, Ratify, Draw, Exhaust |
| 68 | **Funeral Index** | II | Deed | 2 | Deal 5 damage for each card you have Archived this combat. Count at most 8 cards. Exhaust. | Deal 6 damage per Archived card. | Damage, Archive, Scaling, Exhaust |
| 69 | **Blood Testimony** | III | Deed | 2 | Deal 9 damage to ALL enemies. Enemies that attacked during the previous enemy turn take 9 additional damage. | Deal 12 base damage; the additional damage becomes 10. | Damage, AoE, History, Retaliation |
| 70 | **Monumental Writ** | IV | Deed | 3 | Queue: Deal 24 damage. When this resolves, deal 12 additional damage for each other card that resolved from your Queue after Monumental Writ was queued and before it resolved. Count at most 3 cards. | Base damage becomes 30. | Queue, Damage, Scaling, Burst |
| 71 | **Blank Warrant** | I | Deed | 2 | Deal 18 damage. If the target has no Paperwork, Doubt, or Seal, deal 5 additional damage. | Deal 22 damage; the conditional bonus remains 5. | Damage |
| 72 | **Continuance** | I | Rite | 2 | At the end of your turn, retain up to 8 Block. | Retain up to 12 Block. | Block, Retention, Rule Change |
| 73 | **Null Catalogue** | II | Working | 1 | Choose up to 2 cards in your discard pile. Archive them. Draw 1 card for each card Archived this way. Exhaust. | Costs 0 Energy. | Archive, Discard, Draw, Exhaust |
| 74 | **Hedge Covenant** | III | Rite | 2 | Whenever Doubt reduces Attack damage, after that Attack has fully resolved, gain Block equal to half the prevented damage, rounded up. | Costs 1 Energy. | Doubt, Block, Conversion |
| 75 | **Stone Levy** | IV | Deed | 2 | Remove up to 20 of your Block. Deal 10 damage plus 2 damage for each Block removed. | May remove up to 25 Block. | Damage, Block, Conversion, Burst |
| 76 | **Violence Allowance** | I | Rite | 2 | The first Deed you play each turn costs 1 less Energy (minimum 0). | Costs 1 Energy. | Deed, Energy |
| 77 | **Stay of Execution** | I | Working | 1 | Choose an enemy with Paperwork. Its Paperwork does not trigger at the end of its next turn. Gain 2 Block per current Paperwork on that enemy, maximum 20 Block. | Maximum becomes 28 Block. | Paperwork, Block, Delay |
| 78 | **Guest Right** | III | Rite | 2 | Once per turn, when an enemy with at least 3 Doubt would deal unblocked Attack damage, remove 3 Doubt and reduce that remaining damage to 0. | Costs 1 Energy. | Doubt, Damage Prevention, Rule Change |
| 79 | **Grievance Ledger** | III | Deed | 2 | Deal 10 damage, plus 6 damage for each time this enemy has attacked during this combat. Count at most 4 attacks. | The bonus becomes 8 damage per counted attack. | Damage, History, Scaling |
| 80 | **Fivefold Compliance** | IV | Deed | 3 | Deal 12 damage, then repeat once for each fulfilled clause: target has at least 10 Paperwork; target has at least 3 Doubt; target is Ratified; you have at least 2 different Junk types in your Exhaust pile; you currently have at least 1 Queued card. | Deal 15 damage per hit. | Damage, Paperwork, Doubt, Ratify, Junk, Queue, Hybrid, Burst |

## 7. Build Architecture

The Bureaucrat is designed around five interconnected mechanical pillars rather than five isolated archetypes.

| Pillar | Core role | Typical payoff direction |
|---|---|---|
| **Paperwork** | Persistent administrative pressure / scaling HP loss | Burst conversion, defense conversion, thresholds, boss scaling |
| **Doubt** | Precision enemy-attack suppression | Defensive engines, retaliation, conversion into damage/Energy/Paperwork |
| **Junk / Archive** | Self-created deck pollution and deliberate processing | Draw, Energy, Block, damage, collection and recursion |
| **Queue** | Delayed execution and future-turn planning | Efficiency, tempo manipulation, backlog scaling and endgame burst |
| **Seal / Ratify** | Setup → short offensive burst window | Draw, Energy, Paperwork bridges and multi-hit/burst finishers |

Normal **damage + Block** remains a viable build foundation. The character is not required to commit to Paperwork or any other special package to win.

## 8. Balance and Implementation Guardrails

- **Rarity and Act gate are separate axes.** A Common may be Act IV-only because of raw efficiency; a Rare may appear in Act I because it is a build-around rather than an immediate numerical nuke.
- Early cards should remain useful later through hooks, conversions and synergy rather than being replaced by simple numerical power creep.
- Act III–IV cards intentionally accelerate engines and include large finishers because late enemies, elites and bosses require substantially higher throughput.
- The strongest endgame Deeds generally require accumulated Paperwork, Doubt, archived cards, Block, a Queue backlog, Ratification, or multi-system setup; their ceiling is intentional.
- Act V adds no new reward cards. Endgame viability must therefore already exist in the Act-IV-complete pool.
- **Watch closely in playtests:** Junk becoming pure upside; Black Ledger activation frequency; Tallow Budget Energy economy; Temporary-copy value; Queue tempo; Skeleton Staff Energy cheating.

## 9. Current Content Status

- [x] 4 starter-card identities with direct upgrades
- [x] 20 Commons
- [x] 35 Uncommons
- [x] 25 Rares
- [x] Every regular card has an upgraded version
- [x] Act gates assigned to all 80 reward cards
- [x] Deed / Working / Rite taxonomy defined
- [x] Paperwork / Doubt / Seal / Ratified rules defined
- [x] Four distinct Junk identities defined
- [x] Archive / Exhaust semantics defined
- [x] Queue timing and targeting defined
- [x] Temporary-copy anti-loop rules defined
- [x] Naming audit and Act-theme pass applied
- [ ] Numerical combat simulation and live playtest balance pass
