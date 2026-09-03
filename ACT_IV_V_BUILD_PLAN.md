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
| 4 | **`Replicated` applications** (§3.3/§3.4) | ✅ **BOUGHT (IV-7)** — `ApplyStatusEffectRequest.Replicated`, carried as far as the applied/merged event, read by `eventIsReplicated`; plus `ApplyTriggerEventStatusNode`, which applies the status the event was about (a copy needs to name a status it only learns at fire time). **The seam list is now closed.** |
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
- [x] **IV-3 — Stage 4, the Floodmark Basins. DONE 2026-09-02.** Flood-Mark Reader, Drowned Field Scribe,
      Silt-Buried Farmer Shade. Encounters 12–15. §3.2 Observed Weighed Result lands here.
      ▸ **What landed:** `Converter/ActFourBasins.cs` + 9 live tests (`Tests/ActFourBasinsTests.cs`). Nothing
      new from the engine again. **"Once per Weighed resolution" became arithmetic:** the resolution keeps two
      growing tallies (`measures_met`, `measures_failed`) and every body that answers resolutions keeps its own
      bookmark in one — so several bodies may listen to the same measure in any order (`ActFour.SinceLastLooked`
      / `MoveTheBookmark`, the idiom the Hungry Grain Thief already ate by). The Stage-2 watcher is now the
      shared `ActFour.FollowTheApplicant`, carrying both lessons it paid for (watch the EXPIRY; settle at the
      TURN start), and the Drowned Field Scribe is its second user.
- [x] **IV-4 — Stage 5, the Tribute Causeway. DONE 2026-09-02.** Foreign Tribute Shade, Donkey of the Third
      Tally, Empty-Handed Envoy. Encounters 16–18.
      ▸ **What landed:** `Converter/ActFourCauseway.cs` + 8 live tests (`Tests/ActFourCausewayTests.cs`).
      Nothing new from the engine. The Shade charges a sheet for the first measure MET each round
      (`measures_met`, ready since IV-3); the Donkey counts RESOLUTIONS through one bookmark in their sum
      (`ResolutionsSinceLastLooked`) and its third entry weighs 2, or 1 if that third measure was met. ★ The
      Envoy needed the player's HAND counted while it still exists — a turn-end rule cannot see it — so the
      count is taken on `CardsDrawn` and on `ActionResolved` (not CardPlayed: the card is still in hand while
      its own play resolves) and **only when the player is the actor**, since an enemy acts after the hand is
      discarded.
