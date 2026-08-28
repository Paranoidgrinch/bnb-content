# Acts I & II — completion plan

**Written 2026-08-22, from a live audit of all three layers** (bnb-content @86dded2, RogueDeck-Core @48d0e12,
bnb-godot). Goal: Act I and Act II **complete and playable end to end in Godot**.

Everything below was verified against the code, the generated blueprint or a live run — not inferred from the
design docs. Where a claim is a judgement call rather than a measurement, it says so.

---

## Where we actually are

| | State |
|---|---|
| **Combat content** | Act I: 25 standard identities, 10 elites, 5 bosses ✅ · Act II: 25 standards, 9 elites, 5 bosses with full phase structure ✅ |
| **Cards** | Final pool authored: 4 starters + 4 Junk + 80 Bureaucrat + 50 general ✅ (345 ship — the rest are ported v2 leftovers, see C-5) |
| **Relics** | Normal 50/50 ✅ · Shop 24/24 ✅ · Event 6/25 ❌ · **Boss 0/30** ❌ |
| **Events** | 15 shipped for Act I but they are the **ported v2 versions, not the Post-Audit canon** ❌ · Act II: 0/15 ❌ |
| **Run structure** | **One act only.** The run ends after the Act-I boss. Act II is authored, tested — and unreachable. ❌ |
| **Engine** | Multi-act, per-act map generation, per-encounter victory rewards, campfire upgrade, prompts, multi-body fights, save/resume — **all already present** ✅ |
| **Godot** | Title, character select, map, combat, events, shop, rewards, save/continue, desktop export ✅ · builds and smoke-runs against today's content ✅ |

The headline: **the engine is not the bottleneck.** Almost all remaining work is content, plus a short list of
Godot presentation gaps.

---

## CONTENT LAYER

### A. The act structure — blocking, do first

Nothing else in Act II matters until a run can reach it.

**A-1 · Declare two acts.** `BlueprintAssembler` sets `MapGeneration` (a single act). The engine reads
`RunBlueprint.Acts` — a `RunActPlan` per act, each with its own `MapGeneration` and its own seed
(`RunSetup.BuildActPlan`, seed stride 7919) — and `RunRunner` advances between them by itself, raising
`ActCompletedRunEvent` / `ActStartedRunEvent`. **No engine work. Content just has to fill the field.**

**A-2 · ⚠ LIVE BUG: the Act-I map draws Act-II encounters.** `MapSpecBuilder.PoolsByRole(data)` groups
*every* encounter that carries a role, with no act filter, and `NodeRefPools[Event]` takes every event.
Verified against a freshly generated blueprint: the Boss pool holds **all ten bosses** — `city_boss_01…05`
*and* `archives_boss_whispering_catalogue_boss`, `…_warden_of_sealed_volumes`, `…_curator_of_misplaced_hours`,
`…_auditor_of_returned_lives`, `archives_boss_grand_cross_reference`. An Act-I run can currently end against
the Grand Cross-Reference. Combat 46, MultiCombat 21, Elite 19 are likewise both acts mixed.
Fixing A-1 properly fixes this, because each act's spec gets its own pools — but it must be an explicit
per-act filter, and it deserves a test that pins each act's pools to that act.

> **The shipped export is deliberately stale.** `game.roguedeck.json` (here and in `bnb-godot/content/`) was
> last generated before Act II existed, so what ships today is a correct Act-I-only game. Regenerating it
> right now would make this bug live. **Do not run `sync-content.sh` until A-2 is fixed.**
>
> **★ OBSOLETE 2026-08-28.** A-2 was fixed in the act-seam work — `MapSpecBuilder` filters every pool to its
> own act — and the document has since been regenerated and synced with all three acts in it. Nothing here
> holds `sync-content.sh` back any more.

