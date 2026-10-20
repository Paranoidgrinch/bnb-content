# The Sorcerer's Apprentice — Character Master

> **Game:** Bureaucrats & Broomsticks (BnB)  
> **Revision:** Design v0.4 — post-audit rebuild, 20 October 2026  
> **Status:** Complete **design specification for a vertical slice**, not an implemented/verified engine feature. Exact numbers are **BALANCE DRAFT** and must be tuned against actual encounters. Named behaviors, constraints and interoperability rules form the proposed implementation contract.  
> **Content:** 42 class-specific card definitions (4 starter, 16 common, 14 uncommon, 8 rare), ten-card starting deck; nine class-specific relics (2 common, 2 uncommon, 2 rare, 3 shop); compatibility profiles for all 50 existing General Pool cards; deterministic Wand interpreter; sample builds and acceptance tests.

---

## 0. Design identity and boundaries

A formally schooled novice who knows each incantation but not quite what happens when one instruction is put after another. The magic is real, the formulas are accurate, and the accidents are generally earned. His world is a candlelit, mildly ridiculous school of sorcery: ink-stained exercise books, chalk circles, singed sleeves, shutters that will not stay shut, badly placed footnotes, and a master who is never present when required.

**Core decision:** *Play the card for its immediate effect, or withdraw it from the deck to make a reusable part of a spell?* Then: *Which order, which length, how much Focus, how much exhaustion, and when to dismantle it?*

**Distinct from other characters**
- **Bureaucrat:** administrative effects, Paperwork, archival procedures and control.
- **Hedge Witch:** cards become consumable ingredients; a completed 3-ingredient recipe is brewed and spent. Exact combinations and family counts matter; **ingredient order does not**.
- **Apprentice:** installed cards remain in the Wand and form a reusable, ordered program. **Position and execution history matter**; no ingredients, brew actions, concentrated recipes or hidden recipe database.

**Naming:** compact older-world names, recognisable physical objects and classroom mishaps. Do not invent machinery or references to objects introduced after 1900. Prefer *Folded Leaf, Snuffer, Clay Weight, Chalked Thumbstall* to abstract 'Arcane / Eternal / Grand ...' padding. Occasional schoolbook bureaucracy is welcome; avoid naming every card after a permit.

**Design constraints**
1. The 42 cards are intentionally approximately half the other characters' 84. No extra filler to enforce equal elemental quotas.
2. Each Apprentice card has **two authored modes**: `play` and `wand`. Its Wand mode is NOT the normal card effect replayed at full strength.
3. The four elements are **combat statuses**, not ingredient families or fixed card-to-operation tags. A Fire-focused card can be an `Echo`, `Shape`, `Burn`, etc.
4. Each card must create a genuinely distinct pick, positioning choice, or timing trade-off. A card may have a deliberately plain Play effect when its Wand effect is the main attraction.
5. General Pool card text and Bureaucrat/Hedge Witch behavior are not modified.
6. No random Miscasts; predictability is a strength. Misfortune remains the Witch's separate chance-based mechanic.
7. New baseline rules should be few; rare cards/relics provide specific controlled exceptions.

---

## 1. Content inventory and acquisition

| Shelf | Starter definitions | Common | Uncommon | Rare | Total definitions |
|---|---:|---:|---:|---:|---:|
| Apprentice | 4 | 16 | 14 | 8 | **42** |
| Existing General Pool | — | 0 | 31 | 19 | **50** |
| Combined definitions | 4 | 16 | 45 | 27 | **92** |

**Starting deck, 10 physical cards:** `4× Candle Stub`, `4× Chalk Line`, `1× Ink Blot`, `1× Flyleaf`. No class-specific relic is required to operate the Wand. Starting HP/energy/draw defaults should be read from character baseline configuration; for vertical-slice trials assume 3 Energy and normal opening draw. Do not hard-code an HP figure merely because the Bureaucrat uses 70.

**Relics (nine):** 2 Common, 2 Uncommon, 2 Rare in the **normal character-specific relic pool**; 3 Shop-only. The remaining existing General Pool relics remain eligible according to the game's acquisition rules. Use character-vs-general weighting to prevent a small class pool from becoming invisible; do not create Act locks solely because a card or relic is Rare.

**Act role vs. availability:** Commons must function in Act I without engine pieces; Uncommons introduce combinatorial construction that can already be rewarding in Act I; Rares may appear early and **must never be blank until Act III**, but can scale substantially with built Wands into Acts III–V. Do not assert the old archive's equal-weight Act-I reward algorithm is the current engine; validate actual RewardGenerator in RogueDeck-Core before setting weights.

**Balance notation:** All numerical costs, HP/damage/Block amounts, caps and frequency limits below are initial implementable defaults (**BD**, balance draft). Their event ordering and special interactions are part of the proposed rules, even when numeric magnitudes change later. `+` is the card's upgraded mode, not a second independent card definition.

---

## 2. Four elemental combat effects

These are separate from Wand operation kinds. Cards in either mode may apply them. They use the existing status framework where possible, but retain the special trigger rules below.

| Status | Holder | Trigger and precise behavior (BD) | Distinction |
|---|---|---|---|
| **Singe** (Fire) | Enemy, negative | When the affected enemy receives a *direct damaging hit* with positive pre-mitigation damage, consume **1 Singe** and add **2 damage** to that hit, **before Block**. One stack max per hit. No passive tick/decay. A 3-hit attack can consume 3. | Hit ammunition, **not** Paperwork/Hex DoT. |
| **Flow** (Water) | Apprentice, positive | At the **start of a Wand Cast**, snapshot `oldFlow`. After successful resolution and Cast Focus payment, consume up to `min(oldFlow, FocusCost)` and refund that many Focus, capped at Focus maximum. New Flow gained in this Cast is *not* in the snapshot and stays for a later Cast. | Focus recycling, cannot refund resources into the current Cast. |
| **Grit** (Earth) | Apprentice, positive | Each enemy **Attack hit** with positive incoming damage consumes at most **1 Grit**, reducing that hit by **2 before Block and Ward Wax** (minimum zero). No effect on status HP loss. Thus works even when Block is plentiful. | Per-hit protection, unlike ordinary Block and persistent Ward Wax. |
| **Gust** (Air) | Apprentice, positive | At the beginning of the next player turn, after normal draw: draw **X** cards, then choose and discard **X** cards; consume the stored X Gust. Drawing respects normal draw/reshuffle rules. New Gust obtained later in that turn waits until next turn. | Next-turn hand selection, not a permanent draw buff. |

`GustResolvedThisTurn` is a boolean/event with the resolved X, available to relics for **that entire player turn**. If too few cards can be drawn or remain in hand, discard only up to the actual hand size; Gust never requests a negative count or an impossible selection. `GritMitigatedAttackDamage` fires when a stack was actually consumed and reduced positive incoming damage; shields/Block do not suppress it because Grit is upstream.

**Status conventions:** Singe is a negative status compatible with existing generic negative-status hooks (e.g. Sootglass Lens, Tarnished Bell). Flow/Grit/Gust are positive statuses. Status-trigger damage is never a direct hit and cannot consume Singe. Censure and generic status prevention are respected by the native status application pipeline. **Relic trigger distinction:** if a relic listens to an actual `DamageDealt`, `BlockGained`, `StatusApplied`, or `CardExhausted` event, a Wand Spell can satisfy that event; a relic requiring `CardPlayed` explicitly cannot. For relics saying "a card grants Block", attach the installed sourceCardInstanceId to the Block event and verify the actual engine predicate.

**Elemental parity is not mandated.** Singe supplies hits and decisions about cashing out. Flow supplies casting tempo; Grit helps survive inscription turns and converts into other resources; Gust helps select/reposition cards. Their differing rhythms are intentional.

---

## 3. Wand state and character actions

### 3.1 Persistent combat state

```text
ApprenticeCombatState
  focus: integer [0..6]                      // BD
  wandExhaustion: integer [0..exhaustionCap] // BD: cap starts at 8, may rise to 12
  wandSlots: nullable CardInstanceId[]       // default length 4; capacity hard cap 6
  castsThisTurn: integer [0..3]             // BD hard safety ceiling
  freeFirstInscriptionUsedThisCombat: bool
  burnoutUsedThisTurn: bool
  storedSpell: optional FrozenSpellEffect    // at most one baseline
  pendingOneCastEffects: flags/list           // bounded, named source
  inscriptionCountSinceLastCast: integer      // capped 2 when read by Dust Mote
  perTurnRelicCounters; perCombatCardCounters
  playedCardSequence; gustResolvedThisTurn
```

The Wand is a **class-owned state**, never a relic. `exhaustionCap` starts at **8** and can become **10/12** through explicitly authored effects; current Exhaustion is always checked against the current dynamic cap. It displays physical card instances, their IDs, order, empty gaps, modifier targeting, exhaustion projections and any reserved effect. It is reset at combat end: surviving installed cards return to their appropriate ordinary post-combat card collection; their card definitions are never permanently removed. Exhausted cards follow standard combat Exhaust rules.

**Slot semantics:** Start with four physical slots. Installed cards can occupy **any free slot** (including gaps). Empty slots have no instruction and are skipped by the default execution iterator. A card is no longer in hand/deck/discard while installed. Installing does **not** `CardPlayed`, discard, or exhaust it; it is a `CardMovedToWand` event.

### 3.2 Native actions (BD)

| Action | Timing / cost | Result / event |
|---|---|---|
| **PLAY** | Existing card Energy cost | Normal authored Play effect; after successful play of an eligible **non-Junk/non-Curse card** gain **+1 Focus** (maximum 6); still emits normal `CardPlayed` and any native events. Rites/Exhaust use native behavior. |
| **INSCRIBE** | Choose a card in hand and empty slot. **First inscription each combat free**; later **1 Focus**, no Energy. | Move physical card to Wand, **do not resolve Play text**. Emit `CardInscribed`; count toward recent insertions. No `CardPlayed`. Junk/Curses and `Unplayable` cannot be inscribed by default. |
| **CAST** | Choose target; must have 1+ occupied playable slot, `castsThisTurn < 3`, sufficient Focus, final projected exhaustion ≤8. Full Wand by default. Focus cost **2** (1–2 executed occupied instructions), **3** (3–4), **4** (5–6). | Compile deterministic execution list and preview; pay Focus once; resolve instructions; settle Burn, Ground, exhaustion, Flow refunds and post-cast shifts; emit `WandCast`, **not** `CardPlayed`. |
| **SWAP** | Once whenever Focus is available, **1 Focus** for an adjacent or any two occupied positions (user-selected); repeatable subject to Focus. | Swap physical card positions; no Play or status effect. |
| **WITHDRAW** | **1 Focus**, choose an installed card, outside a Cast. | Move it to Discard; preserves instance/upgrade. This is **not Exhaust** and not `CardPlayed`. |
| **BURN OUT** | Free action **once per turn** when Wand Exhaustion >0, outside Cast. | Exhaust one installed card; decrease Exhaustion by **3** (floor 0). Emit the existing `CardExhausted` event with source `WandBurnOut`. All related relic hooks are eligible. |

