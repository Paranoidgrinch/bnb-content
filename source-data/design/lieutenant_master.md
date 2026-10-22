# The Lieutenant — Character Master

> **Game:** Bureaucrats & Broomsticks (BnB)  
> **Revision:** v1.0 post-audit consolidation · 22 October 2026  
> **Status:** **Complete authored character design / implementation specification**, not yet implemented or simulation-balanced. All numerical card values and costs are **BALANCE DRAFT**; the logic, named content, deliberate exceptions, event contract and user-approved nomenclature are the proposed design canon.  
> **Inventory:** 4 starter definitions (10 physical starting cards); 20 Commons; 35 Uncommons; 25 Rares; 18 character relics (3 Common, 5 Uncommon, 4 Rare, 6 Shop). Existing General Pool remains independently eligible.  
> **Sources of decisions:** iterative Lieutenant design, explicit user naming selections, targeted gameplay audit `Lieutenant_Gesamtaudit_v0_5.md`, and current exported `archive.json` snapshot. **No claims of engine implementation, automated combat test passes or solved balance.**

---

## 0. Character and fiction

**The Lieutenant** is a young woman newly promoted to lieutenant of the city watch. She earned the post, but she suspects the older guards think she received it too early. She is keenly aware of the new stripes on her sleeve. She insists on standing first in a dangerous doorway, takes responsibility for setbacks that were not entirely hers, and occasionally barks an order before knowing how she will carry it out. Not incompetent; specifically **capable, proud and just uncertain enough to overcommit**.

She carries a service sabre, a battered buckler and a practical city-watch uniform. The equipment is old; her insignia is new. No shining knightly plate or fantasy super-soldier aesthetic. Her supernatural world is bureaucratic, uncanny and absurd, but her response to it is physical and professional. Humour derives from duty, professional pride and unpleasant assignments, **not** from turning her into a bumbling caricature.

**Core fantasy:** *Prove that you can lead from the front. Earn Authority by keeping your word; decide when pride is worth the wound.*

**Personality tension:**
- **Duty versus pride:** A responsible officer must sometimes retreat, delegate or rescind an impossible order.
- **Strength versus control:** She can accumulate Strength for decisive hits, or spend it on other tactical advantages.
- **Bravery versus recklessness:** Open Guard can finance spectacular attacks, but enemy damage remains real.
- **Leadership versus self-doubt:** Orders and Challenges are publicly legible promises. Succeeding earns Authority; failing does not erase the combat.

**Gameplay role:** offensive front-line bruiser with **80 starting maximum/current HP**, default draft **3 Energy per turn**, ordinary BnB card draw unless actual core defaults differ. Stronger direct Attack baseline than Apprentice/Witch; interactive decision density is moderate, not wand-interpreter complexity. She must remain playable when few Orders/Challenges are drafted.

**Tone and names:** user-approved card titles retained verbatim. Compact, old-fashioned city-watch terminology: *Gauntlet, Reveille* is **not** final; user picked *No Excuses*. Avoid 'Arcane', 'Grand', abstract 'Valor' or military equipment introduced after circa 1900. Card names are English. Do not overwrite the author's chosen longer phrases (*Against the Wall*, *Under the Colours*, *Make an Example*, *No Surrender*, etc.) in a future generic naming pass. 'Front Rank' is chosen, **not** 'Frontline'; 'Arms Drill' is chosen, **not** 'Sword Drill'.

### Contrast with existing BnB characters

| Character | Key relationship | Defining choice |
|---|---|---|
| Bureaucrat | paperwork, archival control, administrative pressure | Which procedure will control or exhaust the enemy? |
| Hedge Witch | brewed cards and ingredients, folk remedies and hexes | Play the card now or sacrifice it to a cauldron? |
| Sorcerer's Apprentice | reusable sequence-programmed wand, Focus and Exhaustion | Install, order, cast or burn parts of a spell? |
| **Lieutenant** | Strength, earned Authority, pledges, exposed defense | Commit, keep the order, or break ranks at a cost? |

---

## 1. Content pools and progression

| Shelf | Starter | Common | Uncommon | Rare | Total |
|---|---:|---:|---:|---:|---:|
| Lieutenant class | 4 | 20 | 35 | 25 | **84** |
| Existing General Pool (`archive.json`) | — | 0 | 31 | 19 | **50** |
| Reward-eligible combined pool | — | 20 | 66 | 44 | **130** |

**Starting deck (10 physical cards):** `4 × Watch Cut`, `4 × Buckler Up`, `1 × First Orders`, `1 × New Stripes`. Starting HP 80. Starting Authority 0 unless Commission modifies it.

**Important intentional rarity swap, already applied below:** `Whetstone` is **Common** (reliable early Strength source); `Heave` is **Uncommon** (spend-two-Strength payoff). Do not use old rarities in reward data or QA.

**Relics:** 3 Common, 5 Uncommon and 4 Rare character relics in Normal relic pool, plus 6 character Shop-only relics. These are additions to General relics, never substitutes. Boss/Event relics remain defined by their own acquisition rules. Class-specific relics only appear for Lieutenant. The smaller Apprentice relic pool is irrelevant here.

**Acts:** Rarity denotes probability/complexity, **not a hard Act unlock**. An Act-I Rare must have a usable base effect. Act I is about honest attack/Block and simple orders; II adds spending/conditional returns; III adds substantial engines; IV–V stress high-HP single-target bosses, multi-attacks and status interaction. The archived file does **not** disclose the exact current RewardGenerator weights; verify runtime rather than assuming equal odds.

---

## 2. Combat systems — authoritative rule proposal

### 2.1 Strength (existing native BnB buff)

- Positive Strength adds its current amount to **each eligible direct Attack hit** when that hit resolves. AoE and multi-hit attacks gain Strength per actual hit/target under the same general core semantics. Direct HP-loss effects, Doubt, Status damage and explicitly **non-Deed damage** do **not** automatically receive Strength unless a card explicitly defines a Strength-scaled source.
- Several Lieutenant cards have **Strength payments**. A voluntary payment requires enough *positive* Strength at declaration; the card resolves earlier hits before that payment if its text uses `Then`. The reduction changes Strength on subsequent hits. Negative Strength never counts as currency.
- A payment records **declared** amount and **actual positive Strength removed** separately. Triggers such as Sword Knot, Hilt Knot and Full Tang use **actual** amount. A relic's extra voluntary payment does not create another card-payment event.
- Percentage / half-damage derived from **actual HP damage** (e.g. Make an Example) never gets Strength applied a second time. Use one provenance event per damage instance.

### 2.2 Authority

- Combat-local counter **0–6** (BD). Starts at 0; persists across player turns; does **not** automatically decay. `Commission` is the *only* listed exception carrying a bounded reserve between combats.
- Successful **Order** or **Challenge** grants **1 Authority by default**, then its explicit additional reward. A card's specified success reward of `Gain 2 Authority` means **2 total**, not baseline1 +2. If success text says Strength / Block only, it means *baseline1 Authority plus* the other effects unless the card explicitly replaces Authority.
- An Authority payment is legal only if current Authority covers the **effective cost**. `Brevet` can reduce a next payment; a zero-effective-cost option does not emit `AuthoritySpent`. Spending is atomic before resolving its reward. Over-cap Authority is discarded; losses cannot drive counter below 0.
- `Badge Chain` can replace **one default Authority from natural ChallengeSuccess** with +1 Strength: this is **not an extra Authority or Strength** and cannot be applied twice to one success.
- `Rising Star` observes the actual authority balance **after a positive Authority payment**. End-turn automatic Order rewards are not usable earlier in that turn.

### 2.3 Orders — promises about the Lieutenant's own actions

- Normally **one active Order**. `Chain of Command` raises limit to **two**. Each has an instance ID, source card, deadline, progress, condition and explicit success reward.
- Activation occurs **after the source card's complete play effect**, unless it says otherwise. The source card cannot retroactively satisfy its own new Order. Only subsequent actions/events count.
- A new Order at the limit **replaces** a player-selected old Order: event `OrderReplaced`, **not** `OrderDeadlineFailed`. Replaced/abandoned Orders generate no fulfillment and no natural-failure triggers.
- Orders evaluate **as soon as the requirement is met**, or at their explicit deadline for end-state tests (`exactly 0 Energy`, `no further Deed`, `exactly two cards`, `have ≥N Block`, etc.). Order deadline is not shortened by earlier partial progress.
- The start/end phases matter: at `TurnEnd`, evaluate all pending Orders in stable acquisition order, emit success/failure, resolve their success triggers, then process any `at end of turn after order resolution` card effects. Turn-end Authority cannot be spent on cards already played. For enemy-phase Orders, resolve after the enemy phase's relevant final Attack; if the phase contains no required Attack action, they naturally fail.
- Next-turn Orders like `No Excuses` track the **first card actually played during the next player turn**; if no card is played, fail on its turn end. A Rite played by automatic resolution is not a 'played card' unless emitted natively by the engine.
- **Order deadline failure** is distinct from abandoning. `Reprimand` and `Torn Standard` listen exclusively for true `OrderDeadlineFailed`.
- `Chain of Command` permits conflicting Orders concurrently but does not automatically make them both succeed. If the Rite is removed, the oldest of surplus Orders is replaced without failure.

### 2.4 Challenges — promises tied to one living enemy

- Normally **one active Challenge**. Replacing it ends the old without success/failure bonuses. It records target ID, source instance, condition, progress, strict deadline and whether already extended/transferred.
- Challenge activated **after** source card's damage and all other source-play effects. `Marked Man` and `No Running` do not count their own initial attack. A Challenge cannot be attached to a target already killed by its source card.
- `Deed hit` means an actual direct Attack hit emitted by a **played Deed card**, including multiple hits of one card, whether or not Block absorbs HP damage. `HP damage` is positive post-shield loss. Source card distinctness matters for `Second Blade`, regardless of extra hits from multi-hit/relics. `Witness` can add **virtual hitcount progress**, not extra Attack/Strength/damage events.
- If the target dies: a kill Challenge succeeds only if the source and deadline conditions were met; otherwise Challenge ends `TargetLost`. `Warrant Cord` specifically turns once-per-turn `TargetLost` into 1 Authority (not success). If a success condition is already met on a lethal hit, resolve success **before** death cleanup.
- Default Challenge deadline is **before the target's next action**, with explicit exception for next-turn and attack-response conditions. For conditions requiring the target to actually attack, the required next actual Attack must happen **within the next two enemy actions**, else fail; no automatic success on a buff turn. `Remand` may extend the deadline once; never change what a valid Attack is.
- `Pursuit`: second qualifying hit must occur **in the following player turn**; deadline end of that player turn. `Black Gauntlet`: must face actual target Attack within its next two actions and hit back before that target's subsequent action. `Torn Standard`: its generated follow-up Challenge lasts to **end of the following player turn**.
- `Redress` transfers only to an **eligible living target** that meets original activation requirements, resets progress, **preserves deadline**; cannot move `Pursuit` because its two-turn history is target-specific. It cannot refresh a failed/expired Challenge.
- `Field Pardon` may synthesize **one** success of an obligation active at the beginning of its card play. It emits the normal single fulfillment ID + once-only hooks; no retroactive new Order/Challenge and no infinite re-fulfillment.

