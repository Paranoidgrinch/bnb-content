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

The city shop was relabelled to make any of this possible: its stock is **shelves** whose pools are deeper
than what they show, and every entry says what it is (kind + tags). Without that labelling a price rule matches
nothing and the relics would quietly do nothing. (There were two shelves then, `cards` and `relics`; there are
four now — see "The shop is a fixed shape".)

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

## Act III — Wergild, and the free action that had to be a card

**Make Amends is a card.** The design calls it a "free encounter action"; a combat here has no such thing,
only cards. So the fight puts one in the player's hand the moment a demand is raised. It costs nothing,
survives the turn boundary, and returns to hand after each use for as long as anything is still owed —
which is also how it disappears: nothing owed, nothing offered.

It never offers ITSELF as the payment. A card being played is still in its owner's hand while its own
program runs, and being free it would be refused under the Snail's charter — a trap rather than a decision.
The engine grew a card choice that can name what it will not offer.

**A payment answers the oldest demand.** The design does not say which creditor a point of Wergild goes to
when several are owed, and the oldest-first reading is the one a court would take.

**A demand's clock belongs to the creditor.** Raised is not yet due: it matures at the player's next turn
start and falls due at that turn's end, so a demand raised during the player's own turn does not expire
before the player has had a turn to answer it. Two markers on the creditor say which state it is in.

**The Snail's charter is read from the fight, not from the creditor of the payment.** "Cards with Base Cost
0 cannot be used as Offerings to pay Wergild owed to Charter-Shell Snail" — a payment cannot be told which
creditor it will land on before it lands, so the charter applies while the Snail is owed anything at all. In
the Snail's own encounters that is the same rule; in a hypothetical fight where the Snail and another
creditor are both owed, it is stricter than the design.

**An unpaid demand is cleared by its own creditor.** Each one reads what it is owed, deals 2 damage a point,
takes its Claim and then spends exactly its own stacks — which is what the engine seam for naming whose
status instances a rule means was bought for. A blanket removal would have wiped a demand that had not yet
come due.

## Act III — hospitality, and whose licence was spent (2026-08-28)

**Safe-Conduct provenance costs nothing.** The design allows a Safe-Conduct stack to remember
`granted_by = Roadside Witchling`; it already does, because Safe-Conduct is per-grant instances and an
instance carries its source. What the engine cannot answer is which stack an interceptor happened to spend —
so the Witchling does not ask. She counts her own stacks when the player's turn begins and counts them again
when it ends, and the difference is the whole rule.

**Her grievance is refused by her own gift.** She files a Trespass for a courtesy carried unspent, and the
player is holding licences — including hers — which refuse Trespass. That reads exactly right: the gift is
real, it is not free, and spending it on her own complaint is one of the ways out.

**Every Green Docket body wears a party marker** (`green_docket_body`), so a rule can say "the parties in
this fight" without knowing which side it is looking from. The Crossroads Cup needs it: it answers a
REFUSAL, where the fight's source is the player, and "the enemy with the fewest Claims" would otherwise have
to be spelled twice.

**Each law that measures consecutive cards keeps its own memory of the last Base Cost.** Two laws sharing
one counter would race — the order two CardPlayed rules fire in is not decided — and whichever wrote first
would leave the other comparing a card against itself. The Reckoning Hedge and the Blackthorn Bride both
measure, and can stand in the same fight.

## Act III — the quorum and the appeal (2026-08-28)

**Common Mandate is a licence, not standing.** "Another party may spend 1 Claim belonging to the Mushroom
Circle to pay the Claim cost of its own ability" — nothing changes hands, so the Circle spends one of its
own and marks its neighbour as acting under it. Each identity with a Claim-driven ability answers that
marker as well as the Claim: the Two-Bank Toll Ford charges for standing merely lent to it exactly as it
charges for standing it was granted.

**A spent Claim is announced too** (`claim_consumed`), because Stage 10's Handworn Tally Coin counts
consumption and nothing else, and consumption is distinct from a transfer, a review, or a removal.

**Under Review is read by the one door.** Every rule in the act that reaches for somebody ELSE's Claim goes
through `ActThree.ClaimsOthersMayTake`, which now excludes both the sitting occupier (Prior Possession) and
whoever is currently before the bench.

**Settlement on Appeal lives in the settlement.** Only the moment a demand is settled knows that it was
settled, so the Sedge Bench cannot extinguish the reviewed Claim itself — the rule sits in the Wergild
settlement, where the creditor that paid in full and happens to be under review loses the Claim as well.

**A transfer hands over only what was handed in.** Two rules can answer the same grant, and the pair would
otherwise make two Claims out of one. The transfer is guarded on the giver actually holding something —
which also proves, incidentally, that two triggered programs on one event do see each other's effects.

## Act III — precedent, jurisdiction and the court (2026-08-28)

**Rings of Precedent are a placeholder the design asked for.** The master states the intent — an early Claim
makes the consequence stricter — and leaves the numbers to the balance pass. The plainest reading of it is
what is built: the Old Measure costs 1 Trespass, and one more for every Claim the Stump holds.

**The Precedent Lichen cites the last law anybody was found to have broken.** The design says it chooses a
neighbour's Local Law; a law here is a number written down as the violation goes past, and the only
authority a fight has actually established is the last one invoked. From then on the Lichen answers every
breach of that law as well — the Foxglove's shape exactly, and for the same reason: a law's own answer is
capped once a turn and a breach is not.

**Where the player's attention went is not a number.** Two Stage-9 identities ask about it and each writes
its OWN mark: the Elsewhere Path marks everything aimed at this turn (was the destination ever reached), the
Trail Marker marks only the last thing aimed at (how often did the eye move). Sharing one mark would race —
the Marker must read where the eye WAS before it moves, and the order two CardPlayed rules fire in is not
decided.

**"A name already spoken" is a copy already played.** The engine keeps no history of which card DEFINITIONS
have been played in a combat, and a per-instance mark answers the question the Keeper actually asks in play:
you played that card again. Two copies of the same card in a deck count as two names here, which is a
deviation — and a defensible one, since they are two pieces of paper.

**The Bench hears one matter at a time**, which is what makes it slow enough for reeds to grow through the
record — and which stops the review marker stacking up round after round.

## Act III — the nine elites (2026-08-28)

The elite layer adds no fifth Act-wide mechanic. What follows is every place the port's shape differs from
the master, and why.

**Safe-Conduct refuses a whole application, not a stack.** §5.2 says one licence prevents a full Trespass
application "if a source attempts 2 at once". That is exactly a prohibition that pays for two incoming
stacks with one of its own, so the licence's `StacksPerStack` is 2 — a single Trespass still costs a whole
licence, because the spend rounds up. The Stag's marked verge and the Web's knotted thread are therefore
written as ONE application of two rather than two of one.

**An injunction names the licence that may not answer it.** The Juniper's Against Safe Passage does not
remove the licence and does not make it unspendable — it says that THIS violation is beyond it. That is the
mirror of a prohibition's own "the one status I refuse", and it was bought as an engine seam
(`ApplyStatusEffectRequest.UnrefusableBy`), threaded through the act's single filing point.

**A fallen body can still be named by the mark it wears.** Replace the Fallen has to reach a Line-Bearer
that is down; every status-filtered selector in the engine was living-only. `WithStatus(..., includeFallen)`
is the second seam this arc bought. Standing the Bearer up comes first and healing it second, because
setting health is a living-only operation.

**The Wrong Bridge is one 200-HP body, not 96 + 104.** The design spawns a second body at Phase-I lethal,
transferring the surviving Claims and preserving the open Wergild. Built as one body that turns around at
104, both of those are true by construction rather than by a transfer that would have to be written, tested
and kept from announcing itself as a grant.

**A rule about the whole fight is carried by the player.** The tribunal's Appeal Chain, the Juniper's
Granted Use and the Magistrate's Three Judgments all live on the hero, because they have to outlive any
body they are about (a rule kept on a reed stops working the moment that reed is cut) or because they are
asked of the player's own hand.

**"At the end of the enemy turn" is written at the player's bell.** A turn ENDING happens once per body, so
the Ant Queen's Closed Formation and the Reeds' Appeal Chain would fire once per enemy in a multi-body
encounter. Both are written at the player's turn start instead — the same board, seen from the side that has
to get through it, and exactly once a round.

**A choice offered at the bell is offered in full, and the first one is not offered at all.** An option list
is a fixed list, so the Juniper offers all four Granted Uses and the Magistrate all three Judgments rather
than two of them; the design's "no impossible category may be offered" then holds trivially, because the
traveller picks the one they can live under. The Juniper's own NARROWING is what removes the choice. And the
opening hand is dealt while the fight is still being handed over, before there is anybody to put the
question to — so the first leave and the first judgment are handed down (Deeds, and Conduct) and the asking
starts on the next turn.

**The bell is written into the drawing, in front of the question.** A parked question holds the turn's other
triggers behind it, so a Binding Judgment established by the answer would be run down by the same bell that
established it. The Magistrate's rulings therefore run down inside the same program that asks, before it
asks — and that program only fires on the hand a turn OPENS with, so a card that draws mid-turn does not
reopen the hearing.

**Blocked at settlement is Block that never existed.** A demand falls due as a turn ENDS, and Block granted
there is swept away before the player can meet it. The Reeds' "Nothing Ends Here" therefore books the
Remand's guard and puts it up at the next bell, which is the turn it was ever meant to survive.

**Permitted Exception strikes the Block off there and then.** The design says the next direct hit ignores up
to 6 of the Bearer's Block. A Bearer's Block is only ever gained on the Queen's own turn, so removing up to
6 at the moment the licence is spent leaves exactly the board the design describes.

**Small readings, listed once.** "Directly targets a Line-Bearer" is the card's own target, and an area
effect names nobody — which is the design's AoE exemption for free. "No VALID non-Junk card in hand" is read
as "no non-Junk card": whether a card could legally be played is not a question the rules layer can put to
itself. The Magistrate's redress lifts the OLDEST binding, read as the one with the fewest turns left, since
every binding starts at two and runs down together; the Juniper's relief lifts the first injunction in
order, because a settlement resolves as a turn ends and there is nobody there to ask. Written Refusal files
a Trespass in the Refusal Reed's name — without it, Final Refusal would have nothing to answer, since
nothing else in the tribunal files in the Refusal's name. And the Surveyor's citation, like Make Amends, is
offered again at the bell: a right earned as a turn ENDED has no card to cite it with, because the hand it
was dealt into is put away in the same breath.

**Mark the Verge is a threat that only bites a dry traveller.** With §5.2 as written, one licence refuses a
doubled attempt whole — so the Stag's verge is worth nothing to a player who still holds one. In a solo
Stag fight, where the Stag itself refills the licence every turn, that is never. It is left as the master
wrote it and belongs on the playtest watchlist rather than in a silent rewrite.

## Act III — the five bosses (2026-08-28)

The bosses add no sixth Act-wide mechanic either. Their local systems — Ground, Notarial Seal, Courtesy,
Buried Claim, Favour — live and die inside their own encounters. What follows is every deviation.

**A rule about the end of a turn cannot read the hand.** The turn's end puts the hand away before a rule
about the turn's end runs, so four rules in the act that ask what the player is still holding — the Web's
Thread of Departure, the Trail Marker's third reading, the Notary's Ring of Keeping and Grandmother's Better
Chair — read an empty hand and were silently always-true or always-false. The act now keeps a **hand budget**
instead (`ActThree.HandBudgetCounter`): how many real cards the hand held when it was last dealt to, counted
forward, minus what has been played. It answers the same question, it survives a mid-turn draw because a draw
rewrites it, and it needed no engine change.

**A choice offered at the bell is offered in full, and the first one is not offered at all.** As with the
elites: the Grandmother lays the whole table rather than showing two courtesies, and the Queen's court offers
all four graces. The rule that matters — at most one, and declining is free — is kept exactly. And the
opening hand is dealt while the fight is still being handed over, so the first grant is made rather than
asked: the Ombudsman opens its hearings on the Road, Grandmother lays the table, the Queen's court settles.

**"At the end of the enemy turn" and "the boss's next action" are written where they can be seen.** A
transition is a MARKER that replaces the boss's next action rather than a hook in the turn machinery — which
is exactly the design's "queue it as the next legal boss-state action, no direct attack" — and the Answering
Hill's two-step thresholds fall out of it for free: the stirring sets the answering, and the whole player
turn between them is a real turn to settle in.

**Grandmother's honey is Block on a full purse.** A purse has a hard ceiling here, and the honey is poured
while it is still full. The promise it asks for is unchanged, and that is the half of a courtesy that
matters. Her Warm Tea keeps only the first half of its clause (play a Working); the design's second half,
about the order of a second Attack, is not something a player can read at a glance.

