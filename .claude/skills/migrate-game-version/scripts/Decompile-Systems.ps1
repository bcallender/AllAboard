<#
.SYNOPSIS
    Decompile the two transport AI systems from the installed Cities: Skylines II
    Game.dll and produce All Aboard! "Patched" skeletons ready for the dwell-cap splice.

.DESCRIPTION
    This automates ONLY the parts of a game-version migration that are stable across
    updates:
      1. Locate Game.dll via the CSII_* environment variables.
      2. Run ilspycmd (-lv CSharp7_3) to extract each transport system as a single type.
      3. Rewrite the file so the decomp drops into the mod project AND compiles:
           - namespace Game.Simulation         -> namespace AllAboard.System.Patched
           - class TransportCarAISystem        -> class PatchedTransportCarAISystem
             (word-boundary rename also catches the [Preserve] constructor)
           - inject `using AllAboard.System.Utility;` (the helper's namespace) plus
             `using Game;` and `using Game.Simulation;` (which restore the implicit
             same-namespace resolution -- CityStatisticsSystem, ServiceDispatch,
             SystemUpdatePhase, etc. -- that the namespace move loses)
           - drop the class-level [CompilerGenerated] attribute and declare the class
             `partial`, so the Unity SystemGenerator's generated shell merges instead
             of colliding (CS0101 + CS0579)
           - normalize leading tabs to 4 spaces (cheap fallback so staging output is
             readable even without -Apply)
      4. On -Apply only: run `dotnet format whitespace` against the just-copied files
         so they exactly match the repo's `.editorconfig`. This is what makes the
         migration diff focus on real changes instead of formatting churn.

    It deliberately does NOT touch the StopBoarding hook. That splice is the fragile,
    judgement-dependent part that changes shape every time CO refactors the system, so
    it is left to the agent following SKILL.md + references/hook-splices.md.

    Two artifacts are written per system into -OutDir:
      Unpatched<Name>.cs  - verbatim decomp (namespace/class untouched). Useful for
                            side-by-side diffing during migration. NOT copied into the
                            repo: it lives in `namespace Game.Simulation` with the
                            original class name, which would collide with Game.dll's
                            type if it landed in a compiled folder (CS0433).
      Patched<Name>.cs    - rewritten skeleton ready for the hook splice.

.PARAMETER OutDir
    Where to write the staged artifacts. Defaults to a folder under the system temp dir
    so a bare run never disturbs tracked source.

.PARAMETER Apply
    Copy the Patched*.cs skeletons into AllAboard/System/Patched/, overwriting the
    existing files. The Unpatched*.cs references stay in -OutDir (see above for why
    they are deliberately not copied into the repo).

.PARAMETER GameDll
    Override the auto-detected Game.dll path.

.PARAMETER RepoRoot
    Override the auto-detected repo root (defaults to `git rev-parse --show-toplevel`).

.EXAMPLE
    # Dry run: stage artifacts, change nothing tracked
    powershell -File ./Decompile-Systems.ps1

.EXAMPLE
    # Real migration: write skeletons into the repo
    powershell -File ./Decompile-Systems.ps1 -Apply
#>
[CmdletBinding()]
param(
    [string]$OutDir,
    [switch]$Apply,
    [string]$GameDll,
    [string]$RepoRoot
)

$ErrorActionPreference = 'Stop'

function Fail($msg) { Write-Error $msg; exit 1 }

# --- Resolve repo root -------------------------------------------------------
if (-not $RepoRoot) {
    try { $RepoRoot = (& git rev-parse --show-toplevel 2>$null) } catch {}
}
if (-not $RepoRoot -or -not (Test-Path $RepoRoot)) {
    # Fall back to walking up from the script location (skill lives under .claude/).
    $RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..\..\..')).Path
}
$RepoRoot = $RepoRoot.TrimEnd('\','/')
$patchedDir = Join-Path $RepoRoot 'AllAboard\System\Patched'
if ($Apply -and -not (Test-Path $patchedDir)) {
    Fail "Expected $patchedDir to exist. Is -RepoRoot correct? ($RepoRoot)"
}

# --- Resolve Game.dll --------------------------------------------------------
if (-not $GameDll) {
    # Guard each var: $env:VAR is $null when unset, and Join-Path $null throws a
    # terminating ParameterBindingValidationException under -ErrorActionPreference Stop,
    # which would skip the friendly Fail below. The if () skips null/empty vars.
    $candidates = @()
    if ($env:CSII_MANAGEDPATH)      { $candidates += (Join-Path $env:CSII_MANAGEDPATH 'Game.dll') }
    if ($env:CSII_INSTALLATIONPATH) { $candidates += (Join-Path $env:CSII_INSTALLATIONPATH 'Cities2_Data\Managed\Game.dll') }
    $GameDll = $candidates | Where-Object { Test-Path $_ } | Select-Object -First 1
}
if (-not $GameDll -or -not (Test-Path $GameDll)) {
    Fail "Could not locate Game.dll. Set CSII_MANAGEDPATH/CSII_INSTALLATIONPATH or pass -GameDll. Looked at: $GameDll"
}
$managedDir = Split-Path $GameDll -Parent
Write-Host "Game.dll : $GameDll"
$gameInfo = Get-Item $GameDll
Write-Host "  built  : $($gameInfo.LastWriteTime)  ($([math]::Round($gameInfo.Length/1MB,1)) MB)"

# --- Resolve ilspycmd --------------------------------------------------------
$ilspy = (Get-Command ilspycmd -ErrorAction SilentlyContinue).Source
if (-not $ilspy) {
    Fail "ilspycmd not found on PATH. Install it with:  dotnet tool install -g ilspycmd"
}

# --- Output dir --------------------------------------------------------------
if (-not $OutDir) {
    $OutDir = Join-Path $env:TEMP 'allaboard_migrate'
}
New-Item -ItemType Directory -Force -Path $OutDir | Out-Null
Write-Host "OutDir   : $OutDir`n"

# --- The two systems ---------------------------------------------------------
$systems = @(
    @{ Name = 'TransportCarAISystem';   Type = 'Game.Simulation.TransportCarAISystem'   },
    @{ Name = 'TransportTrainAISystem'; Type = 'Game.Simulation.TransportTrainAISystem' }
)

$generated = @()
foreach ($sys in $systems) {
    $name = $sys.Name
    $type = $sys.Type
    Write-Host "=== $type ==="

    # 1. Decompile. Run from the Managed dir so ilspycmd resolves sibling dependency DLLs.
    Push-Location $managedDir
    try {
        & $ilspy (Split-Path $GameDll -Leaf) -t $type -lv CSharp7_3 --disable-updatecheck -o $OutDir | Out-Null
    } finally {
        Pop-Location
    }
    $raw = Join-Path $OutDir "$type.decompiled.cs"
    if (-not (Test-Path $raw)) { Fail "ilspycmd produced no output for $type (expected $raw)" }

    $text = Get-Content -Raw -LiteralPath $raw
    Remove-Item -LiteralPath $raw -Force

    # 2. Save the verbatim decomp as the Unpatched diff reference.
    $unpatchedPath = Join-Path $OutDir "Unpatched$name.cs"
    Set-Content -LiteralPath $unpatchedPath -Value $text -Encoding UTF8 -NoNewline

    # 3. Header rewrite (the stable, mechanical transform).
    $patched = $text
    $patched = $patched -replace '(?m)^namespace Game\.Simulation\b', 'namespace AllAboard.System.Patched'
    $patched = $patched -replace "\b$name\b", "Patched$name"          # class decl + [Preserve] ctor

    # Inject the usings the relocated decomp needs:
    #   - AllAboard.System.Utility is the helper's namespace.
    #   - Game + Game.Simulation restore the implicit same-namespace resolution lost
    #     by moving out of Game.Simulation (CityStatisticsSystem, ServiceDispatch,
    #     SystemUpdatePhase, the *Request types, etc. -- ILSpy leaves them unqualified
    #     because it decompiled in the Game.Simulation context).
    $patched = $patched -replace '(?m)^namespace AllAboard\.System\.Patched',
                                 "using AllAboard.System.Utility;`r`nusing Game;`r`nusing Game.Simulation;`r`n`r`nnamespace AllAboard.System.Patched"

    # Drop the class-level [CompilerGenerated] and make the class partial. The Unity
    # SystemGenerator emits its own [CompilerGenerated] partial shell for every
    # system; if our class is non-partial it conflicts (CS0101), and if it carries
    # the same attribute the partials' attributes duplicate (CS0579). Removing the
    # attribute and adding `partial` lets the shells merge cleanly.
    $patched = $patched -replace "(?m)^[ \t]*\[CompilerGenerated\]\r?\n([ \t]*public )class (Patched$name\b)",
                                 "`${1}partial class `${2}"

    # Normalize ILSpy's leading tabs to 4 spaces. The decomp uses tabs; the repo's
    # existing C# uses 4 spaces, and a follow-up Rider reformat would rewrite every
    # line otherwise. This keeps the migration diff focused on real changes. (For
    # fully deterministic formatting, layer .editorconfig + `dotnet format` on top.)
    $patched = (($patched -split "`n") | ForEach-Object {
        $line = $_.TrimEnd("`r")
        if ($line -match '^(\t+)(.*)$') {
            (' ' * (4 * $matches[1].Length)) + $matches[2]
        } else {
            $line
        }
    }) -join "`r`n"
    $header = @"
// Auto-generated by the migrate-game-version skill from Game.dll (ILSpy, -lv CSharp7_3).
// Source type: $type
// This is a verbatim vanilla decompile; the All Aboard! dwell-cap hook must be spliced
// into StopBoarding -- see .claude/skills/migrate-game-version/references/hook-splices.md.

"@
    $patched = $header + $patched

    $patchedPath = Join-Path $OutDir "Patched$name.cs"
    Set-Content -LiteralPath $patchedPath -Value $patched -Encoding UTF8 -NoNewline

    # Locate StopBoarding to point the agent at the splice site.
    $sbLine = (Select-String -LiteralPath $patchedPath -Pattern 'bool StopBoarding\(' | Select-Object -First 1).LineNumber
    Write-Host "  -> Patched$name.cs   (StopBoarding near line $sbLine)"
    Write-Host "  -> Unpatched$name.cs (diff reference)"

    $generated += [pscustomobject]@{ Name = $name; Patched = $patchedPath; Unpatched = $unpatchedPath }
    Write-Host ""
}

# --- Apply into the repo -----------------------------------------------------
if ($Apply) {
    $appliedRelPaths = @()
    foreach ($g in $generated) {
        $dest = Join-Path $patchedDir "Patched$($g.Name).cs"
        Copy-Item -LiteralPath $g.Patched -Destination $dest -Force
        Write-Host "applied: $dest"
        $appliedRelPaths += "AllAboard\System\Patched\Patched$($g.Name).cs"
    }

    # Run `dotnet format` so the output matches the repo's .editorconfig exactly --
    # the in-script tabs->spaces normalization handles the bulk, this catches the
    # rest (spacing, blank lines, etc.) and keeps the migration diff to real changes.
    # `whitespace` mode is fast and side-effect-free; full `dotnet format` would also
    # run analyzer fix-ups which can be noisy on decompiler output.
    $solution = Join-Path $RepoRoot 'AllAboard.sln'
    if (Test-Path $solution) {
        Write-Host ""
        Write-Host "Running dotnet format whitespace..."
        Push-Location $RepoRoot
        try {
            & dotnet format whitespace $solution --include $appliedRelPaths | Out-Null
            if ($LASTEXITCODE -eq 0) {
                Write-Host "dotnet format: applied .editorconfig rules"
            } else {
                Write-Warning "dotnet format exited $LASTEXITCODE -- inspect the applied files or reformat manually."
            }
        } finally {
            Pop-Location
        }
    } else {
        Write-Warning "AllAboard.sln not found at $solution -- skipping dotnet format step."
    }

    Write-Host "`nNEXT: splice the StopBoarding hook into each Patched*.cs (see hook-splices.md), then build to verify."
    Write-Host "      The Unpatched*.cs references stay in $OutDir for any side-by-side diffing."
} else {
    Write-Host "Dry run (no repo changes). Re-run with -Apply to write into the repo."
    Write-Host "NEXT: review the staged files, splice the hook, then build to verify."
}
