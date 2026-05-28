# All Aboard! — agent notes

Cities: Skylines II mod that puts a hard cap on transit dwell time. The README has the user-facing description; this file is the orientation an agent needs to work in the repo.

## Layout

```
AllAboard.sln                       — solution at repo root
AllAboard/                          — single C# project (net48)
  AllAboard.cs                      — IMod entrypoint (OnLoad/OnDispose)
  Settings/AllAboardSettings.cs     — sliders + locale
  System/
    Utility/
      PublicTransportBoardingHelper.cs   — the actual mod logic (small)
    Patched/
      PatchedTransportCarAISystem.cs     — vanilla decomp + manual hooks (BUS path)
      PatchedTransportTrainAISystem.cs   — vanilla decomp + manual hooks (TRAIN path)
    Experimental/
      Unpatched*AISystem.cs              — fresh vanilla decomps used for diffing on game updates
      InstantBoardingSystem.cs           — abandoned alt approach, marked "DOES NOT WORK"
  Properties/PublishConfiguration.xml    — Paradox Mods listing + changelog
```

## Build

- `net48`, builds via the CSII modding toolchain. `AllAboard.csproj` imports `Mod.props`/`Mod.targets` from `%CSII_TOOLPATH%` (user env var). Game DLLs (`Game`, `Colossal.*`, `Unity.*`) come from there with `<Private>false</Private>` — never check binaries in.
- Don't run `dotnet build` blindly without `CSII_TOOLPATH` set; the imports will fail silently into a useless build.
- Publish profiles in `Properties/PublishProfiles/` upload to Paradox Mods (mod id `86605`).

## How the mod works

The mod is small in concept and large in code only because it ships full decompiles of two Burst-compiled systems.

1. `OnLoad` disables the stock `TransportTrainAISystem` and `TransportCarAISystem`, then registers `PatchedTransportTrainAISystem`/`PatchedTransportCarAISystem` in the same `GameSimulation` phase.
2. The two `Patched*` files are verbatim JetBrains decompiles of the stock systems with one hook spliced into each: where vanilla checks "are all assigned passengers `Ready`?", the patched code calls `PublicTransportBoardingHelper.ArePassengersReady(...)` instead.
3. The helper's rule: passengers are considered "ready" once `simulationFrameIndex - publicTransport.m_DepartureFrame` exceeds `MaxAllowedMinutesLate` (in in-game minutes), regardless of actual `Ready` flags. This ends boarding and lets the `EndBoarding` path run normally — no teleporting, no flag flipping; the game's own `StopBoarding` cleans up stragglers.
4. Settings are propagated into burst jobs via `SharedStatic<uint>` (`TrainMaxAllowedMinutesLate`, `BusMaxAllowedMinutesLate`). The static initializer in `PublicTransportBoardingHelper` is load-bearing — Burst `SharedStatic` is undefined behavior without one. Do not remove it.

Conversion factor: `SimulationFramesPerMinute = 16384 / 90.0` (≈182.04). Source: cs2.paradoxwikis.com (linked in the helper).

## Game-update workflow

This repo's main maintenance task is "CO shipped a game patch, re-rebase the patched systems." The `migrate-game-version` skill (`.claude/skills/migrate-game-version/`) automates this end-to-end — invoke it when the game updates; the recipe below is the reference it follows and the manual fallback.

1. Decompile the new `Game.dll` (JetBrains dotPeek). Pull the new `TransportCarAISystem` and `TransportTrainAISystem` into `System/Experimental/Unpatched*AISystem.cs`, replacing the previous unpatched copies. Commit that as a "decomp migration" so the rebase shows up cleanly in diff.
2. Diff each `Unpatched*` against the corresponding `Patched*`. The mod's hooks are in exactly two spots per file — find the `if (!forcedStop) { ... }` block inside `StopBoarding` and replace its `ArePassengersReady` block with the helper call. Train hooks are around `PatchedTransportTrainAISystem.cs:1576-1602`; bus hooks are around `PatchedTransportCarAISystem.cs:1736-1742`.
3. Carry over any new vanilla logic (CO frequently tweaks unrelated bits — refueling, dispatch, etc.). Keep the namespace `AllAboard.System.Patched`, partial class names, and the `using AllAboard.System.Utility;` import.
4. Bump `ModVersion` in `AllAboardSettings.cs` and update the changelog at the top of `README.md` and `Properties/PublishConfiguration.xml`.

## Current state vs vanilla 1.5.5

The 1.5.5 stock systems (now in `System/Experimental/`) added their own dwell cap:

```csharp
uint num = math.max(cargoTransport.m_DepartureFrame, publicTransport.m_DepartureFrame);
bool flag = num > 0U && m_SimulationFrameIndex >= num + 1800U;
// when flag is true: skip the ArePassengersReady check and let StopBoarding finish
```

Same trigger as the mod (compare `m_SimulationFrameIndex` to `m_DepartureFrame`), same effect (skip the `Ready`-flag wait, let `EndBoarding` proceed). 1800 frames ≈ 9.89 in-game minutes — hardcoded, applied to both train and car AI identically. The mod's default is 8 minutes, configurable 0–30, with separate train/bus sliders. So vanilla is functionally the mod's approach, just slightly less aggressive and not user-tunable.

This is worth keeping in mind: the README's "Future Work" goal of avoiding wholesale system replacement is now more achievable, since the only remaining value-add over vanilla is the configurable threshold. A drastically smaller mod could in principle just tweak `m_DepartureFrame` ahead of time, or Harmony-patch the threshold constant — but Burst compilation is why this codebase took the wholesale-replacement route in the first place, and that constraint hasn't changed.

## Conventions / gotchas

- `Patched/` files are decompiler output — large, ugly, and intentionally not refactored. Keep edits surgical so future diffs against fresh decomps stay readable.
- `Experimental/` is reference material, not shipped code. The `InstantBoardingSystem` in there is a documented dead end (`DOES NOT WORK` in the file).
- `Settings/AllAboardSettings.ApplyButton` writes directly into the `SharedStatic`s — this is the only way to push slider changes into a running burst job. Don't try to wire settings change events through `OnSettingsApplied` patterns; the existing button flow is intentional.
- The mod ID and display name in `PublishConfiguration.xml` are the live ones on Paradox Mods. Don't change `ModId`.
