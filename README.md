# bnb-content

The content of Bureaucrats & Broomsticks. A C# converter reads the game's authoring data (`source-data/`,
originally from the [Python prototype](https://github.com/Paranoidgrinch/bureaucrats-and-broomsticks-v2)) and
writes `game.roguedeck.json`, a complete RunBlueprint as defined by the RogueDeck Godot export contract.

| Repository | Content |
| --- | --- |
| [RogueDeck-Core](https://github.com/Paranoidgrinch/RogueDeck-Core) | combat and run engine, bot, replay |
| bnb-content (this repository) | cards, characters, enemies, encounters, relics, events, acts |
| [bnb-godot](https://github.com/Paranoidgrinch/bnb-godot) | Godot 4 (.NET) frontend |
| [bnb-runs](https://github.com/Paranoidgrinch/bnb-runs) | recorded playtest runs |

The current export has five acts (The Old City Offices, The Archives, The Green Docket, The Licensing
Labyrinth, The Divine Ledger) and two playable characters, the Bureaucrat and the Hedge Witch: 539 card
definitions, 228 relics, 294 encounters and 104 events. The `*_PLAN.md` files are the design and build plans
per act and topic.

## Layout

- `Converter/`: the converter CLI (`--data source-data --out game.roguedeck.json --seed N`). Mappings are
  explicit; any construct without a mapping stops the conversion with its source location.
- `Tests/`: export validation, JSON round trip, and a scripted run through the engine's real host path
  (`RunPlayback.BuildContent`).
- `source-data/`: the authoring data.
- `ADAPTATIONS.md`: every deviation from the original game, with the reason.
- `ART_SLOTS.md`: generated list of every picture the frontend looks for (229 cards, 210 relics, 269 bodies,
  708 in total), with the design canon's number and description next to each relic. Regenerate it with
  `--art-slots`; a test fails if it is out of date.

Two presentation fields are only known here and are exported for the frontend:

- `Presentation.Enemies[id].Frame`: the display name of each body (enemy, elite or boss) and its kind,
  35 boss, 66 elite, 4 mimic, 164 standard. The name is defined inside the encounters that use the body, and
  the kind depends on the strongest fight the body appears in, not on the enemy itself. A boss exported as
  standard would still work but would be drawn as an ordinary enemy. `EnemyArtTests` checks this.
- `Presentation.Relics[id].Frame`: the pool a relic comes from. The visual canon assigns one frame per pool
  (§10.4), and the pool is only stored in the authoring data. `RelicFrameTests` checks every pool.

## Building

Requires RogueDeck-Core checked out next to this repository (relative `ProjectReference`).

```bash
dotnet build
dotnet run --project Converter -- --data source-data --out game.roguedeck.json --seed 20260717
dotnet test
```

### Release builds

The engine is referenced from the sibling checkout and is not part of `BnbContent.slnx`. A solution only
assigns configurations to its own projects, so `dotnet build -c Release` on the solution builds the converter
in Release but the engine in Debug, and copies the Debug engine into `Converter/bin/Release`. On
`--playtest 1` (2026-09-18) that took 110.9 s instead of 88.1 s for the identical run. Build the project
instead:

```bash
dotnet build Converter/BnbContent.Converter.csproj -c Release
dotnet run --project Converter -c Release --no-build -- --playtest 8
```

Release only switches on `Optimize`; neither repository uses `#if DEBUG` or `Debug.Assert`.
`RogueDeck-Core/Directory.Build.props` provides the engine's non-Debug configurations; before it existed, even
the exported game shipped an unoptimized engine.

## Automated runs

```bash
dotnet run --project Converter -- --playtest 8                    # play 8 whole runs and report what they met
dotnet run --project Converter -- --walk 20260721                 # play exactly the run with this seed
dotnet run --project Converter -- --maps 3                        # generate every act's map and check its shape
dotnet run --project Converter -- --art-slots ART_SLOTS.md        # regenerate the art list
```

`--playtest` runs in parallel (`--jobs N`, default: number of cores). Each run buffers its output, and the
results are printed in seed order.

### Single fights

Whole runs are needed for problems that only a run can show, but for a single fight the rooms before it are
noise and change between runs. `--fight` runs a defined fighter against a defined opponent:

```bash
# one enemy against the character's own deck
dotnet run --project Converter -- --fight-enemy senior_clerk --fights 10 --fight-hp 60

# a single enemy move on its own
dotnet run --project Converter -- --fight-enemy nanna_sin_lord_of_the_counted_moon --fight-intent crown_of_ur

# a snapshot file next to the content, usable as a regression test
dotnet run --project Converter -- --fight snapshots/tombbreakers.json
```

A snapshot names an `encounter` or an `enemy`; everything else has defaults: `maxHealth`, `health`,
`energy`, `deck`, `relics`, `consumables`, `statuses` (on the fighter), `enemyStatuses`, `intent`, `fights`,
`seed`, `turnBudget`. See `snapshots/tombbreakers.json`.

When reading the results:

- The simulated player plays greedily, roughly the weakest competent play. A fight it wins comfortably is not
  hard; a fight it loses may still be fine.
- Bosses can overwrite pinned values. Statuses that a fight sets every turn (such as Nanna-Sin's lunar ring)
  cannot be set from a snapshot; to reach a late phase, give the fight enough rounds.

## License

[PolyForm Noncommercial 1.0.0](LICENSE). Noncommercial use with attribution is permitted; commercial use
requires permission.