- [x] **IV-5 — Stage 6, the Corvée Yards. DONE 2026-09-02.** Rope-Gang Wraith, Runaway Laborer, Stone-Hauler
      Ushabti. Encounters 19–21.
      ▸ **What landed:** `Converter/ActFourYards.cs` + 8 live tests (`Tests/ActFourYardsTests.cs`). Nothing
      new from the engine. ★ **Fatigue now writes down that it actually took Energy** (`energy_taken_by_fatigue`)
      — losing a resource raises no event a rule can hear, and "has Fatigue" is a different fact; the Wraith
      keeps a bookmark in it. ⚠ The question had to be asked BEHIND the loss in a causal sequence and against
      the pool's MAXIMUM: the turn-start refill is an enqueued effect, so a program reading the pool before
      its own loss resolves sees last turn's leftovers (a three-act-old Minute Moth test caught it). The Laborer's escape reads the gang's brace BEFORE and AFTER the player's turn
      (no damage bookkeeping, once per turn by construction) and leaves by being downed, which is what makes
      the room resolve. Both scaling attacks are authored as `damage_per_status` so the telegraph carries the
      whole formula.
      ▸ (The plan's own line called this the first Burdened pressure block; the master's Stage 6 is Fatigue,
      escape and stone. The master governs — Burdened's pressure block was Stage 3.)
- [x] **IV-6 — Stages 7 + 8. DONE 2026-09-02.** Fallen Capstone Golem, Cornerstone Oath-Stone ·
      Palette-Bearing Apprentice, Hieroglyphic Complaint Wall. Encounters 22–27. §3.5 (Embalmed
      self-enabling) is binding for the Wall.
      ▸ **What landed:** `Converter/ActFourMonument.cs` + 10 live tests (`Tests/ActFourMonumentTests.cs`).
      Nothing new from the engine. Placement is a visible status plus an intent rule (`self_status` min 3),
      so the stone falls every fourth turn with the count on the body; Kept Oaths strike Broken Oaths off the
      record (one telegraphed scaling term, and "a later hit reduced by 4" is what cancelling one comes to);
      Fresh Pigment is an outgoing-application passive spent by the scribe's OWN entry; and the Wall makes
      both halves of its signature itself — `Fade` now records a preserved AFFLICTION (`decays_preserved`)
      and the Wall keeps a bookmark in it.
      ▸ Pinned by test: **five Entombed take the turn before the capstone can fall on it**, so the heaviest
      stone in practice lands at four. The act's two burial clocks meet there and do not stack.
- [x] **IV-7 — Stages 9 + 10. DONE 2026-09-02.** Sun-Seal Bearer, False-Seal Forger · Kneeling Petitioners.
      Encounters 28–33. **§3.3 + §3.4 (Replicated) are the whole point of this step.**
      ▸ **What landed:** `Converter/ActFourSeal.cs` + 8 live tests (`Tests/ActFourSealTests.cs`), and **the
      last row of the seam list**: `ApplyStatusEffectRequest.Replicated` (carried as far as the applied/merged
      event), `eventIsReplicated`, and `ApplyTriggerEventStatusNode` — a rule can now answer an application
      with an application of the SAME thing, which no content could express because a program had no way to
      name a status it only learns at fire time.
      ▸ ★ **Engine finding: a merge named the wrong body.** `StatusMergedCombatEvent` reported the existing
      instance's source, so every "did somebody ELSE just apply something?" rule was wrong whenever the status
      was already there. Fixed: the event answers "who did this?", the instance keeps its own source.
      ▸ ★ **A body's Block lives from its own turn until its next turn start** — which is why the support body
      acts FIRST in all five authored encounters of these stages, and why the tests pin that order.
- [x] **IV-8 — Stages 11 + 12. DONE 2026-09-02.** Natron Bearer, Linen-Wrapped Embalmer, Unfinished Mummy ·
      Fourfold Vessel Guardian. Encounters 34–40. §3.6: the Guardian cycles Body → Breath → Blood → Name, one
      office a turn, and the office is a **marker status**, not a counter.
      ▸ **What landed:** `Converter/ActFourLinen.cs` + 8 live tests (`Tests/ActFourLinenTests.cs`). Nothing
      new from the engine. Four conversions, each capped at once a round and each read from its own shape:
      the Natron Bearer off `decays_preserved` (the tally the Complaint Wall shares), the Embalmer off the
      amplification EVENT ("was the enlarged thing a wrapping, and did the register enlarge it?"), the Mummy
      off a Deed played while the player is preserved. The Guardian's four offices are four named marker
      statuses, each intent opening its own and closing the other three.
      ▸ The appendix's optional cycle guard (a Block turn) was declined: the master's signature is four
      offices and "then repeat", and a fifth step blurs the one thing the identity is for.
- [x] **IV-9 — Stages 13 + 14. DONE 2026-09-02.** False-Door Finder, Cursed Loot Bearer · Star-Table Scribe,
      Moon-Cycle Ibis, Eclipse Scarab. Encounters 41–46. §3.9: Stage 13 reintroduces **Act-III law** locally —
      Safe-Conduct is granted at combat start, never assumed. Reuses `ActThree`'s vocabulary; do not fork it.
      §3.7: the Ibis repeats **1 stack**.
      ▸ **What landed:** `Converter/ActFourWarrens.cs` + 11 live tests (`Tests/ActFourWarrensTests.cs`).
      Nothing new from the engine. §3.9 is one function — `ActFour.NecropolisOpening`, asked of the whole
      roster beside Act III's own `HeroOpening`, so a Finder duo still hands out ONE licence — and it grants
      **Act III's own statuses unchanged**: `green_docket_customs` + 1 `safe_conduct`. Nothing was forked, so
      three Trespass owed to the Finder become the Finder's Claim through the act-III customs verbatim, and
      the Claim is what `False Threshold` swings with (`damage_per_status` off the OWNER, so the telegraph
      carries the whole formula).
      ▸ The Finder answers the passage check on **its own turn start**, with one bookmark in each of the act's
      two tallies (met and missed) — the first body to need BOTH, because it gives a different thing for each.
      The Loot Bearer needed no rule beyond a bookmark in `burden_paid`: "once per card" is structural (the
      tally moves once per card) and the total needs no ceiling (a turn can only pay as many surcharges as it
      had Burdened for).
      ▸ ★ **The Ibis's memory is a FACE, not a variable.** A program can answer a status it learns at fire
      time (`Replicated`, IV-7) but cannot pocket one for three turns, so "remember the Last Rite" is two
      marker statuses on the body, written by the act's own `OriginalAfflictionOnThePlayer` gate. That buys
      more than storage: `Set the Rite` reads the same face and lays **the other** rite, so one pair of
      markers carries both the memory and what is coming — and the player reads both off the ibis.
      ▸ New probe primitive: **`FightProbe.SoloCycle`** — one authored body kept on SEVERAL of its intents, in
      order, with nothing else acting over the top. A body whose identity IS a cycle could not be tested
      before without dragging its whole encounter in (and the Scribe's Inscribed would have quietly resized
      every assertion about the Ibis).
- [x] **IV-10 — Stage 15, the Cartouche Chambers. DONE 2026-09-03.** Name-Erasing Chisel Spirit, Royal
      Genealogy Wall. Encounters 47–49. §3.8: **Royal Favor** is the Wall's own local resource; the player's
      status is never stolen, and a prevented gain grants nothing.
      ▸ **What landed:** `Converter/ActFourCartouche.cs` + 7 live tests (`Tests/ActFourCartoucheTests.cs`),
      and — against this plan's own expectation that the act was pure content from IV-8 on — **two small
      engine buys**, both general and both proved in Core:
      **(a) an application now reports HOW MUCH it landed.** `StatusMergedCombatEvent.AppliedStacks` (the
      delta; `Stacks` stays the resulting total) and `eventAmount` answering with it on both application
      events. §3.8 says "Royal Favor equal to the stacks gained", and a merge could only report the pile:
      one stack on top of three read as four.
      **(b) `eventPreventerIs` — which prohibition refused this?** The exact mirror of IV-7's
      `eventAmplifierIs` ("what paid for the enlargement?"). What was refused was already askable; who did the
      refusing was not, and a rule that answers its own refusals must not fire for a stranger's ward.
      ▸ ★ **ERASED is not REMOVED, and the stage is built on the difference.** The chisel is a PROHIBITION on
      the player (scope Buffs, one stack, re-set each round) — the only shape in this engine that means "never
      gained". A status that landed and was then stripped was still gained, and every rule that answers a gain
      would already have heard it, the Wall's lineage first. So §3.8's priority rule needs no priority table:
      an erased blessing raises no application, the Wall hears nothing, and a later one that survives is still
      the first it hears. The two bodies order themselves.
      ▸ The chisel is served with the fight (`HeroOpeningStatuses`) as well as topped up each round, because a
      fight's first round starts before its bodies are dressed — the same lesson `FollowTheApplicant` paid for
      at IV-0.
      ▸ Also new in the converter DSL: **`block_per_status`**, the defensive twin of `damage_per_status`, so
      the Wall's `Royal Line` telegraphs its whole formula instead of only its floor.
      **Acceptance for the standard roster's IDENTITIES is met here: 35 of 35** (Stages 16–17 are final forms
      of existing bodies and add none).