### 2.5 Open Guard — deliberate exposure, not Vulnerable

- `Open Guard X` is a Lieutenant-owned special exposure counter; it is not general negative-status copyable/cleansable by normal Status cards, and does not count toward generic negative-status diversity bonuses.
- On the **first actual enemy Attack hit** against the Lieutenant in the next enemy phase, add **+2×X raw Attack damage before Block** (BD), then consume all stacks. An annulled/non-hit event does not consume them. Multi-hit actions only amplify the first real hit. If no eligible hit occurs, OG expires at start of the next player turn.
- The extra incoming component is attributed through actual prevention layers and HP. `OpenGuardExtraHpDamage` = HP lost **with** OG minus HP that would have been lost to the same event **without** OG using the same pre-hit defenses; never negative. This can be 0 even when raw OG bonus was positive. The actual combat engine must define identical branch comparisons for Block, Ward Wax and other prevention.
- `Split Chinstrap`, `No Surrender` can succeed with 0 HP damage even if OG was consumed; `Broken Nose`, `Blood Debt`, `Brandy Flask` require **positive attributable extra HP loss**, not simply presence of OG.
- `Parade Spur` additionally requires **at least one real enemy Attack action**, and survival, for its deferred Authority gain. No Authority for a harmless buff-only enemy phase.
- All exposure-dependent effects must survive stacking and caps without becoming random. Authority / Strength / HP attribution is visible in combat log and UI.

### 2.6 Event and timing contract (suggested names)

```text
CardPlayStart, CardPlayed, CardResolved, DeedHit, HpDamageDealt
StrengthPaid(cardId, declared, actual), RelicStrengthSurchargePaid
AuthorityPaid(costBeforeReduction, actualPaid, balanceAfter)
OrderIssued, OrderFulfilled(orderId, naturalOrSynthetic), OrderDeadlineFailed(orderId), OrderAbandoned, OrderReplaced
ChallengeIssued, ChallengeFulfilled(challengeId, naturalOrSynthetic), ChallengeTargetLost(challengeId), ChallengeReplaced
EnemyAttackActionStarted, EnemyAttackHit, EnemyAttackActionFinished, EnemyPhaseFinished
OpenGuardGained, OpenGuardConsumed, OpenGuardExtraRawDamage, OpenGuardExtraHpDamage
EndTurnOrderEvaluation, EndTurnDeferredCardEffects, PlayerTurnStarted
```

For every obligation: **at most one terminal event**. Trigger chains use event queues with source IDs, stable ordering, round/turn counters and already-processed sets. `OrderFulfilled` can feed Rites; a Rite's reward is never a second Order fulfillment. Forced order replacement never triggers deadline-failure relics. UI shows target, progress, deadline and the exact consequence of success or natural failure.

### 2.7 Rare/relic guardrails

- `Armsmaster` uses an elective **declared Strength payment of ≥2**: player must have nominal amount available before discount; pays one fewer **actual** Strength; card effect is based on declared amount while StrengthPaid triggers use actual.
- `Forced March` gains +3 Strength and +1 OG; at end of the current player turn **remove up to 3 remaining positive Strength** (or +4/-4 when upgraded). No negative Strength debt is generated. Strength deliberately spent before end turn remains spent.
- `Field Pardon` and `Field Pardon+`: identical **3 Authority** success-payment, only on active obligation present at CardPlayStart. The card itself grants 1 Authority. Upgrade improves Block and adds draw; Exhaust remains.
- `Pike Square`: minimum damage prevention = `4 + 2 × current positive Strength` for first hit **per different attacker** next enemy phase; retaliation capped **12 per enemy**. All retaliation is non-Deed and not Strength-boosted again.
- `Open Visor`: after gaining Block, if you have no OG, you may gain 1 OG to enable the next Deed's Block-bypass. Already-active OG requires no extra OG payment.
- `Scarlet Epaulette`: one extra Attack hit **inside** same Deed resolution, not another played Deed. Strength counts once on extra hit, and the extra hit cannot re-trigger the relic.
- `Parade Ground`, `Blood Debt`, `Reprimand` revised to **active Workings**, rather than three additional passive Rare Rites. Keep other Rites as authored.

---

## 3. Card catalog — complete base + upgrade texts

**Common interpretation:** `Deed` is the attack-like native type; `Working` is the non-Rite action; `Rite` is the longer-lived effect. All cards below are **class-specific** and may appear only for the Lieutenant. Each `Upgrade (+)` describes the complete changed lines; all other clauses remain from Base unless stated. Each card's ID is deliberately stable and unique. Costs are base/upgraded-unless-change is specified. Rites persist for combat under native lifecycle. An Exhaust card follows native Exhaust pile rules. All numerical values are BD.

## 3.1 Starter definitions (10-card starting deck)

### S01 · Watch Cut

**ID:** `lt_watch_cut` · **Cost:** 1 Energy · **Type:** Deed

**Base:** Deal **6** damage. Deal **2 extra damage** if target is currently Challenged.

**Upgrade (+):** 8 base damage; Challenge bonus unchanged.

**Design purpose / rules:** The ordinary strike. Challenge check happens when hit resolves, not at card draw.

### S02 · Buckler Up

**ID:** `lt_buckler_up` · **Cost:** 1 Energy · **Type:** Working

**Base:** Gain **5 Block**. If you have Open Guard, gain **2 additional Block**.

**Upgrade (+):** 7 base Block, same OG bonus.

**Design purpose / rules:** Counterweight to New Stripes; does not itself remove exposure.

### S03 · First Orders

**ID:** `lt_first_orders` · **Cost:** 1 Energy · **Type:** Working

**Base:** Gain **4 Block**. **Order:** Play a Deed later this turn. **Success:** Gain **1 Authority total**.

**Upgrade (+):** Gain 7 Block. Order unchanged.

**Design purpose / rules:** Tutorial Order; source Working cannot fulfill its own prospective task.

### S04 · New Stripes

**ID:** `lt_new_stripes` · **Cost:** 1 Energy · **Type:** Working

**Base:** Gain **1 Strength**. You may gain **1 Open Guard** to **Challenge a living enemy:** hit it with a Deed before its next action. **Success:** Gain **1 Authority total**.

**Upgrade (+):** Also gain **4 Block**. Strength and optional Challenge unchanged.

**Design purpose / rules:** Fresh promotion: an attractive, optional personal risk. Cannot initiate Challenge if all enemies dead.

## 3.2 Commons (20; including Whetstone after rarity swap)

### C01 · Pike Butt

**ID:** `lt_pike_butt` · **Cost:** 1 Energy · **Type:** Deed

**Base:** Deal **4 damage twice** to the same target.

**Upgrade (+):** 5 damage twice.

**Design purpose / rules:** Straightforward positive Strength multiplicative hits; one card instance, two hits.

### C02 · Cut the Strap

**ID:** `lt_cut_the_strap` · **Cost:** 1 Energy · **Type:** Deed

**Base:** Remove up to **8 Block** from target, then deal **6 damage**.

**Upgrade (+):** Remove up to 10 Block; deal 8 damage.

**Design purpose / rules:** Block removal is not Attack damage; can expose HP for subsequent effects.

### C03 · Heel Turn

**ID:** `lt_heel_turn` · **Cost:** 0 Energy · **Type:** Deed

**Base:** Deal **3 damage**. If you already have Open Guard when played, deal **3 damage twice** instead.

**Upgrade (+):** 4 damage per hit.

**Design purpose / rules:** A zero-Energy risk payoff. It does not consume OG.

### C04 · Clear the Square

**ID:** `lt_clear_the_square` · **Cost:** 2 Energy · **Type:** Deed

**Base:** Deal **8 damage to ALL enemies**. Gain **1 Open Guard per living enemy actually hit**, maximum 3 OG gained.

**Upgrade (+):** 11 damage to ALL.

**Design purpose / rules:** Risk naturally grows in group fights; hit count uses existing enemy set, not killed after attack only.

### C05 · Clenched Jaw

**ID:** `lt_clenched_jaw` · **Cost:** 1 Energy · **Type:** Working

**Base:** Gain **6 Block**. If you lost HP to an enemy Attack during the **previous enemy phase**, gain **1 Strength**.

**Upgrade (+):** Gain 9 Block.

**Design purpose / rules:** HP-loss history refers to previous enemy phase, not the current one.

### C06 · Buckle

**ID:** `lt_buckle` · **Cost:** 1 Energy · **Type:** Working

**Base:** Gain **1 Strength**, **3 Block**, and **1 Open Guard**. If an Order is fulfilled **after this card resolves during the current player turn**, remove 1 Open Guard at turn end.

**Upgrade (+):** 5 Block; other clauses unchanged.

**Design purpose / rules:** A mixed risky Strength source; OG cleanup happens at end turn and does not grant OG-consumed-by-hit bonuses.

### C07 · Roll Call

**ID:** `lt_roll_call` · **Cost:** 0 Energy · **Type:** Working

**Base:** Draw **1 card**. **Order:** Play at least one Deed **and** one Working later this turn. **Success:** Gain **1 Authority total**.

**Upgrade (+):** Draw 2, then discard 1; Order unchanged.

**Design purpose / rules:** Counts later card instances, not card tags on its own source.

### C08 · Gatehouse

**ID:** `lt_gatehouse` · **Cost:** 1 Energy · **Type:** Working

**Base:** Gain **8 Block**. **Order:** During the next enemy phase, at least one enemy performs an Attack action against you and you take **0 unblocked Attack HP damage** throughout that phase. **Success:** Gain **1 Authority total**.

**Upgrade (+):** Gain 11 Block.

**Design purpose / rules:** A buff-only enemy phase is failure; native damage shield interactions apply.

### C09 · Last Bell

**ID:** `lt_last_bell` · **Cost:** 1 Energy · **Type:** Deed

**Base:** Deal **7 damage**. **Order:** End this player turn with exactly **0 Energy**. **Success:** Gain **1 Authority total**.

**Upgrade (+):** 10 damage.

**Design purpose / rules:** Final Energy is checked at deadline; extra Energy may interfere.

### C10 · Scabbard

**ID:** `lt_scabbard` · **Cost:** 0 Energy · **Type:** Working

**Base:** Gain **4 Block**. **Order:** Play **no further Deed** this turn and end the turn with **at least 1 unspent Energy**. **Success:** Gain **1 Authority total**.

**Upgrade (+):** Gain 6 Block.

