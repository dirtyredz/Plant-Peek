---
id: dk-dd9ae2a8
type: task
created: 2026-10-05
status: todo
since: 2026-10-05
area:
priority: P2
rank: zw
parent:
fixes: []
blocked_by: []
relates: []
---
# Split game/ growth model from interop bridges

From docs/BACKLOG.md (Placement follow-ups (from the 2026-09-01 structure review))

- **P2 — `game/` is carrying two concerns (10 of 18 files).** It holds both the growth *model*
  (`GrowthReader`, `StageGraph`, `Requirements`, `GrowthPaths` — pure interpretation of the game's
  grow-graph) and the live interop bridges (`PlantTargeting`, `InteractionTarget`,
  `GameNameplateBridge`, `NameplateGuard`, `GameFonts`, `GamePalette`). They have different churn
  histories and different seams. Consider `game/growth/` as a sub-seam, or promoting the growth model
  to its own top-level component — it is arguably the mod's real domain. Two more interop files tip
  `game/` over the 12-file flat-bucket cap.
