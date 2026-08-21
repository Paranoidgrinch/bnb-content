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

- **The Final Notice sits on the PLAYER, not on the Knight.** The countdown, the served notice and the
  acknowledgement are statuses on the applicant: every program that has to read them runs from a player-turn
  trigger, where the player is the single-selector Source, and the Knight's intent rules read them as opponent
  statuses. It also puts the deadline where the player can see it.
- **The acknowledgement is a card, like the Petition's clauses.** A combat has no yes/no prompt, so the offer
  is dealt into the hand on the response turn: playing it signs, leaving it there refuses, and it exhausts
  either way so the offer stands for exactly one turn.
- **A downed combatant's own statuses read as absent**, so the Spear's death trigger cannot check that the
  fallen body was the Spear. It asks instead whether the fallen one has an ALLY wearing one of the Knight's
  phase mirrors — true only for the Spear, since nothing else in the fight is the Knight's ally.
- **"Final Notice, maximum 3" is clamped at the player's next turn end**, not at the moment the Spear's death
  pushes it up: the push comes from the enemy side, where the player's stacks cannot be read. A notice at 4 is
  visible for one enemy turn and then spends the extra stack instead of banking it.
- **The Knight's Last Warning and the Spear's Authorized Pierce have no cooldowns**; their fixed cycles space
  them out (every fifth and every fourth intent), which is what the design's cooldowns amount to here.

## The Deputy Undersecretary (Act-I boss)

- **The Desk files its Matters itself, at the player's turn start**, up to two a turn and never past three open.
  The design hangs matter creation on individual intents; an intent program runs from the boss's side, where
  the player's Desk cannot be read at all (the player is only reachable through a multi-target selector). The
  Desk therefore lives entirely in player-turn triggers, and the "desk full" rule still pays the Deputy its 6
  Block. Matter ELIGIBILITY (only demands the current hand could theoretically meet) is dropped: a program
  cannot inspect the hand for playable types.
- **Four of the six Matters are implemented.** Complaint (12 damage in a turn), Petition (10 Block in a turn),
  Request for Additional Review (its own action, see below) and Notice of Missing Response (one Attack and one
  Form). "Application Fee Outstanding" duplicates the Review, and "Request for Clean Record" needs the engine
  to tell a self-inflicted status from a boss-applied one, which it cannot.
- **The Review's "boss-context action, not a card" IS a card** (File the Request, 1 Energy), dealt into the
  hand with the Matter — the same device the Petition's clauses and the Knight's acknowledgement use, because
  a combat has no boss-context button.
- **Backlog is kept twice**: on the player (where the turn-end program can read it to cap it) and mirrored onto
  the Deputy (where its own turn-start check and the transition read it). Each side only writes what it can.
  Per category it stops at 2 — the maximum Executive File intensity — and the total at 5.
- **The transition closes every open Matter without extra Backlog.** "Due 1 becomes Overdue, Due 2 closes" would
  need the Deputy to read the player's Matter stacks, which it cannot; the declaration is a clean sweep.
- **Executive File "Unanswered Complaint" guards at the Deputy's turn END**, not its start: Block is cleared at
  a combatant's turn start AFTER that turn's triggers have run, so a start-of-turn guard would wipe itself. The
  guard now stands through the player's turn, which is what it is for.
- **Phase-II intents rotate on a beat counter** (executive adjournment → disposition memorandum → close a file
  → decide without hearing → Everything Outstanding) instead of per-intent cooldowns, which the engine has no
  notion of. "Close an Unanswered File" keeps its own two-use limit through a counter.

## The Queue Commissioner (Act-I boss)

- **The queue advances at the START of the player's turn**, not at the end of the Commissioner's — the same
  beat, one step later, but on the side where the position can be READ. A joining marker skips the very first
  advance so the fight opens at Position 3 as designed.
- **"Move the player backward unless Priority prevents it" is resolved a beat later, too.** The intent marks
  the player as sent to the back; the player's turn-start program spends Priority or takes the step. From the
  boss's side neither the position nor Priority can be read at all.
- **The Administrative Choice is two cards** (Petition for Priority, Yield Your Place) dealt in at the turn's
  start and exhausted at its end; HOLD POSITION is simply playing neither. A latch enforces "one per turn".
