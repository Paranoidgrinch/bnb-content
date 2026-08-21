# Bureaucrats and Broomsticks
## Master Standard Encounter Pools — Acts I–IV

**Status:** Final post-audit canonical standard-combat master; identities and encounter structure locked pending implementation playtesting  
**Source:** The individually audited and curated Act I–IV standard-enemy / encounter-pool documents  
**Total unique standard-enemy identities:** 110  
**Total standard encounter templates:** 162

---

## Global Overview

| Act | Theme | Unique identities | Encounter templates |
|---|---|---:|---:|
| I | The City | 25 | 32 |
| II | The Endless Archives | 25 | 35 |
| III | The Green Docket | 25 | 40 |
| IV | The Licensing Labyrinth | 35 | 55 |
| **Total** |  | **110** | **162** |

### Final naming rule

Enemy identities should read primarily as **names, roles, creatures, objects or titles**, not as miniature descriptions of their own passive. Repeated constructions such as `The X That Y`, `X Without a Y`, `The X Who Y`, `X That Remembers Y` and `X That Refuses Y` are avoided for final top-level enemy identities. Folklore, absurdity and poetry should come from the chosen noun image or title rather than from a repeated relative-clause template.

The final audit therefore favors forms such as `Dead-Letter Ouroboros`, `Crabwise Shelf`, `Orphan Citation`, `Reckoning Hedge`, `Errant Boundary Stone` and `Oathbound Gate`. Encounter titles may still use sentence-like scene language where that serves the encounter as a vignette; **identity names do not rely on that pattern by default**.

### Final support-first rule

If an enemy's signature fundamentally requires another participant, that identity is not forced into a fake solo encounter. Support-first enemies are paired with existing bodies so their defining rule is visible in the encounter in which they are introduced.

### Act V — The Divine Ledger

Act V intentionally has **no standard-enemy encounter pool**.

Its combat structure is the already-finalized boss gauntlet:

> three extreme boss fights selected from the six-god boss pool, with the act structured around those divine confrontations rather than standard or elite encounters.

Therefore this master standard-encounter document contains Acts I–IV only.

---

## Canonical Use

This file consolidates the four individually redesigned standard-combat documents into one reference.

The individual Act documents remain useful as focused working files, but for cross-act review this master file is the canonical consolidated view of:

- standard-enemy identities;
- signature mechanics;
- encounter templates;
- recurrence rules;
- cross-enemy interaction semantics;
- act-specific cleanup and audit decisions.

Act I already contains concrete implementation values. Acts II–IV now additionally contain provisional HP and intent bands in the **Master Combat Balance Appendix** at the end of this file. These ranges are implementation targets, not immutable final balance numbers.

---


---

## Act I — The City

**Curated pool:** 25 unique identities / 32 encounter templates

**Status:** Final curated design pass before implementation balance testing  
**Pool:** 25 unique standard-enemy identities / 32 encounter templates  
**Structure:** 8 combat stages, 4 standard encounter templates per stage  
**Core rule:** Each standard enemy has one clear signature mechanic. Complexity comes from combinations, not from long individual rules text.

---

## 1. Shared Act-I Vocabulary

Act I uses the established statuses:

- Panic
- Doubt
- Paperwork
- Fatigue
- Strength

In addition, enemies gain one explicit anti-Paperwork status:

### Bookworm X

**Positive enemy status.**

Immediately before that enemy's Paperwork would resolve:

1. remove up to **X Paperwork** from that enemy;
2. remove exactly the same number of **Bookworm** stacks.

If the enemy has no Paperwork:

> Bookworm remains.

Example: 5 Paperwork + 2 Bookworm → 3 Paperwork + 0 Bookworm before Paperwork resolves.

#### Bookworm rules

- removes only enemy Paperwork;
- does not prevent Paperwork from being applied;
- can be applied to self or allies;
- suggested maximum: 3;
- is the **only standard Act-I enemy mechanic that removes enemy Paperwork**.

---

## 2. Encounter Structure

| Stage | Solo | Duo | Total |
|---|---:|---:|---:|
| Queue | 3 | 1 | 4 |
| Counter | 3 | 1 | 4 |
| Form | 4 | 0 | 4 |
| Seal | 2 | 2 | 4 |
| Ordinance | 3 | 1 | 4 |
| Delay | 3 | 1 | 4 |
| Appeal | 2 | 2 | 4 |
| Enforcement | 3 | 1 | 4 |
| **Total** | **23** | **9** | **32** |

---

## STAGE 1 — QUEUE

### 1. A Very Official Line
**Solo HP:** 29  
**Role:** card-count pressure

#### Passive — The Queue Advances
If the player ends their turn after playing **3+ cards**, gain 1 **Queue Position**. Maximum 3.

At 3:
> replace the next normal intent with **Everyone Moves at Once**, then Queue Position → 0.

#### Intents
**Shuffle Forward** — 5 damage + 1 Panic.  
**Compress the Queue** — 8 Block.  
**Shuffle Backward for Administrative Reasons** — 7 damage + 5 Block.  
**Everyone Moves at Once** — 12 damage + 1 Panic.

---

### 2. Number-Ticket Wisp
**Solo HP:** 25  
**Role:** Panic interaction

#### Passive — Your Number Came Up
Whenever Panic is removed through its **normal consumption/decay rule**:
> Wisp takes 4 direct damage.

Separate cleansing does not trigger this.

#### Intents
**Miscalled Number** — 5 damage + 1 Panic.  
**Ticket Flicker** — 8 damage.  
**Skip Ahead** — 6 Block + 1 Panic.

---

### 3. Queue-Crier Homunculus
**Solo HP:** 31  
**Role:** Panic cash-out

#### Passive — Lost Your Place
Direct attacks gain +3 damage per Panic on the player, maximum +9. Panic is not consumed.

#### Intents
**Recite the Waiting Order** — 6 damage + 1 Panic.  
**Call a Number That Is Not Yours** — 7 damage, modified by Lost Your Place.  
**Move Everyone Back One Place** — 8 Block + 1 Panic.

---

### 26. Duo — The Line Has Started Moving
- A Very Official Line — **19 HP**
- Queue-Crier Homunculus — **21 HP**

The Line rewards shorter turns; the Crier makes accumulated Panic dangerous.

---

## STAGE 2 — COUNTER

### 4. Wrong-Window Scribe
**Solo HP:** 34  
**Role:** card-type sequencing

#### Passive — Not This Counter
The first non-Junk card type each player turn becomes **Wrong Window**.

The first later card of that same type:
> Scribe gains 5 Block.

Maximum once per player turn.

#### Intents
**Ask for Another Form** — 8 damage + 1 Doubt.  
**Send You Next Door** — 8 Block.  
**Stamped Rejection** — 12 damage.

---

### 5. Receipt-Eyed Clerk
**Solo HP:** 35  
**Role:** Doubt cash-out

#### Date Discrepancy
> 6 damage +2 per current Doubt, maximum +8.

Doubt is not removed.

#### Intents
**Ask for Proof** — 1 Doubt.  
**Receipt Lash** — 10 damage.  
**Reconcile the Date** — 7 Block + 1 Doubt.  
**Date Discrepancy** — resolve formula above.

---

### 6. Triplicate Examiner
**Solo HP:** 41  
**Role:** third-copy punishment

#### Passive — Three Copies Required
Record the first non-Junk card type of the turn.

When the player plays the **third card of that type**:
- Examiner gains 8 Block;
- player gains 1 Doubt.

Maximum once per player turn.

#### Intents
**Compare the Copies** — 8 damage.  
**Archive the Second Copy** — 10 Block.  
**The Third Copy Contradicts** — 6 damage twice.

---

### 27. Duo — Wrong Window, Same Queue
- Wrong-Window Scribe — **24 HP**
- A Very Official Line — **20 HP**

The returning Line pressures card quantity while the Scribe pressures card-type repetition.

---

## STAGE 3 — FORM

Stage 3 has four solos and no duo so the player can learn the Paperwork/Bookworm ecosystem cleanly.

### 7. Filing Beetle
**Solo HP:** 40  
**Role:** Bookworm tutorial

#### Intents
**Worm-Eaten Folio** — gain 2 Bookworm + 6 Block.  
**Mandible Stamp** — 10 damage.  
**Mandatory Attachment** — apply 2 Paperwork to the player.

---

### 8. Unsigned Form Ghost
**Solo HP:** 43  
**Role:** Paperwork threshold vulnerability

#### Passive — Still Missing a Signature
While the Ghost has fewer than **3 Paperwork**:
> takes 25% less direct card damage.

At 3+ Paperwork:
> reduction is disabled.

If Bookworm later lowers it below 3:
> reduction returns.

#### Intents
**Missing Signature** — player +3 Paperwork.  
**Spectral Initial** — 9 damage.  
**Reject the Filing** — gain 1 Bookworm + 8 Block.

---

### 9. Duplicate Copy Mites
**Solo HP:** 37  
**Role:** Bookworm support

#### Passive — Carbon Copies
The first time each round another enemy gains Bookworm:
> Mites gain 4 Block.

#### Intents
**Spread Through the Binding** — every living enemy gains 1 Bookworm; Mites gain 2 instead.  
**Carbon-Paper Bites** — 3 damage three times.  
**Loose Copy** — player +2 Paperwork.  
**Scuttle Between Pages** — 8 Block.

---

### 10. Blank-Line Leech
**Solo HP:** 45  
**Role:** Paperwork retaliation

#### Passive — Feed on the Filed Margin
For every 2 Paperwork on the Leech:
> direct attacks deal +2 damage, maximum +8.

Paperwork is not removed by this passive.

#### Intents
**Blank-Space Bite** — 8 damage, modified by Feed on the Filed Margin.  
**Suck the Ink Dry** — gain 2 Bookworm.  
**Incomplete Field** — player +2 Paperwork.

---

## STAGE 4 — SEAL

### 11. Wax Notary
**Solo HP:** 48  
**Role:** Paperwork → defense

#### Passive — Paper Seals Wax
The first time each player turn Wax Notary receives Paperwork:
> gain 5 Block.

Paperwork remains. Maximum once per player turn.

#### Intents
**Drip Hot Wax** — 7 damage + 1 Paperwork to the player.  
**Harden the Seal** — 12 Block.  
**Notarial Mallet** — 13 damage.

---

### 12. Sealed Door Ward
**Solo HP:** 56  
**Role:** permanent break window

#### Passive — One Remaining Seal
Begins combat with **Seal active**.

While active:
> the first card hit against the Ward each player turn deals 4 less damage.

If the player deals at least **18 HP damage** to the Ward during one player turn:
- Seal breaks permanently;
- Ward takes 6 direct damage.

#### Intents
**Barred Slam** — 13 damage.  
**Seven Wax Seals** — 14 Block.  
**Sealed Threshold** — player +1 Paperwork; Ward +8 Block.

---

### 13. Oath Candle
**Solo-equivalent HP:** 39  
**Role:** defensive support / support-first identity

Oath Candle is not used as a solo encounter. Its signature exists to amplify another enemy's defenses, so every canonical appearance includes a partner.

#### Passive — Witness the Seal
The first time each round another enemy gains Block:
> that enemy gains 3 additional Block.

No recursion.

#### Intents
**Blue-Flame Prick** — 8 damage.  
**Hold the Oath** — all living enemies gain 5 Block.  
**Smoke-Written Oath** — player +1 Doubt +1 Paperwork.

### Support Encounter — Witness at the Sealed Threshold
- Oath Candle — **27 HP**
- Sealed Door Ward — **39 HP**

The Ward supplies the defensive event that makes `Witness the Seal` visible immediately. The player can break the Ward's Seal to reduce the anchor while the Candle rewards every defensive reset.

---

### 28. Duo — Certified Pest Control
- Wax Notary — **34 HP**
- Duplicate Copy Mites — **26 HP**

The Notary gets immediate defense from the first Paperwork application each turn; the returning Mites distribute Bookworm to erase future enemy Paperwork.

---

## STAGE 5 — ORDINANCE

### 14. Contradictory Signpost
**Solo HP:** 49  
**Role:** player chooses the next consequence

#### Passive — Both Directions Mandatory
After normal draw, show two directions whenever both are reachable:

- **LEFT — Attack**
- **RIGHT — Skill**

The first matching non-Junk card played determines the Signpost's next special intent.

#### LEFT
**Dangerous Shortcut** — 15 damage.

#### RIGHT
**Long Administrative Route** — 9 damage + 9 Block.

#### Neither
**No Route Listed** — player +1 Doubt +2 Paperwork.

#### Fallback
**Turn in Place** — 8 Block.

---

### 15. Exception Imp
**Solo HP:** 40  
**Role:** enemy that partially helps the player

#### Passive — Loophole
The first time each round the enemy side would apply a negative status to the player:
> reduce that application by 1 stack.

If only 1 stack would be applied:
> prevent it completely.

Whenever Loophole reduces at least one stack:
> Exception Imp gains 1 Strength.

#### Intents
**Technicality** — 9 damage.  
**Hide in the Footnote** — 8 Block.  
**Exception to the Exception** — another enemy gains 7 Block; if solo, Imp gains 10 Block.

---

### 16. Old Statute Ghost
**Solo HP:** 54  
**Role:** status recurrence

#### Passive — Still in Force
The first time each round one of the following fully disappears from the player:
- Panic
- Doubt
- Fatigue

gain 1 **Precedent** and remember the most recently disappeared eligible status.

At 2 Precedent:
> apply 1 stack of the remembered status to the player.

Then:
> Precedent → 0.

#### Intents
**Ancient Penalty** — 10 damage + 1 Paperwork.  
**Still in Force** — 1 Doubt + 10 Block.  
**Repealed Never** — 15 damage.

---

### 29. Duo — Exception to an Ancient Rule
- Exception Imp — **29 HP**
- Old Statute Ghost — **38 HP**

The Ghost reasserts expired bureaucracy. The Imp can reduce those status applications through Loophole, but grows stronger every time the exception fires.

---

## STAGE 6 — DELAY

### 17. Inverted Hourglass
**Solo HP:** 51  
**Role:** Fatigue storage

#### Resource — Stolen Sand
Whenever Fatigue actually reduces the player's available Energy:
> gain 1 Stolen Sand.

Maximum 3.

#### Signature — Turn the Glass
> 8 damage +4 per Stolen Sand.

Then:
> Stolen Sand → 0.

#### Other intents
**Upward Sand** — player +1 Fatigue.  
**Glass Stillness** — 12 Block.  
**Falling Upward** — 11 damage.

---

### 18. Fading Number Token
**Solo HP:** 43  
**Role:** self-expiring enemy

#### Passive — Your Number Is Fading
At end of each enemy turn:

If the player has **no Fatigue**:
> Token loses 3 HP.

If the player has Fatigue:
> nothing happens.

The Token never heals through this passive.

#### Intents
**Number Fades** — 7 damage + 1 Fatigue.  
**Sharp Edge** — 10 damage.  
**Unreadable Digit** — 1 Fatigue + 1 Doubt.

---

### 19. Minute Moth
**Solo HP:** 36  
**Role:** zero-Energy pressure

#### Resource — Stolen Minute
If the player ends their turn with exactly **0 Energy**:
> gain 1 Stolen Minute.

Maximum 2.

At 2:
> replace the next normal intent with **Wingbeat Delay**.

#### Signature — Wingbeat Delay
> 8 damage + 1 Fatigue.

Then:
> Stolen Minutes → 0.

#### Other intents
**Nibble the Hour** — 7 damage.  
**Dusty Wings** — 7 Block.  
**Land on the Clock** — 5 damage + 5 Block.

---

### 30. Duo — The Hour Has Not Been Called
- Inverted Hourglass — **36 HP**
- Fading Number Token — **31 HP**

The Token applies Fatigue and survives longer while Fatigue remains. When that Fatigue actually removes Energy, the Hourglass stores the lost time as Stolen Sand.

---

## STAGE 7 — APPEAL

### 20. Counterclaim Imp
**Solo HP:** 45  
**Role:** anti-debuff target

#### Passive — Counterclaim
The first time each player turn the player directly applies a negative status to Counterclaim Imp:
> player gains 1 Paperwork.

Maximum once per player turn.

#### Intents
**Red-Ink Claim** — 9 damage + 1 Paperwork.  
**Spiteful Filing** — 8 Block.  
**Countersuit** — 13 damage.

---

### 21. Self-Correcting Record
**Solo HP:** 53  
**Role:** adapts to recent damage type

#### Passive — Correct Against the Evidence
The first time each player turn one card deals at least **10 HP damage** to the Record:
> remember that card's type.

The next damaging card of the same type against the Record that turn:
> deals 4 less damage.

Then clear the correction.

Maximum once per player turn.

#### Intents
**Rewrite the Statement** — 10 damage + 1 Doubt.  
**Correct Against You** — 10 Block.  
**Record Snap** — 15 damage.

---

### 22. Sustaining Gavel
**Solo-equivalent HP:** 44  
**Role:** ally amplifier / support-first identity

Sustaining Gavel is not used as a solo encounter. Its identity is the act of copying another enemy's gained Block or Strength; a solo fallback would turn it into an ordinary damage body and is therefore intentionally avoided.

#### Passive — Sustained
The first time each round another living enemy gains Block or Strength:
> copy half the amount gained, rounded down.

Block copies as Block. Strength copies as Strength. No recursion.

#### Intents
**Sustained Strike** — 11 damage.  
**Order in the Chamber** — all living enemies gain 4 Block.  
**Strike the Desk** — 14 damage.

### Support Encounter — Sustained Counterclaim
- Sustaining Gavel — **30 HP**
- Counterclaim Imp — **33 HP**

Counterclaim Imp's defensive turns give the Gavel something meaningful to copy, while `Order in the Chamber` can reinforce the Imp and feed the Gavel's own response. The encounter teaches the player to read support amplification rather than simply focus the newest body.

---

### 31. Duo — The Evidence Exists in Triplicate
- Self-Correcting Record — **40 HP**
- Triplicate Examiner — **30 HP**

The returning Examiner punishes reaching the third card of the opening type. The Record punishes following one large hit with another damaging card of the same type.

---

## STAGE 8 — ENFORCEMENT

### 23. Warrant Bailiff
**Solo HP:** 58  
**Role:** player-Paperwork cash-out

#### Passive — Outstanding Warrant
While the player has at least **4 Paperwork**:
> Bailiff's direct attacks deal +5 damage.

Paperwork is not removed.

#### Intents
**Serve the Warrant** — 13 damage.  
**Official Stance** — 11 Block + 1 Strength.  
**Iron Notice** — 8 damage + 1 Paperwork.

---

### 24. Threshold Seizure Ward
**Solo HP:** 61  
**Role:** strongest normal Bookworm support

#### Passive — Seize the Filing
The first time each round the player applies Paperwork to any enemy:
> that affected enemy gains 1 Bookworm.

Maximum once per round total.

#### Intents
**Seize the Threshold** — 10 damage + 8 Block.  
**Lawful Hold** — 14 Block.  
**Quarantine the Docket** — another living enemy gains 2 Bookworm; if solo, Ward gains 2 Bookworm.

---

### 25. Civic Battering Ram
**Solo HP:** 69  
**Role:** Momentum threat

#### Resource — Momentum
Maximum 4.

**Build Momentum** — gain 2 Momentum + 10 Block.  
**Ram the Case** — 11 damage +4 per Momentum, then Momentum → 0.  
**Back Up** — 10 damage + 1 Strength.

#### Counterplay — Break the Approach
The first time each player turn the Ram's entire Block is removed through card damage:
> Momentum −1, minimum 0.

---

### 32. Duo — The Warrant Seizes the Docket
- Warrant Bailiff — **43 HP**
- Threshold Seizure Ward — **45 HP**

The Bailiff punishes Paperwork on the player. The Ward counters Paperwork applied by the player to enemies.

---

## 5. Duo HP Scaling

Duo bodies use approximately **65–75%** of their solo HP depending on role.

| Duo | Enemy A | HP | Enemy B | HP | Total |
|---|---|---:|---|---:|---:|
| The Line Has Started Moving | Very Official Line | 19 | Queue-Crier | 21 | 40 |
| Wrong Window, Same Queue | Wrong-Window Scribe | 24 | Very Official Line | 20 | 44 |
| Witness at the Sealed Threshold | Oath Candle | 27 | Sealed Door Ward | 39 | 66 |
| Certified Pest Control | Wax Notary | 34 | Duplicate Copy Mites | 26 | 60 |
| Exception to an Ancient Rule | Exception Imp | 29 | Old Statute Ghost | 38 | 67 |
| The Hour Has Not Been Called | Inverted Hourglass | 36 | Fading Number Token | 31 | 67 |
| Sustained Counterclaim | Sustaining Gavel | 30 | Counterclaim Imp | 33 | 63 |
| Evidence Exists in Triplicate | Self-Correcting Record | 40 | Triplicate Examiner | 30 | 70 |
| Warrant Seizes the Docket | Warrant Bailiff | 43 | Threshold Seizure Ward | 45 | 88 |

These are implementation starting points, not immutable final balance.

---

## 6. Bookworm Distribution Audit

Only five core identities interact directly with Bookworm:

