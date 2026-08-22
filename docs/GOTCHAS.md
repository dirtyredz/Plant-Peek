# GOTCHAS — Plant Peek

Non-obvious traps. Each: **trap → why → do instead.** Deep audit in
[../research/01-growth-system.md](../research/01-growth-system.md).

## Save safety

- **Calling a requirement's `IsRequirementCompleted` to see if it's met** → half these game methods have
  side effects: six call `GuidPersistenceList.FindOrCreate` (**inserts a save record**),
  `MusicPlayedStageRequirement` rewrites its own persistence, `RandomChanceGrowStageRequirement` consumes
  Unity's global RNG. At 12 Hz that corrupts saves and breaks determinism. → **Allowlist only verified
  pure checks** (`Requirements.CanEvaluateSafely`); show everything else as `?`. New types default to
  unevaluated.
- **`GuidPersistenceList.FindOrCreate` for a peek** → it writes a record for anything missing. → Always
  `TryGetByGuid` (see `GrowthReader.ReadChoppedPercent`, `ReadHarvestState`). This invariant is currently
  asserted by prose comments at two sites — a `TryPeek<T>` helper is backlogged (P1-e).

## Unity / game quirks

- **`Camera.main` is null** → the gameplay camera isn't tagged `MainCamera` (Cinemachine). → Fall back to
  the highest-depth active screen camera (`PlantTargeting.ResolveCamera`).
- **Gating the hover on an empty UI show-stack** → `EnergyScreen`/`ManaScreen` sit in it permanently, so
  it's never empty. → Gate on `PlayerCursorInteractionScreen.IsShowing` (present iff the player can point
  at the world).
- **Plain raycast misses interaction colliders** → many are triggers. → `Physics.RaycastAll` with
  `QueryTriggerInteraction.Collide`, take nearest `GrowableView`.
- **`BepInEx KeyboardShortcut.IsPressed` returns false while any other key is held** → it blocks modifier
  combos, so the bind never fires while walking (holding W). → Use `Hotkey.IsHeld/WasPressed`.
- **`CachedGrowStages` is not a growth chain** → it's `GetComponentsInChildren<GrowStage>()`, so it
  includes the chop-down stump; counting entries reported "stage 5 of 6" on mature trees. → BFS from the
  first stage over growth paths only, with a `visited` set (also terminates regrow loops, unlike the
  game's `GetFinalGrowStage`).
- **`TimesHarvested` looks like a lifetime tally** → `Regrow()` zeroes it each cycle, so it's only ever
  0/1. → Treat non-zero as "picked, waiting to regrow", not a count.
- **A picked/regrowing vine still sits at its final stage** → the stage graph alone says "fully grown"
  while the vine is bare. → Read `PlantHarvestableView` + regrow timing to say "picked · fruits again".
- **`PlantHarvestInteractable` vs `PlantHarvestableView`** → the interactable also requires the player to
  stand at the right height, so a ready crop read as unready from a few tiles away. → Read the *View*.

## Config

- **Changing a default does nothing on an existing install** → BepInEx keeps existing `.cfg` values, so a
  new default only reaches a fresh file. Bit this mod once (`Detail` default). → Edit the live `.cfg` or
  delete it and relaunch when changing a default.
- **Config descriptions longer than one line overflow Mod Menu** → keep them one short line; put rationale
  in the README.
- **Renaming a `.cfg` section/key orphans saved values** → section keys are stable; only display names
  (via `ConfigDescription` tags) change.

## Look-and-feel

- **`GameFonts.cs` / `GamePalette.cs` / `PanelSprite.cs` are vendored VERBATIM from ChestLabels** → fix
  bugs in *both* copies; do not let them diverge.
- **No green in the game's palette** → met/unmet is carried by weight (gold = "needs you"), not hue.
- **Checkmark glyphs may render as tofu** → the game font has gaps (no pencil glyph either). → `✓/✗` are
  opt-in (`UseCheckmarks`, off).
- **`ExpandTrigger = Click` does not intercept the click** → the game still harvests/scythes the plant. →
  `Hold` is the default for that reason.

## Structure

- **`NameplateGuard` finalizer is global** → it runs for every `NameplateScreen.Show` and can mask a
  game/other-mod exception, not only Plant Peek's. Deliberate but broad; scoping to our own anchor is
  backlogged (P1-c) and needs in-game validation.

_Living doc — refresh with /project-docs when it drifts._
