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

### Act I's authored fifteen
Act I's events are no longer converted at all — they are authored from the post-audit master
(`Converter/Events/ActOneEvents.cs`), and the ported v2 events that wore the same names are gone from the
loader. What the master asks for and the port does differently:

- **The Licensed Vendor's "open the regular vendor"** is a counter built INSIDE the event — five cards, six
  relic offers and one removal, each buyable once, at the city shop's prices. An event cannot open a shop
  NODE, and the node is where the engine keeps a shop's live state, so this vendor has no reroll and its
  shelf is authored once rather than dealt fresh each run. (The act's two shop nodes per path still are.)
- **"A temporary 0-cost Exhaust copy of the card you chose"** (The Almost-Helpful Clerk) becomes the card
  itself, in the opening hand, free for that first play — a new marking, *Stamped*. A card Exhausts because
  its DEFINITION says so and there is no per-instance Exhaust mark, so a copy of an arbitrary card cannot be
  made temporary. Same tempo, one card fewer.
- **Certified Original** keeps its "cost −1 for that play" and drops its "and gain Exhaust for that play",
  for the same reason.
- **Expedited Route's "30% less Max HP"** is paid as unblockable damage at the opening bell — 30% of each
  enemy's own maximum, read per body. Maximum health cannot be lowered from outside a fight.
- **"Combat grants no Gold"** (Garnished Reward, Expedited Route) is a garnishment in two beats: the fight
  ending arms a bailiff, and the bailiff cancels the very next Gold that arrives, which is that fight's
  purse. The purse is paid out by the MAP, after the resolved event every rule hears, so it cannot be
  withheld — only taken back.
- **"125 Gold if won by end of round 3, otherwise 25"** (Receipt of Prior Effort): the fight writes down the
  round it is on, and the run reads that off the result. There is no if-expression for a number, so the rate
  is arithmetic — `125 − 100·min(1, max(0, rounds−3))`.
- **Transform / duplicate / upgrade / remove let the PLAYER choose** the card, where the ported events rolled
  it at random. "A different card is Misfiled" needs no exclusion rule: the refiled card is gone by then.
- A promise an event makes for after the next fight is an authored RUN PROGRAM installed by name
  (`Converter/Events/ActOneEventPrograms.cs`, `RunBlueprint.Programs`, `fx.installProgramById`), which is
  what makes it survive both the export and a save.

### The ported events (Act II's, until B-2)
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
and the signatures of stages 1–7 (bar the Checkout Codex) — Brass Maw's Return Parcel, the Object's Recognized Category, the
Ouroboros's re-owing, the two misfiling shelves, the Corridor's Wrong Edition, the three Reference rules, and
the Reading Room's three hand rules.

**Stage 5's two signatures are built too** — the Husk files away what it wrote over, and the Portrait's frame
opens by 8 (as damage when there is no guard left to strip, which is the design's second half).

**All 25 identities and all 35 encounter templates are authored**, with HP and intents from the balance
appendix and the act's vocabulary in the intents themselves (attacks that Overdue, Misfile or Redact). What is
missing is the *signature* of thirteen of them, listed here so nothing is quietly assumed to work:

- **Fatal Comma's wrong-order penalty** — "B before A: Clause A becomes Redacted immediately before
  resolution". Nothing can step between a play and its resolution, so reading the clauses backwards is simply
  a missed reward; the correct order still cuts the Comma for 8, and leaving both unread still owes it.
- **Errata Doppelgänger lifts the redaction AFTER the card lands**, not before. The design has the card
  resolve at full strength; here it lands halved and only the MARK moves on to another card in hand — again
  because nothing intervenes between a play and its resolution.
- **Volume Q-Null** — Misfiled propagation by matching base cost. Now expressible (the base-cost expression
  exists); simply not yet written.
- **Second-Person Entry** — chaining its citations by the card type used to fulfil the last one.
- **Expunged Name** — redacting a card whose name was already played earlier in the combat. Nothing reads
  per-definition play history beyond the current turn.
- **Checkout Codex** — Behind-the-Desk, with its three player options (wait, demand, end the turn).
- **Unoccurred Tuesday's missing turn is an INTENT, not a skipped turn.** Nothing lets an enemy skip its own
  turn — Stun only stops the player playing cards — so the missing day is an intent that does nothing but
  leave Tuesday exposed (+25 % direct card damage for the round). At the table that is the same thing, and its
  place third in the cycle is what makes it every third turn.
- **Hourglass With Two Bottoms** — two independent scheduled countdowns the player can each delay once. Its
  two futures ship as its two attacks; the delaying is not built.
- **Blank Death Certificate** — returning at ~35 % HP. **The open question is ANSWERED (2026-08-21), and the
  answer was not "the trigger does not fire".** A bearer-scoped Downed trigger DOES fire for the bearer's own
  downing — the Volumes' survivor rule is built on exactly that and is proved by a test. What made the revive
  look dead is that in a Downed program `Source` is the DOWNED COMBATANT and living-only selectors resolve to
  nothing against it (`CombatantDownedTriggeredEffectTargetResolver` sets Source to the downed unit; the
  engine ships `SourceIncludingDownedCombatantTargetSelector` for precisely this). Rewriting the revive with
  that selector is the thing to try.
  Separately and definitely: **the LAST enemy can never be revived.** The combat's outcome is decided by
  `UpdateStandardCombatResultOnLifecycleChangedHandler`, which enqueues Victory the moment no enemy is living
  and never re-checks when that request resolves.
- **Spare-Life Jar** — storing a dead ally's identity and reviving it after a countdown. Blocked by the same
  Downed question. **NOTE (2026-08-22):** the Obituary shows the shape that DOES work — the engine's data-driven
  `StatusDeathPreventionData` (a one-shot pre-down interceptor) stands BEFORE the down instead of undoing it,
  which is the only place to stand: a downed combatant refuses healing and status application.
- **Detached Footnote** — the Source link, and gaining Notes when the Source's own signature triggers. Nothing
  announces "another enemy's rule reached its moment", so this is not approximated.
- **Miscellany Index counts one Residue source, not four.** Three of the design's four are moments only
  another rule knows it reached — a Delinquency resolving, a Reference being fulfilled, a Misfiled card
  actually being skipped — and none announces itself in a way the Index could watch. At 4 it still files
  everything else: one card in hand Redacted, another Misfiled.

## Act II — elites and bosses (2026-08-21)

**All nine elites** are authored with the HP the elite master fixes for each (118–160) and their named intents
at the doc's numbers. **Four of the five bosses** ship as single bodies at their stated HP (258 / 270 / 278 /
288) with their named intents.

What is deliberately NOT built, so nothing is quietly assumed:

- **Every elite's SIGNATURE.** The bodies fight; the mechanics that make each one a puzzle — the Bell's Return
  Receipts and Late Fee, the Colossus's Compression, the Catalogue's Entered Names, the Silence's Echo, the
  Oracle's Black Ink, the linked Volumes, the Drawer's depth, the Clock's Past/Future, the Obituary's three
  endings — are not. These are per-elite systems on the scale of a standard's whole stage.
- **Every boss's PHASE STRUCTURE.** The Act-II bosses are structural, not statline: the Catalogue adapts to
  the tempo you set, the Warden seals instruments, the Curator schedules collapses, the Auditor reconciles
  accounts. Shipped as their intent cycles, they fight but do not yet think.
- **The Grand Cross-Reference is absent entirely.** ~~It is three volumes (68 / 72 / 76 HP) in a first phase
  and a central 96-HP body in a second.~~ **SUPERSEDED 2026-08-22: it is built.** See the section below.

## Act II elite signatures — After-Hours Return Bell (2026-08-21)

The first elite signature is built in full: Proof of Return, the Late Fee ledger, the Return Receipt card and
the Toll. Three readings had to be made explicit.

- **The Bell issues its receipts from its own two Overdue intents**, not from a trigger watching status
  applications. "Whenever the Bell ITSELF creates 1 Overdue" names exactly two places, and writing the receipt
  where the Overdue is written makes the pairing provable rather than inferred.
- **The three-receipt ceiling is read off the cards**, summing hand + draw pile + discard — the three zones a
  live Receipt can be in. A played Receipt exhausts, so leaving the fight frees its slot without a counter to
  maintain. The debt itself is uncapped: the Bell keeps filing Overdue after the printer stops.
- **CONTEST THE FEE is offered even with no Late Fee to contest**, where the design calls it "unavailable".
  An option list has no per-option availability; the option resolves to nothing, which is what a card played
  into an empty board does everywhere else. Proved by a test, so it stays honest rather than silent.
- **The Bell loses 5 HP through its own Block.** "Direct HP Loss, not a Damage event" is written as a health
  set (current − 5), which no Block, damage modifier or damage-taken reaction can see. The test files a
  Receipt while the Bell holds 18 Block and checks both the HP drop and the untouched Block.
- **Cooldown 3 is an eight-entry cycle.** The engine rotates an intent list by round, so "at most every fourth
  intent" is the Toll at slots 4 and 8 of eight. Reopen for One Final Minute appears twice to fill the cycle;
  every other intent keeps its single slot.

## Act II elite signatures — Rolling Stacks Colossus (2026-08-21)

Compression, Open Aisle, the Ladder's tax and Shelf Collapse are built. Compression is counted where the act
already knows a misfiling was ACTUALLY skipped (`ActTwo.TakeBack`), and the hook does nothing when no Colossus
is on the field — the act's rule stays the act's rule.

- **"At Compression 3 the next eligible normal intent becomes Shelf Collapse" is written into every normal
  intent**, not into the intent order: the engine rotates a fixed list, so each intent asks first whether the
  aisles have closed, and whichever comes up IS the collapse. What cannot follow is the telegraph — an intent
  label is fixed at authoring time, so the player sees the ordinary intent's name on the turn it becomes a
  collapse. The Compression counter is visible throughout, which is the warning the design trades on.
- **The collapse re-seeds itself.** It clears Compression and misfiles two cards; those two come up on the
  very next draw and close the aisles by two again. Faithful to the loop as written, and proved by a test so
  nobody rediscovers it as a bug.
- **"Status/Junk replacement cards do not receive Open Aisle" is not filtered.** The mark is a path, and the
  rule that spends it only pays out when the card is PLAYED — which is the one thing an unplayable card
  cannot do.
- **"The last valid card instance played" is a mark the player's own rule re-points at with every play**, so
  Roll Across the Aisle can name it a turn later. It searches the discard pile and the hand — a card that
  exhausted itself is exactly the one the design says "can no longer legally be tracked" — and a turn where
  nothing was played gives it nothing to mark, rather than a random card.
- **A drawn card can now be named by the draw that produced it** (engine: `cardInstance.drawnOutcome`, the
  counterpart of `cardInstance.createdOutcome`). Open Aisle needs the identity of one specific replacement
  card, and the hand it lands in is ordered but not indexable from the end.

## Act II elite signatures — Catalogue of Unwise Names (2026-08-21)

Enter a Name, Recognized, the three Citations and the signature are built. The ledger lives as counters on the
PLAYER and marks on the player's cards, because both sides read it — the Catalogue's intents cash entries and
the player's own plays turn a Recognized entry into an Established one. A counter on the Catalogue could not
be read as a single number by a player-side rule, since "the enemies carrying this status" is a set.

- **Naming is two prompts**, an option ("enter a name" / "decline") and then a card. Both decisions the design
  names survive: declining while an eligible card exists pays the Catalogue 8 Block, and a hand with no
  eligible card raises no prompt and owes nothing. The chosen card is marked ONCE and every later step reads
  the mark — a chooser expression evaluated twice would ask the player twice.
- **The Citation type rotates** (Cost → Form → Record) rather than being picked freely. It is shown on the
  card at the moment of naming, which is what 7.4 actually trades on: the liability is known before the
  benefit is taken.
- **Citation of Form reads B&B's own card types.** The design's Attack / Skill / Power are the generic
  engine's; this game has Deed and Working. A Deed cites as an attack (+5 on the Catalogue's next direct
  attack), a Working as a skill (14 Block), anything else takes the design's own neutral fallback (8 Block).
