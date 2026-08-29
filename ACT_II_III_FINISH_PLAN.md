# Acts II & III — the finish plan

**Written 2026-08-28**, from a live audit (bnb-content @d89ff6e · 869/869 green · RogueDeck-Core @280ebf2 ·
bnb-godot @4bd2c61 · all trees clean, nothing pushed).

**The content of both acts is done** — 25 standards, 9 elites, 5 bosses, 15 events, 15 boss relics, 5 event
relics and the act's own rules, for each of Act II and Act III; the card pool is authored through Act IV and
pinned by `FinalCardPoolTests`. What follows is everything BETWEEN "the act is written" and "the act is
finished": six steps, worked in order, one at a time.

---

## The working protocol

We do **one step per context**. After each step:

1. all suites green — `dotnet test` in bnb-content (~11 min, 869 tests) and in RogueDeck-Core when the step
   touched the engine;
2. `dotnet run --project Converter -- --playtest 3` and `-- --maps 3`;
3. `tools/sync-content.sh` + `godot --headless -- --smoke-marathon` when the step changed the document;
4. commit in each repo it touched, with the step's number in the message body;
5. update `project_bnb_port.md` — the resume block at its top names the NEXT step and the state it starts from;
6. **then compact.** The plan file is the handoff: whoever reads it after a compaction needs nothing else.

House rules that already cost a day each (from `ACT_III_BUILD_PLAN.md`, still binding):

- every new status / card / relic needs a description or `EverythingExplainsItselfTests` breaks the build;
- every `CounterId` is a property, never a `static readonly` field (`DocumentIdTests`);
- only registered vocabulary survives the export (`CombatJsonRegistry.KindOf`).

---

## Step 1 · The replay checkpoint

**The problem, stated precisely.** Under the replay model (`ReplayScript`, `InteractiveRunSession.Replay`)
every answer re-executes the run from its baseline up to the first unanswered prompt. The baseline moves only
where somebody moves it, so the cost of one answer grows with the number of answers behind it — quadratic in
the length of whatever segment is being replayed.

**What is already there** (this is why the step is smaller than it looks):

- `RunWalker.Reload` saves at an interlude and resumes from that save, and `Walk(..., saveEvery: 5)` already
  does it every fifth interlude. So the walker's baseline is *already* capped at five rooms — and it still
  stalls in the Great Toll Frog. **The remaining cost is therefore mostly INSIDE one fight, plus the tail of
  up to four earlier rooms replayed on every card play.**
- The Godot host (`GameHost`) does **no** checkpointing at all — it only saves when asked. Its latency is the
  full run.
- The engine already has a **mid-combat snapshot**: `CombatState` snapshot/restore, `CombatSaveJson`, and
  `InteractiveCombat` resuming from a restored state. Nothing new has to be invented for 1d.

**1a · Measure before fixing. — DONE 2026-08-28.** `RunWalker` now carries a `Meter`: every answer is counted
where it is given, every room reports what it cost when the walk leaves it, and every combat turn reports its
own price. Walked `--playtest 2 --seed 20260801`.

*What it showed.* A clean sawtooth, exactly the shape the replay model predicts:

| | ms per answer |
|---|---|
| the room right after a checkpoint | **10 – 14** |
| four rooms later (the end of a `saveEvery: 5` window) | **200 – 295** |
| act II, before the fix, at the 500th answer | 54.3 s of walk behind it |

So the baseline's distance is the whole cost, and the constant per replay (rebuilding the initial run, every
act's map included) is under ~13 ms — small. **The fix is to move the baseline, not to make replay cheaper.**

**1b · Checkpoint at EVERY interlude, in the session rather than in the walker.** `InteractiveRunSession`
holds `_makeRun`; the checkpoint is: at an interlude park, snapshot the run, rebase `_makeRun` onto a restore
of that snapshot, and clear the script. The session object keeps its identity, so `Changed` subscribers and
the Godot screens need no change. `RunPlayback` supplies the restore function (it owns the blueprint and
already has `RestoreInItsAct`). Then the walker's `saveEvery` becomes redundant and the Godot host gets the
same cap for free.
*The correctness question this step must answer with a test:* a restore has to reproduce the live state
exactly, or the state after the next replay differs from the one the player was just looking at. Test: walk a
whole run checkpointing at every interlude and assert the same report as a walk with no checkpoint at all.