**Design purpose / rules:** Restraint rather than aggression; old player card actions cannot retroactively violate it.

### C11 · Marked Man

**ID:** `lt_marked_man` · **Cost:** 1 Energy · **Type:** Deed

**Base:** Deal **5 damage**. **Challenge:** Hit this enemy with direct Deed Attack damage **twice more** before its next action. **Success:** Gain **1 Authority total**.

**Upgrade (+):** Deal 7 damage.

**Design purpose / rules:** Initial hit does not count; Pike Butt fulfills hitcount with one separate card.

### C12 · Braced Pike

**ID:** `lt_braced_pike` · **Cost:** 1 Energy · **Type:** Working

**Base:** Gain **7 Block**. **Challenge:** Choose an enemy presently intending to attack. On its **next actual Attack action** within its next two actions, take **no unblocked Attack HP damage** from it. **Success:** Gain **1 Authority total** and **1 Strength**.

**Upgrade (+):** Gain 10 Block.

**Design purpose / rules:** Two-action limit avoids indefinite pending challenges from future buffs. Require attack completion before success.

### C13 · Arrest

**ID:** `lt_arrest` · **Cost:** 1 Energy · **Type:** Deed

**Base:** Deal **6 damage**. **Challenge:** Defeat the chosen surviving target **before the end of your next player turn**. **Success:** Gain **1 Authority total** and draw 1 card.

**Upgrade (+):** Deal 9 damage.

**Design purpose / rules:** Source hit may kill target before Challenge is issued, in which case no Challenge is created; still a valid attack card.

### C14 · Witness

**ID:** `lt_witness` · **Cost:** 0 Energy · **Type:** Working

**Base:** Gain **4 Block**. If a hit-count Challenge is active, the **next qualifying direct Deed hit** against its target this turn contributes **two progress hits** instead of one.

**Upgrade (+):** Gain 6 Block.

**Design purpose / rules:** Virtual progress only; no extra damage, Strength, hit or attack relic trigger.

### C15 · Signal

**ID:** `lt_signal` · **Cost:** 0 Energy · **Type:** Working

**Base:** Gain **2 Block**. You may **spend 1 Authority** to gain **1 Energy**.

**Upgrade (+):** If you spend Authority, also draw 1 card. Otherwise unchanged.

**Design purpose / rules:** Zero-Authority fallback is weak Block; conversion can be important with Rising Star.

### C16 · Second Rank

**ID:** `lt_second_rank` · **Cost:** 1 Energy · **Type:** Working

**Base:** Gain **6 Block**. You may **spend 1 Authority** to make your **next Deed played this turn cost 0 Energy** (one use).

**Upgrade (+):** Gain 9 Block.

**Design purpose / rules:** One-use cost setting; does not create a free attack by itself.

### C17 · Whetstone

**ID:** `lt_whetstone` · **Cost:** 1 Energy · **Type:** Working

**Base:** Gain **1 Strength**. If you actually spent positive Strength **earlier this player turn**, draw 1 card.

**Upgrade (+):** Gain **2 Strength**; same conditional Draw.

**Design purpose / rules:** **Rarity changed Uncommon → Common by audit.** Reliable non-OG Strength supplier; condition rewards Strength-spend deck but is not mandatory.

### C18 · Armguard

**ID:** `lt_armguard` · **Cost:** 1 Energy · **Type:** Working

**Base:** Gain **8 Block**. You may spend **1 positive Strength** to gain **4 Ward Wax**.

**Upgrade (+):** Gain 10 Block and 5 Ward Wax if the payment is made.

**Design purpose / rules:** Strength buys long-term defense; Ward Wax follows existing general status rules.

### C19 · Headlong

**ID:** `lt_headlong` · **Cost:** 1 Energy · **Type:** Working

**Base:** Gain **2 Open Guard**. Your **next two Deeds played this turn** cost 1 less Energy each (minimum 0).

**Upgrade (+):** Gain only **1 Open Guard**; the two discounts remain.

**Design purpose / rules:** Does not prevent other OG stacks, cannot discount a Deed twice via itself.

### C20 · Split Lip

**ID:** `lt_split_lip` · **Cost:** 1 Energy · **Type:** Deed

**Base:** Deal **7 damage**. If this hit kills its target, draw 1 card; otherwise gain **1 Open Guard**.

**Upgrade (+):** Deal 10 damage.

**Design purpose / rules:** HP-lethal checks after hit resolution; accurate lethal prediction matters.

## 3.3 Uncommons (35; including Heave after rarity swap)

### U01 · Against the Wall

**ID:** `lt_against_the_wall` · **Cost:** 1 Energy · **Type:** Working

**Base:** Gain **7 Block**. **Order:** End this player turn with at least **14 Block**. **Success:** Gain **1 Authority total**.

**Upgrade (+):** Gain 10 Block.

**Design purpose / rules:** Turn-end Block threshold, not a midturn provisional check.

### U02 · Double File

**ID:** `lt_double_file` · **Cost:** 1 Energy · **Type:** Working

**Base:** Draw 1 card and gain **4 Block**. **Order:** Later this turn, hit **two different enemies** using Deeds. **Success:** Gain **1 Authority total** and **4 additional Block**.

**Upgrade (+):** Draw 2, then discard 1; other clauses unchanged.

**Design purpose / rules:** Situational in 1-enemy encounters; baseline draw/Block remains available.

### U03 · Mind the Gap

**ID:** `lt_mind_the_gap` · **Cost:** 1 Energy · **Type:** Deed

**Base:** Deal **8 damage**. **Order:** End this turn with **at least 1 Open Guard and 8 Block**. **Success:** Gain **1 Authority total**.

**Upgrade (+):** 11 damage.

**Design purpose / rules:** Requires visible exposure and actual defense, not just one of them.

### U04 · Cadence

**ID:** `lt_cadence` · **Cost:** 0 Energy · **Type:** Working

**Base:** Draw 1 card. **Order:** Play **exactly two more cards** during this turn. **Success:** Gain **1 Authority total** at turn end.

**Upgrade (+):** Draw 2, then discard 1.

**Design purpose / rules:** The difference between two and three subsequent plays matters; deadline evaluated at EndTurn.

### U05 · No Excuses

**ID:** `lt_no_excuses` · **Cost:** 1 Energy · **Type:** Deed

**Base:** Deal **8 damage**. **Order:** The **first card actually played next player turn** must be a Deed. **Success:** Gain **1 Authority total** and **4 Block** when fulfilled.

**Upgrade (+):** 11 damage.

**Design purpose / rules:** If no card played next turn before ending, deadline failure.

### U06 · No Surrender

**ID:** `lt_no_surrender` · **Cost:** 1 Energy · **Type:** Working

**Base:** Gain **5 Block**. **Order:** Enter the next enemy phase with at least **1 Open Guard**; at least one enemy must actually attack; take **no unblocked Attack HP damage** during that phase. **Success:** Gain **2 Authority total**.

**Upgrade (+):** Gain 8 Block.

**Design purpose / rules:** **Post-audit corrected version**: no self-damage goal; exposed but unharmed.

### U07 · Gauntlet

**ID:** `lt_gauntlet` · **Cost:** 1 Energy · **Type:** Deed

**Base:** Deal **7 damage**. **Challenge:** Before target next action, deal at least **12 actual HP damage in one later Deed hit** to that target. **Success:** Gain **1 Authority total** and **1 Strength**.

**Upgrade (+):** Deal 10 damage.

**Design purpose / rules:** A big single HP hit after activation, not an ordinary strike through Block.

### U08 · On Your Feet

**ID:** `lt_on_your_feet` · **Cost:** 1 Energy · **Type:** Working

**Base:** Gain **8 Block**. **Challenge:** Choose enemy intending Attack; after its next actual Attack action (within its next two actions), you must have at least **1 Block** remaining. **Success:** Gain **1 Authority total** and **+1 card drawn** at the start of your next turn.

**Upgrade (+):** Gain 11 Block.

**Design purpose / rules:** Requires attack actually resolving; buff-only turns are not successes.

### U09 · Pursuit

**ID:** `lt_pursuit` · **Cost:** 1 Energy · **Type:** Working

**Base:** Draw 1 card. **Challenge:** Hit chosen target with a Deed **later this turn**, and **again in your following player turn**, while it lives. **Success:** Gain **2 Authority total**. **Deadline:** end of following player turn.

**Upgrade (+):** Draw 2, then discard 1.

**Design purpose / rules:** Two-turn opponent-specific memory; **not transferable** by Redress.

### U10 · No Running

**ID:** `lt_no_running` · **Cost:** 1 Energy · **Type:** Deed

**Base:** Choose a living enemy that currently has Block. Deal **9 damage**. **Challenge** (if it remains alive): Deal **actual unblocked HP damage** to this target with a **later Deed** before its next action. **Success:** Gain **1 Authority total** and draw 1 card. If no eligible blocked target exists, play as an ordinary 9-damage Deed without issuing a Challenge.

**Upgrade (+):** Deal 12 damage.

**Design purpose / rules:** **Post-audit fix**: no self-defeating requirement that enemy still has Block after this source attack.

### U11 · Dare

**ID:** `lt_dare` · **Cost:** 0 Energy · **Type:** Working

**Base:** Gain **6 Block** and **1 Open Guard**. **Challenge:** Choose enemy intending Attack; intentionally **lose at least 1 HP from its next actual Attack action** (within its next two actions), and survive. **Success:** Gain **2 Authority total** and **1 Strength**.

**Upgrade (+):** Gain 9 Block.

**Design purpose / rules:** Deliberate HP-risk challenge. Different from No Surrender.

### U12 · Second Blade

**ID:** `lt_second_blade` · **Cost:** 1 Energy · **Type:** Deed

**Base:** Deal **5 damage twice**. **Challenge:** Hit this target with Deeds originating from **two different subsequently played Deed card instances** before its next action. **Success:** Gain **1 Authority total** and **1 Strength**.

**Upgrade (+):** Deal 6 damage twice.

**Design purpose / rules:** The source card does not count; Pike Butt twice in same card is one card instance.

### U13 · Testimony

**ID:** `lt_testimony` · **Cost:** 1 Energy · **Type:** Working

**Base:** Gain **6 Block**. **Challenge:** Fulfill an active Order while chosen target remains alive and before its next action. **Success:** Gain **1 Authority total** and draw 1 card.

**Upgrade (+):** Gain 9 Block.

**Design purpose / rules:** Has a condition even without enemy direct involvement; requires active Order to complete.

### U14 · Whistle

**ID:** `lt_whistle` · **Cost:** 0 Energy · **Type:** Working

**Base:** Gain **3 Block**. You may spend **1 Authority** to draw **2 cards**.

**Upgrade (+):** Gain 5 Block.

**Design purpose / rules:** Draw versus retained Authority; avoid infinite loops by positive Authority payment.

