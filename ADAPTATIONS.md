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
