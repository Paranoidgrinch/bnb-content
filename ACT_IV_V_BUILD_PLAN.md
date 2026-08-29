# Acts IV & V — the build plan

**Written 2026-08-29**, from a live audit of the three repos (RogueDeck-Core @676c446 · bnb-content @0c6d5d4 ·
bnb-godot @3fe5f4c, all clean, all on origin/main; Core 1430/754/573/365, bnb-content 925/925,
`--playtest 3` Victory 3/3, Godot `Victory acts=3 rooms=73`).

Acts I–III are finished and playable. **What is missing for the full game is Act IV and Act V.** This file is
the handoff across every compaction until both are done.

**The user's build order** (set 2026-08-29, the same order Acts II and III were built in):

> **Act IV first, whole:** normal enemies + encounters → elites → bosses → cards + relics → events.
> **Then Act V**, the same way as far as it applies.

---

## The working protocol

**One step per context.** After each step, in this order:

1. all suites green — `dotnet test` in bnb-content (~11 min) and in RogueDeck-Core when the step touched the
   engine;
2. `dotnet run --project Converter -- --playtest 3` and `-- --maps 3`;
3. `tools/sync-content.sh` + `godot --headless -- --smoke-marathon` whenever the document changed
   (**rebuild bnb-godot first** if a new `RunJson`/`CombatJson` kind was added — otherwise Godot boots with
   "Unknown … json kind" and hangs silently);
4. commit in each repo the step touched, with the step's number in the message body; push to `origin/main`;
5. update `project_bnb_port.md` — the resume block at its top names the NEXT step and the state it starts from;
6. **then compact.** Whoever reads this file after a compaction must need nothing else.

House rules that have each already cost a day (still binding, from `ACT_III_BUILD_PLAN.md`):

- every new status / card / relic needs a description, or `EverythingExplainsItselfTests` breaks the build;
- every `CounterId` is a **property** (`static CounterId X => new("…")`), never a `static readonly` field
  (`DocumentIdTests`) — a field below its first user initialises as a null string;
- only registered vocabulary survives the export (`CombatJsonRegistry.KindOf`);
- `RepeatUntil` is a do-while; a plain `sequence` starts all its steps at once (`CardAuthoring.Seq` emits a
  causal one);
- a rule at TURN END cannot read the hand; block granted at turn end is block that never existed;
- a counter the player must act on is promoted to a **marker status** — a bare counter reaches no screen
  (`ReadableBossStateTests`).

---

## Sources, in the order they outrank each other

| Doc (`source-data/design/`) | Scope |
|---|---|
| `…Standard_Encounter_Pools_Acts_I-IV_FINAL_AUDIT.md` §Act IV | **35 identities, 55 encounters, 17 stages**, the audit's binding interaction rules (§3.1–3.9), and the Act-IV balance appendix (HP/intent bands per stage) |
| `…Master_Elite_Pool_Acts_I-IV_FINAL_AUDIT.md` §Act IV | **10 elites**, their HP table, earliest depth, shared elite rules §6.1–6.5 |
| `…Master_Boss_Pool_Acts_I-V_FINAL_AUDIT.md` §Act IV (l. 6877–9329) | **8 bosses**, HP 580–640, shared boss rules §5.1–5.4 |
| `…Master_Boss_Pool_Acts_I-V_FINAL_AUDIT.md` §Act V (l. 9330–end) | **6 gods**, the gauntlet, the Divine Rule Area |
| `BnB_Final_Events_Master_PostAudit.md` §ACT IV | **20 events** + 9 event relics; §ACT V says explicitly: **0 events, 0 relics** |
| `BnB_Final_Relics_Master_PostAudit.md` §Act IV Boss Relics (l. 976+) | **24 boss relics**, 3 per boss |
| `BnB_Run_Systems_Master.md` §9 + §13 | what Act V does NOT have |
| `docs/bnb-act-map-specs.md` (Core) | Act IV map: Combat 12, MultiCombat 3, Elite 4, Event 4, Rest 3, Treasure 2, Shop 3, Mimic 20 % |