**No baseline fifth/sixth slot for free.** The Common **Spare Notch** grants +1 slot for the current combat (max 6), and Rare/other content may interact with capacity. Longer designs are *possible*, not mandatory. Bonus slots persist until combat ends and are not removed while occupied. No need to find a specific Rare to test five- or six-slot formulas (two copies of Spare Notch can suffice).

**Focus is retained between turns** up to cap; exhaustion decreases by **3 at the start of each player turn** (BD), before card draw and after previous turn's enemy actions. Cast legality uses net projected exhaustion **after** Ground/Burn reductions; cannot start a Cast that ends above limit, even if it contains a Burn at the end. Casts cannot create negative Exhaustion.

**Exhaustion calculation (BD):** `base 1 + 1 per executed heavy operation − Ground credits − Burn credits`; heavy operations initially `{Echo, Shape/Fork, Order(reverse), Keep, conditional extra spell}`. Basic Speak, simple Heighten, and ordinary single-target Clause are *not* heavy. Count a duplicated Speak result's processing as +1 heavy (one per duplication, with event safety cap). Execution of 5–6 slots adds **+1 length surcharge**. Clamp result ≥0. Cards may explicitly add/modify. This is intentionally *not* the same curve as Focus cost.

**Safety:** At most **3 Wand Casts per player turn** (BD). Maximum **24 emitted atomic combat effects per Cast** (BD) even if Echo, Fork and rare modifiers combine. A Cast that would exceed this cap is **not legal** and is shown as such in preview; never silently drop later effects. These limits are safety parameters for initial implementation, not claims of final balance.

### 3.3 The Cast transaction

1. Player selects a full chain or a legally available contiguous segment (Brass Dividers).
2. Snapshot current enemy intents, installed card instances, selected target, `oldFlow`, relevant statuses, positional effects, stored spell and one-cast flags.
3. Compile into a **finite**, non-recursive list of effect actions. Determine resolved targets, skip rules, effective instruction count, final Focus and Exhaustion, additional copies and Burn removals. Invalid casts are disabled **before** any Focus is paid.
4. Pay Focus once; execute a previously stored Spell first, if any, then listed instructions in order.
5. `Speak` effects call existing damage/Block/status systems. Their results (not entire command trees) can be stored for permitted copies.
6. Resolve Burn/automatic Exhaust with ordinary CardExhausted hooks. Settle actual Exhaustion. Apply capped Flow refund from `oldFlow` and only then any queued new Flow. Handle post-cast physical moves/refills in deterministic order.
7. Emit `WandCastResolved`, update per-turn/per-combat counters, clear recent-inscription count and display breakdown.

Enemy death during a chain: a remaining single-target hostile `Speak` retargets to the **living enemy with lowest current HP**, then stable combat index; preserve original target when alive. A target-locked special condition that becomes impossible skips its **own** effect, does not reselect an invalid target. Preview states retargeting rule and may show conditional estimates where enemy death changes outcomes.

---

## 4. Ten composable Wand primitives

**Technical operation kinds**, not cards, elemental families or ten extra statuses:

| Kind | Execution contract | Conditions / restrictions |
|---|---|---|
| `SPEAK` | Emit explicit authored effect specs: damage, Block, Ward Wax, eligible status, or other **non-copyable** safe utility. | Card author provides exact values/targets and copyability whitelist. |
| `HEIGHTEN` | Apply authored numerical adjustment or transformation to the **next eligible Speak result**; multiple heightens compose in slot order. | Does not arbitrarily magnify Draw/Energy/Gold/Focus. |
| `ECHO` | Emit a bounded copy of the **most recently resolved eligible Speak result**, scaled by the current Echo card. | Copies a frozen allowed *effect result*, not effects that generated it; cannot echo Echo, Keep, Cast or another engine command. |
| `SHAPE` | Change the **next** appropriate Speak's structure: fork, join, scatter, redirect, transform hit/Block. | Explicit compatibility list per card; preserves bounded totals unless authored cost states otherwise. |
| `CLAUSE` | Condition the following Speak / instruction on a named, snapshotted condition. | If false, skip its payload (no ghost damage), continue following instructions. |
| `ORDER` | Reorder a defined finite *unexecuted* subset (next two, remaining section, section reversed). | One transformation per source instruction per Cast; order mutations never rewind execution pointer. |
| `KEEP` | Capture a frozen eligible Speak effect into the single `storedSpell` buffer; execute before instructions on the **next successful Cast**. | Only copyable safe effects; no storage of a second Spell unless explicitly replaced/discarded. Stored spell itself cannot create Keep/Clone. |
| `GROUND` | Alter final Wand Exhaustion, sometimes with condition or Focus/Grit/Singe payment. | Changes **exhaustion**, not Energy. Reduction applied to net projected Cast exhaustion. |
| `BURN` | Execute authored one-off payload and Exhaust the source installed card during Cast settlement. | Native Exhaust event, removal once, no instant recursively triggered Cast. |
| `STOP` | End a chain early, choose segment, or install a defined post-cast state transformation. | No retroactive cancellation of already resolved effects. Rare-level modifiers may rewrite section boundaries before compilation. |

**Memory model:** `lastResolvedSpeak` is a frozen `SpeakResult` **after** its allowed numerical modifiers, target selection and splitting (the split produces an ordered atomic batch). Echo can copy the batch only if the `CopyPolicy` permits it. Each Speak declares one `primaryKind`: `damage` if it contains direct damage, else `defense` if it contains Block/Ward Wax, else `status` for status-only effects, else `utility`; this resolves Watermark/Contrast and other type tests even for mixed Spells. The `lastResolvedSpeak` does not contain position-sensitive bonuses or engine triggers; copies do **not** recheck 'first/last slot' or rebuild old modifier pipelines. Modifiers on a mixed Speak transform **only their eligible portions**: an attack Heighten changes direct damage but does not also raise attached Singe stacks; a Block Heighten changes Block only. A `JOIN` shape combines **two immediately following compatible damage SPEAK instructions** at compile time; if only one follows, it has no effect, visually marked in preview. `KEEP` captures a **result specification**, not an already-applied HP loss; it resolves at next Cast start against current legal targets. A globally stored next-Cast Spell fires on the next successful Cast **even if Brass Dividers selects a physical section that excludes its source slot**: the stored effect has already left its physical source instruction.

**No accidental amplification of shared-card Rites.** General Pool imprints are hand-authored proxies and use copyability restrictions as needed. `SPEAK` may create Flow/Gust/Draw only when explicitly marked *not copyable*, and such resource effects cannot be forked, echoed, joined or used as a recursion source.

**Unproductive modifier rule:** An instruction lacking a valid predecessor/follower becomes `FIZZLE` (displayed), with its normal slot Focus/exhaustion cost still considered unless the card says otherwise; it does not automatically search the whole program for a substitute. This makes position matter, but the preview prevents surprise losses.

**Whitespace:** Empty slots skipped. A `gapCrossed` event fires when moving from occupied index `i` to occupied `j > i+1` in the compiled physical reading order. With an `ORDER` reversal, evaluate gaps in actual compiled source order, not imaginary adjacent slots. `Bone Spacer` consumes the first eligible gap-crossing per turn.

---

## 5. Upgrade and rarity principles

- A card upgrade may improve Play, Wand, or both but does not erase the meaningful distinction between their modes.
- A Common must make sense in Act I if the player lacks its ideal supporting card.
- An Uncommon introduces a new relationship, not just a larger Heighten/Echo.
- A Rare may change a program's execution grammar, reuse an Exhausted wand card under restrictions, or turn a whole Cast's structure into its payoff.
- Card texts below use ordinary English concise enough to become in-game tooltip prose after UI formatting. **All numbers are BD.**
- `I` means inscription/installation; it is not a magic ingredient. `<first/last>` always refers to the **selected executed section**, unless explicitly 'physical Wand slot'.

## 6. Starter cards — 4 definitions, 10 copies

| ID / copies | Name · type · E | PLAY | WAND imprint | UPGRADE (PLAY / WAND) |
|---|---|---|---|---|
| `ap_candle_stub` ×4 | **Candle Stub** · Deed · 1 | Deal **6** damage; apply **1 Singe**. | `SPEAK`: deal **4** damage; if the first executed occupied slot, also apply **1 Singe**. | Damage **8** / damage **6**. |
| `ap_chalk_line` ×4 | **Chalk Line** · Working · 1 | Gain **5 Block** and **1 Grit**. | `SPEAK`: gain **4 Block**; if another Speak follows, reduce this Cast's exhaustion by **1** (once). | **7 Block** / **6 Block**. |
| `ap_ink_blot` ×1 | **Ink Blot** · Working · 1 | Draw **2**, discard **1**; gain **1 Flow**. | `HEIGHTEN`: next eligible Speak gains **+50%** damage or Block (round down); eligible status-only Speak gains **+1** stack instead. | Draw **3**, discard **1** / Heighten **+75%** (status still +1). |
| `ap_flyleaf` ×1 | **Flyleaf** · Working · 0 | Draw **1**, discard **1**; gain **1 Gust**. | `ECHO`: repeat last eligible Speak's copyable payload at **50%** (round up). | Gain **2 Gust** / Echo at **65%** (round up). |

**Tutorial beat:** Inscribe Candle Stub, then Ink Blot in an earlier slot or Flyleaf after it. The Combat UI should clearly show that `Ink Blot → Candle Stub → Flyleaf` is different from `Candle Stub → Ink Blot → Flyleaf`. The starter Ward can act as an endpoint. Ordinary Play must remain fully adequate while the player learns inscription.

---

## 7. Commons — 16, new curated pool

The old 20-Common exercise is **superseded**. The new 16 deliberately avoid four sets of 'damage plus element' variations. Five concepts were replaced outright and others given a more distinct role. The dominant reason to take each is shown in the last column.