- **Citation of Record reuses the act's Reference machinery**, with a `cite` override so it cites the tracked
  card rather than whatever is first in hand.
- **"The oldest eligible Established Entry" is first-in-pile-order**, searched discard → draw → hand. The
  engine keeps no per-entry timestamp, and an Established card is by definition one that has been played.
- **Nothing restores the discount on play.** The engine already spends a per-copy price at the play itself,
  which is exactly "after full resolution, Recognized is removed" — a refund of our own would have left the
  card dearer than printed.
- **Strike from Catalogue (7.5) is NOT built.** It turns on "a card identity not currently represented by an
  Entry", and nothing compares one card instance's identity against a set of tracked ones.

## Act II elite signatures — Silence Between Two Words (2026-08-21)

The Unspoken Pair, the spoken/unspoken distinction, all three turn-end outcomes, Echo and the Unspoken
Verdict are built.

- **The resolution runs at the PLAYER's turn end**, where the design puts it — and it is the only moment that
  works. The Silence's Block is wiped when its own turn begins, so a resolution one beat later (the act's
  usual "collect at your own turn start" shape) could never take "up to 10 current Block" off anything.
- **The Verdict deals a flat 6.** Echo is a passive damage modifier (+4 per stack on direct damage) that the
  next attack spends, so the signature's "6 + 4 per Echo" is written as 6 and let the modifier add the rest —
  at Echo 4 that is exactly 22, counted once rather than twice.