**The Queen's law is filed rather than gated.** "The first non-Junk card played while the player has no
Safe-Conduct" could never fire in this port: the act opens every Green Docket fight with a licence, her own
graces hand out more, and in a solo court nothing else spends them. Filed unconditionally it behaves exactly
as the design describes — leave to speak is what a licence IS, so while you hold one the court takes it and
says nothing, and the first word you say without one is the violation.

**Block granted where the turn ends is Block that never existed.** The Reeds' "Nothing Ends Here" books the
Remand's guard and raises it at the next bell; the Queen's Sovereign Reciprocity gives the player its 4 Block
inside her own action, where a solo fight has nothing left to spend it on — it is left as the master wrote it
and belongs on the playtest watchlist.

**Small readings, listed once.** The Ombudsman's Grounds are two counts beside the Claim, not a per-stack
label, so a Counter-Petition moves one from the one count to the other and raises no announcement — which is
the design's "neither creation nor transfer" exactly. The Notary's "oldest binding" and the Magistrate's are
both read as the shortest-lived, since every one starts at the same length and runs down together. The Hill
buries the standing it is granted by removing the Claim the instant it is made, which is what "would gain"
means where a Claim cannot be intercepted. And every player-side free action in the act — Make Amends, Cite
the Old Survey, Counter-Petition, Spend a Counterseal, Right of Audience — is a card the fight puts in the
player's hand, because a combat here has no free actions, only cards.

## Act III — the fifteen boss relics (2026-08-28)

Each of the five Act-III bosses hands over one of its own three, and each of the fifteen is a piece of ITS
boss's machinery given to the player. The deviations:

**Every "once per turn, free action" is a card.** A combat in the Green Docket has no free actions — the act
settled that when Make Amends was built — so the Ombudsman's twine, Grandmother's three courtesies, the
Queen's cup and her name-tally are cards the relic puts in the holder's hand at the bell. Each is free,
exhausts when played, and is offered again next turn (the tally only once a combat). The relic is what
remembers; the card is the hand the player reaches out with.

**A relic's own turn-start trigger has already been and gone.** A relic is put on at the first bell, so a
rule it installs on TurnStarted misses the turn it was installed in. Everything that has to be true from the
first turn — the Notary's two rings arming and resetting — is written on the DRAWING of the hand instead.

**Boundary Tally does not ask which Ground.** The opening choice is dropped and the alternation kept, for
the same reason the Ombudsman's own hearings open on the Road: a question put before the fight has begun has
nobody to answer it.

**"Costs 0" is the price refunded.** Both Countersealed Rings and the Silver Name-Tally make a card free
AFTER it has been paid for, because that is when the rule knows which card it was — and what the holder
feels is the same Energy back. It arrives through HeldEnergy, like every other Energy this pool promises
while the purse may be full.

**The Ring of Restraint keeps its own consolation.** The design gives both it and the Ring of Keeping the
same one — retain a chosen card and cheapen it — which would make them one relic. Here a turn that never
reaches three cards simply keeps the ring armed into the next.

**Silver Name-Tally guards instead of weakening.** "Its next attack deals 10 less" has no engine face — there
is no per-enemy outgoing-damage reduction — so the holder takes 10 Block against what was coming. The points
are the same; who holds them is not.

**Survey Cairn buries without asking, and the Surveyed Milestone reads percentages.** A prompt at the turn's
end has nobody to ask, and the trade is the same either way: twelve Block about to be swept up, for an
Energy and a card. The Milestone's landmark is the highest-health enemy on the field rather than the
highest-MAX-health, which is the same body in every encounter that has one.

## Act III — the fifteen Green Docket events, and the act itself (2026-08-28)

The road's doors, from `BnB_Final_Events_Master_PostAudit.md` §"ACT III", AUTHORED like Act I's and Act II's
(`Converter/Events/ActThreeEvents.cs` + `ActThreeEventPrograms.cs` + `ActThreeEventObjects.cs`), and with
them **Act III joins the walked run**: `ActRules.For(3)` gives the act its per-path table (the audit's
Combat 8 / MultiCombat 2 / Elite 3 / Event 3 / Rest 2 / Treasure 1 / Shop 2), its three lanes — the old
road, the hedgeways, the water meadows — its depth gates and its rest and treasure voice, and
`BabLoader.Acts` loads `acts/act_3_green_docket.json`.

**No engine seam was needed.** Everything the fifteen ask for is said with pieces the first two acts bought:
authored run programs installed by name, combat openings, per-card tags read by a status rule, and the act's
own Wergild machinery.

What could not be translated straight:

- **The manifest's two extra map knobs are read and ignored.** `act_3_green_docket.json` still carries the
  original generator's `first_elite_depth` and `elite_weight_multiplier`. Both are answered by this port's
  own map rules (`EarliestDepthPercent[Elite]` and the lane weights), so `BabMapSettings` accepts them
  rather than letting a strict load abort on them — which is the same treatment the other four dead knobs
  already get.
- **The act had no mimic, and the map spec asks for one at 15%.** `green_docket_mimic_01` — The Counted
  Cairn, a `cairn_of_stray_paths` at 104 HP — is authored into the encounter file and the manifest's
  `mimic_chance` corrected from the ported 0.10 to the audit's 0.15. It is the one Act-III fight that is not
  on §5's stage table, which is why `ActThreePoolTests` pins it separately from the 12 + 28.
- **A shop node cannot be opened from a door**, so the Conceptual Toll and the Travelling Chandler are
  counters built INSIDE the event, exactly as Act I's Licensed Vendor is: an authored stock, each item
  bought at most once, at the city's prices less the discount the design names (15% and 20%). What it costs
  is the reroll and the per-run redraw — these two shelves are the same shelf every run.
- **"Enemies have 20% less Max HP" is paid at the bell.** Max health cannot be lowered from outside a fight,
  so the shortcut roads (the Witch's shortest road, the Waystone's forgotten name) take the shortfall as
  unblockable damage at the opening, read per body off its own maximum.
- **"Grants no Gold" is a bailiff, not a suppression.** A fight's purse is paid out by the map after the
  combat resolves, so `GarnishedReward` arms `GarnishThePurse`, which takes back the very next positive Gold
  change and then steps down.
- **"After victory gain N Gold" is its own promise.** The footpath's 80 and the complaint's 60 are paid by
  the fight the door was about — and paid whether or not something else garnished that fight's purse, which
  is why they are separate programs rather than a branch of the bailiff.
- **A vow is written down, not enforced.** Moonlit Mushrooms' quorum and the Ant Queue's line never stop a
  card; each opens a counter at 1 on the first round and only ever lowers it, and the run reads the outcome
  off `CombatResolvedRunEvent` afterwards — the same idiom Act II's `VowHeld` uses. Kept and lapsed are two
  programs over the same event, each ruling the other out and each uninstalling both, because a branch
  decided by the fight that just ended has to be a program CONDITION and not a conditional effect.
- **"First full Wergild payment grants 1 extra Safe-Conduct"** is read off the payer. A Clear Stream's
  bottle is a marker status the fight opens with; the act's one settlement (`ActThreeWergild.Settlement`)
  adds the bearer's stacks of it to the licence it grants and then empties the bottle — the same shape the
  Oath-Fish's marker already had on the creditor's side.
- **The Chandler's flame is HELD, not gained.** "+1 Energy on turn 1" lands on a full pool at the opening
  bell and would be silently clamped away, so the point waits as `held_energy` and arrives the moment its
  holder runs dry (`HeldEnergy`).
- **"1 less Safe-Conduct, minimum 0"** is a `modifyStatusStacks` of −1 in the next fight's opening: a stack
  removed from a status nobody has is simply not removed, so the floor needs nothing said about it.
- **The Spider's exception is written on any card, not on an Exhaust card.** The design asks for "one
  compatible Exhaust card"; an event's card picker cannot filter on a tag the deck may not contain at all,
  and a branch that can offer nothing is worse than one that offers everything.
- **The Kindly Procession's fourth step is not gated by stage.** The engine has no reading of how deep the
  run is at the moment a door OPENS — only of which depths a door may appear at — so "Stage 9+" would have
  nothing to test. The door itself waits for Stage 8, and the step's own price (12 Max HP, and every party
  on the next road with standing) is what keeps it honest.
- **The Ombudsman's Warning cannot un-file a boss's first Claim.** "If the Act Boss is the Ombudsman of Root
  and Road, remove his first generated Claim once" would need a promise that reaches inside a specific
  fight's rules and cancels one application; the branch keeps its two upgrades and its Elite-or-Boss
  licence, which is the part the player can plan around.

---

## The replay baseline, and the card a greedy player can play for ever (2026-08-28)

Two findings, both about the HOST rather than the game, and together they are why a walk could not reach the
end of Act III. Neither changed a rule of Bureaucrats & Broomsticks.

**1 · The replay baseline never moved.** The interactive session re-executes the whole run from its baseline
for every answer, so the price of one answer grew with the number of answers behind it. Measured on the walk
(`RunWalker` now carries a `Meter`: answers are counted where they are given, every room reports its cost when
the walk leaves it, every combat turn reports its own): **10–14 ms per answer right after a checkpoint, and
200–295 ms four rooms later.** The constant part — rebuilding the initial run, every act's map included — is
under 13 ms, so the distance to the baseline was the whole cost.

The fix is in Core, in `InteractiveRunSession.Continue`: continuing past an interlude snapshots the run,
rebases the baseline onto a restore of that snapshot and empties the replay script. The interlude is the run's
one quiescent point, and `RunRunner.WalkGraph`'s resume arm continues *past* the node a save was taken at —
which is exactly what continuing means, so the rebase IS the answer and nothing is recorded. A run that cannot
be captured (a pending combat modifier whose body is not value-capturable) falls back to recording: slow,
never wrong. Same walk, same seed, **answer for answer identical**, and 28.2 s instead of 54.3 s to the same
room. Godot gets it for free — the session keeps its identity, so nothing subscribed to it changed.