1. **Filing Beetle** — self-protection/tutorial.
2. **Unsigned Form Ghost** — self-protection that can close its Paperwork vulnerability.
3. **Duplicate Copy Mites** — team Bookworm support.
4. **Blank-Line Leech** — self-cleanses Paperwork because Paperwork also increases its offense.
5. **Threshold Seizure Ward** — late dedicated anti-Paperwork support.

No other standard Act-I enemy removes enemy Paperwork through another route.

This keeps Paperwork viable while ensuring the player cannot assume every stack will persist forever.

---

## 7. Recurrence Map

| Returning enemy | Debut | Return |
|---|---|---|
| A Very Official Line | Stage 1 | Stage 2 duo |
| Duplicate Copy Mites | Stage 3 | Stage 4 duo |
| Triplicate Examiner | Stage 2 | Stage 7 duo |

The default philosophy is:

> familiar enemy + new context > entirely new body.

---

## 8. Final Roster

### Queue
1. A Very Official Line
2. Number-Ticket Wisp
3. Queue-Crier Homunculus

### Counter
4. Wrong-Window Scribe
5. Receipt-Eyed Clerk
6. Triplicate Examiner

### Form
7. Filing Beetle
8. Unsigned Form Ghost
9. Duplicate Copy Mites
10. Blank-Line Leech

### Seal
11. Wax Notary
12. Sealed Door Ward
13. Oath Candle

### Ordinance
14. Contradictory Signpost
15. Exception Imp
16. Old Statute Ghost

### Delay
17. Inverted Hourglass
18. Fading Number Token
19. Minute Moth

### Appeal
20. Counterclaim Imp
21. Self-Correcting Record
22. Sustaining Gavel

### Enforcement
23. Warrant Bailiff
24. Threshold Seizure Ward
25. Civic Battering Ram

---

## 9. Final Encounter Pool

#### Stage 1 — Queue
1. A Very Official Line
2. Number-Ticket Wisp
3. Queue-Crier Homunculus
4. The Line Has Started Moving

#### Stage 2 — Counter
5. Wrong-Window Scribe
6. Receipt-Eyed Clerk
7. Triplicate Examiner
8. Wrong Window, Same Queue

#### Stage 3 — Form
9. Filing Beetle
10. Unsigned Form Ghost
11. Duplicate Copy Mites
12. Blank-Line Leech

#### Stage 4 — Seal
13. Wax Notary
14. Sealed Door Ward
15. Witness at the Sealed Threshold
16. Certified Pest Control

#### Stage 5 — Ordinance
17. Contradictory Signpost
18. Exception Imp
19. Old Statute Ghost
20. Exception to an Ancient Rule

#### Stage 6 — Delay
21. Inverted Hourglass
22. Fading Number Token
23. Minute Moth
24. The Hour Has Not Been Called

#### Stage 7 — Appeal
25. Counterclaim Imp
26. Self-Correcting Record
27. Sustained Counterclaim
28. The Evidence Exists in Triplicate

#### Stage 8 — Enforcement
29. Warrant Bailiff
30. Threshold Seizure Ward
31. Civic Battering Ram
32. The Warrant Seizes the Docket

---

## 10. Balance Watchlist

- **Bookworm density:** must counter Paperwork without invalidating the Bureaucrat archetype.
- **Unsigned Form Ghost:** test whether 3 Paperwork is the correct vulnerability threshold.
- **Blank-Line Leech:** ensure Paperwork retaliation feels risky but not punitive to the archetype.
- **Exception Imp:** Loophole must be useful enough that “kill it last” is sometimes correct.
- **Old Statute Ghost:** Precedent 2 may trigger too frequently depending on normal status decay.
- **Inverted Hourglass:** Stolen Sand needs enough warning before a large cash-out.
- **Threshold Seizure Ward:** strongest standard anti-Paperwork source; keep late.
- **Civic Battering Ram:** Momentum values depend strongly on average Act-I damage output.
- **Duo HP:** verify action-economy pressure before raising HP.

---

## 11. Canonical Verdict

Act I standard combat is now built around:

- **25 memorable enemy identities**
- **9 designed duo encounters**
- **32 total standard encounter templates**
- **4 encounters per combat stage**

The roster intentionally trades raw novelty for:

- recognition;
- mastery;
- recurrence;
- clearer enemy character;
- stronger combination design.

> **New combinations before new bodies.**


---

## Act II — The Endless Archives

**Curated pool:** 25 unique identities / 35 encounter templates

**Status:** Canonical content-design snapshot  
**Scope:** Standard enemies and standard encounters only  
**Unique enemy identities:** 25  
**Encounter templates:** 35  
**Core principle:** Fewer enemy identities, more meaningful recurrence and recombination.

---

## 1. Act-II Identity

Act II attacks the deck as an archival object. The archive classifies, misplaces, references, revises, restricts, schedules, certifies and cross-references.

The redesign compresses the former oversized roster around one principle:

> **New combinations before new bodies.**

The player should increasingly recognize enemies and understand their rules, then be challenged by seeing familiar rules interact in new configurations.

---

## 2. Universal Act-II Mechanics

### Overdue
Source-bound negative player status.

At **2 Overdue from the same source**:
1. remove 2 Overdue from that source;
2. apply 1 Paperwork;
3. resolve that source's visible **Late Consequence**.

If the source dies, its remaining Overdue disappears. Overdue may trigger repeatedly.

### Misfiled
Negative mark on a concrete card instance.

When a Misfiled card would next be drawn:
1. it does not enter the hand;
2. it goes directly to discard unless a specific enemy rule changes that destination;
3. draw a replacement card;
4. remove Misfiled.

Misfiled persists through zones until the card's next draw. A card cannot hold more than one Misfiled mark.

### Referenced
Source-bound mark on a concrete card instance.

If the Referenced card is played, the Reference is fulfilled and clears after the card fully resolves.

If the Referenced card leaves the hand unplayed, the Reference clears and gives 1 Overdue from its source.

If the source dies, its remaining References disappear. Multiple different sources may Reference the same card.

### Redacted
Negative mark on a concrete card instance.

On its next play, positive numerical effects are reduced by 50%, rounded down. Then Redacted clears.

Redacted reduces damage, Block, healing, draw, Energy gain, positive player statuses and negative enemy statuses.

It does not reduce Energy cost, hit count, target count, generated-card count, Retain, Exhaust, movement, conditions, percentages or negative self-effects.

Redacted may coexist with Misfiled and Referenced.

---

## 3. Final Stage Structure

| Stage | New identities | Solo | Combination | Total |
|---|---:|---:|---:|---:|
| 1 — Hall of Returns | 3 | 3 | 1 | 4 |
| 2 — Misfiled Stacks | 3 | 3 | 1 | 4 |
| 3 — Whispering Catalogue | 3 | 3 | 1 | 4 |
| 4 — Hushed Reading Room | 3 | 3 | 1 | 4 |
| 5 — Redaction Galleries | 3 | 3 | 1 | 4 |
| 6 — Scriptorium of Errata | 2 | 2 | 1 | 3 |
| 7 — Restricted Annex | 2 | 2 | 1 | 3 |
| 8 — Archive of Misplaced Hours | 2 | 2 | 1 | 3 |
| 9 — Necrology Vaults | 2 | 1 | 2 | 3 |
| 10 — Hall of Concordances | 2 | 1 | 2 | 3 |
| **Total** | **25** | **23** | **12** | **35** |

---

## STAGE 1 — THE HALL OF RETURNS

### 1. Brass Maw of Returns
**Role:** Overdue introduction / delayed cash-out.

#### Signature — Return Parcel
Whenever Brass Maw resolves its Delinquency:
> gain 1 **Return Parcel**, maximum 2.

Its next direct attack gains:
> +5 damage per Return Parcel.

After that attack:
> Return Parcel → 0.

The Maw stores returned lateness and spits it back later.

### 2. Object Listed as “Other”
**Role:** classification pressure.

#### Signature — Miscellaneous Classification
The first non-Junk card type played each player turn becomes **Recognized Category**.

If the player later plays at least one different card type that turn:
> Object loses 5 Block; if it has no Block, it takes 3 direct damage instead.

If the player ends the turn having played only the Recognized Category:
> Object gains 6 Block.

### 3. Dead-Letter Ouroboros
**Role:** recursive Overdue.

#### Signature — Return to Sender
Whenever this enemy's Delinquency fully resolves:
> immediately apply 1 new Overdue from this same source.

Thus:
> 2 Overdue → Delinquency → 0 → immediately 1 Overdue.

### Encounter 4 — Returned as “Other”
**Brass Maw of Returns + Object Listed as “Other”**

The first standard multi-enemy Overdue encounter. Core lesson:
> Overdue is source-bound.

---

## STAGE 2 — THE MISFILED STACKS

### 4. Crabwise Shelf
**Role:** Misfiled destination manipulation.

#### Signature — Shelved Sideways
When a card Misfiled by this Shelf is skipped during draw:
> it goes to the bottom of the Draw Pile instead of discard.

Replacement draw still happens normally.

### 5. Volume Q-Null
**Role:** Misfiled propagation.

#### Signature — Null Reference
When a card Misfiled by Volume Q-Null is skipped, inspect the immediate replacement card.

If that replacement has the same persistent Base Cost:
> it also becomes Misfiled.

Maximum one propagation per original skip. No recursion.

### 6. Corridor in the Wrong Edition
**Role:** present access vs future access.

#### Signature — Wrong Edition
After normal draw, select one valid card in hand.

If the player plays it this turn, it resolves normally and then becomes Misfiled.

If it is not played, there is no additional consequence.

### Encounter 8 — Crabwise Return
**Crabwise Shelf + Dead-Letter Ouroboros**

Dead-Letter Ouroboros returns from Stage 1. Overdue recursion and future card-access disruption now coexist.

---

## STAGE 3 — THE WHISPERING CATALOGUE

### 7. Second-Person Entry
**Role:** repeating Reference chain.

#### Signature — You Are Cited Again
When the player fulfills a Reference from this Entry, remember the card type used.

After the next normal draw:
> Reference one valid card of that same type.

Maximum one follow-up Reference at a time.

### 8. Fanged Alphabet
**Role:** observed card-cost behavior → future Reference.

#### Signature — Learned Letter
If the player plays two consecutive cards with the same Base Cost:
> remember that cost class.

After the next normal draw:
> Reference one valid card of that Base Cost.

### 9. Orphan Citation
**Role:** flexible Reference fulfillment.

#### Signature — Reconstruct the Source
After normal draw, Reference one valid card.

The player may fulfill the Citation by either:
1. playing the exact Referenced card; or
2. playing another card with both the same Base Cost and the same card type.

If option 2 is used, the original Reference also clears successfully.

### Encounter 12 — Citation in Volume Q-Null
**Second-Person Entry + Volume Q-Null**

The Entry creates future card-type demand while Q-Null disrupts future draw access.

---

## STAGE 4 — THE HUSHED READING ROOM

### 10. Unclaimed Reading Table
**Role:** hand-pressure Quiet Rule.

#### Signature — Reserved Seat
After the player's **fourth played card** in a turn:
> the oldest remaining valid non-Junk card in hand goes directly to discard.

Maximum once per turn.

If that discarded card was Referenced:
> its Reference fails normally and creates Overdue.

### 11. Mute Margin
**Role:** shrinking card-play limit.

#### Signature — Shrinking Margin
Start with a visible limit of 5 cards.

If the player exceeds the limit:
> one remaining valid card in hand becomes Misfiled.

Then:
> future limit −1, minimum 3.

If the player completes a full turn without exceeding the limit:
> limit +1, maximum 5.

### 12. Choir of Unspoken Words
**Role:** end-turn hand-state puzzle.

#### Signature — Leave One Word Unspoken
If the player ends the turn with exactly 1 valid non-Junk card in hand:
> Choir loses 6 Block; if it has insufficient Block, excess converts to direct damage according to tuning.

If the player ends with 0 valid non-Junk cards:
> Choir gains 1 **Voice**.

At 2 Voice:
> its next direct attack gains +8 damage, then Voice → 0.

### Encounter 16 — Reserved Silence
**Unclaimed Reading Table + Second-Person Entry**

The Entry References a card. The Table may force that same card from the hand, naturally causing Reference failure and Overdue.

---

## STAGE 5 — THE REDACTION GALLERIES

### 13. Palimpsest Husk
**Role:** Redacted → future Misfiled.

#### Signature — Older Text Beneath
The first time each player turn a Redacted card is fully played:
> after resolution, that card becomes Misfiled.

### 14. Expunged Name
**Role:** repeated card-name punishment.

#### Signature — No Longer Recognized
The first time each player turn the player plays a card whose name has already been played earlier in the combat:
> that card becomes Redacted immediately before resolution.

Maximum once per player turn.

### 15. Vacant Portrait
**Role:** Redacted as offensive opportunity.

#### Signature — The Absence Becomes Visible
The first time each player turn the player plays a Redacted card:
> Portrait loses 8 Block.

If it has less than 8 Block:
> remaining value becomes direct damage.

### Encounter 20 — Absent Subject Citation
**Vacant Portrait + Second-Person Entry**

A Referenced card can also become Redacted. The player may therefore face a damaged compliance card that simultaneously opens the Portrait's defenses.

---

## STAGE 6 — SCRIPTORIUM OF ERRATA

### 16. Fatal Comma
**Role:** card-order puzzle.

#### Signature — Clause A / Clause B
After normal draw:
> mark two different valid cards as Clause A and Clause B.

If A is played before B:
> Fatal Comma takes 8 direct damage.

If B is played before A:
> Clause A becomes Redacted immediately before resolution.

If neither is played:
> apply 1 Overdue from Fatal Comma.

If only one is played:
> no additional consequence beyond the relevant first-order result.

### 17. Errata Doppelgänger
**Role:** moving Redaction.

#### Signature — Revision Pass
The first time each player turn the player would play a Redacted card:
> remove Redacted immediately before resolution.

The card resolves at full strength.

After it fully resolves:
> another valid card in hand becomes Redacted.

If no other valid card exists:
> no new Redaction is applied.

### Encounter 23 — The Comma Beneath the Palimpsest
**Fatal Comma + Palimpsest Husk**

Fatal Comma may Redact Clause A. If the player still uses it, Palimpsest can make it Misfiled afterward.

Natural chain:
> incorrect clause order → Redaction → future Misfiling.

---

## STAGE 7 — RESTRICTED ANNEX

### 18. Checkout Codex
**Role:** access restriction.

#### Signature — Behind the Desk
After normal draw:
> choose one valid non-Junk card and place it visibly **Behind the Desk**.

It is temporarily unavailable.

The player has three options.

#### Wait Properly
Play another card first.

Then:
> the Behind-the-Desk card returns normally to hand.

#### Demand Immediate Access
Use a free encounter action:
> return the card immediately to hand, but it becomes Redacted.

#### End the Turn Without Retrieval
> apply 1 Overdue from the Checkout Codex.

At the start of the next player turn:
> the card returns normally.

### 19. Mnemonic Chain
**Role:** persistent card-instance tracking.

#### Signature — Remembered Volume
The first eligible card played against the Chain:
> becomes a remembered concrete card instance.

When that exact instance later re-enters the hand:
- it becomes Referenced by the Chain;
- it costs +1 Energy for that turn.

If the player plays it anyway:
> Chain takes 8 direct damage.

If it leaves the hand unplayed:
> normal Reference failure generates Overdue.

After successful fulfillment:
> the remembered relation clears and a later card may become the next Remembered Volume.

### Encounter 26 — Borrower on Record
**Checkout Codex + Mnemonic Chain**

Checkout Codex controls short-term access. Mnemonic Chain controls long-term consequences when the same card instance returns.

---

## STAGE 8 — ARCHIVE OF MISPLACED HOURS

### 20. Unoccurred Tuesday
**Role:** missing enemy turn / damage window.

#### Signature — Tuesday Does Not Occur
Every third turn belonging to Unoccurred Tuesday:
> that turn does not happen.

During the missing turn:
- Tuesday takes no normal action;
- Tuesday's own Scheduled Intents do not advance;
- Tuesday gains no Block from its own turn;
- direct card damage against Tuesday is increased by 25%.

After the missing turn:
> its next guaranteed intent is **Resume on Wednesday**.

#### Resume on Wednesday
> Deal 18 damage.

### 21. Hourglass With Two Bottoms
**Role:** two simultaneous future threats.

#### Signature — Two Futures at Once
Maintain two visible Scheduled Intents:
- **Left Bottom**
- **Right Bottom**

with separate countdowns.

The first Attack each player turn may:
> increase Left Bottom countdown by 1, maximum 3.

The first Skill each player turn may:
> increase Right Bottom countdown by 1, maximum 3.

Each side may be delayed at most once per player turn.

### Encounter 29 — Two-Bottom Tuesday
**Unoccurred Tuesday + Hourglass With Two Bottoms**

Critical clarity rule:
> Unoccurred Tuesday pauses only itself.

The Hourglass's time proceeds normally.

---

## STAGE 9 — NECROLOGY VAULTS

### 22. Blank Death Certificate
**Role:** conditional self-return.

#### Death Clause — Certification Required
On the first lethal damage:

If the player fulfilled a Reference from this enemy during the same turn:
> death is certified and final.

Otherwise:
> Blank Death Certificate returns once at roughly 35% HP.

After this return:
> it cannot return again.

### 23. Spare-Life Jar
**Role:** interruptible ally resurrection.

This enemy is intentionally strongest as an encounter component rather than as a solo.

#### Signature — Spare Life
When another enemy dies:
> store that enemy's identity as **Stored Life**.

Then visibly prepare:

#### Pour Back the Life
> Countdown: 1 enemy turn.

If the Jar remains alive when the countdown resolves:
> return the stored enemy once at 30% Max HP.

Then:
> Stored Life is consumed.

If the Jar dies before resolution:
> Stored Life is lost.

Maximum:
> one resurrection per combat.

### Encounter 31 — Dead-Letter Revival
**Spare-Life Jar + Dead-Letter Ouroboros**

Dead-Letter Ouroboros returns from Stage 1. Its recursive Overdue identity is now paired with literal return from death.

### Encounter 32 — Revised Mortality
**Spare-Life Jar + Errata Doppelgänger**

Errata Doppelgänger revises textual mistakes. The Jar revises mortality.

---

## STAGE 10 — HALL OF CONCORDANCES

### 24. Detached Footnote
**Role:** dynamic partner support.

This enemy is intentionally not required to have a solo encounter.

#### Signature — Source Link
At combat start:
> Footnote links visibly to one other enemy.

That enemy becomes its **Source**.

#### Marginal Note
The first time each round the Source's signature mechanic actually triggers:
> Footnote gains 1 Note.

Maximum:
> 2 Notes.

At 2 Notes:
> its next direct attack becomes **See Note Below**.

#### See Note Below
> Deal 14 damage.  
> Apply 1 Overdue from Footnote.  
> Notes → 0.

#### Source death
If the current Source dies and another valid enemy remains:
> at the next legal enemy window, Footnote links to that enemy.

Then:
> Notes → 0.

If no Source remains:
> Footnote can no longer gain Notes.

### 25. Miscellany Index
**Role:** synthesis of all four universal Act-II mechanics.

#### Resource — Residue
The first time each round each of the following occurs:
- a Delinquency resolves;
- a Misfiled card is actually skipped;
- a Reference is fulfilled;
- a Redacted card is played;

gain:
> 1 Residue.

Maximum:
> 4.

At 4 Residue:

#### Everything Else
> Residue → 0.

Then:
- one valid card in hand becomes Redacted;
- another valid card becomes Misfiled.

If only one valid card exists:
> apply Redacted only.

### Encounter 34 — Orphan Citation, See Footnote
**Orphan Citation + Detached Footnote**

Footnote begins linked to Orphan Citation. `Reconstruct the Source` can feed its Notes through the normal Source rule.

### Encounter 35 — Everything Else, See Footnote
**Final standard encounter of Act II**

**Palimpsest Husk + Detached Footnote + Miscellany Index**

This is the only planned three-enemy standard encounter in Act II.

Footnote initially links to Palimpsest Husk.

A Redacted card can become Misfiled through Palimpsest, later feed Index through a Misfiled skip, and contribute toward the final synthesis trigger.

When Palimpsest dies:
> Footnote may re-link to Miscellany Index.

The encounter introduces no new vocabulary. It tests fluent use of everything learned in the act.

---

## 4. Final Enemy Roster

### Hall of Returns
1. Brass Maw of Returns
2. Object Listed as “Other”
3. Dead-Letter Ouroboros

### Misfiled Stacks
4. Crabwise Shelf
5. Volume Q-Null
6. Corridor in the Wrong Edition

### Whispering Catalogue
7. Second-Person Entry
8. Fanged Alphabet
9. Orphan Citation

### Hushed Reading Room
10. Unclaimed Reading Table
11. Mute Margin
12. Choir of Unspoken Words

### Redaction Galleries
13. Palimpsest Husk
14. Expunged Name
15. Vacant Portrait

### Scriptorium of Errata
16. Fatal Comma
17. Errata Doppelgänger

### Restricted Annex
18. Checkout Codex
19. Mnemonic Chain

### Archive of Misplaced Hours
20. Unoccurred Tuesday
21. Hourglass With Two Bottoms

### Necrology Vaults
22. Blank Death Certificate
23. Spare-Life Jar

