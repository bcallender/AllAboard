---
name: migrate-game-version
description: >-
  Migrate the All Aboard! mod's decompiled transport AI systems
  (PatchedTransportCarAISystem / PatchedTransportTrainAISystem) to a new Cities:
  Skylines II game version. Use this whenever the user mentions a new CS2 version
  or game patch (e.g. "1.5.9f1", "CO shipped an update", "the game updated",
  "new Paradox build"), wants to re-decompile / rebase / migrate the patched
  systems, is working on an `integration/<version>` branch, or the mod stopped
  compiling after a game update. It decompiles Game.dll with ilspycmd, rewrites
  the namespace/class, splices the configurable dwell-cap hook into StopBoarding,
  build-verifies, and bumps the version + changelog. Prefer this skill over doing
  the decompile-and-patch by hand.
---

# Migrate All Aboard! to a new game version

When Colossal Order ships a CS2 patch, the mod's two `Patched*AISystem` files —
verbatim decompiles of the stock `TransportCarAISystem` / `TransportTrainAISystem`
with one hook spliced into each — must be re-rebased onto the new game code. The
mod replaces those systems wholesale (Burst compilation rules out a lighter Harmony
patch), so a fresh decompile is unavoidable on each update.

This skill splits the job into the part that's mechanical and stable (scripted) and
the part that needs judgement every time because CO reshapes the code (you do it,
guided by `references/hook-splices.md`). The compile is the correctness gate.

## Prerequisites

- **ilspycmd** on PATH (`dotnet tool install -g ilspycmd`). This is the
  ICSharpCode.Decompiler CLI — the same engine Rider bundles. The mod's older files
  carry a dotPeek header, but dotPeek-the-standalone isn't required; ilspycmd output
  at `-lv CSharp7_3` is block-scoped and net48-compatible, which is what matters.
- The game installed and **updated to the target version** — the script reads the
  live `Game.dll` via the `CSII_INSTALLATIONPATH` / `CSII_MANAGEDPATH` env vars the
  modding toolchain sets. Confirm `Game.dll`'s timestamp matches the new patch.
- `CSII_TOOLPATH` set (the build imports `Mod.props`/`Mod.targets` from it).

## Workflow

### 1. Decompile + rewrite (scripted)

Run the helper from the repo root. Dry-run first to eyeball the output, then apply:

```powershell
# stage only — nothing tracked changes
pwsh .claude/skills/migrate-game-version/scripts/Decompile-Systems.ps1

# write into the repo: Patched skeletons -> System/Patched,
# verbatim decomps -> System/Experimental as diff references
pwsh .claude/skills/migrate-game-version/scripts/Decompile-Systems.ps1 -Apply -WriteUnpatched
```

This decompiles both systems at `-lv CSharp7_3`, then applies the stable header
rewrite: `namespace Game.Simulation` → `AllAboard.System.Patched`, class +
constructor rename to `Patched*`, and injects `using AllAboard.System.Utility;`. The
resulting `Patched*.cs` are still pure vanilla logic — **the hook is not yet spliced.**

The `Unpatched*.cs` copies in `System/Experimental/` are the diff anchors: they let
you see exactly what CO changed this version, and (after you splice) they make the
hook diff trivially reviewable.

**Commit this as the "decomp migration"** before splicing — matching the repo's
convention (`chore: migrate decomp to <version>`). That keeps the next commit (the
splice) showing only the hook, which is what a reviewer cares about.

### 2. Splice the hook (judgement)

Read `references/hook-splices.md` and apply the hook to each `Patched*.cs`. There's
one hook per file, inside `StopBoarding`. In short: excise vanilla's hardcoded
1800-frame dwell cap and route the passenger-readiness decision through
`PublicTransportBoardingHelper.ArePassengersReady(...)` so the user's slider is the
sole authority.

Do **not** pattern-match last version's exact text — CO renames locals (`flag2` vs
`flag3`) and refactors freely (1.5.9 moved the train's per-vehicle check into its own
method). Find the hook by its role (the loop/method touching `CreatureVehicleFlags.Ready`
inside `if (!forcedStop)`), preserve everything else the new decomp added, and apply
the documented transform.

### 3. Build-verify (the gate)

```powershell
dotnet build AllAboard.sln -c Release
```

A clean build is the real correctness check — it confirms the fresh decomp + your
splice line up with the new game API. Note `Mod.targets` deploys the built mod to your
local Mods folder on success, so a green build also stages it for in-game testing.

If it fails to compile, it's almost always because CO changed an API the system uses
(a renamed field, a new method parameter). The fix is *not* to hand-port logic — the
fresh decomp already contains the new vanilla code. Re-check that your splice didn't
drop or mistype something, and that any field the hook references (`m_CurrentVehicleData`,
`m_Passengers`, `m_SimulationFrameIndex`) still exists under that name in the new decomp.

### 4. Bump version + changelog

- `AllAboard/Settings/AllAboardSettings.cs` — bump `ModVersion`.
- `README.md` — add a changelog entry at the top.
- `AllAboard/Properties/PublishConfiguration.xml` — add the matching changelog entry.
  Leave `ModId` untouched (it's the live Paradox Mods id).

Ask the user for the changelog wording if it's not obvious from the diff — it's
user-facing copy, not something to invent.

### 5. Hand off

Report the build result and the version bump. In-game behavior (does boarding actually
cap at the slider value?) can only be confirmed by the user playing a save — say so
rather than claiming the migration is "done/working" off a green build alone.

## Notes

- Keep edits to `Patched*.cs` surgical. They're decompiler output; the smaller your
  footprint, the cleaner the diff against next version's fresh decomp.
- The `EnableDiagnostics` path (`BoardingDiagnosticsSystem`) and the `Experimental/`
  dead ends are unrelated to this migration — don't touch them.
- If `ilspycmd` or the game install can't be found, the script fails loudly with the
  fix; don't paper over it by guessing paths.
- This is a "trust but verify" pipeline: the script handles the boring 99%, but the
  splice and the build are where correctness is won. Don't skip the build.