### U15 · Dispatch

**ID:** `lt_dispatch` · **Cost:** 1 Energy · **Type:** Working

**Base:** Gain **6 Block**. You may spend **2 Authority** to return a chosen **Deed from discard** to your hand.

**Upgrade (+):** Gain 9 Block.

**Design purpose / rules:** Discard retrieval; card must still be a real accessible instance, not a card in Exhaust.

### U16 · Make an Example

**ID:** `lt_make_an_example` · **Cost:** 2 Energy · **Type:** Deed

**Base:** Deal **14 damage** to one target. You may spend **1 Authority** to deal **non-Deed damage to each other enemy equal to half the actual HP damage dealt to the primary target**, rounded down.

**Upgrade (+):** Deal 18 initial damage.

**Design purpose / rules:** No extra Strength on spread damage; needs 2+ enemies for spend option.

### U17 · Under the Colours

**ID:** `lt_under_the_colours` · **Cost:** 1 Energy · **Type:** Rite

**Base:** The **first time each player turn** an Order is fulfilled, gain **1 Strength**.

**Upgrade (+):** Cost 0 Energy.

**Design purpose / rules:** Long-run link between discipline and physical Strength. Can react to Field Pardon synthetic success once.

### U18 · Flank

**ID:** `lt_flank` · **Cost:** 1 Energy · **Type:** Deed

**Base:** Deal **9 damage** to one enemy. You may spend **1 Authority** to deal **7 direct damage to a different enemy**.

**Upgrade (+):** 12 initial damage; 9 damage to the other enemy.

**Design purpose / rules:** Cannot spend the option in single-target encounters; second hit has own Strength according to native Deed hit rules.

### U19 · Ultimatum

**ID:** `lt_ultimatum` · **Cost:** 1 Energy · **Type:** Deed

**Base:** Deal **8 damage**. You may spend **2 Authority to apply 2 Doubt** to the target.

**Upgrade (+):** Deal 11 damage. Authority payment and **2 Doubt** unchanged.

**Design purpose / rules:** **User-mandated canonical change:** NO Strength removal. Doubt uses existing BnB status semantics.

### U20 · Remand

**ID:** `lt_remand` · **Cost:** 1 Energy · **Type:** Working

**Base:** Gain **7 Block**. You may spend **1 Authority** to extend an active Challenge by **one additional player turn**. Each Challenge may be extended **at most once**.

**Upgrade (+):** Gain 10 Block.

**Design purpose / rules:** If Challenge has a target-attack deadline, extension adds a player-turn response window but does not invent an enemy Attack. Show exact new deadline in UI.

### U21 · Stand Down

**ID:** `lt_stand_down` · **Cost:** 0 Energy · **Type:** Working

**Base:** Gain **4 Block**. You may spend **1 Authority** to **abandon your active Challenge** and draw **3 cards**.

**Upgrade (+):** Gain 6 Block.

**Design purpose / rules:** Explicit abandonment: no ChallengeSuccess, no natural failure and no Warrant Cord target-loss.

### U22 · Arms Drill

**ID:** `lt_arms_drill` · **Cost:** 1 Energy · **Type:** Deed

**Base:** Deal **4 damage twice**. You may spend **1 positive Strength** to deal **4 damage a third time**.

**Upgrade (+):** Deal 5 damage per hit.

**Design purpose / rules:** User-selected title **Arms Drill**; third hit sees Strength after payment.

### U23 · Overhand

**ID:** `lt_overhand` · **Cost:** 2 Energy · **Type:** Deed

**Base:** Deal **14 damage**. If you have at least **3 positive Strength** at hit time, this hit **ignores up to 10 enemy Block** (does not remove that Block).

**Upgrade (+):** Deal 18 damage.

**Design purpose / rules:** A heavy single-hit counterpart to Pike Butt and multi-hit archetype.

### U24 · Heave

**ID:** `lt_heave` · **Cost:** 1 Energy · **Type:** Deed

**Base:** Deal **8 damage**. **Then** you may spend **2 positive Strength** to deal **8 damage again** to the same living enemy.

**Upgrade (+):** Deal 10 damage per hit.

**Design purpose / rules:** **Rarity changed Common → Uncommon by audit**; earlier hit benefits from Strength before the spend; second uses new Strength.

### U25 · Grip

**ID:** `lt_grip` · **Cost:** 0 Energy · **Type:** Working

**Base:** Declare a payment of **0–2 positive Strength**. For each **declared** Strength unit, draw **1 card** and gain **4 Block**; normally all declared Strength is removed, but Armsmaster can discount the actual payment. If zero is declared, gain **2 Block**.

**Upgrade (+):** Gain 5 Block per spent Strength; 3 Block if none.

**Design purpose / rules:** Applies Armsmaster nominal-payment policy when declaring 2 Strength; spend event uses actual removed amount.

### U26 · Shield Punch

**ID:** `lt_shield_punch` · **Cost:** 1 Energy · **Type:** Deed

**Base:** Deal **5 damage**; gain Block equal to **4 + your current positive Strength** after the attack.

**Upgrade (+):** 8 damage; base Block amount becomes 5.

**Design purpose / rules:** Strength stays as buff, providing direct Block; different from Armguard.

### U27 · Scar

**ID:** `lt_scar` · **Cost:** 1 Energy · **Type:** Rite

**Base:** The first time in each enemy phase you actually lose HP to an enemy Attack, gain **1 Strength**.

**Upgrade (+):** Cost 0 Energy.

**Design purpose / rules:** Scales from taking actual HP loss, not extra raw OG damage alone.

### U28 · Broken Pike

**ID:** `lt_broken_pike` · **Cost:** 2 Energy · **Type:** Deed

**Base:** Deal **12 damage** to a target. Then you may spend **2 positive Strength** to deal **7 additional non-Deed damage to ALL OTHER enemies**.

**Upgrade (+):** 16 initial damage and 9 spread damage.

**Design purpose / rules:** First Deed hit gains pre-spend Strength. Spend-based splash gets no second Strength bonus.

### U29 · Front Rank

**ID:** `lt_front_rank` · **Cost:** 1 Energy · **Type:** Deed

**Base:** Deal **11 damage**. If you already had Open Guard when played, deal **4 additional damage** to that hit. Then gain **1 Open Guard**.

**Upgrade (+):** 14 base damage; bonus unchanged.

**Design purpose / rules:** User chose **Front Rank**, not Frontline; paired thematically with Second Rank.

### U30 · Recoil

**ID:** `lt_recoil` · **Cost:** 1 Energy · **Type:** Working

**Base:** Gain **8 Block**. If you enter the next enemy phase with Open Guard and **lose any HP from an enemy Attack** in that phase, draw **2 additional cards next player turn** (once for this card instance).

**Upgrade (+):** Gain 11 Block.

**Design purpose / rules:** Once-instance deferred draw, not per-hit and not necessarily caused by OG extra damage.

### U31 · Ruckus

**ID:** `lt_ruckus` · **Cost:** 0 Energy · **Type:** Deed

**Base:** Deal **3 damage to ALL enemies**. If at least **two different enemies are hit**, gain **1 Open Guard** and draw 1 card.

**Upgrade (+):** 4 damage to ALL.

**Design purpose / rules:** Crowd fighting; intentionally weaker in solo fights.

### U32 · Poise

**ID:** `lt_poise` · **Cost:** 1 Energy · **Type:** Working

**Base:** Gain **10 Block**. You may spend **1 Authority**; if paid, your **next Deed this turn cannot generate Open Guard**, though existing OG remains.

**Upgrade (+):** Gain 13 Block.

**Design purpose / rules:** A self-control effect, not a generic OG purge.

### U33 · Overreach

**ID:** `lt_overreach` · **Cost:** 1 Energy · **Type:** Rite

**Base:** The first time each player turn you **gain any Open Guard**, your next Deed this turn receives **+4 base-independent damage on its first direct Attack hit**.

**Upgrade (+):** First hit gets +6 damage instead.

**Design purpose / rules:** Once-per-turn; added damage combines with normal Strength on that hit only.

### U34 · Bloody Cobbles

**ID:** `lt_bloody_cobbles` · **Cost:** 1 Energy · **Type:** Deed

**Base:** Deal **7 damage**. If you lost HP from an enemy Attack in the previous enemy phase, deal **7 damage twice instead**. Gain **1 Open Guard**.

**Upgrade (+):** 9 damage per hit.

**Design purpose / rules:** Self-reinforcing but HP-risky multi-hit bruiser.

### U35 · Broken Nose

**ID:** `lt_broken_nose` · **Cost:** 1 Energy · **Type:** Rite

**Base:** The first time each enemy phase **Open Guard causes at least 1 additional actual HP loss** to you from an enemy Attack, gain **1 Authority and 1 Strength**.

**Upgrade (+):** Cost 0 Energy.

**Design purpose / rules:** Requires attributed marginal OG HP loss, once per phase; not just any HP loss.

## 3.4 Rares (25; three over-passive Rites revised to active Workings)

### R01 · Chain of Command

**ID:** `lt_chain_of_command` · **Cost:** 1 Energy · **Type:** Rite

**Base:** Gain **7 Block**. Your simultaneous active Order limit becomes **2**. The first time in each player-turn cycle you fulfill **two distinct Orders**, draw **2 additional cards next player turn**.

**Upgrade (+):** Gain 10 Block; the following-turn bonus draws 3 cards.

**Design purpose / rules:** Orders retain individual IDs. With only one issued Order, still a Block fallback and future flexibility. Triggers once per cycle, not every order after #2.

### R02 · Brevet

**ID:** `lt_brevet` · **Cost:** 1 Energy · **Type:** Working · Exhaust

**Base:** Gain **1 Authority and 6 Block**. The **next positive Authority cost paid on a card this turn** is reduced by **1**, minimum 0.

**Upgrade (+):** Gain 9 Block and draw 1 card; authority rule unchanged.

**Design purpose / rules:** Discount applies once; no actual payment => no AuthoritySpent triggers. May be played on Act I as modest acceleration.

### R03 · Insubordination

**ID:** `lt_insubordination` · **Cost:** 1 Energy · **Type:** Deed

**Base:** Deal **10 damage**. You may **abandon** one active Order; if you do, deal **10 damage again** to the same surviving target and gain **1 Open Guard**.

**Upgrade (+):** Deal 12 damage per hit.

**Design purpose / rules:** An intentional breach, not `OrderDeadlineFailed`; cannot farm Reprimand/Torn Standard by abandoning.

### R04 · Parade Ground

**ID:** `lt_parade_ground` · **Cost:** 1 Energy · **Type:** Working

**Base:** Gain **10 Block**. At the end of this player turn, **if you fulfilled an Order after playing Parade Ground**, gain **5 Ward Wax for each unspent Energy**, counting at most **2 Energy**.

