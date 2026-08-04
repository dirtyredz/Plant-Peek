# Releasing Plant Peek

The shared rules live in [12-versioning-and-release.md](https://github.com/dirtyredz/chest-labels/blob/main/12-versioning-and-release.md).
This file is what is specific to this mod, and where it currently stands.

## Packaging

```powershell
.\mods\PlantPeek\pack.ps1
```

Produces `dist/PlantPeek-<version>.zip` laid out as Nexus and Vortex expect:

```
BepInEx/plugins/PlantPeek/PlantPeek.dll
```

Note that is **not** the dev deploy path. `dotnet build` deploys to
`plugins/MoonlightPeaksMods/PlantPeek/` to keep hand-built DLLs clear of Vortex; players get
the plain `plugins/PlantPeek/` layout. `pack.ps1` builds with `SkipDeploy=true`, so packaging
never overwrites the copy under test.

The script reads the version from the csproj and **refuses to pack if `Plugin.cs` disagrees** —
the two are reported to players separately, and a mismatch means an archive that lies about
what is inside it.

## No test project, on purpose

Chest Labels has a console runner because it has a JSON sidecar and a parser worth testing.
Plant Peek has neither: every code path reads Unity and game types, so a runner outside the
game could not exercise anything real. Verification is manual — see the checklist below.

If a pure-logic layer ever appears here (a formatter, a parser), it should get a runner.

## Pre-release checklist

Automated checks, verified for 1.0.0:

- [x] **Font** — `defaultFontAsset` appears only as the fallback inside `GameFonts.Apply`
- [x] **Colour** — no colour literals outside `GamePalette.cs` and `PanelSprite.cs`
- [x] **Shape** — the panel is the game's own nameplate banner; the fallback plate is 9-sliced
      and rounded, never a flat rectangle
- [x] **Versions agree** — csproj `1.0.0`, `PluginVersion` `1.0.0`
- [x] **CHANGELOG** has exactly one entry for this version
- [x] **Diagnostics off** — `VerboseLogging` defaults to `false`
- [x] **Save-safe** — no Harmony patches, no writes; the requirement allowlist exists precisely
      to keep this true (see `research/01-growth-system.md`)

Still to do by hand before publishing:

- [ ] **Fresh install** — delete `com.dirtyredz.moonlightpeaks.plantpeek.cfg`, launch, confirm
      the defaults are sensible and Mod Menu renders every description without overflowing.
      This has bitten twice: a changed default never reaches an existing config file, so a
      fresh file is the only way to see what a new player gets
- [ ] **Screenshots** — six shots per the list in [NEXUS.md](NEXUS.md), on the current build.
      Only `banner.png` and `thumbnail.png` exist so far
- [ ] **Save verified untouched** — back up a save, play a day hovering plants, confirm no diff
      beyond normal play
- [ ] Install the packed zip on a clean BepInEx and confirm it loads from
      `plugins/PlantPeek/`, not just the dev path

## Open decisions

- **Version 1.0.0.** The mod is feature-complete and the API surface is settled, so this is a
  first release rather than a preview. Chest Labels opened at 0.6.0 because it had shipped
  iterations behind it; this has not. Change it in the csproj *and* `Plugin.cs` if you would
  rather open lower — `pack.ps1` enforces that they match.
- **Localisation.** All strings are English literals in `PlantHover` and `Requirements`. The
  game exposes `LocalizationLibrary.Translate` with kebab-case keys, but there are no keys for
  this mod's wording. Worth doing if the mod finds an audience.
