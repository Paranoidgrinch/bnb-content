# Adaptations

Every place the RogueDeck port deliberately deviates from the original game, and why. Everything not
listed here is a faithful mechanical translation (verified by the tests against the real source data).

## Scope
- **Act I, Bureaucrat only.** One RunBlueprint = one map with one boss; the other eight classes and
  acts II–V are out of scope for the demo.
- The card types (action/form/argument/curse) and the `authority` resource have **no rules semantics
  in the original either** — they ride along as presentation tags; authority is dropped.

## Map
- The original generates a fresh `staged_pilgrimage` layout per run; the engine's map is authored
  data. The converter runs the same generator rules (same weights, caps, lane-split logic) **once,
  seeded** — `--seed N` bakes a different map. Per-run map variety is a possible later engine arc.
- `event_combat_chance` (20% of event rolls become fights) is baked at conversion time, so the
  "surprise fight" is visible on the map instead of being a surprise.
- The treasure mimic (5%) is likewise rolled at bake time.
- The staged map has **no shop node type** in the original (shops appear via events/acts). One
  depth-5 combat node is deliberately replaced with the **city shop** so the ported game exercises
  the full shop machinery (buy cards/relics at the original base prices, card removal 75g, reroll 25g).

## Combat
- `weighted_random` intent patterns (2 of 128 enemies) fall back to the ordinary intent cycle.
- Enemy intents telegraph as **labels** ("Bite with Reservation (7)") — the number is baked into the
  label, not recomputed live.
- Paperwork/poison tick at the bearer's **turn start** (engine damage-over-time automation; kind
  DamageOverTime, block just cleared) instead of the original's turn end. Net effect per round is the
  same; a DoT can now finish an enemy before it acts.
- Doubt consumes one stack per DAMAGING action of the bearer; if the bearer also authored a ticking
  DoT on someone else, the tick would consume a stack too (no damage-kind filter on triggers) — no
  Act-1 content hits this combination.

## Reworked Act-I identities (production phase)
The final Act-I roster is authored from the FINAL_AUDIT master pools, which outrank the demo data and the
older Act-I enemy pool wherever HP or intents disagree. Readings taken where the design text is ambiguous:
- **"the first non-Junk card type of the turn"** (Wrong-Window Scribe, Triplicate Examiner) is read as
  literally the FIRST card's type — Junk is not skipped. The engine records the opening type per turn
  (`firstCardPlayedHasTag`); "first non-Junk" would need a second, junk-aware opening record.
- The counter passives fire on the **exactly-Nth** card of that type (2nd for the Scribe, 3rd for the
  Examiner), which is the design's "maximum once per player turn" without extra cooldown state.
- **Duplicate Copy Mites are ONE body** (37 HP) as the final design has them; the demo fielded two 16 HP
  "Duplicate Copy Mite" bodies in *Copies Upon Copies*, which is now that single solo. Their passive **Carbon
  Copies** is deferred to the Stage-4 duo (see ACT_I_BUILD_PLAN.md) — in a solo there is no other enemy that
  could gain Bookworm, so it can never fire.
- **"The first time each round another enemy gains Block"** (Oath Candle, "Witness the Seal") is latched on
  the CANDLE, so it is once per round overall, exactly as written. Encounter triggers cannot name a
  combatant, so the program loops over `alliesWithStatus(witness_the_seal)` — which is at once the "is the
  Candle in this fight" gate, the "is the gainer on the Candle's side" gate (the hero guarding itself finds
  nobody) and the handle on the latch holder (`iterationTarget`).
- **The Signpost's two roads are Attack vs. anything else.** B&B has no "Skill" card type — its non-attack
  pool is forms and arguments — so LEFT is a card tagged `attack` and RIGHT is any other card. "Both
  directions mandatory" is realised as: the FIRST card of the player's turn picks the road.
- **"The first time each round the enemy side would apply a negative status"** (Exception Imp, "Loophole") is
  undone a beat AFTER it lands rather than intercepted: the engine's data-authored interceptors read the
  TARGET's statuses, and here the exception belongs to an enemy. A single-stack filing is therefore applied
  and immediately removed, which is invisible in the numbers but visible in a combat log.
- **A status whose LAST stack is spent raises `StatusExpired`**, not `StatusRemoved` or
  `StatusStacksChanged` — every mirror passive (Imp, Ghost, Hourglass) listens for it, or it never sees the
  moment a debuff is finally gone.
- **The Imp and the Ghost read "which status just moved" from a mirror** of the player's debuff counts kept
  in their own counters (a trigger program cannot read the event's status id). A status the player already
  carries when the fight BEGINS is invisible to that mirror until something touches it again — starting
  statuses are applied without raising events. No Act-I encounter opens the player with Panic/Doubt/Fatigue.
- **"Exception to the Exception — another enemy gains 7 Block; if solo, the Imp gains 10"** guards the whole
  enemy side for 7 (the Imp included). There is no "my allies except me" selector in the authoring surface,
  and the solo's 10 would need a roster-size condition.
- **"Direct attacks gain +3 damage per Panic, maximum +9"** (Queue-Crier Homunculus, "Lost Your Place") is
  baked into the enemy's one pure ATTACK intent — "Call a Number That Is Not Yours", the intent the design
  itself annotates with the passive. Its mixed damage+Panic and block+Panic intents stay flat, so the
  Panic cash-out is one telegraphed hit per cycle rather than a permanent global multiplier.

## Events
- `lose_hp` keeps the original's "events cannot kill" clamp (computed damage, min HP 1).
- `heal_percent_max_hp` keeps the original's round-up.
- `duplicate_card` picks distinct random cards (the original could roll the same card twice).
- `gain_relic` draws from the event-eligible pool (non-boss, class-allowed) but **may duplicate a
  relic the player already owns** (pools cannot exclude owned entries at runtime).
- `open_shop` (1 event) → a card-reward offer instead (events cannot open shops).
- `next_combat_card_reward_bonus` (1 event) → an immediate extra card offer (reward-offer modifiers
  are not serializable).
- `next_combat_enemy_hp_loss_percent` (1 event) → flat opening damage (25% of ~30 HP = 7) to all
  enemies; the per-target percent needs an amount shape the curated model doesn't have.

## Relics
- Pickup effects (heal / gold / +max energy) are **bundled into every grant site** (reward offers,
  shop entries, event pools) — same moment as the original's on-pickup hook.
- `increase_max_energy` uses the engine's `resourceMax.` counter: permanent for the run (the original
  also never removes relics mid-run).
- `increase_card_reward_count` (3 relics) → **+15 gold per combat victory** (offer-count scaling is a
  code escape, not data).
- `shop_price_discount` (2 relics) → **+10 gold back per shop purchase** (a rebate instead of a
  discount; prices are per-entry data).
- `increase_gold_rewards` is faithful (flat bonus per combat victory).

## Rewards
- Post-fight rewards: gold (original difficulty ranges) + pick 1 of 3 pool cards; elites and the boss
  add a random relic. The card pick has **no skip button** at the engine level — the host UI decides
  whether to surface one.