**Upgrade (+):** Gain 12 Block; each eligible unspent Energy grants 6 Ward Wax.

**Design purpose / rules:** **Reworked from passive Rite to an active Working.** Reward checks after end-turn Orders resolve; strong on a deliberate restrained turn, good 10-Block fallback without Orders.

### R05 · Drum Major

**ID:** `lt_drum_major` · **Cost:** 1 Energy · **Type:** Working

**Base:** Gain **6 Block** and draw 1 card. The **first Order fulfilled before the start of your next player turn** grants **1 bonus Energy**: immediately if you still have a usable current player turn, otherwise at the start of next player turn.

**Upgrade (+):** Gain 9 Block; rest unchanged.

**Design purpose / rules:** One-time delayed buff from this card. Turn-end successes cannot refund Energy into the already closed turn.

### R06 · Black Gauntlet

**ID:** `lt_black_gauntlet` · **Cost:** 1 Energy · **Type:** Deed

**Base:** Deal **11 damage**. **Challenge:** Within this enemy's next **two actions**, survive an actual Attack action from it; **then** hit it with a Deed before its **following action**. **Success:** Gain **2 Authority total** and **1 Strength**.

**Upgrade (+):** Deal 14 damage.

**Design purpose / rules:** Opponent need not hit HP; attack must actually execute; target must remain alive for counterattack. Overrides basic next-action deadline.

### R07 · Succession

**ID:** `lt_succession` · **Cost:** 2 Energy · **Type:** Rite

**Base:** Gain **1 Strength and 5 Block**. **Once per player-turn cycle**, when you fulfill a Challenge, you may immediately issue a new Challenge to a **different living enemy**: hit it with a Deed before its next action. **Success of generated Challenge:** Gain **1 Authority total**. If no other enemy is alive, draw 1 additional card next turn instead.

**Upgrade (+):** Gain 2 Strength on Play (Block unchanged).

**Design purpose / rules:** Generated success cannot produce another Successor in same cycle. Boss fallback avoids dead Rare in single-target fight.

### R08 · Service Record

**ID:** `lt_service_record` · **Cost:** 1 Energy · **Type:** Deed

**Base:** Deal **8 damage**, plus **4 damage per Challenge naturally or synthetically fulfilled earlier in this combat**, counting at most **4**.

**Upgrade (+):** Deal 10 base damage. In addition, if this attack kills its target, gain 1 Authority (once per player turn).

**Design purpose / rules:** Scales at most +16 base before normal Strength. Does not count failed, abandoned or target-lost Challenges.

### R09 · Redress

**ID:** `lt_redress` · **Cost:** 1 Energy · **Type:** Working

**Base:** Gain **10 Block**. You may transfer your active Challenge to a **different, eligible living enemy**; reset progress but preserve its original deadline. Each Challenge can be transferred only once. **Cannot transfer Pursuit.**

**Upgrade (+):** Gain 13 Block; if you transfer, draw 1 card.

**Design purpose / rules:** Revalidate `intending to attack`, `has Block` etc. against replacement target; never reopen expired Challenge.

### R10 · Riposte

**ID:** `lt_riposte` · **Cost:** 1 Energy · **Type:** Rite

**Base:** Gain **6 Block**. **Once per enemy phase**, after a Challenged enemy completes an actual Attack action against you, deal **5 retaliatory Attack damage** to that enemy. It receives positive Strength as a single retaliation hit but is **not a played Deed**.

**Upgrade (+):** Retaliate for 8 base damage.

**Design purpose / rules:** Cannot satisfy “play a Deed” or “two different played Deeds”; damage has explicit relic/Rite source.

### R11 · Countercharge

**ID:** `lt_countercharge` · **Cost:** 1 Energy · **Type:** Deed

**Base:** Deal **9 damage**. Then you may spend **up to 2 positive Strength**. For each **declared and fulfilled** Strength unit paid, apply **1 Doubt** to target and gain **1 Authority**.

**Upgrade (+):** Deal 12 damage; maximum declared Strength payment increases to 3.

**Design purpose / rules:** With Armsmaster: card effect uses declared amount, actual Strength spent controls Sword Knot/Full Tang. Authority gains respect cap6.

### R12 · Hilt Knot

**ID:** `lt_hilt_knot` · **Cost:** 1 Energy · **Type:** Rite

**Base:** Gain **1 Strength**. The first time each player turn you **actually pay positive Strength for a card**, draw **1 card** and gain **5 Block**.

**Upgrade (+):** Instead draw 2 then discard 1, and gain 7 Block.

**Design purpose / rules:** Once-per-turn; payment of 0 due to discount does not trigger. Distinct from Sword Knot relic Authority gain.

### R13 · Forced March

**ID:** `lt_forced_march` · **Cost:** 1 Energy · **Type:** Working

**Base:** Gain **3 Strength and 1 Open Guard**. At the end of **this** player turn, remove **up to 3 remaining positive Strength** (not below 0).

**Upgrade (+):** Gain 4 Strength and 1 Open Guard; remove up to 4 remaining positive Strength at turn end.

**Design purpose / rules:** **Post-audit fix:** no negative debt or hidden temporary Strength status. Strength spent during turn is gone and cannot also be removed; may deliberately optimize around Strength spend.

### R14 · Pike Square

**ID:** `lt_pike_square` · **Cost:** 2 Energy · **Type:** Working

**Base:** Gain **12 Block**. During the next enemy phase, for the **first actual Attack hit of each different attacking enemy**, prevent up to **4 + 2×current positive Strength damage** before ordinary Block. Deal back **non-Deed retaliatory damage** equal to damage actually prevented, **maximum 12 per enemy**.

**Upgrade (+):** Gain 15 Block; prevention becomes **4 + 3×current positive Strength**, with the same per-enemy retaliation cap12.

**Design purpose / rules:** **Post-audit fix**: useful fallback at 0 Strength; finite per-enemy retaliation. Damage prevention does not reduce OG stored stacks or change hitcount.

### R15 · Armsmaster

**ID:** `lt_armsmaster` · **Cost:** 2 Energy · **Type:** Rite

**Base:** Gain **1 Strength and 8 Block**. At the start of each player turn choose one for that turn: **(A)** gain **1 Strength**; or **(B)** your next card that **declares a positive Strength payment of at least 2** may spend **one less actual Strength** (at least 1 actual). The full card effect is based on the **declared** amount.

**Upgrade (+):** Gain **2 Strength** and 8 Block when played; start-of-turn choices unchanged.

**Design purpose / rules:** Nominal payment must be available before reduction; actual `StrengthPaid` events exclude saved Strength. No zero-cost “spent Strength” trigger.

### R16 · Full Tilt

**ID:** `lt_full_tilt` · **Cost:** 1 Energy · **Type:** Deed

**Base:** Deal **9 damage** to one target. You may spend **0, 1 or 2 additional Energy**; for each Energy actually spent, deal **one additional 9-damage Attack hit** to that same surviving target and gain **1 Open Guard**.

**Upgrade (+):** 11 damage per hit.

**Design purpose / rules:** A single played Deed with 1–3 Strength-scaling Attack hits; Energy committed immediately on play and no indefinite repeats.

### R17 · Blood Debt

**ID:** `lt_blood_debt` · **Cost:** 1 Energy · **Type:** Working

**Base:** Gain **6 Block**. **Prepare a one-phase reprisal:** the first time during the **next enemy phase** Open Guard causes additional actual HP damage to you, record that marginal HP loss, capped at **18**. At the start of your next player turn, deal the recorded amount as **non-Deed damage** to the responsible enemy if alive, otherwise to a chosen surviving enemy. If no extra OG HP loss occurs, deal no reprisal damage.

**Upgrade (+):** Gain 9 Block; record cap increases to 24.

**Design purpose / rules:** **Reworked from passive Rite to active Working**. One stored reprisal per card instance, consumed next player start; does not get positive Strength twice.

### R18 · Open Visor

**ID:** `lt_open_visor` · **Cost:** 1 Energy · **Type:** Working

**Base:** Gain **7 Block**. If you have **no Open Guard**, you may gain **1 Open Guard**. If you have Open Guard after this choice, your **next Deed played this turn** ignores enemy Block on its **first direct Attack hit**.

**Upgrade (+):** Gain 10 Block; the Deed ignores Block on **all** its direct hits instead.

**Design purpose / rules:** **Post-audit fix:** fully usable even with no OG source; exposure is optional but real. Block bypass does not remove opponent Block.

### R19 · Splintered Shield

**ID:** `lt_splintered_shield` · **Cost:** 1 Energy · **Type:** Working

**Base:** Gain **14 Block**. You may destroy **all current Block** after gaining it; if you do, gain **1 Strength, 1 Authority, 2 Open Guard**.

**Upgrade (+):** After destruction, immediately regain **4 Block**.

**Design purpose / rules:** An explicit opportunity cost: all previous Block as well as newly gained Block is sacrificed. If you decline, excellent standalone defense.

### R20 · Last Ditch

**ID:** `lt_last_ditch` · **Cost:** 1 Energy · **Type:** Deed

**Base:** Deal **10 damage +1 for each 3 missing HP**, up to **12 missing-HP bonus**. You may gain **1 Open Guard** to **double only that missing-HP bonus** on this play.

**Upgrade (+):** 13 base damage and up to 16 missing-HP bonus, with same optional OG doubling.

**Design purpose / rules:** **Retains corrected version from prior Rare audit**, not older mandatory-OG wording. At full HP it is still a 10-damage Deed.

### R21 · Reprimand

**ID:** `lt_reprimand` · **Cost:** 1 Energy · **Type:** Working

**Base:** Gain **9 Block**. If **at least one Order naturally failed at its deadline during your previous player turn or during the immediately preceding enemy phase**, gain **2 Strength and 1 Open Guard**. Replacement or abandonment does not qualify.

**Upgrade (+):** Gain 12 Block; if qualified gain **3 Strength** and 1 Open Guard.

**Design purpose / rules:** **Reworked from passive Rite to active Working**. A failure at the **end** of the preceding player turn counts. Abandoned or replaced Orders never count; no permanent passive failure engine.

### R22 · Rising Star

**ID:** `lt_rising_star` · **Cost:** 1 Energy · **Type:** Rite

**Base:** Gain **1 Authority**. The first time each player turn a **positive Authority payment** makes your remaining Authority **exactly 0**, gain **1 Strength and 4 Block**.

**Upgrade (+):** Gain 2 Authority when played; trigger unchanged.

**Design purpose / rules:** A spend-all rather than hoard engine; Brevet discounts cannot fake a nonzero payment.

### R23 · Watchword

**ID:** `lt_watchword` · **Cost:** 0 Energy · **Type:** Working

**Base:** Gain **5 Block**. If **both an Order and a Challenge** are fulfilled after this card is played and before the start of your next player turn, gain **1 Strength, 1 Authority**, and draw **1 extra card next turn**. Triggers once per card instance.