### Hall of Concordances
24. Detached Footnote
25. Miscellany Index

---

## 5. Final 35 Encounter Templates

| # | Stage | Encounter |
|---:|---|---|
| 1 | Returns | Brass Maw of Returns |
| 2 | Returns | Object Listed as “Other” |
| 3 | Returns | Dead-Letter Ouroboros |
| 4 | Returns | Returned as “Other” |
| 5 | Misfiled | Crabwise Shelf |
| 6 | Misfiled | Volume Q-Null |
| 7 | Misfiled | Corridor in the Wrong Edition |
| 8 | Misfiled | Crabwise Return |
| 9 | Catalogue | Second-Person Entry |
| 10 | Catalogue | Fanged Alphabet |
| 11 | Catalogue | Orphan Citation |
| 12 | Catalogue | Citation in Volume Q-Null |
| 13 | Reading Room | Unclaimed Reading Table |
| 14 | Reading Room | Mute Margin |
| 15 | Reading Room | Choir of Unspoken Words |
| 16 | Reading Room | Reserved Silence |
| 17 | Redaction | Palimpsest Husk |
| 18 | Redaction | Expunged Name |
| 19 | Redaction | Vacant Portrait |
| 20 | Redaction | Absent Subject Citation |
| 21 | Errata | Fatal Comma |
| 22 | Errata | Errata Doppelgänger |
| 23 | Errata | The Comma Beneath the Palimpsest |
| 24 | Restricted | Checkout Codex |
| 25 | Restricted | Mnemonic Chain |
| 26 | Restricted | Borrower on Record |
| 27 | Hours | Unoccurred Tuesday |
| 28 | Hours | Hourglass With Two Bottoms |
| 29 | Hours | Two-Bottom Tuesday |
| 30 | Necrology | Blank Death Certificate |
| 31 | Necrology | Dead-Letter Revival |
| 32 | Necrology | Revised Mortality |
| 33 | Concordances | Miscellany Index |
| 34 | Concordances | Orphan Citation, See Footnote |
| 35 | Concordances | Everything Else, See Footnote |

---

## 6. Recurrence Map

| Returning enemy | Debut | Later encounter |
|---|---|---|
| Dead-Letter Ouroboros | Stage 1 | Stage 2, Stage 9 |
| Volume Q-Null | Stage 2 | Stage 3 |
| Second-Person Entry | Stage 3 | Stage 4, Stage 5 |
| Palimpsest Husk | Stage 5 | Stage 6, Stage 10 |
| Errata Doppelgänger | Stage 6 | Stage 9 |
| Orphan Citation | Stage 3 | Stage 10 |

The goal is:
> “I know this enemy.”

followed by:
> “But I have never had to solve it together with this one.”

---

## 7. Support-First Identities

### Spare-Life Jar
Best used as a visible resurrection support target. Its identity becomes meaningful when something else can die.

### Detached Footnote
Best used as a relationship enemy. Its entire fantasy depends on having a Source.

Forcing either into a mandatory solo encounter would weaken the concept.

---

## 8. Locked Design Principles

- **25 unique enemy identities**
- **35 encounter templates**
- **23 solo encounters**
- **12 combination encounters**
- exactly one planned three-enemy standard encounter
- later stages rely increasingly on recurrence and recombination
- no additional universal Act-II status systems
- the core vocabulary remains Overdue, Misfiled, Referenced and Redacted
- the final standard encounter introduces no new vocabulary

> **The Endless Archives become harder because everything begins referring to everything else.**

---

## 9. Balance Pass Still Outstanding

This document fixes:
- enemy identities;
- signature mechanics;
- encounter composition;
- recurrence structure.

It does **not yet permanently lock**:
- exact HP;
- exact damage;
- Block values;
- intent weights;
- cooldowns;
- countdown numbers;
- duo/trio HP scaling.

Those belong in the dedicated implementation balance pass.

---

## 10. Canonical Verdict

Act II standard combat now consists of:

> **25 unique standard enemies**

combined into:

> **35 standard encounter templates**

across:

> **10 stages**.

The act progresses from learning individual archive phenomena to understanding relationships between already-known archival rules.

> **New combinations before new bodies.**


---

## Act III — The Green Docket

**Curated pool:** 25 unique identities / 40 encounter templates

**Status:** Canonical content-design snapshot  
**Scope:** Standard enemies and standard encounters only  
**Unique enemy identities:** 25  
**Encounter templates:** 40  
**Core principle:** The Green Docket becomes harder through relationships, precedent, testimony, reciprocity and jurisdiction — not through endless new enemy bodies.

---

## 1. Act-III Identity

Act III is the act of customary law.

Its bureaucracy is not centralized in desks, seals or archives. It exists because:

- paths remember how they were used;
- stones remember where borders once stood;
- witnesses disagree about what happened;
- gifts create obligations;
- claims become communal;
- appeals suspend ownership;
- old behavior becomes precedent;
- multiple jurisdictions overlap;
- favors return as debts.

The defining design rule is:

> **Law exists because everyone remembers what everyone else did.**

Act III should therefore contain fewer unique enemy bodies than the original oversized roster, but significantly more recombination between known participants.

---

## 2. Universal Act-III Mechanics

### Safe-Conduct

Positive player status.

A player may spend 1 Safe-Conduct to prevent the full Trespass application from a concrete source.

Suggested maximum:

> 3.

Normal Act-III combats may begin with:

> 1 Safe-Conduct.

Specific enemies may grant additional Safe-Conduct.

---

### Trespass

Source-bound negative player status.

At:

> 3 Trespass from the same source

resolve:

1. remove those 3 Trespass;
2. create 1 Claim on that source.

Trespass does not directly deal damage.

---

### Claim

Persistent positive enemy resource.

Suggested maximum:

> 3 per enemy.

Claims represent recognized standing, right, entitlement or grievance.

Different enemies interpret or consume Claims differently.

Claims are deliberately not a universal damage multiplier.

The interesting question is:

> **What does this particular party believe its Claim allows it to do?**

---

### Wergild

Source-bound demand owed by the player.

A Wergild demand is due by the end of the next player turn.

The player may use the free encounter action:

> **Make Amends**

to pay one point of Wergild by either:

- spending 1 Energy; or
- discarding one eligible card.

If fully paid:

> player gains 1 Safe-Conduct.

If any amount remains unpaid:

> player takes 2 direct damage per unpaid point.

If the source is still alive:

> source gains 1 Claim.

---

## 3. New Canonical Claim Rules

These definitions are required to prevent ambiguous interactions and infinite loops.

### Newly Created Claim

A Claim counts as **newly created** only when it originates from:

- 3 Trespass from the same source;
- unpaid Wergild;
- an effect that explicitly says to create or gain a new Claim.

A newly created Claim may trigger abilities that listen for:

> a Claim being created.

---

### Claim Transfer

A transferred Claim:

> changes owner.

It is not considered newly created.

Therefore a transfer does **not** retrigger effects that listen for:

> newly created Claims.

This distinction prevents loops between effects such as:

- Boundary Stone transfer;
- Ditch Lamprey transfer;
- Bracken Moot Hearing;
- later jurisdiction effects.

---

### Claim Consumption

A Claim is consumed only when an effect explicitly spends or removes it as a cost.

Consumption is distinct from:

- transfer;
- review;
- copying;
- being counted;
- being protected.

This distinction is important for:

> Handworn Tally Coin.

---

## 4. Safe-Conduct Provenance

Most Safe-Conduct is mechanically identical.

However, specific enemies may need to know:

> who granted a particular Safe-Conduct stack.

This does **not** create a second status.

A Safe-Conduct stack may internally store:

> `granted_by = source`

when required.

Current canonical use:

> Roadside Witchling.

Only Safe-Conduct granted by the Witchling counts for her Courtesy rule.

Safe-Conduct granted by:

- Cup;
- Oath-Fish;
- Coin;
- other sources

does not satisfy Witchling Courtesy unless explicitly stated.

---

## 5. Final Encounter Structure

| Stage | Solo | Duo/Trio | Total |
|---|---:|---:|---:|
| 1 — Road of Permitted Turns | 2 | 2 | 4 |
| 2 — Surveyed Hedgerows | 1 | 3 | 4 |
| 3 — Meadow of Living Testimony | 0 | 4 | 4 |
| 4 — Tollwater Crossings | 3 | 1 | 4 |
| 5 — Wayside Covenants | 2 | 2 | 4 |
| 6 — The Quorum Ring | 0 | 4 | 4 |
| 7 — Mire of Appeals | 0 | 4 | 4 |
| 8 — Old-Growth Precedents | 2 | 2 | 4 |
| 9 — Moonlit Jurisdictions | 1 | 3 | 4 |
| 10 — Court Beneath the Hill | 1 | 3 | 4 |
| **Total** | **12** | **28** | **40** |

Act III deliberately contains far more combination encounters than Act II. Support-first identities are introduced only where their relationship mechanic can actually fire.

This is thematically appropriate:

> customary law becomes interesting when multiple parties possess competing rights.

---

## STAGE 1 — THE ROAD OF PERMITTED TURNS

### 1. Permit Hare

**Role:** Trespass tutorial / simple Local Law  
**Identity:** A road official in hare form who behaves as though every path requires a permit.

#### Local Law — No Hasty Passage

If the player plays a third card during a player turn:

> apply 1 Trespass from Permit Hare.

Maximum:

> once per player turn.

If Safe-Conduct prevents this Trespass:

> Permit Hare's defensive response may be reduced according to implementation tuning.

#### Design purpose

The simplest Act-III lesson:

> the law is visible, breakable and source-bound.

Permit Hare is intentionally retained as a recurring character later in the act.

---

### 2. Mossbound Clerk

**Role:** customary-use precedent  
**Mechanic source:** Ancient Entitlement  
**Identity:** A clerk so old that moss records procedures more reliably than ink.

#### Signature — The First Use Became Custom

The first non-Junk card played in the combat establishes its card type as:

> **Customary Use**.

Beginning with the next player turn:

If the first non-Junk card played in a turn is of a different type:

> apply 1 Trespass from the Clerk.

Maximum:

> once per turn.

#### Design purpose

The Clerk does not ask what the written rule says.

It remembers:

> how the procedure was first performed.

That memory becomes law.

---

### 3. Cairn of Stray Paths

**Role:** support / remembers foreign Trespass  
**Identity:** A roadside cairn that grows whenever travelers leave the sanctioned path.

This enemy is intentionally strongest in combination encounters.

#### Passive — Every Detour Leaves a Stone

The first time each player turn the player actually receives Trespass from another source:

> gain 1 Detour Stone.

At 2 Detour Stones:

1. remove both;
2. choose another valid living enemy;
3. that enemy gains 1 newly created Claim.

The Cairn does not trigger on Trespass applications prevented by Safe-Conduct.

#### Design purpose

A violation committed against one party becomes:

> precedent supporting someone else's Claim.

---

### Encounter 1 — The Hare Checks the Road
**Permit Hare**

Clean Trespass tutorial.

---

### Encounter 2 — The First Use Became Custom
**Mossbound Clerk**

Introduces customary law without multi-enemy pressure.

---

### Encounter 3 — Every Detour Leaves a Stone
**Permit Hare + Cairn of Stray Paths**

Permit Hare generates visible Trespass pressure.

The Cairn remembers actual violations and can convert repeated misconduct into Claims for another party.

---

### Encounter 4 — Stray-Path Precedent
**Mossbound Clerk + Cairn of Stray Paths**

Breaking the Clerk's customary-use rule may feed the Cairn.

The player sees that one party's law can become another party's standing.

---

## STAGE 2 — THE SURVEYED HEDGEROWS

### 4. Reckoning Hedge

**Role:** Claim changes the Local Law  
**Identity:** A living hedge that measures travelers and then changes the measurement after disputes.

#### Local Law — Current Survey

Initial form:

> playing two consecutive cards with the same Base Cost applies 1 Trespass.

When the Hedge gains a Claim:

> reverse the law.

Reversed form:

> playing two consecutive cards with different Base Costs applies 1 Trespass.

Each new Claim flips the law again.

Maximum:

> one Trespass from this Local Law per player turn.

#### Design purpose

A Claim changes:

> what the boundary actually means.

---

### 5. Errant Boundary Stone

**Role:** Claim transfer / support-first identity  
**Identity:** A boundary marker that never stays where official maps insist it belongs.

Errant Boundary Stone is never introduced alone. Claim transfer only becomes legible when another claimant exists.

#### Passive — Wandering Title

Whenever this Stone gains a newly created Claim:

> it may transfer one of its Claims to a valid ally with fewer Claims.

The transferred Claim:

> is not newly created.

#### Stage-2 tutorial setup — Prior Dispute

In the two Stage-2 tutorial combinations containing Errant Boundary Stone:

> the Stone begins with 1 newly created Claim and resolves `Wandering Title` before the first player action.

This is encounter scaffolding, not a universal passive. Later appearances receive no free Claim. It ensures that Claim transfer is actually demonstrated within the expected standard-combat duration.

#### Design purpose

Ownership moves because:

> the boundary itself moves.

This is a recurring identity and will return in Stage 9.

---

### 6. The Hawthorn Tenant

**Role:** target priority / protected Claims / support-first identity  
**Identity:** An ancient tenant whose occupation of the hedgerow predates every surviving deed.

Hawthorn Tenant is never used alone. Both `Respect the Occupied Plot` and `Prior Possession` require another party to make the tenancy dispute meaningful.

#### Local Law — Respect the Occupied Plot

The first time each player turn the player attacks Hawthorn Tenant while another living enemy has lower current HP:

> apply 1 Trespass from Hawthorn Tenant.

#### Passive — Prior Possession

Claims belonging to Hawthorn Tenant:

- cannot be transferred away by foreign passives;
- cannot be copied by foreign passives;
- cannot be consumed as the cost of another enemy's ability.

Other enemies may still:

> give Claims to Hawthorn Tenant.

#### Design purpose

Once possession has been recognized:

> other parties cannot casually overwrite it.

---

### Encounter 5 — Counter-Survey
**Reckoning Hedge**

Solo introduction to a Claim changing the meaning of one Local Law.

---

### Encounter 6 — The Errant Line
**Reckoning Hedge + Errant Boundary Stone**

`Prior Dispute` creates an immediate Claim-transfer demonstration. The Hedge then shows that the transferred political landscape and its own later Claims are different legal facts.

---

### Encounter 7 — Thorn Lease
**Reckoning Hedge + Hawthorn Tenant**

Encounter HP scaling must leave **Reckoning Hedge at lower starting HP than Hawthorn Tenant**. The Hedge therefore supplies the neighboring party that makes `Respect the Occupied Plot` readable from turn one.

The player must decide whether attacking the Tenant is worth generating its standing while the survey law keeps changing.

---

### Encounter 8 — Title Lodged in Hawthorn
**Errant Boundary Stone + Hawthorn Tenant**

Encounter HP scaling must leave **Errant Boundary Stone at lower starting HP than Hawthorn Tenant**.

`Prior Dispute` lets the Stone move a Claim onto the Tenant immediately. Once there:

> Prior Possession prevents foreign mechanics from moving it away again.

The player sees both Claim transfer and protected standing in the same clean interaction.

---

## STAGE 3 — THE MEADOW OF LIVING TESTIMONY

### 7. Foxglove Witness

**Role:** secondary witness source / support-first identity  
**Identity:** A poisonous flower that insists it witnessed the violation from the edge of the meadow.

Foxglove Witness always appears beside a Local-Law source; without another party's violation there is nothing for it to witness.

#### Passive — I Saw That Too

When the player actually receives Trespass caused by violating another enemy's Local Law:

> remember that violated law for the remainder of the player turn.

Source replacement does not change which Local Law was violated. If Contrary Magpie takes ownership of the resulting Trespass, Foxglove still remembers the original law that produced it.

If the player violates the same remembered Local Law again during that turn:

> Foxglove attempts to apply 1 Trespass from itself.

Maximum:

> once per player turn.

Foxglove's own testimony does not count as a new violation of the remembered Local Law.

#### Design purpose

One act can have:

> multiple witnesses with separate legal standing.

---

### 8. Contrary Magpie

**Role:** Trespass source manipulation / support-first identity  
**Identity:** A magpie that always remembers the event differently from everyone else.

Contrary Magpie always appears with another Trespass source. Its identity is ownership of testimony, not standalone status pressure.

#### Passive — Contrary Testimony

The first time each player turn another enemy would apply Trespass:

If Magpie has fewer Claims than the original source:

> Magpie may replace the original source of that Trespass with itself.

Only after source replacement:

> Safe-Conduct may be offered.

#### Design purpose

The argument is not about whether the event occurred.

It is about:

> who gets to claim they witnessed it.

---

### Encounter 9 — Roadside Testimony
**Reckoning Hedge + Foxglove Witness**

The Hedge supplies a Local Law whose same/different-cost pattern can occur more than once in one turn. Foxglove therefore has a concrete second violation to witness instead of relying on a law that only has one meaningful trigger window.

---

### Encounter 10 — Contrary Permit
**Permit Hare + Contrary Magpie**

#### Encounter setup — Prior Standing
Permit Hare begins with **1 Claim**. This is visible before the first player action.

Permit Hare therefore supplies a simple, already-known Trespass source for which Magpie immediately has fewer Claims and may contest ownership. The encounter teaches source manipulation during the first meaningful violation instead of waiting three or four turns for the inequality to arise naturally.

---

### Encounter 11 — Surveyed Detours
**Reckoning Hedge + Cairn of Stray Paths**

Cairn returns from Stage 1. Hedge violations create the actual foreign Trespass that the Cairn can remember, eventually converting repeated misconduct into standing elsewhere.

---

### Encounter 12 — Two Witnesses, One Account
**Reckoning Hedge + Foxglove Witness + Contrary Magpie**

#### Encounter setup — Contested Survey
Reckoning Hedge begins with **1 Claim**, so `Current Survey` begins in its reversed form and Contrary Magpie may contest the first resulting Trespass.

This is the first planned three-body standard encounter in Act III, but two bodies are fragile support identities. The Hedge supplies the actual Local Law; Foxglove can testify to a repeated violation of that law; Magpie can dispute who owns the original testimony.

The interaction order is explicit:

1. Hedge Local Law is violated;
2. Magpie may replace the source of the Hedge Trespass;
3. the actual received Trespass is recorded;
4. Foxglove remembers the violated Hedge law, not the rewritten source identity;
5. a later repeat of the Hedge pattern may produce Foxglove Trespass.

No witness can create testimony from nothing.

Foxglove remembers the law that was broken.

Magpie may rewrite:

> who legally owns the testimony.

One says:

> “I saw that too.”

The other says:

> “No. I saw it otherwise.”

---

## STAGE 4 — THE TOLLWATER CROSSINGS

### 9. Charter-Shell Snail

**Role:** restricted Wergild payment  
**Identity:** A snail whose shell is engraved with an entire payment charter.

#### Passive — Payment According to Charter

Cards with Base Cost 0:

> cannot be used as Offerings to pay Wergild owed to Charter-Shell Snail.

All other normal Wergild rules apply.

#### Design purpose

The payment procedure is literally:

> written on the shell.

---

### 10. Streamside Oath-Fish

**Role:** beneficial Wergild settlement  
**Identity:** A river fish that treats fulfilled restitution as a sacred oath.

#### Passive — Oath Accepted

When Wergild owed to Oath-Fish is fully paid:

> gain 2 Safe-Conduct instead of 1.

#### Design purpose

The player may intentionally value settlement.

Not every demand is purely punitive.

---

### 11. Two-Bank Toll Ford

**Role:** Claim and Wergild coexist  
**Identity:** A crossing where both sides insist that the toll belongs to them.

#### Passive — Toll on Both Banks

Whenever Ford gains a newly created Claim:

> immediately create Wergild 1 from Ford.

The Claim:

> remains.

It is not consumed.

#### Design purpose

A recognized Claim and an active demand are:

> separate legal facts.

---

### Encounter 13 — Shell Charter
**Charter-Shell Snail**

---

### Encounter 14 — Oaths in Running Water
**Streamside Oath-Fish**

---

### Encounter 15 — Both Banks Demand Payment
**Two-Bank Toll Ford**

---

### Encounter 16 — Oaths on Both Banks
**Two-Bank Toll Ford + Streamside Oath-Fish**

The Ford converts Claims into immediate obligations.

Oath-Fish makes successful settlement especially valuable.

The player begins to evaluate:

> whether a Claim is worth provoking because of what restitution may grant later.

---

## STAGE 5 — THE WAYSIDE COVENANTS

### 12. Roadside Witchling

**Role:** conditional gift / Safe-Conduct provenance  
**Identity:** A small roadside witch whose hospitality is genuine but socially binding.

#### Signature — Courtesy Safe-Conduct

Witchling may grant:

> 1 Safe-Conduct with provenance `granted_by = Roadside Witchling`.

If the player carries Witchling-granted Safe-Conduct through a later full player turn without spending any of it:

> apply 1 Trespass from Witchling.

If the player spends Witchling-granted Safe-Conduct:

> Witchling heals according to implementation tuning.

Only her own granted stacks count.

#### Design purpose

The gift is real.

Ignoring the gift:

> is rude.

