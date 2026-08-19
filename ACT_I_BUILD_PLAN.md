# Act I — Real-Game Build Plan (converter → game.roguedeck.json → Godot)

## ▶ RESUME POINT (2026-08-19, bnb-content: Stages 1-3 done, RogueDeck-Core @13e42ce)
All engine primitives for reworked Act-I enemies are DONE + pushed. Converter authoring pattern proven
end-to-end for owner-scoped passives, cross-combatant passives, intent rules, capped scaling. All suites
green (RogueDeck-Core Core 1402/Scenario 684/Run 448/Sandbox 294; bnb-content 46). 9 of 25 identities final.

**Authored so far (real final identities):**
- Stage 1 — `a_very_official_line` (new id): full pattern (queue_advances passive + self_counter intent rule
  + special "everyone_moves" reset). city_easy_01 repointed to it.
- Stage 1 — `queue_crier_homunculus` (31 HP): "Lost Your Place" is baked into its one pure ATTACK intent
  (`call_a_number_that_is_not_yours` = 7 dmg +3 per Panic, cap +9, Panic not consumed); the mixed and block
  intents stay flat. Reading documented in ADAPTATIONS.md ("Reworked Act-I identities"), which now also
  records the opening-type + exactly-N readings of the two counter passives. `EnemyMapper.Label` now
  telegraphs the whole scaling formula ("7 dmg +3 per Panic (max +9)") — the base was missing before.
- **Stage 2 (Counter) — COMPLETE, all three solos:**
  - `wrong_window_scribe`: "Not This Counter" cross-combatant passive (EncounterPassives).
  - `receipt_eyed_clerk` (35 HP): the Doubt cash-out is a pure INTENT — `date_discrepancy` =
    `damage_per_status` doubt, 6 base +2/stack, cap 8, joined to the 4-intent cycle (Ask for Proof /
    Receipt Lash 10 / Reconcile the Date 7 block + 1 Doubt / Date Discrepancy). No passive, no rule.
  - `triplicate_examiner` (41 HP): "Three Copies Required" — on the player's 3rd card of the turn's opening
    type, 8 Block for the Examiner + 1 Doubt for the player. Shared helper
    `EncounterPassives.OnNthCardOfTheOpeningType(n, effect)` now backs both counter passives (exactly-N ⇒
    once per player turn without cooldown state). Intents rewritten to the FINAL_AUDIT numbers.
- **Stage 3 (Form) — COMPLETE, all four solos** (no duo in this stage by design):
  - `filing_beetle` (40 HP): Bookworm tutorial — Worm-Eaten Folio gives ITSELF 2 Bookworm + 6 Block.
  - `unsigned_form_ghost` (43 HP): "Still Missing a Signature" — watcher status toggles a shield status
    carrying ScalePercent 75 (Direct), because passive modifiers cannot be conditional.
  - `duplicate_copy_mite` (37 HP, now ONE body, name "Duplicate Copy Mites"): Spread Through the Binding =
    side-wide Bookworm +1 plus one more on itself (that is the design's "Mites gain 2 instead"). Its passive
    Carbon Copies is deferred to the Stage-4 duo — see Deferred.
  - `blank_line_leech` (45 HP): "Feed on the Filed Margin" baked into Blank-Space Bite via the new
    `status_on` + `per_stacks` DSL fields.
  - **Vocabulary added: `bookworm`** (StatusMapper, raw program; min(P,B) via a branch, no scratch value) and
    the engine's `StatusStacksChanged` trigger event.
- **Rule established: FINAL_AUDIT numbers WIN** over both the demo data and the older
  `Act_I_Final_Enemy_Pool.md` (they disagree on HP and intents) — rewrite HP + intents to the audit, keep the
  enemy id, keep an intent id only where the successor intent is the same mechanic.

**Immediate next steps (resume here):**
1. Stage 4 (Seal family): `wax_notary`, `sealed_door_ward`, `oath_candle` — then Stages 5-8 (Ordinance /
   Delay / Appeal / Enforcement), 4 identities each.
2. The duos ("Wrong Window, Same Queue", "The Line Has Started Moving", the Stage-4 Mites duo, …) need the
   per-roster HP override first (step 4 format gap) — the audit gives them REDUCED per-encounter HP.
3. Deferred passives to pick up with their encounters: Carbon Copies (Mites, Stage-4 duo), Number-Ticket Wisp.

**Test harness:** `Tests/FightProbe.cs` carves a ONE-FIGHT blueprint out of the real converted game (single
combat node → probe encounter cloned from the enemy's authored roster entry), so any signature can be asserted
in a live fight: `FightProbe.Solo("<enemy>", "<intent>", ("paperwork", 5))` → `FightProbe.Start(probe)`.

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
- **Duplicate Copy Mites "Carbon Copies"** (first time each round ANOTHER enemy gains Bookworm → Mites gain 4
  Block) — author it with the Stage-4 duo: in a solo no other enemy exists, so it can never fire. Sketch: a
  marker status on the Mites so a cross-combatant encounter trigger can target them
  (`AllAlliesOfSourceWithStatus`), an `eventTarget`-has-no-marker gate to mean "another" enemy, a
  bookworm-stacks gate as the "which status was applied" proxy (encounter triggers carry no filters), and a
  once-per-round counter — whose RESET needs care, because RoundStarted status triggers carry no bearer filter.
- Number-Ticket Wisp "Your Number Came Up" (reacts to PLAYER Panic DECAY — the decay is a stack change on the
  hero, so `StatusStacksChanged` (RogueDeck-Core @13e42ce) now sees it as an ENCOUNTER trigger; still needs a
  way to tell decay from any other reduction. Re-check when Stage 1 is revisited).
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

### 1. Statuses (StatusMapper) — DONE
- Panic / Doubt / Paperwork / Fatigue / Strength ported; **Bookworm DONE with Stage 3**. Both subtleties
  resolved: (a) the ordering needed an engine fix — damage-over-time now reads its stacks at RESOLUTION
  (RogueDeck-Core @a428156), so a turn-start trigger can shrink the tick it precedes; (b) min(P,B) needs no
  scratch value — branch on which side is smaller and each removal reads the status that has not been touched
  yet. Proven in live fights by `Tests/BookwormStatusTests.cs`.

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

### 3. Act-I enemies (source-data + EnemyMapper) — 25 identities  *(IN PROGRESS: 9/25 final; Stages 1-3 complete)*
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