The ported v2 data (`source-data/{enemies,encounters}/act_4_licensing_labyrinth*.json`, 42 enemies /
41 encounters) is the OLD demo pool and is **superseded**, exactly as Acts II and III superseded theirs. It
stays loadable only until the authored pool replaces it.

---

## What already exists (do not rebuild it)

- **The Act-IV card pool is DONE** (Phase B): `Converter/Cards/ActIVCards.cs` (both pools, with upgrades) and
  `Converter/Cards/ActIVRites.cs` (Temple Tally, Processional Calendar, Hieratic Measure, Candle Cathedral,
  Absolute Interdict). Act-IV gates are pinned in `FinalCardPoolTests`.
- **Normal (50) and Shop (24) relics are DONE** and act-agnostic. **Boss relics: 45 of 69 done** (Acts I–III);
  the missing 24 are Act IV's. **Event relics: 16 of 25 done**; the missing 9 are Act IV's. Act V has none.
- The whole authoring substrate: `ActThree.cs`-shaped act files, `EncounterPassives`, `RawIntentPrograms`,
  `Elites/`, `Bosses/`, `BossPhases.cs`, `EventStory`, `FightProbe`, `RunWalker`, the Godot smokes.
- **The run simulator and the balance trainer** (bnb-godot `tools/simulate.sh`, `tools/train.py`) — the
  instruments that will verify both acts. ⚠ Their fitness is currently written against the **Act-III** boss
  (`damageToAct3Boss`); step **IV-24** and step **V-7** move it.

## What does NOT exist yet

- `ActRules.For(act)` **throws for 4 and 5**; `BabLoader.Acts` loads only acts 1–3.
- No Act-IV enemy, elite, boss, event or boss-relic content at all.
- No Act-V structure of any kind: the engine has never run an act that is three boss rooms.

---

# ★ DECISION REQUIRED BEFORE STEP IV-0

**The five Act-IV keywords have no canonical definition in any handed-over master.** All three masters say
"the existing Act-IV core vocabulary — Weighed · Burdened · Inscribed · Entombed · Embalmed" and then use them
several hundred times without ever writing down their rule. The document they refer to (`act_iv_*.md`) is not
in `source-data/design/`.

They must therefore be **reconstructed from usage and signed off by the user before a single enemy is
written** — every one of the 35 identities, 10 elites and 8 bosses is a reading of these five words, so
getting one wrong is a rewrite of the act, not a patch.

The reading the usages support (to be confirmed or corrected):