Using it:

> confirms the social bond.

---

### 13. The Blackthorn Bride

**Role:** paired promise / Claim threshold escalation  
**Identity:** A thorn-wreathed bride enforcing reciprocal obligation.

#### Local Law — A Promise Must Be Paired

After the player plays a card with Base Cost 2 or higher:

> the next card played should have Base Cost 0 or 1.

If the next card also costs 2 or more:

> apply 1 Trespass from the Bride.

#### Passive — Betrothal Claim

When Bride reaches:

> 1 Claim

player gains:

> 1 Safe-Conduct.

When Bride reaches:

> 2 Claims

immediately create:

> Wergild 2 from Bride.

#### Design purpose

The relationship progresses:

> welcome → commitment → obligation.

---

### 14. Crossroads Cup

**Role:** recurring gift that redistributes obligation  
**Identity:** An unattended ceremonial cup placed where roads, customs and promises intersect.

#### Passive — Drink Before Choosing

Every two player turns:

> player gains 1 Safe-Conduct.

The first time each player turn the player spends Safe-Conduct:

> the living enemy with the fewest Claims gains 1 newly created Claim.

#### Design purpose

The Cup helps the player.

Its help also:

> creates obligation somewhere in the social network.

This object returns in Stage 10.

---

### Encounter 17 — The Witch at the Milestone
**Roadside Witchling**

---

### Encounter 18 — The Bride at the Threshold
**Blackthorn Bride**

---

### Encounter 19 — A Cup at the Crossroads
**Crossroads Cup + Roadside Witchling**

Both provide Safe-Conduct.

But:

- Witchling expects her gift to be used;
- Cup turns Safe-Conduct usage into Claims.

Hospitality becomes:

> a polite debt engine.

---

### Encounter 20 — Blackthorn Toast
**Blackthorn Bride + Crossroads Cup**

Cup distributes Claims.

Claims on Bride may immediately produce:

- Safe-Conduct;
- then Wergild.

The encounter can escalate through generosity alone.

---

## STAGE 6 — THE QUORUM RING

Stage 6 intentionally contains:

> no solo encounters.

A quorum requires multiple parties.

---

### 15. Mandated Mushroom Circle

**Role:** collective mandate  
**Identity:** A mushroom ring that cannot legally act until its own procedure recognizes plurality.

#### Local Law — Quorum Requires Dissent

If the player has played at least two non-Junk cards in a turn and all cards played so far that turn share the same card type:

> at end of turn apply 1 Trespass from Mushroom Circle.

Maximum:

> once per player turn.

#### Passive — Common Mandate

Once per enemy turn:

> another living enemy with no Claim may spend 1 Claim belonging to Mushroom Circle to pay the Claim cost of its own ability.

The Claim:

> remains owned by Mushroom Circle until consumed.

This is consumption, not transfer.

#### Design purpose

The Circle owns the mandate.

Another member may:

> act in its name.

---

### 16. The Bracken Moot

**Role:** collective Claim hearing  
**Identity:** A fern-grown assembly where every grievance is heard whether or not anyone asked.

#### Passive — Claims Are Heard Together

Whenever another enemy gains a **newly created Claim**:

> Bracken Moot gains 1 Hearing.

At 2 Hearings:

1. Hearings → 0;
2. the living enemy with the most Claims gains 1 newly created Claim.

Claim transfers:

> do not generate Hearings.

#### Design purpose

The Moot turns isolated claims into:

> communal political pressure.

---

### Encounter 21 — Hare Before the Quorum
**Mushroom Circle + Permit Hare**

Permit Hare returns.

The Circle may allow another party to act using its communal Claim.

---

### Encounter 22 — Boundary Hearing
**Bracken Moot + Errant Boundary Stone**

Boundary transfers Claims.

Moot reacts only to newly created Claims.

This distinction is intentional and prevents loops.

---

### Encounter 23 — Two-Bank Quorum
**Mushroom Circle + Two-Bank Toll Ford**

Circle can allow another enemy to use its Claim.

Ford can therefore exercise a Claim-based ability:

> without personally owning the Claim beforehand.

---

### Encounter 24 — Detour Hearing
**Bracken Moot + Cairn of Stray Paths**

Cairn converts repeated violations into newly created Claims.

Moot immediately hears those Claims.

A known early-road support interaction becomes a full political process.

---

## STAGE 7 — THE MIRE OF APPEALS

Stage 7 intentionally contains:

> no solo encounters.

An appeal requires:

- an existing grievance;
- another party;
- a procedure that changes its legal status.

---

### 17. Ditch Lamprey of Appeals

**Role:** Claim takeover and return  
**Identity:** A lamprey living in drainage ditches who attaches itself to grievances traveling upstream.

#### Passive — Attach to the Appeal

The first time each round another enemy gains a newly created Claim:

If Lamprey has fewer Claims than that source:

> Lamprey may transfer that Claim to itself.

Remember:

> the original owner.

Later, through its own action:

> Lamprey may transfer that same Claim back to its remembered source.

Transfers are not newly created Claims.

#### Design purpose

A grievance can temporarily belong to:

> the appeal itself.

---

### 18. The Sedge Bench

**Role:** Claim review and settlement  
**Mechanic source:** Sedge Bench + Leech of Reconsideration  
**Identity:** A marsh bench that hears matters slowly enough for reeds to grow through the record.

#### Passive — Under Review

At the start of its enemy turn:

> mark the oldest eligible Claim belonging to another enemy as **Under Review**.

While Under Review:

- the Claim still exists;
- counts toward Claim totals;
- cannot be transferred;
- cannot be consumed.

#### Passive — Settlement on Appeal

If Wergild owed to the same source as the Reviewed Claim is fully paid while Review is active:

> remove the Reviewed Claim.

#### Action — Call the Matter

Sedge Bench may create a small Wergild demand from the Reviewed source without consuming the Reviewed Claim.

#### Design purpose

An appeal does not erase ownership.

It can:

> suspend the Claim long enough for settlement to extinguish it.

---

### Encounter 25 — Upstream Appeal
**Ditch Lamprey + Two-Bank Toll Ford**

Ford creates Claims and Wergild.

Lamprey may separate:

> the ownership of the Claim from the source of the demand.

---

### Encounter 26 — Charter Review
**Sedge Bench + Charter-Shell Snail**

Snail's Claim may become unavailable for use while its associated Wergild is litigated.

Full settlement can remove the underlying Claim.

---

### Encounter 27 — Boundary Appeal
**Ditch Lamprey + Errant Boundary Stone**

Both move Claims.

But only through transfers.

No transfer counts as new Claim creation.

The encounter is a controlled legal tug-of-war, not a loop.

---

### Encounter 28 — Betrothal Review
**Sedge Bench + Blackthorn Bride**

Bride's dangerous Claim thresholds can be frozen by Review.

A reviewed Claim may later be removed through successful settlement.

---

## STAGE 8 — THE OLD-GROWTH PRECEDENTS

### 19. Sleeping Stump Auditor

**Role:** escalating precedent  
**Identity:** A tree stump whose rings contain records of every prior measure.

#### Local Law — The Old Measure

Beginning with the second player turn:

If the player plays more cards than in the previous player turn:

> apply 1 Trespass from Stump.

#### Passive — Rings of Precedent

Claims progressively strengthen this same Local Law.

Exact thresholds and numerical escalation belong to the balance pass, but the design intent is:

- early Claim: stricter consequence;
- later Claim: repeated application or stronger Trespass;
- highest state: entrenched precedent.

#### Design purpose

The standard does not change arbitrarily.

It becomes stricter because:

> previous disputes hardened it into precedent.

---

### 20. Precedent Lichen

**Role:** copies another Local Law  
**Identity:** Old lichen that survives by citing older authority growing beneath it.

This enemy is strongest in combinations.

#### Passive — Cited Authority

When Lichen gains a Claim:

> choose another living enemy with a Local Law.

Lichen copies:

> only that enemy's Local Law.

It does not copy:

- Claim passives;
- Wergild rules;
- resources;
- support passives.

The copied Local Law remains for its defined duration according to implementation tuning.

#### Design purpose

The Lichen does not invent law.

It says:

> “See older authority.”

---

### 21. Footfall Root

**Role:** permanent memory from past Claims  
**Identity:** A deep root network that remembers movement long after surface evidence disappears.

#### Passive — Deep Memory

Whenever Root gains a newly created Claim:

> gain 1 Memory, maximum 4.

Memory remains even if the underlying Claim later:

- moves;
- is consumed;
- is removed.

Memory modifies Root's later actions according to balance tuning.

#### Design purpose

Settlement may eliminate the Claim.

It cannot eliminate:

> what the forest remembers.

---

### Encounter 29 — The Old Measure
**Sleeping Stump Auditor**

---

### Encounter 30 — Footsteps Become Precedent
**Footfall Root**

---

### Encounter 31 — Two Authorities Agree
**Precedent Lichen + Sleeping Stump Auditor**

Lichen can cite the Stump's Local Law.

The Stump's precedent may become:

> authority for two separate sources.

---

### Encounter 32 — Deep-Root Precedent
**Precedent Lichen + Footfall Root**

Root accumulates persistent Memory.

Lichen temporarily imports another source's Local Law.

The encounter contrasts:

- remembered history;
- cited authority.

---

## STAGE 9 — THE MOONLIT JURISDICTIONS

### 22. The Untranslated Trail Marker

**Role:** rotating legal interpretation  
**Identity:** A trail marker whose inscription is clearly authoritative and completely untranslated.

#### Signature — Three Readings

The Trail Marker cycles through three visible interpretations.

#### Reading I — Repeated Measure
> two consecutive cards with the same Base Cost → 1 Trespass.

#### Reading II — Wandering Attention
> the second target change during a player turn → 1 Trespass.

#### Reading III — Empty Hands Are Unwitnessed
> ending the player turn with no valid non-Junk card in hand → 1 Trespass.

When either:

- Trail Marker gains a Claim; or
- Safe-Conduct is spent specifically against its Trespass,

advance to the next Reading.

#### Design purpose

Everyone agrees the inscription is law.

The dispute concerns:

> what it says.

---

### 23. Elsewhere Path

**Role:** mandatory destination / support-first identity  
**Identity:** A path that consistently reaches somewhere other than where maps claim it should.

Elsewhere Path is never used alone. `Destination` requires another living enemy by definition.

#### Passive — Destination

At the start of each player turn:

> mark another living enemy as Destination.

If the player ends the turn without targeting that enemy at least once:

> apply 1 Trespass from Path.

If the Destination dies before being targeted:

> Path gains 1 newly created Claim.

#### Design purpose

The law concerns:

> whether you went where the path said you were going.

---

## Returning Identity — Permit Hare

The former `Hare of Two Jurisdictions` is not a separate enemy.

It is:

> the same Permit Hare from Stage 1.

By Stage 9, two legal systems claim authority over it.

#### Passive — Which Court Has Standing?

At 0–1 Claims:

> **Road Law** applies: the third played card causes Trespass.

At 2–3 Claims:

> **Hill Law** applies: the first Base-Cost-0 card played each turn causes Trespass.

The player recognizes the original Road Law, then sees jurisdiction rewrite which law governs the same character.

---

## Returning Identity — Errant Boundary Stone

The former `Silver Boundary Stone` is not a second enemy.

It is:

> the Stage-2 Boundary Stone under higher jurisdiction.

#### Passive — Superior Jurisdiction

Foreign Claim-transfer effects may not move a Claim:

> from a source with more Claims to a source with fewer Claims.

The Boundary Stone itself:

> ignores this restriction when using its own Wandering Title ability.

#### Design purpose

The same wandering border now possesses:

> precedence over lesser border disputes.

---

### Encounter 33 — Three Readings on One Stone
**Untranslated Trail Marker**

---

### Encounter 34 — The Hawthorn Destination
**Elsewhere Path + Hawthorn Tenant**

The Path designates a destination; the Tenant makes attacking that destination legally costly when another body is weaker. The player may have to choose between honoring the road and respecting occupation.

---

### Encounter 35 — Two Courts Claim the Hare
**Permit Hare + Errant Boundary Stone**

Two early recurring characters return together.

Permit Hare's governing law now depends on Claim level.

Boundary Stone imposes jurisdictional hierarchy on transfers.

This is one of the primary recurrence payoffs of Act III.

---

### Encounter 36 — No Path Has Final Authority
**Untranslated Trail Marker + Elsewhere Path**

Marker determines:

> which interpretation of law currently applies.

Path determines:

> which destination must be honored.

Interpretation and jurisdictional destination become separate problems.

---

## STAGE 10 — THE COURT BENEATH THE HILL

### 24. Keeper of Buried Names

**Role:** repeated names become both guilt and payment  
**Identity:** A court official who keeps every spoken name buried beneath the hill.

#### Local Law — Names Once Spoken

The first time each player turn the player plays a card whose name has already been played earlier in the combat:

> apply 1 Trespass from Keeper.

#### Passive — Buried Names as Payment

If a card is used as an Offering to pay Keeper's Wergild and that card's name has already been played earlier in the combat:

> that card pays 2 Wergild instead of 1.

#### Design purpose

The same repeated name:

> creates guilt  
> and carries greater value in restitution.

---

### 25. Handworn Tally Coin

**Role:** full Act-III reciprocity loop  
**Identity:** A coin that remembers each exchange more clearly than the people who made it.

#### Passive — All Claims Have Value

Whenever any enemy actually consumes a Claim:

> gain 1 Tally.

At 3 Tally:

1. Tally → 0;
2. player gains 1 Safe-Conduct;
3. living enemy with the fewest Claims gains 1 newly created Claim.

#### Passive — Paid in Kind

Whenever Wergild is fully paid:

> Coin loses 4 HP.

#### Design purpose

The Coin records:

> Claim → expenditure → protection → new Claim → restitution.

---

## Returning Identity — Crossroads Cup

The former `Cupbearer of Small Promises` does not survive as a separate standard enemy.

The Cup itself returns from Stage 5.

It has now reached:

> the Court Beneath the Hill.

Its canonical Safe-Conduct/Claim rule remains recognizable.

#### Design purpose

The object encountered casually at a crossroads is revealed to have always belonged to:

> a deeper economy of favors.

---

### Encounter 37 — Names Kept Below Ground
**Keeper of Buried Names**

Keeper receives one solo so its repeated-name / Offering paradox can be learned cleanly.

---

### Encounter 38 — A Small Promise Reaches the Court
**Crossroads Cup + Keeper of Buried Names**

Cup generates Safe-Conduct and may create Claims when that protection is used.

Keeper later converts standing into Wergild pressure.

Repeated cards can then become superior payment.

---

### Encounter 39 — Every Name Has Value
**Keeper of Buried Names + Handworn Tally Coin**

Keeper may consume Claims.

Coin counts Claim consumption.

Fully paying the resulting Wergild damages Coin.

The act's central currencies begin feeding one another.

---

### Encounter 40 — Handworn Reckoning
**Crossroads Cup + Keeper of Buried Names + Handworn Tally Coin**

This is the second and final planned three-enemy standard encounter in Act III. The first is the Stage-3 testimony capstone `Two Witnesses, One Account`, where two of the three bodies are deliberately fragile supports.

Possible interaction chain:

> Cup grants Safe-Conduct  
> → Safe-Conduct prevents Trespass  
> → Cup creates a Claim  
> → Keeper eventually converts standing into Wergild  
> → Claim consumption gives Coin Tally  
> → Wergild is paid  
> → Coin takes damage  
> → at 3 Tally, Coin grants Safe-Conduct and creates another Claim.

No new universal mechanic appears.

The challenge is:

> reciprocity becoming self-sustaining.

---

## 6. Final Enemy Roster

### Road of Permitted Turns
1. Permit Hare
2. Mossbound Clerk
3. Cairn of Stray Paths

### Surveyed Hedgerows
4. Reckoning Hedge
5. Errant Boundary Stone
6. The Hawthorn Tenant

### Meadow of Living Testimony
7. Foxglove Witness
8. Contrary Magpie

### Tollwater Crossings
9. Charter-Shell Snail
10. Streamside Oath-Fish
11. Two-Bank Toll Ford

### Wayside Covenants
12. Roadside Witchling
13. The Blackthorn Bride
14. Crossroads Cup

### The Quorum Ring
15. Mandated Mushroom Circle
16. The Bracken Moot

### Mire of Appeals
17. Ditch Lamprey of Appeals
18. The Sedge Bench

### Old-Growth Precedents
19. Sleeping Stump Auditor
20. Precedent Lichen
21. Footfall Root

### Moonlit Jurisdictions
22. The Untranslated Trail Marker
23. Elsewhere Path

### Court Beneath the Hill
24. Keeper of Buried Names
25. Handworn Tally Coin

---

## 7. Recurring Identities

### Permit Hare

Appears in:

- Stage 1;
- Stage 3;
- Stage 6;
- Stage 9.

Progression:

> road official → collective participant → subject of competing jurisdictions.

---

### The Hawthorn Tenant

Appears in:

- Stage 2;
- Stage 9.

Progression:

> occupied plot → destination inside overlapping jurisdiction.

---

### Errant Boundary Stone

Appears in:

- Stage 2;
- Stage 6/7 combinations;
- Stage 9.

Progression:

> moving boundary → disputed Claim → superior jurisdiction.

---

### Cairn of Stray Paths

Appears in:

- Stage 1;
- Stage 3;
- Stage 6.

Progression:

> roadside memory → witness-like evidence → political Hearing source.

---

### Crossroads Cup

Appears in:

- Stage 5;
- Stage 10.

Progression:

> roadside gift → courtly economy of obligation.

---

## 8. Final 40 Encounter Templates

| # | Stage | Encounter |
|---:|---|---|
| 1 | Road | The Hare Checks the Road |
| 2 | Road | The First Use Became Custom |
| 3 | Road | Every Detour Leaves a Stone |
| 4 | Road | Stray-Path Precedent |
| 5 | Hedgerows | Counter-Survey |
| 6 | Hedgerows | The Errant Line |
| 7 | Hedgerows | Thorn Lease |
| 8 | Hedgerows | Title Lodged in Hawthorn |
| 9 | Testimony | Roadside Testimony |
| 10 | Testimony | Contrary Permit |
| 11 | Testimony | Surveyed Detours |
| 12 | Testimony | Two Witnesses, One Account |
| 13 | Tollwater | Shell Charter |
| 14 | Tollwater | Oaths in Running Water |
| 15 | Tollwater | Both Banks Demand Payment |
| 16 | Tollwater | Oaths on Both Banks |
| 17 | Covenants | The Witch at the Milestone |
| 18 | Covenants | The Bride at the Threshold |
| 19 | Covenants | A Cup at the Crossroads |
| 20 | Covenants | Blackthorn Toast |
| 21 | Quorum | Hare Before the Quorum |
| 22 | Quorum | Boundary Hearing |
| 23 | Quorum | Two-Bank Quorum |
| 24 | Quorum | Detour Hearing |
| 25 | Appeals | Upstream Appeal |
| 26 | Appeals | Charter Review |
| 27 | Appeals | Boundary Appeal |
| 28 | Appeals | Betrothal Review |
| 29 | Precedents | The Old Measure |
| 30 | Precedents | Footsteps Become Precedent |
| 31 | Precedents | Two Authorities Agree |
| 32 | Precedents | Deep-Root Precedent |
| 33 | Jurisdictions | Three Readings on One Stone |
| 34 | Jurisdictions | The Hawthorn Destination |
| 35 | Jurisdictions | Two Courts Claim the Hare |
| 36 | Jurisdictions | No Path Has Final Authority |
| 37 | Court | Names Kept Below Ground |
| 38 | Court | A Small Promise Reaches the Court |
| 39 | Court | Every Name Has Value |
| 40 | Court | Handworn Reckoning |

---

## 9. System Audit

### Identity Redundancy

The redesign deliberately merges or removes overlapping identities.

#### Merged / transplanted
- Permit Hare + Hare of Two Jurisdictions → one recurring Hare.
- Errant Boundary Stone + Silver Boundary Stone → one recurring Stone.
- Crossroads Cup + Cupbearer of Small Promises → one recurring Cup.
- Ancient Entitlement mechanic → Mossbound Clerk.
- Stone With a Case Number protection mechanic → Hawthorn Tenant.
- Woodpecker Law-Speaker communal Claim mechanic → Mandated Mushroom Circle.
- Leech of Reconsideration settlement mechanic → Sedge Bench.

This preserves mechanical quality while reducing body count.

---

### Local-Law Redundancy

The original pool overused:

- same-cost sequences;
- target switching;
- exact card counts;
- zero-Energy checks;
- simple Claim → damage scaling.

The curated roster reduces these sharply.

Remaining similar patterns differ in purpose.

#### Card-count rules
- Permit Hare: fixed third-card law.
- Sleeping Stump: relative comparison to previous turn with precedent escalation.

#### Cost-sequence rules
- Hedge: Claim flips the rule itself.
- Bride: high-cost promise must be paired.
- Trail Marker: one possible interpretation among three.

#### Targeting rules
- Hawthorn Tenant: possession/HP hierarchy.
- Path: mandatory destination.
- Trail Marker: target-switch rule exists only as one rotating reading.

---

### Claim Transfer Density

The act now escalates Claim manipulation deliberately:

#### Stage 2
> Claims can move.

#### Stage 3
> Trespass source can change.

#### Stage 6
> Claims can be used communally.

#### Stage 7
> Claims can be appealed, transferred back or frozen.

#### Stage 9
> some transfers are legally superior to others.

This is intentional progression.

---

### Loop Safety

The following global distinction is mandatory:

> **Claim transfer is never Claim creation.**

Therefore transfers do not trigger:

- Bracken Moot Hearings;
- other `newly created Claim` listeners.

This prevents transfer loops.

---

### Safe-Conduct Provenance

Only source-sensitive effects should inspect provenance.

Current standard-enemy use:

> Roadside Witchling.

All other Safe-Conduct remains mechanically identical.

---

### Appeal Structure

Stage 7 contains no solo encounters.

This is intentional.

The player does not learn seven new appeal creatures.

Instead:

> previously known Claims are brought into a new legal procedure.

---

### Precedent Structure

Stage 8 keeps three different meanings of precedent:

#### Sleeping Stump
> a known rule becomes stricter.

#### Lichen
> authority is copied by citation.

#### Root
> past Claims leave permanent memory.

No redundant fourth or fifth precedent creature is required.

---

## 10. Difficulty Progression

### Stages 1–2
Learn:
- Local Laws;
- Trespass;
- Claims;
- simple movement of standing.

### Stage 3
Learn:
- testimony;
- multiple sources;
- disputed authorship of Trespass.

### Stage 4
Learn:
- Wergild as a strategic object rather than pure punishment.

### Stage 5
Learn:
- Safe-Conduct as gift, courtesy and obligation.

### Stage 6
Shift:
- individual rights become collective mandate.

### Stage 7
Shift:
- Claims become appealable legal objects.

### Stage 8
Shift:
- behavior becomes precedent.

### Stage 9
Shift:
- multiple jurisdictions interpret the same behavior differently.

### Stage 10
Synthesis:
- gift;
- Claim;
- consumption;
- Wergild;
- Safe-Conduct;
- renewed obligation.

The act becomes harder through:

> relationships, not vocabulary inflation.

---

## 11. Balance Pass Still Outstanding

This document locks:

- identities;
- signature mechanics;
- recurrence;
- encounter composition;
- Claim-transfer semantics;
- Safe-Conduct provenance.

It does **not yet permanently lock**:

- HP;
- damage values;
- Block values;
- Claim thresholds beyond structural design;
- exact Wergild amounts;
- intent weights;
- cooldowns;
- duo/trio HP scaling.

Those belong in a dedicated implementation balance pass.

---

## 12. Canonical Verdict

Act III standard combat now contains:

> **25 unique enemy identities**

combined into:

> **40 standard encounter templates**

across:

> **10 stages**.

The act intentionally contains more combination encounters than Acts I and II because its central fantasy depends on social and legal relationships between multiple parties.

The defining principle is:

> **The Green Docket does not become harder because more laws appear. It becomes harder because old customs acquire witnesses, Claims, appeals, precedent and competing jurisdiction.**


---

## Act IV — The Licensing Labyrinth

**Curated pool:** 35 unique identities / 55 encounter templates

**Status:** Canonical content-design snapshot  
**Scope:** Standard enemies and standard encounters only  
**Unique standard-enemy identities:** 35  
**Encounter templates:** 55  
**Stages:** 17  
**Core design rule:** Act IV becomes harder through accumulated procedure, preservation, monumentality and recurrence — not through endless new Scribes, Cobras, Jackals, Scarabs and Ushabti.

---

## 1. Redesign Verdict

The previous Act-IV standard pool was drastically oversized:

- 102 unique standard-enemy identities;
- 136 encounter slots;
- many repeated visual nouns;
- many repeated mechanics of the form:
  - status X → Block;
  - Weighed failure → Strength;
  - 3+ Entombed → damage;
  - Inscribed trigger → generic numeric bonus.

The curated redesign reduces this to:

> **35 identities / 55 encounters**

while preserving the strongest mechanical ideas and almost all of the strongest character fantasies.

The act now follows a clear progression:

1. **Stages 1–5:** measure, register, burden, bury, levy;
2. **Stages 6–10:** labor, monument, writing, sealing, procession;
3. **Stages 11–15:** preservation, division of the body, necropolis law, fixed time, royal identity;
4. **Stages 16–17:** no new bodies — only known figures in their final institutional forms.

The late act therefore feels less like:

> “Here are twelve more Egyptian monsters.”

and more like:

> **“Here are the same institutions after they have become eternal.”**

---

## 2. Core Act-IV Systems

The standard-enemy pool assumes the existing Act-IV core vocabulary:

- **Weighed**
- **Inscribed**
- **Burdened**
- **Entombed**
- **Embalmed**

The redesign does not add additional universal Act-IV statuses.

It instead changes:

> how known enemies interpret, amplify, preserve, convert and combine those five systems.

Stage 13 may temporarily reintroduce the Act-III vocabulary:

- Safe-Conduct;
- Trespass;
- Claim.

That return is localized and explicitly telegraphed.

---

## 3. Canonical Interaction Rules Added by the Audit

These rules are binding for the redesigned standard pool.

### 3.1 One Primary Weighed Requirement

When multiple enemies in one encounter could establish a Weighed check for the same resolution window:

> only one check is the **Primary Measure**.

Other enemies may observe the result of that measure.

They do not create overlapping contradictory Weighed requirements unless an encounter explicitly says otherwise.

Example:

#### Contradictory Measures
Crooked Rod Bearer establishes the Primary Measure.

Reed-Cord Surveyor then evaluates:

> how far the player deviated from that same measure.

---

### 3.2 Observed Weighed Result

Enemies may listen to a completed Weighed check without owning it.

An observed result can expose:

- success;
- failure;
- absolute distance from the required value.

Observation does not create a second Weighed check.

---

### 3.3 Replicated Status Application

A status application created by an effect such as False-Seal Forger is marked:

> **Replicated**.

A Replicated application:

- may be observed by normal non-replication listeners;
- may trigger effects such as Kneeling Petitioners;
- may interact with Embalmed normally;
- **cannot trigger another replication effect**;
- cannot count as the first original application for another copy chain.

This prevents recursive status loops.

---

### 3.4 False-Seal Forgery Is +1 Stack, Not Full Duplication

False-Seal Forger no longer duplicates an arbitrarily large completed application.

The first qualifying negative status application by another enemy each round is followed by:

> **+1 additional stack of that same status from the Forger.**

This additional stack is Replicated.

This preserves the forgery fantasy while preventing explosive scaling.

---

### 3.5 Embalmed-Dependent Solo Enemies Must Self-Enable

A solo enemy whose signature requires Embalmed must have at least one move that can establish the required state itself.

Canonical examples:

- Hieroglyphic Complaint Wall;
- Natron Bearer;
- Unfinished Mummy.

They may not rely on a second enemy merely to make their own signature functional.

---

### 3.6 Fourfold Vessel Guardian Applies One Office Per Turn

The Guardian visibly cycles:

> Body → Breath → Blood → Name.

Only the active office applies its core status package on that turn.

The Guardian does not dump all four status families simultaneously.

---

### 3.7 Moon-Cycle Ibis Repeats One Stack

When Moon-Cycle Ibis repeats its previous negative status:

> it reapplies **1 stack** of that status.

It does not reproduce the entire previous application amount.

This keeps the mechanic predictable rather than multiplicative.

---

### 3.8 Royal Genealogy Does Not Clone Arbitrary Player Status Logic

The old “copy any positive player status” concept was too ambiguous for:

- bespoke player statuses;
- non-stackable effects;
- Retain-like or rule-changing buffs.

The final rule uses a dedicated local resource:

> **Royal Favor**

The first actual positive status gain by the player each round gives the Wall Royal Favor equal to stacks gained, up to its cap.

The Wall later converts Royal Favor into its own defense/offense.

The player's status is not stolen or removed.

If Name-Erasing Chisel Spirit prevents the status from being gained:

> the Wall receives no Royal Favor.

---

### 3.9 Necropolis Law Is Self-Contained

Encounters containing False-Door Finder explicitly reintroduce limited Act-III law.

At combat start:

> player gains 1 Safe-Conduct.

False-Door Finder's passage procedure can create additional Safe-Conduct through correct compliance.

Its failures may create Trespass.

The player is never expected to arrive with unexplained Act-III resources.

---

## 4. Final 35 Identity Roster

| # | Debut | Identity |
|---:|---|---|
| 1 | Stage 1 | Reed-Cord Surveyor |
| 2 | Stage 1 | Crooked Rod Bearer |
| 3 | Stage 2 | Uncounted Pilgrim |
| 4 | Stage 2 | Cobra of the Entry Mark |
| 5 | Stage 2 | Name-Eating Baboon |
| 6 | Stage 3 | Crocodile of the Short Measure |
| 7 | Stage 3 | Jar-Seal Scarab Swarm |
| 8 | Stage 3 | Hungry Grain Thief |
| 9 | Stage 4 | Flood-Mark Reader |
| 10 | Stage 4 | Drowned Field Scribe |
| 11 | Stage 4 | Silt-Buried Farmer Shade |
| 12 | Stage 5 | Foreign Tribute Shade |
| 13 | Stage 5 | Donkey of the Third Tally |
| 14 | Stage 5 | Empty-Handed Envoy |
| 15 | Stage 6 | Rope-Gang Wraith |
| 16 | Stage 6 | Runaway Laborer |
| 17 | Stage 6 | Stone-Hauler Ushabti |
| 18 | Stage 7 | Fallen Capstone Golem |
| 19 | Stage 7 | Cornerstone Oath-Stone |
| 20 | Stage 8 | Palette-Bearing Apprentice |
| 21 | Stage 8 | Hieroglyphic Complaint Wall |
| 22 | Stage 9 | Sun-Seal Bearer |
| 23 | Stage 9 | False-Seal Forger |
| 24 | Stage 10 | Kneeling Petitioners |
| 25 | Stage 11 | Natron Bearer |
| 26 | Stage 11 | Linen-Wrapped Embalmer |
| 27 | Stage 11 | Unfinished Mummy |
| 28 | Stage 12 | Fourfold Vessel Guardian |
| 29 | Stage 13 | False-Door Finder |
| 30 | Stage 13 | Cursed Loot Bearer |
| 31 | Stage 14 | Star-Table Scribe |
| 32 | Stage 14 | Moon-Cycle Ibis |
| 33 | Stage 14 | Eclipse Scarab |
| 34 | Stage 15 | Name-Erasing Chisel Spirit |
| 35 | Stage 15 | Royal Genealogy Wall |

Stages 16 and 17 introduce:

> **no new standard identities.**

Known enemies return in transformed institutional roles.

---

## STAGE 1 — THE BOUNDARY STELAE

### 1. Reed-Cord Surveyor

**Role:** Weighed precision tutorial  
**Identity:** A surveyor carrying knotted reed cord, insisting every distance can be reduced to an approved measure.

#### Signature — Survey Error

The Surveyor establishes a simple visible Weighed requirement.

When that requirement resolves:

> evaluate the absolute distance between required and actual Energy expenditure.

Consequences scale by error band.

Conceptual bands:

- exact → success;
- one step away → minor consequence;
- two or more away → stronger consequence.

Exact numerical consequences remain balance-tunable.

#### Design purpose

The player learns:

> precision matters, not merely binary success/failure.

---

### 2. Crooked Rod Bearer

**Role:** predictable Weighed rhythm  
**Identity:** An official carrying a measuring rod whose standard is wrong but perfectly consistent.

#### Signature — Crooked Standard

Its Primary Measure alternates:

> 1 → 3 → 1 → 3 …

The sequence is visible.

#### Design purpose

The standard is crooked.

The bureaucracy is still predictable.

---

### Encounter 1 — The Surveyor Measures the Road
**Reed-Cord Surveyor**

Pure Weighed precision tutorial.

---

### Encounter 2 — The Crooked Standard
**Crooked Rod Bearer**

Pure rhythm-learning encounter.

---

### Encounter 3 — Contradictory Measures
**Reed-Cord Surveyor + Crooked Rod Bearer**

Crooked Rod Bearer establishes the Primary Measure.

Surveyor observes:

> success and distance from that same measure.

No duplicate Weighed check is created.

---

## STAGE 2 — THE GATE OF COUNTED NAMES

### 3. Uncounted Pilgrim

**Role:** registration-state enemy  
**Identity:** A traveler who somehow reached the gate without ever receiving a valid number.

#### Passive — No Number in the Register

While the player has no Inscribed:

> Pilgrim receives a defensive benefit.

While the player has at least 1 Inscribed:

> Pilgrim becomes Counted and loses that protection.

If Inscribed fully disappears:

> the Pilgrim becomes Uncounted again.

#### Design purpose

The unregistered traveler becomes legible only when:

> the player is themselves inside the register.

---

### 4. Cobra of the Entry Mark

**Role:** Inscribed modifies a concrete later status  
**Identity:** A sacred gate cobra whose mark authorizes the next official affliction.

#### Signature — Entry Venom

If Inscribed is present when the Cobra performs its marked status action:

> that next status application is strengthened.

The modification is telegraphed before resolution.

#### Design purpose

Inscribed changes:

> what the next entry means.

---

### 5. Name-Eating Baboon

**Role:** support / converts Inscribed interaction into later authorization / support-first identity  
**Identity:** A baboon stealing names from tablets and chewing them into false credentials.

Name-Eating Baboon is never used alone. It can generate Stolen Name by itself, but its actual payoff explicitly modifies another enemy's status application; a solo encounter would therefore show only half of its identity.

#### Resource — Stolen Name

The first time each round Inscribed actually modifies another negative status application:

> gain 1 Stolen Name.

Maximum:

> 2.

At 2 Stolen Names:

> the next original negative status application by another enemy receives +1 stack.

Then:

> Stolen Names → 0.

A status bonus created by Stolen Name cannot itself generate another Stolen Name in the same resolution.

---

### Encounter 4 — No Number in the Register
**Uncounted Pilgrim**

---

### Encounter 5 — The Entry Mark
**Cobra of the Entry Mark**

---

### Encounter 6 — Chewed Credentials
**Uncounted Pilgrim + Name-Eating Baboon**

Pilgrim supplies a second official status source while Baboon supplies Inscribed and Doubt. Stolen Name can therefore both accumulate and be spent inside the same encounter.

---

### Encounter 7 — Counterfeit Entry Mark
**Cobra of the Entry Mark + Name-Eating Baboon**

Cobra uses Inscribed.

Baboon converts the interaction into later false authorization.

---

## STAGE 3 — THE GRANARY COURTS

### 6. Crocodile of the Short Measure

**Role:** Weighed vs Burdened resource conflict  
**Identity:** A crocodile enforcing a deliberately unfair grain measure.

#### Signature — Short Measure

The Crocodile combines:

- a visible Weighed demand;
- Burdened pressure.

Burdened changes Energy economy.

The Weighed check demands precision from that same economy.

#### Design purpose

The imposed burden sabotages:

> the player's ability to meet the official measure.

---

### 7. Jar-Seal Scarab Swarm

**Role:** damage leak → Burdened  
**Identity:** Scarabs attaching seals and storage tags to anything they can reach.

#### Passive — Seal the Excess

When the Swarm's designated multi-hit attack deals at least one unblocked HP hit:

> apply Burdened.

Maximum:

> once per designated attack.

#### Design purpose

The swarm physically attaches:

> more things for the player to carry.

---

### 8. Hungry Grain Thief

**Role:** profits from Burdened surcharge  
**Identity:** A thief who survives on whatever the bureaucracy forces others to carry.

#### Resource — Ration

When Burdened causes the player to actually pay additional Energy for a card:

> gain 1 Ration.

Maximum gain:

> once per card played.

At its threshold:

> consume Rations to strengthen or sustain the Thief.

Exact threshold and numeric reward remain balance-tunable.

---

### Encounter 8 — The Short Measure
**Crocodile of the Short Measure**

---

### Encounter 9 — The Jar Seal Breaks
**Jar-Seal Scarab Swarm**

---

### Encounter 10 — Granary Theft
**Hungry Grain Thief**

Its own actions can apply limited Burdened, so the solo is self-sufficient.

---

### Encounter 11 — Weighted Theft
**Crocodile of the Short Measure + Hungry Grain Thief**

Burdened created by the Crocodile makes the Thief's Rations easier to generate.

---

## STAGE 4 — THE FLOODMARK BASINS

### 9. Flood-Mark Reader

**Role:** Weighed failure → Entombed  
**Identity:** A flood official reading water marks after the water has already moved.

#### Passive — High Water Mark

When the player fails a Weighed requirement:

> apply 1 Entombed.

Maximum:

> once per Weighed resolution.

---

### 10. Drowned Field Scribe

**Role:** Entombed strengthens Paperwork  
**Identity:** A scribe whose records remain intact although the fields and clerk are submerged in silt.

#### Passive — Silted Record

At a visible Entombed threshold:

> Paperwork applications from the Scribe become stronger.

The threshold is telegraphed.

---

### 11. Silt-Buried Farmer Shade

**Role:** delayed flood clock  
**Identity:** A farmer whose land and body were buried by the same administratively measured flood.

#### Resource — Flood

Start with a visible Flood countdown.

At the beginning of each relevant player turn:

> Farmer declares **Keep the Furrow**, a simple Weighed requirement.

If fulfilled:

> Flood does not advance that cycle.

If failed:

> Flood advances by 1.

At the final Flood step:

> apply 1 Entombed and reset the Flood sequence.

#### Design purpose

The solo is now fully self-sufficient.

Correct measurement can actually:

> delay the burial.

---

### Encounter 12 — Read the Floodmark
**Flood-Mark Reader**

---

### Encounter 13 — Silted Record
**Drowned Field Scribe**

Its own move set can apply Entombed before Silted Record becomes relevant.

---

### Encounter 14 — Rising Field
**Silt-Buried Farmer Shade**

---

### Encounter 15 — Faulty Flood Record
**Flood-Mark Reader + Drowned Field Scribe**

Weighed failure:

> creates Entombed.

Entombed:

> strengthens the Scribe's Paperwork.

---

## STAGE 5 — THE TRIBUTE CAUSEWAY

### 12. Foreign Tribute Shade

**Role:** success still has administrative cost  
**Identity:** A foreign envoy who delivered the correct tribute and discovered that correctness was not the final fee.

#### Passive — Administrative Cost of Tribute

The first time each round the player successfully fulfills a Weighed requirement:

> apply Paperwork.

#### Design purpose

> “The tribute was correct. Processing was not included.”

---

### 13. Donkey of the Third Tally

**Role:** repeated measurement becomes burden  
**Identity:** The same exhausted donkey later encountered in the event **The Donkey Counted Three Times**.

#### Resource — Tally

Whenever a Weighed requirement resolves:

> gain 1 Tally.

At 3 Tally:

> Tally → 0.

Then apply Burdened.

If the third resolution was a success:

> the Burdened consequence is reduced.

#### Design purpose

The donkey is not carrying three loads.

It was:

> entered three times.

---

### 14. Empty-Handed Envoy

**Role:** support around Weighed completion state  
**Identity:** An envoy whose hands are empty for reasons nobody can agree on.

This enemy is intentionally combination-first.

#### Passive — Nothing Was Presented

When a Weighed requirement resolves while the player ends that turn with no valid unplayed card in hand:

If the measure succeeded:

> Envoy loses a defensive layer / becomes vulnerable.

If the measure failed:

> apply Inscribed.

#### Design purpose

Empty hands can mean:

- everything was properly presented;
- or nothing was presented at all.

---

### Encounter 16 — Correct Tribute, Wrong Procedure
**Foreign Tribute Shade**

---

### Encounter 17 — The Donkey Was Counted Three Times
**Donkey of the Third Tally**

---

### Encounter 18 — Nothing Was Presented, Yet the Fee Remains
**Foreign Tribute Shade + Empty-Handed Envoy**

Shade provides the principal Weighed pressure.

Envoy interprets the player's final hand state.

---

## STAGE 6 — THE CORVÉE YARDS

### 15. Rope-Gang Wraith

**Role:** Fatigue disrupts labor rhythm  
**Identity:** A dead work gang still pulling in step long after the workers are gone.

#### Passive — Lose the Work Rhythm

When Fatigue actually removes Energy from the player:

> Wraith gains Work Strain.

Its next designated labor attack is strengthened by Work Strain.

Then:

> Work Strain resets.

---

### 16. Runaway Laborer

**Role:** non-lethal escape objective  
**Identity:** A conscript who is trying to leave the system rather than defeat the player.

#### Resource — Escape

The first time each player turn the player removes all Block from another enemy that had Block:

> Runaway Laborer gains 1 Escape.

At 2 Escape:

> Runaway Laborer leaves combat.

This counts as resolved for encounter completion.

#### Design purpose

The player can win by:

> breaking the structure holding the laborer in place.

---

### 17. Stone-Hauler Ushabti

**Role:** Burdened surcharge becomes construction resource  
**Identity:** A stone-hauling funerary worker performing compulsory labor with perfect obedience.

#### Resource — Stone

When Burdened causes the player to pay additional Energy:

> Ushabti gains 1 Stone.

Maximum gain:

> once per card.

