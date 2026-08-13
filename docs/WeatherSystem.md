# Weather System

**Core file:** `Assets/Scripts/Managers/WeatherManager.cs` (singleton `MonoBehaviour`, `DontDestroyOnLoad`)

## Overview

Weather is primarily a **presentation layer** over season/storm/cold-day state rather than an independent gameplay driver. It has no probability roll of its own in the live game — season decides which weather *category* is possible on a given day, and `StormScheduler` supplies the specific storm/cold-day detail; `WeatherManager` just renders the result (particles, lighting, sound).

## Enum

```csharp
public enum WeatherType { Clear, Sunny, Rain, Snow, Storm }
```

- **Clear** — no precipitation effects; fireflies appear at night if it's Summer
- **Sunny** — sun beams + sun dust, active only in a midday time window (default 0.4–0.6 of the day cycle)
- **Rain** — rain particles, dims the sun
- **Snow** — snow particles; rate differs between ambient winter snowfall and blizzards
- **Storm** — Rain + Lightning/thunder together

## Weather Selection

Despite legacy inspector fields (`minWeatherDuration`, `maxWeatherDuration`, `SetRandomWeather()`) suggesting randomized cycling, that code path is dead. Live weather is fully deterministic, decided once per day by:

```csharp
ApplyWeatherForDay(bool isStormDay, bool isWinter, int coldLevel)
```

Called from `Initialize()` (on scene load) and `OnWeatherNewDay()` (subscribed to `DayNightManager.OnNewDay`).

**Decision priority:**
1. `isStormDay` (from `StormScheduler.GetCurrentDayWoodMultiplier() > 1f`) → **Storm**
2. `isWinter` (from `SeasonManager.GetCurrentSeason()`) and `coldLevel > 0` → **Snow** (blizzard rate if `coldLevel > 1`, else ambient rate)
3. Summer and `coldLevel < 0` (a "hot" day) → **Sunny**
4. Otherwise → **Clear**

`coldLevel` comes from `CalendarManager.GetCurrentDayData().coldDayType` (`Chilly`/`Cold`/`Frozen`), itself scheduled by `StormScheduler` as discrete **cold spells** — contiguous cold snaps that ramp Cold → Frozen → Cold before easing back to the Chilly baseline, rather than an independent roll per day. Spell count/placement is biased toward later quarters of winter, so cold spells get more frequent as the season progresses.

Calling `SetWeather()` also sets `autoWeather = false`, since calendar-driven selection has fully taken over from the old random system.

## Transitions

- No cross-fade between weather *types* — `SetWeather()` disables all current effects, then activates the new type's. Particle systems stop gracefully (`ParticleSystem.Stop(true, StopEmitting)`) so existing rain/snow finishes falling rather than cutting off.
- Sun beams fade intensity smoothly (`Mathf.MoveTowards`, controlled by `sunBeamFadeDuration`) and drift slowly in world space, wrapping around the camera.
- Rain/Snow intensity is adjustable via `SetRainIntensity(float)` / `SetSnowIntensity(float)`, backed by `ParticleSystem.emission.rateOverTime`.

## Gameplay Effects

- **Sun dimming**: Rain/Storm multiply `DayNightManager.sunIntensity` by `stormSunIntensityMultiplier` (0.5), restored when weather clears. This is independent of (stacks with) SeasonManager's own ambient/tint lighting.
- **Lightning/thunder**: Storm weather drives `LightningController` — randomized flash sequences with configurable duration/intensity/delay, thunder audio played with a small randomized delay (0.05–0.2s) to simulate distance.
- **Fireflies**: Clear weather, nighttime, Summer only.
- **Sun beams/dust**: Sunny weather, midday time window only.
- No direct evidence of weather affecting movement speed, combat, or work rate — those effects come from the underlying season/cold-day/storm data (firewood cost, crop growth) rather than the `WeatherType` enum itself.

## Cold Spells (`StormScheduler.cs`)

**Status: implemented 2026-07-21, not yet playtested.** Replaces an earlier per-day independent-probability model; documented here as a handoff for whoever tunes/verifies it next.

Cold/Frozen days are not independent daily rolls — they're generated as discrete **cold spells**, each a contiguous run of days that ramps up in severity and eases back down:

```
Cold (rampDays) → Frozen (peakDays) → Cold (taperDays)
```

Days outside any spell default to `ColdDayType.Chilly` (baseline winter, no wood-cost effect) automatically — there's no need to schedule the "chilly" bookends explicitly, they're just whatever's left over.