- [x] **IV-11 — Stages 16 + 17, the final forms. DONE 2026-09-03.** No new identities: Crooked Rod Bearer →
      Feather-Bearer, Crocodile → Crocodile Beneath the Balance, Stone-Hauler → Golden Ushabti Captain,
      Palette-Bearer → Eternal Reed Scribe, Cornerstone Oath-Stone → Oathbound Gate. Encounters 50–55.
      ▸ **What landed:** `Converter/ActFourBalance.cs` + 10 live tests (`Tests/ActFourBalanceTests.cs`) and
      the acceptance gate `Tests/ActFourPoolTests.cs` (6 tests). Nothing new from the engine, and **no new
      vocabulary**, which is both stages' whole point: every word here — the measure and its distance, Stone,
      preservation, Kept and Broken Oaths — is one the player was taught by the body now holding the office.
      ▸ The Feather-Bearer answers a resolution two ways and the SUCCESS is the interesting one: an exact
      measure opens the balance ON THE BEARER (+8 to every blow that lands) for exactly one player turn. The
      window is closed by the NEXT weighing rather than by a duration, because the answer fires at the
      bearer's own turn start and a duration counted from there would expire before the player could use it.
      A miss is 16 + 5 per point of distance, and the cap of 31 is reached at exactly three out — the widest
      a measure of 3 can be missed.
      ▸ The Crocodile's jaws open on either of two conditions the player can see (a failed weighing, or 3
      Entombed) and close on the bite. The Captain quarries the same Stone the Hauler did and spends it all
      on the Court. The Scribe's "Preserved Entry" IS Embalmed, one stack — the act's own preservation
      language rather than a second one, so nothing becomes permanent.
      ▸ ⚠ **One thing the master asks for is NOT implemented, and it needs an engine seam:** the Oathbound
      Gate's "import up to 2 visible stored Oath Memories **if the player previously encountered the
      Oath-Stone in the current run**". The engine has no run→combat memory — nothing between a finished
      encounter and the next one's roster build — so the Gate is fielded with 2 Broken Oath as ENCOUNTER
      SCAFFOLDING (`enemy_statuses`, the same seam Act III's Boundary Stone uses). That is visible before the
      first player action and capped at 2 exactly as the audit requires; what it is not is conditional. The
      seam it wants: a run-level counter written at encounter end and readable at encounter build.
      ▸ The Sealed Court trio is the **only Act-IV encounter with per-roster HP** (141/97/84 = 322). The
      master prices it explicitly (62–64% / 49–51% / 46–49%, 296–349 together) and three solo bodies would be
      595 against an act whose duos land at 300–360.
      **Acceptance MET: the standard pool is complete — 35 identities / 40 rosters / 55 encounters / 17
      stages, and every body inside its appendix HP band, pinned in `Tests/ActFourPoolTests.cs`.**

## Elites — 4 steps

Shared rules §6.1–6.5: elite-local counters stay encounter-local; a Weighed value must be achievable from the
deterministic current state; a queued signature is telegraphed and never interrupts a card or a Weighed
resolution. HP from the master's table.

- [x] **IV-12 — Surveyor of the Errant Cord (248) · Scarab Host of the Sealed Granary (255) ·
      Rope-Master of the Corvée (275 + summons). DONE 2026-09-03.** The Surveyor offers **two achievable
      Weighed values** — the solvability filter is its own machinery, written once and reused by every later
      elite.
      ▸ **What landed:** `Converter/ActFourElites.cs` (the shared layer) + `ActFourEliteSurveyor.cs`,
      `ActFourEliteScarab.cs`, `ActFourEliteRope.cs`, and 8 live tests (`Tests/ActFourEliteTests.cs`) plus an
      HP pin in `ActFourPoolTests`. **Nothing new from the engine.**
      ▸ §6.2 is `ActFour.Achievable(demand)` — clamped to the player's Energy pool at the moment the demand
      is made, floored at 1 — and it is in the shared file because the Surveyor is only the first body to
      ask: the Sphinx, the Decans and the Treasury all generate requirements, and a filter each of them
      re-derived would drift.
      ▸ **A choice an enemy offers is CARDS**, the Living Petition Chorus's idiom, and it carried both bodies
      that needed one: the Surveyor's two boundaries (whose figures are counters on the player, so one card
      pair covers every offer) and the Scarab Host's three seals (offered only for chambers still intact).
      Both exhaust at the turn's end, so refusing is an answer.
      ▸ ★ **BLOCK EXPIRES AT ITS OWNER'S TURN START, so "remove up to 10 current Block" answered at the
      elite's own turn start would always find nothing there.** What a stripped brace actually costs a body
      is the brace it does not get, so a far-boundary success leaves SLACK IN THE CORD: the next Block it
      gains is that much weaker, and the slack is spent making it. Same number, and it lands where the player
      can see it.
      ▸ ★ **A summoned body has no action script** — the engine's intent selector only knows the roster the
      fight opened with — so the Rope-Master's Haulers act the way every summon in this engine acts: a marker
      status with a turn-start program, which is also where the Hauler's death is heard. `countTargets` gives
      the "one hand works per enemy turn, taking it in turns" bound honestly, including the sole-hauler case.
      ▸ ⚠ **A damage-received trigger's SOURCE is the attacker, not the bearer.** The Scarab Host's break
      offer read its own seals off the player until a test caught it; a body must address itself through the
      rule it wears whenever the acting side is not its own.