**Upgrade (+):** Gain 8 Block; on fulfillment gain **2 Authority instead of 1**, other rewards unchanged.

**Design purpose / rules:** Uses independent Order/Challenge IDs. A synthetic Field Pardon can finish one only, not both.

### R24 · Watch Surgeon

**ID:** `lt_watch_surgeon` · **Cost:** 1 Energy · **Type:** Rite

**Base:** Gain **8 Block**. The first time in each player-turn cycle you fulfill an Order or Challenge, you may spend **1 Authority** to heal **2 HP** (up to max HP). The Authority just earned may pay for this.

**Upgrade (+):** Heal 3 HP when spent; rest unchanged.

**Design purpose / rules:** Sustain engine; once-cycle even if two obligations succeed. Watch Brandy Flask + Ward Wax synergy in simulation.

### R25 · Field Pardon

**ID:** `lt_field_pardon` · **Cost:** 1 Energy · **Type:** Working · Exhaust

**Base:** Gain **10 Block and 1 Authority**. You may spend **3 Authority** to **declare exactly one Order or Challenge that was already active at the beginning of this card play successfully fulfilled**, resolving its normal reward and success triggers **once**, then ending it.

**Upgrade (+):** Gain **13 Block and 1 Authority** and draw **1 card**. The synthetic-success cost remains **3 Authority** and Exhaust remains.

**Design purpose / rules:** **Post-audit fix**: no upgraded 2-Authority exploit. Cannot synthesize a success for an obligation issued by Field Pardon, cannot fulfill two via one event.

---

## 4. Character relic catalog — 18 complete effects

Every relic below is **Lieutenant-only**. Common/Uncommon/Rare are eligible in the normal character relic pool; Shop is **Shop-exclusive**. No relic is silently available as a Boss relic. Values are BALANCE DRAFT. The physical object/short flavour text should emphasize old municipal watch equipment, not heroic fantasy prizes.

### 4.1 Common relics (3)

### C01 · Tin Fife

**ID:** `lt_relic_tin_fife` · **Pool:** Common

**Effect:** The **first time each player turn** you issue an Order, **draw 1 card, then put 1 card from your hand on the bottom of your draw pile**.

**Intent / interactions:** A fife is a small military flute, **not a whistle**. Hand filtering, not net card advantage. The newly issued Order is active before the draw/discard; its own source card does not count as a subsequently played card.

### C02 · Chipped Buckler

**ID:** `lt_relic_chipped_buckler` · **Pool:** Common

**Effect:** The first time each player turn a **direct Deed hit** strikes the current **Challenged target**, gain **4 Block**.

**Intent / interactions:** Works with optional starter New Stripes Challenge. The relevant hit must be a native Deed hit; source Challenge card cannot itself contribute retroactively.

### C03 · Split Chinstrap

**ID:** `lt_relic_split_chinstrap` · **Pool:** Common

**Effect:** **Once per enemy phase**, if a real enemy Attack hit **consumes Open Guard** and that entire hit causes **0 actual HP damage**, gain **1 Authority at the start of your next player turn**.

**Intent / interactions:** Safe risk-reward; distinguish any HP damage from marginal OG-caused HP damage. At most one queued Authority credit per phase.

### 4.2 Uncommon relics (5)

### U01 · Sword Knot

**ID:** `lt_relic_sword_knot` · **Pool:** Uncommon

**Effect:** The first time each player turn you **actually pay at least 1 positive Strength for a card effect**, gain **1 Authority after that card resolves**.

**Intent / interactions:** Strength spend needs actual removal, not discounted nominal cost; no self-recursive payment.

### U02 · Warrant Cord

**ID:** `lt_relic_warrant_cord` · **Pool:** Uncommon

**Effect:** **Once per player turn**, if an active Challenge ends because its target dies **without Challenge success**, gain **1 Authority**.

**Intent / interactions:** Observes `ChallengeTargetLost` only, not natural `ChallengeFulfilled`. A killing blow on an Arrest kill-Challenge resolves success and does not also grant Warrant Cord.

### U03 · Parade Spur

**ID:** `lt_relic_parade_spur` · **Pool:** Uncommon

**Effect:** At the end of your player turn, if you have Open Guard, you may **gain 1 additional Open Guard** and arm a one-phase wager. **If at least one enemy actually executes an Attack action against you in the following enemy phase and you survive it**, gain **1 Authority at the start of your next turn**.

**Intent / interactions:** **Post-audit fix**: non-attacking buff phases are not a free payout. The extra OG is real and raises first-hit exposure. The wager is not cumulative or re-triggered in the same phase.

### U04 · Pay Chest

**ID:** `lt_relic_pay_chest` · **Pool:** Uncommon

**Effect:** At the end of your player turn, if you have **at least 3 Authority**, gain **5 Block**.

**Intent / interactions:** Hoard incentive. Check current Authority after end-turn Order successes, so a fulfilled endturn Order can reach threshold. General defensive relics still operate independently.

### U05 · Dented Gorget

**ID:** `lt_relic_dented_gorget` · **Pool:** Uncommon

**Effect:** Once per enemy phase, if an enemy Attack hit **consumes Open Guard**, your **first Deed played next player turn** costs **1 less Energy**, minimum 0.

**Intent / interactions:** Does not require OG-caused HP damage. Any unused discount expires at next player-turn end; multiple hits cannot stack discounts.

### 4.3 Rare relics (4)

### R01 · Torn Standard

**ID:** `lt_relic_torn_standard` · **Pool:** Rare

**Effect:** **Once per player turn**, when an Order **naturally fails at its deadline**, you may Challenge a living enemy: **hit it with a Deed by the end of your following player turn**. **Success:** Gain **1 Authority total**.

**Intent / interactions:** Special deadline deliberately survives intervening enemy actions. Generated Challenge replaces the old one without previous Challenge natural failure. Only `OrderDeadlineFailed`, not Stand Down/Insubordination/replacement.

### R02 · Full Tang

**ID:** `lt_relic_full_tang` · **Pool:** Rare

**Effect:** The first time each player turn you **pay positive Strength for a card**, choose a living enemy and deal **6 non-Deed damage for each actual Strength spent**, counting at most **3 Strength**.

**Intent / interactions:** At most 18 base damage per turn; cannot receive Strength again. If Armsmaster discounts paid Strength, damage uses the **actual** spent count.

### R03 · Scarlet Epaulette

**ID:** `lt_relic_scarlet_epaulette` · **Pool:** Rare

**Effect:** Once per player turn, when you **first gain Open Guard**, empower your **next single-target Deed this turn** to deliver **one extra direct Attack hit** to its target, with base damage equal to **half the Deed's printed base damage, rounded down**. Positive Strength applies once to this extra hit.

**Intent / interactions:** The extra hit is part of the same played Deed instance (counts for Marked Man hitcount, not Second Blade distinct-card requirement). It cannot re-trigger this relic, and only eligible direct Attack Deeds receive it.

### R04 · Commission

**ID:** `lt_relic_commission` · **Pool:** Rare

**Effect:** Begin each combat with **1 Authority**. At victorious combat end, **store up to 2 unspent Authority**; add that stored amount at the beginning of the **next combat**, in addition to the normal 1. Overwrite rather than accumulate the reserve at each combat end.

**Intent / interactions:** Reserve persists as run state; current Authority still capped6. Aborted/defeated combats follow native run lifecycle. This gives choices near victory beyond standard instant spending.

### 4.4 Shop-exclusive relics (6)

### S01 · Lead Knuckles

**ID:** `lt_relic_lead_knuckles` · **Pool:** Shop

**Effect:** **Once per player turn**, when playing a single-target Deed while you have Open Guard, you may **Exhaust one OTHER card from your hand**. If you do, add **8 damage to that Deed's first direct Attack hit**.

**Intent / interactions:** The other card follows native Exhaust, not Archive. If there is no eligible other card, cannot pay. Bonus is not its own separate Attack hit.

### S02 · Iron Shackle

**ID:** `lt_relic_iron_shackle` · **Pool:** Shop

**Effect:** The first time each player turn you **issue a Challenge** to an enemy, you may **gain 1 Open Guard** to **apply 2 Doubt** to that Challenged target.

**Intent / interactions:** Doubt is the existing status; no new confusion mechanic. OG cost is optional, enemy must be valid/live after Challenge source finishes.

### S03 · Brandy Flask

**ID:** `lt_relic_brandy_flask` · **Pool:** Shop

**Effect:** After a **victorious combat**, heal HP equal to **half the additional HP damage actually caused by Open Guard across that combat**, rounded down, maximum **6 HP**.

**Intent / interactions:** Accumulate `OpenGuardExtraHpDamage`, not raw 2×OG. The flask cannot heal more than marginal exposure loss, and actual max-HP cap still applies. Flavour: watchroom brandy marked “medical use”.

### S04 · Badge Chain

**ID:** `lt_relic_badge_chain` · **Pool:** Shop

**Effect:** **Once per player turn**, when a Challenge succeeds and would grant at least **1 Authority**, you may replace **1 Authority from that success reward** with **1 Strength**.

**Intent / interactions:** No doubling: choose Authority or Strength. Synthesized success can be eligible if its normal success includes Authority, but cannot replace Authority the success does not actually award.

### S05 · Worn Epaulette

**ID:** `lt_relic_worn_epaulette` · **Pool:** Shop

**Effect:** The first time each player turn you fulfill an Order, your **next Working costs 1 less Energy**, minimum0. The discount may be used during this turn or your next player turn and then expires. Discounts from this relic do not stack.

**Intent / interactions:** Name retained from existing proposal pending any later explicit rename. Different from Scarlet Epaulette despite shared garment part.

### S06 · Spur Buckle

**ID:** `lt_relic_spur_buckle` · **Pool:** Shop

**Effect:** Once per player turn, **after a card actually spends positive Strength**, you may pay **one additional positive Strength** via this relic to gain **1 Energy** after that card resolves.

**Intent / interactions:** Extra relic Strength consumption is **not** another CardStrengthPaid, so no Full Tang/Sword Knot/Hilt Knot cascade. Requires one remaining positive Strength after the card payment.

---

## 5. Build families and complete synergy examples

These are **real, mechanically distinct draft strategies**, not mandatory categories or hard deckbuilding locks. General Pool remains selectable. No build below requires a named Rare just to deal damage in Act I. Some endgame synergies necessarily require finding several specific cards/relics; that is an upside, not a baseline assumption.

### 5.1 Strength Bruiser — keep it simple

**Entry:** `New Stripes`, `Whetstone`, `Buckle`, `Pike Butt`, `Arms Drill` (later); other standard direct-Deed and defensive cards.

**Core:** Gain positive Strength, deliver repeated Attack hits. The reliable early Strength source is **Common Whetstone** (post-audit). The most straightforward version barely uses Authority; it works on a single boss and in mixed fights.

