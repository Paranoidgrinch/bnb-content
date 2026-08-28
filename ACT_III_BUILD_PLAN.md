# Act III — The Green Docket: the build sheet

Sources, in the order they outrank each other:

| Doc | Scope |
|---|---|
| `source-data/design/…Standard_Encounter_Pools_Acts_I-IV_FINAL_AUDIT.md` §Act III | 25 identities, 40 encounters, the four universal mechanics, and the balance appendix's HP/intent bands |
| `source-data/design/…Master_Elite_Pool_Acts_I-IV_FINAL_AUDIT.md` §Act III | the elites |
| `source-data/design/…Master_Boss_Pool_Acts_I-V_FINAL_AUDIT.md` §Act III | the bosses |
| `source-data/design/bureaucrat_final_cards.md`, `general_final_cards.md` | the Act-III card pools — ALREADY BUILT (Phase B) |
| `source-data/design/BnB_Final_Relics_Master_PostAudit.md` | the relics |
| `source-data/design/BnB_Final_Events_Master_PostAudit.md` §ACT III | the fifteen events |
| `docs/bnb-act-map-specs.md` (RogueDeck-Core) | the act's map shape: Combat 8, MultiCombat 2, Elite 3, Event 3, Rest 2, Treasure 1, Shop 2, MimicChance 15 |

**Build order (the user's, set 2026-08-28):** normal enemies + encounters → elites → bosses →
cards → relics → events. The same order Act II was built in: first what you fight, then what
you fight with.

---

## What Act III IS

> **Law exists because everyone remembers what everyone else did.**

Act II's pressure was source-bound DEBT. Act III's is source-bound STANDING. Four universal
mechanics carry the whole act, and 25 identities are 40 encounters because the act recombines
known parties rather than inventing new bodies.

| Mechanic | Whose it is | Rule |
|---|---|---|
| **Trespass** | on the player, bound to a source | at 3 from one source: remove those 3, that source gains 1 **newly created** Claim. Deals no damage itself. |
| **Safe-Conduct** | on the player | spend 1 to prevent a Trespass application. Normal Act-III combats open with 1. Suggested max 3. |
| **Claim** | on an enemy | recognised standing, max 3. NOT a damage multiplier — each party reads its own Claims differently. |
| **Wergild** | owed by the player to a source | due by the end of the next player turn; **Make Amends** pays a point with 1 Energy or an eligible card. Paid in full ⇒ 1 Safe-Conduct. Unpaid ⇒ 2 direct damage per point, and the source gains 1 Claim. |

**Newly created ≠ transferred.** A Claim is newly created only by 3 Trespass, by unpaid Wergild,
or by an effect that says "create". A transfer changes owner and retriggers nothing. This is
the rule that keeps Boundary Stone / Ditch Lamprey / Bracken Moot from looping, so the two are
two different things in the content as well: `claim` is the resource, `claim_created` is the
announcement, and only a creation raises the announcement.

---

## Engine seams this arc has bought

| # | What | Where |
|---|---|---|
| 1 | **A prohibition can name the one status it refuses.** Safe-Conduct is protection against Trespass and nothing else; the broad Censure reading would have made it the best defensive status in the game. | Core `53906de` |
| 2 | **An applied status can say who it is from.** Every Local Law fires on a PLAYER action, where the acting source is the player — and the Trespass it applies is owed to the enemy whose law it is. A named source that is not there files nothing. | Core `59f0dff`, `e35c00f` |
| 4 | **A rule can settle what is owed to one named party** — `modifySelectedStatus` / `removeSelectedStatus` take the same source selector `applyStatus` grew. Wergild falls due on the PLAYER's turn, and each creditor must clear its own demand. | Core `817bf0f` |
| 5 | **A card choice can say what it will not offer.** The card being played is still in hand while its own program runs, so "discard a card" offered Make Amends itself. | Core `658aa30` |
| 3 | **The wrapping selectors can be written down** — `first` above all, which is the only sanctioned way to read ONE combatant out of a list, and therefore the only way a serialized program can say "the enemy that carries this mark". | Core `a211ada` |

Open seams the later stages will want, listed when they are first needed:

- ~~"the living enemy with the fewest / most Claims"~~ — **bought** (Core `caa029d`), and Stage 2's
  Wandering Title is its first reader.
- ~~a free encounter action~~ — **built**: a combat here has no free actions, only cards, so Make Amends is
  a card the fight puts in your hand when a demand is raised. It costs nothing, survives the turn boundary
  (`TurnEndHandDestinationZone = Hand`) and returns to hand after each use while anything is still owed.

## The act's own shapes, once Stage 3 had settled them

- **One filing point.** `ActThree.Violate(lawgiver, law, latch)` is what a Local Law calls;
  `ActThree.FileTrespass(lawgiver)` is what anything that is not a law calls (a pressure intent, a witness's
  own testimony). The pressure intents are authored programs (`ActThree.Intent`) for exactly this reason —
  their JSON entries stay as they are, because that is what the telegraph is written from.
- **A violation and the law's answer to it are two things.** A law answers one breach a turn; the breach
  itself is uncapped, and that is what the witnesses hear. It is why the Foxglove is put beside the Hedge.
- **Selectors read the whole field**, never "my allies" or "my enemies": which side a selector means depends
  on whose action woke the rule, and Act III's rules fire from both sides. `Applicant` and `Lawgiver(law)`
  are the two addresses everything uses.
- **A law is a number** (`HastyPassageLaw`, `CustomaryUseLaw`, …) written onto the player as the violation is
  filed, because the Trespass itself cannot carry which law it came from — and because the Magpie rewriting
  the source must not change what the Foxglove says it saw.

---

## Steps

- [x] **0 — the vocabulary + Stage 1.** DONE 2026-08-28 — 11 live tests in `Tests/ActThreeStageOneTests.cs`. `Converter/ActThree.cs`: Trespass, Safe-Conduct, Claim,
      the announcement, and the act's customs (the rule on the player that turns 3 Trespass into a
      Claim). Permit Hare, Mossbound Clerk, Cairn of Stray Paths + encounters 1–4.
