# B-1 — Act I's fifteen events, against the Post-Audit master

Written at the end of the session that finished B-3, so the next one does not have to re-derive any of it.
The canon is `source-data/design/BnB_Final_Events_Master_PostAudit.md` §"ACT I". The shipped 15 events carry
the same names but different rules — they are ported v2 and are being **replaced**, not edited.

## What already exists (do not rebuild)

| Piece | Where |
|---|---|
| 5 temporary cards, 4 markings + marking rule, 7 next-combat rules | `Converter/Events/ActOneEventObjects.cs` (B-3) |
| All 6 Act-I event relics | `Converter/Relics/EventRelics.cs` — originality_stamp, unclaimed_property_tag, uncalled_ticket, threshold_ward, crossed_out_map, inherited_bone_folder |
| Card ↔ fight carry-over | run card tag → per-instance mark, engine @9e041c8 |
| Held Energy (the pool's hard ceiling) | `Converter/HeldEnergy.cs` |
| Event → engine mapping, effect by effect | `Converter/EventMapper.cs` |

## The shape to build

Act I's events stop being source-data JSON and become **authored C#**, like the cards, relics and bosses:
`Converter/Events/ActOneEvents.cs`, built from `ActOneEventObjects`. `BabLoader` keeps loading the v2 JSON
only until the last of the 15 is replaced, then Act I's event file is dropped from the loader and
`MapSpecBuilder`'s event pool comes from the authored set. (Act II's ported events stay until B-2.)

Every branch is one `EventChoice` with run effects. Three recurring devices:

- **a marking** — `new TagCardsRunEffect(RunSelectors.DeckCards…, new RunCardTagId(ActOneEventObjects.Misfiled), true)`;
- **a next-combat rule** — `Openings.NextCombat(new CombatNodeModel("applyStatus", "source", …, StatusId: ActOneEventObjects.PriorityNumber))`;
- **a temporary card in the next fight** — an opening that `createCardInstance`s it into the draw pile or hand.

## Per event: what it needs beyond the vocabulary

| # | Event | Notes |
|---|---|---|
| 1 | The Misfiling Cabinet | transform + 50 gold + marking · remove + two temporary cards |
| 2 | The Certified Copy Drawer | duplicate + Duplicate Copy · **originality_stamp** + Sealed marking |
| 3 | The Self-Amending Fee Table | pay 150/upgrade 2 · +75 gold + **Audit Notice** (run program on `CombatResolvedRunEvent`, gold −4 per HP lost, cap 80) |
| 4 | The Lost-and-Found Desk | Under Review + **post-combat upgrade** of the tagged card (run program: upgrade `DeckCards.WithTag(under_review)`, then untag) · **unclaimed_property_tag** |
| 5 | The Licensed Vendor | ⚠ "open the vendor" — events cannot open shops (ADAPTATIONS); keep the card-reward adaptation or decide otherwise · 1-of-3 relic pick + Garnished Reward + Fine Print |
| 6 | The Complaint Ledger | plain |
| 7 | The Waiting Token Exchange | **uncalled_ticket** + Restricted Public Hours · upgrade + Priority Number + Notice of Delay in opening hand |
| 8 | The Almost-Helpful Clerk | "a 0-cost Exhaust copy of the card you chose" → mark the card, opening uses `createCardCopy` · **Expedited Route** (enemy −30 % max HP ⇒ opening damage as % of max; no gold ⇒ run program) |
| 9 | The Witness Queue | plain (relic grant, 2 Duplicate Copies, Summons in hand, Witness Protection + Witnessed Procedure) |
| 10 | The Sealed Back Door | extra card reward after the next combat (run program on victory) · **threshold_ward** + enemies open with 4 Strength |
| 11 | The Clerk's Tea Break | plain |
| 12 | The Friendly Filing Cabinet | transform, then Fast-Track the **new** copy (`RunSelectors.LastAddedCard`) |
| 13 | Receipt of Prior Effort | "125 gold if won by round 3, else 25" → a combat rule writes the round into a counter, the run reads `CombatResolvedRunEvent.HeroCounters` (the Shop relics do exactly this mirror trick) |
| 14 | The Contradictory Map | Expedited Route · card reward + Correct Window + Wrong Form · **crossed_out_map** (its "move to any node" is map manipulation a relic cannot reach — check what the relic currently does and write the adaptation down) |
| 15 | The Archive Window | **inherited_bone_folder** + Fine Print · Under Review + returns upgraded + **Certified Original** (a permanent per-card tag + a rule that reads it, same machinery as the markings) |

## Traps this session paid for

- A run is rebuilt from its own answers under the **replay model** — a tag written directly onto `session.Run`
  is written away again. Tests must go through a real event choice, then the fight (`ActOneEventObjectTests`).
- **Energy above the pool max is impossible.** Anything promising Energy at a turn's start must hold it.
- **A turn-end program cannot see the hand** — the discard handler runs first.
- **`turnNumber` counts turns within a ROUND.** Use `roundNumber`, or count your own.
- Static field initializers run in declaration order: ids and counters go ABOVE the rules that name them.

## Order

1. Events 6, 9, 11 (plain) — proves the authored-event shape end to end.
2. Events 1, 2, 4, 7, 12, 15 — the markings, which is what B-3 was built for.
3. Events 3, 8, 10, 13, 14 — the run-level ones (gold rules, post-combat rewards, counters).
4. Event 5 last: it is the one with an open design question.
5. Then drop Act I's ported events from `BabLoader` and let the map draw the authored set.