| ID | Name · type · E | PLAY | WAND imprint | UPGRADE (PLAY / WAND) | Primary decision |
|---|---|---|---|---|---|
| `ap_wax_drop` | **Wax Drop** · Working · 1 | Apply **2 Singe** to each enemy (maximum 3 targets). | `HEIGHTEN/SHAPE—Brand`: the next eligible damaging Speak applies **1 Singe** to each enemy it actually hits (once per enemy per atomic hit group, max 3 total). | Play apply **3** / Wand max **4** total stacks. | Adds a *trigger* to later hits rather than just adding damage. |
| `ap_snuffer` | **Snuffer** · Deed · 1 | Consume up to **3 Singe** on one enemy; deal **4 damage per consumed** Singe to that enemy. | `GROUND—Quench`: consume up to **2 Singe** on selected enemy; reduce this Cast's exhaustion by **2 per stack**. | Play **5 per** / Wand may consume **3**, reduction still capped **4**. | Spend attack ammunition for relief. |
| `ap_ink_dish` | **Ink Dish** · Working · 0 | Look at top **3** draw cards, put **1** in hand and remaining on deck in chosen order; if selected card costs 2+, gain **1 Flow**. | `SPEAK—Reservoir`: when the **last eligible Speak of this Cast resolves**, gain **1 Flow** (not copyable; after-Cast queue). | Inspect **4** / gain **2 Flow**. | Deck-selection versus preparing future casting. |
| `ap_folded_leaf` | **Folded Leaf** · Working · 1 | Return **one card** from Discard to the top of Draw; gain **1 Gust**. | `KEEP`: capture the next eligible Speak as a stored Spell; it fires before the next successful Cast, and is not executed now. If the buffer is full, execute the Speak normally. | Draw **1** afterward / stored Speak gets a **+25%** numeric effect bonus, subject to copyability. | Separate a spell's setup and payoff across Casts. |
| `ap_blotting_sand` | **Blotting Sand** · Working · 1 | Remove **1 negative Status stack** from yourself; gain **5 Block**. | `SHAPE—Remedy`: if next eligible Speak would apply a **negative status** to an enemy, apply **no** such stacks; instead remove up to **2 negative status stacks** from yourself and gain **3 Block** per removed stack. Does not convert damage portion. | Remove **2** / remove up to **3**. | Turn an offensive status imprint into a corrective charm. |
| `ap_chalk_circle` | **Chalk Circle** · Working · 1 | Gain **7 Block**; if you do not Cast this turn, also gain **1 Grit** at end of turn. | `GROUND`: reduce Cast exhaustion by **2**. | **10 Block** / reduce by **3**. | Reward a deliberate quiet turn or make short Wands cheap. |
| `ap_clay_weight` | **Clay Weight** · Working · 1 | You may spend up to **2 Focus**; gain **5 Block per Focus** spent. If none spent gain **4 Block**. | `GROUND`: reduce exhaustion by **1** for each preceding executed heavy modifier, capped **3**. | **7 per Focus** / cap **4**. | Use Focus as immediate Block; stabilize genuinely complex chains. |
| `ap_masons_dust` | **Mason's Dust** · Working · 1 | Spend **1 Grit** to gain **5 Ward Wax**; if you cannot, gain **1 Grit** instead. | `SPEAK`: consume up to **2 Grit**, gaining **3 Ward Wax per stack**; if no Grit, gain **4 Block**. Non-copyable Grit removal, copyable Block only. | **7 Ward Wax** / **4 Ward Wax per Grit**. | Convert protection across time, not simply stack more Block. |
| `ap_open_casement` | **Open Casement** · Working · 1 | Swap two Wand cards for free, then draw **1**; if fewer than two occupied, gain **1 Gust** instead. | `ORDER`: swap the **next two** occupied instructions before their resolution; cannot move through an already executed instruction. | Draw **2** / additionally reduce exhaustion by **1** if swap changes which Speak is Heightened. | Reorder without removing the cards. |
| `ap_windblown_leaf` | **Windblown Leaf** · Working · 1 | Put **one card from hand** into Draw, shuffle Draw, then draw **2**; gain **1 Gust**. | `SHAPE—Scatter`: next enemy-targeted, single-target **damage** Speak hits ALL enemies at **60%** of its original damage each. Any non-damage status remains primary-target only. | Draw **3** / **75%**. | Change target geometry rather than hit count. |
| `ap_penny_whistle` | **Penny Whistle** · Deed · 1 | Deal **5 damage**; if you have played at least one other non-Junk card this turn, gain **1 additional Focus**. | `STOP—Cut`: terminate this Cast after the immediately previous Speak; if at least one Speak resolved, reduce Cast exhaustion by **2**. Remaining slots are skipped. | Deal **8** / reduction **3**. | A long Wand need not always execute in full. |
| `ap_wick_trimmer` | **Wick Trimmer** · Working · 1 | You may Exhaust **one other card from hand**; if you do, draw **2**. Otherwise gain **5 Block**. | `BURN`: gain **7 Block**, reduce final Cast exhaustion by **2** and Exhaust this installed card during settlement. | Draw **3** / **10 Block**. | Accessible early Burn, actual Exhaust synergies. |
| `ap_spare_notch` | **Spare Notch** · Working, Exhaust · 1 | Extend the Wand by **one slot** for this combat (maximum six), then Exhaust. At cap, gain **8 Block** instead. | `GROUND`: reduce exhaustion by **1**, or by **3** if the selected execution includes **5+** occupied instructions. | Cost **0** / reduce by **2** or **4**. | Long Wand support without gating capacity behind a Rare. |
| `ap_pinprick` | **Pinprick** · Deed · 1 | Deal **5 damage**; if the target intends to Attack, gain **5 Block**. | `CLAUSE`: next eligible damaging Speak gains a second small **3-damage** hit if the target intends Attack; if not, original Speak resolves without bonus. The bonus hit may consume Singe. | Deal **7** / bonus hit **5**. | Read enemy intent rather than simply add statuses. |
| `ap_cup_of_water` | **Cup of Water** · Working · 1 | Consume up to **2 Flow** to draw **1 card per stack** and gain **3 Block per stack**; if no Flow, gain **2 Flow** instead. | `SHAPE—Soak`: convert the next eligible **damage-only** Speak into **Block** equal to **80%** of its pre-mitigation damage value; original damage is not dealt. Non-copyable transformation. | **4 Block per Flow** / **100%** conversion. | Choose between hurting and shielding using the *same* installed Spell. |
| `ap_dust_mote` | **Dust Mote** · Working · 1 | Gain **5 Block**. If you inscribed a card this turn, draw **1**. | `SPEAK`: deal **3 damage +3 per card inscribed since your last successful Cast**, counting Dust Mote itself if relevant, capped at **+6**. | **7 Block** / base **5**, same cap. | Rewards rebuilding frequently, not permanent occupancy. |

### Common-pool coverage

- Reliable damage: Candle Stub, Snuffer, Pinprick, Dust Mote.
- Status architecture: Wax Drop, Snuffer, Blotting Sand.
- Hand/deck selection: Ink Blot, Flyleaf, Ink Dish, Folded Leaf, Windblown Leaf.
- Defensive economy: Chalk Line, Chalk Circle, Clay Weight, Mason's Dust, Cup of Water.
- Wand grammar: Open Casement, Penny Whistle, Wick Trimmer, Spare Notch, Folded Leaf, Ink Dish, Wax Drop.

**No numeric linear ladder** such as Cinder → Candle Stub → stronger Candle Stub. `Cinder` is **removed**. The previously proposed `Soot in the Eye`, `Red Wax`, `Wellwater`, `Wet Page`, `Lime Dust`, `Paperweight`, `Window Draught`, `Loose Sheets`, `Hat in the Wind` and similar first-draft filler are not additional cards; their worthwhile verbs have been consolidated into the present pool.

---

## 8. Uncommons — 14, differentiated mechanics

| ID | Name · type · E | PLAY | WAND imprint | UPGRADE (PLAY / WAND) |
|---|---|---|---|---|
| `ap_split_wick` | **Split Wick** · Working · 1 | The next normal direct-damage card played this turn splits its largest hit into **2 hits** of **60%** damage each; statuses attached to that hit apply only once. | `SHAPE—Fork`: next eligible damage Speak becomes **2 × 60%** damage, with a fresh Singe check per hit. | **2 × 70%** / **2 × 70%**. |
| `ap_cracked_slate` | **Cracked Slate** · Deed · 1 | Deal **9 damage**. You may consume **1 Grit** to deal **+7 damage**. | `SHAPE—Join`: the **next two consecutive damage SPEAKS** become one hit equal to their combined base damage **+3**; their statuses apply once each, with separate prevention checks. | Damage **12**, Grit bonus **9** / combined hit **+5**. |
| `ap_crosswind` | **Crosswind** · Working · 1 | Move up to **3 Singe** from one enemy to another; if there is no second enemy, gain **2 Gust** instead. | `SHAPE—Crossfire`: on the next eligible *multi-hit damage Speak*, alternate hit targets between **two selected living enemies**; on a lone enemy, all hits go to it. No additional hits. | Move up to **5** / each alternated target receives **1 additional Singe** total if hit. |
| `ap_wrong_way_round` | **The Wrong Way Round** · Working · 1 | Draw **2**, then choose up to **2 occupied Wand cards** to swap for free; gain **1 Gust** if you swapped. | `ORDER`: reverse the execution order of all **remaining unexecuted instructions** of the selected segment, exactly once. Reduce exhaustion by **1** if there are 2+ to reverse. | Draw **3** / reduction **2**. |
| `ap_rubbing` | **Rubbing** · Working · 1 | Choose a **non-Temporary Working other than Rubbing** in Discard; create a **Temporary** copy in hand that Exhausts after Play (or on removal from the Wand). Original stays in Discard. | `KEEP—Rubbing`: store **50%** of the last eligible Speak in the single `storedSpell` buffer, but trigger it **once on your next BURN OUT**, instead of next Cast. If buffer occupied, gain **4 Block** instead. | Copy's Energy cost −1 (min 0) / stored result at **70%**. |
| `ap_watermark` | **Watermark** · Working · 1 | Choose: gain **2 Flow**; **or** consume 1 Flow to reduce Wand Exhaustion by **3** and gain **1 Gust**. | `HEIGHTEN—Contrast`: if the previous resolved Speak was of a **different effect kind** (`damage`, `defense`, `status`) from the next, Heighten that next Speak by **+50%**; otherwise gain **1 Flow after Cast**. | Gain **3 Flow** / **+75%**. |
| `ap_torn_flyleaf` | **Torn Flyleaf** · Working · 1 | Withdraw one installed card **to hand for free**, then draw **1**; can be used before the Wand is filled. | `ORDER—Slip`: **after this Cast**, Torn Flyleaf moves into the **nearest free physical slot to its right**, if one exists. If it moves, gain **1 Focus**. No effect if none free. | Draw **2** / may instead move left or right into nearest free slot. |
| `ap_footnote` | **Footnote** · Working · 0 | Choose: draw **1 card**; or reduce Wand Exhaustion by **2**. | `CLAUSE—Final Breath`: if the Cast Focus payment leaves **0 Focus**, the next eligible single-hit damage Speak is split into **two 60% hits**; otherwise it executes once normally. | Draw **2** / **two 70% hits**. |
| `ap_candle_both_ends` | **Candle at Both Ends** · Deed · 1 | Exhaust another card from hand; draw **3** and gain **1 Focus**. If no other card can be exhausted, deal **9 damage** instead. | `BURN`: repeat the most recent eligible **damage** Speak at **150%** strength, then Exhaust Candle at Both Ends. If no previous Speak, deal **9 damage** and Burn. | Draw **4** / repeat at **175%** (no predecessor: **12**). |
| `ap_firebreak` | **Firebreak** · Working · 1 | Remove up to **3 Singe** from a target. Gain **1 Grit per stack** removed (max 3); if none removed, gain **6 Block**. | `GROUND`: if **any Singe was consumed by direct hits earlier this Cast**, reduce final Exhaustion by **3** and gain **1 Grit**; otherwise reduce by **1**. | Gain **+3 Block** after removing Singe / reduction **4** if triggered. |
| `ap_kiln_shelf` | **Kiln Shelf** · Working, Exhaust · 2 | Raise your Wand Exhaustion cap by **2 for this combat** (maximum cap 12). Exhaust. If already at 12, gain **12 Block** instead. | `GROUND—Cap`: the **heavy-operation** portion of this Cast's exhaustion is capped at **2**; if it would exceed 2, pay **1 additional Focus** in the preflight. | Cost **1** / cap heavy portion at **1** for same extra Focus. |
| `ap_borrowed_stone` | **Borrowed Stone** · Working · 1 | Spend **1 Grit** to gain **1 Energy and 2 Focus**; if no Grit, gain **8 Block**. | `SPEAK`: gain **4 Block +2 per Wand card Exhausted earlier this combat**, maximum **+8**. If at least two were Exhausted, reduce exhaustion by **1**. Block is copyable; the history check isn't. | **10 Block** fallback / base **6 Block**, same bonus. |
| `ap_cold_ash` | **Cold Ash** · Working · 1 | Consume up to **3 Singe** from one enemy. Reduce its next Attack action's total damage by **2 per consumed stack** (bounded at zero). If it does not Attack before its next action resolves, this reduction expires. | `SPEAK`: gain **1 Grit for every 2 Singe** consumed by direct Wand hits **earlier in this Cast**, maximum **2 Grit**; if no Singe was consumed, gain **5 Block**. Not copyable when producing Grit. | Reduction **3 per** / round up Singe/2, max 2 Grit. |
| `ap_seal_press` | **Seal Press** · Working · 1 | Spend up to **2 Grit**; gain **4 Ward Wax per spent stack** and **4 Block**. If none, gain **7 Block**. | `KEEP—Shield Seal`: capture next eligible **defensive SPEAK** at **75%** into the one storedSpell slot, to trigger **just before the first enemy Attack hit this enemy turn** (rather than now). It expires at start of your next turn if no attack arrives. If buffer occupied, execute the Speak normally. | Ward Wax **5 per Grit** / stored defense **100%**. |