It can consume Stones for:

- defensive bracing;
- stronger work actions.

Its rotation contains regular Block generation so the Runaway Laborer encounter remains functional.

This same identity returns in Stage 17 as:

> **Golden Ushabti Captain**.

---

### Encounter 19 — Keep the Work Rhythm
**Rope-Gang Wraith**

---

### Encounter 20 — The Stones Grow Heavier
**Stone-Hauler Ushabti**

---

### Encounter 21 — Break the Gang
**Runaway Laborer + Stone-Hauler Ushabti**

Breaking the Ushabti's bracing twice can allow:

> the Runaway Laborer to escape.

---

## STAGE 7 — THE MONUMENT WORKS

### 18. Fallen Capstone Golem

**Role:** delayed Entombed execution  
**Identity:** A capstone that has already fallen from the monument and is still somehow being officially installed.

#### Resource — Placement

The Golem begins a visible Placement sequence.

Its own actions create Entombed pressure.

As Placement approaches completion:

> **Set the Capstone** becomes more dangerous at higher Entombed.

#### Design purpose

The burial is not hypothetical.

The final stone:

> is already above the player.

---

### 19. Cornerstone Oath-Stone

**Role:** remembers compliance  
**Identity:** A foundation stone that records whether requirements were fulfilled when the monument was built.

#### Signature — Foundation Measure

The Oath-Stone regularly establishes a visible simple Weighed requirement when fighting solo.

#### Resources — Kept Oath / Broken Oath

The first visible compliance check observed each round resolves as:

- success → Kept Oath;
- failure → Broken Oath.

Only one Oath token can be recorded per round.

Kept Oaths:

> weaken selected later actions.

Broken Oaths:

> strengthen selected later actions.

The exact numeric conversion remains balance-tunable.

This same identity returns in Stage 17 as:

> **Oathbound Gate**.

---

### Encounter 22 — The Capstone Is Already Above You
**Fallen Capstone Golem**

---

### Encounter 23 — Oath in the Foundation
**Cornerstone Oath-Stone**

---

### Encounter 24 — Fault in the Monument
**Fallen Capstone Golem + Cornerstone Oath-Stone**

Oath-Stone provides the compliance check.

Capstone provides the physical burial clock.

Broken Oaths make the impending monument placement harder to survive.

---

## STAGE 8 — THE HALL OF REED AND INK

### 20. Palette-Bearing Apprentice

**Role:** first Inscribed application is stronger  
**Identity:** A junior scribe whose fresh pigment makes the first entry unusually authoritative.

#### Passive — Fresh Pigment

The first Inscribed application each round:

> gains +1 stack.

Maximum:

> once per round.

The same identity returns in Stage 17 as:

> **Eternal Reed Scribe**.

---

### 21. Hieroglyphic Complaint Wall

**Role:** Embalmed preservation → Complaint  
**Identity:** A carved wall whose grievances have remained legally active for generations.

#### Resource — Complaint

Whenever Embalmed prevents a negative status from naturally decaying:

> gain 1 Complaint.

Complaint strengthens selected future actions.

#### Self-Enabling Move — Preserve the Complaint

The Wall's own rotation includes an action that applies:

- one naturally decaying negative status;
- Embalmed.

Thus its solo encounter does not depend on another enemy.

---

### Encounter 25 — Fresh Pigment
**Palette-Bearing Apprentice**

---

### Encounter 26 — Undismissed Complaint
**Hieroglyphic Complaint Wall**

---

### Encounter 27 — Fresh-Pigment Grievance
**Palette-Bearing Apprentice + Hieroglyphic Complaint Wall**

Fresh Inscribed strengthens later preservation setup.

Preserved decay feeds Complaint.

---

## STAGE 9 — THE COURTS OF THE ROYAL SEAL

### 22. Sun-Seal Bearer

**Role:** authorizes the first official status  
**Identity:** A bearer carrying a royal sun seal whose authority exists only while its impression remains intact.

#### Passive — Authorized Impression

While Sun-Seal Bearer has Block:

> the first original negative status application by its side each round gains +1 stack.

Then:

> consume part of the Bearer's Block.

A Replicated application does not become the round's original application.

---

### 23. False-Seal Forger

**Role:** counterfeit support / support-first identity  
**Identity:** A forger whose false impression is convincing precisely because it appears after the real one.

False-Seal Forger is never used alone. `Counterfeit Authorization` explicitly copies an original status application from another enemy, so a hidden assistant would merely disguise a duo as a solo.

#### Passive — Counterfeit Authorization

The first original negative status application by another enemy each round:

> after resolving, False-Seal Forger applies +1 additional stack of that same status.

That added stack is:

> Replicated.

It cannot trigger another replication chain.

---

### Encounter 28 — The Authorized Impression
**Sun-Seal Bearer**

---

### Encounter 29 — Counterfeit Venom
**Cobra of the Entry Mark + False-Seal Forger**

The early gate Cobra returns as a clean original status source. Its marked application gives the Forger something concrete to counterfeit without inventing a hidden assistant or a new permanent body.

---

### Encounter 30 — Authorized Counterfeit
**Sun-Seal Bearer + False-Seal Forger**

Order:

1. original application;
2. Sun-Seal authorization if available;
3. original resolves;
4. Forger adds exactly +1 Replicated stack.

No recursive copying.

---

## STAGE 10 — THE PROCESSIONAL GALLERIES

### 24. Kneeling Petitioners

**Role:** public legitimacy support  
**Identity:** A procession of petitioners whose visible submission makes every official act look more legitimate.

This identity is intentionally combination-only.

#### Passive — Processional Approval

The first time each round another enemy successfully applies a negative status to the player:

> all living enemies gain Block.

A Replicated status can trigger this if the round's Processional Approval has not yet been used.

Maximum:

> once per round.

---

### Encounter 31 — Processional Seal
**Kneeling Petitioners + Sun-Seal Bearer**

---

### Encounter 32 — Fresh Ink Before the Procession
**Kneeling Petitioners + Palette-Bearing Apprentice**

---

### Encounter 33 — False-Sealed Petition
**Kneeling Petitioners + False-Seal Forger**

The forgery may still be socially legitimized.

The once-per-round cap prevents repeated Block cascades.

---

## STAGE 11 — THE HOUSE OF LINEN

### 25. Natron Bearer

**Role:** preservation becomes burial  
**Identity:** A funerary worker drying everything that would otherwise decay.

#### Passive — Dry What Would Decay

Whenever Embalmed prevents a negative status from naturally decaying:

> apply 1 Entombed.

Maximum:

> once per round.

#### Self-Enabling Move — Drying Rite

Natron Bearer's own rotation includes an action that applies:

- Embalmed;
- one naturally decaying negative status.

---

### 26. Linen-Wrapped Embalmer

**Role:** Inscribed → stronger Embalmed → Burdened  
**Identity:** An embalmer whose written instructions determine how tightly the body is wrapped.

#### Passive — Instructions for Wrapping

When Inscribed strengthens an Embalmed application:

> apply 1 Burdened.

Maximum:

> once per round.

The Embalmer can apply both Inscribed and Embalmed through its own moves.

---

### 27. Unfinished Mummy

**Role:** movement while preserved → Entombed  
**Identity:** A body still in process, with hooks, cloth and ritual instruments attached.

#### Passive — Hooks Still Attached

While the player has Embalmed:

> the first Attack played each player turn adds 1 Entombed after resolving.

Maximum:

> once per player turn.

#### Self-Enabling Move — Incomplete Wrapping

The Mummy's own move set can apply Embalmed.

---

### Encounter 34 — Dry What Would Decay
**Natron Bearer**

---

### Encounter 35 — Instructions for Wrapping
**Linen-Wrapped Embalmer**

---

### Encounter 36 — Hooks Still Attached
**Unfinished Mummy**

---

### Encounter 37 — The Body Is Not Yet Finished
**Linen-Wrapped Embalmer + Unfinished Mummy**

Possible chain:

> Inscribed → enhanced Embalmed → Burdened → Attack under Embalmed → Entombed.

Because each conversion is capped, the encounter remains legible.

---

## STAGE 12 — THE CANOPIC VAULTS

### 28. Fourfold Vessel Guardian

**Role:** predictable four-office status cycle  
**Identity:** A single guardian embodying the entire canopic bureaucracy rather than four separate organ monsters.

#### Signature — Fourfold Office

Cycle visibly:

#### Body
> Burdened.

#### Breath
> Panic.

#### Blood
> Poison.

#### Name
> Inscribed.

Then repeat.

Only the active office applies its core status package on that turn.

#### Design purpose

One identity replaces the former proliferation of:

- Liver-Jar Shade;
- Lung-Jar Wind;
- Stomach-Jar Scarabs;
- Vessel-Seam Cobra;
- similar canopic bodies.

---

### Encounter 38 — The Fourfold Office
**Fourfold Vessel Guardian**

---

### Encounter 39 — Wrapped Before Division
**Fourfold Vessel Guardian + Unfinished Mummy**

The Guardian advances one office at a time.

The Mummy makes Embalmed movement dangerous.

---

### Encounter 40 — Vessel of the Name
**Fourfold Vessel Guardian + Linen-Wrapped Embalmer**

The Guardian's Name office produces Inscribed.

The Embalmer can convert that registration into:

> stronger preservation and Burdened.

---

## STAGE 13 — THE NECROPOLIS WARRENS

### 29. False-Door Finder

**Role:** localized return of Act-III law  
**Identity:** A tomb guide who officially certifies the wrong entrance.

#### Local Rule — Necropolis Passage

At combat start:

> player gains 1 Safe-Conduct.

False-Door Finder periodically establishes a visible Weighed passage check.

If fulfilled:

> player gains 1 Safe-Conduct, up to the local cap.

If failed:

> apply 1 Trespass from False-Door Finder.

Standard Act-III Trespass → Claim rules apply locally.

#### Design purpose

The player has been told:

> the wrong entrance is legally correct.

---

### 30. Cursed Loot Bearer

**Role:** physical burden becomes administrative burden  
**Identity:** A robber carrying objects that generate their own paperwork as they become harder to transport.

#### Passive — Every Object Requires a Form

Whenever Burdened actually increases the Energy cost paid for a card:

> apply Paperwork.

Maximum:

> once per card.

This identity remains consistent with the event:

> **The Tomb Robbers' Fire**.

---

### Encounter 41 — This Entrance Is Legally Valid
**False-Door Finder**

---

### Encounter 42 — Every Object Requires a Form
**Cursed Loot Bearer**

Its own moves can apply Burdened.

---

### Encounter 43 — False-Door Contraband
**False-Door Finder + Cursed Loot Bearer**

The player simultaneously handles:

- localized law;
- passage measurement;
- physical burden;
- administrative cost.

No additional universal status is introduced.

---

## STAGE 14 — THE CHAMBER OF FIXED DAYS

### 31. Star-Table Scribe

**Role:** fixed Weighed calendar  
**Identity:** A scribe whose astronomical table defines the correct measure for each appointed day.

#### Signature — Fixed Decan Measure

Primary Measure cycles visibly:

> 1 → 2 → 3 → 1 …

No random order.

---

### 32. Moon-Cycle Ibis

**Role:** predictable recurrence of last status  
**Identity:** An ibis that repeats ritual afflictions according to lunar return.

#### Memory — Last Rite

Whenever Moon-Cycle Ibis successfully applies a negative status:

> remember its type as Last Rite.

At its visible cycle point:

> reapply 1 stack of Last Rite.

It does not repeat the full original amount.

---

### 33. Eclipse Scarab

**Role:** fixed catastrophic calendar event  
**Identity:** A scarab whose procession contains a scheduled absence of noon.

#### Signature — Black Noon

Every fourth own turn:

> replace the normal intent with **Black Noon**.

Black Noon applies a fixed combined threat involving:

- Panic;
- Entombed.

The schedule is visible well in advance.

The exact numeric package remains balance-tunable.

---

### Encounter 44 — The Fixed Decan Measure
**Star-Table Scribe**

---

### Encounter 45 — Black Noon
**Eclipse Scarab**

---

### Encounter 46 — Fixed-Day Moon
**Star-Table Scribe + Moon-Cycle Ibis**

Two predictable clocks operate simultaneously.

Neither is random.

---

## STAGE 15 — THE CARTOUCHE CHAMBERS

### 34. Name-Erasing Chisel Spirit

**Role:** removes first positive status gain  
**Identity:** A living chisel that treats favor, blessing and identity as mistakes in stone.

#### Passive — Erase the Favor

The first time each round the player would gain a positive status:

> prevent that status gain.

Then apply the Chisel's visible drawback, such as Doubt.

Maximum:

> once per round.

The erased status is considered:

> never gained.

---

### 35. Royal Genealogy Wall

**Role:** appropriates positive status value  
**Identity:** A royal lineage wall that claims every blessing as ancestral property.

#### Resource — Royal Favor

The first time each round the player actually gains a positive status:

> Wall gains Royal Favor equal to stacks gained.

Cap:

> 3 Royal Favor.

The Wall later spends Royal Favor through its own actions for:

- defense;
- royal retaliation.

The Wall does not clone bespoke player-status logic.

#### Priority with Chisel

If Name-Erasing Chisel prevents the first attempted positive status:

> Royal Genealogy Wall gains no Royal Favor from it.

A later actual positive status gain that round may:

> feed Royal Genealogy Wall.

---

### Encounter 47 — Erase the Favor
**Name-Erasing Chisel Spirit**

---

### Encounter 48 — Dynastic Favor Claim
**Royal Genealogy Wall**

---

### Encounter 49 — Expunged Royal Favor
**Name-Erasing Chisel Spirit + Royal Genealogy Wall**

The player can intentionally expose a smaller first buff to the Chisel.

A later successful buff may then:

> feed the Wall.

The interaction is ordered and deterministic.

---

## STAGE 16 — THE HALL OF THE BALANCE

Stage 16 introduces:

> no new standard identities.

Two known figures reach their final roles.

---

### Returning Identity — Crooked Rod Bearer → Feather-Bearer

The early survey official now carries:

> the feather of final measure.

#### Signature — True Balance

Feather-Bearer establishes a visible exact Weighed target.

If fulfilled:

> open a large defensive vulnerability / damage window.

If failed:

> retaliation scales with distance from the target.

This merges:

- the early predictable measurement fantasy;
- the strongest saved distance-from-measure mechanic.

---

### Returning Identity — Crocodile of the Short Measure → Crocodile Beneath the Balance

The grain-measure crocodile now waits:

> under the final scale.

#### Signature — Jaws of Misjudgment

A failed Weighed resolution or a high visible Entombed threshold:

> opens Jaws.

The next designated attack becomes substantially stronger.

Then:

> Jaws close.

---

### Encounter 50 — Feather's True Measure
**Feather-Bearer**

---

### Encounter 51 — Jaws Beneath the Scale
**Crocodile Beneath the Balance**

---

### Encounter 52 — The Measure and the Jaws
**Feather-Bearer + Crocodile Beneath the Balance**

One final exact measure.

One known predator waiting for failure.

No new mechanic is introduced.

---

## STAGE 17 — THE SEALED COURT BEFORE ETERNITY

Stage 17 introduces:

> no new standard identities.

Three earlier figures have been completely absorbed into the eternal institution.

---

### Returning Identity — Stone-Hauler Ushabti → Golden Ushabti Captain

The former compulsory laborer is now:

> an officer commanding work.

#### Resource — Stone

The same Stone identity remains recognizable.

The Captain may now spend Stones to:

- brace allies;
- coordinate work;
- protect the Court.

The exact numeric commands remain balance-tunable.

---

### Returning Identity — Palette-Bearing Apprentice → Eternal Reed Scribe

The former apprentice has become:

> the Court's permanent writer.

#### Passive — The Entry Does Not Close

The first important negative status application by the enemy side each round receives:

> **Preserved Entry**.

Its next natural decay is prevented once.

This is a localized persistence rule derived from the same preservation language as Embalmed.

It does not create an infinite no-decay state.

---

### Returning Identity — Cornerstone Oath-Stone → Oathbound Gate

The foundation stone from Stage 7 is now part of:

> the final door.

#### Resources — Kept Oath / Broken Oath

The Door retains the Oath-Stone's familiar language.

If the player previously encountered the Oath-Stone in the current Act-IV run:

> the Door may import up to 2 visible stored Oath Memories from that encounter.

Only Oath tokens are imported.

It does not inspect arbitrary hidden run history.

During the current combat:

> the Door continues recording visible compliance success/failure.

This route memory is displayed before the first player action.

#### Design purpose

The final door remembers:

> exactly the kind of promise the player already learned it could remember.

No new vocabulary is introduced.

---

### Encounter 53 — Oathbound Gate
**Oathbound Gate**

One of the hardest standard solo encounters in the game.

---

### Encounter 54 — The Eternal Shift
**Golden Ushabti Captain + Eternal Reed Scribe**

Two former low-level workers have become:

- command;
- permanent record.

---

### Encounter 55 — The Sealed Court Before Eternity
**Oathbound Gate + Golden Ushabti Captain + Eternal Reed Scribe**

The only planned three-enemy standard encounter in Act IV.

The final interaction is:

> labor → writing → preservation → monument → remembered compliance.

No new system appears.

Everything is known.

The institution is simply:

> complete.

---

## 5. Final 55 Encounter List

| # | Stage | Encounter |
|---:|---|---|
| 1 | Boundary Stelae | The Surveyor Measures the Road |
| 2 | Boundary Stelae | The Crooked Standard |
| 3 | Boundary Stelae | Contradictory Measures |
| 4 | Counted Names | No Number in the Register |
| 5 | Counted Names | The Entry Mark |
| 6 | Counted Names | Chewed Credentials |
| 7 | Counted Names | Counterfeit Entry Mark |
| 8 | Granary Courts | The Short Measure |
| 9 | Granary Courts | The Jar Seal Breaks |
| 10 | Granary Courts | Granary Theft |
| 11 | Granary Courts | Weighted Theft |
| 12 | Floodmark Basins | Read the Floodmark |
| 13 | Floodmark Basins | Silted Record |
| 14 | Floodmark Basins | Rising Field |
| 15 | Floodmark Basins | Faulty Flood Record |
| 16 | Tribute Causeway | Correct Tribute, Wrong Procedure |
| 17 | Tribute Causeway | The Donkey Was Counted Three Times |
| 18 | Tribute Causeway | Nothing Was Presented, Yet the Fee Remains |
| 19 | Corvée Yards | Keep the Work Rhythm |
| 20 | Corvée Yards | The Stones Grow Heavier |
| 21 | Corvée Yards | Break the Gang |
| 22 | Monument Works | The Capstone Is Already Above You |
| 23 | Monument Works | Oath in the Foundation |
| 24 | Monument Works | Fault in the Monument |
| 25 | Reed and Ink | Fresh Pigment |
| 26 | Reed and Ink | Undismissed Complaint |
| 27 | Reed and Ink | Fresh-Pigment Grievance |
| 28 | Royal Seal | The Authorized Impression |
| 29 | Royal Seal | Counterfeit Venom |
| 30 | Royal Seal | Authorized Counterfeit |
| 31 | Processional Galleries | Processional Seal |
| 32 | Processional Galleries | Fresh Ink Before the Procession |
| 33 | Processional Galleries | False-Sealed Petition |
| 34 | House of Linen | Dry What Would Decay |
| 35 | House of Linen | Instructions for Wrapping |
| 36 | House of Linen | Hooks Still Attached |
| 37 | House of Linen | The Body Is Not Yet Finished |
| 38 | Canopic Vaults | The Fourfold Office |
| 39 | Canopic Vaults | Wrapped Before Division |
| 40 | Canopic Vaults | Vessel of the Name |
| 41 | Necropolis Warrens | This Entrance Is Legally Valid |
| 42 | Necropolis Warrens | Every Object Requires a Form |
| 43 | Necropolis Warrens | False-Door Contraband |
| 44 | Fixed Days | The Fixed Decan Measure |
| 45 | Fixed Days | Black Noon |
| 46 | Fixed Days | Fixed-Day Moon |
| 47 | Cartouche Chambers | Erase the Favor |
| 48 | Cartouche Chambers | Dynastic Favor Claim |
| 49 | Cartouche Chambers | Expunged Royal Favor |
| 50 | Hall of the Balance | Feather's True Measure |
| 51 | Hall of the Balance | Jaws Beneath the Scale |
| 52 | Hall of the Balance | The Measure and the Jaws |
| 53 | Sealed Court | Oathbound Gate |
| 54 | Sealed Court | The Eternal Shift |
| 55 | Sealed Court | The Sealed Court Before Eternity |

---

## 6. Encounter Distribution

| Stage | Solo | Multi | Total |
|---|---:|---:|---:|
| 1 | 2 | 1 | 3 |
| 2 | 2 | 2 | 4 |
| 3 | 3 | 1 | 4 |
| 4 | 3 | 1 | 4 |
| 5 | 2 | 1 | 3 |
| 6 | 2 | 1 | 3 |
| 7 | 2 | 1 | 3 |
| 8 | 2 | 1 | 3 |
| 9 | 1 | 2 | 3 |
| 10 | 0 | 3 | 3 |
| 11 | 3 | 1 | 4 |
| 12 | 1 | 2 | 3 |
| 13 | 2 | 1 | 3 |
| 14 | 2 | 1 | 3 |
| 15 | 2 | 1 | 3 |
| 16 | 2 | 1 | 3 |
| 17 | 1 | 2 | 3 |
| **Total** | **32** | **23** | **55** |

