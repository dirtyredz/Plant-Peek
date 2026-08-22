# FEATURES — Plant Peek

What the mod does. Status: ✅ shipped · 🚧 in progress · 💤 planned.

## Hover panel
- ✅ **World hover panel** — hover a plant, see its state in the world (`Hover.ShowHover`).
- ✅ **Layered detail** — `Hidden` / `NameOnly` / `Standard` / `Full`, resting vs expanded
  (`Display.Detail`, `Display.ExpandedDetail`).
- ✅ **Expand trigger** — `Hold` (default, Left Alt) / `Toggle` / `Click` / `Never`
  (`Display.ExpandTrigger`, `Display.ExpandKey`). Movement-safe key checks.
- ✅ **Game nameplate rendering** — draws in the game's own banner (`Hover.UseGameNameplate`), with a
  self-drawn 9-sliced fallback plate; optional tint (`Hover.NameplateTint`).
- ✅ **Game-target preference** — uses the game's cursor-interaction target, raycast fallback
  (`Hover.PreferInteractionTarget`).

## What it reports
- ✅ **Stage position** — "stage N of M" via a growth-only graph walk (stumps/spread excluded).
- ✅ **Day estimate** — "~Nd left", suppressed when any non-time gate would make it fiction
  (`Display.ShowDaysLeft`).
- ✅ **Watered state** — "watered"/"dry" for the crop's required water type (`Display.ShowWatered`).
- ✅ **Outstanding requirements** — "waiting on …", only unmet by default; `?` for checks that would
  touch the save (`Display.ShowMetRequirements`).
- ✅ **Harvest / regrow** — "ready to harvest", "picked · fruits again in Nd".
- ✅ **Chopping progress** — "chopped N%" for trees; "chopped" for stumps.
- ✅ **Footer** — "planted Nd ago · Nd at this stage" (Full).
- ✅ **Produce naming** — "Grapes" not "Grape Seeds" (`Display.UseProduceName`).
- ✅ **Opt-in checkmark glyphs** — ✓/✗ (`Display.UseCheckmarks`, off; font may lack glyphs).

## Safety & robustness
- ✅ **Read-only** — no writes to the save; allowlisted side-effect-free requirement checks only.
- ✅ **Self-disabling on error** — an exception in the poll disables the hover rather than spamming.
- ✅ **NameplateGuard** — shields the shared nameplate from another mod's broken `Show` postfix.
- ✅ **Verbose diagnostics** — first-raycast log + one-off per-crop growth dump (`Diagnostics.VerboseLogging`).

## Tooling
- ✅ **Single-sourced version** (csproj `<Version>` → `ModBuildInfo`), **auto-deploy build**, `pack.ps1`
  Nexus archive.

_Living doc — refresh with /project-docs when it drifts._