**Play line:** Starting at Strength 0, play `New Stripes` → Strength1; later play `Pike Butt` → two hits, each with +1 Strength. Upgrade and add Whetstone. Against Block-heavy targets prefer `Cut the Strap` or `Overhand` rather than blindly adding tiny hits.

**Weakness:** Multi-hit scaling can overwhelm single-hit choices at high Strength. Compare at Str 0/3/6/10; do not blindly increase damage of all multi-hits.

### 5.2 By the Book — Orders and Authority

**Entry:** `First Orders`, `Roll Call`, `Gatehouse`, `Scabbard`, `Signal`, `Cadence`, `Under the Colours`. Distinct best-case: `Chain of Command`, `Drum Major`, `Commission`, `Worn Epaulette`.

**Core:** Follow self-imposed turn restrictions and invest or conserve earned Authority. Active Orders are limited; playing another Order at base limit **replaces** the previous one, so chaining two independent Orders requires Chain of Command.

**Play line with Chain:** `Cadence` (Order: precisely two subsequent cards) → `First Orders` (second Order) → `Watch Cut` (completes First Orders immediately) → End Turn (two subsequent cards after Cadence: First Orders + Watch Cut; completes Cadence). Gain Authority twice; `Chain of Command` schedules bonus draw next turn. **Without Chain**, First Orders replaces Cadence and only First Orders can succeed.

**Built-in tension:** `Last Bell` (0 Energy at end) and `Scabbard` (≥1 Energy at end; no Deed) conflict; Chain of Command does **not** waive this. `Parade Ground` wants fulfilled Order **and** unspent Energy; Scabbard suits it, Last Bell cannot grant the unspent-Energy payoff.

### 5.3 Duelist — Challenges against a chosen enemy

**Entry:** `New Stripes`, `Marked Man`, `Pike Butt`, `Braced Pike`, `Gauntlet`, `Arrest`, `Second Blade`, `Overhand`, `Redress`, `Service Record`, `Chipped Buckler`, `Badge Chain`.

**Core:** A chosen enemy becomes a tactical focus with a deadline. `Marked Man` needs **two subsequent hits**; `Pike Butt` generates exactly two (one card). `Second Blade` needs hits from **two distinct subsequently played Deeds**, so the very same Pike Butt does **not** suffice. `Gauntlet` needs one *unblocked HP hit ≥12*; a small multi-hit card may fail it while `Overhand` succeeds.

**Play line:** `Marked Man` hits once and issues Challenge. `Pike Butt` subsequently hits twice; Challenge succeeds → 1 Authority. With `Chipped Buckler`, the first of those hits also grants Block. `Badge Chain` may swap the earned Authority into Strength, not both.

**Single-boss viability:** `Braced Pike`, `Gauntlet`, `Black Gauntlet`, `Riposte` and `Service Record` work against a single target. `Arrest` and `Double File` are weaker in certain encounters, **not** mandatory backbone cards.

### 5.4 Strength-Spender — turn accumulated Strength into tools

**Entry:** `Whetstone`, `Buckle`, `Heave` (now Uncommon), `Grip`, `Armguard`, `Arms Drill`, `Countercharge`, `Hilt Knot`, `Armsmaster`, `Sword Knot`, `Full Tang`, `Spur Buckle`.

**Core:** Strength can become additional hits, Block, Ward Wax, Doubt, Authority, card draw or Energy. This is distinct from passively hoarding permanent Strength.

**Worked example:** At **Strength 3**, play `Heave` for 1 Energy against one living enemy. First hit: 8+3=11 pre-mitigation. Then declare Strength payment 2 → actual Strength falls to1. Second hit: 8+1=9 pre-mitigation. `Sword Knot` grants 1 Authority after the full card. `Full Tang` adds **6×2=12 non-Deed damage** to chosen enemy, once this turn; **no third Strength bonus**. If `Armsmaster` was in its reduction mode and fully eligible, declared payment2 costs **actual1**, second hit sees Strength2, `Full Tang` deals only6 because it reads actual removal, while the card's repeat still resolves.

**Price identity:** `Grip` with Armsmaster may declare2 (draw2 and Block8) while physically spending1. The card effect tracks declared units, and actual spend triggers only once. `Spur Buckle` can remove an *additional* Strength to generate Energy, but that surcharge does not generate another card payment event.

### 5.5 Front Rank — fast and exposed

**Entry:** `Headlong`, `Heel Turn`, `Front Rank`, `Ruckus`, `Overreach`, `Full Tilt`, `Open Visor`, `Scarlet Epaulette`, `Dented Gorget`.

**Core:** Obtain immediate tempo by exposing yourself. Not simply 'play attacks with Vulnerable': OG is **one next-enemy-hit amplifier**, has tracked additional HP effect and is not generically cleansable.

**Play line:** `Headlong` adds OG and discounts next two Deeds. `Heel Turn` at OG performs two hits; `Front Rank` profits from preexisting OG and adds another. `Scarlet Epaulette` adds an extra hit to next eligible single-target Deed. The first actual next-enemy Attack gets OG raw damage +2 per remaining stack. If completely blocked, `Split Chinstrap` can create Authority; if real extra OG HP loss occurs, `Broken Nose` and `Blood Debt` can react instead.

**Decision:** Often the best play is to **stop** after two attacks rather than add more OG. A small hit can consume all OG; a multi-hit enemy only amplifies first hit.

### 5.6 Shield and Steel — tank without being passive

**Entry:** `Buckler Up`, `Gatehouse`, `Against the Wall`, `No Surrender`, `Braced Pike`, `Shield Punch`, `Armguard`, `Pike Square`, `Parade Ground`, `Chipped Buckler`, `Pay Chest`.

**Core:** Block plus extra prevention, some Ward Wax, convert earned Strength into persistence; may deliberately hold Open Guard but actually prevent HP damage. Strength does not automatically make Block stronger, except through clearly authored cards.

**Play line:** Play `No Surrender`, then gain OG via `Buckle` or other source, enter enemy phase with OG and enough Block. A real enemy Attack occurs, OG amplifies its first hit, but after mitigation Lieutenant loses0 HP → Order succeeds for 2 Authority. `Split Chinstrap` can add another at the start of next turn (authority cap applies). `Parade Spur` cannot earn Authority in an attack-free enemy phase.

### 5.7 The Bloodied Veteran — damage received with a price

**Entry:** `Clenched Jaw`, `Scar`, `Dare`, `Broken Nose`, `Bloody Cobbles`, `Blood Debt`, `Last Ditch`, `Watch Surgeon`, `Brandy Flask`.

**Core:** Sometimes let a real hit through; later turn retaliates harder. HP is a finite real resource: healing cannot retroactively undo a death or grant extra rewards from blocked raw damage.

**Worked example:** OG3 adds +6 **raw damage** to next hit. With the same Block/prevention, the hit might cause only **2 additional actual HP loss** attributable to OG. `Broken Nose` qualifies (+1 Strength +1 Authority once). `Blood Debt` stores **2**, not 6. `Brandy Flask` accumulates **2** for post-victory healing (half is 1), still capped6. If Block absorbs all incremental damage, those marginal-damage rewards do not happen.

**Weakness:** too much sustain (`Watch Surgeon`, `Brandy Flask`, Ward Wax relics) may erase the cost; measure net run HP, not just per-combat damage.

### 5.8 Pride and Failure — admit the mistake, then use it

**Entry:** `Cadence`, `No Excuses`, `Insubordination`, `Stand Down`, `Reprimand`, `Torn Standard`.

**Core:** There are three entirely different outcomes: naturally missed deadline, voluntary abandon, replacement by new Order. Only **natural** failure pays Reprimand/Torn Standard.

**Worked example:** `Cadence` issues Order, but the player later plays three cards after it. At end turn, **OrderDeadlineFailed** (not success). At following enemy phase and next player turn, `Torn Standard` offered a Challenge lasting until end next player turn; `Reprimand` played during next turn reads that end-turn natural failure and grants Strength. If instead `Insubordination` abandoned Cadence, it granted a second attack and Open Guard, **but no Reprimand/Torn Standard payoff**.

**Build constraint:** Not the starter strategy. It is a viable rare/relic-assisted niche; no requirement to make routine failed Orders mathematically preferable to honest successful Orders.

### 5.9 Authority: spend now vs bank for later

**Entry:** `Whistle`, `Dispatch`, `Ultimatum`, `Signal`, `Second Rank`, `Pay Chest`, `Rising Star`, `Commission`, `Badge Chain`, `Brevet`, `Field Pardon`.

**Tension A — keep:** Ending with ≥3 Authority grants Block from `Pay Chest`; finishing a combat with 1–2 may carry those points to next combat via `Commission`.

**Tension B — zero out:** Spending your **last** positive Authority on `Signal` or another card activates `Rising Star` once/turn for Strength+Block.

**Tension C — swap currencies:** `Ultimatum` spends **2 Authority for 2 Doubt** (no Strength removal); `Badge Chain` trades natural Challenge-success Authority for Strength; `Sword Knot` gains Authority from **actually** spent Strength.

**Important finite line:** `Field Pardon` first grants1 Authority, may then spend3 to synthesize exactly one pre-existing success → its reward can restore at most its normal single Authority amount, with appropriate once-per-turn triggers. Upgraded form **still costs3**; it Exhausts. No guarantee of a free permanent loop.

---

## 6. Compatibility with existing General Pool and other BnB systems

The `archive.json` snapshot lists **50 General Pool cards: 31 Uncommon, 19 Rare, 0 Common**. The following are verified source examples, not invented new General cards; their effects stay as documented in that archive. Do **not** rewrite the General Pool to facilitate Lieutenant synergies.

| Existing General card | Why it matters to Lieutenant | Boundaries |
|---|---|---|
| **Black Tribunal** | Large damage can scale with different negative Status types, including Doubt when legitimately applied | Doubt does not count multiple times just because stack count is high; follow printed diversity cap |
| **Notary Beetle** | The first application of a new negative Status to enemy may create an extra stack; interacts with Ultimatum / Iron Shackle Doubt | Only if status application event is native/valid; per-turn cap stays intact |
| **Sealed Mantle** | Adds Ward Wax after an attack phase with no unblocked Attack HP damage, convenient for No Surrender / Gatehouse lines | Requires actual enemy Attack and its own text; no payout on pure buff turn |
| **Votive Covenant** | Ward Wax decay follows whether unblocked Attack damage occurred, making careful defensive turns worthwhile | Does not eliminate OG exposure by itself |
| **Waxen Surety** | Adds Ward Wax, enabling durable Block and defensive Orders | Do not confuse Ward Wax stacks with Block at end-of-turn thresholds |
| **Tallow Reserve** | Converts existing Block into Ward Wax | Costs Block; can make Against the Wall harder to fulfill |
| **Grand Dispensation** | Versatile 2-of-4 effects incl. draw/Energy help finish awkward Orders or recover | Exhausts; no infinite free choice-chain assumed |
| **Borrowed Candle** | Hand selection makes future-turn No Excuses/particular Deed setups easier | Exhausts; no special 'Order' native event from draw |
| **Dawn Summons** | Rewards being first card played, tension with next-turn No Excuses if first played must be Deed | The first-card condition is the existing card's actual printed requirement |
| **Blacklisted / Sovereign Prohibition** | Censure adds Status denial/control to a bruiser who otherwise lacks it | Status prevention must not blanket-cleanse OG (it is exposure state, not standard Status) |