### Uncommon audit results

- `Second Copy` **renamed and rebuilt as Rubbing**: delayed copy requires an actual **Burn Out**, rather than being a second simple Echo.
- `Watermark` **no longer duplicates Folded Leaf's Keep**: it rewards changing categories between Spells, or accumulates Flow.
- `Footnote` is a concrete Focus-sensitive Clause, not a placeholder for an unspecified condition.
- `Firebreak`, `Cold Ash` and `Snuffer` all consume or observe Singe **at different times and for different outcomes**: intentional pre-cast cashout, during-cast defensive harvest, and post-hit Grounding.
- `Kiln Shelf` supports unusually long programs by **raising the cap or bounding heavy-operation load**, not just being an enlarged Chalk Circle.
- `Seal Press` is a genuinely reactive enemy-turn defensive preparation, not yet another repeat at Cast time.
- `Ink Under Glass` and `False Entry` are **removed**: too redundant, or require a second generic Play interpretation for most Spells.

---

## 9. Rares — eight build-defining exceptions

Each Rare has a useful Play even without a complex Wand. No Rare is merely 'large Heighten' or 'Echo again'.

| ID | Name · type · E | PLAY | WAND imprint | UPGRADE (PLAY / WAND) |
|---|---|---|---|---|
| `ap_masters_correction` | **Master's Correction** · Working · 1 | Choose another card in hand: either mark its **next normal Play this turn as costing 1 less Energy** (expires at end of turn), or **immediately INSCRIBE** it for 0 Focus into a chosen free slot; then draw **1**. | `ORDER—Errata`: once per Cast, when the **next eligible SPEAK** is reached, you may use a **frozen copyable Speak template of another installed card** as its effect **instead** (choose source during preview; its source card does not execute a second time). Does not duplicate Rites/resources. | Discount **2 Energy** / chosen replacement's base numeric value **+25%**. |
| `ap_final_examination` | **Final Examination** · Deed · 2 | Spend up to **all currently held Focus** after paying this card's Energy; deal **6 +4 per Focus spent** damage and gain **4 +3 per Focus spent** Block. | `ORDER—Oral Exam`: while this card is in a selected Cast section, **choose at preflight** whether that section is read forward or backward; reversing adds **+1 exhaustion**. The card itself is a rule-bearing no-op, not a second Cast. | Damage **+2 per Focus** / reversing costs **0** extra exhaustion. |
| `ap_lessons_ash` | **Lessons in Ash** · Working · 1 | Exhaust up to **2 other cards** from hand; draw **1 per card** exhausted and gain **1 Focus per card** (on top of normal Play Focus), max 2. | `KEEP—Ash Trace`: after this card resolves, your **next BURN OUT** this combat additionally emits one fixed **8-damage** hostile Speak on its former position's chosen target at end of that Burn Out action. One charge only; **does not occupy a Wand slot** or storedSpell. Not echoable. | Draw **+1** in total if any exhausted / trace damage **12**. |
| `ap_glass_rod` | **Glass Rod** · Deed, Exhaust · 2 | Your next Cast this combat gets **+50%** copyable damage and Block, but gains **+3 Exhaustion**; draw **1**. Exhaust the played card. | `SHAPE—Prism/BURN`: for the next eligible **damage or Block Speak**, produce **both**: **70%** of its original effect and **70%** of that same numerical magnitude in the opposite form (damage ↔ Block). After resolving, Exhaust Glass Rod. If no eligible Speak follows, create **8 Block** and still Burn. | Damage/Block **+65%** / paired portions **85%** each. |
| `ap_second_hand` | **Second Hand** · Working, Exhaust · 2 | Return **one card** from your combat Exhaust pile to hand as a Temporary copy; after its next Play or Inscribe-and-Exhaust cycle it Exhauts again. Exhaust Second Hand. | `BURN/RECALL—once per combat`: when a Wand card is Burned Out, you may take **one eligible exhausted non-Junk card** and inscribe it in the freed slot **after action settlement**; this ability is armed by Second Hand while installed. The recalled card is marked `RecallOnce`, returns to Exhaust if removed from Wand again. | Cost **1** / may recall at the first **two distinct** Burn Outs per combat. |
| `ap_fourfold_lesson` | **The Fourfold Lesson** · Working · 2 | Deal **7 damage** and apply **1 Singe**; gain **1 each Flow, Grit, Gust**. This is a deliberate, self-contained four-element Rare. | `CLAUSE—Fourfold`: track which of `{Singe application, Flow gain, Grit gain, Gust gain}` have **actually occurred during this player turn** (through normal Play or Wand effects, excluding repeated/copy-generated applications). At the end of this selected section, if **2 kinds**, gain **5 Block**; if **3 kinds**, also remove **2 exhaustion**; if **4 kinds**, also gain **1 Focus** after Cast. At most once per Cast, the card itself adds no element. | Deal **10** and gain **2 Gust** / at 2 kinds gain **8 Block**, otherwise same. |
| `ap_perfect_copy` | **A Perfect Copy** · Working · 1 | Rearrange all occupied Wand cards for **0 Focus**, then draw **2**. | `ORDER—Palimpsest`: **after** a full-Wand Cast resolves, rotate the physical installed cards **one occupied position to the right** (leaving physical gaps in place). Never executes a card a second time in the same Cast. If only part of Wand cast, no rotation. | Draw **3** / may choose left or right rotation. |
| `ap_last_word` | **The Master's Last Word** · Deed, Exhaust · 2 | Spend your currently stored Focus (up to **6**) and deal **10 +5 per Focus** damage to one enemy; gain **3 Grit** if at least 4 Focus were spent. Exhaust. | `STOP—Final Word`: must be the **last executed instruction** of the selected section; consume **remaining Focus after the Cast's base Focus cost** (before any Flow refund). For each **distinct operation kind** successfully executed earlier in this Cast, deal **6 damage** to target (max 5 kinds). Spend 1 extra Focus per extra hit beyond the first, as available; when no remaining Focus, make only the first hit. Adds **+2 exhaustion**. Has no effect if improperly positioned (shown in preview). | Damage **+7 per Focus** / **8 damage** per eligible hit, same hit cap. |