- [x] 1 — Stage 2, the Surveyed Hedgerows (Reckoning Hedge, Errant Boundary Stone, Hawthorn Tenant)
      DONE 2026-08-28 — 11 live tests in `Tests/ActThreeStageTwoTests.cs`.
- [x] 2 — Stage 3, the Meadow of Living Testimony (Foxglove Witness, Contrary Magpie)
      DONE 2026-08-28 — 7 live tests in `Tests/ActThreeStageThreeTests.cs`. **This stage forced the act's
      architecture:** every Trespass in Act III is now filed through one place (`ActThree.Violate` for a law,
      `FileTrespass` for anything else), including the pressure INTENTS, because the Magpie decides who a
      violation is owed to BEFORE it lands and the Foxglove needs to know which law was broken.
- [x] 3 — Stage 4, the Tollwater Crossings — **Wergild + Make Amends** (Charter-Shell Snail,
      Streamside Oath-Fish, Two-Bank Toll Ford). DONE 2026-08-28 — 10 live tests in
      `Tests/ActThreeStageFourTests.cs`. A demand belongs to ONE creditor: its clock, its settlement and its
      reward are all the creditor's, kept in `Converter/ActThreeWergild.cs`.
- [x] 4 — Stage 5, the Wayside Covenants (Roadside Witchling, Blackthorn Bride, Crossroads Cup)
      DONE 2026-08-28 — 9 live tests in `Tests/ActThreeStageFiveTests.cs`. Provenance needed no engine work:
      a licence carries its giver's name because Safe-Conduct is per-grant instances, and "was any of HERS
      spent" is the difference between two readings a turn apart.
- [x] 5 — Stage 6, the Quorum Ring (Mandated Mushroom Circle, Bracken Moot) — DONE 2026-08-28, 6 live
      tests. Added the act's third announcement, `claim_consumed`: a Claim SPENT is not one moved or one
      taken away, and Stage 10 counts nothing else.
- [x] 6 — Stage 7, the Mire of Appeals (Ditch Lamprey, Sedge Bench) — DONE 2026-08-28, 6 live tests.
      **`TransferClaim` now refuses to hand over what nobody handed in**: two rules can answer the same
      grant (the Boundary Stone passing a title on and the Lamprey attaching to it are written to meet), and
      between them they would otherwise make two Claims out of one — standing created by a transfer, which
      is the one thing the act's vocabulary exists to prevent.
