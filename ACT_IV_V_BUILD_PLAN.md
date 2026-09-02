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

# ★ THE FIVE ACT-IV KEYWORDS — RATIFIED 2026-08-29

**The five Act-IV keywords have no canonical definition in any handed-over master.** All three masters say
"the existing Act-IV core vocabulary — Weighed · Burdened · Inscribed · Entombed · Embalmed" and then use them
several hundred times without ever writing down their rule; the document they refer to (`act_iv_*.md`) is not
in `source-data/design/`. They were therefore reconstructed from usage and **ratified by the user on
2026-08-29**. The table below is now canon for the whole act — every one of the 35 identities, 10 elites and
8 bosses is a reading of these five words.

## Weighed X — *the measure*

The player is given a **visible requirement for this turn: spend exactly X Energy**. At end of turn, required
is compared against actual expenditure.

- exact ⇒ success;
- otherwise failure, and the **absolute distance** between required and actual is itself readable, so an
  enemy can punish by error band (Reed-Cord Surveyor) instead of binary pass/fail.
- Several enemies in one encounter observe **one Primary Measure** rather than raising contradictory checks
  (audit §3.1, §3.2).
- **An impossible requirement is never offered as the only option** (elite §6.2, boss §5.2): the value must be
  achievable from the deterministic current state.

## Burdened X — *the tax*

Not general cost pressure but a concrete, temporary **tax on playing cards**: Burdened raises the Energy
actually paid for a card, and **playing such a taxed card works one stack of Burdened off** (it is consumed by
being paid).

- That is exactly why it collides with Weighed: the tax changes what "actual expenditure" comes to, so paying
  it and hitting the measure are one decision.
- The Colossus of the Endless Procession explicitly checks whether **at least one Burdened stack was worked
  off by playing a taxed card** — so "a stack was consumed by payment" must be an observable event, not just
  a smaller number afterwards.

## Inscribed X — *the register, and the amplifier*

Thematically: **you are written into the register**, and enemies may simply ask whether `Inscribed > 0`
(Uncounted Pilgrim). But its universal mechanic is the important half:

> **Inscribed amplifies the NEXT status application on the player and is consumed doing it.**

- It applies to a **negative** application (an enemy debuff lands harder) **and to a positive** one.
- Therefore the player can deliberately **spend Inscribed on their own buff** rather than let it magnify the
  next incoming debuff. That choice is the act's central player-side decision, and the Act-IV relics and the
  **Keeper of the Living Cartouche** are built on it.
- Name-Erasing Chisel Spirit preventing a gain means the amplification never happens (and the Royal Genealogy
  Wall receives no Royal Favor, §3.8).

## Entombed X — *burial pressure*

Accumulates. **At 5 the player is stunned and loses the turn**; the threshold then resolves/resets so the
cycle can build again. This universal 5-threshold is used explicitly and repeatedly by the elite master
(§6.3), including the rule that a queued elite action does not resolve during the skipped turn unless its own
countdown says so.

## Embalmed X — *preservation*

Prevents the **natural decay / natural expiry of temporary status values on its bearer**, in both polarities:

- a player can exploit it to **preserve their own buffs**;
- an enemy uses it to **hold a debuff in place**;
- an enemy whose signature needs Embalmed **must be able to create it itself** (§3.5: Hieroglyphic Complaint
  Wall, Natron Bearer, Unfinished Mummy) and may not depend on a second body to function.

---

## What this costs in the engine — the seam list IV-0 must settle

Each row is a capability the ratified vocabulary requires. **IV-0's first job is to check each against
RogueDeck-Core** and buy only what is genuinely missing (the standing rule: if it composes from existing
primitives, it is a capability test, not a feature).

