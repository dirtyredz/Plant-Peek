# DECISIONS — Plant Peek

Design decisions worth not re-litigating. Newest first. Seeded from the README, research notes, git
history, and the 2026-08-22 structural review; rationale is the authors' where recorded, inferred where
noted.

## 2026-08-22 — Decompose the `PlantHover` God-file (P1 pass) + fix the growth-path bug
**What:** Extracted `PlantTargeting` (camera + plant resolution) and `GameNameplateBridge` (game-nameplate
anchor, Show/Hide keying, tint cache) out of `PlantHover` (now **358 lines**, from 795). Fixed a real bug:
`StageGraph` and `Requirements` had diverged on what counts as a growth path, letting a chop path outrank
the real one — unified via a shared `GrowthPaths.IsGrowthTransition`.
**Why:** `PlantHover` was a God-MonoBehaviour; these are cohesive, independently-testable seams. The
growth-path split was a latent correctness bug, not just tidiness.
**Rejected:** the `TryPeek<T>` persistence helper (P1-e) — its shared base type doesn't resolve cleanly,
so forcing it would be over-abstraction; the fallback-plate UI split (`PlantHoverPanel`, P2-f) — left for
later since `Reposition` straddles plate + nameplate anchor. **Deferred as a product call:** scoping the
global `NameplateGuard` finalizer (P1-c) changes whether PlantPeek shields other mods' nameplates.
**Caveat:** the targeting/nameplate extractions and the growth-path fix are compile-verified but not yet
validated in-game.

## 2026-08-22 — Extract `PanelText` from the `PlantHover` God-file
**What:** Moved all text formatting (`Format*`, the met/unmet colour palette, `Tick`/`Cross`/`Separate`)
out of `PlantHover.cs` (795→595 lines) into a new `PanelText.cs`.
**Why:** `PlantHover` sat at the ~800-line God-class cap mixing ~5 responsibilities; formatting was the
cleanest seam — all `static`, zero `MonoBehaviour` coupling, so it's pure compile-verified code motion.
**Rejected:** splitting the further responsibilities (targeting, nameplate, plate UI) in the same pass —
those carry instance state and Unity-object-ownership risk and need in-game validation; backlogged
instead.

## 2026-08-22 — Onboarded to the structure-review workflow
**What:** Bootstrapped the living-doc set + installed the pre-push structure-review gate.
**Why:** Standard workspace practice; a published mod that will keep receiving small changes benefits
from the push-checkpoint review. **Rejected:** opting out (`.structure-review-optout`) — the mod has
real structural debt worth tracking.

## ~2026-08 — Read-only, with an allowlist for requirement evaluation
**What:** The mod never writes to the save. Requirement checks are called only for types verified to be
side-effect-free; all others are labelled but shown as `?`.
**Why:** Many game requirement checks mutate persistence (`FindOrCreate`), rewrite their own state, or
consume global RNG — unacceptable at a 12 Hz hover. New/unknown types default to unevaluated.
**Rejected:** calling the obvious-looking `IsRequirementCompleted` on everything (would break the
read-only guarantee and consume RNG). See [../research/01-growth-system.md](../research/01-growth-system.md).

## ~2026-08 — Draw in the game's own nameplate banner
**What:** Default UI is `NameplateScreen.Show` (the character-name banner), tinted plum to match Chest
Labels; a self-drawn 9-sliced plate is the fallback.
**Why:** Modded-looking pixels are a defect; the game's banner keeps font/shape/animation correct even
if a patch restyles them. **Rejected:** a self-drawn plate as default (never matched the game).

## ~2026-08 — Day estimate is deliberately shy
**What:** `~Nd left` is suppressed whenever the current transition waits on a season, random roll,
feeding or petting; watering does not suppress it.
**Why:** Growth is a gated graph, not a countdown — any number under a non-time gate is fiction.
**Rejected:** always showing a number (misleads on gated crops).

## ~2026-08 — Name after produce, not the planted seed
**What:** `UseProduceName` (default on) names a plant "Grapes" via `GetProduceItemAsset()`, not "Grape
Seeds". **Why:** the produce is what the player means when pointing at a mature vine. **Rejected:** the
raw item name (reads wrong for grown crops).

## ~2026-08 — Custom `Hotkey` checks instead of `KeyboardShortcut.IsPressed`
**What:** Hold/toggle keys use a bespoke check. **Why:** BepInEx's `KeyboardShortcut.IsPressed` returns
false while any other key is held — so the binding never fired while the player was walking (holding W).
**Rejected:** the built-in check.

## ~2026-08 — No `src/PlantPeek/` subdirectory; version single-sourced from csproj
**What:** Plugin `.cs` sit flat in `src/`; `[BepInPlugin]` version comes from `ModBuildInfo.Version`
generated from the csproj `<Version>`. **Why:** one project, so the extra level said nothing; a
hardcoded version string drifts. **Rejected:** ChestLabels-style subdir (it has two projects); a
hardcoded `PluginVersion`.

## ~2026-08 — Removed the bottom-of-screen "Hold LeftAlt" hint
**What:** A built, working interaction-prompt hint was removed. **Why:** the game raises a prompt per
plant, so a forest produced a constant stream of hints — not worth the chatter in a cozy game.
**Rejected:** keeping it. Findings preserved in the README.

_Living doc — refresh with /project-docs when it drifts._