- **The Phase-II Service Window choice is one card** (Ask for Expedited Service): taking it swaps the 25/30 %
  opening for 15 % and leaves the player at Position 1 instead of sending them back into the queue.
- **Phase-II intents rotate on a beat counter** rather than per-intent cooldowns, and "Last Number of the Day"
  is announced as a status on the player one full action before it lands, as the design requires.

## The Lord Sealkeeper (Act-I boss)

- **The Seal Ward rises at the player's turn start**, not the Keeper's: Block is cleared at a combatant's own
  turn start after its triggers run, and the Ward's whole purpose is to stand between the player and the boss.
- **Breaking a Seal is three cards.** "The player chooses which Seal breaks" becomes three one-turn offers laid
  into the hand the moment the break is earned; taking one shatters that Seal, and a latch keeps it to one per
  player turn. The break condition is read as "the blow landed with no Block left on the Keeper", which is what
  "remove all Block, then cause HP loss" amounts to in one damage event.
- **Every Fragment exists twice**: as a card in the player's hand (kept between turns — a boss-context action,
  not a card of the deck) and as an "outstanding" marker on the Keeper. The marker is how the boss's own
  programs know what is still unspent, since a boss program can never read the player's hand or statuses.
- **Fragment of Testimony scrubs 2 Paperwork.** "Up to 2 stacks of one negative status" needs the player to
  pick a status; Paperwork is the Keeper's own currency, so that is what the Fragment removes.
- **The Seal of Execution's +4 is a flat modifier** rather than "the first direct attack each boss turn": the
  Keeper attacks at most once per turn, so the two are the same without a per-turn latch.
- **Reclaim takes the first unspent Fragment in Seal order** and is announced one action ahead; with nothing
  left to reclaim the intent is not chosen at all, and the signature unlocks in its place.

## The Municipal Dragon (Act-I boss)

- **The hoard's Block goes up at the player's turn start**, for the same reason as the Seal Ward: Block is
  cleared at a combatant's own turn start, after its triggers have run.
