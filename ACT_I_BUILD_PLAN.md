# Act I — Real-Game Build Plan (converter → game.roguedeck.json → Godot)

## ▶ RESUME POINT (2026-08-18, bnb-content @7a92066, RogueDeck-Core @b645378)
All engine primitives for reworked Act-I enemies are DONE + pushed. Converter authoring pattern proven
end-to-end for owner-scoped passives, cross-combatant passives, intent rules, capped scaling. All suites
green (RogueDeck-Core Core 1401/Scenario 684/Run 448/Sandbox 292; bnb-content 28).

**Authored so far (real final identities):**
- Stage 1 — `a_very_official_line` (new id): full pattern (queue_advances passive + self_counter intent rule
  + special "everyone_moves" reset). city_easy_01 repointed to it.
- Stage 2 — `wrong_window_scribe` (existing id): "Not This Counter" cross-combatant passive (EncounterPassives).

**Immediate next steps (resume here), Stage 2 completion — enemies already exist in demo data, ENHANCE in place:**
1. `receipt_eyed_clerk` (demo hp 24; design 35): add a "Date Discrepancy" intent = `damage_per_status`
   target player, status `doubt`, amount 6 (base), amount_per_stack 2, cap 8. No passive. (Its current intents
   are ask_for_proof_of_arrival / receipt_lash / question_the_date — add the discrepancy attack.)
2. `triplicate_examiner` (demo hp 33; design 41): add "Three Copies Required" cross-combatant passive to
   EncounterPassives — on the player's 3RD card of the turn's opening type (cardsPlayedThisTurnWithTag==3 AND
   firstCardPlayedHasTag), the Examiner gains 8 Block AND the player gains 1 Doubt. (Mirror NotThisCounter but
   ==3 and add an ApplyStatus(doubt) to the hero/eventTarget-from-source side.)

**Migration learnings (apply going forward):**
- Enemy ids OFTEN already exist in demo data → ENHANCE in place (add starting_statuses / intent_rules / a
  Date-Discrepancy-style intent), do NOT append duplicates (queue_crier_homunculus collided). Check with
  `grep -c '"id": "<id>"' source-data/enemies/city_enemies.json` first.
- Demo HP values differ from the design's final HP — decide per enemy whether to update max_hp to the design
  value (recommended for faithfulness).