**2 · `Make Amends` is a card a greedy player can play for ever, and that is correct.** It costs nothing and
puts a fresh copy of itself back in your hand while anything is still owed — deliberately, so that a payment
which could not go through (an empty purse, the Juniper's injunction against coin) still leaves a way to try
again. A human ends the turn. The walker had no reason to: the returned copy is a NEW instance, so refusing
the instance does not help, and the play is not *refused* — it simply achieves nothing. It span at 100 % CPU
inside one turn of the Great Toll Frog, which is what had been read as "the replay latency" for a week.

The walker now reads the table either side of a play — energy, both healths, both status counts AND their
stacks, hand, draw and discard — and stops offering a card whose play moved none of it, **by definition id**,
for the rest of that turn. Two details cost an attempt each:

- **the exhaust pile must not be in the reading.** A card that burns itself and returns a copy grows that pile
  every time, so counting it makes exactly this loop look busy for ever;
- **the reading has to be taken where nothing is pending.** `Make Amends` parks halfway through its own
  resolution to ask which way you are paying, so a reading taken the moment `PlayCard` returns straddles an
  open question and always differs. The judgement now happens at the next quiescent point instead.

Behind it sits a backstop: fifty plays in one turn is not a turn anybody makes, so it ends the walk with a
note rather than spinning.

**The result: `--playtest` walks the whole game.** `ok seed 20260801: Victory, 73 rooms over 3/3 acts` — the
first time a run has ever been played from the first room of the city to the last of the Green Docket.
`WholeRunTests` is no longer bounded to two acts.

## The same lesson, one repo over: the Godot smoke marathon (2026-08-28)

With the walk fixed and Act III finally in the exported document, `godot --headless -- --smoke-marathon`
still reported `result=Ongoing acts=2`. The content was not at fault — the probe was, in three ways worth
keeping written down, because all three are traps the *next* headless player will fall into too.

**It did not say why it stopped.** Every exit from the marathon's loop looked the same from outside:
`Ongoing`, no error. Each `break` now sets a reason and the summary prints it together with the room and the
encounter — `stopped because the step limit ran out at act 2 r7c1 (archives_elite_after_hours_return_bell)`.
That one line turned a guessing game into a diagnosis.

**Its greedy player had none of the walker's guards.** It played the first affordable card in hand, for ever
— never noticing a play the engine had *refused*, never noticing a play that moved nothing, and with no
ceiling on a turn or a fight. It had crossed two acts on luck. It now carries the same three rules the walker
learned above, `TableState` reading included.

**★ And the guards must not key on `driver.Current`.** This is the one that cost a whole run to find. Under
the replay model the fight is rebuilt from the blueprint on *every answer*, so `ReferenceEquals(combat,
fight)` announces a brand-new fight at every step — which reset the turn counter, the play counter, the
refused set and the barren set each time, leaving the guards in place and completely inert. A fight begins
when the driver has one and ends when it does not; turns are counted where the probe itself ends them. Card
*instance* ids, by contrast, are stable across replays — the rebuild is deterministic — which is why keying
`refused` on them works and keying anything on object identity does not.

Result: `smoke-marathon: result=Victory acts=3 rooms=73 error=none`, 242 s over 2534 answers, and the
per-answer cost stays flat across the three acts (72 → 87 → 113 ms) instead of climbing with the run.

One unrelated fix fell out of it: `CardVisuals.Back` called `VideoStreamPlayer.Play()` before the node was in
the tree, directly under an `Autoplay = true` that does precisely that on entry. The call was redundant and
printed `Condition "!is_inside_tree()" is true` with a fourteen-frame backtrace per card back — 283 error
lines in one marathon, which is how the verdict got buried in the first place.

## The boss that would not end, and the crash that was actually killing the walk (2026-08-28)

The Warden of Sealed Volumes had been carried for a week as the oldest open finding: *does not end within 100
turns, so a walk that draws him stops there*. He was never guilty. Nothing had ever drawn him — no walk, no
marathon — and the accusation was inference from a walk that stopped in Act II for another reason entirely.
Put on the table against the character's own starting deck, with the walker's greedy play, he goes down in
**25 turns**. Two walks now draw him and finish the game (`seed 20260901`, `seed 20260904`).

What was really stopping walks is worth the entry:

**★ A card that resolves the Queue can be put IN the Queue, and then it resolves itself for ever.** Night
Docket says "Resolve your oldest Queued card immediately". Skeleton Staff says "Queue a card from your hand"
— and does not ask what the card does. Queue Night Docket with it, and at the next turn start the Queue's
resolution window runs Night Docket, whose program opens a *nested* window; the card is still sitting in the
Queue, because a queued card leaves it only once its program has finished; so the nested window finds it and
starts it again. Not a slow fight, not a loop the walker's guards could see: a **stack overflow**, which
takes the whole process down with it and cannot be caught. Priority Docket and Customary Due are the same
door in Act III.

Bought as an engine seam rather than patched in content, because no rule an author writes should be able to
kill the process, and the trap is generic — any card that resolves the Queue is one "queue a card" away from
it, the Act IV Processional Calendar included. `CombatState` now knows which queued cards are mid-resolution
(`IsResolvingQueuedCard`), and `QueueResolution` passes over them: a card resolves once per window, and
"your oldest Queued card" means the oldest that is not the one asking. Night Docket keeps working exactly as
written from the hand, and a queued Night Docket now reaches the card *below* it — once. Pinned by
`QueueTortureTests` (RogueDeck-Core).

**The instrument had two faults of its own.** A walk's turn line reported only its timing, so a fight at turn
80 losing 6 HP a turn and a fight stuck since turn 12 looked identical — which is precisely how the Warden
came to be blamed. It now names who is still standing and on how much health. And `--playtest n --seed s`
seeds *two* things, the game and the first walk (`s`, `s+1`, …), so a walk reported as "seed 20260909" is not
reproducible by `--seed 20260909`: that builds a different game. The header now says which game is being
walked.

**★ And a body at zero was not down unless DAMAGE put it there.** With the crash gone, one walk in ten still
stopped at `a fight did not end in 100 turns` — and the walk's new turn line said why in one word:
`grandmother_clause 0/350`, for eighty-eight turns. She pays 5 HP for every courtesy the player keeps, and
that payment is a `SetHealthNode`, not damage; only `DamageDealt` downed a combatant, so paying her last five
left her standing at zero, unremovable, in a fight that could not end. Content writes "loses N HP" that way
in **nineteen files** — every one of them was the same trap.

Fixed in the engine after weighing it against the content fix, because the alternative was the same three
lines repeated at twenty-one sites and remembered again in Acts IV and V. `SetHealthEffectHandler` now
enqueues the Downed request when it empties a pool, whatever emptied it. This REVERSES a documented decision:
`SetHealthTests` used to say in its header that "setting HP to 0 here does not down the combatant" and pinned
it in a test named for it. The header and the test now say the opposite, and the reason is that alive-at-zero
is a state nothing else in the engine can act on. Of 3110 tests across the four suites, exactly one depended
on the old reading. Setting health UP is untouched, which is what the revival pattern needs.

**And the fix for a finding like this is a net, not a probe.** `Tests/BossLengthTests.cs` fights all fifteen
bosses of Acts I–III with the walker's greedy player, the starting deck and an unkillable tester, and gives
each a 40-turn budget. A walk only ever meets the boss its seed picked; every one of the fifteen is one seed
away from being that boss.

## The shop is a fixed shape, and most of the relics were unreachable (2026-08-29)

`BnB_Run_Systems_Master` §4.1 does not describe a shop, it fixes one: **3 General cards, 4 Character cards,
2 Shop relics, 2 Normal relics**, plus the removal desk. What was built showed **5 cards from one mixed pool
and 2 relics drawn from every relic whose rarity was not "boss"**, which is a different shop wearing the same
name. Three things were wrong under that, in rising order of size.

- **The card shelf had no pools.** One bag of five cannot be 3 + 4, and the split is not cosmetic: §4.2 says a
  future character replaces the four Character slots and *inherits* the three General ones. A mixed shelf hands
  the next character the Bureaucrat's cards. The pool a card belongs to is not a field on the card — it is
  which design sheet it was written on, which is the file it lives in — so `FinalCards` composes
  `GeneralPool`/`CharacterPool` from the `General*`/`Bureaucrat*` classes, and `ActIVCards`, which holds both,
  says which half is which.

- **Event relics were on sale.** §2.5 and §2.6 say an Event or Boss relic never enters Treasure or Shop
  generation. The shelf filtered on `Rarity != "boss"` alone, so every Event relic — each of them the payoff of
  one named branch — could be bought over the counter. The fix is not a longer filter: the four pools are
  already separate by construction (`RelicAuthoring.Pool`), so the shelves now draw from
  `FinalRelics.Pool(Shop)` and `Pool(Normal)` and there is no filter to forget.

- ★ **And the shelf was drawing from the wrong world entirely.** Its pool was `ConversionPools.Relics` — the
  *ported* v1 relics — while the 50 authored Normal relics and 24 authored Shop relics sat in the exported
  document with nothing able to hand any of them out. Only **2 of those 74 ids** also exist in the ported data
  (`archive_key`, `emergency_inkwell`), which is the only reason the number is not 74. So **72 authored relics
  had never once been obtainable in a run**, and no test noticed, because a shop full of valid relics looks
  exactly like a shop full of the right ones. The shop reaches both final pools now. **Treasure and the
  combat/elite relic rewards still draw the ported pool** (`pools.RelicGrantSource`) — that is §3.3, a
  different node, and still open.

Two smaller consequences of there being four shelves rather than two. A relic that names a shelf now has to
mean it: Crooked Display Case says "one additional **Normal** Relic", so it grants on the normal shelf alone,
and Turnover Bell says "all unsold **cards**", so it restocks both card shelves. And a Shop relic has no
Common/Uncommon/Rare — its pool is its rarity — so it is priced at 190, which is what an unlabelled relic
already cost on that shelf; §4.5 declares prices balance variables, so nothing else moved.

`Tests/ShopShelfTests` pins both halves of the promise, and pins them differently: the COUNTS are checked by
drawing the shelf the way a visit does, because a count is a property of the display; the POOLS are checked
against the authored offers, because a wrong pool is a wrong shop even on the draw where it does not surface.

## Nine Shop relics asked the event a question after it had left (2026-08-29)

Opening the shop to the Shop pool made eight relics reachable that had never once fired, and the first walk
through the new shelf died on one of them: `InvalidOperationException: Event field 'combat.heroHpRemaining'
was evaluated without a matching event in context`.

A declarative run program is evaluated in **two moments**, not one. Its condition — and any effect TEMPLATE —
is built at *dispatch*, while the event that woke it is still in scope. A plain effect handed to the same
trigger is wrapped as a literal, queued, and drained *afterwards*, when the event is gone. So

```csharp
RunPrograms.When<CombatResolvedRunEvent>(WonAnElite,
    new ComputedResourceRunEffect(Gold, RunExpr.Min(RunEventValues.ShopCurrencyPaid, …)))
```

reads correctly, compiles, and serializes — and throws the moment it fires, because by then nobody is left to
answer. Nine programs across eight relics had it: Bounty Hook, Wastebroker's Permit, Filing-Fee Stamp,
Debtor's Signet, Notary's Waiver, Priority Window Pass, Warranty Tag, Indemnity Stamp and Departmental
Purchase Order — every one of them a Shop relic, which is exactly why none of it had ever surfaced.

All nine are templates now (`RunEffectTemplates.GainResource` / `.ChangeCounter`), except Bounty Hook, whose
question is a *branch* rather than an amount: "20 Gold, or 35 below half HP" is two triggers with exhaustive,
mutually exclusive conditions, because a condition is allowed to read the event and a queued
`ConditionalRunEffect` is not.

Two things came out of it that outlive the bug:

- **`RunEffectTemplates.ChangeCounter` (engine).** The counter twin of `GainResource` was simply missing, so
  "record half of the Gold you just paid" had no data shape at all — the two counter cases could not have been
  written correctly. `tests/RogueDeck.Run.Tests/DeclarativeProgramTests` now also pins the trap itself: a
  queued `ComputedResourceRunEffect` reading the event throws, and that test is the reason the templates exist.
- **`Tests/QueuedEffectTests` (content).** It walks every relic program and every installed program in the
  assembled document and fails on any event-reading expression buried inside a queued effect. Its second fact
  runs the detector against the shape Bounty Hook had, because a net nobody has seen catch anything is not
  evidence that the water is empty — the Warden's lesson, applied.

## The Brass Bookmark keeps one card, and the engine learned to hear a card arrive (2026-08-29)

The relic's line is "the first non-Junk card that enters your hand outside the normal draw step each turn gains
Retain until the start of your next turn." It used to retain the WHOLE hand for one turn, on the excuse that
retention was a property of a card definition. That excuse expired with the per-instance `RetainedCardMark`
seam: a mark can hold one COPY back where a definition flag holds every copy of that card.

What was still missing was the question, not the answer. Nothing could hear a card ARRIVE. Two events say it —
a card MOVED into a pile, and a card MADE in one — and neither was reachable from authored content: the
`CardMovedToZone` and `CardInstanceCreated` trigger contexts existed in Core with nothing that could bind a
status trigger to them. So the engine gained the two trigger events, a bearer filter each, and one expression
(`eventCardZone`) for the thing a move trigger must ask first: a card leaving your hand and a card arriving in
it are the same event, and every rule about one of them is wrong about the other. `TriggerEventCardInstance`
now answers in all of those contexts, so a rule can name the card the event is about rather than guess it back
out of a pile by position.

"Outside the normal draw step" then needs no clause of its own: drawing reports as a draw, not as a move, so a
drawn card never reaches the trigger at all. The relic is three triggers — one per way a card can arrive, and
one at the start of your turn that takes the mark off again, which is where "until your next turn" ends. Both
arrivals share the once-a-turn latch, so exactly one card is kept however it got there.

## What the player can see of a boss (2026-08-29)

Three of these are presentation decisions, and one is a rule about what may stay hidden. All four came out of
the same eyes-on pass: a boss's state is only state if it reaches the screen.

**Two counters were promoted; the rest stay counters, on purpose.** Some boss state is a counter rather than a
status, and a counter reaches no frontend: it has no name, no rules text, and no registry to look either up
in. Rendering counters generically was rejected — "curator_dial 2" is not readable and never could be — so the
player-facing ones become marker statuses in content and the frontend stays content-agnostic.

Two qualified, and both by the same test: **the player is asked to act on a number the game never showed
them.**

- **The Curator's dial** (`curator_dial`, 0 PRESENT / 1 FUTURE / 2 PAST). Its own marker's text says "its dial
  shows which hour it is working in", and it showed nothing. The dial is what the five telegraphed intent
  names MEAN — the same slot hits, files or reaches back depending on where it stands — so the telegraph
  without the dial is a name with no sense in it. It now wears one of three faces (`curator_dial_present` /
  `_future` / `_past`); the counter stays the arithmetic, `ShowTheDial()` runs in the same breath as
  `TurnTheDial()`, and the fight starts wearing the PRESENT face because the dial has not turned yet.
- **The Warden's announced key** (`warden_seal_type`, 1 Restraint / 2 Procedure / 3 Evidence). The
  announcement and the sealing are deliberately a turn apart: "Inspect the Claim" names the key, and the seal
  falls at the player's next draw. That turn in between is the one the design gives the player to plan in, and
  it was spent against a number nobody could see. Three markers now stand on the PLAYER (that is whose hand is
  reached into), applied and cleared wherever the counter is written.

Everything else stays a counter, and the rule is: **a counter may stay invisible when what it holds is already
on the screen, or when showing it would be a promise the fight does not keep.** By family:

- **Spent latches** (`*_spent`, `*_used`, `*_this_turn`, `*_due`, `catalogue_entry_used`). They only stop a
  thing happening twice. Either it has happened, and its effect is visible, or it has not.
- **Records of the player's own turn** (`curator_activity` / `_opening` / `_compliance` / `_force`,
  `catalogue_record_*`, `warden_played_*`, `seen_*`). They restate a turn the player just took. Where a boss
  ACTS on such a record, the acting is a status: the Catalogue's "Established Tempo — Busy" and its siblings
  are exactly the record made readable at the moment it starts to matter.