- **"Order an Inspection" became "File an Objection"** (the Dragon's next attack deals 5 less). Revealing the
  intent after the previewed one is a UI capability, not something a combat program can do; the Authorization
  slot keeps a defensive read-ahead of comparable value.
- **The Authorization actions are cards dealt in while the player holds Authorization** and exhausted at the
  turn's end; each spends one Authorization and a latch keeps it to one action per turn. The Citation only
  appears once the Dragon is unlicensed and there is something to cite.
- **UNLICENSED is the Code Violation status itself** (+2 direct damage per stack), so the Phase-II base values
  are exactly the design's and the +8 cap falls out of the four-Violation ceiling the burning imposes.
- **The Inferno is announced a full player turn ahead** through a status on the player, as the design requires;
  its cooldown is the beat rotation.

## The Living Charter (Act-I boss)

- **All six Articles ship**, two of them on new ENGINE capabilities: Due Notice on postponed status
  applications (a waiting status is visible and cleansable but inert until the bearer's next turn), Full
  Disclosure on disclosure (a status widens what its bearer may see — the top of their own draw pile and one
  enemy action past the ordinary telegraph). Both are laws that bind both sides, so the Charter carries the
  Article and the player carries a mirror of it. Reciprocal Burden is implemented but currently outside the
  published rotation.
- **The published rotation is Continuance → Redress → Mutual Security → Due Notice → Full Disclosure.** A strike-down publishes
  the next Article in that order; the Emergency Amendment publishes one beside the standing one, so how many
  strikes it takes to reach a particular Article depends on the fight. The rotation index is kept on BOTH
  sides, because a card can only read the player's counters and a boss action only its own.
- **Article selection is fixed rather than drawn per fight** (Continuance → Redress → Mutual Security): a
  random per-encounter pick would have to happen in the run layer, which does not build encounters that way.
- **Continuance is banked and re-granted rather than "retained".** Block is cleared at a combatant's own turn
  start after that turn's triggers run, so each side banks half of its standing Block at its turn start and is
  given it back at the first moment a trigger can act after the clear: the Charter's half lands when the
  player's turn begins, the player's half right after their draw.
- **Judicial Review nominates the standing Article deterministically** (the one published first), and the
  answer is two one-turn cards; leaving them unplayed is the design's UPHOLD without its 6 Block.
- **Reciprocal Burden counts every cross-side status application**, since a program cannot ask whether the
  applied status was a negative one.
- **The Constitutional Crisis Exceptions are a card and an automatic spend**: the player's Exception becomes
  "Claim an Exception" (the Articles leave them alone for that turn), and the Charter's is spent automatically
  on the first Article effect that would hand the player an advantage against it.

## The Act-I map

- **The routes differ by design, not by luck.** The act's columns draw from three flavours — "the long queue"
  (fights), "errands" (events and shops), "the quiet corridor" (rests and treasure) — so which side of the map
  a path keeps to decides both what it holds and the order it holds it in. On top of that every path has
  CEILINGS (at most 3 rests, 3 shops, 3 treasures, 5 events, 2 elites, 2 duos), so no single route can be
  farmed for safety. Both are engine features added for this: `MapGenerationSpec.LaneProfiles` and
  `PerPathMaximums`.
- **The guarantees still hold underneath**: the per-path minimums (8 fights, the duo, the elite, 3 events, 2
  rests, 2 treasures, 2 shops) are met by the narrow funnel rows as before, and a ceiling may never be set
  below its minimum.
- **A standard fight pays a purse, not a number.** Gold is a per-role SPREAD rolled per fight (25–40 for an
  ordinary fight, 30–45 for the duo, 35–55 for a mimic, 45–70 for an elite, 90–120 for the boss) — the engine
  gained an optional range on its resource-change effect for this. The card offer is the same everywhere: three
  cards, pick one. Both the exact gold bands and the reward card pool are still open design questions.


## The final card pool's keyword substrate (2026-08-20)

The final design docs (`source-data/design/`) outrank both the ported v2 data and the older Act-I docs,
exactly as the FINAL_AUDIT enemy pools did. Where the keywords now differ from what this port shipped:

- **Paperwork tolls at the END of its bearer's turn**, as the design (and the original game) say. The port
  had moved it to the bearer's TURN START, because the damage-over-time automation ticks there and that was
  the only way to keep Doubt's attack penalty off it. It is now an authored tick of kind DamageOverTime that
  ignores Block, so the timing is right AND no Direct-restricted modifier can reshape it. Practical effect:
  an enemy acts once more before its Paperwork kills it, and Paperwork filed on the player during an enemy
  turn tolls at the end of the player's next turn rather than at its start.
- **Bookworm stays on the bearer's TURN START.** With Paperwork at the turn's end, start-and-end of the same
  turn is the cleanest reading of "immediately before the Paperwork resolves", and it needs no ordering
  agreement between two statuses firing on one event.
- **Doubt is spent once per ACTION, not once per hit.** The port spent a stack per damage event, so a
  three-hit attack ate three Doubt; the design says a multi-hit Attack consumes one. The stack is claimed on
  the action's first hit (engine: `claimOnceThisAction`), which reads the same from both sides — one enemy
  intent, or one card the player plays. A blocked attack still spends its Doubt, as the design says.
- **Ward Wax pays its Block after the draw, not at the turn start.** A combatant's Block is cleared at its
  own turn start once its triggers have run, so a guard granted there would be swept away. Consequence: Ward
  Wax pays nothing to a bearer that does not draw cards, which suits a status the design calls player-facing.
- **Censure's "a Status paid as a COST cannot be prevented" is not implemented**, deliberately: no card in
  either final pool pays a status as a cost yet, and the engine has no notion of one. The rule is
  future-proofing for later characters and gets built with the first card that needs it.
- **Citation is not built yet.** It needs "the action that just resolved dealt no direct damage", which the
  engine cannot yet answer; the action scope added for Doubt is the substrate it will use.
- **Seal converts to a Ratify event in the CARDS that apply it**, not in the status. A status cannot react to
  its own first application — the engine deliberately keeps a status' StatusApplied trigger from seeing
  itself — so "you now hold 3" would be invisible on the application that created the status. Every source of
  Seal therefore goes through the shared authoring helper.

### Act-I Bureaucrat card deviations

- **Certified Kindling** — "Archive a card from your hand. Gain 4 Block. If it was Junk, gain 4 additional
  Block." A card program cannot inspect the card a player is ABOUT to choose, so the Junk case is settled
  first: while the hand holds Junk the card takes the first of it and pays the bonus, and only with no Junk in
  hand does the player pick freely. The Junk is therefore taken rather than offered — which is the choice a
  player after the bonus would make anyway.
- **Seal converts at most twice per application.** "At 3 Seal, spend 3 and Ratify" is written as two nested
  questions rather than a loop, because the engine's repeat-until runs its body once before it ever asks (it
  would Ratify a single Seal). Two is the ceiling anything in the game can reach in one application: the
  largest grant is 3, on top of at most 2 already standing.
- **A card's clauses run causally.** Card text is read top to bottom and later clauses routinely ask about
  what earlier ones did, so every card's steps wait for the previous step to have HAPPENED. A plain engine
  sequence starts all its steps at once, which would make "if this Ratifies the target" read stale state.

### Engine questions the card pool raised — all now answered

- **An enemy's upcoming intent** is readable from a combat program: the driver that owns the intent rules
  installs a projection on the combat state, so "if the target intends to Attack" is an ordinary condition.
  A script-driven scenario, where the enemy's action is dictated rather than chosen, installs none and every
  such question answers "no".
- **Citation** works: an action now announces, when it closes, whether it struck the other side. Damaging is
  the design's wording taken literally — at least one ordinary hit landed on an opposing combatant, and Block
  soaking it changes nothing. A status ticking is not an action, so the action that applied that status is
  never blamed for it.
- **Choose one of N** is a program node: named options, the player picks, they resolve in pick order and an
  option cannot be taken twice. It parks the fight the way an in-combat card choice does; Studio and the
  Godot frontend both render it, and headless play takes the first options so such a card always resolves.

### Act-I Bureaucrat uncommons and rares

- **Cinder Warrant** and **Certified Kindling** take the Junk rather than offering it. "You may Archive a Junk
  card; if you do, repeat this attack" is taken automatically whenever there is Junk in hand, because
  Archiving Junk and striking twice is never the worse choice.
- **Counter Ward**'s rider is "your next card this turn costs 1 less", not "the next card you QUEUE". A cost
  modifier can be narrowed to a card TAG, so this could be tightened later by tagging Queue cards; as written
  the player simply spends the discount on what they meant to.
- **Dubious Authority** answers Doubt leaving an enemy that has already dealt damage this turn, which is how
  "consumed after an enemy attacks" is told apart from a card that merely removes Doubt (Formal Dissent).
- **Licensed Disposal** Archives the first Junk in HAND after the draw, not strictly the first Junk DRAWN —
  a distinction only visible when Junk was already being held.
- **Privy Seal**'s "Requires at least 1 Seal" is a condition, not a play restriction: the engine has no
  data-authorable requirement, so the card is playable but does nothing without a Seal.
- **Skeleton Staff** is a Working the player uses at will rather than a lasting end-of-turn option, because
  "at the end of your turn you MAY…" needs a prompt the engine raises on its own behalf. Queueing an
  arbitrary card is a real effect now (`queueCard`), which is what the card is for.
- **Blank Warrant**'s "no Paperwork, Doubt, or Seal" checks Paperwork only. A condition compares one value at
  a time, and Paperwork is the one the Bureaucrat almost always has on a target it has touched.
- **Violence Allowance** is two statuses: the Rite that keeps the books and the allowance that carries the
  discount, because a passive modifier cannot be conditional — its presence is the condition. The discount is
  narrowed to Deeds by card tag, so nothing else is cheapened while it waits.

### Act-I general cards

- **False Signature** cheapens your NEXT card rather than a card you choose in hand. The engine prices a card
  by what its owner is wearing, not by a mark on one card, so "choose a card in your hand; it costs 1 less"
  has nowhere to live yet. The bargain is otherwise intact: the discount is spent by the next card, and that
  card hands the surcharge to the one after it, for the rest of the combat. **Per-instance card cost marks
  are the engine feature this wants**, and a long list of relics wants it too — see the relic phase.
- **Sanctioned Charm** hands back ONE Censure. The refusal has already been paid for by the time anything can
  answer it, and the event does not say how many stacks it cost.
- **Silent Hearing** pays its Block after the player's next DRAW, not at the moment the enemy strikes: Block
  granted during an enemy turn is swept away at the player's own turn start. Same reason Ward Wax pays after
  the draw.
- **Notary Beetle** names the negative statuses it can seed (Paperwork, Doubt, Seal, Lien, Citation, Blood
  Ink), because no effect can apply "whatever status the event named". A new negative status has to be added
  to that list. "Does not already have that Status" needs no state at all: a status arriving where there was
  none raises StatusApplied, and one landing on itself raises StatusMerged, so watching only the former IS
  the condition.
- **Usurer's Moon** is answered inside the Lien resolution, because only the resolution knows how much Block
  it took. Same shape as Red Ink Doctrine inside the Paperwork tick.
- **Anything that asks about Block** — Tallow Reserve's requirement, Forfeit Seal's "still has Block",
  Foreclosure's claim — goes through a scratch COUNTER first: a condition compares a value read off a
  combatant, and Block is not one of the values it can read.

### Act-II cards

- **Cross-Filing** moves its Paperwork rather than offering to: the card cannot ask the player to point at a
  second enemy. Written as a spread over every enemy plus a double subtraction on the original target, which
  nets to exactly "M off the target, M onto each other enemy", and skipped outright when the target is alone.
- **Smudged Index** Archives a chosen card from the draw pile. "Look at the top 4 and put the others back in
  any order" is a prompt the engine does not raise; the Archiving is the part that matters.
- **Dead Letter Office** counts Junk types in the EXHAUST pile, where Archived cards go. Junk that exhausted
  itself counts too, so it can over-count slightly.
- **Ghost Register** lets the player choose which Archived card comes back, because a rule that hears "the
  archive count went up" cannot point at the card that caused it. The copy arrives with one free play rather
  than costing 0, for want of per-instance card costs.
- **Moonlit Counterfeit** asks twice — once for the card to copy, once for the original to Exhaust — and its
  upgrade simply spares the original. The copy is free through one free play.
- **Sanguine Errata** removes a stack the engine picks by rule (a polarity-filtered selection) rather than by
  prompt, so Blood Ink itself can be the one chosen.
- **"Different statuses" is counted by naming them.** Stacks are countable; distinct statuses are not, so
  CardAuthoring keeps the list of negative and positive statuses the game files and asks each whether it is
  present. A new status of either kind has to be added there.
- **Standing Citation** is answered inside the Citation trigger, because only that trigger knows it is about
  to spend a stack — the same shape as Red Ink Doctrine inside the Paperwork tick.

### Acts III and IV

- **Priority Docket / Customary Due** charge for the queueing themselves, because the queueCard effect pays
  nothing. Customary Due copies from the discard pile — "a card that resolved during your previous turn" is a
  memory the engine does not keep, and the discard pile is where such a card is.
- **Hearth Compact** retains the Doubt when the HIT that spent it got nothing through, rather than when the
  whole attack did. A trigger fires per hit and the action is not over yet.
- **Hedge Covenant** works the prevented damage back from what landed: the doubted hit is three quarters of
  what was aimed, so the quarter prevented is a third of what landed, rounded up. The engine reports what
  landed, not what was averted.
- **Guest Right and Wax Indemnity** take the hit and give it back. Nothing can soften damage that is already
  landing, so the player ends the exchange where the card says they should, at the cost of the mitigation
  reading as healing.
- **Monumental Writ** counts what is still queued behind it when it resolves. The Queue resolves oldest
  first, so that is the same set as "cards queued after it" — but only for cards queued in the same turn.
- **Last Office** counts the statuses the enemy does NOT carry rather than the ones that have run out on
  anybody this combat; the engine keeps no such history.
- **Compound Indictment** tops up every negative status the target carries, up to five kinds, rather than
  letting the player pick which five.
- **Temple Tally** credits each enemy for the fives it has already crossed, so a pile that shrinks and grows
  again crosses nothing twice.
- **A Rite that changes what a KEYWORD does is a marker the keyword looks for.** Hieratic Measure lives in
  the Ratify conversion, Candle Cathedral and Wax Reliquary in Ward Wax, Debt Ouroboros and Usurer's Moon in
  the Lien resolution, Standing Citation in Citation, Red Ink Doctrine in the Paperwork tick, Hearth Compact
  and Hedge Covenant in Doubt. Only the rule that owns the moment can answer a question about it.

## The final relic pool (2026-08-20)

A relic that changes a fight is a hidden status handed to the player when the fight opens (`Openings.EveryCombat`
applies it), so a relic's rule is written in exactly the language a Rite is written in. A relic that changes the
RUN — gold after a victory, HP on pickup — is a run program instead. The deviations below are the places where a
relic's line asks for something the engine cannot see.

### The 50 Normal relics

- **Brass Bookmark** retains the whole hand rather than one chosen card. Retention is a property of a card, and
  choosing which card to keep at the moment a turn ends is not a prompt the engine raises.
- **Redaction Knife** discards the oldest card in hand instead of a chosen one, for the same reason — the cost
  has to be paid without stopping to ask.
- **Binder's Awl** pays on the next draw after the draw pile runs dry, not at the shuffle itself; a reshuffle
  is not an event a rule can hear.
- **Hollow Wax Bead** and **Lead Counterweight** cannot read what a card cost, so they pay a flat amount rather
  than scaling with the card that triggered them.
- **Concordance Medallion** and **Chancery Ribbon** spread a flat 1 to the other enemies instead of matching
  what was just filed: a rule sees THAT a status landed, not how many stacks it carried onto that target.
- **Rootbound Staff** fires every combat rather than once per rest cycle — a relic's rule is installed fresh at
  every opening bell and keeps no memory between fights.
- **Blood-Price Token** takes its 3 HP rather than offering the trade; a rule cannot raise a yes/no prompt
  outside a card's own choose-one.
- **Refusal Rosary** pays after every victory instead of on the refusal itself, because a refused enforcement
  is not a run-layer event.
- **The once-a-turn latch is read and written on the WEARER**, not on the event's source: who "source" names
  differs between a card being played, a status landing and a turn starting, but the relic's wearer is the
  same combatant in all three, and the promise is theirs.

## The 24 Shop relics (2026-08-21)

Most of the Shop pool is economy, and the engine grew the seams for it (node tags, combat→run tallies, shop
price rules, shelves, credit and debt, reward rules). What could not be translated straight:

- ~~**"The first time each Act" is a run FLAG that is never cleared.**~~ **Resolved 2026-08-21**: the run
  layer learned what an act is, so these are ACT flags now (`RunExpr.ActFlag` / `SetActFlagRunEffect`) and the
  act boundary forgets them. No longer an adaptation.
- **Wastebroker's Permit counts every Archive, not only Junk.** The Archive mark records THAT a card was
  archived, not which one; asking what was archived would mean marking every card as it goes. Still capped at
  3 per combat, so the payout is unchanged in practice for a Junk-heavy deck.
- **Secondhand Reliquary's blood price is charged on buying a Normal Relic, not specifically the marked one.**
  A price rule bends a price; it cannot leave a note on the item saying which one it bent. Both halves key off
  the same once-per-Act flag, so the relic can still only ever cost 5 HP once and only alongside its discount.
- **Notary's Waiver banks its Waivers when the fight ends**, not the instant you Ratify. The count is kept in
  the fight and collected on victory, like every other combat→run tally; losing the fight loses the Waivers.
- ~~**Witchmarket Purse pays out on entering the Act's boss node.**~~ **Resolved 2026-08-21**: it waits for
  `actCompleted`, which is a real moment now.
- **Guest-Favor Token's second option upgrades a card you own** instead of offering a special two-card reward:
  a relic definition is static and has no handle on the Act's card pool. "Without entering combat" is also not
  something the run can observe, so any resolved Event counts.
- **Merchant Punchcard's Punches are spent automatically** as far as they help, rather than the player
  choosing how many to redeem before the first purchase. Shop credit is spent in whole units and never
  overpays, so nothing is ever wasted — but a player cannot deliberately save a Punch for a dearer item.
- **Warranty Tag pays out at the next Shop instead of letting you hand the relic back.** Returning a specific
  relic is not something a rule can name (the relic it would return is not known when the rule is written), so
  the warranty settles as a refund of half the Gold actually paid, and does not expire.
- **Filing-Fee Stamp reads an enemy's Paperwork as it goes down**, because a moment later the corpse's
  statuses are gone with it. The 20-Gold-per-combat cap is applied when the run collects the tally rather than
  while it accrues — capping the running total would silently lose the last enemy's share.

The city shop was relabelled to make any of this possible: its stock is now two **shelves** (`cards`,
`relics`) whose pools are deeper than what they show, and every entry says what it is (kind + tags). Without
that labelling a price rule matches nothing and the relics would quietly do nothing.

## The Act-I Event relics (2026-08-21)

Six of the 25 Event-exclusive relics belong to Act I; the other nineteen are named by events in Acts II–IV,
which do not exist yet. What could not be translated straight:

- **Originality Stamp does not check for a same-name copy.** "The first played non-Junk card that has another
  persistent same-name copy" would mean asking how many copies of the played card's DEFINITION are still in
  the piles; nothing can ask that. It copies the first non-Junk card you play instead. The copy's own −1 cost
  also had to move: creating a card hands back no handle on the instance that was made, so the discount rides
  on the wearer as "your next card costs 1 less" — the shape the card pools already use.
- **Unclaimed Property Tag marks the top of the draw pile.** The pile is already shuffled, so its first card
  IS the random one. The mark rides on the instance for the whole fight rather than "the first time it enters
  hand, that turn" — the engine prices an instance, it does not price a turn.
- **Uncalled Ticket takes whatever is still in hand,** not specifically an unaffordable card: a rule cannot ask
  whether a particular card was affordable when the turn ended.
- **Inherited Bone Folder does not upgrade the card it marks.** Nothing upgrades a card mid-fight — the card
  pools have no such node either. It keeps the cheaper half and gives the extra card unconditionally, the same
  way Rootbound Walking Staff dropped its condition.
- **Threshold Ward answers any positive status on the other side,** which is what the design says; the rule
  has to check that whoever gained it is not the wearer, since it watches the whole fight.
- **Crossed-Out Map became a real engine feature** rather than an adaptation: a step that ignores the paths
  (`GrantUnrestrictedStepRunEffect`). "The next row" is read back out of the edges as distance from the start,
  because these maps are layered but nothing records the rows. The charge is spent only when the shortcut is
  actually taken, so walking an ordinary fork keeps it.

## Act II — The Endless Archives (2026-08-21)

Numbers come from the master's **MASTER COMBAT BALANCE APPENDIX**, which gives ranges rather than exact
values. Convention: **the midpoint of each range, rounded half up to a whole number**; duo HP from the
appendix's scaling table (ordinary body 68–78 % → 74 %). Where a number and a signature rule disagree the
appendix itself says the signature wins.

- **A source collects its Overdue at its OWN turn start**, not the instant the second Overdue lands. The
  design collects immediately; collecting on the turn of the one owed is what makes "this source's stacks"
  provable — the bearer is the acting source, so what it spends is demonstrably its own and never a
  neighbour's. In play the difference is one turn of delay.
- **Overdue is applied one stack at a time as its own instance**, so "2 from the same source" means two
  filings. A merged stack remembers only the last source, which would let one enemy spend another's debt.
- **A misfiled card is taken back a beat after it arrives.** The engine hands the hand over before anything
  can object (`CardsDrawn` fires with the cards already held), so the archive reclaims the card immediately
  afterwards and fetches a replacement. Invisible in the numbers, visible in a combat log — the same
  beat-late shape Act I's Exception Imp uses. There is no window in which the player can act on it.
- **Which shelf misfiled a card is written into the MARK**, not looked up from the marker's source: a program
  cannot ask who put a mark there, and the destination has to be answerable at the moment the card is taken
  back. Hence two marks — the plain one goes to discard, the Crabwise Shelf's goes back into the draw pile.
- **Volume Q-Null does not propagate its misfiling.** "If the replacement has the same persistent Base Cost it
  also becomes Misfiled" needs to compare two card instances' base costs, which nothing can read. Q-Null is
  for now a plain misfiler; the propagation is deferred rather than approximated.
- **"Misfile 1 card" marks the top of the draw pile.** The pile is already shuffled, so its first card is the
  random one — Act I's Unclaimed Property Tag reads randomness the same way.
- **A Reference's whole rule lives on the CITING ENEMY**, not on the player: the Overdue an unanswered
  citation costs has to come FROM that enemy, and a rule running on the player would file it from the player.
  The enemy checks at its own turn start, which is also the moment that knows the answer — the hand has just
  been put down, so anything still marked was not played.
- **Second-Person Entry does not chain its citations by card type.** The design remembers the type used to
  fulfil a citation and cites that type next; here every draw is cited afresh. The chain needs a remembered
  card TYPE surviving across turns, which is a counter per type and a follow-up-only flag — deferred rather
  than approximated.
- **What the Fanged Alphabet learns is kept on the PLAYER.** Both moments it cares about are the player's (a
  card played, a hand drawn), and in a fight-wide trigger the player is the one structural single target
  available: a "whoever wears the rule" selector can match several combatants and so cannot be read as one
  counter at all.