- [x] **IV-13 — Keeper of the Living Cartouche (300) · Mummified Overseer of the Linen House (318) ·
      The Treasury of the Two Pans (330). DONE 2026-09-03.** Glyphs, Wrapping, Value-vs-Quantity accounting.
      The Cartouche is **Inscribed's boss-grade reader**.
      ▸ **What landed:** `Converter/ActFourEliteCartouche.cs`, `ActFourEliteOverseer.cs`,
      `ActFourEliteTreasury.cs` + 8 live tests (`Tests/ActFourEliteReadingTests.cs`). **Nothing new from the
      engine** — and the ratified amplifier came through: the Keeper needed no more than the two questions
      IV-1 and IV-7 bought (`eventAmplifierIs`, `eventStatusPolarityIs`), so the register is proven, not
      underspecified.
      ▸ ★ **AN AMPLIFICATION READS THE OTHER WAY ROUND FROM EVERY OTHER STATUS EVENT.** In that context
      `source` is the body the enlarged status LANDED ON — the one wearing the register — and `eventTarget` is
      whoever applied it, so a rule can answer the applier. The Keeper's glyph gate asked the event target for
      the applicant marker, which reads "did the PLAYER apply it": true of a blessing they cast on themselves
      (golden glyphs worked) and false of every curse the Keeper writes (black glyphs never landed). Second
      body of this session to be bitten by per-family source semantics, after the Scarab Host's damage
      trigger — the two are now written down together in ADAPTATIONS.
      ▸ The Overseer's wrapping needed a mirror for `decays_preserved`: **`decays_unpreserved`**, written at
      the same one fading point in `ActFour.Fade`. Tighten on a held affliction, loosen on a lapsed one — and
      reading a whole turn's worth at the Overseer's own turn start is what makes the master's "at most twice
      a round" enforceable at all, since a rule firing per fade could only count, never cap.
      ▸ The Treasury weighs a turn against ITSELF: cards played (junk not counted) against Energy actually
      spent, both read from expressions the act already had. Its Credits are cards again, and "once per player
      turn" is a latch the first draw sets on the player.
