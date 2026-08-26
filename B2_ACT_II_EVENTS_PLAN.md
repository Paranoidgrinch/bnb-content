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

## B-2c — Removed History

"When a persistent card is permanently removed, store identity, permanent upgrade state, inscriptions and
explicitly restorable persistent modifications. Restoring recreates the card and deletes the entry."

Three events read it (8 The Infinite Return Slot, 15 The Librarian) and two write to it. **The run layer has no
such store**, and this is the open design decision of B-2:

- `RunState` has flags and counters (ints), not a list of card records. A removed card's identity + upgrade
  level + tags is a record, and there may be several.
- Candidate shapes: (a) an engine seam — a `RemovedCards` list on RunState, snapshotted like the deck; (b) a
  content-only encoding — counters keyed by card id (`removed.<cardId>`), which loses the upgrade level and
  the inscriptions; (c) drop the restore branches and pay those events out another way.
- (a) is the honest one and is small — the deck already snapshots exactly this record shape
  (`RunCardSaveData`). **Decide before writing events 8 and 15.**

## B-2d — the fifteen, and the earliest-stage gate

`Converter/Events/ActTwoEvents.cs`, `AuthoredEvents.For` gains act 2, then Act II's event JSON leaves
`BabLoader`. The four shapes from B-1 all apply (a run effect now · a marking · a one-fight rule · a promise
installed by name in `ActTwoEventPrograms`).

⚠ **Earliest Stage N is not expressible.** Every Act-II event carries one (Misfiled Prophecy 2 … The Necrology
Window 9), and `MapGenerationSpec.NodeRefPools` draws refs without any notion of depth — a stage-1 node can
open the Librarian. Either add a per-ref minimum row to the spec (the third seam of this block) or note the
gate as unhonoured. The pools already draw WITHOUT replacement, so the seam is a filter at draw time.

Events needing more than the vocabulary above:
- **3, 13** "the first fourth card each turn Redacts a random remaining hand card" — a per-turn card-play count.
- **4, 7** Borrower's Keeping / Reservation — done in B-2a; the permanent upgrade on victory is a run program.
- **11 The Necrology Window** — "the primary enemy's first lethal event returns it once at 30 % Max HP" is the
  engine's pre-down interceptor (the Appellate Phantom's Remand is the same shape), installed for one fight.
  "Waits through ineligible Elite/Boss nodes" needs a run program that only fires on a normal combat.
- **13 The Last Quiet Table** — the Vow is a cap on non-Junk cards per turn, and breaking it only forfeits.
- **15 The Librarian** — "enemies have 25 % less Max HP, no Gold, one extra card reward" is Act I's Expedited
  Route plus the Sealed Back Door's extra reward; both already exist as Act-I programs to copy.
