# bnb-content

Bureaucrats & Broomsticks, remade as RogueDeck content: a C# converter that reads the original
game's data files (`source-data/`, from Paranoidgrinch/bureaucrats-and-broomsticks-v2) and emits
`game.roguedeck.json` — a complete RunBlueprint per the RogueDeck Godot export contract.

Scope: **Act I "The Old City Offices", Bureaucrat only** — the demo run the Godot frontend is
built and tested against.

## Layout
- `Converter/` — the converter CLI (`--data source-data --out game.roguedeck.json --seed N`).
  Explicit mapping tables, fail-loud: any unmapped construct aborts with its source location.
- `Tests/` — conversion gates: export validation, JSON roundtrip, and a scripted end-to-end run
  through the engine's real host path (`RunPlayback.BuildContent`).
- `source-data/` — snapshot of the original game's `data/` directory.
- `ADAPTATIONS.md` — every place the port deviates from the original, and why.
- `ART_SLOTS.md` — **generated**: one row per picture the frontend will look for (254 cards + 210 relics),
  with the design canon's number and object line beside every relic. Regenerate it with `--art-slots`;
  a test fails if it has gone stale.
  ⚠ A relic also exports **which pool it was won from**, as `Presentation.Relics[id].Frame` — because the
  visual canon fixes one frame per pool (§10.4) and the frontend draws its relic shelf by that frame before
  a single object on it is recognised. The pool lives in the authoring record, which the frontend never
  sees, so this is the only way out; `RelicFrameTests` counts every pool across the seam.

## Building
Expects a sibling checkout of [RogueDeck-Core](../RogueDeck-Core) (relative `ProjectReference`).

```
dotnet build
dotnet run --project Converter -- --data source-data --out game.roguedeck.json --seed 20260717
dotnet test
```

## Driving it without a human

```
dotnet run --project Converter -- --playtest 8      # walk 8 whole runs and report what they met
dotnet run --project Converter -- --walk 20260721   # walk exactly ONE run — the one a report calls "seed …"
dotnet run --project Converter -- --maps 3          # lay out every act's map and check its shape
dotnet run --project Converter -- --art-slots ART_SLOTS.md   # rewrite the picture list (see above)
```

### The sparring ring — one fight, a stated loadout

Walking is right for what only a run can find and wrong for everything else: the thirty rooms in front of a
fight are noise, they cost minutes, and they are not the same thirty rooms twice. `--fight` states a fighter
and an opponent and runs that.

```
# a ten-second look: one authored enemy, the character's own deck
dotnet run --project Converter -- --fight-enemy senior_clerk --fights 10 --fight-hp 60

# one of its moves on its own — "what does THIS cost", not "what does the fight average"
dotnet run --project Converter -- --fight-enemy nanna_sin_lord_of_the_counted_moon --fight-intent crown_of_ur

# the durable form: a snapshot beside the content, which is a regression test that reads like a bug report
dotnet run --project Converter -- --fight snapshots/tombbreakers.json
```

A snapshot names an `encounter` **or** an `enemy` and everything else has a default —
`maxHealth`, `health`, `energy`, `deck`, `relics`, `consumables`, `statuses` (on the fighter),
`enemyStatuses`, `intent`, `fights`, `seed`, `turnBudget`. See `snapshots/tombbreakers.json`.

Two things to know before reading the numbers:

- **The player is a floor.** It plays greedily, which is roughly the worst competent play — a fight it wins
  comfortably is not hard, and one it loses may still be fine.
- **A boss may overwrite what you pinned.** Statuses a fight restates every turn (Nanna-Sin's whole lunar
  ring) cannot be set from a snapshot; reach a late phase by giving the fight the rounds it counts.