Only one standard encounter uses three enemies:

> **The Sealed Court Before Eternity**

All other multi-enemy standard encounters are duos.

---

## 7. Recurrence and Transformation Map

### Crooked Rod Bearer
Stage 1:

> faulty human measurement.

Stage 16:

> **Feather-Bearer** — final measure.

---

### Crocodile of the Short Measure
Stage 3:

> unfair grain measurement.

Stage 16:

> **Crocodile Beneath the Balance** — punishment beneath final judgment.

---

### Cobra of the Entry Mark
Stage 2:

> first official Inscribed/status interaction.

Stage 9:

> original royal-mark source inside `Counterfeit Venom`.

The recurrence lets the player recognize the authentic mark before the False-Seal Forger copies it.

---

### Stone-Hauler Ushabti
Stage 6:

> compulsory labor.

Stage 17:

> **Golden Ushabti Captain** — command authority.

---

### Palette-Bearing Apprentice
Stage 8:

> first authoritative ink.

Stage 10:

> public procession.

Stage 17:

> **Eternal Reed Scribe** — permanent record.

---

### Cornerstone Oath-Stone
Stage 7:

> foundation remembers compliance.

Stage 17:

> **Oathbound Gate**.

---

### Donkey of the Third Tally
Standard combat:

> Stage 5.

Event continuity:

> **The Donkey Counted Three Times**.

This is the same exhausted animal.

---

## 8. Final Audit

### 8.1 Identity Redundancy — Passed

The redesigned pool no longer needs separate families of:

- six Cobras;
- five Jackals;
- several status Scarab swarms;
- many interchangeable Scribes;
- four separate canopic organ monsters;
- repeated generic Ushabti.

The strongest cultural motifs remain.

Their frequency is now deliberate rather than automatic.

---

### 8.2 Mechanic Redundancy — Passed

The redesign removes most instances of:

> status X → generic Block/Strength/damage.

Remaining status-reactive enemies ask distinct questions:

- exact measurement;
- measurement distance;
- measurement rhythm;
- Burdened resource conflict;
- preservation;
- registration;
- forgery;
- public legitimacy;
- burial clock;
- predictable calendar;
- identity erasure;
- royal appropriation.

---

### 8.3 Solo Self-Sufficiency — Passed After Cleanup

The audit identified several signatures that previously relied on another enemy.

They are now self-sufficient:

#### Silt-Buried Farmer Shade
owns its own Keep the Furrow measure.

#### Hieroglyphic Complaint Wall
can establish Embalmed + a decaying status itself.

#### Natron Bearer
owns Drying Rite.

#### Unfinished Mummy
owns Incomplete Wrapping.

#### Hungry Grain Thief
can create limited Burdened in solo.

#### False-Seal Forger
has a minor non-identity assistant/status source in solo implementation.

The player never faces a solo enemy whose signature cannot occur.

---

### 8.4 Status Density — Passed With Caps

The densest stages are intentionally late:

- House of Linen;
- Canopic Vaults;
- final Court.

Important caps keep them readable:

- Fourfold Guardian uses one office per turn;
- Linen-Wrapped Embalmer conversion once per round;
- Natron Entombed conversion once per round;
- Mummy movement trigger once per player turn;
- Petitioners approval once per round;
- Forger adds only one Replicated stack.

No duo should create uncontrolled multi-status cascades from a single event.

---

### 8.5 Replication Loops — Resolved

False-Seal Forger now adds:

> exactly +1 Replicated stack.

Replicated applications:

> cannot trigger replication.

This eliminates the main recursive-status risk.

---

### 8.6 Weighed Conflict — Resolved

Only one Primary Measure may exist per resolution window unless explicitly overridden.

Observers such as:

- Reed-Cord Surveyor;
- Flood-Mark Reader;
- Oath-Stone;
- Crocodile Beneath the Balance

may react to the same resolved measure.

They do not create competing requirements.

---

### 8.7 Processional Stage — Passed

Kneeling Petitioners intentionally never appear alone.

Their identity is:

> public validation of another authority.

All three Stage-10 encounters therefore use a known official:

- Sun-Seal Bearer;
- Palette-Bearing Apprentice;
- False-Seal Forger.

This is stronger than inventing three new processional bodies.

---

### 8.8 Canopic Compression — Passed

The former organ-specific enemies are intentionally compressed into:

> Fourfold Vessel Guardian.

This is one of the largest quality improvements in the redesign.

The four canopic offices survive mechanically without consuming four separate identities.

---

### 8.9 Stage 13 Law Return — Passed After Cleanup

False-Door Finder explicitly grants the local Safe-Conduct needed to make the return of Act-III law understandable.

No hidden resource assumption remains.

---

### 8.10 Royal Favor — Clarified

Royal Genealogy Wall no longer attempts to duplicate arbitrary bespoke player statuses.

It records:

> Royal Favor.

This preserves the fantasy while making the interaction deterministic and engine-safe.

---

### 8.11 Recurrence Frequency — Passed

The redesigned Act IV does not overuse every recurring figure.

The important arcs are few enough to remain memorable:

- measure → final judgment;
- short measure → crocodile under the balance;
- laborer → captain;
- apprentice → eternal scribe;
- cornerstone → final door.

This is enough recurrence to create a world without making the act feel like constant rematches.

---

### 8.12 One-Off Identity Audit — Passed

Several enemies appear in only one or two encounter templates.

They remain because they ask distinct questions that no other identity covers cleanly.

Examples:

- Uncounted Pilgrim;
- Donkey of the Third Tally;
- Rope-Gang Wraith;
- Natron Bearer;
- Eclipse Scarab.

None currently exists merely to fill a roster slot.

---

## 9. Balance Pass Still Outstanding

This document locks:

- the 35 identities;
- the 55 encounter templates;
- stage placement;
- recurrence arcs;
- signature mechanics;
- loop-prevention semantics;
- self-sufficiency requirements;
- major trigger ordering.

It does **not yet permanently lock** all numerical tuning, including:

- HP;
- exact damage;
- exact Block;
- some status stack amounts;
- cooldown lengths;
- threshold values;
- duo/trio HP scaling.

Those values should be finalized during implementation/playtest balance.

---

## 10. Canonical Verdict

Act IV standard combat is now:

> **35 unique identities**

across:

> **55 encounter templates**

and:

> **17 stages**.

The old 102-body pool has been replaced by a smaller cast whose members recur, develop and become absorbed into the monument itself.

The defining progression is:

> measure  
> → register  
> → burden  
> → bury  
> → preserve  
> → authorize  
> → memorialize.

And the defining late-act idea is:

> **The Licensing Labyrinth stops introducing new officials because the officials you already know have become the architecture.**

---

# MASTER COMBAT BALANCE APPENDIX — PROVISIONAL IMPLEMENTATION TARGETS

## Purpose

This appendix adds the first numeric implementation target for every curated standard-enemy identity.

The numbers are deliberately expressed primarily as **ranges** rather than final exact values.

They are intended to answer:

- roughly how much HP should this enemy have when encountered alone;
- what damage band should its normal intents occupy;
- how much Block should a defensive intent create;
- how many status stacks should a normal versus major status intent apply;
- how aggressively should HP be reduced in duo/trio encounters.

The signature mechanics in the main document remain the source of truth.

If a number in this appendix conflicts with a signature rule in the main body:

> **the signature rule wins and the number should be retuned around it.**

---

## Global HP Scaling for Encounter Composition

All HP ranges below are **solo-equivalent HP** unless the enemy is explicitly combination-only.

Use these values as the starting point for encounter construction:

| Encounter role | Recommended HP multiplier |
|---|---:|
| Solo | **100%** |
| Duo — ordinary body | **68–78%** |
| Duo — fragile support / priority target | **60–70%** |
| Duo — tank / anchor | **75–85%** |
| Trio — ordinary body | **58–68%** |
| Trio — fragile support | **50–60%** |
| Trio — tank / anchor | **65–75%** |

Round to clean whole numbers after scaling.

### Important

Do **not** scale every member of a combination identically.

The intended question should decide the HP distribution.

Examples:

- `Spare-Life Jar` should be killable quickly enough that its one-turn resurrection window is meaningful;
- `Hieroglyphic Complaint Wall` may remain relatively tanky while its partner is reduced more heavily;
- `Oathbound Gate` remains the anchor of the final Act-IV trio while the Captain and Scribe are more aggressively scaled down.

---

## Global Intent Budget Philosophy

Damage values below refer to the **total raw damage of one enemy action** before Block or other player mitigation.

For multi-hit actions the listed damage band is the approximate total unless the intent explicitly gives `NxM`.

### Status stack philosophy

A normal standard-enemy intent should usually apply:

> **1 stack** of its relevant negative status.

Two stacks should be reserved for:

- highly telegraphed signature intents;
- countdown payoffs;
- late-Act-IV set pieces;
- situations where the status is the primary threat and direct damage is correspondingly lower.

Three or more stacks from a single standard-enemy action should be exceptional.

### Block philosophy

A normal defensive action should usually prevent roughly:

> one medium player attack at that point in the run,

not an entire offensive turn.

Tank identities may exceed the normal range, especially when their role is to create target-priority decisions.

---

## Act-Level Reference Bands

| Act | Early solo HP | Late solo HP | Typical light hit | Typical medium hit | Major telegraphed hit | Typical Block |
|---|---:|---:|---:|---:|---:|---:|
| I | 25–40 | 45–69 | 5–8 | 8–12 | 12–18 | 6–18 |
| II | 44–64 | 82–114 | 8–12 | 13–18 | 20–27 | 12–24 |
| III | 54–88 | 106–140 | 9–13 | 14–20 | 20–28 | 14–28 |
| IV | 80–116 | 150–198 | 10–15 | 16–23 | 24–34 | 16–36 |
| IV final forms | 166–210+ | 210–238 | 15–20 | 20–27 | 28–36 | 24–40 |

The increasing difficulty should still come primarily from:

> **mechanic interaction and target priority**, not exponential HP inflation.

---

# ACT I — EXISTING NUMERIC BASELINE

Act I is already the calibration act.

Its 25 standard enemies currently contain explicit `Solo HP` and concrete Intent values directly in their main enemy entries.

Observed solo-HP range:

> **25–69 HP**

Median solo HP:

> **43 HP**

Those existing values remain the reference baseline for Acts II–IV.

Duo HP already follows roughly the same scaling philosophy described above and does not need to be rewritten here.

---

# ACT II — THE ENDLESS ARCHIVES

The archive should feel more oppressive through card access and timing than through huge damage spikes.

Most ordinary status/card-manipulation intents therefore sit below the damage of the heaviest direct attacks.

## Stage 1 — Hall of Returns

| Enemy | Solo HP | Pressure intent | Secondary intent | Defense / signature budget |
|---|---:|---|---|---|
| Brass Maw of Returns | **46–54** | `Return Intake` — **8–10 dmg + 1 Overdue** | `Brass Bite` — **11–14 dmg** | `Brass Shutter` — **12–16 Block**; Parcel attack receives the existing **+5 per Return Parcel** |
| Object Listed as “Other” | **50–58** | `Improper Category` — **9–12 dmg + 1 Overdue** | `Handling Fee` — **12–15 dmg** | `Miscellaneous Storage` — **14–18 Block** |
| Dead-Letter Ouroboros | **44–50** | `Forwarding Loop` — **8–11 dmg + 1 Overdue** | `Returned Unopened` — **12–15 dmg** | `Self-Addressed Notice` — **10–14 Block + 1 Overdue** |

## Stage 2 — Misfiled Stacks

| Enemy | Solo HP | Pressure intent | Secondary intent | Defense / signature budget |
|---|---:|---|---|---|
| Crabwise Shelf | **52–60** | `Mis-Shelve` — **9–12 dmg + Misfile 1 card** | `Sideways Advance` — **13–16 dmg** | `Brace the Stacks` — **14–18 Block** |
| Volume Q-Null | **48–56** | `Null Index` — **8–11 dmg + Misfile 1 card** | `Undefined Entry` — **13–16 dmg** | `Close the Volume` — **12–16 Block** |
| Corridor in the Wrong Edition | **55–64** | `Wrong Edition` — **8–11 dmg + mark 1 card** | `Dead-End Turn` — **12–15 dmg** | `Revision Collapse` — **16–20 dmg** after the marked-card setup |

## Stage 3 — Whispering Catalogue

| Enemy | Solo HP | Pressure intent | Secondary intent | Defense / signature budget |
|---|---:|---|---|---|
| Second-Person Entry | **56–64** | `Second-Person Citation` — **9–12 dmg + Reference 1 card** | `You Are Here` — **13–16 dmg** | `Cross-Reference` — **10–14 Block + Reference 1 card** |
| Fanged Alphabet | **54–62** | `Learned Letter` — **9–12 dmg** while observing Base Cost | `Bite Back` — **14–18 dmg** | `Re-Index` — **12–16 Block** |
| Orphan Citation | **58–66** | `Missing Source` — **Reference 1 card + 0–8 dmg** | `Unsupported Assertion` — **14–18 dmg** | `Scholarly Rebuke` — **10–13 dmg + 1 Overdue** |

## Stage 4 — Hushed Reading Room

| Enemy | Solo HP | Pressure intent | Secondary intent | Defense / signature budget |
|---|---:|---|---|---|
| Unclaimed Reading Table | **62–70** | `Clear the Table` — **12–15 dmg** | `Quiet Reminder` — **9–12 dmg + 1 Overdue** | `Reserved Seat` — **16–20 Block** plus existing forced discard rule |
| Mute Margin | **66–74** | `Narrow the Margin` — **10–13 dmg + Misfile 1 card** | `Edge Cut` — **15–18 dmg** | `White Space` — **16–20 Block** |
| Choir of Unspoken Words | **60–68** | `Held Note` — **10–13 dmg + 1 Overdue** | `Unspoken Crescendo` — **16–20 dmg**, then existing **+8 at 2 Voice** | `Hush` — **14–18 Block** |

## Stage 5 — Redaction Galleries

| Enemy | Solo HP | Pressure intent | Secondary intent | Defense / signature budget |
|---|---:|---|---|---|
| Palimpsest Husk | **70–80** | `Scrape the Surface` — **11–14 dmg + Redact 1 card** | `Older Ink` — **15–19 dmg** | `Layer Over` — **16–20 Block** |
| Expunged Name | **68–76** | `Strike the Name` — **12–15 dmg + Redact 1 card** | `Unpersoned Blow` — **17–21 dmg** | `Seal the Register` — **14–18 Block** |
| Vacant Portrait | **76–86** | `Erase the Face` — **11–14 dmg + Redact 1 card** | `Absence Accuses` — **16–20 dmg** | `Empty Frame` — **18–23 Block** so the Redacted-card counterplay matters |

## Stage 6 — Scriptorium of Errata

| Enemy | Solo HP | Pressure intent | Secondary intent | Defense / signature budget |
|---|---:|---|---|---|
| Fatal Comma | **72–82** | `Set the Clauses` — **0–6 dmg + mark Clause A/B** | `Punctuation Cut` — **14–18 dmg** | `Editorial Stay` — **15–19 Block**; correct A→B ordering keeps existing **8 direct damage to Comma** |
| Errata Doppelgänger | **80–90** | `Errata Transfer` — **12–16 dmg + Redact 1 card** | `Revised Strike` — **18–22 dmg** | `Fresh Binding` — **16–20 Block** |

## Stage 7 — Restricted Annex

| Enemy | Solo HP | Pressure intent | Secondary intent | Defense / signature budget |
|---|---:|---|---|---|
| Checkout Codex | **84–94** | `Check You Out` — **10–13 dmg + Behind-the-Desk 1 card** | `Access Fee` — **16–20 dmg** | `Restricted Binding` — **18–22 Block** |
| Mnemonic Chain | **88–98** | `Remember Volume` — **10–14 dmg + remember 1 exact card instance** | `Chain Snap` — **19–24 dmg** | `Tighten Link` — **16–20 Block** |

## Stage 8 — Archive of Misplaced Hours

| Enemy | Solo HP | Pressure intent | Secondary intent | Defense / signature budget |
|---|---:|---|---|---|
| Unoccurred Tuesday | **82–92** | `Calendar Residue` — **14–18 dmg** | `Tuesday Does Not Occur` — **no action; +25% direct card damage taken** | `Resume on Wednesday` — **22–26 dmg** |
| Hourglass With Two Bottoms | **90–102** | `Left Future` — **14–18 dmg at countdown 1** | `Right Future` — **22–27 dmg at countdown 3** | `Flip the Glass` — **16–20 Block**; Attack/Skill delay rules unchanged |

## Stage 9 — Necrology Vaults

| Enemy | Solo HP | Pressure intent | Secondary intent | Defense / signature budget |
|---|---:|---|---|---|
| Blank Death Certificate | **94–106** | `Serve Certificate` — **13–16 dmg + Reference 1 card** | `Premature Certification` — **19–23 dmg** | `Final Filing` — **22–27 dmg**; first uncertified death still returns at **~35% HP** |
| Spare-Life Jar | **78–88 solo-equivalent** | `Borrowed Breath` — **10–13 dmg + 1 Overdue** | `Seal the Jar` — **18–22 Block** | `Pour Back the Life` — **revive stored ally at 30% Max HP after 1 enemy-turn countdown** |

## Stage 10 — Hall of Concordances

| Enemy | Solo HP | Pressure intent | Secondary intent | Defense / signature budget |
|---|---:|---|---|---|
| Detached Footnote | **80–92 solo-equivalent** | `Marginal Jab` — **12–15 dmg** | `See Note Below` — **14–17 dmg + 1 Overdue** | `Find Another Source` — relink + **12–16 Block** |
| Miscellany Index | **100–114** | `Index Everything` — **15–19 dmg** | `Cross-List` — **10–13 dmg + Redact or Misfile 1 card** | `Everything Else` — existing **Redact 1 + Misfile 1** package, preferably **0–8 dmg** only |

---

# ACT III — THE GREEN DOCKET

Act III damage can rise moderately, but Claims, Wergild and multi-party interactions remain the main threat.

Wergild values from standard enemies should normally be:

> **1–2**

with **3** reserved for late, telegraphed Claim cash-outs.

## Stage 1 — Road of Permitted Turns

| Enemy | Solo HP | Pressure intent | Secondary intent | Defense / signature budget |
|---|---:|---|---|---|
| Permit Hare | **62–70** | `Check the Permit` — **8–11 dmg + 1 Trespass** | `Road Kick` — **12–15 dmg** | `Stamp Passage` — **12–16 Block** |
| Mossbound Clerk | **66–74** | `Record Custom` — **8–11 dmg + 1 Trespass** | `Old Procedure` — **13–16 dmg** | `Moss Seal` — **14–18 Block** |
| Cairn of Stray Paths | **54–62 solo-equivalent** | `Loose Stone` — **8–10 dmg** | `Stonefall` — **12–15 dmg** after Detour buildup | `Brace Cairn` — **15–19 Block**; Detour→Claim passive unchanged |

## Stage 2 — Surveyed Hedgerows

| Enemy | Solo HP | Pressure intent | Secondary intent | Defense / signature budget |
|---|---:|---|---|---|
| Reckoning Hedge | **72–82** | `Measure Back` — **9–12 dmg + 1 Trespass** | `Thorn Retaliation` — **14–18 dmg** | `Close the Hedge` — **16–20 Block**; Claim flips Local Law |
| Errant Boundary Stone | **70–80** | `Move the Marker` — **10–13 dmg + 1 Trespass** | `Boundary Slam` — **15–19 dmg** | `Settle the Line` — **17–21 Block**; Claim-transfer rule unchanged |
| Hawthorn Tenant | **78–88** | `Enforce the Plot` — **11–14 dmg + 1 Trespass** | `Eviction` — **16–20 dmg** | `Thorn Lease` — **16–20 Block**; Prior Possession unchanged |

## Stage 3 — Meadow of Living Testimony

| Enemy | Solo HP | Pressure intent | Secondary intent | Defense / signature budget |
|---|---:|---|---|---|
| Foxglove Witness | **70–80** | `Testify` — **9–12 dmg + 1 Trespass** | `Poisoned Statement` — **13–16 dmg** | `Witness Shelter` — **14–18 Block** |
| Contrary Magpie | **68–78** | `Contrary Cry` — **9–12 dmg + 1 Trespass** | `Steal Testimony` — **13–17 dmg** | `Bright Evidence` — **14–18 Block** |

## Stage 4 — Tollwater Crossings

| Enemy | Solo HP | Pressure intent | Secondary intent | Defense / signature budget |
|---|---:|---|---|---|
| Charter-Shell Snail | **84–96** | `Charter Toll` — **10–13 dmg + Wergild 1** | `Slow Levy` — **15–18 dmg** | `Shell Charter` — **20–24 Block**; Base-Cost-0 Offering restriction unchanged |
| Streamside Oath-Fish | **78–90** | `Oath Bite` — **11–14 dmg + Wergild 1** | `Current Strike` — **15–19 dmg** | `River Shelter` — **16–20 Block**; full payment still grants **2 Safe-Conduct** |
| Two-Bank Toll Ford | **90–104** | `Collect Both Banks` — **12–15 dmg + 1 Trespass** | `Flood Toll` — **16–20 dmg** | `Close the Crossing` — **20–24 Block**; newly created Claim still produces **Wergild 1** |

