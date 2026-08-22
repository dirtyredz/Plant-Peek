# STRUCTURE — Plant Peek

Where things live in the code, and where change is expected. System design lives in
[docs/ARCHITECTURE.md](docs/ARCHITECTURE.md); why-decisions in [docs/DECISIONS.md](docs/DECISIONS.md).

`Last full review: 2026-08-22`

## Overview

A BepInEx 5 / HarmonyX plugin for the Unity Mono game **Moonlight Peaks** (netstandard2.1). Hover a
growing plant and a panel says what stage it is at and what it is waiting for. **Strictly read-only**
toward the save — see [docs/GOTCHAS.md](docs/GOTCHAS.md). One `MonoBehaviour` (`PlantHover`) polls at
~12 Hz, reads plant state through a read-only model (`GrowthReader`), and draws the game's own
nameplate banner.

Plugin `.cs` sit **flat in `src/`** (no `src/PlantPeek/` subdir — intentional; single project). Version
is single-sourced from `src/PlantPeek.csproj` `<Version>` via `GenerateModBuildInfo` in the
workspace-synced `Directory.Build.props`.

## Architecture at a glance

```
PlantHover (MonoBehaviour: poll → resolve → read → format → draw)
  ├─ InteractionTarget  ── game's own cursor target (reflection), preferred
  ├─ (raycast fallback) ── Physics.RaycastAll → GrowableView
  ├─ GrowthReader ──────── GrowableView → PlantInfo (read-only model)
  │     ├─ StageGraph ──── BFS over grow paths → stage N of M, fully-grown
  │     └─ Requirements ── stage exit conditions → met/unmet/unknown lines
  ├─ PanelText ─────────── PlantInfo + DetailLevel → TMP-markup string
  ├─ Diagnostics ───────── one-off per-crop growth dump (VerboseLogging)
  └─ GameFonts/GamePalette/PanelSprite ── vendored look-and-feel (see below)

Plugin ── BepInEx entry + config surface;   NameplateGuard ── standalone Harmony finalizer
Hotkey ── movement-safe key checks
```

## Components

| File | Responsibility | Exposes | Depends on | Seam (where change lands) |
|---|---|---|---|---|
| [src/Plugin.cs](src/Plugin.cs) (197) | Composition root: BepInEx entry, ~20 `ConfigEntry` bindings, startup log, wires `PlantHover` + patches `NameplateGuard` | `PluginGuid/Name/Version`, `DetailLevel`/`ExpandMode` enums, all config statics | BepInEx, HarmonyX | add a config knob; change startup wiring |
| [src/PlantHover.cs](src/PlantHover.cs) (595) | `MonoBehaviour` hover orchestration: poll loop, expand-input, camera resolve, plant raycast/targeting, fallback-plate UI, game-nameplate integration + tinting | `PlantHover` component | GrowthReader, PanelText, InteractionTarget, Hotkey, Diagnostics, GameFonts, GamePalette, PanelSprite | still the largest file — further seams in Structural debt below |
| [src/PanelText.cs](src/PanelText.cs) (230) | Pure formatting: `PlantInfo` + `DetailLevel` → TMP string; owns the met/unmet colour palette | `PanelText.Format` | GrowthReader.PlantInfo, Requirements.State, GamePalette, config | change wording, colours, glyphs, detail layout |
| [src/GrowthReader.cs](src/GrowthReader.cs) (471) | Read-only model: `GrowableView` → `PlantInfo` (name, stage, harvest, water, chopped %, day estimate). No writes, no side-effecting game calls | `GrowthReader.Read`, `PlantInfo`, `ReadStageCosts` | StageGraph, Requirements, game persistence | add a readable fact about a plant |
| [src/Requirements.cs](src/Requirements.cs) (261) | Stage exit conditions → readable met/unmet/unknown lines, behind a **side-effect allowlist**; picks the best grow path | `Requirements.Read`, `State`, `Entry` | game requirement types | support a new requirement type (add to allowlist only after reading its check) |
| [src/StageGraph.cs](src/StageGraph.cs) (106) | BFS over grow paths (growth only) → stage number, count, fully-grown | `StageGraph.Measure`, `HasGrowthPath` | game GrowStage/GrowPath | change how stage position is computed |
| [src/InteractionTarget.cs](src/InteractionTarget.cs) (91) | Reads the game's own cursor-interaction target via reflection | `InteractionTarget.FindPlant` | HarmonyX AccessTools, game UI | game renames the private field |
| [src/Hotkey.cs](src/Hotkey.cs) (58) | Key checks that survive a held movement key (not `KeyboardShortcut.IsPressed`) | `Hotkey.IsHeld`, `WasPressed` | BepInEx, UnityEngine.Input | change hold/toggle semantics |
| [src/NameplateGuard.cs](src/NameplateGuard.cs) (48) | Harmony finalizer: swallows another mod's broken `NameplateScreen.Show` postfix so our label survives | patch class | HarmonyX | broaden/narrow the suppression (see debt P1) |
| [src/Diagnostics.cs](src/Diagnostics.cs) (108) | One-off per-crop growth dump behind `VerboseLogging` | `Diagnostics.LogPlantOnce` | GrowthReader.ReadStageCosts | change the diagnostic dump |
| [src/GameFonts.cs](src/GameFonts.cs) · [GamePalette.cs](src/GamePalette.cs) · [PanelSprite.cs](src/PanelSprite.cs) | Shared look-and-feel (game font, palette, 9-sliced plate) | statics | UnityEngine, TMPro | **vendored VERBATIM from ChestLabels — fix bugs in both copies, do not diverge** |