- [x] 7 — Stage 8, Old-Growth Precedents (Sleeping Stump Auditor, Precedent Lichen, Footfall Root) —
      DONE 2026-08-28, 7 live tests. The Lichen reuses the Foxglove's seam: a second party answering a
      violation from inside the filing.
- [x] 8 — Stage 9, Moonlit Jurisdictions (Untranslated Trail Marker, Elsewhere Path + two returning forms) —
      DONE 2026-08-28, 7 live tests. **Where the player's attention went is not a number**, so the fight
      marks whoever a card was aimed at — and each identity writes its OWN mark, because the Path asks
      whether the destination was ever reached and the Marker asks how often you changed your mind, and one
      shared mark would race.
- [x] 9 — Stage 10, the Court Beneath the Hill (Keeper of Buried Names, Handworn Tally Coin) —
      DONE 2026-08-28, 5 live tests. **THE STANDARD POOL IS COMPLETE**: 25 identities, 40 encounters,
      12 solo and 28 combination, pinned in `Tests/ActThreePoolTests.cs`.
- [x] **10 — the elites.** DONE 2026-08-28 — nine encounters, 85 live tests across nine files
      (`Tests/ActThreeElite*Tests.cs`), pinned against the master's HP table in `Tests/ActThreePoolTests.cs`.
      Files: `Converter/ActThreeElites.cs` (roster, intent dispatch, the shared settlement hook) plus one
      per elite — `Stag / Web / Bridge / Frog / Queen / Juniper / Surveyor / Reeds / Magistrate`.
      **Two engine seams bought:** an application can name the one prohibition that may not refuse it
      (Core `280ebf2`, which is the Juniper's injunction against safe passage), and a selector can name the
      body that fell where a mark still is (Core `285ac00`, which is Replace the Fallen). Every deviation
      from the master is written down in `ADAPTATIONS.md` §"Act III — the nine elites".
- [x] **11 — the bosses.** DONE 2026-08-28 — five encounters, 60 live tests across five files
      (`Tests/ActThreeBoss*Tests.cs`), pinned against the master's HP table in `Tests/ActThreePoolTests.cs`.
      Files: `Converter/ActThreeBoss{Ombudsman,Notary,Grandmother,Hill,Queen}.cs`. **No engine seam was
      needed**; what the bosses did buy is the act's **hand budget** — a turn-end rule cannot read the hand,
      because the turn's end puts it away first, and four rules already shipped were quietly wrong about it.
      Every deviation is written down in `ADAPTATIONS.md` §"Act III — the five bosses".
- [x] **(cards) — the Act-III card pools, checked rather than built.** DONE 2026-08-28. The 22 Act-III
      reward cards were authored in Phase B (`Converter/Cards/BureaucratActIII.cs` 1/5/6 and
      `GeneralActIII.cs` 0/5/5) and were already pinned by rarity and Act gate in
      `Tests/FinalCardPoolTests.cs`. What the check added is a pin on WHICH cards Act III adds, card for
      card, and a cross-read of every name against the two sheets. Nothing was missing and nothing was
      wrong. Note: the Act-III cards speak the CHARACTER's keywords (Paperwork, Doubt, Blood Ink, Ward Wax,
      Lien), not the act's four mechanics — the sheets ask for exactly that, and Safe-Conduct, Trespass,
      Claim and Wergild stay enemy-side vocabulary answered by the cards the FIGHT hands over.

- [x] **12 — the act itself.** DONE 2026-08-28 — `ActRules.For(3)` (the audit's per-path table, ceilings,
      three lanes — the old road / the hedgeways / the water meadows — depth gates, and the act's rest and
      treasure voice), `BabLoader.Acts` loads `acts/act_3_green_docket.json`, and the act's missing mimic is
      authored (`green_docket_mimic_01`, The Counted Cairn) with the manifest's chance corrected to the
      audit's 15%. `BabMapSettings` now tolerates the manifest's two dead knobs (`first_elite_depth`,
      `elite_weight_multiplier`). **Act III is in the walked run.**
