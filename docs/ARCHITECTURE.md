# ARCHITECTURE — Plant Peek

How the system works at runtime. Code-shape map is in [../STRUCTURE.md](../STRUCTURE.md).

## System overview

Plant Peek is a single BepInEx 5 plugin loaded into Moonlight Peaks (Unity Mono, IL2CPP **not** used).
At `Awake` it binds config, adds one `PlantHover` `MonoBehaviour` to its plugin GameObject, applies the
`NameplateGuard` Harmony finalizer, and logs its font/trigger state. From then on all work is driven by
`PlantHover.Update`.

There is **no game hover system to hook** and **no Harmony patch on the gameplay path** — the mod polls
each frame and reads public game state. The only Harmony patch is the defensive `NameplateGuard`
finalizer, which writes nothing.

## The poll loop (PlantHover)

Runs every frame; two cadences:

- **Every frame:** expand-input (`ExpandMode.Click`/`Toggle`/`Hold`) is sampled so no click or key tap
  is dropped between polls; the plate is repositioned so it tracks a moving camera.
- **~12 Hz (80 ms gate):** the full `Poll()` — resolve target, read model, format, draw.

`Poll()` steps:
1. **Stand-down gate** — bail unless `PlayerCursorInteractionScreen.IsShowing`. This screen is present
   exactly when the player can point at the world, and absent in menus/cutscenes/pause. (Testing the UI
   show-stack for emptiness does *not* work — Energy/Mana screens sit in it permanently.)
2. **Resolve camera** — `Camera.main` is null in this game (Cinemachine, untagged). Fall back to the
   highest-depth active screen camera, cached.
3. **Resolve plant** — prefer the game's own cursor-interaction target (`InteractionTarget`, via
   reflection on `PlayerCursorInteractionScreen.showingSource.Context`); on a miss, fall back to
   `Physics.RaycastAll` (triggers included) and take the nearest `GrowableView`. A growing crop often
   has no interaction, so unlike Chest Labels the interaction target cannot be the only source.
4. **Detail level** — resting `Detail` vs `ExpandedDetail`, chosen by `IsExpanded()`.
5. **Read** — `GrowthReader.Read(plant)` → `PlantInfo`, or bail.
6. **Format** — `PanelText.Format(info, level)` → TMP-markup string.
7. **Draw** — game nameplate (`NameplateScreen.Show(anchor, CustomNameplateData(text))`) when enabled
   and available, else the mod's own 9-sliced plate. The nameplate is keyed by an invisible anchor
   RectTransform parked at the plant's screen position each frame.

## Data model: PlantInfo

`GrowthReader.Read` assembles one immutable-ish snapshot per poll from a `GrowableView`:

- **Identity:** `Name` (produce name if `UseProduceName`, else the planted-seed name).
- **Position in growth:** `StageNumber`/`StageCount` from `StageGraph` (BFS over *growth* paths only,
  excluding chop/spread paths; a `visited` set terminates regrow loops), `IsFullyGrown`, `IsStump`.
- **Harvest:** `ReadyToHarvest` (from `PlantHarvestableView.IsHarvestable`, not the interactable),
  `Regrows`, `DaysUntilRegrow`.
- **Water:** `WateredToday?` — the co-located `WaterableView` tile's state for the crop's required
  water type; null for wild plants/trees.
- **Chopping:** `ChoppedPercent?` — damage taken vs required, computed from public fields (never the
  save-writing requirement check).
- **Requirements:** `NextStageRequirements` — the best grow path's exit conditions as met/unmet/unknown.
- **Estimate:** `EstimatedDaysLeft?` — sum of remaining per-stage `GrowthTime` parameters minus days
  banked, **suppressed** unless every outstanding requirement is merely time (or water, which the player
  supplies). Any season/roll/feed/pet gate makes a number fiction, so it is withheld.

## External interfaces

- **BepInEx** — plugin lifecycle, `ConfigEntry` surface, logging. Config file is named by the plugin
  GUID; section keys are stable (renaming orphans saved values).
- **HarmonyX** — `AccessTools` reflection (InteractionTarget) and the one `NameplateGuard` finalizer.
- **Game assemblies** (`Moonlight Peaks_Data/Managed`, `Chicken.*` namespaces) — `GrowableView`,
  `NameplateScreen`, persistence types, grow-stage graph. All access is read-only.
- **Build** — `Directory.Build.props` references the whole `Managed` folder (hence benign MSB3277
  warnings) and generates `ModBuildInfo.Version` from the csproj `<Version>`; `pack.ps1` builds
  `dist/PlantPeek-<version>.zip` in Nexus layout. Both files are workspace-synced canonicals.

## Design notes

- **Read-only is the core invariant.** See [GOTCHAS.md](GOTCHAS.md) and
  [../research/01-growth-system.md](../research/01-growth-system.md): half the game's requirement checks
  have side effects (six call `FindOrCreate`, one rewrites persistence, one consumes global RNG), so
  evaluation is an allowlist of verified-pure checks; everything else is shown as `?`.
- **Visual integration over invented UI.** The panel is the game's own nameplate banner; colour comes
  only from `GamePalette`. A self-drawn plate is the fallback, never a flat rectangle.

_Living doc — refresh with /project-docs when it drifts._
