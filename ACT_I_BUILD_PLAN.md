# Act I — Real-Game Build Plan (converter → game.roguedeck.json → Godot)

## ▶ RESUME POINT (2026-08-19, bnb-content: step 3 COMPLETE, RogueDeck-Core @779cf9f)
All engine primitives for reworked Act-I enemies are DONE + pushed. Converter authoring pattern proven
end-to-end for owner-scoped passives, cross-combatant passives, intent rules, capped scaling. All suites
green (RogueDeck-Core Core 1402/Scenario 684/Run 448/Sandbox 298; bnb-content 69). ALL 25 identities final.

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
- **Stage 4 (Seal) — COMPLETE, all three identities + the Candle's support duo:**
  - `wax_notary` (48 HP): "Paper Seals Wax" — the first Paperwork RECEIVED each player turn → 5 Block.
    "Received" = the count went UP (remembered in a counter), so a duo partner's Bookworm cannot trip it.
  - `sealed_door_ward` (56 HP): "One Remaining Seal" — first card hit each player turn dealt 4 less
    (dampener status the seal re-arms and the first hit spends); 18+ HP in one player turn breaks the seal
    permanently (+6 recoil). The seal status's presence IS the "active" flag.
  - `oath_candle` (39 HP, 27 in its duo): "Witness the Seal" — the first time each round another enemy gains
    Block it gains 3 more. Needed the engine's new `BlockGained` event (@09d8298); the program uses
    `forEachTarget(alliesWithStatus(marker))` so the loop body is simultaneously the "is the Candle here",
    "is the gainer on its side" and "which combatant holds the latch" gate (`iterationTarget`).
  - **Per-roster HP override DONE**: `BabEncounter.enemy_health` (positional, null = the enemy's own max_hp)
    → `EncounterMapper`. Duos are unblocked; `city_normal_seal_08` is now "Witness at the Sealed Threshold"
    (Ward 39 / Candle 27).
- **Stage 5 (Ordinance) — COMPLETE, all three identities + the duo:**
  - `contradictory_signpost` (49 HP): the first card of the player's turn picks the road (Attack → Dangerous
    Shortcut 15, anything else → Long Administrative Route 9+9); no card at all → No Route Listed.
  - `exception_imp` (40 HP, 29 in its duo): "Loophole" — the first negative status the enemy side files on
    the player each round loses a stack (a single-stack filing is voided) and the Imp gains 1 Strength.
  - `old_statute_ghost` (54 HP, 38 in its duo): "Still in Force" — each round the first full disappearance of
    Panic/Doubt/Fatigue banks a Precedent; at 2 it re-files a stack of the one that just went. The cash-out
    lives in that status's own branch, so "the most recently disappeared" needs no extra memory.
  - `city_normal_ordinance_08` is now the duo "Exception to an Ancient Rule" (Imp 29 / Ghost 38).
  - **Two new authoring tools:** the hero carries an `the_applicant` marker in EVERY encounter (selectors are
    structural — this is the only way a passive can ask "did this happen to the player?"), and the
    **debuff mirror** (the enemy keeps `seen_<status>` counters and compares) answers "which status just
    moved", which no trigger program can read from the event. LIMITATION: statuses present at the first bell
    are invisible to the mirror (starting statuses raise no events) — see ADAPTATIONS.md.