## Stage 5 — Wayside Covenants

| Enemy | Solo HP | Pressure intent | Secondary intent | Defense / signature budget |
|---|---:|---|---|---|
| Roadside Witchling | **86–98** | `Courtesy Gift` — **+1 Safe-Conduct, 0 dmg** | `Roadside Hex` — **12–15 dmg + 1 Trespass** | `Warm Kettle` — **16–20 Block or heal 4–6 HP** |
| Blackthorn Bride | **94–108** | `Thorn Vow` — **13–16 dmg + 1 Trespass** | `Blackthorn Embrace` — **18–22 dmg** | `Veil of Thorns` — **18–22 Block**; Claim 2 still creates **Wergild 2** |
| Crossroads Cup | **76–88 solo-equivalent** | `Offer the Cup` — **+1 Safe-Conduct** | `Spill at the Crossroads` — **10–13 dmg + 1 Trespass** | `Silver Rim` — **16–20 Block**; first Safe-Conduct use still creates a Claim elsewhere |

## Stage 6 — The Quorum Ring

| Enemy | Solo HP | Pressure intent | Secondary intent | Defense / signature budget |
|---|---:|---|---|---|
| Mandated Mushroom Circle | **88–100 solo-equivalent** | `Call Quorum` — **10–13 dmg + 1 Trespass** | `Common Mandate` — ally may consume Circle Claim | `Ring of Caps` — **20–24 Block**, or **8–12 Block to each ally** in a duo |
| Bracken Moot | **92–104 solo-equivalent** | `Hear Petition` — **10–13 dmg + 1 Trespass** | `Majority Finding` — **16–20 dmg** after Hearing buildup | `Adjourn` — **20–24 Block** |

## Stage 7 — Mire of Appeals

| Enemy | Solo HP | Pressure intent | Secondary intent | Defense / signature budget |
|---|---:|---|---|---|
| Ditch Lamprey of Appeals | **92–104 solo-equivalent** | `Attach to the Appeal` — **11–14 dmg** + existing Claim transfer | `Drain Standing` — **16–20 dmg** | `Bog Grip` — **18–22 Block**; returning Claim is normally **0 dmg** |
| Sedge Bench | **104–118 solo-equivalent** | `Contempt of Review` — **17–21 dmg + 1 Trespass** | `Call the Matter` — create **Wergild 1–2** from reviewed source | `Hold Under Review` — **20–24 Block** |

## Stage 8 — Old-Growth Precedents

| Enemy | Solo HP | Pressure intent | Secondary intent | Defense / signature budget |
|---|---:|---|---|---|
| Sleeping Stump Auditor | **110–124** | `Old Measure` — **12–15 dmg + 1 Trespass** | `Ring Strike` — **18–22 dmg** | `Rooted Stay` — **22–26 Block**; Claims strengthen the Local Law rather than raw stats |
| Precedent Lichen | **92–104 solo-equivalent** | `Cite Authority` — **8–11 dmg + copy 1 Local Law** | `Old Stone Flake` — **14–18 dmg** | `Lichen Hold` — **18–22 Block** |
| Footfall Root | **116–132** | `Remember Footstep` — **13–16 dmg + 1 Trespass** | `Memory Crush` — **16–20 dmg + 3 per Memory**, cap **+12** | `Deep Root` — **22–28 Block** |

## Stage 9 — Moonlit Jurisdictions

| Enemy | Solo HP | Pressure intent | Secondary intent | Defense / signature budget |
|---|---:|---|---|---|
| Untranslated Trail Marker | **114–128** | `Unreadable Warning` — **13–16 dmg + 1 Trespass** | active Reading supplies the Local Law | `Turn the Sign` — **20–24 Block** |
| Elsewhere Path | **106–120** | `Arrive Elsewhere` — **14–17 dmg + 1 Trespass** | `Dead-End` — **19–23 dmg** | Destination selection itself should deal **0 dmg** |

### Returning Stage-9 forms

| Returning identity | Suggested solo-equivalent HP | Intent adjustment |
|---|---:|---|
| Permit Hare — Two Jurisdictions form | **104–118** | Road/Hill law swap unchanged; attacks rise to **14–19 dmg**, defensive intent **20–24 Block** |
| Boundary Stone — Superior Jurisdiction form | **112–126** | attacks **15–20 dmg**, defensive intent **22–26 Block**; Superior Jurisdiction itself deals no damage |

## Stage 10 — Court Beneath the Hill

| Enemy | Solo HP | Pressure intent | Secondary intent | Defense / signature budget |
|---|---:|---|---|---|
| Keeper of Buried Names | **124–140** | `Speak the Name` — **14–18 dmg + 1 Trespass** | `Buried Demand` — consume Claim to create **Wergild 2–3** | `Name Below` — **20–24 dmg** or `Crypt Seal` — **22–26 Block** |
| Handworn Tally Coin | **106–120 solo-equivalent** | `Pass Hand to Hand` — **12–15 dmg** | `Minted Rebuke` — **17–21 dmg** | `Minted Shelter` — **18–22 Block**; existing 3-Tally loop unchanged |

---

# ACT IV — THE LICENSING LABYRINTH

Act IV is substantially longer and mechanically denser than the earlier acts.

Its HP rises, but the intent is **not** to turn every standard enemy into a sponge.

Late-stage difficulty should come from:

- overlapping status conversion;
- scheduled threats;
- recurrence;
- transformed familiar enemies;
- demanding target priority.

## Stage 1 — Boundary Stelae

| Enemy | Solo HP | Pressure intent | Secondary intent | Defense / signature budget |
|---|---:|---|---|---|
| Reed-Cord Surveyor | **80–90** | `Set the Measure` — **9–12 dmg + Primary Weighed** | `Reed Lash` — **13–16 dmg** | `Re-Tension Cord` — **14–18 Block**; near miss ≈ **1 Paperwork**, major miss ≈ **2 Paperwork** |
| Crooked Rod Bearer | **84–94** | `Crooked Measure` — **10–13 dmg + Weighed 1/3 cycle** | `Rod Strike` — **13–17 dmg** | `Brace the Standard` — **15–19 Block**; failed measure normally **1 Paperwork** |

## Stage 2 — Gate of Counted Names

| Enemy | Solo HP | Pressure intent | Secondary intent | Defense / signature budget |
|---|---:|---|---|---|
| Uncounted Pilgrim | **86–98** | `Petition Entry` — **10–13 dmg + 1 Inscribed** | `Uncounted Blow` — **14–17 dmg** | `Unregistered Shelter` — **16–20 Block** while Uncounted |
| Cobra of the Entry Mark | **88–100** | `Entry Mark` — **9–12 dmg + 1 Inscribed** | `Entry Venom` — **12–15 dmg + 1 Poison**; Inscribed may add **+1 stack** | `Coiled Seal` — **15–19 Block** |
| Name-Eating Baboon | **84–96** | `Steal the Name` — **8–11 dmg + 1 Inscribed** | `False Credential` — **11–14 dmg + 1 Doubt** | `Scramble the Gate` — **16–20 Block**; Stolen Name adds **+1 stack** to later status |

## Stage 3 — Granary Courts

| Enemy | Solo HP | Pressure intent | Secondary intent | Defense / signature budget |
|---|---:|---|---|---|
| Crocodile of the Short Measure | **94–108** | `Short Measure` — **11–14 dmg + Primary Weighed** | `Load the Scale` — **10–13 dmg + 1 Burdened** | `Snap at the Deficit` — **17–21 dmg** |
| Jar-Seal Scarab Swarm | **88–100** | `Seal Swarm` — **3×4 to 3×5 dmg**; if any HP hit lands → **1 Burdened** | `Scuttle` — **10–13 dmg** | `Seal the Jar` — **16–20 Block** |
| Hungry Grain Thief | **90–102** | `Sack Weight` — **10–13 dmg + 1 Burdened** | `Feast on Rations` — **16–20 dmg** or heal **4–6 HP** at threshold | `Hide in the Granary` — **16–20 Block** |

## Stage 4 — Floodmark Basins

| Enemy | Solo HP | Pressure intent | Secondary intent | Defense / signature budget |
|---|---:|---|---|---|
| Flood-Mark Reader | **98–112** | `Read the High Mark` — **11–14 dmg + Primary Weighed** | `Silt Lash` — **15–19 dmg** | `Levee Notes` — **17–21 Block**; failed Weighed → **1 Entombed** |
| Drowned Field Scribe | **100–114** | `Silted Filing` — **12–15 dmg + 1 Paperwork** | `Drowned Record` — **16–20 dmg** | `Mud Ledger` — **18–22 Block**; at threshold Paperwork becomes **2 stacks** |
| Silt-Buried Farmer Shade | **102–116** | `Keep the Furrow` — **10–13 dmg + Primary Weighed** | `Mud Pull` — **13–16 dmg + 1 Entombed** | Flood completion → **1 Entombed**; `Raise the Bank` — **18–22 Block** |

## Stage 5 — Tribute Causeway

| Enemy | Solo HP | Pressure intent | Secondary intent | Defense / signature budget |
|---|---:|---|---|---|
| Foreign Tribute Shade | **108–122** | `Assess Tribute` — **12–15 dmg + Primary Weighed** | `Foreign Levy` — **17–21 dmg** | successful first Weighed each round still gives **1 Paperwork**; `Seal Receipt` — **18–22 Block** |
| Donkey of the Third Tally | **112–126** | `Tally Kick` — **13–16 dmg** | `Load Register` — **10–13 dmg + 1 Burdened** | third Tally → **1–2 Burdened**; `Brace the Load` — **18–22 Block** |
| Empty-Handed Envoy | **96–108 solo-equivalent** | `Diplomatic Rebuke` — **14–17 dmg** | failed empty-hand measure → **1 Inscribed** | successful empty-hand measure removes **14–18 Block / defensive layer**; `Empty Palm` — **17–21 Block** |

## Stage 6 — Corvée Yards

| Enemy | Solo HP | Pressure intent | Secondary intent | Defense / signature budget |
|---|---:|---|---|---|
| Rope-Gang Wraith | **112–128** | `Keep the Rhythm` — **13–16 dmg + 1 Fatigue** | `Rope Snap` — **18–22 dmg**, plus **+4–8** if Work Strain active | `Pull Together` — **20–24 Block** |
| Runaway Laborer | **96–108** | `Desperate Swing` — **12–15 dmg** | `Hide Behind the Gang` — **16–20 Block** | at **2 Escape**, leaves combat; do not inflate HP to compensate |
| Stone-Hauler Ushabti | **120–136** | `Haul Stone` — **14–17 dmg + 1 Burdened** | `Stone Blow` — **17–21 dmg + 3 per Stone**, cap **+9** | `Brace the Load` — **22–28 Block** |

## Stage 7 — Monument Works

| Enemy | Solo HP | Pressure intent | Secondary intent | Defense / signature budget |
|---|---:|---|---|---|
| Fallen Capstone Golem | **136–154** | `Falling Dust` — **14–17 dmg + 1 Entombed** | `Set the Capstone` — **22–28 dmg + 4 per Entombed**, cap **+12** | `Set Support` — **24–30 Block** while Placement advances |
| Cornerstone Oath-Stone | **128–146** | `Foundation Measure` — **12–15 dmg + Primary Weighed** | `Broken-Oath Smash` — **18–22 dmg + 4 per Broken Oath**, cap **+12** | `Foundation Wall` — **24–30 Block**; Kept Oath may reduce a later hit by **3–5** |

## Stage 8 — Hall of Reed and Ink

| Enemy | Solo HP | Pressure intent | Secondary intent | Defense / signature budget |
|---|---:|---|---|---|
| Palette-Bearing Apprentice | **112–126** | `Fresh Pigment` — **11–14 dmg + 1 Inscribed** | `Brush Stroke` — **16–19 dmg** | `Palette Guard` — **18–22 Block**; first Inscribed each round gains **+1 stack** |
| Hieroglyphic Complaint Wall | **142–160** | `Preserve the Complaint` — **10–13 dmg + 1 Embalmed + 1 decaying status** | `Carved Accusation` — **16–20 dmg + 1 Paperwork** | `Stone Defense` — **28–34 Block**; Complaint adds roughly **+2 dmg each**, cap **+8** |

## Stage 9 — Courts of the Royal Seal

| Enemy | Solo HP | Pressure intent | Secondary intent | Defense / signature budget |
|---|---:|---|---|---|
| Sun-Seal Bearer | **126–142** | `Authorized Mark` — **12–15 dmg + 1 Inscribed** | `Seal Strike` — **18–22 dmg** | `Royal Impression` — **22–28 Block**; while Blocked first original status gains **+1 stack** |
| False-Seal Forger | **116–132 solo-equivalent** | `Forgery Setup` — **10–13 dmg + 1 Doubt** | `Imitation Cut` — **15–18 dmg** | `Counterfeit Seal` — **18–22 Block**; first foreign status gets exactly **+1 Replicated stack** |

## Stage 10 — Processional Galleries

| Enemy | Solo HP | Pressure intent | Secondary intent | Defense / signature budget |
|---|---:|---|---|---|
| Kneeling Petitioners | **112–128 solo-equivalent** | `Petition Chant` — **12–15 dmg + 1 Doubt** | `Public Approval` passive — first foreign status grants **6–9 Block to all enemies** | `Kneel in Unison` — **8–12 Block to each ally** or **20–24 self Block** |

## Stage 11 — House of Linen

| Enemy | Solo HP | Pressure intent | Secondary intent | Defense / signature budget |
|---|---:|---|---|---|
| Natron Bearer | **136–152** | `Drying Rite` — **12–15 dmg + 1 Embalmed + 1 Fatigue** | `Natron Dust` — **16–20 dmg + 1 Doubt** | `Pack Natron` — **24–30 Block**; prevented decay → **1 Entombed**, max once/round |
| Linen-Wrapped Embalmer | **142–160** | `Write Instructions` — **11–14 dmg + 1 Inscribed** | `Wrap Tight` — **13–16 dmg + 1 Embalmed** | `Linen Guard` — **24–30 Block**; Inscribed-enhanced Embalmed → **1 Burdened**, max once/round |
| Unfinished Mummy | **150–170** | `Incomplete Wrapping` — **12–15 dmg + 1 Embalmed** | `Hook Drag` — **19–23 dmg** | `Stillness` — **24–30 Block**; first Attack under Embalmed → **1 Entombed** |

## Stage 12 — Canopic Vaults

| Enemy | Solo HP | Pressure intent | Secondary intent | Defense / signature budget |
|---|---:|---|---|---|
| Fourfold Vessel Guardian | **160–180** | `Body Office` — **14–17 dmg + 1 Burdened** | `Breath` — **12–15 dmg + 1 Panic**; `Blood` — **13–16 dmg + 1–2 Poison** | `Name` — **10–13 dmg + 1 Inscribed**; optional cycle guard **26–32 Block** instead of adding extra statuses |

## Stage 13 — Necropolis Warrens

| Enemy | Solo HP | Pressure intent | Secondary intent | Defense / signature budget |
|---|---:|---|---|---|
| False-Door Finder | **142–158** | `Certify Passage` — **12–15 dmg + Primary Weighed** | failure → **1 Trespass**; success → **+1 Safe-Conduct** | `False Threshold` — **17–21 dmg**; `Stone Door` — **24–30 Block** |
| Cursed Loot Bearer | **148–166** | `Cursed Load` — **15–18 dmg + 1 Burdened** | `Loot Swing` — **20–24 dmg** | `Inventory Shield` — **24–30 Block**; Burdened surcharge → **1 Paperwork per card**, max once/card |

## Stage 14 — Chamber of Fixed Days

| Enemy | Solo HP | Pressure intent | Secondary intent | Defense / signature budget |
|---|---:|---|---|---|
| Star-Table Scribe | **152–170** | `Fixed Decan Measure` — **13–16 dmg + Weighed 1→2→3 cycle** | `Star Stroke` — **18–22 dmg** | `Table Cover` — **24–30 Block**; failed measure may add **1 Inscribed** |
| Moon-Cycle Ibis | **146–164** | `Set the Rite` — **14–17 dmg + 1 status stack** | `Moon Peck` — **19–23 dmg** | `Wing Shelter` — **22–28 Block**; cycle repeats exactly **1 stack** of Last Rite |
| Eclipse Scarab | **164–184** | `Solar Scar` — **18–22 dmg** | `Approach Noon` — **24–30 Block** | every fourth turn `Black Noon` — **10–14 dmg + 2 Panic + 1 Entombed** |

## Stage 15 — Cartouche Chambers

| Enemy | Solo HP | Pressure intent | Secondary intent | Defense / signature budget |
|---|---:|---|---|---|
| Name-Erasing Chisel Spirit | **156–176** | `Chisel Cut` — **18–22 dmg + 1 Doubt** | `Chip the Cartouche` — **21–25 dmg** | `Stone Dust` — **24–30 Block**; first positive status gain each round is erased |
| Royal Genealogy Wall | **176–198** | `Dynastic Rebuke` — **18–22 dmg** | spend Royal Favor for **+3 dmg per Favor**, cap **+9** | `Royal Line` — **28–36 Block**, optionally **+4 Block per Favor**, cap **+12** |

## Stage 16 — Hall of the Balance — Final Forms

These are transformed versions of existing identities and do not count toward the 35-body roster.

| Final form | Solo HP | Pressure intent | Secondary intent | Defense / signature budget |
|---|---:|---|---|---|
| Feather-Bearer | **174–194** | `True Balance` — exact Primary Weighed target | `Feather Sweep` — **20–24 dmg** | success opens **18–24 points of defensive loss / vulnerability**; failure retaliation ≈ **16 dmg + 5 per point of distance**, cap around **31**; `Balance Guard` — **26–32 Block** |
| Crocodile Beneath the Balance | **186–208** | `Jaws Closed` — **18–22 dmg** | when Jaws open → **26–32 dmg**, optionally **+3 per Entombed**, cap **+9** | `Wait Beneath` — **24–30 Block**; Jaws open only from known Weighed/Entombed conditions |

## Stage 17 — Sealed Court Before Eternity — Final Forms

| Final form | Solo HP | Pressure intent | Secondary intent | Defense / signature budget |
|---|---:|---|---|---|
| Golden Ushabti Captain | **182–204 solo-equivalent** | `Golden Maul` — **21–25 dmg** | `Issue the Load` — **14–17 dmg + 1 Burdened** | `Command Brace` — **10–14 Block to each ally**, plus Stone-resource scaling |
| Eternal Reed Scribe | **166–188 solo-equivalent** | `Unclosing Entry` — **14–17 dmg + 1 negative status** | `Eternal Script` — **20–24 dmg** | `Reed Ward` — **24–30 Block**; Preserved Entry prevents **one** natural decay only |
| Oathbound Gate | **210–238** | `Read the Oath` — **15–18 dmg + visible compliance check** | `Broken-Oath Judgment` — **24–30 dmg + 4 per Broken Oath**, cap **+12** | `Bar the Way` — **30–38 Block**; imported Oath Memory capped at **2 tokens** |

### Final trio scaling recommendation

For `The Sealed Court Before Eternity`, start approximately at:

- Oathbound Gate: **62–64%** of solo HP;
- Golden Captain: **49–51%**;
- Eternal Reed Scribe: **46–49%**.

Across the provisional solo-equivalent bands, this targets roughly **296–349 combined HP** before later tuning. The final standard trio remains a severe capstone encounter, but it no longer risks exceeding the raw body mass of a Very-High Act-IV elite with the same three-body action economy.

---

# IMPLEMENTATION BALANCE CHECKLIST

Before numeric values are considered final, every standard encounter should pass all of the following tests.

## Time-to-kill

A normal solo should usually survive long enough to demonstrate its signature:

> **2–5 player turns**, depending on stage and role.

A fragile support in a duo may legitimately die faster.

A tank should not survive merely because its HP is enormous; its defensive identity should come from:

- Block;
- scheduled protection;
- target-priority mechanics.

## Damage budget

A standard enemy should not combine in one ordinary intent:

> top-of-act major-hit damage **and** a major multi-stack status payload

unless it is clearly telegraphed as a signature/cooldown action.

## Status budget

Prefer:

- **1 status stack + medium damage**;
- **2 stacks + light damage**;
- **major damage + no new status**.

## Duo audit

For every duo ask:

1. Which enemy is the natural priority target?
2. Can the player realistically kill that target before its combo payoff if they commit damage?
3. Does removing one enemy meaningfully simplify the fight?
4. Are the two HP pools scaled so that the answer is not simply “kill the lower-HP enemy every time”?

## Trio audit

A trio should have:

- one anchor;
- one support;
- one pressure body.

It should not contain three full solo HP pools.

## Final rule

These values are the **first implementation target**.

Playtest data may move them substantially.

The desired outcome is not numerical symmetry.

It is:

> **every enemy survives long enough for its idea to matter, but not so long that the player has already solved the idea and is merely reducing HP.**