**General Pool status semantics:** `Doubt`, `Ward Wax`, `Censure`, `Blood Ink`, `Lien` etc. continue to use native BnB implementation. This document intentionally does **not** redefine Doubt numerically; `Ultimatum`'s user-approved text is `Apply 2 Doubt`. Follow engine hooks for multiplicative status effects, not improvised equivalents.

**Other characters:** Every Lieutenant-only Order/Challenge/OG native event is scoped to her character state. No Bureaucrat card that happens to be named 'Order' becomes a Lieutenant Order automatically. No Witch Ingredient or Apprentice Wand operation is inferred from Deed/Working/Rite types. Shop-only Lieutenant relics cannot leak to other character pools.

---

## 7. Presentation and UI contract

1. **Portrait / attitude:** side-on, slightly forward-leaning stance; short and functional equipment; youthful woman with a newly polished insignia on worn uniform. Draw expression as focused, wary and overdetermined—not smiling mascot, emotionless tyrant or fantasy queen.
2. **Authority meter:** visibly bounded **0–6** with expected gain/spend preview, including Commission reserve indicator only when relevant.
3. **Orders:** one (or two with Chain of Command) small explicit panels with source, success condition, countdown/deadline and failure consequence. Success animates earned Authority exactly once. No vague icon-only obligation.
4. **Challenges:** display target enemy, threshold/hitcount, deadline and whether currently transferable/extendable. If no eligible target for a conditional challenge, card shows 'attack only' fallback where applicable.
5. **Open Guard:** clear number next to player defense and an indicator on next enemy Attack intent preview for **+raw exposure**. Show true preview to HP after current Block without falsely guaranteeing exact outcomes where enemy/status mechanics are conditional.
6. **Strength:** native existing status UI, but voluntary Strength expenditures require confirmable cost and a preview of subsequent Attack values. Armsmaster declared vs actual must be explicit.
7. **Deferred effects:** Drum Major Energy, Recoil draw, Scarlet Epaulette next Deed, Blood Debt reprisal, Worn Epaulette discount and No Excuses next-turn Order need visible small timers/charges. Avoid an unbounded cloud of passive icons.
8. **Animation:** military gestures, sidestepping, a buckler strike, torn fabric, loose cobbles and old watch equipment. No automatic fancy magic trails. When activating Doubt, show a deliberate stare, challenge or shouted order—not a Bureaucrat stamp.

---

## 8. Required acceptance and regression tests

The tests below are **planned requirements**, not reported passing tests. Numbers are BD; each requirement should be asserted in native harness / combat simulator.

| ID | Test | Expected outcome |
|---|---|---|
| LT-001 | Fresh starter deck count | 4 Watch Cut +4 Buckler Up +1 First Orders +1 New Stripes; HP80 |
| LT-002 | Rarity counts | 4/20/35/25, 3/5/4/6 relics; Whetstone Common, Heave Uncommon |
| LT-003 | Starter Challenge | New Stripes→Watch Cut: +1 Authority if target survives first card; OG persists |
| LT-004 | Base Order replacement | Cadence→First Orders replaces Cadence; no failure event |
| LT-005 | Chain of Command | Both Cadence & First Orders fulfilled sequentially with exactly two later cards |
| LT-006 | Enemy Attack requirement | Gatehouse/No Surrender do NOT succeed on non-Attack enemy phase |
| LT-007 | Challenges require source postresolution | No Running source hit never retroactively fulfills its own Challenge |
| LT-008 | No Running patch | Target with initial Block after source hit can be fulfilled by later unblocked HP Deed hit |
| LT-009 | Hit vs distinct card | Marked Man passes via Pike Butt double hit; Second Blade does not with single Pike Butt |
| LT-010 | HP-only Gauntlet | Huge pre-block hit dealing 0 HP never qualifies |
| LT-011 | Pursuit next-turn exception | Requires Deed both turns; does not fail solely on intervening enemy action |
| LT-012 | Braced Pike no automatic buff-phase success | Actual enemy Attack in next two target actions required |
| LT-013 | Redress eligibility | Changed target revalidates initial condition, preserves deadline, Pursuit denied |
| LT-014 | Order vs Challenge failures | Natural failure distinct from abandon/replaced/target loss |
| LT-015 | Torn Standard | End-turn failed Order issues Challenge lasting through next player-turn end |
| LT-016 | Warrant Cord | Target lost awards at most 1 Authority/turn but no ChallengeSuccess |
| LT-017 | Authority cap | Gains stop at6, Commission carries max2, no negative cost |
| LT-018 | Badge Chain replacement | 1 Challenge-success Authority → 1 Strength, not both |
| LT-019 | Rising Star | Actual positive Authority payment to exactly0 triggers once, 0 cost does not |
| LT-020 | Field Pardon / + | Both cost3 Authority for synthetic success, once only, card Exhausts |
| LT-021 | Synthetic vs natural success | One synthetic success may trigger existing success Rites once, not two obligations |
| LT-022 | Heave spend & hit order | First hit uses full Strength, second uses reduced Strength |
| LT-023 | Armsmaster declarative payment | Declared2 with eligible Strength actually1, full card effect2, event StrengthPaid(actual1) |
| LT-024 | Forced March negative prevention | Start Str0, gain3, spend2, end-turn remove only Str1, never -2 |
| LT-025 | Sword Knot / Full Tang actual-only | With Armsmaster reduction, both read real spend1 rather than declared2 |
| LT-026 | Spur Buckle surcharge | Additional Strength spend may grant Energy but not another CardStrengthPaid |
| LT-027 | OG first hit | OG3 adds +6 raw only to first actual enemy Attack hit, consumes OG |
| LT-028 | Open Guard phase expiry | On no actual enemy Attack hit, OG expires next player-turn start |
| LT-029 | OG marginal HP attribution | +6 raw OG, +2 actual HP → Broken Nose/Blood Debt/Brandy track2 |
| LT-030 | No Surrender success | Has OG, real enemy Attack, no unblocked HP damage →+2 Authority |
| LT-031 | Dare opposite case | Needs actual HP loss from targeted attack, no success if fully Blocked |
| LT-032 | Split Chinstrap | OG consumed by hit with 0 HP damage → queued +1 Authority |
| LT-033 | Parade Spur patch | Buff-only enemy phase generates no Authority from extra OG wager |
| LT-034 | Pike Square | Str0 reduces up to4 first hit per enemy, max retaliation12 per enemy |
| LT-035 | Open Visor fallback | With 0 OG, may gain1 to enable Block bypass; bypass ≠ block removal |
| LT-036 | Scarlet Epaulette | Extra hit same Deed instance; Marked Man yes, Second Blade no |
| LT-037 | Parade Ground active | Is Working, converts unspent Energy to Ward Wax only after own-turn OrderSuccess |
| LT-038 | Blood Debt active | Is Working, tracks first marginal OG loss only next enemy phase |
| LT-039 | Reprimand active | Is Working, natural Order failure history yes; abandon/replacement no |
| LT-040 | Brandy Flask | Victory heal floor(actual OG incremental HP /2), capped6 and by maxHP |
| LT-041 | Watch Surgeon | Once per cycle heal with positive Authority payment even on synthetic success, obey HP cap |
| LT-042 | Last Ditch corrected | At full HP base Deed useful; optional OG doubles only missing-HP portion |
| LT-043 | Ultimatum Doubt | Spend2 Authority→apply exactly2 Doubt; never modify target Strength |
| LT-044 | General Pool common count | General50 =0 Common /31 Uncommon /19 Rare in archived snapshot |
| LT-045 | Event recursion ceiling | Cross-card Order/Challenge/relic chains terminate, terminal events exactly once |
| LT-046 | Character isolation | Lieutenant-only statuses/actions don't affect other class pools |

---

## 9. Outstanding balance tasks and release gates

**Ready for a first vertical slice of implementation:** Cards/relics are fully specified and many prior contradictions resolved. But declaring 'balanced' or 'fully implemented' would be unsupported. Required next steps:

1. Inspect current RogueDeck-Core handling of `Deed/Working/Rite`, positive/negative Strength, CardPlayed vs Deed-hit, damage stages, Block/Ward Wax, Status immunity and existing Doubt. Do not assume archive presentation text is an engine schema.
2. Implement and unit-test Order/Challenge lifecycle **before** entering all cards. The event/instance-ID model is a dependency, not an optional UI flourish.
3. Implement Open Guard as a first-hit exposure with a consistent **marginal HP attribution** routine (Brandy Flask/Blood Debt must agree). Use deterministic resolution and visible preview.
4. Patch/prototype core starter and one Challenge/Order; test 10 prewritten turn traces. Only then import all 84 cards and 18 relics.
5. Measure early **Strength source density** and rarity draws. Whetstone/Heave swap must be visible in reward data. Check general-pool applicability; archived contents don't establish current generator probabilities.
6. Compare DPS and survivability over **Acts I–V**, including multi-hit enemies, single bosses, enemy Block, high-Strength scaling, status-immunity fights and long combat.
7. Test health sustain with OG/Scar/Watch Surgeon/Brandy Flask/Ward Wax and per-cycle authority gain to prevent a runaway HP/Strength engine.
8. Verify rare cards' play patterns: no universal obligatory Rare Rite; at least strong post-audit active choices (Parade Ground, Blood Debt, Reprimand) and early fallback (Pike Square, Open Visor, Service Record, Last Ditch).
9. Review language of all card/tooltips with actual game UI (especially longer Challenges) after rules are implemented. Short display keywords can link to full tooltip conditions; do not hide critical deadline information.
10. Conduct first-pass numerical tuning and scripted outcome tests; edit numbers without silently changing semantics, named card inventory or user-selected names.

### Separate status labels

- **Content complete:** 84 cards + 18 relics authored with upgrades and acquisition categories — **yes**.
- **Audit changes incorporated:** rarity swap, No Running, Forced March, Armsmaster, Field Pardon, Pike Square, Open Visor, Parade Spur, shifted Rites, deadline clarifications — **yes**.
- **Integration verified against current engine:** **no**.
- **All cards balanced against every encounter:** **no**.
- **General Pool copied/overwritten:** **no**; present only as external references.

