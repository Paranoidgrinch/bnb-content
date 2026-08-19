# Adaptations

Every place the RogueDeck port deliberately deviates from the original game, and why. Everything not
listed here is a faithful mechanical translation (verified by the tests against the real source data).

## Scope
- **Act I, Bureaucrat only.** One RunBlueprint = one map with one boss; the other eight classes and
  acts II–V are out of scope for the demo.
- The card types (action/form/argument/curse) and the `authority` resource have **no rules semantics
  in the original either** — they ride along as presentation tags; authority is dropped.

## Map
- The act is **generated per run** from a `MapGenerationSpec` (the document carries rules, not nodes),
  honouring the per-path minimums of `docs/bnb-act-map-specs.md`: 8 Combat, 1 MultiCombat, 1 Elite,
  3 Event, 2 Rest, 2 Treasure, 2 Shop on EVERY entry→boss path. The earlier port baked one seeded
  layout at conversion time; that is gone, along with its baked mimic roll and its baked
  `event_combat_chance` surprise-fight nodes.
- The treasure mimic is a live 5% per Treasure node (Act I; 10/15/20 in the later acts).
- The staged map has **no shop node type** in the original (shops appear via events/acts). The
  generated act gives Shop its own role — two per path — pointing at the same **city shop** the port
  authored (cards/relics at the original base prices, card removal 75g, reroll 25g).
- Generated fights pay out per ROLE (gold + a card offer; elite/boss/mimic add a relic) rather than
  per encounter, since the layout no longer knows which fight sits where.

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
- **"The first time each player turn the player directly applies a negative status"** (Counterclaim Imp) is
  read as any status the player files on it — a trigger program cannot see a status' polarity, and everything
  the Bureaucrat applies to an enemy is a debuff.
- **Sustaining Gavel copies Block only.** The design also copies half of a Strength gain, but Act I never
  applies more than 1 Strength at a time and half of 1 rounds down to nothing.
- **Roster order is turn order, and Block clears at its owner's turn start** — so a support body that copies
  an ally's Block (the Gavel) is fielded BEFORE the ally it sustains, or its copy is wiped moments later.
- **"The first time each round the player applies Paperwork to any enemy"** (Threshold Seizure Ward) is read
  as "the filer is the player and the target now carries Paperwork" — Paperwork is the only status the
  Bureaucrat files on enemies, and a program cannot read the event's status id. Its "Quarantine the Docket"
  likewise guards the whole enemy side with 2 Bookworm (no "my allies except me" selector).
- **"Panic removed through its normal decay"** (Number-Ticket Wisp) is a drop of exactly ONE stack: Panic
  sheds one per turn end, while a cleanse takes the whole pile at once. That is what lets the Wisp tell its
  own burnout from someone tidying up.
### Elites
- **The Three Appointments' countdowns tick at each body's OWN turn end**, not at the player's — the same
  once-per-round cadence, a beat later in the round. A countdown is armed at the body's first turn start
  (starting statuses cannot carry a counter value) and stays at 0 after expiring until a scheduling move sets
  a new one.
- **The anti-spike rule** ("at most one Appointment may accelerate per round") is a mark the accelerating body
  puts on the player for that round; the other bodies' intent rules read it and stand down to a safe move.
  Since an intent rule replaces whatever the cycle offered, a blocked body may skip a non-accelerating intent
  too — the alternative would need a "which intent is next" condition the engine does not have.
- **The Iron Warrant's orders are a fixed rotation of three**, each a check the engine can actually make at
  the player's turn end: pay the fee (end on empty Energy), file two kinds (two different card types played),
  observe the sequence (the turn's opening card is not an Attack). The design generates an order from the
  live state; a program cannot inspect a hand to pick an achievable demand, so the rotation stands in — it
  keeps the "never twice in a row" rule and never asks for the impossible.
- **Compliance Credit is dealt as damage.** "Remove up to 5 Block, the rest as HP loss" is exactly what 5
  damage does; the design wants the overflow not to count as a damage EVENT, which the engine cannot express
  without a bespoke effect.
- **The Appellate Staircase's Case moves at its holder's turn end**, with a "already moved this round" mark
  on the receiver so it cannot climb two Steps in one round. A round-end loop would be the natural place, but
  a program cannot read a counter off its ITERATION target there, only write to it.
- **The ruling is announced by whichever Step still holds the Case after trying to climb.** The design has the
  Case ascend past the highest LIVING Step; a program cannot ask whether a neighbour is alive, so a Step that
  tried to hand the Case on and still has it announces instead — killing the Upper Step delays the ruling by
  one round rather than preventing it.
- **The Remand is death PREVENTION, not a revive.** "The Phantom returns once at 24 HP" is authored as the
  engine's one-shot pre-down interceptor: reviving a downed body is impossible by construction — healing and
  status application refuse a downed target, and the program guard rejects such a program outright. The
  Writ's 12 HP is paid BEFORE the Phantom's +2 Strength, so the case cannot buff its own recoil.
- **The Phantom keeps Uncertain Remand in its cycle after a Final Judgment.** The design removes that intent
  from the pool; an intent cycle is fixed data, and the engine has no "drop an action" operation.
- **The Petition's clauses are CARDS.** A combat has no yes/no prompt, so each clause is a 0-cost card the
  Chorus puts in the player's hand at the start of their turn: playing it SIGNS (benefit now, liability
  recorded on the Petition), leaving it there REFUSES (its turn-end-in-hand program pays the Petition
  instead). Either way it exhausts, so a clause is offered once per reading cycle.
- **The Protective Clause's refusal grants the Petition 1 Strength** instead of "+3 on its next direct
  attack": a one-shot damage buff has no shape in this vocabulary, and Strength is what the Evidentiary
  refusal already gives.
- **The Monolith STORES damage by healing it back.** "While Closed, HP loss is stored as Pending Business
  instead of being removed" is realised as: the hit lands, the Monolith immediately heals exactly what it
  lost, and the amount is banked on a counter. The engine has no "redirect this damage into a track"; the
  numbers are identical, and Block still soaks first, but a combat log shows the hit and the heal.
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

- **The Seizure Procession marks a card when the player DRAWS, not at the turn's start.** Turn-start triggers
  run before the turn's draw, so at that moment the hand is still empty; the Lantern therefore rides on the
  engine's CardsDrawn event (added to the authoring vocabulary for this) and a latch status on the player
  keeps it to one card per turn.
- **Seized cards go to the exhaust pile and the tally is kept on the player.** A destroyed Cart does not hand
  its loot back — the design's "returned when the Cart falls" would need a zone the engine does not have. The
  seizure count lives on the player because every part of the program can address the player with a single
  selector, while an enemy inside a card loop cannot be read at all.
- **The Marshal's cap of +4 Strength is implicit.** The Cart's capacity of two seizures already keeps the
  Marshal at +2, so no separate tally is needed.

- **The portcullis rises the instant the threshold is crossed**, inside the damage trigger, rather than at the
  player's turn end. The design asks for the next-intent preview to refresh while the player can still react;
  a program cannot repaint a preview, but changing the gate before the turn ends means the intent the enemy
  then picks is the new band's — and any host that reads the intent live shows it. "Held Open" doubles as the
  once-per-turn latch: it marks the round as forced, so the gate does not settle back that round.
- **The two rulings of each gate band alternate on a beat counter** the Judicator flips at its own turn end.
  Intent rules can compare a counter but not a round's parity, so the beat stands in for "alternate".

