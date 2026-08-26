# B-2 — Act II's fifteen events, against the Post-Audit master

The canon is `source-data/design/BnB_Final_Events_Master_PostAudit.md` §"ACT II". Act II's shipped events are
still the ported v2 ones; they are being **replaced**, exactly as Act I's were in B-1.

Act II is bigger than Act I was: fifteen events, five event relics, three temporary cards, five permanent
inscriptions, a run-level **Removed History**, and a per-event **earliest stage** the map has no way to honour
yet. So it is built in four steps, and the first is done.

## ✅ B-2a — the shared objects (DONE)

`Converter/Events/ActTwoEventObjects.cs`, tested by `Tests/ActTwoEventObjectTests.cs` through
`Tests/ArchiveProbe.cs` (a real event door, then the fight behind it).

| Piece | State |
|---|---|
| 3 temporary cards — Unfinished Citation, Redacted Leaf, Borrower's Claim | ✅ |
| One-fight markings — Misfiled and Redacted (ActTwo's own marks), Borrower's Keeping, Reservation | ✅ `ArchiveMarkingsRule` |
| 5 permanent inscriptions — Authorized Revision, Illuminated Initial, Concordant Pair, True Name, Late-Bound | ✅ one rule each |

**Two engine seams bought for it** (both in RogueDeck-Core, both tested there):
1. **The per-instance output scale may WIDEN.** The mark was read as `num < den` — a redaction could halve a
   card but an inscription could not say "half again as much", although the execution context had always
   allowed both directions. `CardPlay` and `CardOutputScaling.Scale` now treat it as a fraction.
2. **"Put this card on top of the pile it is already in" is no longer a no-op.** A same-zone move returned
   early, so a tutor that fetches within the draw pile did nothing. An explicit `Top` now repositions; `Bottom`
   keeps the historical no-op.

**Adaptations** (write these into ADAPTATIONS.md when B-2c lands):
- A Reference belongs to the enemy that filed it, so there is no single "Referenced" mark — the Citation
  clears **every** kind from the first card in hand carrying that kind.
- The Redacted Leaf redacts as it is **read** (the start of your turn, from your hand), not "immediately
  before resolution" — there is no hook between choosing a card and resolving it.
- Concordant Pair fetches from the **draw pile only**. A played card has already reached the discard pile, so
  a rule reaching in there would fetch the partner that was just played back out of it.
- True Name is a **correction, not a shield**: nothing hears a mark being put on a card, so the first mark the
  archive writes on it is struck off (with its halving) at the start of the next round, once per fight.
- Late-Bound always **Retains**, and pays out from the second turn on — a turn-end program cannot see the
  hand, so the waiting is granted rather than watched.
- A borrowed/reserved card comes back Retaining and free, but **not combat-upgraded**: an in-combat upgrade
  would have to transform a card into its own `<id>+`, which no node can name.

## ✅ B-2b — the five Act-II event relics (DONE)

`Converter/Relics/EventRelics.cs` §ActII + `Converter/Relics/ActTwoEventRelicRules.cs`, tested live in
`Tests/ActTwoEventRelicTests.cs` — each relic taken at a real door, so the test exercises the relic's own run
program installing its rule as the fight opens.

| Relic | Source branch | Adaptation |
|---|---|---|
| **Unreturned Library Card** | The Perpetual Borrower — Pocket the library card | the Exhaust half is dropped (a card Exhausts by DEFINITION) |
| **Reversible Shelf Label** | The Reciprocal Shelf — Take the loose shelf label | it remembers the **copy**, not the name — nothing can hold a name or compare one later |
| **Blank Cameo** | The Redacted Portrait — Restore the missing face | the protection is a **correction**, struck off at the next round's start, like True Name |
| **Vow Bead** | The Last Quiet Table — complete the Vow | the opt-in **cap is dropped**; a cap the player takes each turn would be a prompt every turn, and breaking the Vow costs nothing anyway — the Bead just notices restraint |
| **Inverted Sealstone** | The Inward Seal — Break the seal outward | the card comes home at the **next draw**, not mid-play: the play puts the card where it belongs after the trigger has run |

★ **The two remembering relics pay for the turn-end blindness.** A turn-end program cannot see the hand
(`DiscardHandOnTurnEndedHandler` runs first), so "the card you did not play" is written down while it is still
true: the hand is marked as it is dealt, each played card loses the mark as it is played, and what is still
marked in the discard pile when the turn ends is exactly what was held and never used.

## ✅ B-2c — Removed History (DONE — decided (a), the engine seam)

`RunState.RemovedCards` (RogueDeck-Core @52ee1a4). Every permanent removal writes a `RemovedCardRecord` —
definition, upgrade level, run tags (which is what an inscription is), shred composition — and
`fx.restoreRemovedCard` gives one back: the player picks, the card returns with the state it left with plus
whatever the recovering event adds (`ExtraUpgrades`, `Tags`), and the entry is struck out, so a card is
recoverable once. `removedCardCount` is the expression "if the history has entries" asks. The history rides
through a save with the deck.

Per-copy MEMORY is deliberately not kept: it is a scratchpad a rule owns for as long as the copy exists.
The design's "explicitly restorable persistent modifications" are the run tags, which are kept.

What the two events need, in the vocabulary that now exists:
- **8 The Infinite Return Slot** — "Reach for a lost page" → `RestoreRemovedCardRunEffect()`.
- **15 The Librarian** — "Ask for a forgotten book" → `ConditionalRunEffect` on `removedCardCount > 0`:
  `RestoreRemovedCardRunEffect(ExtraUpgrades: 1, Tags: ["true_name"])`, else a Rare card reward.

## B-2d — the fifteen (NEXT: this is all that is left of B-2)

### The shape, and what to copy

Mirror B-1 exactly — that pass is the worked example, and every piece of it has a twin here:

| B-1 (Act I, done) | B-2d (Act II, to write) |
|---|---|
| `Converter/Events/ActOneEvents.cs` | `Converter/Events/ActTwoEvents.cs` |
| `Converter/Events/ActOneEventPrograms.cs` | `Converter/Events/ActTwoEventPrograms.cs` |
| `AuthoredEvents.For` returns Act I's | …gains `act == ActTwoEvents.Act ? ActTwoEvents.All(pools, rng) : []` |
| `BabLoader` dropped `events/act_1_city_events.json` | drop `events/act_2_archives_events.json` — then `Many<BabEvent>` loads NOTHING and the whole ported-event path (`EventMapper`, `BabEvent`, `PoolsFor`) can go |
| `Tests/EventStory.cs` + `ActOneEventTests` + `ActOneEventLiveTests` | the same three; `EventStory` needs an ARCHIVES fight (it hardcodes `form_rat_a`) — give it the enemy/intent as parameters, or copy `ArchiveProbe`'s `dead_letter_ouroboros` / `self_addressed_notice` |

An event says its thing in one of four ways, all of which now exist:
1. **a run effect now** — gold, a card removed/transformed/upgraded, a relic taken, a card recovered;
2. **something written on one card for ONE fight** — `ActTwoEventObjects` (Misfiled, Redacted, Borrower's
   Keeping, Reservation) + `Openings.NextCombat(Applies(ArchiveMarkings))` + the expire program;
3. **a one-fight RULE** — a status the next fight opens with;
4. **a lasting PROMISE** — an authored run program installed by name (`fx.installProgramById`).

⚠ **An event that writes Misfiled must also open the fight with `ActTwo.ArchiveRegulations`** — that is the
rule which takes a misfiled card back as it reaches your hand, and it is otherwise only installed by the
enemies that misfile. It is idempotent by construction, so installing it twice is free.

### The fifteen, branch by branch (canon: `BnB_Final_Events_Master_PostAudit.md` §ACT II)

| # · Event (earliest stage) | Branches | Beyond the vocabulary |
|---|---|---|
| 1 · Misfiled Prophecy (2) | transform 1 + a different card begins Misfiled · give 1 card **Authorized Revision** + Unfinished Citation into next combat's discard | — |
| 2 · The Self-Correcting Index (6) | upgrade 2, one of them begins Redacted · remove 1, up to 2 others begin Misfiled | — |
| 3 · The Locked Reading Room (4) | **Rare** card reward + next combat: the first FOURTH card each turn Redacts a random other hand card · pay 40 Gold, give 1 card **Illuminated Initial** · heal 20 % | the fourth-card rule (see below) · a RARE-only reward pool |
| 4 · The Perpetual Borrower (7) | lend a card → **Borrower's Keeping**, and victory upgrades the original · choose 1 of 3 Uncommon cards + Borrower's Claim · pay 60 Gold, heal 15 %, upgrade 1 · **Unreturned Library Card** + lose 8 % max HP + Borrower's Claim | the victory upgrade is a run program (Act I's `UnderReviewReturns` is the model) · rarity-filtered reward pools |
| 5 · The Reciprocal Shelf (2) | transform 1 + 50 Gold · card reward + a different card begins Misfiled **and** Redacted (if none eligible: 1 Paperwork instead) · **Reversible Shelf Label** + 1 random card Misfiled in each of the next 2 combats | "each of the next 2" = Act I's `WrongFormAgain` shape |
| 6 · The Margin Notes (3) | give 2 cards **Concordant Pair** · give 1 card **Illuminated Initial** · upgrade 1 + Redacted Leaf into next combat | the Pair needs ONE choice tagging TWO cards — `ForEachCardRunEffect(Choose(2), [TagThisCard])` |
| 7 · Unclaimed Reservation (7) | choose 1 of 3 Uncommon + it begins next combat in **Reservation** · heal 25 % · 70 Gold + one opening-hand card cannot be played until another is | the "locked until another is played" card |
| 8 · The Infinite Return Slot (7) | remove 1 + 40 Gold (the removal writes the history by itself) · **`RestoreRemovedCardRunEffect()`** + Borrower's Claim into next combat | ✅ B-2c |
| 9 · The Redacted Portrait (5) | pay 100 Gold → **Blank Cameo** · give 1 card **True Name** · heal 15 % | — |
| 10 · The Lost-Hour Bottle (8) | next combat R1 +1 Energy, R2 +1, R3 −2 · give 1 card **Late-Bound** | the Energy must be HELD (`HeldEnergy`), never gained — Act I's `RestrictedPublicHours` is the model |
| 11 · The Necrology Window (9) | heal 35 % + next normal combat the primary enemy returns once at 30 % max HP, then +75 Gold on victory · lose 8 HP, remove 1, upgrade another (unavailable if lethal) | the revive is the engine's **pre-down interceptor** (the Appellate Phantom's Remand is the same shape) · "waits through ineligible Elite/Boss nodes" needs a program that only fires on a normal combat |
| 12 · The Almost-Helpful Clerk, Reassigned (1) | a card begins Redacted; playing it while Redacted upgrades it after victory · next combat prevents the first enemy marker + 35 Gold · heal 20 % | "played while still Redacted" needs a combat counter the run reads (Act I's Receipt is the mirror trick) |
| 13 · The Last Quiet Table (4) | the **Vow**: win a combat never playing >3 non-Junk cards in a turn → **Vow Bead** · **Rare** card reward + Redacted Leaf in the opening hand + the fourth-card rule · heal 25 % | the Vow is a combat counter (highest non-Junk count in any turn) the run reads on victory |
| 14 · The Inward Seal (7) | **Inverted Sealstone** + 2 cards begin Misfiled **and** Redacted · upgrade 2, one begins Redacted and the other Misfiled · +8 max HP + next combat opens with 2 Paperwork and 1 Doubt | — |
| 15 · The Librarian at the End of the Aisle (8 · Rare) | if the history has entries: `RestoreRemovedCardRunEffect(ExtraUpgrades: 1, Tags: ["true_name"])`, else a **Rare** card reward · remove 1 + heal 15 % · next normal combat: enemies −25 % max HP, no Gold, one extra card reward | branch 1 is a `ConditionalRunEffect` on `removedCardCount` ✅ B-2c · branch 3 is Act I's Expedited Route + the Sealed Back Door's extra reward, both already written |

Recurring pieces worth building ONCE, in `ActTwoEventPrograms`:
- **the markings expire** after the fight that honoured them (Act I's `MarkingsExpire`, with
  `ActTwoEventObjects.SpentAfterOneFight()`);
- **the inscriptions' rules install in every later fight** (Act I's `CertifiedOriginal`, one per inscription —
  or one program applying all five rules, since a card without the tag makes each rule a no-op);