**1c · Re-measure. — the fix is in, first numbers below.** The same walk, same seed, same answers: the walk
reached act II's `r0c2` at the **500th answer in both runs** — answer for answer identical, which is the
determinism claim holding — and took **28.2 s instead of 54.3 s** to get there. Per-room cost no longer
climbs across a window: it stays in the **10 – 67 ms** band. What still grows is the cost INSIDE one fight
(a fight's own answers are still all replayed): an act-III fight measured 124 → 153 ms/answer between turns
10 and 13.

**1c-bis · The Great Toll Frog was never the replay model at all.** With the baseline moving, the walk
reached the Frog (`r22c2`) in 124 s instead of never — and then hung again, at 104 % CPU, inside a single
turn. Instrumenting the play loop named the cause, and it is not a bug in the act:

> **`Make Amends` costs nothing and puts a fresh copy of itself back in your hand while anything is still
> owed** — deliberately, so that a payment which could not go through (an empty purse, the Juniper's
> injunction against coin) still leaves a way to try again.

A human ends the turn. The walker is a greedy player with no reason to stop: the returned copy is a NEW
instance, so refusing the instance does not help, and the play is not *refused* — it simply achieves nothing.
The walker now reads the table either side of a play (energy, both healths, both status counts, all four
zones) and, when nothing moved, **stops offering that card by DEFINITION for the rest of the turn**. Behind it
sits a backstop: fifty plays in one turn is not a turn anybody makes, so it ends the walk with a note rather
than spinning. Neither touches the game — the instrument was the thing that could not stop.

**1d · Only if 1c still stalls: the in-fight checkpoint.** At each player turn boundary, snapshot the combat
(`CombatSaveJson`) and rebase the replay onto a run whose current node resumes that combat
(`InteractiveCombat` already resumes; the seam to buy is entering a node with a pre-restored combat). This
caps a fight's cost at one turn. It is the expensive half of this step and is deliberately conditional.

**1d · The in-fight checkpoint was NOT needed.** With 1b and 1c-bis in, the fight's own answers are the only
thing replayed inside a fight and they stay in the tens of milliseconds. Not built; the combat snapshot seam
stays available if a later act needs it.

**Done — 2026-08-28.** `--playtest --seed 20260801` reports **`ok seed 20260801: Victory, 73 rooms over 3/3
acts, 305 steps`** — the first run ever played from the city's first room to the Green Docket's last.
`WholeRunTests` no longer takes only the first two acts: it walks every act the document has.

---

## Step 2 · Act III into the game

`game.roguedeck.json` — here and in `bnb-godot/content/` — is from 2026-08-27 and says `Acts: 2`. The Green
Docket exists only in the converter.

- regenerate the document (`dotnet run --project Converter`), check `Acts: 3` and the encounter / relic counts;
- `tools/sync-content.sh`;
- `godot --headless -- --smoke-marathon` — with step 1 done this must now run to the end. Record
  `result=Victory acts=3 rooms=…`;
- commit the document in both repos (bnb-content and bnb-godot).

**Done when:** a Godot marathon reports a victory over three acts.

**Done — 2026-08-28.** `smoke-marathon: result=Victory acts=3 rooms=73 error=none` — the Godot host plays
the whole game, 23 + 24 + 26 rooms, each act ending on its boss. The document carries all three acts (364
cards, 214 encounters, 182 relics, 58 events, 3 shops) and is synced into `bnb-godot/content/`. The three
`--playtest` walks and `--maps 3` are clean.

*What it cost, and why it is worth writing down.* Regenerating and syncing took a minute; the marathon then
stopped at `archives_elite_after_hours_return_bell` with `result=Ongoing acts=2` and — this was the whole
problem — **said nothing about why**. Three things had to be fixed, none of them in the content:

1. **The probe now says why it stopped.** Every `break` in `SmokeMarathon` sets a reason, and the summary
   prints it with the room and the encounter that was being fought (`stopped because the step limit ran out
   at act 2 r7c1 (archives_elite_after_hours_return_bell)`). A probe that stops silently is worth nothing:
   `result=Ongoing` looks identical whether the run met a wall or the harness did.
2. **The probe's greedy player got RunWalker's three guards** — do not replay a card the engine REFUSED, do
   not repeat a card that moved nothing on the table, and cap the turn (50 plays) and the fight (100 turns).
   Without them it spent its entire 20 000-step budget in one elite. See step 1 and `ADAPTATIONS.md`.
3. **★ The guards must not key on `driver.Current`.** Under the replay model the fight is rebuilt from the
   blueprint on *every answer*, so an instance comparison reports "a new fight" at every step and silently
   resets every counter. This cost one whole run to find: the guards were in, and nothing changed. A fight
   begins when the driver has one and ends when it does not; turns are counted where the probe ends them.

Also removed: `CardVisuals.Back` called `VideoStreamPlayer.Play()` before the node was in the tree, next to an
`Autoplay = true` that already does exactly that when it enters. Harmless, but it printed one
`Condition "!is_inside_tree()" is true` plus a fourteen-frame backtrace per card back — 283 error lines in a
single marathon, enough to bury the verdict itself.

---

## Step 3 · The Warden of Sealed Volumes ends

The Act-II boss does not finish within 100 turns — the oldest open finding, and it stops any walk that draws
him. Sources: `Converter/Bosses/WardenOfSealedVolumes.cs`, `Tests/WardenOfSealedVolumesTests.cs`,
`ADAPTATIONS.md` §"Act II bosses — The Warden of Sealed Volumes", and the boss master's audit entry.

Diagnose first: probe the fight and find out whether it is a healing/block loop, an unreachable phase gate, or
simply too much HP behind too little pressure. Fix in content, not in the engine, unless the probe proves a
rule cannot be expressed. Add a test that pins the fight's LENGTH, not only its rules — a turn budget the
boss must die inside, with the walker's own greedy player.

**Done when:** the Warden dies inside a stated turn budget under the walker's play, and a `--playtest` seed
that draws him walks through.

**Done — 2026-08-28. ★ The Warden was never guilty.** Nothing had ever drawn him — no walk, no marathon — and
the finding was inference from a walk that stopped in Act II for another reason. Put on the table against the
character's own starting deck with the walker's greedy player, he goes down in **25 turns**; seeds 20260901
and 20260904 now draw him and finish the game. The week he spent on this list is the argument for everything
below: an accusation nothing ever tested is not a finding.

Two real faults were underneath, both bought as engine seams in RogueDeck-Core (details in `ADAPTATIONS.md`
§"The boss that would not end, and the crash that was actually killing the walk"):

1. **★ A card that resolves the Queue could be put IN the Queue, and then resolved itself for ever — a stack
   overflow that took the process down.** Night Docket ("resolve your oldest Queued card") plus Skeleton
   Staff ("queue a card from your hand", which does not ask what the card does). A queued card leaves the
   Queue only once its program has finished, so the nested window found it still waiting. Priority Docket and
   Customary Due are the same door in Act III. Fixed in the engine, not the content: no rule an author writes
   should be able to kill the process. `CombatState.IsResolvingQueuedCard` + `QueueResolution` passes over a
   card that is mid-resolution. Pinned by two new `QueueTortureTests`.
2. **★ A body at zero was not down unless DAMAGE put it there.** The Grandmother Clause pays 5 HP for every
   courtesy the player keeps — a `SetHealthNode`, not damage — and paying her last five left her standing at
   `0/350` for eighty-eight turns in a fight that could not end. Content writes "loses N HP" that way in
   nineteen files. `SetHealthEffectHandler` now downs a combatant whose pool it empties. This reverses a
   documented decision (`SetHealthTests` said the opposite in its header and pinned it in a test named for
   it); of 3110 tests exactly one depended on the old reading. **User decision, asked and given.**

And two faults in the instruments themselves, which is why the Warden was blamed for a week:

3. **A walk's turn line reported only its timing.** A fight at turn 80 losing 6 HP a turn and a fight stuck
   since turn 12 looked identical. It now names who is still standing and on how much health — and that one
   line turned the Grandmother from a mystery into a diagnosis on the first run.
4. **`--playtest n --seed s` seeds two things**, the game and the first walk (`s`, `s+1`, …), so a failure
   reported as "seed 20260909" is not reproducible by `--seed 20260909`: that builds a different game. The
   header now says which game is being walked.

**And the test is a net, not a probe:** `Tests/BossLengthTests.cs` fights **all fifteen bosses of Acts I–III**
with the walker's greedy player, the starting deck and an unkillable tester, against a 40-turn budget. A walk
only ever meets the boss its seed picked; every one of the fifteen was one seed away from being that boss.

---

## Step 4 · The shop, against Run Systems Master §4.1–4.3

The master fixes every regular shop at **3 general cards + 4 character cards** and **2 Shop-exclusive relics +
2 Normal relics**. What is built (`Converter/EventTemplates.cs` `ShopTemplate`, `ConversionPools`) shows 5
cards from one mixed pool and 2 relics drawn from *every* non-boss relic — so **event relics can appear in a
shop**, which §4.3 forbids outright.

- split the card shelf into its two slot groups, each drawing from its own pool under the act's gates
  (`FinalCards.RewardPool(act)` already gates; the general/character split needs the pools separated);
- split the relic shelf: shop-exclusive slots from `ShopRelics`, normal slots from `NormalRelics`, and
  neither from event or boss relics;
- keep prices where they are — §4.5 declares them balance variables, not content;
- test the shape: every shop offers 3+4 and 2+2, and no shelf ever holds an event or boss relic.

**Done when:** the shelf matches the master and a test pins it.

> **Done — 2026-08-29.** The shelf is 3 + 4 and 2 + 2, `Tests/ShopShelfTests` pins both the counts (by drawing
> the shelf) and the pools (against the authored offers). Three faults, in rising order of size:
>
> 1. the card shelf had no pools at all, so a General/Character split could not be made — `FinalCards` now
>    composes `GeneralPool`/`CharacterPool` from the sheets the cards were written on;
> 2. **Event relics were on sale** (§2.5), because the filter was `Rarity != "boss"` alone;
> 3. ★ the shelf drew from the **ported** relics, so **72 of the 74 authored Normal and Shop relics had never
>    been obtainable in a run**. Only `archive_key` and `emergency_inkwell` share an id with the ported data.
>
> Opening the Shop pool then exposed **nine programs across eight Shop relics** that crash when they fire: a
> plain effect handed to a trigger is queued and drains after the event is gone, so a `Computed…RunEffect`
> reading the event throws. All nine are effect TEMPLATES now; Bounty Hook, whose question is a branch rather
> than an amount, is two triggers. Engine: `RunEffectTemplates.ChangeCounter` (the counter twin of
> `GainResource`, simply missing) plus a test that pins the trap. Content: `Tests/QueuedEffectTests` walks
> every relic and installed program in the document and proves itself on the shape that was wrong.
>
> **Still open, and not this step's:** Treasure and the combat/elite relic rewards still draw the *ported*
> pool (`ConversionPools.RelicGrantSource`), so §3.3 — "1 random eligible **Normal** Relic" — is not kept
> anywhere but the shop.

---

## Step 5 · The Brass Bookmark says what it does

The relic's text promises one kept card ("the first card that enters your hand outside the normal draw each
turn is kept until your next turn"); the rule attaches `RetainHandTag` and keeps the **whole hand** for the
first turn (`NormalRelics.cs` and `RelicRules.BrassBookmark`). The old excuse — retention is a property of a
card definition — expired with the `RetainedCardMark` seam in Core.

Rebuild it as designed against that seam, delete the ADAPTATION note it no longer needs, and test that exactly
the named card survives the turn and nothing else does.

**Done when:** the relic's behaviour and its text are the same sentence, and the deviation is struck from
`ADAPTATIONS.md`.

---

## Step 6 · The Godot presentation pass

The four survivors of the alpha pass's list.

- **G-3 · combatant counters are invisible.** Some boss state is a counter, not a status — the Curator's dial,
  the Warden's announced seal, the Catalogue's beat, and Act III's hand budget. The standing recommendation is
  to **promote the player-facing ones to marker statuses in content**, keeping the frontend content-agnostic.
  Decide case by case; whatever stays a counter must be provably invisible-by-design.
- **G-4 · phase bosses telegraph their Phase-I intent name** (the engine rotates one intent list, so a slot
  keeps its authored name across phases). Surface the phase marker prominently enough that the mismatch reads
  as the boss changing, not as a bug.
- **G-6 · the boss relic arrives as "The spoils"** instead of by name. It is a one-option pick; give it the
  relic's name and a beat.
- **G-5 · four bodies on screen** has still never been looked at — it is the Grand Cross-Reference, Act V's
  boss, so it can be checked with a probe scene rather than played to.

Out of scope, and stated so: **G-7, art.** Nothing here ships art; the game stays styled text panels.

**Done when:** a marathon plus an eyes-on pass through an Act-II and an Act-III boss shows no unnamed state.

---

## What this plan deliberately does NOT contain

- Acts IV and V (manifest, enemies and encounters are staged in `source-data/`, nothing is built);
- the remaining 9 event relics and the boss relics of Acts IV–V;
- balance, in any form — the design's own deferred pass;
- art.