| # | Capability | Status as far as the audit could tell |
|---|---|---|
| 1 | **How much Energy the player has spent this turn**, readable by a condition at end of turn | ✅ **BOUGHT (IV-0)** — `CombatantCardPlayTurnStats.ResourceSpentThisTurn`, fed from the cost-payment event, read by the `resourceSpentThisTurn` expression. The exact twin of `resourceGainedThisTurn`; counts what was ACTUALLY paid |
| 2 | **A cost tax that is consumed by being paid** | ✅ **COMPOSES (IV-0)** — a flat `CardCost` passive modifier plus a `CardCostPaid` trigger that works a stack off and counts the payment (`burden_paid`). The one engine touch: `event` now means "what the play cost" under that trigger, where it used to mean 0 |
| 3 | **A status that amplifies the next status application on its bearer and is consumed** — either polarity | ✅ **BOUGHT (IV-0)** — `StatusAmplificationSpec` + `DeclarativeStatusAmplificationInterceptor`, the mirror of `StatusPreventionSpec`: runs after prevention, never enlarges itself, one enlargement per application (`ApplyStatusEffectRequest.Amplified`), and announces polarity + size for IV-13 |
| 4 | **`Replicated` applications** (§3.3/§3.4): an application carries a mark saying it was copied, and a replicated application can never trigger another replication | Statuses are applied without such a mark today |
| 5 | **Stun at a threshold, then reset** (Entombed 5) | ✅ **COMPOSES (IV-0)** — read at the bearer's turn start; Stun for one turn, five spent. ⚠ but the validator was inert on the path the game is played on; fixed in Core, see IV-0's record |
| 6 | **Decay prevention on a bearer, both polarities** (Embalmed) | ✅ **COMPOSES (IV-0)** — almost nothing in this game decays by DURATION; fading is authored (Panic, Poison, Fatigue, Ward Wax), so preservation is written at the one fading point, `ActFour.Fade`, which all four now go through |

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

- [x] **IV-0 — the vocabulary + Stage 1 (Boundary Stelae). DONE 2026-09-02.** All five ratified keywords, in
      `Converter/ActFour.cs`, each with a live test of its own rule (Weighed's exact-spend comparison and its
      error distance, Burdened's tax **and its consumption by payment**, Inscribed amplifying the next
      application in **both** polarities, Entombed's stun at 5 and its reset, Embalmed holding a value that
      would otherwise decay); the six-row seam list above is worked through against Core FIRST; the one place a Weighed check is raised, resolved and observed (the Act-III
      lesson: `ActThree.Violate` was worth the day it cost); `ActRules.For(4)` minimally walkable;
      `BabLoader` loads `acts/act_4_licensing_labyrinth.json`. Identities: Reed-Cord Surveyor, Crooked Rod
      Bearer. Encounters 1–3, incl. the audit's §3.1 Primary Measure rule (E3 is its first reader).
      ▸ **What landed:** all five keywords in `Converter/ActFour.cs` with a live test each
      (`Tests/ActFourStelaeTests.cs`, 12 tests); Stage 1's two identities + 3 encounters
      (`Converter/ActFourStelae.cs`, `source-data/{enemies,encounters}/act_4_licensing_labyrinth*.json` — the
      ported v2 demo pool is REPLACED, as Acts II/III replaced theirs); `BabLoader` loads Act IV's enemies and
      encounters. Seam list settled: rows 1 and 3 were bought in Core (`resourceSpentThisTurn` on
      `CardPlayTurnStats`; `StatusAmplificationSpec` + `DeclarativeStatusAmplificationInterceptor` +
      `StatusApplicationAmplified` trigger + `Amplified` mark on the request), rows 2/5/6 compose and are
      capability tests only, row 4 (`Replicated`) is untouched and still belongs to IV-7. Design choices in
      `ADAPTATIONS.md` §"Act IV — the five words".
      ▸ **Deliberate deviation:** `ActRules.For(4)` still throws and the act-4 MANIFEST is still not in
      `BabLoader.Acts`. An act in `Acts` is an act the run walks, and `MapSpecBuilder` requires it to field a
      boss — which Act IV has none of until IV-16…IV-19. The act therefore joins the walked run at **IV-24**,
      which is where the plan already puts it. Loading its bodies and fights (done) is what a probe needs.
      ▸ **A finding that outlived the step:** card-play validators (Stun, one-attack-per-turn, unplayable)
      were consulted only on `CombatCardPlayProcessor`'s strict path — never on `PlayCardEffectRequest`, which
      is the path the host, the playtest walker and Godot all use. A stunned player could play their whole
      hand. Fixed in Core (`PlayCardEffects.cs` asks the validators and no-ops on refusal;
      `tests/.../PlayCardEffectValidatorTests.cs`). Nothing in Acts I–III used stun, so nothing had asked.

