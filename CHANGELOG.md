# Changelog

## 1.0.0

First release.

Hover a growing plant and see where it is in its growth and what it is waiting on. Read-only:
no persistence layer, no Harmony patches, nothing written to the save.

**What it shows**

- The crop's own name — a vine reads *Grapes*, not *Grape Seeds*
- Stage position, from a walk of the growth graph
- Whether today's water has been given, for the water type that crop actually needs
- An estimated days-to-grown, shown only where an honest estimate is possible
- What the next stage is waiting on, listing only what is still outstanding
- `ready to harvest`, `picked · fruits again in 2d`, `chopped 60%` and `chopped` for stumps

**Presentation**

- Nothing is drawn until you hold a key (default Left Alt), so the farm stays uncluttered
- Drawn in the game's own nameplate banner, tinted the plum Chest Labels uses

**Notes from the build**, all documented in `research/01-growth-system.md`:

- Growth is a graph of stages gated by requirements, not a timer, so a countdown is often a
  lie. The estimate is suppressed whenever the plant waits on a season, a random roll, feeding
  or petting.
- Six of the fourteen requirement types write to the save when asked whether they are
  satisfied, and one rewrites its own persistence outright. Requirement evaluation is an
  allowlist of verified pure reads; anything else is named but never called, and shown with a
  trailing `?`.
- `GrowStageContainer.GetFinalGrowStage()` never terminates on a regrowing crop, and
  `CachedGrowStages` is not a growth chain — it contains stages growth never reaches, such as
  the stump a chopped tree becomes.

**Tried and dropped before release**

- A bottom-of-screen prompt offering the key. It worked, but the game raises an interaction
  prompt for every plant on the ground, so a forest became a stream of hints.
