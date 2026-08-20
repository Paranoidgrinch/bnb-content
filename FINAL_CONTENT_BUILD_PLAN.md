# Final Content Build Plan — cards → relics → events → run systems

Source of truth (in `~/Downloads`, copied into `source-data/design/`):

| Doc | Scope |
|---|---|
| `general_final_cards.md` | 50 character-unspecific reward cards, 5 general statuses |
| `bureaucrat_final_cards.md` | 4 starters, 4 Junk, 80 Bureaucrat reward cards, Bureaucrat keywords |
| `BnB_Final_Relics_Master_PostAudit.md` | 168 relics (50 normal / 24 shop / 25 event / 69 boss) |
| `BnB_Final_Events_Master_PostAudit.md` | 65 events (I 15 · II 15 · III 15 · IV 20 · V 0) + 25 event relics |
| `BnB_Run_Systems_Master.md` | treasure/mimic, shop inventory, campfire, Act V rules |

These OUTRANK every older doc and the ported v2 `source-data/cards`, exactly as the
FINAL_AUDIT enemy pools outranked the demo enemies (see ACT_I_BUILD_PLAN.md).

The user's build order is **cards → relics → events → systems master**. Within each
phase the work runs **Act I first** (the only act that exists as a playable blueprint),
then II–IV, so every batch is testable in a live fight the day it is written.

---

## Where the content lives

The v2 card DSL (`source-data/cards/*.json` → `CardMapper`) cannot express the final pool
("if the target is Ratified, repeat this attack"). The final cards are therefore **hand-authored
C#** under `Converter/Cards/`, the same way the elites and bosses are hand-authored — the DSL
path stays only for whatever old data still feeds the demo.

```
Converter/Cards/CardAuthoring.cs     shared builders (Deed/Working/Rite, upgrade pairing, tags)
Converter/Cards/Keywords.cs          the status + keyword substrate (below)
Converter/Cards/BureaucratStarter.cs 4 starters + 4 Junk
Converter/Cards/BureaucratActI.cs …  the 80 reward cards, one file per act
Converter/Cards/GeneralActI.cs …     the 50 general cards, one file per act
```

Every card is authored **with its upgrade** (`<id>` + `<id>+`) because the engine's
`UpgradeSuffixWhenDefined` deck mapper resolves upgrades by id suffix.

---

## Phase A — the keyword substrate

Nothing else can be built until these are real. Each row says whether it is data-only
(authorable today) or needs engine work in RogueDeck-Core.

### Bureaucrat keywords

| Keyword | Rule | Build |
|---|---|---|
| **Paperwork X** | end of bearer's turn: lose X HP, ignores Block, no decay | data — raw `TurnEnded` program with `DamageKind.DamageOverTime` (NOT the DoT tag, which ticks at turn start). **Changes the shipped Act-I behaviour** (tick moves start→end); Bookworm moves with it. |
| **Doubt X** | next X Attack *actions* deal 25 % less; 1 stack per action, multi-hit consumes 1 | data — already shipped |
| **Seal / Ratified** | 3 Seal → remove 3, Ratify; Ratified = each Deed vs the bearer deals +3 **once per card** until end of player turn | data for the Seal→Ratify conversion (`StatusStacksChanged` trigger); **ENGINE E-2** for once-per-card |
| **Archive** | move a card to Exhaust as a *distinct action* | data — the Archive macro also pulses a marker status so Rites can react (the debuff-mirror pattern) |
| **Queue** | pay cost + lock target now, resolve FIFO at next turn start before the draw | **ENGINE E-1** |
| **Junk** | 4 generated nuisance cards | data |
| **Temporary copy** | combat-only instance, cannot be re-copied | data (`createCardCopy`) + a `temporary` tag the copy effects refuse to read |

### General statuses

