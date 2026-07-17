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

## Building
Expects a sibling checkout of [RogueDeck-Core](../RogueDeck-Core) (relative `ProjectReference`).

```
dotnet build
dotnet run --project Converter -- --data source-data --out game.roguedeck.json --seed 20260717
dotnet test
```