Dependency direction is clean: `PlantHover → {GrowthReader, PanelText, InteractionTarget, Hotkey,
Diagnostics}`; `GrowthReader → {StageGraph, Requirements}`. No cycles; nothing reaches back up.

## Key flows

- **Hover → panel** (`PlantHover.Poll`): stand-down gate (`PlayerCursorInteractionScreen.IsShowing`)
  → resolve camera (`Camera.main` is null here) → resolve plant (interaction target, else raycast) →
  `GrowthReader.Read` → `PanelText.Format` → draw in the game nameplate (`NameplateScreen.Show`) or the
  fallback plate. Input (click/key) is sampled every frame; the model read is gated to ~12 Hz.
- **Read model** (`GrowthReader.Read`): resolve addon/stages → `StageGraph.Measure` → harvest/water/
  chopped state → `Requirements.Read` for the current stage → `EstimateDaysLeft` (suppressed unless the
  only outstanding requirement is time/water).

## Conventions

- Read-only toward the save is a hard invariant — `TryGetByGuid`, never `FindOrCreate`; requirement
  evaluation is an **allowlist** of verified pure checks.
- Colour comes only from `GamePalette`; met/unmet is carried by weight, not hue (no green in the
  game's palette). See [docs/GOTCHAS.md](docs/GOTCHAS.md).
- Config descriptions stay one short line (Mod Menu overflows); rationale lives in the README.
- `pack.ps1` + `Directory.Build.props` are **workspace-synced canonicals** — do not hand-edit here.

## Where to find things

- A displayed string is wrong → `PanelText.cs` (wording/colour) or `GrowthReader.cs` (the fact).
- Wrong plant targeted → `PlantHover.ResolvePlant` / `InteractionTarget.cs`.
- A requirement shows as `?` → `Requirements.CanEvaluateSafely` (deliberate; see GOTCHAS).
- Day estimate missing → `GrowthReader.EstimateDaysLeft` (honest-suppression rules).
- Nameplate styling/tint → `PlantHover` nameplate methods + `GamePalette`.

## Structural debt

The 2026-08-22 full review (componentization + abstraction lenses + Codex cross-model) found the
below. **Fixed in that pass:** extracted `PanelText` out of the `PlantHover` God-file (795→595 lines,
now clear of the ~800 cap); removed dead `InteractionTarget.IsAvailable`; corrected the README's "no
Harmony patches" claim. Everything else is tracked in [docs/BACKLOG.md](docs/BACKLOG.md):

- **P1 — `PlantHover` still holds 3 further responsibilities.** Camera+targeting (`PlantTargeting`),
  game-nameplate integration+tinting (`GameNameplateBridge`), and fallback-plate UI construction are
  separable. Non-trivial (instance state, Unity object ownership, nameplate animation) — schedule, do
  not rush. `BACKLOG` P1-a/b.
- **P1 — `NameplateGuard` finalizer is global scope.** It runs for *every* `NameplateScreen.Show` and
  suppresses TypeLoad/TypeInit/MissingMember exceptions regardless of caller — can mask a game/other-mod
  failure. Codex rated P0; deliberate + narrow, so tracked P1. Fix (scope to our own anchor) needs
  in-game validation with/without the incompatible mod. `BACKLOG` P1-c.
- **P1 — growth-path semantics duplicated & diverging** between `StageGraph.IsGrowth` and
  `Requirements.Read`'s `advances` check; a damage/replacement path can be scored over the real growth
  path (empty "waiting on"). Extract a shared `IsGrowthTransition`. Needs in-game validation. `BACKLOG` P1-d.
- **P1 — save-safety invariant duplicated as prose** across two `TryGetByGuid` peeks in `GrowthReader`;
  a `TryPeek<T>` helper centralizes "never FindOrCreate". `BACKLOG` P1-e.
- **P2 — model leaks presentation/diagnostics config**: `GrowthReader` reads `UseProduceName` and
  `VerboseLogging` directly. `StageGraph.Measure` mutates `PlantInfo` (wrong-direction dep) rather than
  returning a value. `GrowthTiming`/`WaterState` are latent sub-file seams if `GrowthReader` grows past
  ~600 lines. Addon/stage resolution duplicated with `Diagnostics`. `BACKLOG` P2-a…d.

_Living doc — refresh with /project-docs when it drifts._