- **The Unclaimed Reading Table clears the oldest card in hand, Junk included.** "The oldest remaining valid
  non-Junk card" would need a zone iteration that REFUSES a tag; one can require a tag but not exclude one.
- **The Choir's crescendo is folded into Voice itself** (+4 per Voice on its next direct attack, spent by it)
  rather than converting two Voices into a separate +8 status. Two Voices are the design's +8, and one status
  fewer to keep in step.
- **★ Every rule in Act II that reaches into the hand puts a no-op in front of itself** (`ActTwo.Guarded`).
  The played card is still in the hand at the very first instant of a CardPlayed trigger and gone a beat
  later, so a rule that looks immediately takes the card the player just spent — invisibly, because that card
  was on its way to the discard pile anyway. Not an engine defect (an earlier note here claimed one; it was
  wrong). Demonstrated in `RogueDeck.Sandbox.Tests/CardPlayedTriggerHandTimingTests`.
- **The Object's Recognized Category is the literal first card of the turn**, Junk not skipped: the engine
  records one opening type per turn, exactly as Act I reads it for the Wrong-Window Scribe.

## Act II — what is built and what is not (2026-08-21)

**Built and proved in live fights:** the four universal mechanics (Overdue, Misfiled, Referenced, Redacted)
and the signatures of stages 1–4 — Brass Maw's Return Parcel, the Object's Recognized Category, the
Ouroboros's re-owing, the two misfiling shelves, the Corridor's Wrong Edition, the three Reference rules, and
the Reading Room's three hand rules.