- **Scalars two rules must agree on** (`real_cards_dealt` — Act III's hand budget — `notary_ring`,
  `liability_*`). The hand budget is the clearest case: it exists because a turn's end puts the hand away
  before a rule about the turn's end can read it, and what it holds is *the hand the player is looking at*.
- **Rotations whose result is already telegraphed** (`warden_seal_rotation`, `gcr_rotation`). Which slot comes
  next is answered by the intent shown.
- **The Whispering Catalogue's beat** (`catalogue_beat`) is the one judged case, and it fails on the second
  half of the rule rather than the first. The beat picks which family the next prediction is read from — but
  it is bumped at the player's own turn end and again whenever the Catalogue reclassifies, so the value
  standing during the player's turn is never the one the Catalogue will read. A face on it would be off by one
  from the only question it could answer, and the prediction it produces arrives as a status the player reads
  and answers anyway.

**The phase is drawn on the intent, not filed after it.** Every phased boss rotates ONE intent list, so a slot
keeps its Phase-I name for the whole fight: the Warden still telegraphs "Inspect the Claim" while that slot
means the Phase-II thing now. That reads as a wrong label — and the one thing that makes it read as the boss
changing instead is the phase marker, which was one chip among a dozen, filed after the stacks and the
countdowns. The phase markers of all fifteen bosses (the phase a boss is IN and the telegraph that it is about
to change) are now tagged `phase` in the presentation manifest — `Converter/BossPhases.cs` — and the frontend
draws a tagged status as a banner directly above the intent instead of as a chip. Presentation is
engine-ignored, so this changes no rule; it decides where a true thing is written. The Curator's three dial
faces ride along, because the dial is read at the same moment and answers the same question about the same
line.

