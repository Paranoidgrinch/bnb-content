# Act I — Real-Game Build Plan (converter → game.roguedeck.json → Godot)

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
- Panic / Doubt / Paperwork / Fatigue / Strength already ported. **Add Bookworm** (positive enemy status:
  before the bearer's Paperwork resolves, remove min(Paperwork, Bookworm) of each). Author as a TurnStarted
  trigger ordered BEFORE the Paperwork DoT tick; prove the ordering in the smoke test.

### 2. Enemy model + DSL extensions (BabModel / EffectMapper / EnemyMapper)
The reworked enemies need three things the demo converter lacks:
- **Passives / reactions:** add a `passives` (triggered-program) shape to `BabEnemy` → engine triggered
  effects. Most Act-I signatures are passives ("The Queue Advances", "Lost Your Place", "Your Number Came
  Up", "Not This Counter", "Three Copies Required").
- **Richer effect DSL** (`EffectMapper`): map the recipe-catalogue patterns — scaling attack
  (`damage_per_status`, already present), status thresholds, card marks (Misfiled/Referenced/Redacted),
  counters/tracks (Queue Position), source-scoped stacks.
- **Conditional intents / one-shot overrides:** add intent rules to `BabEnemy` → `EnemyIntentRule`
  (SelfHasCounter for "replace next intent with Everyone Moves at Once", etc.).

### 3. Act-I enemies (source-data + EnemyMapper) — 25 identities
Rewrite `source-data/enemies/city_enemies.json` to the FINAL roster with HP/intents/passives from the
`...Standard_Encounter_Pools...(1).md` list, cross-checked against the FINAL master pool. Author each
signature via the recipe catalogue.

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
