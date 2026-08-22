# Plant Peek

Hover a growing plant and see where it is in its growth, and what it is waiting for.

**Status:** 🚀 **Published** — v1.0.1 live on Nexus as
[mod 120](https://www.nexusmods.com/moonlightpeaks/mods/120). See [RELEASING.md](RELEASING.md).

Build the archive with `.\pack.ps1` → `dist/PlantPeek-1.0.0.zip`.

## How to see the detail

**Hold Left Alt** while hovering a plant. Nothing is drawn at all until you do — `Detail`
defaults to `Hidden`, so the farm stays uncluttered. The startup log states the current trigger.

Rebind with `Display.ExpandKey`, or change the trigger with `Display.ExpandTrigger`
(`Hold` / `Toggle` / `Click` / `Never`).

> Name confirmed 2026-08-03. **Plant Peek** is the display name; `PlantPeek` is the assembly,
> namespace and directory, and `com.dirtyredz.moonlightpeaks.plantpeek` is the plugin GUID —
> which also names the config file, so it should not change now.

## Why this one

Confirmed gap, re-verified 2026-08-03 against the Nexus listing **sorted by date** (88 mods;
nothing published since Chest Labels) and by reading the full pages of the two mods that could
plausibly overlap:

- **Extra Tooltip** v1.1.4 (28 Jul, unchanged) — shows growth time, yield and magical needs when
  you hover a **seed in a menu**. Decompiling it previously showed all 18 Harmony patches target
  menu widgets; nothing touches world objects.
- **Farming QoL** v1.0.0 (29 Jul, unchanged) — keeps the native "needs water" icon visible above
  dry crops. That is a binary always-on indicator on one requirement, not per-plant stage state.

Nobody shows you what an actual growing plant in the world is doing.

## What it shows

The panel is layered, so a hover is quiet by default and says more on demand.

| Level | Output |
|---|---|
| `NameOnly` *(default at rest)* | `Grapes` |
| `Standard` | `Grapes`<br>`stage 2 of 4 · ~5d left · dry` |
| `Full` *(default when expanded)* | `Grapes`<br>`stage 2 of 4 · ~5d left · dry`<br>`needs water · summer`<br>`planted 6d ago · 2d at this stage` |

Only **outstanding** requirements are listed, under "waiting on" — listing satisfied ones
alongside them made a healthy plant read as though it were short of something. `ShowMetRequirements`
brings the full set back.

A requirement followed by **`?`** is one this mod deliberately does not check, because the
game's own check for it writes to your save. See the audit in
[research/01-growth-system.md](research/01-growth-system.md).

Tick/cross glyphs are available via `UseCheckmarks` but off by default — the game's font has
known gaps (it has no pencil glyph either, which is why Chest Labels draws its own), so they
may land as empty boxes until confirmed.

### Naming

The planted item is the seed, so the game's own name for a growing vine is *"Grape Seeds"*.
`UseProduceName` (default on) names the plant after `GetProduceItemAsset()` instead — what it
is going to give you — falling back to the planted item's name when a crop has no produce.

### The day estimate is deliberately shy

**Growth is not a countdown.** It is a graph of stages, each gated by requirements, so a plant
can sit unchanged indefinitely. `~5d left` is therefore suppressed whenever the current
transition waits on a season, a random roll, feeding or petting — any number would be fiction.
Watering does *not* suppress it, since that is on the player. See
[research/01-growth-system.md](research/01-growth-system.md).

The estimate assumes the `GrowthTime` item parameters are in stage order. That is inferred
rather than proven — nothing in the assembly reveals their order, since the game only ever sums
them. Turn on `VerboseLogging` and hover one of each crop: the log dumps stage counts, per-stage
costs and each path's requirements so the assumption can be checked against real crops.

### A note on `ExpandTrigger = Click`

Click mode works, but this mod does not intercept the click — the game still does whatever
left-clicking a plant normally does with the equipped tool, which can mean harvesting it or
destroying it with a scythe. `Hold` (Left Alt) is the default for that reason.

## Save safety

**Read-only.** No persistence layer, no sidecar file, and no Harmony patch that touches your
save — the mod only reads public properties and calls the game's own requirement checks. Its one
Harmony patch is `NameplateGuard`, a finalizer that suppresses another mod's broken
`NameplateScreen.Show` postfix; it writes nothing. The one exception is documented
and deliberately avoided: `RandomChanceGrowStageRequirement.IsRequirementCompleted` consumes
Unity's global RNG, so it is never called. Details in the research notes.

## Layout

```
Plant-Peek/
├── README.md
├── CHANGELOG.md               one entry per released version
├── NEXUS.md                   mod page copy + screenshot shot list
├── RELEASING.md               packaging, checklist, open decisions
├── TESTING.md                 what was verified, and what each bug turned out to be
├── pack.ps1                   builds dist/PlantPeek-<version>.zip
├── screenshots/               banner, thumbnail, and page captures
├── research/
│   └── 01-growth-system.md    decompilation findings, and the two methods not to call
└── src/
    ├── Directory.Build.props
    ├── PlantPeek.csproj       builds + auto-deploys to BepInEx/plugins/MoonlightPeaksMods
    ├── Plugin.cs              BepInEx entry point and config
    ├── GrowthReader.cs        read-only model: plant → stage, water, day estimate
    ├── Requirements.cs        stage exit conditions → readable met/unmet lines
    ├── StageGraph.cs          stage position, walking growth paths only
    ├── InteractionTarget.cs   the plant the game itself is pointing at
    ├── Hotkey.cs              key checks that survive a held movement key
    ├── Diagnostics.cs         one-off per-crop growth dump, behind VerboseLogging
    ├── NameplateGuard.cs      Harmony finalizer shielding the shared nameplate
    ├── PlantHover.cs          world hover UI, adapted from ChestLabels/HoverLabel.cs
    ├── PanelText.cs           formats a PlantInfo into the panel's TMP string
    ├── GameFonts.cs           ┐
    ├── GamePalette.cs         ├ copied verbatim from ChestLabels — fix bugs in both
    └── PanelSprite.cs         ┘
```

There is no project subdirectory under `src/`: this mod is a single project, so the extra level
named after the mod said nothing. Chest Labels keeps one because it has two projects to
separate.

## Visual integration

Per [10-visual-integration.md](https://github.com/dirtyredz/chest-labels/blob/main/10-visual-integration.md), which is explicit that
modded-looking pixels are a defect:

- **The panel is the game's own nameplate banner** (`UseGameNameplate`, default on) — the same
  one used for character names, via `NameplateScreen.Show(anchor, new CustomNameplateData(text))`.
  Font, colour, shape and reveal animation are all the game's, and stay right even if a patch
  restyles them. This is what Chest Labels ships, and a self-drawn plate is why this mod
  initially looked nothing like it.
- **Fallback plate** — set `UseGameNameplate = false` for the mod's own 9-sliced rounded plate
  (`PanelSprite`) with Gelica and the game's outline preset (`GameFonts`). Still never a flat
  rectangle, and never the stock TMP font.
- **Colour** — only `GamePalette`. The game's palette has no green, so met/unmet is carried by
  weight rather than hue: satisfied requirements fade back into the panel, outstanding ones
  are picked out in the gold the game uses for numbers that matter. Gold consistently means
  "this is the part that needs you".
- **`NameplateTint` defaults to `#4A2E8F`**, the plum Chest Labels uses, so the two mods match.
  Left untinted that banner is the **orange** the game uses for NPC names — which is why blank
  is not the default. The tint applies only while this mod's panel is showing; every image's
  original colour is cached and restored, so the game's own nameplates never inherit it.

## Tried and removed: a bottom-of-screen hint

A third interaction prompt reading *"Hold LeftAlt for plant details"* was built and worked —
cloning the screen's `textWidget` row so it inherited the plate, font and reveal animation.

**Removed anyway.** The game raises an interaction prompt for every plant on the ground, so
walking through a forest produced a constant stream of hints. Discoverability was not worth
that much chatter in a cozy game.

Worth keeping the findings, since the same ground gets retrodden:

- `BaseInteractionScreen` exposes `AddSource`/`RemoveSource`, but a screen only ever draws
  **one** source — `UpdateShowingSource` keeps `sources.LastOrDefault()`. Adding a source
  *replaces* the current prompt rather than stacking under it. Two prompts on screen at once
  are two different screens (`PlayerInteractionScreen` and `PlayerCursorInteractionScreen`),
  each drawing its own row.
- The prompt row is the private `textWidget` field, an `AnimatedWidget` holding plate, icon and
  label. `arrowWidget` is the separate world-space chevron that Chest Labels hides.
- A cloned `AnimatedWidget` is **invisible by default**: the screen animates the original, and
  the copy keeps whatever alpha and scale it was cloned at. Call `Show()` on the copy's own
  widget rather than forcing alpha.

With this gone the mod's only Harmony patch is `NameplateGuard` (a finalizer that shields the
shared nameplate from another mod's broken postfix — see `src/NameplateGuard.cs`); `HarmonyLib`
is otherwise used only for `AccessTools` reflection in `InteractionTarget`.

## Targeting

`PreferInteractionTarget` (default on) reads the game's own cursor-interaction target through
`PlayerCursorInteractionScreen`'s private `showingSource.Context`, so the panel sits on exactly
the plant the interaction arrow is on — no mismatch between the mod's reach and the game's.

Unlike Chest Labels, this **cannot** be the only source. A chest is always interactable; a crop
halfway through growing frequently has nothing to do to it, and that is precisely the plant this
mod exists to describe. So a miss falls through to the mouse raycast rather than showing nothing.

## Building

```bash
dotnet build "src/PlantPeek.csproj"
```

Deploys to `BepInEx\plugins\MoonlightPeaksMods\PlantPeek` automatically. Pass
`-p:SkipDeploy=true` to build without copying.

The `MSB3277` warnings about `System.IO.Compression` and `System.Net.Http` are benign — they
come from referencing the game's whole `Managed` folder, which ships its own framework
assemblies.

## Config

`BepInEx/config/com.dirtyredz.moonlightpeaks.plantpeek.cfg`

Descriptions in the config file are deliberately **one short line each** — Mod Menu renders
them in a settings row and overflows on anything longer. The reasoning behind each setting
lives in this README instead.

⚠️ **A changed default does nothing if you already have the file.** BepInEx keeps existing
values, so a new default only reaches a fresh install. This already bit this mod once — the
`Detail` default moved to `Hidden` and every existing install carried on at `NameOnly`, which
looked exactly like the change had not been deployed. When changing a default, edit the live
`.cfg` too, or delete it and relaunch. See
[12-versioning-and-release.md](https://github.com/dirtyredz/chest-labels/blob/main/12-versioning-and-release.md).

| Key | Default | What it does |
|---|---|---|
| `Hover.ShowHover` | `true` | Master switch for the world hover |
| `Hover.UseGameNameplate` | `true` | Draw in the game's own nameplate banner; false uses the mod's plate |
| `Hover.NameplateTint` | *(blank)* | Hex tint for the banner; blank keeps the game's colour |
| `Hover.PreferInteractionTarget` | `true` | Use the game's interaction target, falling back to a raycast |
| `Display.Detail` | `Hidden` | Detail at rest: `Hidden`, `NameOnly`, `Standard`, `Full` |
| `Display.ExpandedDetail` | `Full` | Detail once expanded; set equal to `Detail` to disable expanding |
| `Display.ExpandTrigger` | `Hold` | `Never`, `Hold`, `Toggle`, `Click` — see the caveat above |
| `Display.ExpandKey` | `LeftAlt` | Key for `Hold` and `Toggle` |
| `Display.UseProduceName` | `true` | "Grapes" rather than "Grape Seeds" |
| `Display.ShowWatered` | `true` | Show whether today's water has been given |
| `Display.ShowDaysLeft` | `true` | Show the estimate, where honest |
| `Display.UseCheckmarks` | `false` | Append ✓/✗ glyphs — may be tofu, see above |
| `Hover.HoverHeight` | `0.8` | Height above the plant, in world units |
| `Hover.HoverFontSize` | `22` | Font size of the first line; detail lines scale off it |
| `Hover.HoverBackgroundAlpha` | `0.3` | Plate opacity, 0–1; 0 is text only |
| `Diagnostics.VerboseLogging` | `false` | First raycast, plus the per-crop growth dump |