- [x] **IV-14 — Sphinx of the Processional Measure (344) · The Tombbreakers Three (112+100+108).
      DONE 2026-09-03.** Voluntary ritual costs; a three-body kill-order elite.
      ▸ **What landed:** `Converter/ActFourEliteSphinx.cs`, `ActFourEliteTombbreakers.cs` + 7 live tests
      (`Tests/ActFourEliteProcessionTests.cs`). **Nothing new from the engine.** The Sphinx is the fourth body
      to take the card-offer idiom — three prices, two shown a turn, no hidden right answer — and the
      Tombbreakers bring Act-III law into the tomb with them, so `NecropolisOpening` now answers for them too.
      ▸ ★ **A `SequenceEffectNode` does not see what it just wrote.** The Sphinx's third answer has to read
      the mark it left one node earlier, and it read the count from BEFORE — so the procession never opened
      on the answer that opened it. Causal sequencing is not a stylistic preference: any program that asks
      about state it has changed in the same breath must be a `CausalSequenceEffectNode`. The Scarab Host's
      seal card had the same latent shape (break the last seal, then ask whether any remain) and was fixed
      with it, untested and unnoticed until this one surfaced.
      ▸ ★ **A measure is never standing when an enemy acts.** Weighed is taken at the end of the turn it
      stands in and removes itself doing so, so the Sphinx's "3 per Act-IV negative status TYPE" can only ever
      meet Burdened and Entombed on its own: the reachable band is 25–31 against the master's stated 25–37.
      The Weighed term is kept — it is live against any body that raises a measure on its OWN turn, which is
      most of the act — but the Sphinx alone cannot reach it, and the cap of 37 never binds.
      ▸ Tomb-Preserved is deliberately NOT Embalmed, as the master insists: the act's preservation holds a
      fading thing in place on its wearer, and a robber wearing that could prolong its own afflictions.