- **Echo is capped, not refused.** At Echo 3 a two-Echo turn takes the one that fits.
- **"The same instance cannot be selected two turns running" is a three-pass sieve**: mark every non-Junk card
  eligible, strike the eligibility of last turn's Words, and put them back only if fewer than two survive.
  That last pass is the design's "if at least three alternatives exist", read from the other side — when the
  hand is too thin to avoid a repeat, the repeat is allowed.
- **Junk is excluded by marking and then striking**, because a loop can select BY a tag but not around one.
- **Which two cards become the Words is the SILENCE's choice, not the player's** (the design says "select",
  where the Catalogue says "the player may select"), so it is deterministic: the first two eligible cards.
- **Engine seam bought:** counting a zone by a per-instance MARK (`CombatantZoneCardCountExpression.Mark`).
  The sieve has to ask how many cards are still eligible, and the count could previously only ask what KIND
  of card each was.

## Act II elite signatures — Black-Ink Oracle (2026-08-21)

Black Ink, the riddle rhythm, all three responses and the signature are built.

- **The hiding is presentation; the engine poses and GRADES.** 9.4 is what makes the riddle buildable: the
  queried field must be deterministic and part of the card definition, so the question asks about the printed
  cost and the answer is checked against the card itself. A frontend is what can actually black the field
  out — and nothing in the exchange depends on it being invisible, which 9.8 explicitly allows ("the
  encounter never requires memorizing the deck to remain playable").
- **The three responses are five options**: three ANSWER claims about the cost (0 / 1 / 2-or-more), plus
  REVEAL and DECLINE. An option list is the only prompt shape there is, and a claim the engine can grade is
  the only kind of answer it can score.
- **REVEAL is always selectable.** With an Energy it costs the Energy; without one it costs an Overdue owed
  to the Oracle — the design is explicit that it must never be a safe option that cannot be chosen.
- **Being wrong and declining cost the same** (an ink and a Redaction), which is what keeps DECLINE a
  decision rather than a free out.
- **Black Ink is NOT a damage modifier** (unlike the Silence's Echo), so Devour the Unstated Answer computes
  14 + 4 × ink itself and then clears the ink.
- **9.7 low-HP evolution is not built.** "The Oracle may visually redact two fields while still asking one
  question" is entirely a presentation change; the engine's riddle is unaffected either way.

## Act II elite signatures — Volumes of Cause and Consequence (2026-08-22)

**The Volumes now ship as TWO bodies** (76 + 84 HP) rather than one 160-HP block, because the Concordance is
a line drawn between two things and cannot exist inside one. The encounter fields both, and the Act-II pool
test pins that exactly one elite is a pair.

- **The Concordance is written into the reference itself.** `ActTwo.Reference` gained `onFulfilled` /
  `onFailed` hooks, so fulfilling a Causes citation wounds Causes (9 HP through its own Block, as a health
  set) and Supports Consequences, while failing it does the ordinary Act-II thing AND Unsupports it.
- **Supported / Unsupported are passive ±6 modifiers on direct damage**, spent by the attack that carries
  them. Enforce the Result is therefore a flat 17 and comes out at 11 / 17 / 23 without stating the number
  three times.
- **Causes ANNOUNCES a citation in its intent; the citation lands after the player's next draw.** A card
  cited during the enemy's turn is a card about to be discarded — the design means a citation you can answer,
  which is why every other Act-II citation rides on CardsDrawn. Insert a False Premise announces the same
  citation plus the redaction.
- **The survivor rule is written from the DOWNED volume's side.** In a Downed program `Source` is the downed
  combatant, not the status's bearer, so the survivor is "the allies of the one that just fell" — and because
  ally selectors resolve living combatants only, a simultaneous death finds no survivor and hands out
  nothing, exactly as 10.8 requires. Bearer scope (not Anywhere) is what makes it run once for one death.
- **10.7 and Return to the Premise are NOT built.** Both manipulate the intent PREVIEW — advancing Causes'
  displayed move one step, swapping the two volumes' previewed intents. The engine rotates a fixed list per
  round and nothing can reach into that order. Return to the Premise ships as its 8 Block.
- **The shortened solo movesets (10.8) are not built** either; the survivor keeps its full cycle plus the
  Strength.

## Act II elite signatures — Drawer of Infinite Returns (2026-08-22)

Nested Return at all three depths, Closed Drawer, Depth Pressure and the signature are built.

- **The drawer is the Banished pile.** "The card leaves normal combat zones temporarily" needs a zone nothing
  else reaches into, and Banished is the only one — Exhaust is touched by ordinary cards.
- **A turn-end program must not look in the hand.** By the time a TurnEnded program reaches its
  card-touching nodes the hand has already been put down, so the returning card is in the discard pile. The
  rule therefore reads "not in the drawer" rather than "in hand", and takes the card back from wherever it
  landed. (Found from the combat log, after the counters moved and the card did not.)
- **Depth 3's "Retain for that turn" is not built.** Retain is a property of a card DEFINITION
  (`TurnEndHandDestinationZone`), not of one copy, so a per-instance Retain has nothing to hang on. It costs
  little here: an unplayed Depth-3 card returns to Depth 3 anyway, which is what Retain was protecting.
- **Depth Pressure counts entering Depth 2 and every Depth-3 return let pass** — not the step from 2 to 3,
  which the design leaves free.
- **11.9 death cleanup is not built.** Returning the nested card to the discard pile when the Drawer dies
  matters only for what the combat hands back afterwards, and the run layer rebuilds the deck from the run's
  own list — a banished copy is not lost from the deck, only from that fight.

### A note on probes, not on the rule
The opening hand is dealt while the fight is still being set up, before an interactive driver exists, so the
Drawer's FIRST offer is answered by the headless default (it files the first card). That is a property of the
test harness — from the second player turn on the offer is a real prompt with a real refusal, and the tests
exercise it there.

## Act II elite signatures — Presentless Clock (2026-08-22)

Temporal Attribution, both hands, the record slots, both Clock reactions and both signatures are built.

- **Future is a passive ScalePercent(50) on the two eligible pipelines** (`DamageDealt` restricted to Direct,
  and `BlockGain`), and the status records what was DEALT as the remainder still owed. Recording the dealt
  amount rather than "original minus dealt" is the design's own "approximately 50 %", and it keeps the two
  halves equal without the original ever being knowable after the fact.
- **Past records a fraction and lets the effect land in full**, which is exactly what the design distinguishes
  between the two hands.
- **Eligible primary effects are direct card damage and card-generated Block.** The design's third kind,
  a direct negative status application, is NOT built: an echo would have to name WHICH status to apply again,
  and the record keeps a kind and a number, not a status identity. (`OutgoingStatusApplicationStacks` exists
  as a pipeline, so the Future half would be reachable; the Past echo is what has nowhere to put the answer.)
- **An arming expires at the player's TURN END, not at the turn start.** Written at the turn start it would
  arrive AFTER the new arming and take it straight off again — a turn-start program is still draining when
  the draw happens. Without the expiry, a hand armed and never used goes on catching, so the turn you filed
  to the Future would quietly file to the Past as well. (Found in the combat log: applied, then removed, one
  line apart.)
- **The Clock reads the PLAYER's record counters.** The records are the player's effects and live on the
  player; the Clock reaches across at its own turn start.
- **12.5's "that mode is unavailable"** is an offer that does nothing when the slot is full — an option list
  cannot hide an option. The rule that matters, that a record is never overwritten, is kept.

### A harness note
`EndTurn()` hands control back while the new turn's start effects are still draining, so a test that measures
immediately can miss them. Answering the turn's prompt (or any further interaction) carries them to the end.
This is a property of the probe, not of the rules.

## Act II elite signatures — Obituary with Three Endings (2026-08-22)

All three lives, both rewrites, both death conditions and the Notice are built. **This completes every Act-II
elite signature.**

- **The rewrites are death PREVENTION, not resurrection.** `StatusDeathPreventionData` is the engine's
  data-driven one-shot pre-down interceptor: it stands before the down and hands back a surviving HP total.
  Reviving afterwards is impossible by construction — a downed combatant refuses healing and status
  application — so this is the only place to stand. The prevention consumes its own status, which is exactly
  the design's "Phase-I rewrite: maximum once".
- **A prevention interceptor cannot ask a question**, so the death CONDITION is expressed by whether the
  clause is on the Obituary at all, and the player's own rules keep it in step: a settled record takes the
  Respectable Life off, a Redacted card played takes the Heroic Life off. With nothing owed at the start, the
  first death really is final — which is 13.1 as written.
- **"Obituary-issued Overdue" is tracked as a counter on the player.** A source-bound status cannot be read
  as a scalar from the player's side, and both ends of this rule have to agree on the number. The real
  Overdue is still applied; the counter is what the clause reads.
- **Each of the five intent slots carries all three lives.** The engine rotates ONE intent list, so a slot
  reads differently depending on which life is being lived. The telegraph shows the Phase-I name throughout;
  the phase marker on the Obituary is what a player actually reads.
- **Surviving HP is clamped to the combatant's MAXIMUM.** Worth knowing when probing: a frail test body turns
  "survives at 46" into "survives at 12" and proves nothing. The tests use the authored 128-HP body.
- **The next life's clause only goes on at the player's next turn start**, so a rewritten body is briefly
  unprotected within the turn that rewrote it. That follows the design's "no attack occurs during the
  transition window" reading rather than contradicting it, but it is a real window and is noted here.

## Act II bosses — The Whispering Catalogue (2026-08-22)

The boss is built in full: Turn Record, Whispered Predictions, Authority and Contradictions, the five
Established Entries, the transition into the Complete Description, and the Final Entry. What was read
differently is listed here.

- **The Opening categories are this game's taxonomy.** The boss master names Attack / Skill / Power-Other;
  Bureaucrats and Broomsticks has Deed / Working / Rite / Junk, so an Opening is recorded as Deed, Working, or
  Rite-and-anything-else. Junk is skipped exactly as the design says. The engine's own
  `firstCardPlayedHasTag` was NOT used for this: it records the first card played whatever it is, so a Junk
  card would fill the slot the design reserves for the first real one.
- **The Turn Record is written at the player's turn END, and the engine's play stats survive it.**
  `CardPlayTurnStats` resets on the combatant's TURN START, not at turn end, so at the player's `TurnEnded`
  the count of cards played this turn is still standing and can simply be read. This is what lets the Tempo
  be the engine's own number rather than a hand-maintained counter.
- **The prediction is derived from the record by a rotating beat**, over three families: tempo, opening,
  conduct. Each family falls back to tempo when the record holds no such habit, and tempo is always valid.
  The design's further requirement that BOTH branches be currently achievable — "only generated if an Attack
  and a non-Attack opening are both still possible from this hand" — has no question behind it in the engine
  and is not attempted; the record's own gate is what decides which predictions are legal.
- **Phase II's second prediction is chosen to be a DIFFERENT reading, not the next beat.** Taking the next
  beat let both predictions fall back to tempo and merge into one, which quietly turned the Complete
  Description back into Phase I. The second reading now asks what the record actually holds: a tempo primary
  is joined by the opening or conduct habit, and an opening or conduct primary is joined by the tempo. A
  record holding nothing but its tempo yields one prediction, because there is only one honest thing to say.
- **Authority and Contradictions are visible stacks on the Catalogue; the rest of the ledger is counters on
  the player.** The ledger is about the player's cards and the player's plays, so the player carries it and
  the boss reads it from across the table — in a solo boss fight each side's lowest-health enemy is simply
  the other side, which gives the whole rule one spelling from either end.
- **The Established Entry's once-per-turn latch is a single counter**, because only one Entry can ever stand.
  "You Have Been Described" raises the allowance to two rather than re-arming the latch.
- **The five intent slots each carry Phase I, Phase II and the transition.** The engine rotates ONE intent
  list. The design's cooldowns of 2 and 3 intents are satisfied by the cycle itself: a five-slot rotation
  brings any slot round again only every fifth action, so no slot can repeat inside its cooldown.
- **"Correct the Contradiction" is not made ineligible, it resolves to nothing** when there is no
  Contradiction to correct. An intent cycle has no per-slot availability, and a cycled intent with nothing to
  do doing nothing is the same answer a card played into an empty board gets everywhere in this act.
- **All the boss's triggers are Bearer-scoped, deliberately.** Every one of these programs reads `Self`;
  under `Anywhere` each would fire on both turns and file the player's habits against the Catalogue's body
  and the Catalogue's phase against the player's. Only the Catalogue's citation stays `Anywhere`, because it
  sits on the boss while the draw and the play it watches are the player's.
- **The intent ids in the enemy JSON were renamed** from the placeholder record-names to the master's five
  Phase-I intents. The placeholder body fought at the right numbers but under names taken from the Entries;
  the boss now carries the intents the design actually gives it.

## Act II bosses — The Warden of Sealed Volumes (2026-08-22)

Sealed Zone, all three keys, Custody, Total Lockdown, Keys Turn Against the Lock and the Final Signature are
built. What was read differently:

- **The Sealed Zone is the Banished pile.** It is the one place nothing else in the engine reaches into,
  which is what "leaves normal combat zones but remains fully visible" has to mean for the return to be the
  Warden's alone. Each Seal marks its volume with its own tag, which is what says which key opens it.
- **"Retain; Cost 0" needed an engine seam.** Cost 0 was already sayable per instance
  (`CardCostDeltaCounter`); retention was not — the definition flag prices every copy alike and the
  retain-hand status tag holds the WHOLE hand. `StandardCombatIds.RetainedCardMark` is the per-instance
  counterpart, bought for this boss (RogueDeck-Core @48d0e12). The mark is cleared at the player's next turn
  start, so the retention really is "for that turn".
- **The two candidates are the first two cards in hand, offered as a choice.** The design's "displayed
  selector priority" has no engine counterpart — nothing orders a zone by cost or rarity — so the priority is
  positional. What the design actually asks for is preserved exactly: two candidates, and the player decides
  which volume is surrendered, which is what stops the Warden sniping one irreplaceable card.
- **The Evidence-to-Procedure conversion is decided at the SEALING, not at the announcement.** A citation
  needs a card left in hand once the volume is taken, and the hand only exists on the player's own draw —
  during the Warden's turn, when the announcement is made, the player's hand has already been put down, so
  the check asked there is always answered "no" and the third key would never be turned.
- **The key rotation is the Warden's own counter**, chosen from where it stands and advanced afterwards, so
  the first lock a fight meets is the Seal of Restraint as the design lists them. A key whose slot is already
  occupied is passed over, which matters only under Total Lockdown where two volumes are held at once.
- **Review Provisional Permission grants its draw to the NEXT correct release**, rather than highlighting one
  named Seal. With at most two seals, both released the same way, the practical effect is identical and it
  avoids a per-seal review flag that nothing else would read.
- **Six Phase-II intents over five slots.** "Seal the Remaining Access" is ineligible with no free slot, and
  that is exactly where "Review Provisional Permission" goes — the two share a slot and the design's own
  eligibility rule decides between them. No slot can repeat inside its cooldown, because a five-slot rotation
  brings any one round again only every fifth action.
- **Death cleanup returns every held volume to the discard.** In a `Downed` program the acting Source is the
  FALLEN combatant — the Warden itself — so the volumes are fetched from across the table, where their owner
  stands.

## Act II bosses — The Curator of Misplaced Hours (2026-08-22)

The Dial, the Turn Record, the timeline, Borrow One Minute, Free Adjustment, the transition and the Final
Signature are built. What was read differently:

- **A scheduled hour is a TIMED STATUS on the Curator.** The engine ticks a duration down at its bearer's own
  turn end and announces the expiry, which is exactly "in N enemy turns" — with a number on the table the
  player can read and that both Borrow One Minute and the Curator's own moves can push around
  (`ModifyStatusDuration`). Nothing else in the engine is a timeline, and nothing else needed to be.
- **The Dial's zero is PRESENT.** The design starts the dial there and a counter starts at zero, so PRESENT is
  what zero has to mean. The cycle itself is the design's: PRESENT → FUTURE → PAST → PRESENT is the same
  rotation as PAST → PRESENT → FUTURE, entered where the design enters it.
- **Filed hours resolve at the Curator's turn END**, which is where the engine ticks durations. The design
  says they "resolve only at the next legal enemy-action window"; a turn end is such a window, it is
  deterministic, and it is one the player can count to.
- **File the Successful Method misfiles a DRAW-PILE card immediately** rather than marking a hand card after
  the next draw. The Curator acts on its own turn, when the player's hand is already down — a mark written
  then would land on cards about to be discarded. The act's own misfiling beat puts the mark on the draw pile,
  which is where a misfiling can still cost the player something.
- **Borrow One Minute chooses from a list of the Curator's hours**, not from a picker over statuses, which the
  engine does not have. An option naming an hour that is not filed resolves to nothing — the same answer a
  card played into an empty board gets everywhere in this act. The card lives permanently in the player's hand
  (it returns there at turn end) and a once-per-turn latch is what limits it, so exactly one minute exists.
- **The Free Adjustment refunds the Energy rather than pre-empting the cost.** A card's cost is paid before
  its program runs, so "cost 0 while you hold one" is written as taking the Energy back. The maximum of 1 is
  enforced by construction: the status does not stack.
- **"No Present Intervention" protects whichever hour is nearest**, read as countdown 1, rather than naming
  one at the moment it is announced. With at most three hours and a visible countdown the effect is the same,
  and it needs no per-hour flag that nothing else would read.
- **Past Without Ending is not built as a copy of the last PAST action.** Repeating "the numerical damage
  structure of the last damaging PAST action at +25%" would need the engine to remember an action's damage
  shape, which nothing records. The Phase-II PAST sector runs the same five PAST actions, which are already
  written out of the record — the boss keeps its thesis without an engine seam bought for one intent.
- **Temporal Overlap counts resolutions per Curator turn**, reset at its turn start, which is the window the
  durations tick in.

## Act II bosses — The Auditor of Returned Lives (2026-08-22)

Supporting Documentation, the three Accounts and their response window, Discrepancy, the two later phases and
the Death Clause are built. What was read differently:

- **The Death Clause is the engine's death PREVENTION**, the same tool the Obituary uses: a one-shot pre-down
  interceptor that hands back a surviving health total. There is no other place to stand — a downed combatant
  refuses healing and status application, so a death cannot be undone afterwards — and consuming its own
  status is exactly "the Death Clause can trigger only once".
- **SUBMIT FINAL RECEIPT is a standing readiness, not a prompt at the moment of death.** A prevention
  interceptor cannot ask a question, so the condition is expressed by whether the clause is ON the Auditor,
  and the player's own record keeps it in step: while the two Documentation a receipt costs are on the table
  the clause comes off and the death is final; below that it goes back on. This is the design's own
  counterplay bullet — "keep 2 Documentation available before lethal" — made into the rule itself. The
  Documentation is not spent, because nothing fires on a death that takes; what it buys is being ready.
- **The response window is one full player turn, enforced by the answer counter.** The Account is queued at
  the Auditor's turn start, so the action in THAT turn is an ordinary one; the answer is given on the player's
  next draw, and only the action after that is the resolution. Resolving in the queueing turn would take the
  window away, which §8.11 explicitly forbids.
- **The answer is counted on the AUDITOR.** It belongs to the Account, so it is written from the player's side
  as "across" and read from the Auditor's own side as "self" — writing it on whichever body happened to be
  opposite the acting one put the queue's reset and the player's answer on two different combatants.
- **With fewer than 2 Documentation the Account goes unreconciled without asking.** The design calls the
  choice "voluntary if enough Documentation exists"; there is nothing to submit below two, and an option list
  has no per-option availability.
- **The Identity penalty redacts AND cites the same card in one beat**, on the hand after the crossing, so the
  card that was damaged is the card that can pay it back — which is what makes that citation Auditor-issued
  and therefore worth Documentation.
- **A probe body for the Death Clause must be at least 72 HP.** Surviving health is CLAMPED to the
  combatant's maximum, so a frailer test body quietly turns "returns at 72" into "returns at its own max" and
  proves nothing. The same trap the Obituary's tests document.

## Act II bosses — The Grand Cross-Reference (2026-08-22)

The last of the five, and the one that was previously not shipped at all. Four bodies, the link rotation, the
three volume passives, the engine's structural actions, the kill-order transition and all three Final Theses
are built. What was read differently:

- **"The central body cannot initially be attacked" is written as complete immunity**, not untargetability:
  the engine has no targeting restriction, and a body nothing can hurt is what that means at the table (a
  `DamageReceived` passive scaled to 0%). It has to be ON the board from the first bell all the same — the
  combat's outcome is decided the moment no enemy is living, so a concordance that only appeared after the
  third volume fell would never appear at all. Its 96 HP is its authored maximum from the start; the
  transition only takes the immunity off.
- **Which volume fell LAST is read off a roll the engine keeps.** A downed combatant's own statuses read as
  absent and it is not selected by a living-only selector, so "did this volume just fall" is asked from
  outside: the engine records who was standing at its own turn start, and a volume that was standing and no
  longer is, is the one that fell. That roll is what writes the Final Thesis, which is the whole boss.
- **Binding Authority is a reaction, not an interception.** A passive modifier is read from combatant state
  and cannot see the size of the packet, and none can be gated once per turn. So the struck volume is HEALED
  the 5 it should not have taken and the partner is dealt 5 — which leaves both bodies exactly where the
  design puts them and keeps its point that the damage is redistributed rather than erased. The secondary
  damage is dealt by the rule rather than by a card, so it cannot feed the rule again.
- **The linked partner is named rather than derived.** Nothing selects "the other member of the current
  pair" — a selector reaches allies or enemies, not one named half of a link. The reading goes to the
  Authority whenever something else was struck (it is half of the pair in two rotations out of three) and the
  other way, to whichever volume still stands, when the Authority itself was struck.
- **"The isolated volume does not act" is a gate on each volume's own intents.** All three bodies take their
  turn as the engine schedules them; the one outside the current pair resolves to nothing, which is what
  keeps three bodies from becoming three times the action. With fewer than three standing there is no
  isolated volume and everyone acts.
- **The engine's Phase-I actions never deal direct damage**, as §9.2 requires, and its Phase-II moves live in
  the same five slots — the structural action and the move it becomes are written side by side.
- **Restate the Premise is a flat half rather than a copy of the previous move.** Repeating "the previous
  non-signature move at approximately 50% magnitude" would need the engine to remember an action's shape,
  which nothing records; the same reasoning as the Curator's Past Without Ending.
- **Define the Applicable Case issues an ordinary citation.** Its "display 2 candidates and let the player
  choose" needs a picker over cards restricted to a marked subset, which the engine does not have — the same
  gap the Warden's sealing works around with an option list, and not worth a second bespoke prompt here.

## Boss relics (BnB_Final_Relics_Master_PostAudit.md §6)

Three of these are engine limits rather than choices, and each one bites several relics at once.

- **★ Energy promised while the pool is full is HELD, not gained.** A combatant's Energy pool has a hard
  ceiling — the engine clamps every gain to the pool's own max, which in this game is the 3 the turn refills
  to — so "at the start of your turn, gain 1 Energy" lands on a full pool and does nothing whatsoever. The
  affected content applies a `held_energy` status instead (`Converter/HeldEnergy.cs`): the point arrives the
  moment the holder runs dry, so it still buys the extra card the design's numbers are for.

  Eight boss relics hold their Energy this way — Unfinished Docket, Access Seal-Shard, Brass Service Bell,
  Ivory Number Disc, Errata Ribbon, Misdated Pocket Watch, Borrowed Minute, and the Deferred Appointment
  Book's third turn. **The same ceiling had silently voided six pieces of content written earlier**, all
  fixed in the same pass: the Blood-Stamped Bond, the Rootbound Walking Staff, the Binder's Awl and the Iron
  Astrolabe (Normal relics — the Astrolabe's whole promise is that Energy), the Called Next boon (Event
  relic), and the Rite that pays out recorded refusals. Anything that grants Energy MID-turn (the Emergency
  Inkwell at an empty pool, the Archive Censer when a keyword lands) was never affected and is untouched.
- **★ A turn-end program cannot see the hand.** `DiscardHandOnTurnEndedHandler` is registered ahead of the
  turn-end triggers, so by the time a relic's end-of-turn rule runs the hand is already in the discard pile.
  The Backlog Counterseal therefore writes the hand down as it stands (at the draw, and after each card
  played) and pays out what was last written; the Red-Ribboned Matter's choice and the Custody Shackle's
  custody both move to the DRAW, with the Shackle letting its card go again the moment the turn turns busy —
  which is the condition the design states, read from the other end. **The same ordering had silently voided
  the Conservator's Thread**, which tested a hand that is never there; it now reads the hand as it stands and
  is fixed in this pass.
- **★ "Turn N" is counted per relic.** `turnNumber` counts turns within a ROUND — in a duel the player's turn
  is always turn 1 — so the Deferred Appointment Book, the Brass Service Bell and the Master Release Key
  count their own turns in a counter instead.
- **The Municipal Dragon's two writs fire themselves.** The design gives them a free action the holder spends;
  the engine has no player-activated relic, so each fires at the moment it would have been spent — the first
  time in a fight the holder runs out of Energy with cards still in hand. The Civic Entry Warrant strips the
  enemies' standing Block rather than ignoring it, which is the same outcome for the turn it is spent in.
- **The Inspector's Brass Charter ships as its Block half.** Revealing an enemy's *following* intent is a
  frontend affordance the engine does not carry.
- **The Margin of Appeal defuses an intent instead of replacing it.** Choosing what an enemy does instead is
  not something a relic can reach — intents are the enemy's own rotation — so once per combat the enemies'
  next turn deals half damage.
- **The Custody Shackle takes the first card in hand, not the dearest.** There is no "the highest-cost card in
  this zone" expression.
- **The Identity Writ watches types, not names.** Nothing in a fight remembers which card NAMES have been
  played; the stats it keeps are per turn and per type. The repeat that counts is the second card of the same
  type in a turn.
- **The Closure Writ heals a quarter of what is MISSING, capped at 10.** The run layer does not track how much
  health a particular fight cost.

## Act II — the fifteen archives events (2026-08-27)

The archives' doors, from `BnB_Final_Events_Master_PostAudit.md` §"ACT II". Like Act I's they are AUTHORED
(`Converter/Events/ActTwoEvents.cs` + `ActTwoEventPrograms.cs`) rather than converted; with this pass the
ported v2 event JSON is out of the loader entirely, and `EventMapper`, `BabEvent` and `BabData.Events` are
deleted — every door in the game is now written in C# against the engine's own vocabulary.

**One engine seam bought.** "Earliest Stage N" is on every Act-II event and had nowhere to go:
`MapGenerationSpec.NodeRefPools` draws refs with no notion of depth, so the Librarian at the end of the aisle
could be the first room of the run. `MapGenerationSpec.NodeRefMinimumDepthPercent` (RogueDeck-Core) gates a
ref by how deep into the act its node sits. It is a **percentage, not a row index**, because the generated
map is taller than the act's stage ladder — gate funnels are inserted into the backbone — so a row number
authored against the design's stage count would land at the wrong depth. Act II converts stage N of ten to
`(N−1)·100/9`; Act I gates nothing and is byte-identical.

What could not be translated straight:

- **A card cannot be made unplayable for a while, so it is priced instead.** Unclaimed Reservation's third
  branch registers a card to somebody else; there is no per-instance Unplayable (a card Exhausts or is
  unplayable because its DEFINITION says so), so the register writes +9 Energy on the copy, which nobody can
  pay. The first card played strikes the price off. "If left unplayed it becomes Misfiled" is read one beat
  later, at the next turn's draw — the established Act-II idiom, since a turn-end program cannot see the hand.
- **The Necrology Window's borrowed life is two statuses.** The engine's death prevention is authored data,
  but the health it survives at is a CONSTANT, and an act's bodies do not share a maximum. So the prevention
  catches the body at 1 HP and a companion rule immediately writes it back up to 30 % of its OWN maximum,
  once. "The primary enemy" is `highestHealthEnemy`: nothing in a generated fight says which body an encounter
  thinks of as primary.
- **"One of the two you just improved" is asked as "one of your improved cards."** A single prompt cannot
  improve two cards and then point at one of them; the second prompt reaches every upgraded card in the deck.
  The archives are not fussy about which.
- **A rarity-filtered reward is a real pool, and its tag rides INSIDE the offer.** `CardRewardSource(rarity)`
  draws Rare/Uncommon only. Where the door promises something about the card taken ("choose 1 of 3 Uncommon
  cards, and it starts in a Reservation"), the tag is part of the offer's own effects rather than a following
  `LastAddedCard` write — a reward is declinable, and a declined offer must write nothing.
- **The Reciprocal Shelf's "if no eligible card exists, 1 Paperwork instead" is unreachable.** Run selectors
  cannot see a card's printed type, so "no eligible non-Junk card" cannot be asked; a deck always has a card
  to mark. The fallback is dropped.
- **8 % of Max HP is a flat 6.** `ChangeMaxHealthRunEffect` takes a constant, and the bureaucrat's maximum is
  70. The Perpetual Borrower's library card costs 6.
- **The Librarian is not drawn as a Rare event.** `NodeRefPools` has no per-ref weights; its depth gate is
  what makes it late instead of unlikely.

★ **Two traps this pass paid for, both worth remembering:**

1. **A branch decided by the fight that just ended must be a program CONDITION.** `ConditionalRunEffect` is
   enqueued and evaluated LATER, when the resolved combat is no longer the event in context, and asking that
   context for `combat.counter.…` throws rather than returning zero. A run program is DATA — `game.roguedeck.json`
   is the whole game — so `RunEffectTemplates.Custom` is not an escape either: it has no serialization kind
   and the document stops round-tripping. So the two outcomes are **two programs over the same event**, each
   ruling the other out and each cleaning up after both (the Vow, the whispered amendment). Exactly one fires.
2. **A counter the run will read is written down at the opening bell.** A fight the rule was never in reads
   the counter as zero, which is how a waiting promise tells "the fight I was about" from any other fight —
   so the Vow writes a ONE it can only lose, and the amendment a zero it can only gain.

…and one thing that is NOT an adaptation but reads like a bug in a test: the opening draw empties a small
deck's draw pile, and an empty pile is reshuffled from the discard. Anything a door FILED somewhere (a
misfiled card taken back, a Citation put in the discard) is dealt straight back into the hand. A test that
wants to see where a card was put needs a deck that does not fit in one hand.

---

## The alpha pass — Acts I & II, played end to end (2026-08-27)

Everything below came out of actually walking whole runs (`Converter --playtest`, `Tests/WholeRunTests`)
rather than out of reading the design.

**An act is as long as it PROMISES to be.** Two prescriptions disagreed. The audit's per-path table
(`docs/bnb-act-map-specs.md`) already fixes an act's length: every promise becomes a full row every route
crosses, so Act I's nineteen promises *are* nineteen rooms. The act manifest's `steps_before_boss` (9) is the
ported v2 number from a map model that had no such promises — and it was being added **on top**, which made a
nine-stage act twenty-eight rooms long. It now counts toward the promises rather than after them
(`MapSpecBuilder.FreeRows`), leaving a floor of five free rows. Five is not a taste: the free rows are the
only ones where two routes can hold different KINDS of room — every promise is a full row every route crosses
— and below five the fightiest and the quietest way through the city stop differing at all
(`EndToEndSmokeTests.The_routes_through_the_act_differ…`). A route is now 24 rooms rather than 28, and what it
costs is admitted here: with nineteen of them promised, this act is a **staged pilgrimage** (the manifest's own
word for its layout) where the choice is mostly WHICH fight and WHICH door, not what kind of room comes next.

**Where a kind of room may first stand is now authored** (`ActRules.EarliestDepthPercent` →
`MapGenerationSpec.RoleMinimumDepthPercent`, new engine seam). The per-path table says how much of a thing a
route holds and nothing about where, and the answer used to be the gate order: a **shop in the opening row**,
where nobody has any gold, and an **elite in the fourth**, with the starting deck. The city now opens its shop
at 12 % of the act, its ambush at 20 % and its elite at 35 %; the archives at 10 / 12 / 22 %.

**A promise is spread across the act, not taken in turns.** `FlattenGates` round-robined the kinds, which
looked even and was not: with eight fights promised and one of everything else, the rare kinds all fell in the
opening passes and Act I ended in a wall of seven identical fights with nothing to recover at. Each copy is
now placed at its own share of the sequence.

**The campfire has its second action.** `BnB_Run_Systems_Master` §3 gives a waiting room *Authorized Leave*
**or** *Submit an Amendment*; only the heal was ever built. The amendment is offered unconditionally — a "how
many cards could be improved" guard is not expressible as data (a count over a selector is an escape node and
would not serialize) and is not needed, since a choice with nothing to improve picks nothing.

★ **The trap this pass paid for: C# static field ORDER can leave an id nameless.** A `static readonly CounterId`
declared BELOW the card that uses it is still `default` when that card's initializer runs — and `default` of
an id struct is a **null string**. Twenty-two cards shipped with nameless counters, and at the end of every
fight one of them was played in, the run died with `ArgumentNullException` out of a dictionary. Nothing caught
it because a null id serializes perfectly happily. Every `CounterId` in the converter is now a **property**
(`static CounterId X => new("…")`), which cannot be ordered wrong, and `Tests/DocumentIdTests` fails the build
if any id reaches the document without a name.

---

## Every name now says what it means (2026-08-28)

Three quiet holes, all of the same shape — a thing with a name and nothing behind it:

- **82 of the game's 500 statuses** reached the player with a name and no explanation, among them Panic,
  Fatigue, Strength, Poison and Bookworm — the five words a player meets first. Eight more "explained"
  themselves by repeating their own name.
- **113 of the 162 relics** had no hover text: only the PORTED relics got a presentation entry, and the
  authored pools (normal, shop, boss, event) are two thirds of them.
- **31 of the 353 cards** had none either — and they were the encounter-given ones (a Notice, a Clause, a
  Fragment, a boss's action card), which is to say the cards a player meets without warning.

Nothing noticed, because all three are perfectly valid data.

All of them explain themselves now, and `Tests/EverythingExplainsItselfTests` fails the build if a new one
arrives mute (or explains itself by echoing its name). The fixes went to the causes rather than the symptoms:
three authoring helpers took a name and no description (`PassiveStatuses.Marker` / `Passive`, and the elites'
local `Marker`, which passed the NAME as the description) and now require one; `BnbCard.Compile` carries the
card's rules text into the document itself rather than leaving it in the manifest; and the manifest fills in
anything it did not name from the entity's own text.

The five keyword statuses are described **here** rather than copied from `source-data/statuses/statuses.json`:
the original's text says "at the start of the player's turn" for statuses this engine ticks on whoever is
carrying them, and a description that describes another game's rule is worse than none.

---

## Act III — the Green Docket's vocabulary (2026-08-28)

**Safe-Conduct spends itself.** The design says the player *may* spend 1 Safe-Conduct to prevent a Trespass
application "from a concrete source". There is no moment in this engine at which a player is asked; a
prohibition is an interceptor and answers before anyone can be consulted. Safe-Conduct therefore refuses the
next Trespass automatically, oldest stack first. Since the licence refuses Trespass and nothing else, there
is nothing to save it for.

**One stack pays for one stack.** Every Trespass in the act is applied one at a time, so "prevent the full
Trespass application" and "prevent one stack" are the same rule today. If a later source ever files 2 at
once, one Safe-Conduct will refuse one of them.

**A Claim is a resource; a claim being MADE is a separate announcement.** The design's own §3 spends a page
keeping "newly created" apart from "transferred", because that distinction is what stops the Boundary
Stone / Ditch Lamprey / Bracken Moot loop. A single status could not carry both readings — moving stacks
between two enemies raises the same events as granting them — so the content keeps two: `claim`, the
standing, and `claim_created`, a count that only ever grows. Everything that "creates" a Claim goes through
`ActThree.CreateClaim`, which raises both; a transfer moves only the first.

**The Claim ceiling is enforced where Claims are made**, not by the status. An enemy already holding 3
neither gains a fourth nor announces one, so a rule waiting on a new Claim does not fire for a Claim that
was never granted.

**"The first time each player turn" is a latch cleared at the player's turn start.** Turns here belong to
combatants, not to the table, so the window a rule counts in runs from one player turn start to the next —
which means a violation the enemies file during their own turn belongs to the player turn that just ended.
The Cairn of Stray Paths is the first rule this matters for and it reads correctly either way; later
identities that count "per player turn" use the same window.

**Trespass from a Local Law is filed in the lawgiver's name.** A Local Law answers something the PLAYER
did, so the acting source of the rule is the player. Filing the violation in the player's name would mean
no source ever reaches three, so the engine grew a seam for it (`applyStatus` may name its source), and a
law whose author is dead files nothing at all.

**Encounter scaffolding is a per-encounter starting status.** The design asks for the Boundary Stone to open
its two teaching fights already holding a Claim, and to open every later one bare — so a body's opening
statuses can now be extended per ENCOUNTER (`BabEncounter.enemy_statuses`, indexed into the roster) rather
than only per identity.

The scaffolding itself is a rule and not a Claim, because a Claim placed as a starting status is placed
without ever being APPLIED — no event, so nothing that listens for a new Claim would hear it, and Wandering
Title would never fire. `Prior Dispute` therefore grants the Claim at the player's first turn start, before
the draw and before any action, and then removes itself.

**"Two consecutive cards" is read within a turn.** The Reckoning Hedge's memory of the last Base Cost is
cleared when the player's turn starts, so the first card of a turn is never consecutive with the last card of
the one before it.

**The Hedge's survey is flipped only by Claims it was GRANTED.** A Claim handed to it by the Boundary Stone
standing next to it does not reverse the law, which is exactly what the design's Encounter 6 is built to
teach — and it falls out of reading the announcement rather than the resource.

**Prior Possession is a mark, not a prohibition.** Nothing in the engine asks a status's permission before
moving it. Every rule in the act that moves or spends somebody ELSE's Claim goes through one selector
(`ActThree.ClaimsOthersMayTake`), which excludes whoever the fight has recognised as the sitting occupier.

**Act III files every Trespass in one place.** The Contrary Magpie decides who a violation is owed to BEFORE
it lands — the design is explicit that Safe-Conduct is only offered against the source the argument leaves
behind — so it cannot be a reaction to the application. And the Foxglove Witness needs to know WHICH law was
broken, which a Trespass does not carry and which the Magpie's rewriting would destroy anyway. So a law is a
number written onto the player as the violation goes past, and the pressure intents are authored programs
rather than DSL effects so that an intent's Trespass is filed the same way a Local Law's is. Their JSON
entries keep their own actions, because the telegraph is written from those.

**A violation and the law's answer to it are two different things.** The design caps a Local Law at one
Trespass per player turn and then, in Encounter 9, asks the Foxglove to witness "a concrete second violation"
of that same law in the same turn. Both are true: the BREACH is uncapped, and only the law's own Trespass is
once a turn. That is exactly why the Foxglove is put beside the Hedge — breaking the survey twice still costs
you, just not through the Hedge.

One consequence worth knowing: a turn whose FIRST breach is refused by Safe-Conduct is a turn the meadow
hears nothing, because the refusal still spends the law's once-a-turn answer and nothing ever lands to be
witnessed.