- [x] **13a — the fifteen Act-III boss relics.** DONE 2026-08-28 — three per boss, in
      `Converter/Relics/BossRelics.cs` (+ `ActThreeBossRelicRules.cs` and the six action cards in
      `ActThreeBossRelicCards.cs`), 15 live tests in `Tests/ActThreeBossRelicTests.cs` and the pool pinned
      at 45 in `Tests/BossRelicTests.cs`. Deviations in `ADAPTATIONS.md` §"Act III — the fifteen boss
      relics". NOTE: the relic master calls the fourth boss "The Hill That Answers"; the encounter is named
      for the boss master's "The Answering Hill", and that name wins.

- [x] **13b — the fifteen events.** DONE 2026-08-28 — `Converter/Events/ActThreeEvents.cs` (the doors),
      `ActThreeEventPrograms.cs` (the promises), `ActThreeEventObjects.cs` (five permanent inscriptions, the
      two vows, the environmental demand, the bottled water) and `Relics/ActThreeEventRelicRules.cs` +
      `EventRelics.ActIII` (Mootcap, Dissenting Spore, Antway Marker, Complaint Leaf, Guest-Right Brooch).
      46 tests: `Tests/ActThreeEventTests.cs` (7, the shape of the set and the depth gate) and
      `Tests/ActThreeEventLiveTests.cs` (39, every door played). No engine seam was needed. Every deviation
      from the master is written down in `ADAPTATIONS.md` §"Act III — the fifteen Green Docket events".

## House rules that already cost a day each

- Every new status / card / relic needs a **description**, or `Tests/EverythingExplainsItselfTests`
  breaks the build.
- Every `CounterId` is a **property** (`static CounterId X => new("…")`), never a `static readonly`
  field — a field declared below the card that uses it is still `default` when that card
  initialises, and `default` of an id struct is a null string (`Tests/DocumentIdTests`).
- `CombatantTargetSelectors.FirstTarget` has **no serialization kind**. A list selector is fine
  wherever one combatant is wanted; the effect takes the first it resolves to.
- After each block: `dotnet run --project Converter -- --playtest 3` and `-- --maps 3`, then
  `tools/sync-content.sh` and `godot --headless -- --smoke-marathon`.

## Open findings after the act closed (2026-08-28) — the second one is FIXED, see below

- **`--playtest` cannot walk past Act II on most seeds.** The Warden of Sealed Volumes still does not end
  within 100 turns (the pre-existing Act-II finding), so a walk that draws him stops there.
  ★ **WITHDRAWN 2026-08-28 — he was never guilty.** Nothing had ever drawn him; the accusation was inference
  from a walk that stopped in Act II for another reason. He falls in 25 turns to the starting deck, and two
  walks now draw him and finish the game. What was really stopping walks was a stack overflow in the card
  Queue (`ADAPTATIONS.md` §"The boss that would not end, and the crash that was actually killing the walk").
- **A walk that DOES reach Act III stalls inside the Great Toll Frog elite** (seed 20260801, r22c2): the
  walker spins at 100% CPU for half an hour without producing the next room. The run is replayed from its
  own answers for every answer, so the cost of one more answer grows with the run — and by the third act, a
  long fight is where that finally bites. This is the same **growing replay latency** that stopped
  `--smoke-marathon`, and it is now the most expensive open item in the port: it blocks the end-to-end check
  of every act from here on. **The interlude checkpoint is the fix.**
  Everything before that stall walked correctly: the act's rooms, its doors (`the_ombudsmans_warning`,
  `moonlit_mushrooms`, …), its shop, its rest and its elites all resolve.

**★ RESOLVED the same day.** The stall was two things, neither of them a rule of the act: the replay baseline
never moved (fixed in Core — `InteractiveRunSession.Continue` now checkpoints at every interlude), and the
walker would replay `Make Amends` for ever, because that card is *designed* to keep coming back while a debt
stands. `--playtest` now walks all three acts to a Victory. Both are written up in `ADAPTATIONS.md`
§"The replay baseline, and the card a greedy player can play for ever". **The Warden of Sealed Volumes was closed
on 2026-08-28** — see the withdrawal above and `ACT_II_III_FINISH_PLAN.md` step 3.