- [x] **IV-1 — Stage 2, the Gate of Counted Names. DONE 2026-09-02.** Uncounted Pilgrim, Cobra of the Entry
      Mark, Name-Eating Baboon. Encounters 4–7. First readers of **Inscribed** — the Pilgrim reads it as a
      mere state (`Inscribed > 0`), while the stage must also show its amplifying half, or the player never
      learns that the register is spendable.
      ▸ **What landed:** `Converter/ActFourGate.cs` + 9 live tests (`Tests/ActFourGateTests.cs`). The Cobra
      needs NO code — the register enlarges its venom by itself. The Pilgrim's Uncounted is a visible marker
      worth 30 % less attack damage, recounted on five events (**expiry is the one that matters**: the last
      stack of the register goes by being SPENT) plus every turn start (a fight's first round starts before
      its bodies are dressed, so a round-start hook fires for nobody). The Baboon reads the amplification
      EVENT — two general engine reads were bought for it, `eventStatusPolarityIs` (Stage 15's Royal Genealogy
      Wall wants the same question with Buff) and `eventAmplifierIs` (§3.4's copy-never-feeds-the-copier
      guard); encounter triggers can now also hear amplification/prevention/resolved actions.
      ▸ **Test lesson worth keeping:** an interactive fight is a REPLAY — a status poked into the live combat
      between answers is thrown away by the next one. Use starting statuses (`FightProbe.SoloAgainstHero`,
      new `FightProbe.RosterAgainstHero`) or let the fight produce it.
- [x] **IV-2 — Stage 3, the Granary Courts. DONE 2026-09-02.** Crocodile of the Short Measure, Jar-Seal
      Scarab Swarm, Hungry Grain Thief. Encounters 8–11.
      ▸ **What landed:** `Converter/ActFourGranary.cs` + 10 live tests (`Tests/ActFourGranaryTests.cs`). The
      first stage that needed NOTHING from the engine — the measure asks for the whole turn (3) and the
      Crocodile's own other jaw is what makes it unmeetable; the Swarm reads "did any of the three hits reach
      flesh" as health before/after; the Thief takes its cut of `burden_paid` at its OWN turn start against a
      bookmark, which is ordering-free and is the master's "once per card played" for nothing.
- [ ] **IV-3 — Stage 4, the Floodmark Basins.** Flood-Mark Reader, Drowned Field Scribe, Silt-Buried Farmer
      Shade. Encounters 12–15. §3.2 Observed Weighed Result lands here.
- [ ] **IV-4 — Stage 5, the Tribute Causeway.** Foreign Tribute Shade, Donkey of the Third Tally,
      Empty-Handed Envoy. Encounters 16–18.
- [ ] **IV-5 — Stage 6, the Corvée Yards.** Rope-Gang Wraith, Runaway Laborer, Stone-Hauler Ushabti.
      Encounters 19–21. First **Burdened** pressure block — the stage where the tax and the measure collide
      on purpose: paying the tax changes what the turn's actual expenditure comes to.
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
      The Treasury of the Two Pans (330).** Glyphs, Wrapping, Value-vs-Quantity accounting. The Cartouche is
      **Inscribed's boss-grade reader**: it writes Black/Golden Glyphs out of amplified applications, so this
      is where the ratified amplifier is either proven or shown to be underspecified.
- [ ] **IV-14 — Sphinx of the Processional Measure (344) · The Tombbreakers Three (112+100+108).**
      Voluntary ritual costs; a three-body kill-order elite (the Ant-Queen lesson: a body whose pool is
      emptied by anything falls — already fixed in Core).
- [ ] **IV-15 — Keeper of the Thirty-Six Decans (365) · Colossus of the Endless Procession (388).**
      The six-watch exam and the three-step discipline cycle; escalation is capped. The Colossus asks whether
      **a Burdened stack was worked off by playing a taxed card** — the observable-payment half of seam 2 is
      what this encounter is built on.
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

- [x] ★ the five-keyword decision — **ratified 2026-08-29**, written up above as canon
- [x] **IV-0 — vocabulary + Stage 1 — DONE 2026-09-02** (Core seams 1 + 3 bought; 2/5/6 compose; 4 open for IV-7)
- [x] **IV-1 — Stage 2, the Gate of Counted Names — DONE 2026-09-02** (5 identities / 7 encounters so far)
- [x] **IV-2 — Stage 3, the Granary Courts — DONE 2026-09-02** (8 identities / 11 encounters so far)
- [ ] IV-3 … IV-11 standards · IV-12 … IV-15 elites · IV-16 … IV-19 bosses · IV-20 … IV-21 cards+relics ·
      IV-22 … IV-23 events · IV-24 the act
- [ ] V-0 structure · V-1 … V-6 the six gods · V-7 the whole game