| Keyword | Whose | Proposed rule | Evidence |
|---|---|---|---|
| **Weighed X** | on the player | a visible **requirement to spend exactly X Energy** during your turn, resolved at end of turn; failure is punished by the enemy that owns the check, and the *absolute distance* from X is readable | "required and actual Energy expenditure", "absolute distance between", "exact Energy requirement", "a Weighed value greater than the player's realistically spendable Energy is not offered" |
| **Burdened X** | on the player | a weight that makes acting more expensive (cost/energy pressure), the counterpart Weighed conflicts with | "Weighed vs Burdened resource conflict"; Weighed failure converts into Burdened on the Broken Royal Weight relic |
| **Inscribed X** | on the player | being **written into the register**: a stack the act reads as a state ("while the player has at least 1 Inscribed…"), amplified by some enemies | Uncounted Pilgrim, §3.x "Inscribed amplification", Name-Erasing Chisel Spirit prevents the gain |
| **Entombed X** | on the player | accumulating burial; **at 5 it stuns** (skips the player's turn) and resets | elite §6.3 "Entombed retains its universal stun at 5" |
| **Embalmed X** | on either side | **preservation: prevents status decay** on the bearer; some enemies need it to function at all and must self-enable it | §3.5 "Embalmed-dependent solo enemies must self-enable"; "Embalmed decay prevention" |

**The two engine questions that follow from it**, both to be answered in IV-0:

1. **Weighed needs "how much Energy did the player spend this turn".** Core has no such value today
   (no `EnergySpent`). Two routes: (a) a content-side counter that adds the played card's cost on
   `CardPlayed` — needs an expression for *the cost of the card that was just played*, which may not exist;
   (b) an engine seam: the resource system records per-turn spend, readable as an ordinary combatant value.
   **(b) is the recommendation** — Weighed is the spine of the whole act, and the Act-III lesson is that the
   act's central question must be askable in one place.
2. **`Replicated` status applications** (audit §3.3/§3.4): an application must be able to carry a mark saying
   it was copied, so replication cannot chain. Core applies statuses without such a mark today. Likely the
   second seam.

Everything else in the audit — Primary Measure, observed results, one office per turn, Royal Favor — is
content, not engine.

---

# PART I — ACT IV: THE LICENSING LABYRINTH

35 identities · 55 encounters · 17 stages · 10 elites · 8 bosses · 20 events · 24 boss relics · 9 event relics.
**The largest act in the game** (Act III was 25/40/9/5/15/15/5).

## Standards — 12 steps

Each step: author the stage's identities in `Converter/ActFour*.cs`, its encounters into the pool, and prove
each signature in a live fight (`FightProbe`) with one test file per stage. HP and intent numbers come from
the **balance appendix** §ACT IV, stage by stage; where the master says "balance-tunable", the appendix band
decides and the choice is written into `ADAPTATIONS.md`.

- [ ] **IV-0 — the vocabulary + Stage 1 (Boundary Stelae).** The five keywords as decided above, in
      `Converter/ActFour.cs`; the one place a Weighed check is raised, resolved and observed (the Act-III
      lesson: `ActThree.Violate` was worth the day it cost); `ActRules.For(4)` minimally walkable;
      `BabLoader` loads `acts/act_4_licensing_labyrinth.json`. Identities: Reed-Cord Surveyor, Crooked Rod
      Bearer. Encounters 1–3, incl. the audit's §3.1 Primary Measure rule (E3 is its first reader).
- [ ] **IV-1 — Stage 2, the Gate of Counted Names.** Uncounted Pilgrim, Cobra of the Entry Mark,
      Name-Eating Baboon. Encounters 4–7. First readers of **Inscribed**.
- [ ] **IV-2 — Stage 3, the Granary Courts.** Crocodile of the Short Measure, Jar-Seal Scarab Swarm,
      Hungry Grain Thief. Encounters 8–11.
- [ ] **IV-3 — Stage 4, the Floodmark Basins.** Flood-Mark Reader, Drowned Field Scribe, Silt-Buried Farmer
      Shade. Encounters 12–15. §3.2 Observed Weighed Result lands here.
- [ ] **IV-4 — Stage 5, the Tribute Causeway.** Foreign Tribute Shade, Donkey of the Third Tally,
      Empty-Handed Envoy. Encounters 16–18.
- [ ] **IV-5 — Stage 6, the Corvée Yards.** Rope-Gang Wraith, Runaway Laborer, Stone-Hauler Ushabti.
      Encounters 19–21. First **Burdened** pressure block.
- [ ] **IV-6 — Stages 7 + 8.** Fallen Capstone Golem, Cornerstone Oath-Stone · Palette-Bearing Apprentice,
      Hieroglyphic Complaint Wall. Encounters 22–27. §3.5 (Embalmed self-enabling) is binding for the Wall.
- [ ] **IV-7 — Stages 9 + 10.** Sun-Seal Bearer, False-Seal Forger · Kneeling Petitioners. Encounters 28–33.
      **§3.3 + §3.4 (Replicated) are the whole point of this step** — the second engine seam is proved here.
- [ ] **IV-8 — Stages 11 + 12.** Natron Bearer, Linen-Wrapped Embalmer, Unfinished Mummy · Fourfold Vessel
      Guardian. Encounters 34–40. §3.6: the Guardian cycles Body → Breath → Blood → Name, one office a turn,
      and the office is a **marker status**, not a counter.
- [ ] **IV-9 — Stages 13 + 14.** False-Door Finder, Cursed Loot Bearer · Star-Table Scribe, Moon-Cycle Ibis,
      Eclipse Scarab. Encounters 41–46. §3.9: Stage 13 reintroduces **Act-III law** locally — Safe-Conduct is
      granted at combat start, never assumed. Reuses `ActThree`'s vocabulary; do not fork it. §3.7: the Ibis
      repeats **1 stack**.
- [ ] **IV-10 — Stage 15, the Cartouche Chambers.** Name-Erasing Chisel Spirit, Royal Genealogy Wall.
      Encounters 47–49. §3.8: **Royal Favor** is the Wall's own local resource; the player's status is never
      stolen, and a prevented gain grants nothing.
- [ ] **IV-11 — Stages 16 + 17, the final forms.** No new identities: Crooked Rod Bearer → Feather-Bearer,
      Crocodile → Crocodile Beneath the Balance, Stone-Hauler → Golden Ushabti Captain, Palette-Bearer →
      Eternal Reed Scribe, Cornerstone Oath-Stone → Oathbound Gate. Encounters 50–55.
      **Acceptance: the standard pool is complete — 35 identities / 55 encounters pinned in
      `Tests/ActFourPoolTests.cs` against the master's roster and the appendix's HP table.**

## Elites — 4 steps

Shared rules §6.1–6.5: elite-local counters stay encounter-local; a Weighed value must be achievable from the
deterministic current state; a queued signature is telegraphed and never interrupts a card or a Weighed
resolution. HP from the master's table.

- [ ] **IV-12 — Surveyor of the Errant Cord (248) · Scarab Host of the Sealed Granary (255) ·
      Rope-Master of the Corvée (275 + summons).** The Surveyor offers **two achievable Weighed values** —
      the solvability filter is its own machinery, written once and reused by every later elite.
- [ ] **IV-13 — Keeper of the Living Cartouche (300) · Mummified Overseer of the Linen House (318) ·
      The Treasury of the Two Pans (330).** Glyphs, Wrapping, Value-vs-Quantity accounting.
- [ ] **IV-14 — Sphinx of the Processional Measure (344) · The Tombbreakers Three (112+100+108).**
      Voluntary ritual costs; a three-body kill-order elite (the Ant-Queen lesson: a body whose pool is
      emptied by anything falls — already fixed in Core).
- [ ] **IV-15 — Keeper of the Thirty-Six Decans (365) · Colossus of the Endless Procession (388).**
      The six-watch exam and the three-step discipline cycle; escalation is capped.
      **Acceptance: 10 elite encounters, earliest-depth table honoured, all pinned.**

## Bosses — 4 steps

HP 580–640. §5.2 solvability filter, §5.3 player agency before punishment, §5.4 transition timing, and the
Act-I–III rule that **the phase is written ON the intent** (`BossPhases.cs`, tagged `phase`). Every boss gets
its live test file and enters `Tests/BossLengthTests` (40-turn budget against the starting deck).

- [ ] **IV-16 — The Pharaoh of the Sealed Name (630) · The Weigher of the Unspoken Heart (610).**
      Three Royal Names / Cartouche Ward; Balance + earned Feather windows.
- [ ] **IV-17 — The Architect of the Impossible Pyramid (640) · The Lady of the Black Granaries (600).**
      Monument + player-chosen Blueprint; four Granary Seals as four state functions.
- [ ] **IV-18 — The First Scribe of the House of Life (580) · The Mother of Natron and Resin (610).**
      The player writes the next enemy turn and may erase at a cost; Vessels + washing.
- [ ] **IV-19 — The Vizier of the King's Mouth (590 + Offices) · The Queen of the Flood Reckoning (620).**
      Office kill order permanently shapes Phase II; Water Level + player-side Sluice Authority.

## Cards and relics — 2 steps

- [ ] **IV-20 — the cards, audited against the act that now exists.** The pool is written; what is NOT proven
      is that the five Act-IV Rites do what the finished vocabulary means (`ActIVRites`: Hieratic Measure is
      read inside `CardAuthoring.Ratify`, Candle Cathedral inside Ward Wax, Absolute Interdict through
      Censure's prohibition, Processional Calendar resolves the Queue — the queue-self-resolution trap is
      fixed in Core but must be re-proved here). Pin the Act-IV offer by rarity, and check every Act-IV card
      whose text names a keyword this act now defines.
- [ ] **IV-21 — the 24 boss relics**, 3 per boss, forced 1-of-3 on the kill, in `Relics/ActFourBossRelic*.cs`
      following the Act-III shape (including relic-granted action cards where a relic needs one).
      ⚠ The two traps from Act III's relic step: a `Computed…RunEffect` that reads the event **crashes** when
      it is queued as a literal — use templates (`QueuedEffectTests` covers every relic in the document); and
      a relic pool nothing draws from looks exactly like a working one (`ShopShelfTests`).

## Events — 2 steps

20 events (master §ACT IV), each with its branch structure and availability band (Early–Mid / Mid / Mid–Late /
Late / Late·Rare), in `Converter/Events/ActFourEvents.cs` + `ActFourEventPrograms.cs` + `ActFourEventObjects.cs`,
tested with `EventStory` (walk the door, name the branch, win the fight, ask the run what it paid).

- [ ] **IV-22 — events 1–10.** The Dry Nilometer · The Black Granary · The Red Linen Procession ·
      The Nameless Cartouche · The Forewritten Tablet · The Tomb Robbers' Fire · The Triple-Counted Donkey ·
      The Four Canopic Jars · The Chamber of False Measures · The Crocodile at the Weighing Place.
- [ ] **IV-23 — events 11–20 + the 9 event relics.** The Wall of Old Complaints · The Copper Tithe ·
      The Unnamed Throne · The Fixed-Day Festival · The Broken Sluice · The Unfinished Burial ·
      The Survey of the Dead · The House of Life at Night · The Merciful Balance · Cartouche Repair Bench.
      Relics: Cup of the Lowest Mark · Red Linen Knot · Blank Cartouche · Jar of Borrowed Breath ·
      Broken Royal Weight · Petition Chisel · Tablet of the Missing Name · Funerary Linen Coil ·
      Mercy Counterweight. Also §4.6 of the Run-Systems Master: two of these are **shop-like event markets**.

## The act itself — 1 step

- [ ] **IV-24 — Act IV becomes a room the run walks.** `ActRules.For(4)` in full: per-path minimums
      (Combat 12, MultiCombat 3, Elite 4, Event 4, Rest 3, Treasure 2, Shop 3), per-path ceilings, lanes,
      `EarliestDepthPercent` (the elite table's depths 3–12 mapped to percent, the way Act II's stages were),
      mimic **20 %** with its own mimic encounter, the act's rest/treasure room texts, and the campfire heal
      percentage for act 4 (§13: it decreases per act). Then:
      `--playtest 3` must report **Victory 4/4 acts**, `--maps 3` clean, Godot `--smoke-marathon` clean,
      and the run simulator's fitness moves from `damageToAct3Boss` to a per-act table so the trainer keeps
      measuring (`bnb-godot/scripts/RunSimulator.cs`, `tools/train.py`, both ANLEITUNGen).

---

# PART II — ACT V: THE DIVINE LEDGER

Not an act — a **gauntlet**. Boss → Boss → Boss, 3 of 6 gods chosen at act start, order visible from the
start, no repeats. No standards, elites, events, shop, treasure, campfires, healing, gold, card or relic
rewards, **no boss relics**. HP loss carries across all three. Boss-internal systems reset between them.
Every god gets a prominent **Divine Rule Area** in the combat UI. Act V is intentionally brutal: a strong run
may still lose, and that is correct.

- [ ] **V-0 — the gauntlet as a structure.** The engine already ends an act when its map runs out
      (`RunRunner.WalkLinear`), so Act V is a **linear three-node map whose nodes are boss rooms** — no
      edges, no other roles. What must be built: drawing 3 of 6 without repetition per run; showing all three
      and their order from the first room (map screen + a run-level announcement); a boss room that grants
      **nothing** (the spoils bundle is Act-I–IV's, not Act V's); no rest/heal seam between them; the
      `ActRules.For(5)` shape for an act that has no per-path minimums at all. Plus the Godot side: the
      Divine Rule Area as a real, consistently placed panel that each god fills differently.
      **Acceptance: a run walks a placeholder Act V of three trivial divine rooms and reports Victory 5/5.**
- [ ] **V-1 — Nisaba, Keeper of the First Tablet.** *She writes.* The First Tablet: what is written becomes
      real; the fight asks whether you can prevent what is already written.
- [ ] **V-2 — Inanna, Mistress of the Eanna Ledger.** *She claims.*
- [ ] **V-3 — Nanshe, Keeper of the Just Ration.** *She allocates.* The Ration Tablet.
- [ ] **V-4 — Nanna-Sin, Lord of the Counted Moon.** *He counts.* The Lunar Calendar.
- [ ] **V-5 — Utu, Witness of Every Oath.** *He witnesses.* Oaths / Witness.
- [ ] **V-6 — Enlil, Voice of the Unalterable Decree.** *He decrees.* Decrees.

Each god: read its section of the boss master end to end first, then author it as its own file plus a live
test file; every unusual rule must be **telegraphed on screen** (the ReadableBossState rule applies with full
force — these fights are "almost a separate game mode"); each god enters `BossLengthTests`.

- [ ] **V-7 — the whole game.** `--playtest` over **5 acts**, several seeds, all Victory; `--maps` clean;
      Godot `--smoke-marathon` through all five acts with its latency reported per act (the replay-checkpoint
      risk below); the bug runner over ≥50 runs with an empty "runs worth reading"; the balance trainer
      re-pointed at the **Act-V** arrival and run a full generation set, so the first real balance data for
      the complete game exists. Then: the victory screen means *the game is won*, not "the map ran out".

---

## Risks, named now

1. **Replay latency over five acts.** Every answer replays the run from its baseline; the interlude
   checkpoint (`InteractiveRunSession.Continue`) caps it, but the cap has never been measured past act 3.
   **Measure it at IV-24 and again at V-7**; if act 4 answers cost noticeably more than act 3 answers, the
   checkpoint must move inside the act before Act V is authored.
2. **Act IV is bigger than any act so far and its design is looser** — the master repeatedly says
   "balance-tunable" and "conceptual bands" where Act III gave exact numbers. Every such choice goes into
   `ADAPTATIONS.md` in the step that makes it, or the act becomes unreviewable.
3. **The five keywords are a single point of failure** (see the decision block above).
4. **Act V has no precedent in the engine**: no act has ever been three bosses, and no boss has ever been
   allowed to override normal combat logic as broadly as the gods are supposed to. V-0 exists to find out
   what that costs before six fights are written against it.
5. **A pool nothing draws from looks like a working pool** (Act III's relic finding: 72 of 74 relics were
   unreachable and every test passed). Every new pool in this plan gets a test that proves a run can actually
   reach it.

## Status

- [ ] ★ the five-keyword decision
- [ ] IV-0 … IV-11 standards · IV-12 … IV-15 elites · IV-16 … IV-19 bosses · IV-20 … IV-21 cards+relics ·
      IV-22 … IV-23 events · IV-24 the act
- [ ] V-0 structure · V-1 … V-6 the six gods · V-7 the whole game
