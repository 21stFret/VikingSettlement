# Season System

**Core file:** `Assets/Scripts/Managers/SeasonManager.cs` (singleton `MonoBehaviour`, implements `ISaveable`)

## Overview

The season system is a simple two-state cycle — `Summer` and `Winter` — driven by day count rather than real time. It is the root of a "calendar stack" of managers (`SeasonManager → CalendarManager → StormScheduler → WeatherManager`), and other gameplay systems (farming, fishing, lumber, firewood consumption, wheat growth) read its state directly to scale production and survival costs.

## Enum

```csharp
public enum Season { Summer, Winter }
```

Nested inside `SeasonManager`, so external code refers to it as `SeasonManager.Season`. There is no spring/autumn — this is an intentional "Age 1" simplification; a code comment notes season length will eventually be driven by Age progression.

## Key Fields

| Field | Default | Purpose |
|---|---|---|
| `daysPerSeason` | 30 | Length of both Summer and Winter |
| `currentSeason` | `Summer` | Current active season |
| `daysUntilSeasonChange` | counts down from `daysPerSeason` | Countdown to next flip |
| `currentSolarYear` | starts at 1 | Increments each time Summer begins (one Winter→Summer cycle = one year) |
| `summerAmbientMultiplier` / `winterAmbientMultiplier` | 1.2 / 0.8 | Ambient light scaling |
| `winterSunTint` / `summerSunTint` | — | Sun color targets for gradual lighting transition |
| `summerFarmMultiplier` / `winterFarmMultiplier` | 1.0 / 0.25 | Farm output scaling |
| `summerFishingMultiplier` / `winterFishingMultiplier` | 0.8 / 1.2 | Fishing hut output scaling |
| `summerLumberMultiplier` / `winterLumberMultiplier` | 1.0 / 0.6 | Lumber camp output scaling |
| `fireController` | — | Reference to campfire visuals, bootstrapped here via `Setup()` |

## Progression Logic

1. `SeasonManager.Initialize()` subscribes to `DayNightManager.Instance.OnNewDay`.
2. Each new day, `OnNewDay()` decrements `daysUntilSeasonChange`; at ≤0 it calls `ChangeSeason()`.
3. `ChangeSeason()` toggles `currentSeason` (a simple Summer↔Winter flip, not an ordered cycle), resets the countdown to `daysPerSeason`, re-applies lighting, increments `currentSolarYear` when entering Summer, and fires `OnSeasonChanged`.
4. Independently, `SeasonManager` also subscribes to `GameTickManager.Instance.OnFastUpdate`, which every frame `Lerp`s the sun color toward the active season's tint — so while the season *flag* flips instantly, the visual lighting transition is gradual.

## Event

```csharp
public event Action<Season> OnSeasonChanged;
```

Fires once, exactly when the season flips, passing the new season.

## Public API

- `GetCurrentSeason()`
- `GetDaysUntilSeasonChange()`
- `GetCurrentSolarYear()`
- `GetSeasonForDayOffset(int dayOffset)` — pure lookahead, used when simulating time the player skips (e.g. raids)
- `GetProductionMultiplier(BuildingType)` — returns the season-appropriate multiplier for Farm/FishermansHut/LumberCamp; 1.0 for everything else
- `GetProductionMultiplierForDayOffset(BuildingType, int dayOffset)` — combines the two above
- `AdvanceDays(int days)` — silently fast-forwards season state (used on raid return) without firing per-day `OnSeasonChanged` events

These "pure" lookahead functions exist because raids and the background settlement simulator need to project production and firewood costs across a season boundary without mutating live game state.

## Systems That Depend on Season

| System | Usage |
|---|---|
| `SeasonNotificationHook` | Shows a toast popup ("Summer has arrived...", "Winter is here...") on `OnSeasonChanged` |
| `CalendarManager` | Reads current season/countdown each day to build its rolling 30-day calendar |
| `StormScheduler` | Regenerates the winter storm schedule and cold-day schedule on `OnSeasonChanged` |
| `WeatherManager` | Reads current season to gate snow (winter) vs. sunny/fireflies (summer) |
| `Building.cs` | `GetSeasonalMultiplier()` scales Farm/Fishing/Lumber production every tick |
| `HarvestableWheat` | Wheat growth stages freeze entirely during winter |
| `SettlementManager` | Firewood consumption doubles in winter vs. summer (further modified by cold-day type and storms) |
| `SettlementSimulator` / `RaidManager` | Use the day-offset lookahead API to simulate off-screen production/costs across season boundaries |
| `TimeInfoUI` | Displays "Solar Year: N" / "Season: X" |

## Initialization Order

```
GameTickManager → DayNightManager → SeasonManager → CalendarManager → StormScheduler → WeatherManager → ...
```

`SeasonManager` must initialize before `CalendarManager` (which reads season state on its first refresh) and before `StormScheduler` (which listens for `OnSeasonChanged`).

## Persistence

`ISaveable` implementation saves `currentSeason`, `daysUntilSeasonChange`, and `currentSolarYear` into `GameStateSave` (`Assets/Scripts/Save/SaveData.cs`).