| Status | Rule | Build |
|---|---|---|
| **Censure X** | prevent up to X stacks of an incoming Status (negative on the player / positive on an enemy), spend 1 per stack prevented; never prevents Censure; never prevents a Status paid as a cost | **ENGINE E-3** (the shipped interceptor is all-or-nothing, one polarity, consumes the whole status) |
| **Lien X** | end of holder's turn: remove up to X Block, lose that much HP, reduce Lien by the amount resolved; no Block ⇒ no decay | data — raw `TurnEnded` program |
| **Citation X** | after the holder resolves a **non-damaging action**: lose X HP, remove 1 | **ENGINE E-5** — needs "the action that just resolved dealt no direct damage" |
| **Blood Ink X** | another Status on the holder loses ≥1 stack in one event: lose X HP, remove 1 | data — `StatusStacksChanged` + `StatusExpired` (the last-stack gotcha) |
| **Ward Wax X** | start of your turn gain X Block; after the enemy turn lose 1 (no unblocked Attack damage) or 2 (any) | data — `TurnStarted` + `RoundEnded` with an "unblocked damage this round" counter |

### Engine gaps — status

**Closed** (RogueDeck-Core, all pushed, all with live tests through `RunPlayback.BuildContent`):

| # | What | Commit |
|---|---|---|
| E-6 | `dealDamage` carries a damage KIND, so a status tick is HP loss no Direct-restricted modifier touches | `ba87b5f` |
| E-2 | `OncePerAction` on a damage passive — "+N **total** damage" (Ratified) | `8b38b26` |
| E-3 | Prohibition: partial, stack-for-stack status prevention (Censure) + a trigger event for a refusal + **Anywhere-scoped status triggers**, which is what lets a Rite on the player watch the enemies | `fdb30dc`, `f7162e2` |
| — | An **action scope**: a card play or an enemy action is one action, and `claimOnceThisAction` lets content fire once per action however many hits it makes (Doubt) | `a9dda34` |
| E-1 | **The Queue**: a new zone, `QueueOnPlay`, the turn-start resolution window before the draw, and `resolveQueuedCards` for Night Docket / Processional Calendar | `b2fae86` |
| — | A **causal** card sequence (each clause waits for the previous one) + counting a zone **by tag** | `14ec928` |

**Still open** — each blocks a named set of cards:

- **Reading an enemy's upcoming intent from a combat program.** The telegraph is a Scenario-layer delegate,
  not combat state. Blocks Form of Ill Intent, Conditional Approval, Witchmark Citation, Silent Hearing,
  Guestbook Oath, and several relics.
- **Telling a non-damaging action from a damaging one** — all of **Citation**. The action scope is the
  substrate; the damage question is what is missing.
- **Choose one of N** inside a combat program. Malediction Review, Clerical Discretion, Grand Dispensation,
  Mootcap.

## Phase B — cards

Order: substrate → starter + Junk → Bureaucrat Act I (46) → General Act I (19) → Act II → III → IV.
Each batch: author, add to the reward pools with its act gate + rarity, and prove the non-trivial
ones in a live fight via `Tests/FightProbe.cs`.

Reward-pool shape the run systems need (Phase E) — cumulative act gates:
Bureaucrat 46/60/72/80, General 19/29/39/50.

## Phase C — relics
50 normal (18C/18U/14R, 38 general + 12 Bureaucrat) · 24 shop · 25 event · 69 boss.
Pools are separate and never mix; boss relics are a forced 1-of-3 random per boss.

## Phase D — events
65 events with their branch structure, temporary cards, markings, inscriptions and next-combat rules.
Act I first (15 + 6 event relics).

## Phase E — run systems
Treasure (act mimic check → gold + 1 normal relic), the fixed shop inventory
(3 general + 4 character cards, 2 shop + 2 normal relics, removal), campfire
(Take Authorized Leave = Rest, Submit an Amendment = Smith), Act V restrictions.
All numbers stay at the design's deliberate placeholders — the balance pass is explicitly deferred.

---

## Status

- [x] A — keyword substrate (Paperwork, Doubt, Seal/Ratified, Archive, Censure, Lien, Blood Ink, Ward Wax;
      Citation waits on the engine)
- [ ] B — cards — starters + Junk + **Act I Bureaucrat commons (15/15)** done; 65 Bureaucrat and 50 general
      cards to go
- [ ] C — relics
- [ ] D — events
- [ ] E — run systems