- **Stage 6 (Delay) — COMPLETE, all three identities + the duo:**
  - `inverted_hourglass` (51 HP, 36 in its duo): "Stolen Sand" — every time Fatigue actually costs the player
    Energy (i.e. the player's Fatigue count DROPS) it banks a grain, max 3; Turn the Glass cashes them at
    8 + 4 each and empties the glass. New DSL: `damage_per_counter`.
  - `fading_number_token` (43 HP, 31 in its duo): "Your Number Is Fading" — 3 HP at the end of each of its own
    turns unless the player carries Fatigue. Owner-scoped, plain status trigger.
  - `minute_moth` (36 HP): "Stolen Minute" — a player turn ending on exactly 0 Energy hands it a minute; at 2
    an intent rule swaps in Wingbeat Delay, which spends them.
  - `city_normal_delay_08` is now the duo "The Hour Has Not Been Called" (Hourglass 36 / Token 31).
  - **GOTCHA found here:** a status whose last stack is spent raises **`StatusExpired`**, not StatusRemoved or
    StatusStacksChanged. Every mirror passive must listen for it — the Ghost's Stage-5 passive only worked
    because an unrelated status application happened to re-run it; it now listens properly.
- **Stage 7 (Appeal) — COMPLETE, all three identities + both encounters:**
  - `counterclaim_imp` (45 HP, 33 in its support encounter): "Counterclaim" — the first status the PLAYER
    files on it each turn is answered with 1 Paperwork. Owner-scoped; latch clears at its own turn end.
  - `sustaining_gavel` (44 HP, 30 in its support encounter): "Sustained" — the first Block another enemy
    gains each round is copied at half, rounded down. Never a solo.
  - `self_correcting_record` (53 HP, 40 in its duo): "Correct Against the Evidence" — the first card to land
    10+ on it each turn is studied and the next card of that TYPE deals 4 less, once. Needed a new engine
    capability (RogueDeck-Core @779cf9f): damage gated on the source CARD's tag, both as a passive-modifier
    restriction and as a trigger expression (`eventSourceCardHasTag`).
  - `city_normal_appeal_07` = "Sustained Counterclaim" (Gavel 30 / Imp 33, Gavel FIRST — see ADAPTATIONS),
    `city_normal_appeal_08` = "The Evidence Exists in Triplicate" (Record 40 / Examiner 30).
- **Stage 8 (Enforcement) — COMPLETE, and with it ALL 25 identities:**
  - `warrant_bailiff` (58 HP, 43 in its duo): "Outstanding Warrant" — +5 on its attacks while the player is
    4 Paperwork deep, as a buff a watcher switches on and off (a passive modifier cannot be conditional).
  - `threshold_seizure_ward` (61 HP, 45 in its duo): "Seize the Filing" — the first Paperwork the player
    files on an enemy each round gives that enemy 1 Bookworm, which erases the filing at its turn start.
  - `civic_battering_ram` (69 HP): Momentum to 4 (new DSL: `set_counter` with a `cap`), Ram the Case cashes
    it at 11 + 4 each, and "Break the Approach" costs it a Momentum the first time each player turn a card
    strips its Block away entirely (it remembers gaining the guard via its own BlockGained trigger).
  - `city_normal_enforcement_08` = the duo "The Warrant Seizes the Docket" (Bailiff 43 / Ward 45).
- **The two deferrals are CLOSED:** `number_ticket_wisp` (25 HP) — "Your Number Came Up" reads Panic's DECAY
  as a drop of exactly one stack, which the mirror can tell from a cleanse — and Duplicate Copy Mites'
  "Carbon Copies" (its duo `city_normal_seal_07` = "Certified Pest Control", Mites 26 / Notary 34).
- **Rule established: FINAL_AUDIT numbers WIN** over both the demo data and the older
  `Act_I_Final_Enemy_Pool.md` (they disagree on HP and intents) — rewrite HP + intents to the audit, keep the
  enemy id, keep an intent id only where the successor intent is the same mechanic.

**Immediate next steps (resume here):**
1. **Steps 3 and 4 are DONE.** Next is step 5: replace the baked map with a `MapGenerationSpec` built from
   the roles (`Encounters.ByRole` from the curated pools, `NodeRefPools` from the events/rest/treasure/shop,
   PerPathMinimums Combat 8 / MultiCombat 1 / Elite 1 / Event 3 / Rest 2 / Treasure 2 / Shop 2, mimic 5%,
   BalanceTargets), then step 6: an end-to-end smoke that generates an Act-I map and plays it headless.
2. **Open content debt:** the Elite and Boss pools are still the ported DEMO bodies (6 elites, 1 boss, and
   the mimic points at one of them). The audit's own Act-I elite/boss designs are a separate authoring job —
   see the Master Elite / Boss FINAL_AUDIT pools in ~/Downloads.
2. The remaining duos ("Wrong Window, Same Queue", "The Line Has Started Moving", "Certified Pest Control"
   = Notary 34 + Mites 26, …): the per-roster HP override is DONE, so these are now plain authoring.
3. Carbon Copies (Mites) can be authored with "Certified Pest Control" — the Oath Candle's Witness the Seal
   is the same shape (forEachTarget over `alliesWithStatus(marker)` + `iterationTarget` latch), on
   `StatusApplied`/`StatusMerged` with a bookworm gate instead of `BlockGained`.
4. Still deferred: Number-Ticket Wisp (telling Panic DECAY from any other reduction).

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

**Deferred — both CLOSED in Stage 8 (kept for the reasoning):**
- ~~**Duplicate Copy Mites "Carbon Copies"**~~ (first time each round ANOTHER enemy gains Bookworm → Mites gain 4
  Block) — author it with "Certified Pest Control" (Notary 34 + Mites 26); in a solo it can never fire. The
  Oath Candle's Witness the Seal is the finished template for it (see Stage 4), with `StatusApplied` +
  `StatusMerged` and a bookworm gate instead of `BlockGained`.
- Number-Ticket Wisp "Your Number Came Up" (reacts to PLAYER Panic DECAY — the decay is a stack change on the
  hero, so `StatusStacksChanged` (RogueDeck-Core @13e42ce) now sees it as an ENCOUNTER trigger; still needs a
  way to tell decay from any other reduction. Re-check when Stage 1 is revisited).
- ~~Duo/multi encounters need a per-roster HP override~~ — DONE (`enemy_health` on BabEncounter).

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

### 3. Act-I enemies (source-data + EnemyMapper) — 25 identities  *(DONE: 25/25, all eight stages)*
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

### 4. Act-I encounters + role pools (source-data + EncounterMapper) — DONE
`BabEncounter.role` (combat / multi_combat / elite / boss / mimic) marks which pool draws a template; the
demo's other encounters simply carry no role and are inert. The curated pool is exactly the audit's 32
(23 solos + 9 duos, every one of the 25 identities fielded, duos at their reduced HP) plus 6 demo elites,
the boss and one mimic. `Tests/ActOnePoolTests.cs` pins all of it.

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