- [x] **IV-15 — Keeper of the Thirty-Six Decans (365) · Colossus of the Endless Procession (388).
      DONE 2026-09-03.** The six-watch exam and the three-step discipline cycle; escalation capped. The
      Colossus asks whether **a Burdened stack was worked off by playing a taxed card** — and `burden_paid`,
      written at IV-0, answered it with nothing new.
      ▸ **What landed:** `Converter/ActFourEliteDecans.cs`, `ActFourEliteColossus.cs` + 5 live tests
      (`Tests/ActFourEliteExaminationTests.cs`). **Nothing new from the engine.**
      ▸ ★ **The Decans' examination teaches the act to itself, and a test caught it doing so.** Watch II hands
      the player the register and Watch III's burden arrives ONE LARGER unless they spent it — nobody wrote
      that, it is the five words meeting each other, and it is now pinned.
      ▸ ★ **A prohibition cannot answer its own last spend.** The spend is synchronous, inside the
      interception, so by the time the refusal event is handled the final stack is gone and the status with
      it — and a bearer-scoped trigger on the prohibition matches nothing. The Colossus's permanent refusal of
      outside Strength therefore re-arms from the BODY's rule status, which is never spent, and names the
      prohibition it is answering with `eventPreventerIs` (bought at IV-10, and this is what it is for).
      ▸ The **earliest-depth table is now data**: `earliest_depth_percent` on each elite encounter, pinned in
      `ActFourPoolTests` together with the rising curve — and the Tombbreakers as the master's stated
      exception to it, deeper than the Sphinx and lighter, because three bodies acting every round are worth
      more than their combined HP says. ⚠ Wiring the gate into generation waits for the act to become
      walkable: the generator gates events and treasure by ref id (`NodeRefMinimumDepthPercent`) and elites
      are not drawn from a ref pool today.
      **Acceptance MET: 10 elite encounters, the master's HP to the point, the earliest-depth table authored
      and pinned.**

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
- [x] **IV-3 — Stage 4, the Floodmark Basins — DONE 2026-09-02** (11 identities / 15 encounters so far)
- [x] **IV-4 — Stage 5, the Tribute Causeway — DONE 2026-09-02** (14 identities / 18 encounters so far)
- [x] **IV-5 — Stage 6, the Corvée Yards — DONE 2026-09-02** (17 identities / 21 encounters so far)
- [x] **IV-6 — Stages 7 + 8 — DONE 2026-09-02** (21 identities / 27 encounters so far)
- [x] **IV-7 — Stages 9 + 10 — DONE 2026-09-02** (24 identities / 33 encounters so far; **seam list closed**)
- [x] **IV-8 — Stages 11 + 12 — DONE 2026-09-02** (28 identities / 40 encounters so far)
- [x] **IV-9 — Stages 13 + 14 — DONE 2026-09-02** (33 identities / 46 encounters so far)
- [x] **IV-10 — Stage 15 — DONE 2026-09-03** (35 identities / 49 encounters — the identity roster is COMPLETE)
- [x] **IV-11 — Stages 16 + 17 — DONE 2026-09-03** (35 identities / 55 encounters — **THE STANDARD POOL IS
      COMPLETE**, pinned in `Tests/ActFourPoolTests.cs`)
- [x] **IV-12 — the first three elites — DONE 2026-09-03** (3 of 10 elite encounters)
- [x] **IV-13 — elites 4-6 — DONE 2026-09-03** (6 of 10 elite encounters)
- [x] **IV-14 — elites 7-8 — DONE 2026-09-03** (8 of 10 elite encounters)
- [x] **IV-15 — elites 9-10 — DONE 2026-09-03** (**THE ELITE POOL IS COMPLETE** — 10 encounters, pinned) · IV-16 … IV-19 bosses · IV-20 … IV-21 cards+relics ·
      IV-22 … IV-23 events · IV-24 the act
- [ ] V-0 structure · V-1 … V-6 the six gods · V-7 the whole game