**Rare audit decisions:** `Closed Book` and `The Long Sentence` are **cut**. Their worthwhile parts (safe truncation, extended capacity) exist more cleanly in `Penny Whistle`, `Spare Notch` and `Brass Dividers`. `Glass Rod` is no longer a stronger `Candle at Both Ends`: it converts **one effect into both attack and defense** and physically breaks. `Lessons in Ash` does not refill a slot (that is Ash Bookcord's job), and `Second Hand` is the explicit limited return-from-Exhaust exception. `The Fourfold Lesson` rewards real elemental effects during execution; it is not a Witch-like recipe or four fixed tags on installed cards.

---

## 10. Character relics — nine

| Rarity | ID / Name | Exact conceptual effect (BD) | Build it supports / distinct role |
|---|---|---|---|
| **Common** | `ar_sooty_ferrule` — **Sooty Ferrule** | **Once per player turn**, after a Wand Cast in which a direct Wand hit actually **consumed Singe**, gain **1 Focus**. Trigger after Cast (not recursively inside it). | Multi-hit offense, Short Wand. |
| **Common** | `ar_knotted_wristcord` — **Knotted Wristcord** | After the first successful Cast each player turn with **1 or 2 occupied executed Wand cards**, choose one installed card in that executed section and move it **to hand without paying WITHDRAW**. After Cast only; does not generate a `CardPlayed` event. | Short Wand, reusable cards and flexible tempo. |
| **Uncommon** | `ar_blue_inkstone` — **Blue Inkstone** | **Once per player turn**, when Flow from the start-of-Cast snapshot would refund Focus, choose up to **2 refund points**: for each chosen point, **reduce net Wand Exhaustion by 1 instead of refunding 1 Focus**. The chosen Flow is consumed. | Real choice between immediate tempo and future capacity. |
| **Uncommon** | `ar_bent_nib` — **Bent Nib** | **Once per player turn**, immediately after INSCRIBE, optionally swap the inscribed card with an **adjacent occupied card for free**. If `GustResolvedThisTurn`, this free swap may be with **any occupied Wand card**, not just adjacent. | Reconfiguration; works with Gust even though Gust was consumed at turn start. |
| **Rare** | `ar_brass_dividers` — **Brass Dividers** | **Once per player turn**, you may CAST **any contiguous physical slot interval** containing at least one occupied card instead of the whole Wand. Cast Focus cost uses **executed occupied instructions**; the section cannot import still-pending modifiers from slots outside it; **the global storedSpell buffer is independent of physical slot selection and still triggers normally**. | Modular Wand, separate short offense/defense; no extra capacity granted. |
| **Rare** | `ar_ash_bookcord` — **Ash Bookcord** | **Once per turn**, after a Wand card was Exhausted via BURN or BURN OUT, you may INSCRIBE one eligible hand card into that now-empty physical slot **for 0 Focus** after the triggering action finishes. No retroactive execution. | Burning and replenishing the Wand. Competes safely with Second Hand. |
| **Shop** | `ar_feather_catcher` — **Feather Catcher** | When resolving Gust at turn start, **once per turn** you may INSCRIBE one of the cards you would discard into a free Wand slot instead, **for 0 Focus**. Counts as one of the X cards selected for Gust discard, not additional draw. | Gust becomes Wand construction, not only hand filtering. |
| **Shop** | `ar_chalked_thumbstall` — **Chalked Thumbstall** | First time in an enemy turn Grit reduces positive Attack damage, grant **one 2-point Ground credit** to your next successful Wand Cast; credit persists until used, max one, cannot self-trigger. | Defensive recovery into a long Cast. Does not depend on taking HP damage or zero Block. |
| **Shop** | `ar_bone_spacer` — **Bone Spacer** | **Once per player turn**, first Cast whose compiled physical path crosses an **empty slot between two occupied executed slots** gives the next eligible Speak **+25% damage or Block** and **+1 exhaustion**. Triggered by actual physical gap traversal; no free benefit for an empty trailing slot. | Intentionally sparse Wand, not necessarily filling all slots. |

### Relic conflict handling

- `Ash Bookcord` and `Second Hand`: if both offer to occupy the **same vacancy** after a Burn Out, the player selects **one** eligible replacement; no second refill and no extra slot. Unchosen optional effect keeps its once-per-turn charge if it did not fire; a limited per-combat charge stays available until used.
- `Knotted Wristcord` transfers to hand only **after** the Cast has completely resolved and after any legitimate Burn of that same card. Cannot retrieve an already Exhausted instance.
- `Blue Inkstone`: operate on `oldFlow`, not new Flow gained by a Speak during the Cast.
- `Bent Nib`: uses the resolved-Gust event of the current turn. Do not test `gustStacks > 0`, as that misses Gust spent at turn start.
- `Bone Spacer`: a visual gap only matters if the active compiled path crosses it; a segment cast within one side of a gap does not.
- All relics obey the game's existing class eligibility and shop-only acquisition rules; Apprentice-specific relics are not in Bureaucrat/Witch pools.


---

## 11. General Pool compatibility: explicit 50-card Wand profile registry

**Source snapshot:** `/mnt/data/archive.json`, export dated **2026-10-03**; 50 actual General Pool card names and IDs. If the repository changes, reconcile before implementing. The ordinary `play` text, rarity, upgrade, energy and the existing Bureaucrat/Witch handling are **not modified**. These are **proposed Apprentice-only Wand proxy definitions**, separate from the source game's existing cards. Their small numbers are BALANCE DRAFT.

**Purpose:** no blank General-Pool inscriptions; no automated attempt to convert arbitrary text or a full Rite into a repeatable Wand program. A proxy is intentionally narrower than the full card. All 50 are `inscribable: true` by default unless a later combat-eligibility audit discovers a hard engine constraint.

**Copy policy:** Yes = the specified pure numeric/safe subset may be copied by Echo/Fork; No = cannot be copied or forked because it manipulates resources, triggered effects, state, or a one-use conversion. Partial = only named components are copyable. Any copied subeffect fires native damage/Block/status events but never `CardPlayed`.

| General card / registry key | Proxy kind | Apprentice Wand imprint (BD) | Copy? |
|---|---|---|---|
| **Absolute Interdict** (`absolute_interdict`) | `GROUND` | Reduce this Cast's exhaustion by 2 if any Censure actually prevented a status this turn; otherwise gain 2 Block. | No |
| **Black Tribunal** (`black_tribunal`) | `SPEAK` | Deal 7 damage plus 2 per different negative Status on target (up to 5 kinds). | Yes |
| **Blacklisted** (`blacklisted`) | `SPEAK` | Apply 2 Censure to target, +1 if it has a positive Status. | Yes |
| **Blood Marginalia** (`blood_marginalia`) | `SPEAK` | Apply 2 Citation and 1 Blood Ink to target. | Yes |
| **Blood Redaction** (`blood_redaction`) | `SHAPE` | On next negative-status Speak, convert up to 2 applied stacks to 2 Blood Ink each on the same enemy. | No |
| **Blood Tithe** (`blood_tithe`) | `SPEAK` | Deal 6 damage, +3 if target carries Blood Ink. | Yes |
| **Borrowed Candle** (`borrowed_candle`) | `SPEAK` | After Cast, draw 1 card. | No |
| **Candle Cathedral** (`candle_cathedral`) | `SPEAK` | Gain 3 Ward Wax. | Yes |
| **Compound Indictment** (`compound_indictment`) | `SPEAK` | If target has at least 3 different negative Status types, add 1 stack to up to 3 of them. | No |
| **Consecrated Testament** (`consecrated_testament`) | `SPEAK` | Gain 3 Ward Wax if any enemy lost HP from a Status earlier in this Cast; otherwise gain 2 Block. | No |
| **Contempt Finding** (`contempt_finding`) | `SPEAK` | Remove up to 2 Citation from target; gain 3 Block per stack removed. | No |
| **Countermanded Grace** (`countermanded_grace`) | `SPEAK` | Gain 2 Censure on yourself. | No |
| **Crossed Sigil** (`crossed_sigil`) | `SPEAK` | Remove 1 negative Status stack from self; apply 1 Censure to target. | No |
| **Crown Repossession** (`crown_repossession`) | `SPEAK` | Deal 10 damage; remove up to 8 remaining enemy Block; apply 2 Lien. | No |
| **Dawn Summons** (`dawn_summons`) | `SPEAK` | Deal 8 damage, +4 if first executed Speak in the Cast. | Yes |
| **Debt Ouroboros** (`debt_ouroboros`) | `SPEAK` | Apply 3 Lien. | Yes |
| **Exemplary Sentence** (`exemplary_sentence`) | `SPEAK` | Remove up to 2 Citation from one target; ALL enemies lose 3 HP per removed stack. | No |
| **False Signature** (`false_signature`) | `HEIGHTEN` | Next eligible damage or Block Speak +25%; adds 1 Wand exhaustion. | Yes (the later result) |
| **Foreclosure** (`foreclosure`) | `SPEAK` | Deal 5 damage; resolve up to 2 existing Lien on target. | No |
| **Forfeit Seal** (`forfeit_seal`) | `SPEAK` | Deal 5 damage; if enemy still has Block, apply 2 Lien. | Yes (damage only) |
| **Grand Citation** (`grand_citation`) | `SPEAK` | Deal 7 damage to ALL enemies; enemies with Citation take 2 additional damage. | Yes |
| **Grand Dispensation** (`grand_dispensation`) | `SPEAK` | Choose 1 at Cast preview: deal 10 damage to target OR gain 10 Block. | No |
| **Grave Lien** (`grave_lien`) | `SPEAK` | Deal 6 damage and apply 4 Lien. | Yes |
| **Hemal Audit** (`hemal_audit`) | `SPEAK` | Deal 8 damage, then resolve up to 2 Blood Ink triggers on target. | No |
| **Last Office** (`last_office`) | `SPEAK` | For up to 3 absent status types among Paperwork, Doubt, Seal, Lien, Citation, deal 4 damage each. | Yes (damage only) |
| **Malediction Review** (`malediction_review`) | `SPEAK` | Gain 5 Block; choose gain 1 Censure on self OR apply 1 Censure to target. | No |
| **Moonlit Counterfeit** (`moonlit_counterfeit`) | `ECHO` | Repeat the previous eligible damage or Block Speak at 50%. | Yes (copied outcome) |
| **Mortgage Sigil** (`mortgage_sigil`) | `SPEAK` | Apply 2 Lien. If enemy gains Block before next turn, apply 1 additional Lien once. | No |
| **Mortgaged Aegis** (`mortgaged_aegis`) | `SPEAK` | Gain 9 Block and 2 Lien on yourself. | No |
| **Notary Beetle** (`notary_beetle`) | `HEIGHTEN` | Next negative-status applying Speak gets +1 stack of its first negative Status, if eligible. | Yes (later result) |
| **Oath of Refusal** (`oath_of_refusal`) | `SPEAK` | Gain 2 Censure on yourself and 3 Block. | No |
| **Proxy Curse** (`proxy_curse`) | `SPEAK` | Remove 1 negative Status stack from self; apply 2 Blood Ink to target if one was removed. | No |
| **Reciprocal Edict** (`reciprocal_edict`) | `SPEAK` | Apply 2 Censure to target; gain 2 Block. | Yes (Censure/Block only) |
| **Sanctioned Charm** (`sanctioned_charm`) | `SPEAK` | Gain 5 Block and 1 Censure on yourself. | Yes (Block only) |
| **Sanguine Errata** (`sanguine_errata`) | `SPEAK` | Apply 3 Blood Ink; remove up to 1 other negative Status stack from the target. | No |
| **Sealed Mantle** (`sealed_mantle`) | `SPEAK` | Gain 5 Block; if an enemy Attacks and you take no unblocked Attack HP loss this turn, gain 1 Ward Wax. | No |
| **Seizure Writ** (`seizure_writ`) | `SPEAK` | Deal 7 damage, remove up to 8 enemy Block and apply 1 Lien per 4 Block removed (max 2). | No |
| **Silent Hearing** (`silent_hearing`) | `SPEAK` | Apply 2 Citation; if target intends Attack, gain 4 Block. | Yes (Citation/Block only) |
| **Sovereign Prohibition** (`sovereign_prohibition`) | `SPEAK` | Gain 2 Censure on self and apply 1 Censure to ALL enemies. | No |
| **Standing Citation** (`standing_citation`) | `SPEAK` | Apply 3 Citation. | Yes |
| **Tallow Judgment** (`tallow_judgment`) | `SPEAK` | Consume up to 3 Ward Wax; deal 5 +3 per consumed Ward Wax damage. | No |
| **Tallow Reserve** (`tallow_reserve`) | `SPEAK` | Lose up to 4 current Block; gain 2 Ward Wax if full 4 were lost. | No |
| **Usurer's Moon** (`usurers_moon`) | `SPEAK` | Apply 2 Lien and 1 Citation to target. | Yes |
| **Vein Register** (`vein_register`) | `SPEAK` | Apply 2 Blood Ink to target if any other Status lost a stack on it earlier this Cast; otherwise apply 1. | No |
| **Vital Census** (`vital_census`) | `SPEAK` | Deal 6 damage to ALL enemies; enemies carrying Blood Ink lose 2 additional HP. | No |
| **Votive Covenant** (`votive_covenant`) | `SPEAK` | Gain 4 Ward Wax. | Yes |
| **Wax Indemnity** (`wax_indemnity`) | `SPEAK` | Gain 3 Ward Wax. If you have lost HP this combat, heal 1 of that HP. | No |
| **Wax Reliquary** (`wax_reliquary`) | `SPEAK` | Gain 3 Ward Wax and prevent 1 point of the next Ward Wax decay. | No |
| **Waxen Surety** (`waxen_surety`) | `SPEAK` | Gain 4 Ward Wax. | Yes |
| **Witchmark Citation** (`witchmark_citation`) | `SPEAK` | Apply 3 Citation to target. If target intends non-damaging action, draw 1 after Cast. | Yes (Citation only) |

### General proxy authoring requirements

1. All 50 cards need a `wandImprint` child in an **Apprentice-only content overlay** (or separate registry keyed by stable card ID), not a mutation of General Play definitions.
2. **Upgrade rule for General Pool proxies (complete initial canon): the Wand proxy remains exactly the same for an upgraded copy.** The native Play side gains its already-authored upgrade, so an upgraded General card still creates a different Play-versus-Inscription decision. This avoids 50 extra low-value proxy upgrades and keeps the 50-card compatibility overlay deterministic. A future balance pass may author per-card Wand upgrades, but they are **not required by v0.4**.
3. General Pool card Rites are **not installed as persistent Rites**. Their Wand proxies are small Speak/Modifier effects only.
4. Some status names (`Paperwork`, `Doubt`, `Seal`, `Lien`, `Citation`, `Blood Ink`, `Censure`) are legacy shared-engine statuses. Wand applications obey their native mechanics and existing stack caps.
5. General cards have no additional Fire/Water/Earth/Air family tags. A proxy can create an elemental status only if explicitly specified; do not forcibly map all General effects into four elements.
6. The presence of existing `Exhaust` in a General Play card does not cause it to Exhaust simply because its Wand proxy CASTS; Exhaustion only occurs via an explicit `BURN`, `BURN OUT`, or separate authored exception.

---

## 12. Executable Wand examples (numbers BD)

These are *not hidden recipes*. They exercise the interpreter with recognizable, player-buildable compositions. `→` is actual execution order, `□` is a physically empty slot. Unless stated otherwise, values use non-upgraded card definitions and an eligible enemy without defensive mitigation. All examples assume sufficient Focus and admissible Exhaustion, and they reset one-cast counters between examples.

### 12.1 First lesson: a boosted attack, then an Echo

`Ink Blot → Candle Stub → Flyleaf` (3 instructions, Cast Focus 3).

1. Ink Blot arms Heighten (+50%).
2. Candle Stub SPEAK hits for 4 × 1.5 = **6 damage**. It is not the first occupied slot, so it does not apply positional Singe.
3. Flyleaf copies the frozen eligible 6-damage result at 50% = **3 damage**.
4. Total **9 direct damage** across 2 hits. Baseline exhaustion: 1 +1 Echo +1 duplicate result = **3** before reductions.

Compare `Candle Stub → Ink Blot → Flyleaf`: Candle Stub first deals 4 and applies 1 Singe; Ink Blot arms a modifier without a later Speak; Flyleaf copies the Candle result (up to its allowed direct damage and status copying limits). The wasted Heighten is visible before Cast; the same cards produce different results.

### 12.2 Chalk comes after fire

`Candle Stub → Ink Blot → Chalk Line → Flyleaf` (4 instructions, Focus 3).

1. Candle Stub first: **4 damage +1 Singe** on target.
2. Ink Blot strengthens Chalk Line's **4 Block** to **6 Block**.
3. Flyleaf copies that 6-Block defensive Speak at 50% = **3 more Block**.
4. Result: 4 direct damage, 1 Singe, 9 Block. The next Speak condition on Chalk Line is false: it has no following Speak, so no extra Ground.

This basic distinction between **what is in the Wand** and **where it sits** should be shown in the tutorial.

### 12.3 Singe ammunition and multiple hits

`Split Wick → Candle Stub → Flyleaf` (3 instructions).

- Split Wick forks Candle Stub's 4 damage into **2 × 3 damage** (60%, rounded up).
- Each hit can consume one Singe already on the target (adding 2 per hit), so two Singe turn 3+3 into **5+5**.
- Flyleaf may copy the two-hit result as a bounded batch at half strength. Its duplicate hits may consume further Singe, but may not regenerate Fork/Echo commands.
- **Why this matters:** Singe amplification is based on the *number of hits*, not a damage multiplier on the whole Cast.

### 12.4 Turn the wound into a shield

`Cup of Water → Candle Stub` (2 instructions).

- Cup of Water's Wand imprint converts the next damage-only Speak into defensive Block.
- Candle Stub normally deals 4 damage; here it instead gives **3 Block** (80% of 4, rounded down) and no damage.
- Any status attached to the converted damage-only Speak is **not** created, unless an explicit card says otherwise. This is a deliberate trade-off.
- Move Cup of Water to the *end* and it cannot retroactively convert Candle Stub.

### 12.5 A delayed attack across two Casts

Cast A: `Folded Leaf → Candle Stub` (2 instructions, Focus 2).

- Folded Leaf stores Candle Stub's eligible **4-damage** result instead of executing it. No immediate hit.
- Buffer contains one `StoredSpell{trigger=next_cast, value=4 damage}`.

Cast B (on the next successful Cast, same Wand allowed):

- The frozen spell hits for **4 damage at Cast start**, before processing the program.
- Folded Leaf may store Candle Stub again later in the program; it cannot recursively store or duplicate the previously released stored spell.
- If the next Cast fails legality checks, the stored Spell is **not** consumed.

**Counterplay:** the apprentice can choose when to release the attack by choosing when to Cast; this is not the Witch's fixed three-ingredient recipe.

### 12.6 A prepared ward against an incoming attack

`Seal Press → Chalk Line` (2 instructions, Focus 2).

- Seal Press captures 75% of Chalk Line's 4 Block, i.e. **3 Block**, and stores it with `trigger=next_enemy_attack`.
- When the first enemy Attack hit would arrive in the upcoming enemy phase, the Ward is applied **before** Grit, Block and Ward Wax mitigation, then consumed.
- If no enemy attacks before the next player turn starts, the Seal expires unused.
- A normal damage Echo cannot copy this reactive event; it is a Keep buffer with a different trigger.

### 12.7 Four slots, only a chosen half: Brass Dividers

Physical Wand: `Ink Blot | Candle Stub | Clay Weight | Chalk Line`.

- **Offensive interval 1–2:** Heighten then 6-damage attack; 2-card Focus cost **2**, not 4-card cost 3. The other two cards do not execute.
- **Defensive interval 3–4:** Clay Weight provides little or no Grounding (no preceding heavy modifier in **this segment**); Chalk Line gives 4 Block. Again 2-card Focus cost **2**.
- **Whole-Wand Cast:** both halves execute, with different cost and exhaustion; card effects outside the chosen section do not leak into the smaller Cast.

This relic should create actual modular strategies, rather than another general damage multiplier.

### 12.8 Burn, then refill: Ash Bookcord

Physical Wand: `Candle Stub | Candle at Both Ends | Chalk Circle`.

- Candle Stub deals 4.
- Candle at Both Ends repeats the frozen damage at 150% = **6 more damage**, then **Burns** itself.
- Chalk Circle supplies Ground.
- The second physical slot becomes empty **at Cast settlement**, not partway through the current program.
- Ash Bookcord may now install one card from hand there for **0 Focus**, **without executing it retroactively**. During the next Cast a new program is read.
- With Lessons in Ash active, a later **Burn Out** additionally produces its marked 8-damage trace, independent of whether a card refills the vacancy.

### 12.9 A sparse Wand: Bone Spacer

Physical Wand `Ink Blot | □ | Candle Stub | Flyleaf` (3 occupied, Focus 3).

- The compiled path traverses a true physical gap from slot 1 to slot 3.
- Bone Spacer adds +25% damage or Block to the **next eligible Speak** and +1 exhaustion, at most once per turn.
- Ink Blot also adds +50%. Their ordered multipliers apply to Candle Stub; Flyleaf then Echoes the already calculated permitted Speak result.
- A fully filled four-slot Wand cannot claim this gap bonus; neither can a one-card Wand with only trailing empty slots.

### 12.10 Self-rotation: A Perfect Copy

Start physical Wand: `Ink Blot | Candle Stub | Flyleaf | A Perfect Copy`.

After the full Cast resolves, the four **remaining installed** cards rotate right one *occupied position*:

`A Perfect Copy | Ink Blot | Candle Stub | Flyleaf`.

After the next full Cast they rotate again:

`Flyleaf | A Perfect Copy | Ink Blot | Candle Stub`.

Different iterations have different ineffective/effective modifiers; preview is updated after every rotation. No card physically moves during instruction resolution, and no automatic extra Cast occurs.

### 12.11 A genuinely long formula (no Rare required)

Play **two Spare Notch cards** during combat, raising capacity to **6**. On later turns install:

`Ink Blot → Split Wick → Candle Stub → Flyleaf → Clay Weight → Chalk Line`.

- Six occupied instructions, **Focus 4**.
- Heighten affects damage, Split Wick turns it into small hits, Flyleaf repeats the bounded result.
- Clay Weight looks backward at already resolved heavy modifiers and recovers some Exhaustion.
- Chalk Line provides defensive payoff at the end.
- Contrast with a four-slot 'burst' variant that omits Clay Weight and Chalk Line; the long version can be more sustainable **only if** the Ground and protection justify its greater Focus cost and length surcharge.
- `Kiln Shelf` can lift the exhaustion ceiling or contain the heavy modifier load, but is **not required** to reach six slots.

### 12.12 General-Pool synergy: dangerous but bounded

`Notary Beetle → Sanguine Errata → Flyleaf`.

- The General-Pool Notary Beetle proxy Heightens the next **negative status** application by one eligible stack.
- Sanguine Errata proxy applies **3 Blood Ink** and removes an additional negative stack under its own rule; because its imprint is **non-copyable**, Flyleaf cannot echo the full status-removal/replacement transaction.
- The Cast preview marks Flyleaf as `FIZZLE` if no other preceding copyable Speak exists.
- The card is still attractive as ordinary Play, where its native text is unchanged.

### 12.13 Fourfold without ingredient recipes

The Fourfold Lesson inscribed in Wand evaluates **elemental status applications actually made during the current player turn** (excluding copied status results). For instance, ordinary Play effects can have given you Grit and Gust and applied Singe; a later installed Ink Dish may queue Flow after this Cast, which **does not count as earlier in the Cast**. Three valid element types yield the three-type rewards, not four. It does not care about the installed cards' 'family': only events that actually happened.

### 12.14 Rubbing versus Folded Leaf

- Folded Leaf stores the next Spell for **next successful Cast**.
- Rubbing stores a reduced previous Spell for the **next voluntary Burn Out**. This Spell can thus wait across turns while the player keeps using other Cards, but competes for the **one shared storedSpell slot**.
- If one already occupies that slot, the other receives its documented fallback and does not overwrite the first. This avoids accidental double-storing.

### 12.15 Two-turn starter trace (actual Focus and Exhaustion bookkeeping)

Initial conditions: Focus **1/6**, Exhaustion **0/8**, 3 Energy, Wand empty, hand includes two physical Candle Stub instances, Ink Blot and Flyleaf. *Illustrative legal hand; not a claim that these four are guaranteed in the opening draw.*

**Player turn 1**

1. **PLAY** Candle Stub instance A for 1 Energy: deal 6 damage, apply 1 Singe; Focus **1 → 2**.
2. **INSCRIBE** Ink Blot into physical slot 1, first free combat inscription: Focus remains **2**.
3. **INSCRIBE** Candle Stub instance B into slot 2, pay 1 Focus: Focus **2 → 1**.
4. **PLAY** Flyleaf for 0 Energy: draw/discard, gain 1 Gust; eligible Play yields Focus **1 → 2**.
5. **CAST** `Ink Blot → Candle Stub`, two occupied instructions, costs 2 Focus: Focus **2 → 0**. Heightened Spell deals 6 base plus **2** from consuming the existing 1 Singe on the target. Net Cast exhaustion **0 → 1** (base 1; Heighten not heavy).
6. End turn. Wand still holds Ink Blot and Candle Stub.

**Player turn 2**

1. Start-of-turn exhaustion recovery: **1 → 0**. Normal draw, then resolve the 1 Gust: draw 1, choose and discard 1; Gust is consumed.
2. PLAY Chalk Line: gain 5 Block and 1 Grit; Focus **0 → 1**.
3. PLAY any other eligible non-Junk card from hand: native effect resolves; Focus **1 → 2**.
4. CAST unchanged two-card Wand: Focus **2 → 0**. Heighten then Candle Stub again (6 base damage, plus Singe only if currently available on its target). Exhaustion becomes **1** again.

The example proves the basic engine is usable before Uncommons/Rares and shows why installed cards being absent from future hand draws is part of the character's deckbuilding economy.

### 12.16 Burn Out with replacement (separate trace)

Assume at the start of a player turn: Wand Exhaustion **5**, Focus **2**, three installed cards including one expendable card, Ash Bookcord relic held, and a normal eligible card in hand.

1. Use **BURN OUT** on expendable slot: it moves to Exhaust; Exhaustion **5 → 2**. This action is the one allowed Burn Out for this turn.
2. **Ash Bookcord** offers a free post-action inscription of one hand card into the **same vacated slot**. Accepting fills that slot, but does not replay the Exhausted card, `CardPlayed`, or launch a new Cast.
3. Play another eligible normal card to gain 1 Focus: **2 → 3** (and its native effect).
4. Cast the resulting three-slot Wand for 3 Focus. Its effects execute in physical order; the card inserted at step 2 is included **because this is a new Cast**, not the earlier Burn Out. It may generate exhaustion subject to the 8-point cap.

These traces should become executable reference tests before writing any automated balancing simulator.

---

## 13. Supported build families and limits

| Build | Minimum viable engine | Later support | Real decision, not a linear numeric ladder |
|---|---|---|---|
| **Short Hand** | Candle Stub, Flyleaf, ordinary Play Focus | Knotted Wristcord, Sooty Ferrule | Cast 1–2-card Spells frequently; withdraw a Spell for normal Play. |
| **Long Lesson** | Spare Notch, Ink Blot, Chalk Circle | Clay Weight, Kiln Shelf, Blue Inkstone | Sacrifice deck presence and higher Focus to operate 5–6 linked instructions. |
| **Candle Trick** | Wax Drop, Candle Stub, Flyleaf | Split Wick, Sooty Ferrule | Build Singe then spend it across individual hits; sometimes Snuff for a single payoff. |
| **Ash Wand** | Wick Trimmer, Burn Out | Candle at Both Ends, Rubbing, Ash Bookcord, Second Hand, Lessons in Ash | Do not protect the perfect Wand: burn it strategically, replace it, and use Exhaust as tempo. |
| **Split Spell** | Normal Speak in both attack/defense roles | Brass Dividers, Penny Whistle | Cast only the needed part. Full Wand is not always best. |
| **Moving Text** | Open Casement, Torn Flyleaf | Bent Nib, A Perfect Copy, The Wrong Way Round | Manipulate future position, not just initial inscription order. |
| **Delayed Lesson** | Folded Leaf + Speak | Seal Press, Rubbing, Watermark | Move effects across Cast and enemy-turn boundaries, choose trigger conditions. |
| **Broken Seal** | Chalk Line, Mason's Dust, Grit | Chalked Thumbstall, Seal Press, Cup of Water | Protect the player while inscribing and turn old shields/statuses into new timing resources. |
| **Gaps & Margins** | One free middle slot with Speak | Bone Spacer, Brass Dividers | An empty slot can be strategically valuable; capacity does not imply fill-all. |

**Not nine separate forced archetypes.** These are nine overlapping construction strategies using the same grammar. A strong deck can combine Short Hand + Singe, Ash Wand + Delayed Lesson, or Long Lesson + Gaps. Act IV does not demand a rare-defined setup; commons remain relevant as enabling tools.

**Most likely dominant patterns worth testing:**

1. `HEIGHTEN → damage SPEAK → ECHO` universal offense.
2. Non-Junk cheap card loops yielding large Focus with little Energy.
3. Free Inscription + Withdraw/Retrieve as accidental deck-thinning or repeated `CardMoved` triggers.
4. Ash Bookcord or Second Hand plus Burn Out repeated at little opportunity cost.
5. Long Casts being strictly worse than Short Casts because their Focus and Exhaustion both grow.
6. General status proxies becoming useless when facing a boss without supporting status types, despite plentiful Rare names.
7. Grit being too valuable against high-hit-count enemies or too weak against single-hit bosses.

---

## 14. Implementation architecture

### 14.1 Keep the Core small

Recommended **content overlay** instead of rewriting shared cards:

```text
CharacterDefinition: sorcerers_apprentice
  startingDeck: [candle_stub ×4, chalk_line ×4, ink_blot, flyleaf]
  innateState: Focus, WandExhaustion, WandSlots[4], StoredSpell
  characterActions: Inscribe, Cast, Swap, Withdraw, BurnOut
  statusIds: singe, flow, grit, gust
  cardShelf: 42 Apprentice cards
  relicShelf: 9 Apprentice-only relics
  generalWandOverlay: 50 registry entries keyed by existing stable card IDs
```

A Wand is a **combat zone** with ordered nullable slots, not another deck pile. Physical `CardInstanceId` is needed for upgrade, cost mutations and exhaust history. **Temporary copies** may be installed only if their temporary metadata remains attached: when withdrawn, Burned or Burned Out they go to Exhaust instead of Discard; they disappear normally after combat. They never create permanent deck instances. Treat it as a first-class zone for card location queries. This matters for existing relics such as Conservator's Thread (a card leaving hand without being played) and generic Exhaust triggers. All event hooks need explicit source/action context.

### 14.2 Suggested intermediate representation

```text
WandImprint {
  sourceCardInstanceId
  kind: SPEAK | HEIGHTEN | ECHO | SHAPE | CLAUSE |
        ORDER | KEEP | GROUND | BURN | STOP
  payload: typed effect op(s)
  targetPolicy: chosen | all | self | retarget-lowest-HP | ...
  copyPolicy: copyable-effect-mask | none
  snapshotPolicy: preflight | execution
  afterCastOperations: []
  consumedOnce: boolean
}

ResolvedSpeak {
  frozenNumericPayload, targetPolicy, copyPolicy
  sourceCardInstanceId, originalPhysicalSlot, appliedModifiers
}

StoredSpell {
  trigger: next_cast | next_enemy_attack | next_burnout
  frozenEffectSpec, sourceCardInstanceId
  expiry: defined-per-trigger
}
```

`WandProgramPreview` must include planned Focus payment, old/new Flow refunds, exact or estimated damage/Block/status deltas, projected Exhaustion, triggered relics, invalid modifiers, stored Spell state, target retarget policy, Burned cards, expected empty slots and eventual post-cast movement. If dynamic enemy deaths or status-prevention make an exact future result unknowable, mark that segment conditional; **never invent certainty**.

### 14.3 Event contract

| Event / hook | Required separation |
|---|---|
| `CardPlayed` | Ordinary play only; gains Focus for eligible non-Junk. No event for Inscribe, Cast, WITHDRAW, SWAP. |
| `CardMovedToWand` / `CardRemovedFromWand` | Source zone and cause (`Inscribe`, `Refill`, `Withdraw`, `Burn`, `BurnOut`). |
| `WandCastStarted`, `WandSpeakResolved`, `WandCastResolved` | Distinct from `CardPlayed`; individual Speak effects emit ordinary combat effect events. |
| `StatusApplied`, `StatusStackConsumed` | Native hooks for Singe, Flow, Grit, Gust and existing shared statuses. Explicit trigger source prevents recursion. |
| `CardExhausted` | Existing canonical Exhaust hook, used by Burn, Burn Out and ordinary Play when applicable. |
| `GustResolvedThisTurn` | Persist for that player turn; consumers don't rely on remaining Gust stacks. |
| `WandOverloadPreviewFailed` | Prevent Cast without consuming Focus, statuses, Burn charges or changing zone. |

### 14.4 UI and teaching requirements

- A visible four-position Wand track with empty slots, expandable to six, outside normal hand.
- Dragging or selecting a card offers **PLAY** versus **INSCRIBE**. Inscribe is not an extra card Play.
- Normal cards show distinct **PLAY / WAND** text regions; for General cards show a small concise Wand proxy, not a rewritten native card.
- Hover/tap previews show each instruction's effect, arrows for `HEIGHTEN`, `ECHO`, `SHAPE`, and why a modifier will Fizzle.
- **Two meters:** Focus (available) and Exhaustion (load/limit). Do not label the load meter `Exhaust` (which is the separate card zone/action).
- When Cast is blocked, show the precise reason: insufficient Focus, prohibited target, exhausted load limit, per-turn Cast cap, unresolvable program or recursion safety ceiling.
- StoredSpells have one small icon with trigger type and expiry. Shadow positions and physical empty slots must remain legible for Bone Spacer and Brass Dividers.
- Tooltip for `Grit`: applies **before** Block/Ward Wax. Tooltip for `Singe`: applies only to **direct damage hits**, not status HP loss.
- Tutorial sequence: (1) play versus inscribe; (2) 2-slot damage; (3) moving modifier before/after Speak; (4) first overloaded Wand; (5) Burn Out and recover. No requirement to teach all ten primitives in Act I.

---

## 15. Acceptance tests (minimum vertical-slice set)

### A. State, movement and resource safety

1. Fresh combat: capacity 4, empty slots, Focus 1, Exhaustion 0, first Inscription free.
2. Playing a real non-Junk card emits `CardPlayed` once and adds Focus; inscribing never emits `CardPlayed`.
3. Inscribing uses physical card instance, removes it from hand, does not send it to Discard or Exhaust.
4. Cannot inscribe Junk/Curse/unplayable without explicitly permitted exception.
5. Successful WITHDRAW moves to Discard; BURN OUT moves to Exhaust and reduces Exhaustion; these are distinct.
6. First Inscription free only once **per combat**, even across turns; later inscriptions charge Focus unless discounted.
7. Focus persists between turns but cannot exceed cap; Exhaustion decreases at start of turn and cannot become negative.
8. Unaffordable or illegal Cast **does not** consume Focus, status snapshots or Burn/Relic charges.
9. A six-slot Wand requires two capacity increases and cannot exceed hard cap; extension does not remove occupied slots.
10. Three Cast cap per turn applies to all variants, including sections and post-cast effects; Burn/StoredSpell cannot bypass it.

### B. Elements and timing

11. Singe is consumed once per **direct hit**, even if Block absorbs the damage; Paperwork/Blood Ink/Threefold Hex damage does not consume it.
12. Two-hit Speak with 2 Singe gains two independent +2 bonuses and consumes two stacks.
13. Flow gained *during* a Cast cannot refund that Cast; old Flow can, subject to cost/refund cap.
14. Grit is tested before Block; a fully blocked enemy Attack can still consume Grit if it mitigates positive incoming Attack damage.
15. Gust is drawn-and-discarded after normal next-turn draw; Feather Catcher replacement counts as one of the designated discards.
16. Bent Nib sees `GustResolvedThisTurn` even when Gust status stacks have already been removed.

### C. Programming grammar

17. `Heighten → Speak → Echo` repeats the already Heightened **frozen** eligible result, not Heighten itself.
18. `Echo` at start fizzles; preview flags it. `Echo` cannot echo itself, another Echo or a resource-generating proxy.
19. `Fork` changes the count of attacks; `Join` combines exactly two consecutive eligible attacking Spells; both are bounded.
20. `Keep` stores only one Spell. If full, later storage uses specified fallback, does not silently overwrite.
21. A stored `next_cast` effect is not consumed by a failed Cast; a reactive Shield Seal expires at the next player turn if not triggered.
22. Turnabout/reversal may change unexecuted steps but cannot re-execute any instruction.
23. Cup of Water's damage-to-Block conversion suppresses the original direct hit and cannot indirectly consume Singe.
24. Penny Whistle stops **after a previous Speak**; no following instruction executes or generates heavy-operation exhaustion.
25. Compile cannot exceed 24 emitted atomic effects; invalid preview rather than silent truncation or infinite recursion.

### D. Card/relic interaction cases

26. Brass Dividers segment `1–2` cannot receive modifiers placed at slot 3 or 4; it pays for its own two executed cards only.
27. Bone Spacer gives no effect from trailing empty slots, only from an actual gap crossed by an active Cast.
28. A Perfect Copy rotates **after** Cast; the current Cast retains its original instruction order and card instances.
29. Ash Bookcord fills a Burn-created slot only after Cast settlement; its new card never executes in that same Cast.
30. Ash Bookcord and Second Hand cannot place **two cards in the same vacancy**; one chosen provider resolves, the other retains a still-legal charge.
31. Burn Out fires canonical native Exhaust events; Knotted Wristcord cannot return that same Exhausted card.
32. Blue Inkstone consumes only `oldFlow` Flow; it exchanges an actual refund for Grounding, not both.
33. Lessons in Ash's trace does not occupy a physical slot or compete with Ash Bookcord's slot-refill.
34. The Fourfold Lesson counts real non-copy-generated elemental applications during **the player turn**, not card tags.
35. A Perfect Copy + The Wrong Way Round + Open Casement terminates, previews deterministically and never rewinds the iterator.

### E. General Pool and rarity

36. Every one of the **50** General Pool IDs has a valid Apprentice proxy; unknown keys fail content validation, not silently fall back to the card's native Play text.
37. General card native Play, native upgrade, Bureaucrat behavior and Witch ingredient family remain identical in non-Apprentice games.
38. Proxy of Grand Dispensation cannot be repeated to generate Energy/draw/whole Rite. Proxy of Compound Indictment is not arbitrarily repeatable.
39. General proxy of an upgraded card uses the same prescribed Wand proxy as its base version; Play retains native upgraded behavior.
40. Common/uncommon/rare reward eligibility is engine-configurable across Acts I–IV without invented hard Act gates; Act V follows actual no-new-rewards rules where applicable.

---

## 16. Implementation sequence

**Stage 0 — Content & schema:** character record, four elemental statuses, typed Wand-imprint registry, card/relic eligibility; migrate no shared Play card texts.

**Stage 1 — Minimal engine:** Wand zone, four slots, Inscribe/Play/Withdraw/Swap, Focus, Cast, Exhaustion, Burn Out; tutorial starter only. Tests 1–10.

**Stage 2 — Interpreter:** `SPEAK`, `HEIGHTEN`, `ECHO`, `GROUND`, ordered execution, targeting, deterministic preview, copying whitelist and safety cap. Tests 11–19, 25.

**Stage 3 — Advanced grammar:** Shape, Clause, Order, Keep, Burn, Stop, expanded capacity and triggers, reactives, state snapshots. Tests 20–24.

**Stage 4 — Card pool:** 4 starters, 16 commons, 14 uncommons, 8 rares; explicit normal Play/Wand behaviors and upgrades. Check tooltips and reward eligibility.

**Stage 5 — Relics & General Pool:** nine character relics, explicit 50-profile overlay, event bridge, conflict handling. Tests 26–40.

**Stage 6 — Balancing (later):** simulation matrix for Acts I–V; test short-vs-long throughput, Burn replacement chains, status application synergy, low-roll and high-roll reward distributions, boss single-target versus multi-target fights. Do not treat BD values as final. Measure action economy and mean damage/Block per Energy/Focus/exhaustion, as well as card retention/deck-thinning side effects.

**Suggested automated smoke simulations after implementation:** at least 1000 seeded fights each for Short Hand, Long Lesson, Ash Wand, Elemental Tempo, Modular Wand, Moving Text and unoptimized Common-heavy runs, against the same encounter set. Track wins, turns, Focus wasted, Wand idle turns, Casts/turn, net exhaust per cast, 5–6 slot utilization and percent of cards inscribed versus played. **These simulations have not been run.**

---

## 17. Final audit decisions & remaining verification

### Changes implemented in this master

- Cut from 84 to 42 definitions, **redistributed toward Commons**: `4 + 16 + 14 + 8`.
- Kept nine genuinely different Wand/element relics; fixed Grit/Gust timing and refill conflicts.
- Removed duplicated first-pass attack/block variants and the low-value extra Rare rules (`Closed Book`, `The Long Sentence`).
- Rebuilt `Second Copy` as **Rubbing**, an event-triggered stored Spell distinct from Echo and next-Cast Keep.
- Rebuilt `Watermark` as a contrast-sensitive modifier instead of a second Keep.
- Replaced unspecified `Footnote` Clause with a precise Focus-zero decision.
- Converted `Glass Rod` into a one-off **damage ↔ defense Prism**, not a superior Burn attack.
- Added **Spare Notch** at Common to unlock long Wands without a mandatory Rare.
- Gave `The Fourfold Lesson` an event-driven four-element condition instead of a disguised ingredient recipe.
- Mapped all **50 real General Pool IDs** from the available archive snapshot to conservative Wand profiles, without modifying their native cards.
- Defined deterministic resolution, preview, caps, native hooks, post-Cast movement and example builds.

### Matters that cannot be truthfully declared verified from a design document

1. **Actual live RogueDeck-Core hooks:** native `CardPlayed`, Exhaust, status stack consumption, Ward Wax interception, card zones and relic triggers must be inspected in the target codebase before implementing these proposed hook names.
2. **Latest General Pool data:** this master uses the supplied `archive.json` exported 2026-10-03, not a live repository diff. IDs/prose may have changed since that snapshot.
3. **Reward engine:** card rarity weights and character vs general weighting must be reconciled with actual reward/shop behavior. No false claim of Act locking or present reward probabilities.
4. **Numerical balance:** Focus cap 6, Exhaustion cap 8, recovery 3, 3 Cast safety limit, card/upgrade values and damage/healing/status durations are BD; tune using actual fights rather than inventing finalized numbers.
5. **UI feasibility:** the Wand preview and the physical slot/section selectors require frontend work in BnB Godot; this master is not a patch.
6. **Native restricted statuses:** unusual Censure/Lien/Blood Ink reactions, especially triggered on non-CardPlayed Spell effect events, require deterministic regression tests.

**Status:** Conceptually complete, internally constrained content specification for **a first playable implementation**. Not represented as already coded, simulated, numerically tuned or merged. The strongest intended emergent distinction remains the same: *the player writes and rewrites a finite spell program, rather than cooking fixed combinations of cards.*
