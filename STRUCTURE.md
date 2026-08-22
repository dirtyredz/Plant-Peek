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
PlantHover (MonoBehaviour: poll → resolve → read → format → draw; projects the screen point)
  ├─ PlantTargeting ────── resolve camera + plant (interaction target, else raycast)
  ├─ GrowthReader ──────── GrowableView → PlantInfo (read-only model)
  │     ├─ StageGraph ──── BFS over grow paths → StageMeasurement (stage N of M, fully-grown)
  │     └─ Requirements ── stage exit conditions → met/unmet/unknown lines
  ├─ PanelText ─────────── PlantInfo + DetailLevel → TMP-markup string
  ├─ PlantHoverPanel ───── the mod's own fallback plate (canvas/style/fit/position)
  │     └─ GameFonts/GamePalette/PanelSprite ── vendored look-and-feel (see below)
  └─ GameNameplateBridge ─ draw in the game's own nameplate banner (anchor under the panel canvas)

Plugin ── BepInEx entry + config surface;   NameplateGuard ── standalone Harmony finalizer
Hotkey ── movement-safe key checks
```

## Components

| File | Responsibility | Exposes | Depends on | Seam (where change lands) |
|---|---|---|---|---|
| [src/Plugin.cs](src/Plugin.cs) (197) | Composition root: BepInEx entry, ~20 `ConfigEntry` bindings, startup log, wires `PlantHover` + patches `NameplateGuard` | `PluginGuid/Name/Version`, `DetailLevel`/`ExpandMode` enums, all config statics | BepInEx, HarmonyX | add a config knob; change startup wiring |
| [src/PlantHover.cs](src/PlantHover.cs) (261) | `MonoBehaviour` hover orchestration: poll loop, expand-input, stand-down gate, world→screen projection. Delegates targeting, the fallback plate, and the nameplate | `PlantHover` component | PlantTargeting, PlantHoverPanel, GameNameplateBridge, GrowthReader, PanelText, Hotkey | add a poll/expand rule; change the show/hide decision |
| [src/PlantHoverPanel.cs](src/PlantHoverPanel.cs) (182) | The mod's own fallback plate: builds the screen-space canvas + 9-sliced plate + text, styles/sizes it, positions it. Unaware of *when* to show (poll loop) or the nameplate (its canvas hosts the bridge's anchor, wired by the orchestrator) | `EnsureBuilt`, `Activate`, `Deactivate`, `ShowPlate`, `HidePlate`, `PositionAt`, `IsActive` | GameFonts, GamePalette, PanelSprite, config | change the plate's build/style/fit |
| [src/PlantTargeting.cs](src/PlantTargeting.cs) (139) | Resolve the camera (`Camera.main` null here) and the plant under cursor (interaction target, raycast fallback) | `ResolveCamera`, `ResolvePlant` | InteractionTarget, Diagnostics, game physics | change how the hovered plant is chosen |
| [src/GameNameplateBridge.cs](src/GameNameplateBridge.cs) (169) | Draw the panel in the game's nameplate banner: anchor object, Show/Hide reveal-keying, shared-bubble tint cache/restore | `Available`, `Show`, `Hide`, `Reposition`, `EnsureAnchor` | game `NameplateScreen`, config | nameplate behaviour/tint |
| [src/PanelText.cs](src/PanelText.cs) (234) | Pure formatting: `PlantInfo` + `DetailLevel` → TMP string; owns the met/unmet colour palette and the produce-vs-seed name choice | `PanelText.Format` | GrowthReader.PlantInfo, Requirements.State, GamePalette, config | change wording, colours, glyphs, detail layout |
| [src/GrowthReader.cs](src/GrowthReader.cs) (438) | Read-only model: `GrowableView` → `PlantInfo` (both names, stage, harvest, water, chopped %, day estimate). No writes, no side-effecting game calls, **no presentation/diagnostics config** (reports both names; `PanelText` picks; water-warn delegated to `WaterDiagnostics`) | `GrowthReader.Read`, `PlantInfo`, `ReadStageCosts` | StageGraph, Requirements, GrowthPaths, WaterDiagnostics, game persistence | add a readable fact about a plant |
| [src/Requirements.cs](src/Requirements.cs) (265) | Stage exit conditions → readable met/unmet/unknown lines, behind a **side-effect allowlist**; picks the best grow path | `Requirements.Read`, `State`, `Entry` | GrowthPaths, game requirement types | support a new requirement type (add to allowlist only after reading its check) |
| [src/StageGraph.cs](src/StageGraph.cs) (115) | BFS over grow paths (growth only) → `StageMeasurement` (stage number, count, fully-grown), returned not mutated | `StageGraph.Measure`, `StageMeasurement`, `HasGrowthPath` | GrowthPaths, game GrowStage/GrowPath | change how stage position is computed |
| [src/GrowthPaths.cs](src/GrowthPaths.cs) (35) | Single definition of "is this grow path the plant *growing*?" (excludes chop/damage paths) | `GrowthPaths.IsGrowthTransition` | game GrowPath/GrowStage | shared by StageGraph + Requirements so they can't diverge |
| [src/InteractionTarget.cs](src/InteractionTarget.cs) (91) | Reads the game's own cursor-interaction target via reflection | `InteractionTarget.FindPlant` | HarmonyX AccessTools, game UI | game renames the private field |
| [src/Hotkey.cs](src/Hotkey.cs) (58) | Key checks that survive a held movement key (not `KeyboardShortcut.IsPressed`) | `Hotkey.IsHeld`, `WasPressed` | BepInEx, UnityEngine.Input | change hold/toggle semantics |
| [src/NameplateGuard.cs](src/NameplateGuard.cs) (48) | Harmony finalizer: swallows another mod's broken `NameplateScreen.Show` postfix so our label survives | patch class | HarmonyX | broaden/narrow the suppression (see debt P1) |
| [src/Diagnostics.cs](src/Diagnostics.cs) (108) | `VerboseLogging`-gated one-off per-crop growth dump (settles the GrowthTime-order assumption) | `Diagnostics.LogPlantOnce` | GrowthReader.ReadStageCosts | change the diagnostic dump |
| [src/WaterDiagnostics.cs](src/WaterDiagnostics.cs) (53) | `VerboseLogging`-gated once-per-crop "why no watered line" warning; self-gated so the read-only model can call it config-free. Split from `Diagnostics` by dependency direction (this is called *by* GrowthReader; Diagnostics calls *into* it) so neither forms a cycle | `WaterDiagnostics.WarnMissingWaterOnce` | game views (ViewsCollection), `PlantPeekPlugin` config/logger | change the water warning |
| [src/GameFonts.cs](src/GameFonts.cs) · [GamePalette.cs](src/GamePalette.cs) · [PanelSprite.cs](src/PanelSprite.cs) | Shared look-and-feel (game font, palette, 9-sliced plate) | statics | UnityEngine, TMPro | **vendored VERBATIM from ChestLabels — fix bugs in both copies, do not diverge** |

Dependency direction is clean: `PlantHover → {PlantTargeting, PlantHoverPanel, GameNameplateBridge,
GrowthReader, PanelText, Hotkey}`; `PlantHoverPanel → {GameFonts, GamePalette, PanelSprite}`;
`PlantTargeting → {InteractionTarget, Diagnostics}`; `GrowthReader → {StageGraph,
Requirements, WaterDiagnostics}`; `StageGraph, Requirements → GrowthPaths`; `Diagnostics →
GrowthReader.ReadStageCosts` (one-way — the growth dump reuses the model's cost reader; the water
warning is split into `WaterDiagnostics` precisely so this back-edge stays acyclic). No cycles.

## Key flows

- **Hover → panel** (`PlantHover.Poll`): stand-down gate (`PlayerCursorInteractionScreen.IsShowing`)
  → resolve camera (`Camera.main` is null here) → resolve plant (interaction target, else raycast) →
  `GrowthReader.Read` → `PanelText.Format` → `PlantHover` projects the plant's world point to a screen
  point and hands it to both drawers: the game nameplate (`GameNameplateBridge` → `NameplateScreen.Show`)
  or the mod's own plate (`PlantHoverPanel`). Input (click/key) is sampled every frame; the model read
  is gated to ~12 Hz.
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
- Wrong plant targeted → `PlantTargeting.ResolvePlant` / `InteractionTarget.cs`.
- A requirement shows as `?` → `Requirements.CanEvaluateSafely` (deliberate; see GOTCHAS).
- Day estimate missing → `GrowthReader.EstimateDaysLeft` (honest-suppression rules).
- Nameplate styling/tint → `GameNameplateBridge` + `GamePalette`.
- Fallback plate build/style/size → `PlantHoverPanel`; where either drawer sits on screen → `PlantHover.Reposition`.

## Structural debt

The 2026-08-22 full review (componentization + abstraction lenses + Codex cross-model) found the
below. **Fixed since:** extracted `PanelText`, `PlantTargeting`, `GameNameplateBridge`, and finally
`PlantHoverPanel` out of the `PlantHover` God-file (**795 → 261 lines**; it now reads as pure
orchestration — poll, decide, project — with the fallback plate its own component); removed dead
`InteractionTarget.IsAvailable`; corrected the README's "no Harmony patches" claim; and fixed the
growth-path divergence bug (P1-d) via the shared `GrowthPaths` classifier. Remaining items tracked in
[docs/BACKLOG.md](docs/BACKLOG.md):

- **P1 — `NameplateGuard` finalizer is global scope** (open). It runs for *every*
  `NameplateScreen.Show` and suppresses TypeLoad/TypeInit/MissingMember exceptions regardless of caller
  — can mask a game/other-mod failure. Codex rated P0; deliberate + narrow, so tracked P1. Fix (scope to
  our own call, now that `GameNameplateBridge` owns the `Show`) is a **product decision** — see `BACKLOG`
  P1-c — and needs in-game validation with/without the incompatible mod.
- **P1-e — abandoned, not worth forcing.** A `TryPeek<T>` helper to centralize the two `TryGetByGuid`
  "never FindOrCreate" peeks was attempted; the shared `GuidPersistenceList<T>` base does not resolve
  without deeper assembly work, so per "verify before extracting" the two call sites stay as documented
  direct peeks. Left as a low note in BACKLOG.
- **P2 — remaining:** latent `GrowthTiming`/`WaterState` splits inside `GrowthReader`; addon/stage
  resolution duplicated with `Diagnostics`; the "log once per crop" idiom now in two diagnostics files.
  `BACKLOG` P2. **Fixed 2026-08-22:** extracted `PlantHoverPanel`, `PlantHover`'s last sub-seam (P2-f);
  `StageGraph.Measure` now returns a `StageMeasurement` instead of mutating
  `PlantInfo` (P2-b); `GrowthReader` no longer reads presentation/diagnostics config — it reports both
  names for `PanelText` to pick and delegates the water warning to `WaterDiagnostics` (P2-a), which was
  split out of `Diagnostics` so the model→diagnostics call and the diagnostics→`ReadStageCosts` call
  point opposite ways with no cycle.

_Living doc — refresh with /project-docs when it drifts._