**All 25 identities and all 35 encounter templates are authored**, with HP and intents from the balance
appendix and the act's vocabulary in the intents themselves (attacks that Overdue, Misfile or Redact). What is
missing is the *signature* of thirteen of them, listed here so nothing is quietly assumed to work:

- **Volume Q-Null** — Misfiled propagation by matching base cost. Now expressible (the base-cost expression
  exists); simply not yet written.
- **Second-Person Entry** — chaining its citations by the card type used to fulfil the last one.
- **Palimpsest Husk / Vacant Portrait** — "a played Redacted card becomes Misfiled" and "playing a Redacted
  card opens the frame". Written three times (guarded, immediate, on either side of the beat) and never shown
  to work: a mark put on the card that was just played does not take. Not shipped rather than shipped
  unproven. Worth its own engine test before the next attempt — the question is narrow, "can a CardPlayed
  trigger mark the card the event is about", and it is not the hand-timing issue.
- **Expunged Name** — redacting a card whose name was already played earlier in the combat. Nothing reads
  per-definition play history beyond the current turn.
- **Fatal Comma** — Clause A / Clause B ordering. Needs two marks and an order comparison between two plays.
- **Errata Doppelgänger** — moving a redaction from the played card to another in hand.
- **Checkout Codex** — Behind-the-Desk, with its three player options (wait, demand, end the turn).
- **Mnemonic Chain** — remembering one concrete card INSTANCE across zones and turns.
- **Unoccurred Tuesday** — a skipped enemy turn with +25% damage taken during it.
- **Hourglass With Two Bottoms** — two independent scheduled countdowns the player can each delay once.
- **Blank Death Certificate** — returning at ~35 % HP unless a Reference was fulfilled that turn.
- **Spare-Life Jar** — storing a dead ally's identity and reviving it after a countdown.
- **Detached Footnote / Miscellany Index** — the Source link and the four-way Residue synthesis.

Several of these need engine seams that do not exist yet (an enemy skipping its turn, reviving a dead
combatant, per-combat play history). They are deliberately absent, not approximated.