- Passive taxonomy: owner-scoped reaction (bearer's own event, reads opponent) → status starting_status
  (PassiveStatuses); reacts to a PLAYER action → EncounterPassives (per-encounter trigger, target
  AllEnemiesOfSource); pure scaling → bake into the intent (capped damage_per_status).
- Cross-combatant selectors must be SERIALIZABLE (LowestHealthEnemyOfSource / AllEnemiesOfSource — NOT
  FirstTarget, an escape node).

**Deferred (need more engine or are fiddly):**
- Bookworm (2 subtleties: fire before Paperwork DoT handler; min(P,B) removal can't be a naive sequence).
- Number-Ticket Wisp "Your Number Came Up" (reacts to PLAYER Panic DECAY — no per-stack-decay trigger event
  exists; would need a StatusStacksReduced trigger event).
- Duo/multi encounters need a per-roster HP override (encounter format gap) — add to BabEncounter/EncounterMapper
  before authoring the multi encounters (step 4).

---


Production phase: this is no longer the demo port. Act I is rebuilt to the FINAL reworked designs
(25 identities / 32 encounters) and the maps become **procedural hybrid** (fixed structure, random layout
per run) using the engine's `RuleBasedMapGenerator`. Studio is out of scope. The converter references the
engine via ProjectReference, so engine changes are picked up immediately.

Recipes for translating enemy signatures → engine constructs live in
`../RogueDeck-Core/docs/bnb-content-authoring-guide.md`; the per-path map minimums in
`../RogueDeck-Core/docs/bnb-act-map-specs.md`.

## Build order (dependency-first; each step builds + tests + commits)

### 0. Engine prerequisites (RogueDeck-Core) — needed before the procedural map is faithful
- **DONE:** mimic-in-treasure (`TreasureMimicChancePercent`), no-repeat encounter draw, `MultiCombat` role.
- **DONE — non-combat node pool variety:** `MapGenerationSpec.NodeRefPools[kind]` — each Event/Rest/Treasure/
  Shop node draws a distinct ref without replacement (mirrors the combat pool). Falls back to single
  `NodeRefs[kind]`. (RogueDeck-Core @4b78320.)

### 1. Statuses (StatusMapper)
- Panic / Doubt / Paperwork / Fatigue / Strength already ported.
- **Bookworm — DEFERRED to step 3 (author with its enemy).** Two real subtleties found: (a) it must fire at
  the bearer's turn start BEFORE the Paperwork DoT event-handler (ordering, not just a TurnStarted trigger);
  (b) "remove min(Paperwork, Bookworm) of each" can't be a naive Sequence — the second `modifyStatusStacks`
  reads the already-reduced value, so it needs the min captured once (scratch/result-store or a raw
  EffectProgram). Do it in-combat-tested when an enemy fields it.

### 2. Enemy model + DSL extensions (BabModel / EffectMapper / EnemyMapper)
Key de-risking: `EncounterEnemy` already supports `StartingStatuses` + `IntentRules`, so passives + conditional
intents need **no engine change** — passive = a status-with-triggers carried from combat start.
- **DONE — passive + intent-rule plumbing** (@753bad1): `BabEnemy.starting_statuses` + `intent_rules` →
  `EncounterEnemy`, with the condition vocabulary. `EnemyPassiveAndIntentRuleMappingTests`.
- **DONE — passive STATUS mechanism** (@9164448): `PassiveStatuses` authors reactions as statuses-with-triggers
  built from RAW EffectPrograms (serialized via CombatJson), appended to the blueprint, applied via
  `starting_statuses`. First passive `queue_advances` proven end-to-end. Also fixed a recurring engine gap:
  SetCombatantCounter + 4 Selected* node executors were missing from StandardCombatPackage
  (RogueDeck-Core @e48d6dd). The remaining Act-I passives ("Lost Your Place", "Your Number Came Up", "Not This
  Counter", "Three Copies Required", …) are authored **alongside their enemies in step 3** — each needs its own
  selector/timing choices and is in-combat tested there.
- **DSL note:** Act-I INTENTS are covered by the existing EffectMapper effect types (damage/block/status/
  `damage_per_status`/…). New effect types are added per-need when an enemy intent requires one (step 3), and
  for Act-II card marks (mark ops via RAW `EffectProgram` — not in `CombatNodeModel`). No speculative DSL.

### 3. Act-I enemies (source-data + EnemyMapper) — 25 identities  *(IN PROGRESS: pattern proven, 1/25)*
Rewrite `source-data/enemies/city_enemies.json` to the FINAL roster with HP/intents/passives from the
`...Standard_Encounter_Pools...(1).md` list, cross-checked against the FINAL master pool. Author each
signature via the recipe catalogue.
- **DONE — full pattern proven end-to-end** on the first final identity **A Very Official Line** (@c476c81):
  cycled intents + a SPECIAL intent kept out of the cycle (`BabIntent.special`), a passive
  (`queue_advances`, raw-program status), and a `self_counter` intent rule firing the special + resetting the
  track. DSL added as needed: `damage_per_status` base+cap, `set_counter`. All tested.
- **Migration note:** the demo data already holds many enemies (109 encounters); some ids already exist
  (e.g. `queue_crier_homunculus`). When adding a final identity, CHECK for an existing id — either replace
  that enemy in place or use the final id if free. Do NOT append a duplicate.
- **DONE — engine: per-encounter cross-combatant triggered effects** (RogueDeck-Core @b645378):
  `EncounterDefinition.TriggeredEffects` (event + CombatJson program, no bearer filter) registered into only
  that encounter's combat. This unblocks enemy passives that react to PLAYER actions ("Not This Counter",
  "Three Copies Required", etc.) — impossible as owner-scoped status triggers.
- **TODO — converter wiring for cross-combatant passives:** an `EncounterPassives` module (like PassiveStatuses,
  raw programs) keyed by enemy id; `EncounterMapper` aggregates the triggers of the encounter's enemies into
  `EncounterDefinition.TriggeredEffects`. Target the enemy via `AllEnemiesOfSource` (source = the acting hero).
- **TODO — remaining 24 identities** (Stages 1–8): mechanical replication of the above pattern, per enemy:
  read its design, author intents (existing DSL + extend per-need), author its passive if any (PassiveStatuses,
  raw program; cross-combatant reactions like "Your Number Came Up" pick a serializable single-opponent
  selector), wire intent rules, point an encounter at it, test. Curate encounters toward the final 32.
- **Format gap noted:** duo/multi encounters use REDUCED per-enemy HP (design "Duo HP Scaling"), but the
  encounter format has no per-encounter HP override (uses the enemy's max_hp). Add a per-roster HP override to
  `BabEncounter`/`EncounterMapper` when authoring the multi encounters (step 4).

### 4. Act-I encounters + role pools (source-data + EncounterMapper)
Rewrite `source-data/encounters/act_1_city.json` to the 32 templates (23 solo + 9 multi). Classify into the
map spec's role pools: `Combat` (solo), `MultiCombat` (2+ enemies), `Elite`, `Boss`, `Mimic` (≈ a weak
Act-I elite).

### 5. Map: MapBaker → MapGenerationSpec (procedural hybrid)
Replace the baked fixed map with a `MapGenerationSpec` on the blueprint:
- `PerPathMinimums`: Combat 8, MultiCombat 1, Elite 1, Event 3, Rest 2, Treasure 2, Shop 2.
- `TreasureMimicChancePercent = 5`.
- `Encounters.ByRole` = the role pools from step 4; `NodeRefPools` = the event/rest/treasure/shop pools.
- `BalanceTargets` from the design's per-stage HP/intent bands.
Keep the authored events/shops/rest/treasure as the ref pools. Drop `baked.Map` (or pass a trivial
placeholder; `RunSetup` uses `MapGeneration` when present).

### 6. End-to-end smoke (Tests)
Extend the existing `RunPlayback.BuildContent` end-to-end test: generate an Act-I map, assert the per-path
minimums hold (via `MapConstraintValidator`), and play a full run headless. Regression gate.

## Then: Acts II–IV
Same pattern per act (statuses/vocabulary → enemies → encounters → map spec), with the act-specific systems
(Overdue/Misfiled/Referenced/Redacted; Safe-Conduct/Trespass/Claim/Wergild; Weighed/Inscribed/…; Act-V gods)
and the rising per-path minimums + mimic chances (10/15/20).