**A boss's relic arrives as a relic.** Its spoils are one bundle that opens two further picks, and both of
them read "a card reward", because the engine described any reward-that-opens-a-reward as a card. So a boss
announced its relic as a card, twice, under one heading — and the relic then arrived on a screen titled
"Your reward", exactly like the card pick before it. A reward that opens another one cannot know what is
inside (its source has not been generated yet, and generating it early would roll the run's dice), so the
offer now DECLARES its kind: `RewardKinds.Card` / `RewardKinds.Relic` on the nested offers, the engine
describing them from that, and the chooser asking under `reward-<kind>` so a frontend can title the screen
after what is actually being handed over. An offer that declares nothing keeps the plain word "reward".

**Five bodies fit on the screen.** The widest fights in the game are four enemies and the hero — the Grand
Cross-Reference (three volumes and the boss) and Act III's Ant Queen — and nothing had ever looked at one. The
enemy row was a fixed HBox of 200-wide columns, and a fixed row does not shrink: five columns and their gaps
simply walk off the right edge, taking a boss's health bar and intent with them. It is an `HFlowContainer`
now, so a crowd wraps to a second line; a one- or two-body fight is unchanged (one line, right-aligned).
`godot -- --smoke-crowd` walks to the widest fight it can reach, measures the row against the screen and says
whether anything is off it; `--smoke-boss <act>` stands in a named act's boss fight and captures that.

Two further faults came out of the same look, both about what a crowded column does to everything below it.
The combatant NAME did not wrap, so its full length was a minimum width no share-out could argue with — and a
body carrying a dozen statuses made its column taller than the arena, where a `VBoxContainer` hands out
minimum heights before it hands out the leftovers, so **the hand, the deck and the End-turn button were pushed
off the bottom of the screen** by the very state this pass is about making readable. Names wrap, the arena
scrolls, and a long chip list is bounded so it cannot push the enemy's intent below the fold either.

And two about the probes: a walk that never yields to the tree fills Godot's message queue and segfaults in the
second act, and the **marathon** — which never yielded either — reached its Victory line holding **14 GB** of
undeleted screens and was then killed by the OOM killer. Every answer rebuilds the screen out of fresh Control
nodes and frees the old ones with `QueueFree`, which is deferred: a walk that never lets the tree collect never
collects. Both yield one frame every twenty answers now, and a probe stops queueing the card-draw animation.

## Act IV — the five words, and the two the engine had to learn (2026-09-02)

The Licensing Labyrinth is written in five keywords no handed-over master ever defines: **Weighed ·
Burdened · Inscribed · Entombed · Embalmed**. All three masters cite an `act_iv_*.md` that is not in
`source-data/design/`. They were reconstructed from several hundred uses and **ratified by the user on
2026-08-29** (`ACT_IV_V_BUILD_PLAN.md` §"THE FIVE ACT-IV KEYWORDS"). What follows is how that ratification
became code, and every number in it that the masters left open.

**Two of the five needed the engine; three did not.** The seam list the plan carried was worked through
against RogueDeck-Core first, and only what was genuinely missing was bought:

- **What a turn has COST** did not exist. There was `resourceGainedThisTurn` and no mirror, so a measure had
  nothing to compare against. Bought as its symmetric twin: `CombatantCardPlayTurnStats.ResourceSpentThisTurn`,
  fed from the cost-payment event and read by a new `resourceSpentThisTurn` expression. It counts the cost
  ACTUALLY paid, after every modifier — which is what makes Burdened and Weighed one decision instead of two —
  and it sums every resource a cost names, exactly as its twin does. A card played for free adds nothing.
- **A status that ENLARGES the next application to its bearer** did not exist either. The engine could refuse
  an application from the receiving side (Act III's Safe-Conduct, a `StatusPreventionSpec`) but not magnify
  one. Bought as that spec's mirror, `StatusAmplificationSpec` + its interceptor: the next application lands
  larger and the amplifier is spent doing it. It runs AFTER prevention, so what is refused is never enlarged
  into existence; it never enlarges an application of itself; and one application is enlarged once however
  many stacks are held (the enlarged request carries a mark). It announces itself
  (`StatusApplicationAmplifiedCombatEvent` + a `StatusApplicationAmplified` trigger) with the polarity of what
  grew, because IV-13's Keeper of the Living Cartouche writes one glyph for an enlarged blessing and another
  for an enlarged curse, and that is the only place the two are distinguishable.
- **Burdened, Entombed and Embalmed compose** out of what was already there: a flat cost modifier plus a rule
  that hears the payment; the engine's Stun for one turn at a threshold; and a decay that asks whether the
  bearer is preserved. No feature was bought for them — only tests.

**Weighed is exact-spend, and what it leaves behind is a DISTANCE.** The requirement is the stack count, so
the player reads the demand off the status. It resolves at the end of the bearer's turn — the only moment the
question has an answer — and removes itself doing so. The record is one counter on the player,
`measure_result` = 1 + |spent − required|: 0 means no measure has ever been taken in this fight, 1 means
exact, 2 means off by one, 3 or more a major error. One counter rather than two because "was there a measure"
and "how far off" are always asked together, and a reader that had to check two could punish for a distance
belonging to no measure. It stays a counter and is not promoted to a marker status because what the player
must ACT on is the requirement, and that is already a status they can see.

**The record is the LAST completed measure, and it is not cleared.** A punishing intent reads whatever the
last measure came to. In a three-intent cycle that raises one measure and answers it once, the value a body
reads is always the one from its own cycle. Nothing yet needs "a measure resolved *this* turn"; §3.2's
observers at Stage 4 may, and that is where it would be added.

**§3.1's Primary Measure is one condition, not a scheduler.** A measure is raised only if none stands. So in
Encounter 3 whoever acts first that turn owns the check and the other body merely strikes — which is why the
Crooked Rod Bearer is listed FIRST in that encounter's roster, exactly as the design says it should establish
the measure. No second, contradictory Weighed can exist.

**Burdened is a flat surcharge, not a per-stack one.** The masters never price it. Per stack, three stacks
would price a whole hand out of a turn; flat, Burdened X reads "the next X cards you play cost 1 more each",
and the stack is worked off by that surcharge being PAID. Payment is the operative word: a play that ends up
costing nothing works nothing off. The payment is also counted (`burden_paid`), because IV-15's Colossus asks
whether a burden was paid off rather than merely lost — a cleanse takes stacks too.

**Entombed buries at five and resets, and it is read at the bearer's TURN START.** A turn can only be lost
before it is had, so a fifth stack applied during the player's own turn takes the next one. The burial is the
engine's Stun for one turn; five stacks are spent, so the cycle can build again.

**Embalmed spends itself per fade.** The ratified text says preservation prevents "natural decay". In this
game almost nothing decays by duration — fading is authored, one stack at a turn or round boundary (Panic,
Poison, Fatigue, Ward Wax) — so preservation is written at the one place fading happens
(`ActFour.Fade`, which every one of those now goes through): if the bearer is preserved, one Embalmed is
spent instead and the stack stays. That makes Embalmed X read "the next X fades on this character do not
happen", needs no ordering agreement between two turn-end triggers, and gives the same answer whichever
status was about to shrink. A stack spent, cleansed or paid away is not a fade and is untouched.

**Stage 1 numbers, where the appendix gave bands:** Reed-Cord Surveyor 85 HP (band 80–90), Crooked Rod Bearer
88 (84–94); Set the Measure 10 damage, Reed Lash 14, Re-Tension Cord 16 Block; Crooked Measure 11, Rod Strike
15, Brace the Standard 17 Block. The Surveyor's error bands are the appendix's: exact passes, one step away is
1 Paperwork, two or more is 2. The Bearer files 1 Paperwork for any failure and does not care how far off it
was — that difference between the two officials IS the stage.

**The Surveyor asks for exactly 2.** The masters call the value balance-tunable and the elite master requires
a solvability filter (never an impossible requirement); that filter is elite machinery written once at IV-12.
A standard body in the act's opening stage asks for a constant that three Energy can always meet. The Crooked
Rod Bearer's standard alternates 1 → 3 on a counter kept on the body itself, advanced only when a measure was
actually raised — so a turn spent measuring against somebody else's standard does not silently skip a step
and break the sequence the player is being taught to read.

**Act IV's bodies load; Act IV is not yet a room the run walks.** The act's enemies and encounters are in the
blueprint from this step on, so probes can fight them, but the act joins the walked run only at IV-24, when it
has bosses to end on. The ported v2 Act-IV demo pool (42 enemies / 41 encounters of pure damage-and-block) was
replaced rather than kept beside the authored one, exactly as Acts II and III replaced theirs.

**A finding that outlived its step: a stunned player could play their whole hand.** Card-play validators —
Stun, the one-attack-per-turn limit, the unplayable-card refusal — were consulted only on the strict
processor path used by scripted scenarios and tests. The interactive host, the playtest walker and Godot all
play through `PlayCardEffectRequest`, which never asked them, and the message that path already prints
("unaffordable or rejected by a validator") shows this was always meant to. Fixed in Core: the effect path
asks the same validators and no-ops on refusal, so the card stays in hand, nothing is paid and the session
stands. Nothing in Acts I–III used stun, which is why no test had ever asked. **A rule that is only enforced
on the road the game is not played on is not enforced.**

## Act IV, Stage 2 — the Gate of Counted Names (2026-09-02)

The stage's job is that the player cannot learn only half of Inscribed, so its three bodies read the register
three different ways — as a state, as the amplifier, and as something to steal from.

**The Cobra of the Entry Mark has no code at all.** It marks the player, and then the register makes its venom
land larger by itself. Its whole authored existence is three JSON intents. That is the vocabulary working as
designed, and it is the reason the amplification was bought as an engine seam rather than written into each
body that wants it.

**"The modification is telegraphed before resolution" is the register's own status text.** The master asks the
Cobra to announce that the next application will be stronger. There is no per-intent telegraph for it: an
intent's telegraph is generated from its authored JSON actions, so a program that changed the number would
make the telegraph lie. What the player sees instead is Inscribed on their own bar, whose description states
the rule exactly ("the next status applied to you lands with 1 more stack"), plus the incoming intent's plain
"1 Poison". Both facts are on screen before the enemy acts, which is what telegraphing is for.

**The Pilgrim's defensive benefit is a passive on a visible marker, not a conditional inside its intents.**
The appendix ties the benefit to the shelter intent ("16–20 Block while Uncounted"); a shelter that blocks 18
while telegraphing 18 in one state and 0 in the other would be a lying telegraph, and the state the player is
being asked to change would be invisible. So `Uncounted` is a status the player can see, worth 30 % less
attack damage taken, and the shelter blocks what it says it blocks in both states.

**The count watches five events, and the important one is EXPIRY.** A status losing its LAST stack is reported
as an expiry, not as a stack change or a removal — and running out is the commonest way a player leaves the
register, because the last stack goes by being spent on an amplification. A count that watched only
applied/merged/changed/removed showed the Pilgrim as Counted for the rest of the fight. It also recounts at
every turn start from nothing at all, which is what settles the state for a player who walks in already
inscribed: no status event ever fires for that, and a **fight's first round starts before its bodies are
dressed**, so a round-start hook fires for nobody. **Merke: a rule nobody wears yet does not fire.**

**Stolen Name is spent the moment the second name is stolen.** The master says maximum 2, and at 2 the next
negative application gets +1. Rather than keep a full jar and enlarge later, the Baboon puts the forgery on
the player's file at once — so the player sees what is coming and can act on it — and the names reset. A
consequence worth knowing: the Stolen Name status never displays 2.

**The forgery is an amplification of the Baboon's own**, scoped to debuffs. That is what makes "+1 stack to
the next negative application" expressible at all, and it is also why the loop guard the master demands
(§3.4: a copy may never start another copy chain) is answerable: the amplification event names WHICH status
paid, so a magnification the forgery caused is visibly not the register working. Two small engine reads were
bought for it, both general: `eventStatusPolarityIs` ("was the thing that just happened a debuff?" — the
Royal Genealogy Wall at Stage 15 needs the same question with Buff) and `eventAmplifierIs` ("what paid for
this enlargement?"). Encounter triggers can now also hear amplification, prevention and resolved actions,
which they could not before.

**"By another enemy" is asked by status, not by identity.** A program cannot compare two combatants, so the
Baboon asks whether whoever applied the enlarged status wears the Baboon's own rule. One Baboon or three, the
answer is the same, and its own Doubt never feeds it.

**Stage 2 numbers, where the appendix gave bands:** Uncounted Pilgrim 92 HP (86–98), Cobra of the Entry Mark
94 (88–100), Name-Eating Baboon 90 (84–96); Petition Entry 11 damage + 1 Inscribed, Uncounted Blow 15,
Unregistered Shelter 18 Block; Entry Mark 10 + 1 Inscribed, Entry Venom 13 + 1 Poison, Coiled Seal 17 Block;
Steal the Name 9 + 1 Inscribed, False Credential 12 + 1 Doubt, Scramble the Gate 18 Block. Uncounted is worth
30 % less attack damage (the master says only "a defensive benefit").

**A test lesson, paid for twice: an interactive fight is a REPLAY.** Poking a status into the live combat
state between answers looks like it works — and is thrown away by the next answer, which replays the fight
from its baseline. Anything a probe needs on the table has to be a STARTING status
(`FightProbe.SoloAgainstHero`, and now `FightProbe.RosterAgainstHero` for multi-body probes) or something the
fight itself produces.

## Act IV, Stage 3 — the Granary Courts (2026-09-02)

The stage where the act's two economic words are pushed into each other on purpose. **It needed nothing new
from the engine** — the first Act-IV stage that is pure content, which is what the vocabulary was bought for.

**The short measure asks for the whole turn (3).** The master calls the Crocodile's standard "deliberately
unfair" and leaves the number open. Three is exactly meetable with an unburdened hand of one-cost Deeds and
awkward with a burdened one — the Crocodile's own other jaw is what makes its own demand unmeetable, which is
the design's stated purpose ("the imposed burden sabotages the player's ability to meet the official
measure"). No solvability filter is applied: that is elite machinery (§6.2), and a standard body is allowed
to ask for something the turn cannot afford.

**Snap at the Deficit reads the deficit.** The appendix lists it as flat damage in the *signature* column, and
a signature intent named for the deficit that ignores it is a wasted beat — but a program that changed its
damage would make the telegraph lie, because an intent's telegraph is generated from its authored JSON. So
the bite is always the 19 it says, and what the deficit changes is whether one more burden comes with it. A
measure met exactly is bitten just the same: the bite is not the punishment, the burden is.

**"At least one unblocked HP hit" is read as flesh before and after.** The Swarm's three hits are dealt, and
the seal is attached only if the player's health actually went down. Counting damage EVENTS would count the
hits that Block ate; watching the player's Block would be wrong the moment something else on the field spent
it. One seal per swarm, however many of the three got through.

**The Thief takes its cut at its own turn start, not on each payment.** The tax already writes down how many
surcharges have been paid (`burden_paid`, from IV-0); the Thief keeps a bookmark in that tally
(`rations_collected`) and takes the difference when it comes round. That is ordering-free — no agreement is
needed about whether the Thief's rule or the tax's own rule fires first on a payment — and it is the master's
"once per card played" for nothing, because the tax writes one payment per card. It also suits the fiction: a
thief collects its cut, it does not follow you around the office.

**The feast heals ON TOP of the bite rather than instead of it.** The appendix offers "16–20 dmg *or* heal
4–6 at threshold"; an intent that sometimes deals its telegraphed damage and sometimes deals none would be a
lying telegraph. So Feast on Rations always bites for 18, and three rations buy 5 healing on top and are
eaten doing it.

**Stage 3 numbers, where the appendix gave bands:** Crocodile of the Short Measure 100 HP (94–108), Jar-Seal
Scarab Swarm 94 (88–100), Hungry Grain Thief 96 (90–102); Short Measure 12 damage + the measure of 3, Load the
Scale 11 + 1 Burdened, Snap at the Deficit 19; Seal Swarm 3 × 4, Scuttle 11, Seal the Jar 18 Block; Sack
Weight 11 + 1 Burdened, Feast on Rations 18 (+5 healing at 3 Rations), Hide in the Granary 18 Block. The
Thief's threshold and reward are the "balance-tunable" the master left open.

## Act IV, Stage 4 — the Floodmark Basins (2026-09-02)

Where a missed measure stops being an embarrassment and becomes a burial, and where the audit's §3.2 lands: a
body may answer a measure it never demanded. **Like Stage 3 it needed nothing new from the engine.**

**"Once per Weighed resolution" is arithmetic, not a latch.** The record the measure leaves (`measure_result`)
says what the LAST one came to and is never cleared, so a body that punished off it would punish the same
failure again every time it looked. So the resolution also keeps two tallies that only ever grow —
`measures_met` and `measures_failed` — and every body that answers resolutions keeps its own bookmark in one
of them, reads the difference at its own turn start, and moves the bookmark up. Several bodies can listen to
the same measure in any order, each answering exactly the resolutions it has not answered yet; a body that
joins late or dies and is replaced takes its own share and no more. It is the same idiom the Hungry Grain
Thief eats surcharges by, and it is now written once (`ActFour.SinceLastLooked` / `MoveTheBookmark`).

**The Reader's consequence is the passive, not the intent.** The master states it as a passive ("when the
player fails a Weighed requirement: apply 1 Entombed"); the appendix hangs the same cost off the `Levee Notes`
budget line. The passive wins: the Reader answers every resolution wherever the demand came from, which is
what makes Encounter 15 and the cross-stage case work at all. The appendix line is read as where the cost
sits in its budget.

**The Scribe's threshold is a status on the body, because that is how this game telegraphs.** An intent's
telegraph is a fixed string built from its authored JSON (`EnemyMapper.Label`), so no intent can announce a
conditional number. The threshold is therefore a marker the player can see appear on the Scribe (`Silted
Record`), whose text states the rule, and the extra stack rides on the engine's outgoing-application scaling
rather than being written into one intent. Same shape as the Uncounted Pilgrim, and the watcher is now one
shared helper (`ActFour.FollowTheApplicant`) carrying both lessons Stage 2 paid for: **watch the EXPIRY**, and
settle the state at every TURN start.

**The Scribe's second intent applies Entombed**, which the appendix's bare "16–20 dmg" does not say — but
Encounter 13's own note requires the solo to be self-sufficient ("its own move set can apply Entombed before
Silted Record becomes relevant"), and nothing else in its kit could bury anybody. `Drowned Record` is
therefore 18 damage + 1 Entombed.

**The Farmer arrives with the water already standing.** The master asks for "a visible Flood countdown" from
the start; a counter at zero shows nothing, so the Farmer opens with 1 of 3 marks — its own field was buried
by this same flood — and two missed measures finish what is already begun. The water rises by ONE mark per
cycle however many measures were missed in it (a flood is a clock, not a tally), and meeting the furrow does
not lower it: nothing in this act gives anything back. It holds where it stands, which is exactly the
design's "correct measurement can delay the burial".

**Stage 4 numbers, where the appendix gave bands:** Flood-Mark Reader 105 HP (98–112), Drowned Field Scribe
106 (100–114), Silt-Buried Farmer Shade 109 (102–116); Read the High Mark 12 damage + a measure of 2, Silt
Lash 17, Levee Notes 19 Block; Silted Filing 13 + 1 Paperwork, Drowned Record 18 + 1 Entombed, Mud Ledger 20
Block; Keep the Furrow 11 + a measure of 2, Mud Pull 14 + 1 Entombed, Raise the Bank 20 Block. The Silted
Record threshold is 3 Entombed of the 5 that bury (the master says only "a visible Entombed threshold"), and
the Flood is 3 marks.

**A consequence worth knowing:** the burial spends the Entombed that caused it, so the Scribe's ink thins in
the same breath as the player loses the turn. Being buried is the moment the silt drains.

## Act IV, Stage 5 — the Tribute Causeway (2026-09-02)

Stage 4 asked what a missed measure costs. This one asks what a MET one costs, and the answer is the act's
whole joke: "The tribute was correct. Processing was not included." **Nothing new from the engine again** —
the third content-only stage in a row.

**Two acts may share a word without sharing a rule.** Act III's Court already owns a status called `tally`
(the Keeper of Buried Names'), so the Donkey's resource is `donkey_tally` with the same display name. The
converter fails loudly on a duplicate status id, which is how this was caught — the id space is one, the
vocabulary is per act.

**The Donkey counts RESOLUTIONS, not failures.** "It was entered three times" — met or missed, an entry is an
entry, which is why it reads both tallies through one bookmark (`ActFour.ResolutionsSinceLastLooked`). What
being right changes is what the third entry weighs: 1 Burdened instead of 2, read off `measure_result` (1 is
exact). Being counted correctly is still being counted.

**The player's hand had to be counted while it still existed.** The Empty-Handed Envoy reads "the player
ended the turn with nothing left in hand", and a rule at turn end cannot see the hand: the engine discards it
before turn-end triggers run (the house rule from Act III). So the count is taken AS THE TURN HAPPENS — on
`CardsDrawn`, and on `ActionResolved` after each action the player finishes — and written to a counter the
Envoy reads afterwards.

Two things in that are load-bearing, and both were paid for:
- **ActionResolved, not CardPlayed.** A card is still in the hand while its own play resolves, so a
  CardPlayed recount is one too high. ActionResolved closes after the card has been placed.
- **Only when the PLAYER is the actor.** ActionResolved fires for enemies too, and an enemy acts during the
  enemy turn — by which time the player's hand has been discarded. Without that gate every turn read as
  empty-handed, which is exactly what the first run of the tests reported.

**The Envoy's "defensive layer" is an exposure that lasts the player's turn.** The appendix says a successful
empty-hand measure "removes 14–18 Block / defensive layer"; stripping Block at the Envoy's own turn start
would take nothing, because a combatant's Block is cleared at its own turn start anyway. So it is a visible
debuff instead — `Presented in Full`, +50 % damage taken — applied when the Envoy reads the measure and
removed at its next turn start, which is precisely the window the player can use it in.

**Stage 5 numbers, where the appendix gave bands:** Foreign Tribute Shade 115 HP (108–122), Donkey of the
Third Tally 119 (112–126), Empty-Handed Envoy 102 (96–108); Assess Tribute 13 damage + a measure of 2,
Foreign Levy 19, Seal Receipt 20 Block; Tally Kick 14, Load Register 11 + 1 Burdened, Brace the Load 20 Block;
Diplomatic Rebuke 15, Empty Palm 19 Block. The Shade files 1 Paperwork per round in which a measure was met;
the third entry weighs 2 Burdened, or 1 if that third measure was met.

**The Envoy is never fielded alone**, as the master requires ("intentionally combination-first"): it
interprets somebody else's measure, so it appears only in Encounter 18 beside the Shade — and it has two
intents rather than three, because it has no measure of its own to raise.

## Act IV, Stage 6 — the Corvée Yards (2026-09-02)

Compulsory labour, and the three things it does to the people in it. **Nothing new from the engine** — the
fourth content-only stage — but one thing the port had never written down.

**Fatigue now records that it actually took something.** The Rope-Gang Wraith answers "when Fatigue actually
removes Energy from the player", and that is not the same fact as "the player has Fatigue": a bearer with no
pool to refill into loses nothing. Losing a resource raises no event content can hear, so Fatigue writes the
moment down itself (`energy_taken_by_fatigue`), exactly as Burdened writes down its surcharges, and the gang
keeps a bookmark in it.

★ **And WHERE that question is asked cost this step its one bug.** The first version guarded the loss with
"does the bearer have any Energy right now?" — and broke a three-act-old Minute Moth test, which is how it was
caught. **The turn-start refill is an enqueued effect like every other**, so a program that reads the pool
before its own loss resolves is looking at LAST turn's leftovers, not at the refilled pool: a player who spent
everything reads as having nothing, and the bite that is about to land reads as no bite at all. The question
therefore stands BEHIND the loss in a causal sequence (a causal step waits for the queue to drain, which is
what makes before-and-after readings work at all), and it asks whether the pool is now less than FULL —
because the refill has just filled it. Both halves are load-bearing.

A consequence worth knowing, and the reason the test for the negative case looks artificial: since Energy
refills before Fatigue bites, a player who merely spent everything last turn still has something to lose. The
only way to have nothing taken is to have no Energy to refill into.

**Rope Snap's bonus is telegraphed as a formula, not hidden in a program.** The port's intent labels are built
from the authored JSON, and the DSL has a scaling shape for exactly this (`damage_per_status` with a base, a
per-stack bonus and a cap), so the telegraph reads "20 dmg +6 per Work Strain (max +6)" and tells the whole
truth. The intent's program repeats that arithmetic because the strain has to be SPENT in the same breath;
the numbers live in two places on purpose, and the test pins both. Same shape for the Ushabti's Stone Blow
("19 dmg +3 per Stone (max +9)").

**"The player removes all Block from another enemy that had Block" is read as a before and after.** No damage
bookkeeping: the Laborer records what the rest of the gang had standing when the player's turn BEGAN, and
asks at the player's turn end whether it is gone. A combatant's Block is cleared at its own turn start, so a
brace that vanished during your turn is a brace you broke — and the rule is once per player turn by
construction rather than by a latch.

**The Runaway Laborer leaves by being downed.** "Leaves combat; this counts as resolved for encounter
completion" — the engine has no third state between fighting and down, and downing is exactly what makes the
room resolve. The fiction is carried by the status the player watches climb (`Escape`) and by the fact that
nothing is rewarded for it beyond the room ending.

**Stage 6 numbers, where the appendix gave bands:** Rope-Gang Wraith 120 HP (112–128), Runaway Laborer 102
(96–108, and its HP is deliberately NOT inflated to compensate for the escape), Stone-Hauler Ushabti 128
(120–136); Keep the Rhythm 14 damage + 1 Fatigue, Rope Snap 20 (+6 strained), Pull Together 22 Block;
Desperate Swing 13, Hide Behind the Gang 18 Block; Haul Stone 15 + 1 Burdened, Stone Blow 19 (+3 per Stone,
cap +9), Brace the Load 25 Block. Two Escapes buy the conscript its freedom.

**The Ushabti does not spend its stones**, which is what the appendix's cap is for: the blow carries them,
the bracing is its own intent, and the rotation keeps a regular Block generator so Encounter 21 has something
to break.

## Act IV, Stages 7 and 8 — the Monument Works and the Hall of Reed and Ink (2026-09-02)

Two stages of one idea: the building remembers, and so does the ink. **Nothing new from the engine.**

**The Capstone's Placement is a status and a rule, not a rotation.** The master asks for a "visible Placement
sequence" whose completion brings the stone down. So the Golem's ordinary actions each add a stack of
`Placement`, and an intent rule (`self_status`, min 3) swaps in `Set the Capstone`, which spends the whole
placement. The stone therefore falls every fourth turn with the count visible on the body for three of them —
and the drop weighs what the player is already carrying (+4 per Entombed, capped at +12, telegraphed as the
formula).

A consequence the test pins: **five Entombed take the turn before the stone can fall on it**, and spend
themselves doing it — so the heaviest capstone in practice is the one that lands at four. The act's two
burial clocks meet there, and they do not stack.

**Kept Oaths strike Broken Oaths off the record.** The master gives the Oath-Stone two tokens (kept and
broken) and says only that kept oaths "weaken selected later actions", with the appendix pricing it as "may
reduce a later hit by 3–5". A hammer that swings by the record cannot be telegraphed off two opposing
statuses at once — the intent label carries ONE scaling term — so compliance is recorded against the fault:
a kept oath removes a broken one, which is exactly a later hit reduced by 4. The Kept Oath token stays
visible as the ledger of compliance (Stage 17's Oathbound Gate is this same identity, and will want it).

**Only one oath a round** falls out of the bookmark idiom rather than a latch: the stone looks once, at its
own turn start, at the resolutions it has not read yet.

**Fresh Pigment is a passive on the applying side, spent by the scribe's OWN entry.** An
outgoing-application modifier reads the applier's statuses, so the palette thickens only what this body
writes into the register; the rule that spends it therefore also asks whether the applier is the scribe (in a
status-application trigger, `source` is whoever applied it). Another body writing into the register leaves
the palette intact — which the test pins, because that is the half a naive listener gets wrong.

A consequence worth knowing: **the palette's consumption is not observable from outside a round**, because a
fresh one is ground at every round start and no authored body writes into the register twice in one round.
The tests pin what is observable — two stacks per round, and an untouched palette when somebody else writes.

**The Complaint Wall is §3.5 in one intent.** A solo body whose signature needs Embalmed must be able to
create it itself, so `Preserve the Complaint` applies both halves: 1 Panic (which would fade at the player's
turn end) and 1 Embalmed (which stops it). The complaint itself is fed from the one fading point — `Fade` now
records a preserved AFFLICTION on its bearer (`decays_preserved`), and the wall keeps a bookmark in it. Ward
Wax being held is not a grievance, so the recording is negative-only.

**Stage 7 and 8 numbers, where the appendix gave bands:** Fallen Capstone Golem 145 HP (136–154), Cornerstone
Oath-Stone 137 (128–146), Palette-Bearing Apprentice 119 (112–126), Hieroglyphic Complaint Wall 150
(142–160); Falling Dust 15 + 1 Entombed + 1 Placement, Set Support 27 Block + 1 Placement, Set the Capstone 25
(+4 per Entombed, cap +12); Foundation Measure 13 + a measure of 2, Broken-Oath Smash 20 (+4 per Broken Oath,
cap +12), Foundation Wall 27 Block; Fresh Pigment 12 + 1 Inscribed, Brush Stroke 17, Palette Guard 20 Block;
Preserve the Complaint 11 + 1 Panic + 1 Embalmed, Carved Accusation 18 (+2 per Complaint, cap +8) + 1
Paperwork, Stone Defense 31 Block.

## Act IV, Stages 9 and 10 — the Royal Seal and the Processional Galleries (2026-09-02)

Three bodies that do nothing to the player directly and change everything about what the others do: one
authorises, one counterfeits, one legitimises. **This is where the last row of the seam list was bought**
(§3.3 + §3.4), and where two engine findings came out of it.

**`Replicated` is a mark on the application, exactly like `Amplified`.** An `ApplyStatusEffectRequest` can now
say it is a COPY, and the mark rides as far as the applied/merged event — which is the only place a rule can
see it. A copy is an ordinary application in every other way: it lands, it is refused or enlarged like any
other, and rules may answer it (the Kneeling Petitioners deliberately do). What it must never do is start
another copy chain or count as the ORIGINAL a chain is measured from, and both are one question
(`eventIsReplicated`) asked before copying.

**A rule can now answer an application with an application of the same thing** (`ApplyTriggerEventStatusNode`,
`node.applyTriggerEventStatus`). No amount of content could express that: a program had no way to name a
status it only learns at fire time. It is the node the False-Seal Forger is, and the Sun-Seal Bearer uses it
too.

★ **A merge now names the body that just applied it, not the instance's owner.** Found by the Forger:
`StatusMergedCombatEvent` reported `existingStatus.SourceCombatantId`, so every rule that asks "did somebody
ELSE just apply something?" got the wrong body the moment the status was already on the player — which is
most of the time. "Who did this to me?" and "whose status is this?" are different questions, and an event
answers the first; the instance keeps its own source untouched for rules about standing (Act III's
source-bound Trespass reads the STATUS, not the event). Fixed in Core with a test on both halves.

**The Sun-Seal's +1 arrives as a second, marked application rather than by enlarging the first.** By the time
anything can answer an application it has already landed, so "gains +1 stack" is authored as one more stack of
the same status — and marked a copy, so the Forger standing beside it cannot counterfeit the authorization as
if it were the original (§3.3: a replicated application never becomes the round's original). Encounter 30's
order therefore comes out as 1 original + 1 authorised + 1 forged = 3 stacks, and no cascade.

★ **A body's Block lives from its own turn until its next turn start**, and that decides the roster order of
every encounter on these two stages. The seal can only authorise while its impression is intact, and the
procession's bracing is swept away by the braced body's own turn start — so the support body acts FIRST in
all five authored encounters (Bearer before Cobra, Petitioners before everyone). The tests pin the same
ordering, and pin the negative case: a bearer that spent its turn attacking authorises nothing.

**The procession does not check paperwork.** Its approval deliberately omits the replicated question — a
forged affliction legitimises just as well, which is the master's own clause. The case is reachable exactly
once: the procession's OWN chant is not foreign and wins no approval, and the Forger's copy of it is.

**Stage 9 and 10 numbers, where the appendix gave bands:** Sun-Seal Bearer 134 HP (126–142), False-Seal Forger
124 (116–132), Kneeling Petitioners 120 (112–128); Authorized Mark 13 + 1 Inscribed, Seal Strike 20, Royal
Impression 25 Block (pressing the seal costs 6 of it); Forgery Setup 11 + 1 Doubt, Imitation Cut 16,
Counterfeit Seal 20 Block; Petition Chant 13 + 1 Doubt, Kneel in Unison 10 Block to every ally, and the
approval is 7 Block to each body still standing.

**Neither support body is ever fielded alone** — the Forger has nothing to counterfeit and the Petitioners
nothing to legitimise — so both appear only in duos, and the Forger's own encounter borrows Stage 2's Cobra
as the cleanest original status source in the act, exactly as the master prescribes.

## Act IV, Stages 11 and 12 — the House of Linen and the Canopic Vaults (2026-09-02)

Preservation stops being a favour: everything the linen holds in place is one more thing packed around you.
**Nothing new from the engine** — the seam list closed at IV-7, and these two stages are four conversions
built out of what it left.

**Each conversion is capped at once a round, and each is read from a different place.** The Natron Bearer
answers the same `decays_preserved` tally the Complaint Wall carves from, with burial instead of grievance.
The Linen-Wrapped Embalmer answers the amplification EVENT — "was the enlarged thing a wrapping, and was it
the register that enlarged it?" — which is one question now that an amplification names both halves. The
Unfinished Mummy answers a card play while a state stands. That is three different shapes for "a conversion
happened", and each is the cheapest reading of its own trigger.

**The Mummy counts Deeds, because Deed is this game's word for an attack.** The master says "the first Attack
played each player turn"; B&B's card types are Deed / Working / Rite, and the port's cards carry those tags,
so the hooks catch on the first Deed. Its latch is cleared at the PLAYER's turn start rather than the round's,
because the rule is written per player turn.

**The Guardian's office is a face, not a number.** §3.6 and the plan's own marker rule agree: a rotation the
player is meant to plan around must be readable off the body. So the four offices are four named marker
statuses (Body, Breath, Blood, Name), each intent opens its own and closes the other three, and only the open
office applies anything. A counter would have shown "3" and meant nothing.

**The optional cycle guard was declined.** The appendix offers the Guardian a 26–32 Block turn "instead of
adding extra statuses"; the master's signature is four offices and "then repeat", and a fifth step would blur
the one thing the identity is for. Its defence is its 170 HP, and the fight is shorter for it.

**Stage 11 and 12 numbers, where the appendix gave bands:** Natron Bearer 144 HP (136–152), Linen-Wrapped
Embalmer 150 (142–160), Unfinished Mummy 160 (150–170), Fourfold Vessel Guardian 170 (160–180); Drying Rite
13 + 1 Fatigue + 1 Embalmed, Natron Dust 18 + 1 Doubt, Pack Natron 27 Block; Write Instructions 12 + 1
Inscribed, Wrap Tight 14 + 1 Embalmed, Linen Guard 27 Block; Incomplete Wrapping 13 + 1 Embalmed, Hook Drag
21, Stillness 27 Block; Body 14 + 1 Burdened, Breath 13 + 1 Panic, Blood 14 + 1 Poison, Name 11 + 1 Inscribed.

**All three House-of-Linen bodies make their own Embalmed** (§3.5), so each solo is self-sufficient — and
Encounter 37's chain (register → thickened wrapping → weight → blow under linen → burial) works because every
link is capped at once a round, which is what keeps it legible rather than explosive.

## Act IV, Stages 13 and 14 — the Necropolis Warrens and the Chamber of Fixed Days (2026-09-02)

Two stages about what a procedure looks like when it stops pretending not to be a calendar.
**Nothing new from the engine.**

**§3.9 is implemented by REUSE, and reuse is the whole point.** Stage 13 borrows Act III's `green_docket_customs`
and `safe_conduct` unchanged — the same statuses, the same "three Trespass from one source are that source's
Claim", the same prevention that spends a whole licence to refuse one filing. Forking them into
`necropolis_safe_conduct` would have made the localized return a sixth word for the act, which is exactly what
the audit says it is not. The grant is asked of the WHOLE roster (`ActFour.NecropolisOpening`, beside Act III's
own `HeroOpening`), because Safe-Conduct is kept as per-grant instances: asking each body would hand a duo two
licences rather than merging one.

**What the Finder's Claim entitles it to had to be decided.** The master gives the Claim machinery back and
says nothing about what the Finder does with standing — but Act III's own rule is that a Claim is never a
damage multiplier and every party reads its own Claims its own way. The Finder reads its as authority to call
a false threshold a threshold: `False Threshold` is 19 + 4 per Claim, capped at +12, authored as
`damage_per_status` off the OWNER so the telegraph carries the whole formula.

**The Finder answers on its own turn start, with a bookmark in EACH tally.** It is the first body in the act to
need both `measures_met` and `measures_failed` — every earlier one cared about a resolution, or about failures,
but the Finder gives a different thing for each. Answering at its own turn start (rather than off the
resolution) is the act's ordering-free idiom: it takes each resolution once however many other bodies also
watched it.

**The Cursed Loot Bearer has no rule of its own.** "Whenever Burdened actually increases the Energy cost paid
for a card" is a moment the act already writes down — `burden_paid`, the tally Burdened keeps when a surcharge
is PAID rather than cleansed away — so the Bearer is a bookmark in it. The master's "max once per card" is
structural (the tally moves once per card), and the total needs no ceiling either: a turn can only pay as many
surcharges as it had Burdened for.

**The Ibis's Last Rite is a FACE, not a variable — and the face decides the next rite too.** A program can
answer a status it learns at fire time (that is what `Replicated` bought at IV-7) but cannot pocket one for
three turns, so the memory is two marker statuses written by the act's own "an original affliction, on the
player, by this body" gate. An ibis with only one rite in its kit would make that memory inert, so `Set the
Rite` reads the same face and lays **the other** rite: one pair of markers carries the memory AND what is
coming, and the player reads both off the body. The return is one stack (§3.7), and a fight in which the ibis
never landed a rite repeats nothing — "successfully applies" is the master's own wording.

**"Every fourth own turn" is three approaches plus the turn itself**, using the Fallen Capstone Golem's
placement idiom: a visible `approach_of_noon` climbing on the body, an intent rule at 3, and the count cleared
by Black Noon so the procession begins again. A catastrophe the player cannot count down to is just a big
number, which is why the schedule is a status and not a hidden counter.

**Two small local decisions.** Safe-Conduct is capped at 2 in the Warrens — the room opens you with one and
compliance can put one more in your pocket, but nothing stockpiles a way out of the stage. And the Star-Table
Scribe's "failed measure may add 1 Inscribed" lands on `Table Cover`, answering any failure with one stack:
measuring error by BAND is the Reed-Cord Surveyor's office, and giving it to the astronomer as well would blur
the one distinction Stage 1 exists to teach.

## Act IV, Stage 15 — the Cartouche Chambers (2026-09-03)

Two bodies that decide what a blessing of yours is for, and neither answer is "yours". **Two small engine
buys**, both general and both proved in Core, against this act's own expectation that it was pure content
from Stage 11 on — and both were bought because the master's wording could not be honoured without them.

**ERASED is not REMOVED, and the whole stage rests on the difference.** The Name-Erasing Chisel Spirit is a
PROHIBITION worn by the player (scope Buffs, one stack, re-set each round), not a rule that strips a status
after it lands. A status that landed and was then taken away was still GAINED: every rule that answers a gain
has already heard it — the Royal Genealogy Wall's lineage first — and the master is explicit that the erased
status is "never gained". A refusal is the only shape in this engine that means that.

**§3.8's priority rule therefore needs no priority table.** An erased blessing raises no application at all,
so the Wall is fed nothing by it, and a later blessing that survives the same round is still the first one the
Wall hears. The two bodies order themselves, deterministically, out of their own definitions — which is what
lets the player deliberately expose a blessing they can afford to lose in order to spend the chisel.

**Engine buy (a): an application now says how much it landed.** §3.8 asks for "Royal Favor equal to the
stacks gained", and a merge could only report the resulting pile — a one-stack blessing on top of three read
as four. `StatusMergedCombatEvent` now carries `AppliedStacks` beside `Stacks`, and the event-amount
expression answers with it (a first application's total IS its delta, so only the merge needed the extra
field).

**Engine buy (b): `eventPreventerIs`, the mirror of `eventAmplifierIs`.** The engine could already say what
was refused and, since IV-7, what paid for an enlargement — but not which prohibition did the refusing. The
chisel answers its own refusals and must not answer a stranger's. Today nothing else in the port refuses a
buff on the player, so a coarser gate would have worked by accident; the narrow question is the one the rule
actually asks, and it does not compose out of anything already there.

**The chisel is served with the fight as well as topped up each round.** A fight's first round starts before
its bodies are dressed, so a rule nobody wears yet does not fire — the lesson `FollowTheApplicant` paid for in
Stage 1 — and the opening round would otherwise be a free one.

**Royal Favor is spent, not merely used.** The Wall cashes the whole lineage on whichever of its two royal
actions comes round — +3 damage a Favor on the retaliation, +4 Block a Favor on the defence — and is a plain
wall again until the next blessing feeds it. A Favor that survived being cashed would turn the cap of 3 into
a floor.

**New in the converter's effect DSL: `block_per_status`,** the defensive twin of `damage_per_status`. A body
that spends a resource it keeps on itself for DEFENCE could compute the number in a program but could not
telegraph it, and the intent line showed only the floor. Now `Royal Line` reads "32 block +4 per own Royal
Favor (max +12)".

**And a telegraph fix the new statuses forced into the open: intent lines had been printing raw status IDS.**
Any status whose id is more than one word came out with its underscore — "Safe_conduct +1", "+4 per own
Royal_favor" — in 24 intents going back to Act II. The intent line is the one thing a player plans against,
so the id is now read out as words with the small joining ones left lower case: "Safe Conduct",
"Approach of Noon", "Royal Favor".

## Act IV, Stages 16 and 17 — the Hall of the Balance and the Sealed Court Before Eternity (2026-09-03)

Five bodies the player already knows, in the offices the labyrinth was always going to promote them into.
**Nothing new from the engine, and — the whole point of both stages — no new vocabulary.** Every word here is
one the player was taught by the body now holding the office: the measure and its distance, Stone,
preservation, Kept and Broken Oaths.

**The Feather-Bearer's success is the interesting half.** The master asks a met measure to "open a large
defensive vulnerability / damage window", worth 18–24 points. That is authored as a status ON THE BEARER: the
balance is open, and every blow that lands on it goes 8 deeper — three cards' worth is 24, right in the band,
and a player who plays fewer gets less, which is the honest shape of a window. It is closed by the NEXT
weighing rather than by a turn duration, because the answer fires at the bearer's own turn start and a
duration counted from there would expire before the player ever had a turn to use it. A miss is 16 + 5 per
point of distance; the cap of 31 falls at exactly three out, which is the widest a measure of 3 can be missed,
so the cap is a statement about the demand rather than an arbitrary ceiling.

**The Crocodile's two opening conditions are both visible before the player acts** — a weighing they missed,
or 3 Entombed they are already carrying. That is the difference the master draws between a predator and an
ambush, and it is why the jaws are a face on the body and not a hidden flag.

**"Preserved Entry" is Embalmed, one stack.** The master describes it as "derived from the same preservation
language", and the act already has that language with a single fading point every status in the port runs
through. So the Scribe applies 1 Embalmed rather than inventing a per-status preservation: preservation of the
PERSON rather than of one entry, which at the table comes to the same thing, since the entry the Court just
made is the one due to fade next. One stack means exactly one fade is held, which is the audit's "it does not
create an infinite no-decay state".

**⚠ The Oathbound Gate's run-history import is NOT implemented, and it needs an engine seam.** The master says
the Door "may import up to 2 visible stored Oath Memories" *if the player previously encountered the
Oath-Stone in the current Act-IV run*. There is nothing in the engine between a finished encounter and the
next one's roster build — no run-level memory a combat can read — so the Door is fielded with 2 Broken Oath as
ENCOUNTER SCAFFOLDING (`enemy_statuses`, the same seam Act III's Boundary Stone uses to hold a Claim in its
two teaching fights). That satisfies everything the audit checks — visible before the first player action,
capped at two tokens, only Oath tokens, no inspection of hidden history — except the condition itself. The
seam it wants is small and general: a run-level counter written when an encounter ends and readable when the
next one's roster is built. It is deferred rather than bodged, and named here so it is not lost.

**The Sealed Court trio is the only Act-IV encounter with per-roster HP.** Every other combination in this act
fields its bodies at full solo strength, and the act's duos land at 300–360 HP together. Three solo bodies here
would be 595. The master prices the capstone explicitly — 62–64% / 49–51% / 46–49%, 296–349 combined — so the
trio is fielded at 141 / 97 / 84 = 322, which puts the act's hardest standard fight in the same body-mass band
as its duos rather than in elite territory.

**A test-writing gotcha worth stating once:** `FightProbe.Solo` takes the enemy's roster entry from the first
authored encounter that fields it, **scaffolding included**. A probe of the Oathbound Gate therefore starts
with the two Broken Oaths its solo encounter gives it. That is correct — the probe is meant to be the body as
the game fields it — but it means a probe's opening state is not always the enemy's bare `starting_statuses`.

## Act IV, elites — the Surveyor, the Scarab Host and the Rope-Master (2026-09-03)

The first three elite encounters, and the shared layer the other seven will stand on. **Nothing new from the
engine**; three findings, two of them the kind that would have shipped as a quiet lie.

**§6.2 is written once, in the shared file.** "Any elite-generated exact requirement must be checked against
the deterministic current state" is `ActFour.Achievable(demand)`: the demand clamped to the player's Energy
pool at the moment it is made, floored at 1. It lives with the elite layer rather than with the Surveyor
because the Surveyor is only the first body to generate a requirement — the Sphinx, the Decans and the
Treasury all do — and a filter each of them re-derived would drift apart by the fourth one.

**A choice an enemy offers is CARDS.** A combat has no generic prompt, and the Living Petition Chorus already
solved this in Act II: the offer IS cards in the player's hand, playing one is the choice, and leaving them
there is also an answer. Both bodies that needed a choice took it. The Surveyor's two boundaries are ONE pair
of card definitions whose Weighed amount is read from a counter on the player, so a single pair covers every
figure the Surveyor can ever offer; the Scarab Host's three seal-breaking cards are offered only for chambers
still intact. Both exhaust at the turn's end, so refusing costs nothing but gets nothing.

**★ Block expires at its owner's turn start, so "remove up to 10 current Block" was answering into an empty
pool.** The far-boundary success is resolved at the Surveyor's own turn start — the act's ordering-free idiom
— and by that moment the brace it put up last turn is already gone. Removing Block there is a no-op every
time. What a stripped brace actually costs a body is the brace it does not get, so the success now leaves
SLACK IN THE CORD: a status whose stacks reduce the next Block the Surveyor gains, spent on that gain. The
number the master prices (10, or 14 after a re-tension) is unchanged, and it lands somewhere the player can
watch it happen instead of nowhere at all.

**★ A summoned body has no action script.** The engine's intent selector is built from the roster the fight
opened with, so a combatant summoned mid-fight is asked for an intent and answers nothing. It does get turns —
`AddCombatant` appends to the turn order — so the Rope-Master's Stone-Haulers act the way every summon in this
engine acts, and the way the player-board units were designed to: a marker status carrying a turn-start
program. The same marker is where the Hauler's death is heard, which is the only place "when a Hauler dies"
can be asked. `countTargets` over the marker gives the master's "one hand works per enemy turn, taking it in
turns if two live" honestly, sole-hauler case included.

**⚠ A damage-received trigger's SOURCE is the attacker, not the bearer.** The Scarab Host's break-offer read
its own seals off the *player* until a live test caught it — every seal looked broken, so nothing was ever
offered. In that context the receiver is the event's target and the source is whoever struck; a body must
address itself through the rule it is the only one wearing whenever the acting side is not its own.

**Two smaller readings.** "Seal the Jars — only while at least one Seal remains" is authored as what a swarm
with no jars left actually does: it walls up instead (24 Block), rather than an intent that resolves to
nothing. And the Rope-Master's summon roll is counted UP from zero rather than down from two, because a
counter nobody has written reads zero, and "none left" must not be the same answer as "not started".

## Act IV, elites 4–6 — the Cartouche, the Linen House and the Two Pans (2026-09-03)

Three elites that read the act's last three words at boss grade. **Nothing new from the engine** — and the
important negative result: **the ratified amplifier held up**. The Keeper of the Living Cartouche is the body
the plan expected to either prove Inscribed or show it underspecified, and it needed nothing beyond the two
questions IV-1 and IV-7 already bought — "what paid for this enlargement" and "was the thing that grew a curse
or a blessing".

**★ An amplification reads the other way round from every other status event in this engine.** In a
`StatusApplicationAmplified` context, `source` is the body the enlarged status LANDED ON — the one wearing the
register — and `eventTarget` is whoever applied it, so a rule can answer the applier. The Keeper's glyph gate
asked the event target for the applicant marker, which actually reads "did the PLAYER apply this": true of a
blessing the player casts on themselves, false of every curse the Keeper writes. Golden glyphs worked
perfectly and black ones never landed once, which is the most convincing kind of wrong.

Together with the Scarab Host's damage trigger (where `source` is the ATTACKER, not the bearer), that is two
bodies in one session bitten by the same class of thing. The rule to carry forward: **a triggered program's
`source` and `eventTarget` mean whatever that event family found most useful, and they are not the same
across families.** A body that must address ITSELF should do it through the rule it is the only one wearing;
a body that must address the PLAYER should use the applicant marker; and which selector carries which is
worth checking against the adapter every time an event family is used for the first time.

**The Overseer needed a mirror the act was missing.** `decays_preserved` has been written at the one fading
point since Stage 8; the wrapping loosens on the opposite event, so `decays_unpreserved` is now written in the
same place and only for afflictions (a player's own wax lapsing is not a wrapping coming loose). Both halves
are read at the Overseer's own turn start through one bookmark each — which is also what makes the master's
"at most twice a round" enforceable at all: a rule firing once per fade could count them but could never cap
them.

**"Select up to two currently existing temporary negative statuses"** is two picks at index 0 and index 1 of
the player's debuffs. Deterministic (so a replay reproduces it), and a player carrying one affliction has
exactly one wrapped tighter while a clean player takes nothing at all — the master's "no hidden status is
created to fill empty slots" falls out of the selector returning nothing.

**The Treasury weighs a turn against itself.** Quantity is cards played less junk-tagged ones; Value is
`resourceSpentThisTurn`, the same figure the act's own measure reads — so a turn is weighed by one number
throughout the labyrinth rather than by a second accounting. Its Credits are cards again (the third body this
session to take the card-offer idiom), and the master's "once per player turn" is a latch the first draw sets
on the player: both offers stand, and using one closes the counter for the day.

**Two cooldowns are the cycle.** `Correct the Name` and `Close the Accounts` are asked for with a cooldown of
3 intents; both sit in a six-intent cycle, which is twice that, so no separate cooldown machinery was written.

## Act IV, elites 7 and 8 — the Sphinx and the Tombbreakers Three (2026-09-03)

The body that sells you a price list, and the three bodies whose order of death is the fight. **Nothing new
from the engine**; two findings, one of which had been sitting in already-shipped content.

**★ A `SequenceEffectNode` does not see what it just wrote.** The Sphinx's answers each leave a mark and then
ask whether that was the third — and the conditional read the count from before the mark landed, so the
procession never opened on the answer that opened it. Causal sequencing in this codebase is not a stylistic
preference: **any program that asks about state it changed in the same breath must be a
`CausalSequenceEffectNode`.** The Scarab Host's seal card (break the last seal, then ask whether any remain)
had exactly the same latent shape and had shipped two steps earlier without a test that would have caught it;
it is fixed here too.

**★ A measure is never standing when an enemy acts.** Weighed is taken at the END of the turn it stands in and
removes itself doing so, and every enemy action happens after that. So the Sphinx's "+3 per Act-IV negative
status TYPE on the player" can only ever meet two of the three — Burdened and Entombed — and its reachable
band is 25–31 against the master's stated 25–37. The Weighed term stays in the formula because it is live
against any body that raises a measure on its own turn (which is most of the act, and how a second body in a
room meets one standing); what does not hold is the Sphinx's own ceiling, and the cap of 37 never binds. Left
as a balance note rather than quietly re-tuned, because the number is the master's.

**The Tombbreakers bring Act-III law into the tomb with them.** Their Lamp Thief files Trespass under the
source-bound rule, so `ActFour.NecropolisOpening` — written for the False-Door Finder at IV-9 — now answers
for them as well: the same `green_docket_customs` and the same single opening Safe-Conduct, unforked, asked of
the whole roster so three robbers still hand out one licence.

**Tomb-Preserved is deliberately not Embalmed**, and the master says why: the act's preservation holds a fading
thing in place on whoever wears it, so a robber wearing Embalmed could prolong its own afflictions. What the
opened tomb does to its surviving intruders is simpler and only ever good for them, so it is its own
encounter-local word — 4 Block a stack at that robber's turn start, capped at 2.

**Who preserves the survivors.** The master attributes it to the Curse-Bearer ("We Should Not Have Taken
This"), but the effect is written on the shared Tombbreaker marker, which is the only place a robber's death
can be heard at all — and it keeps working when the Curse-Bearer is the one who falls. The tomb is doing the
preserving; the Curse-Bearer is the one who says so out loud.

**The Veteran's Strength rides with its Claim** rather than being re-checked while it holds one. Nothing in
this encounter ever takes a Claim off a robber, so granting the two Strength with the Claim is the same rule
with one fewer moving part.

## Act IV, elites 9 and 10 — the Thirty-Six Decans and the Endless Procession (2026-09-03)

The act's final examination and its last word on discipline, and with them the elite pool closes at ten.
**Nothing new from the engine** — the Colossus's central question ("was a Burdened stack worked off by
PLAYING a taxed card, rather than cleansed or lost?") is `burden_paid`, written at IV-0 for exactly this.

**★ The examination teaches the act to itself.** Watch II of the Thirty-Six Decans hands the player 1
Inscribed; Watch III applies 1 Burdened — and it lands at TWO, because the register enlarges the next thing
that happens to you. Nobody authored that interaction; it is the five words meeting each other, and it makes
the examination's own lesson ("answered by spending the register") true at the table rather than only in the
description. A test asserting 1 caught it, and the test now asserts 2 and says why.

**★ A prohibition cannot answer its own last spend.** The Colossus refuses all outside Strength permanently,
which wants a prohibition that re-arms whenever it is used — but the spend is SYNCHRONOUS, inside the
interception, so by the time the refusal event is handled the final stack is gone and the status with it, and
a bearer-scoped trigger on the prohibition matches nothing at all. The re-arm therefore lives on the body's
own rule status, which is never spent, and says which prohibition it is answering. That last part is what
`eventPreventerIs` was bought for at IV-10: without it the Colossus would restore its refusal every time any
ward anywhere turned anything away.

**Both bodies keep their record at their OWN turn start**, through bookmarks in the act's tallies — which for
the Colossus also means Step III reads a record that is already complete when its own action resolves, with
no agreement needed about the order two turn-end rules fire in.

**Two cycle shapes worth naming.** The Decans' six watches are a counter read at the player's turn start and
advanced at the Keeper's, so the watch standing is always the one the player has a whole turn to answer. The
Colossus's ceremonial pause alternates by a flag the foot raises and the sweep takes down — a flag that can
only ever stand during the one slot it is for, which is how "one secondary action after every completed
cycle" is expressed without an action list that repeats itself.

**The earliest-depth table is data now.** `earliest_depth_percent` on each elite encounter, from the elite
master's "Earliest depth/stage" out of the act's seventeen stages, pinned in `ActFourPoolTests` along with the
property the table encodes: the curve rises across the single-body elites. **The Tombbreakers are the master's
stated exception** — deeper than the Sphinx and lighter than it, because three bodies that all act every round
are worth more than their combined HP says, which is the master's own reason for pricing them low.

⚠ **Wiring the gate into generation waits for Act IV becoming walkable.** The generator gates events and
treasure by ref id (`MapGenerationSpec.NodeRefMinimumDepthPercent`) and roles by kind
(`RoleMinimumDepthPercent`), but elites are not drawn from a ref pool — they are selected by role and weight.
Making them one is a generation change that would move Acts I–III as well, so the table is authored and
pinned now and connected at IV-24, where the act enters the run.

## Act IV, bosses 1 and 2 — the Sealed Name and the Unspoken Heart (2026-09-03)

The first two of the act's eight bosses. **Nothing new from the engine**, and both follow the Act-I–III boss
shape: ONE rotating intent list whose slots mean different things per phase, with the phase written beside the
telegraph through `BossPhases` — the only thing that makes a slot keeping its Phase-I name read as the boss
CHANGING rather than as a bug.

**The Cartouche Ward is legitimacy, not armour.** No blow takes it off; only obeying a Royal Command does, 18
at a time. That inverts the usual boss-shield question: the player is not asked to grind through it, they are
asked every single turn whether THIS command is worth less than the Authority that refusing it hands over. The
commands are the act's own vocabulary read as royal demands — spend exactly 2, end with exactly 1 unspent,
lead with a Deed, lead with a Working, end carrying less register and burial than you began with — and §5.2 is
honoured by issuing only what the turn's deterministic state can meet, with Measure the Throne as the fallback
everywhere because two Energy is what a turn opens with.

**None of the Pharaoh's commands uses the act's measure.** Measure the Throne asks the same question Weighed
asks, and reads `resourceSpentThisTurn` directly instead. That is deliberate twice over: a royal command is
the KING asking and the act's measure belongs to the act, and reading live state at the player's turn end
avoids agreeing with Weighed's own turn-end resolution about which of them runs first.

**★ A card is weighed BEFORE its blow lands.** The Weigher tips its pan on `CardPlayed` and transitions on
`DamageTaken`, and a test that expected the causing card's tip to survive the transition was wrong: the pan
moves first, then the damage resolves, so a transition triggered by a card has the last word on the turn that
caused it. Worth knowing for every boss whose phase change is driven by damage and whose state is driven by
card plays — which is most of them.

**The pan is a signed counter with two visible faces.** A signed number is not a thing a player can look at,
so `balance` is mirrored into `toward_the_heart` / `toward_the_feather` at every move, stacks equal to its
distance from true. Same principle as the Guardian's four offices and the Ibis's remembered rite: what the
player must plan around is a face on the body.

**⚠ `BossLengthTests` now takes a per-boss budget.** Act IV's bosses are priced against a deck three acts of
upgrades deep, and that file's walker brings the character's starting deck on purpose — AND never engages: it
refuses every Royal Command, so the Ward stands at its fifth reduction for the whole fight and never once
opens into an exposure window. The Pharaoh at 630 behind that is the worst case the fight has by construction,
so the two Act-IV bosses get 80 turns rather than 40. The property the file exists for is unchanged: the fight
still ends, and the file still catches one that has stopped ending.