**Generation** (`StormScheduler.PlaceColdSpells`, called from `GenerateColdDaySchedule` on `OnSeasonChanged` into Winter):
1. Pick a total spell count for the winter: `minColdSpellsPerWinter`–`maxColdSpellsPerWinter` (default 2–5).
2. For each spell, independently roll `rampDays` and `taperDays` (each `minColdStageDays`–`maxColdStageDays`, default 1–2) and `peakDays` (`minFrozenStageDays`–`maxFrozenStageDays`, default 1–2) — ramp and taper are rolled separately, so spells aren't necessarily symmetric (e.g. `2 Cold → 2 Frozen → 1 Cold`).
3. Assign the spell to one of winter's four quarters via weighted random pick (`q1SpellWeight`.. `q4SpellWeight`, default `1/2/3/4`) — this is what makes spells **more frequent later in winter**, replacing the old approach of raising per-day odds over time.
4. Place it at a random valid start day within that quarter, rejecting (and retrying up to 20 times) if it would overlap an existing spell within `minGapBetweenColdSpells` (default 2). A spell that can't find room is skipped with a `Debug.LogWarning` rather than failing hard.

All tunables above are `[SerializeField]` on `StormScheduler` — adjust in the inspector, no code changes needed for balance passes.

**Wood cost** (`SettlementFormulas.GetColdDayWoodMultiplier`) was already, and remains, driven directly by whatever `ColdDayType` is scheduled for the day — Chilly ×1.0, Cold ×1.5, Frozen ×2.5 — stacked multiplicatively with the season and storm multipliers in `GetWoodCost`. This is unchanged by the spell rework; it was already correct (not an arbitrary/hardcoded figure), both for live consumption (`SettlementManager.GetTodayWoodCost`) and the offline raid simulator (`SettlementSimulator`).

**Unchanged / still correct as-is:** `GetColdDayType` (per-day lookup with Chilly fallback), calendar writing (`WriteColdDayTypesToCalendar`), and save/load reconstruction (`StormScheduler.LoadSaveData`) — all three just read whatever `ColdDayType` value ended up stored per calendar day, regardless of how it was generated, so they needed no changes.

**To verify next:** play through a full winter and confirm the spell shape/pacing feels right — the stage-length ranges and quarter weights above are first-pass defaults, not tuned against actual play.

## Cutscene Integration

- `CutsceneAction.SetWeatherAction` lets cutscenes call `WeatherManager.SetManualWeather(weather, intensity)`, overriding automatic selection.
- `CutsceneManager` calls `ResumeAutoWeather()` when a cutscene with the `resumeAutoWeather` flag ends or is interrupted, handing control back to the next daily `OnWeatherNewDay` evaluation.

## Visual/Audio Implementation

- Effect prefabs live in `Assets/Prefabs/Weather Effects/`: `Rain Effect`, `SnowEffect`, `Sun Beams`, `Sun dust`, `Fireflies`, `Lightning Controller`.
- All are instantiated once and parented under a `WeatherEffects` container attached to the main camera (so effects follow camera pan), except sun beams, which drift independently in world space and are parented directly to `WeatherManager`.
- Sun beams use URP `Light2D` components with intensity animated by a per-light-offset sine wave, multiplied by fade and weather intensity.
- An inspector "Testing" section (`testWeatherType`, `testIntensity`, `ApplyTestWeather`/`StopAllWeather` buttons) allows manual weather testing independent of the calendar-driven flow.

## Public API

- `SetWeather(WeatherType, float intensity = 1f)`
- `GetCurrentWeather()`
- `EnableRain / EnableSnow / EnableLightning / EnableSunBeams / EnableSunDust / EnableFireflies(bool)`
- `SetRainIntensity(float)` / `SetSnowIntensity(float)`
- `EnableSummerDayEffects(bool)` / `EnableSummerNightEffects(bool)`
- `IsStormActive()`
- `SetManualWeather(WeatherType, float)` / `ResumeAutoWeather()` / `IsManualMode()`
- `SetWeatherIntensity(float)`
- `GetRainParticles()` / `GetSnowParticles()` — raw `ParticleSystem` access
- `ApplyWeatherForDay(bool isStormDay, bool isWinter, int coldLevel)` — the real daily decision entry point; also called by `RaidManager` on raid-return to reapply correct weather after a time-skip

**No C# event** is exposed (no `OnWeatherChanged`) — consumers poll `GetCurrentWeather()` / `IsStormActive()` instead of subscribing.

## Initialization Order / Relation to Season

`WeatherManager.Initialize()` runs **after** the full calendar stack:

```
SeasonManager → CalendarManager → StormScheduler → WeatherManager
```

It subscribes to `DayNightManager.OnDayNightChanged`, `OnDawnEveningChanged`, and `OnNewDay`. Weather is strictly season-subordinate: it has no independent decision authority, only rendering the state computed from Season + Calendar + StormScheduler each day.

## Persistence

`WeatherManager` is **not** `ISaveable` — it derives its state fresh from Season/Calendar/Storm data via `ApplyWeatherForDay()` on scene load, so nothing weather-specific needs to be persisted.