- **the victory upgrade** of a lent / redacted card (Act I's `UnderReviewReturns`);
- **again next fight** (Act I's `WrongFormAgain`);
- **no Gold** (Act I's `GarnishedReward` + `GarnishThePurse`);
- **an extra card reward** (Act I's `ExtraCardReward`).

### The two things still unbuilt

⚠ **1. Earliest Stage N is not expressible.** Every Act-II event carries one, and
`MapGenerationSpec.NodeRefPools` draws refs with no notion of depth — a stage-1 node can open the Librarian
today. The pools already draw WITHOUT replacement, so the seam is a filter at draw time: a per-ref minimum
row on the spec (`NodeRefMinimumRows`, ref id → row), honoured in `RuleBasedMapGenerator` where it picks a
ref. Act I has no such gate, so nothing existing changes. Decide: build the seam, or write the gate down as
unhonoured in ADAPTATIONS.

⚠ **2. A RARE-only card reward.** Three branches ask for one; `ConversionPools.CardRewardSource()` draws
uniformly from the whole act pool. A `CardRewardSource(rarity)` overload is content-side and small.

### Traps (all paid for already — do not re-derive)

- A run is rebuilt from its own answers under the **replay model**: a tag written straight onto `session.Run`
  is written away again. Always go through a real event choice, then the fight.
- **Energy above the pool max is impossible** — hold it (`HeldEnergy`).
- **A turn-end program cannot see the hand** — the discard runs first. Write the hand down at the draw.
- **`turnNumber` counts turns within a ROUND** — use `roundNumber`.
- **Static field initializers run in declaration order**: ids, counters and shared arrays go ABOVE the rules
  that name them (this bit once already in `ActTwoEventObjects`).
- A **CombatNodeModel has no mark filter** — a card program that must find a card by a per-copy mark is
  written with raw engine nodes and set on `CardData.Program` in `Compile()` (the Unfinished Citation does
  exactly this).
- A **same-zone move only repositions with `ZonePlacement.Top`**; Bottom is still a no-op.