**A-3 · Author Act II's map spec.** Act I's rows / lanes / per-path minimums and maximums / mimic chance are
hand-written in `MapSpecBuilder`. Act II needs its own: its ten stages, its own lane flavours, and the mimic
chance the Run Systems Master sets for it (10 %, against Act I's 5 %).

**A-4 · Act II's node furniture.** `ShopId = "city-shop"`, `RestEventId = "rest:waiting-room"` and
`treasure:city-N` are Act-I-named singletons. Act II needs its own shop, campfire and treasure nodes.

**A-5 · `ConversionPools.Act = 1`.** The card-reward pool is gated to Act I, so Act-II fights would still hand
out Act-I cards. The pool has to follow the current act.

**A-6 · Title and presentation.** `GameTitle` still says *"Act I: The Old City Offices"*.

### B. Events — the phase has not started

**B-1 · Re-author Act I's 15 events against the Post-Audit master.** The shipped ones carry the same names but
different rules. Verified on *The Misfiling Cabinet*: the master says *"Transform 1 card; gain 50 Gold; a
different card is Misfiled next combat"*; the shipped v2 version transforms a card and adds a Duplicate Copy.
All 15 need re-reading.

**B-2 · Author Act II's 15 events.** None exist; `BabLoader` loads only `events/act_1_city_events.json`.

**B-3 · The shared act-level event objects** the master defines per act (its "Shared Act-I event objects"
section) are not built.

### C. Relics

**C-1 · Boss relics: 0 of 30 for Acts I–II.** Each Act-I–IV boss has exactly 3, and defeating it awards **1 of
its 3 at random, no choice screen**. Ten bosses across the two acts ⇒ 30 relics. The engine already supports
this precisely: `EncounterDefinition.VictoryReward` is per encounter, so each boss encounter carries its own
1-of-3 pool. **No engine work.**

**C-2 · Event relics: 6 of 25.** Each is tied to a named event branch, so this is blocked behind B — build
them with the events they belong to rather than as a separate pass.

**C-3 · Normal (50) and Shop (24) are complete.** No work.

**C-4 · Rebuild `brass_bookmark` as designed.** Its ADAPTATION note says retention is a property of a card
*definition* so it holds the whole hand instead. That is obsolete as of today's `RetainedCardMark` seam
(RogueDeck-Core @48d0e12) — it can now keep exactly the one card the design names. Small, and it removes a
documented deviation.

**C-5 · Drop the ported v2 card leftovers.** 345 cards ship; only 138 are canon. The rest survive because the
ported events still name them. Once B-1 lands, that dependency is gone and the pool can be exactly the canon.

### D. Run systems

**D-1 · The campfire is missing "Submit an Amendment".** The master gives it two actions — *Take Authorized
Leave* (Rest) and *Submit an Amendment* (Smith, upgrade a card). `EventTemplates.Rest` only offers the heal.
The run layer already has `UpgradeCardsRunEffect`, so this is authoring, not engine work.

**D-2 · Rest heals a different percentage per act** (Act I > Act II). One number per act, from the manifest.

**D-3 · Audit the shop against Run Systems Master §4.** Particularly the fixed inventory shape, the card and
relic generation rules, and the intended ~3/4 general : 1/4 character-specific relic split. The current shop
is close but was written before the master.

---

## ENGINE LAYER

**Nothing is known to be blocking.** Every capability Acts I–II need was checked and is present:

| Need | Already there |
|---|---|
| Several acts in one run | `RunBlueprint.Acts`, `RunSetup.BuildActPlan`, `RunRunner` auto-advance, `ActStarted/CompletedRunEvent` |
| A different map per act | `RunActPlan` holds its own generated map, own seed |
| A boss awarding its own relic | `EncounterDefinition.VictoryReward` (per encounter) |
| Campfire upgrade | `UpgradeCardsRunEffect` in the run layer |
| Option / card prompts | `PendingOptionChoice`, `PendingCardChoice` on the driver |
| Multi-body fights | proven by the Grand Cross-Reference's four bodies |
| Status display names for the UI | `StatusDefinition.DisplayNameKey`, reachable via `CombatState.DefinitionRegistry` |

**The one thing to expect:** authoring 30 boss relics and 30 events *will* surface new gaps, the way the cards
surfaced 12 and the elites 3. Budget for a small number of seams rather than assuming zero.

---

## GODOT LAYER

**G-1 · No act awareness at all.** No script mentions the act. The run would still *work* — the engine
advances by itself — but the player never learns they have entered Act II. Needs: the act on the map screen,
and an act-transition beat (title card) on `ActStartedRunEvent`.

**G-2 · Statuses render as raw ids.** `StatusLine` prints `s.DefinitionId.value`, so a player reads
`scheduled_the_collapse 2t` instead of *"Scheduled: The Collapse — 2 turns"*. This is the single biggest
readability problem in the game right now, because **every Act-II boss is built on visible state** — Authority,
Custody, Documentation, Discrepancy, the seals, the predictions, the filed hours. The fix is small:
`combat.State.DefinitionRegistry?.GetStatus(id).DisplayNameKey`. Durations and stacks already render.

**G-3 · Combatant counters are not rendered.** Some boss state is a counter rather than a status — the
Curator's dial sector, the Warden's announced seal type, the Catalogue's beat. Decide case by case: either
promote the player-facing ones to marker statuses in content (cheap, and keeps Godot generic) or render
counters. *Recommendation: promote in content* — the frontend should stay content-agnostic.

**G-4 · Phase bosses telegraph their Phase-I intent name.** A documented adaptation: the engine rotates one
intent list, so a slot keeps its authored name across phases. Worth surfacing the boss's phase marker
prominently so the mismatch reads as intended rather than as a bug.

**G-5 · Four bodies on screen has never been looked at.** The enemy row and click-to-target are generic and
should hold, but the Grand Cross-Reference is the first four-body fight. Needs an actual visual check.

**G-6 · Boss-relic award needs a beat.** "No choice screen" means it should still be *announced*.

**G-7 · No art ships.** Everything falls back to styled text panels. Playable, not finished — and explicitly
out of scope for "mechanically complete".

---

## Suggested order

1. **A-1 / A-2 / A-3 / A-4 / A-5 / A-6** — the act seam and the per-act pools. Ends with: a run walks Act I,
   meets an Act-I boss, and continues into an Act-II map that fields only Act-II content. This is also the
   bug fix, so it should not wait behind anything.
2. **G-2** — status display names. Five lines, and it makes everything built so far legible.
3. **C-1** — the 30 boss relics. Self-contained, and it makes both acts' bosses feel finished.
4. **B-1 / B-2 / C-2** — the 30 events with their event relics. The largest single block.
5. **D-1 / D-2 / D-3** — campfire Smith, per-act rest, shop audit.
6. **G-1 / G-3 / G-4 / G-5 / G-6** — the Godot presentation pass.
7. **C-4 / C-5** — the two cleanups, once nothing depends on the leftovers.

Steps 1–3 are the ones that turn "Act II exists" into "Act II is played".

---

## Status after the alpha pass (2026-08-27)

Acts I and II are **playable end to end**: three whole runs walked to the Act-II boss with no error, no loop
and no unanswerable state (`Converter --playtest`), and the same walk now stands in the suite as
`Tests/WholeRunTests`. What that pass changed:

| | |
|---|---|
| **B / C / D-1** | The campfire's *Submit an Amendment* is built (D-1). D-2 (per-act rest %) and D-3 (shop audit vs the master) are still open — both acts still heal 25 % and the shop is still the pre-master one. |
| **G-1** | Done: the act's name and the room count stand over the map, and crossing into Act II raises a title card. |
| **G-2** | Was already done (status chips read their authored names). |
| **G-3** | Still open: combatant COUNTERS are not rendered. The recommendation stands — promote the player-facing ones to marker statuses in content rather than rendering counters in Godot. |
| **G-4** | Still open (phase bosses telegraph their Phase-I intent name). |
| **G-5** | Two-body fights checked and read fine (`--smoke-ambush`); the four-body Grand Cross-Reference is Act V's and still unseen. |
| **G-6** | Partly: the boss's own relic arrives as a one-option pick that is announced, but it reads as "The spoils" rather than as the relic's name. |
| **G-7** | Unchanged — no art ships. |

### The three bugs the walk found

1. **A card wrote a nameless counter and killed the run** at the end of every fight it was played in (22 cards
   affected). C# static-field order; see ADAPTATIONS. Fixed, with `Tests/DocumentIdTests` as the net.
2. **The map was invisible.** Godot drew `Blueprint.Map`, which is empty in a generated game; the run's map is
   `RunState.Map`. It also read a room's kind off its id, so no boss, elite or ambush was ever labelled.
3. **The shop was an empty room.** The engine offers only AFFORDABLE choices, and a shop's question is asked
   from inside a visit that ends as the replay parks — so the shelf was gone by the time the UI drew it. The
   session now publishes it (`InteractiveRunSession.PendingShopShelf`) and the shop screen draws the whole
   shelf with prices, greying out what the purse cannot reach.

### The one thing that is not a bug and will get worse

Under the replay model every answer re-runs the whole run, so input latency grows with run length: about
0.2 s per answer early in Act I, ~0.6 s by the middle of Act II, and it keeps climbing. Two acts are playable;
five will not be. The fix already has its machinery — a run can be SAVED at an interlude and resumed
(`RunPlayback.SaveJson` / `Resume`), so the session could checkpoint there and replay only the answers since
the last interlude, which caps the cost at one node's worth of work.
