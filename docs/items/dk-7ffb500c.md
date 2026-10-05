---
id: dk-7ffb500c
type: task
created: 2026-10-05
status: done
since: 2026-10-05
area:
priority: P1
rank: y
parent:
fixes: []
blocked_by: []
relates: []
---
# Centralize growth-path classification

From docs/BACKLOG.md (P1 — structural debt worth scheduling)

- [x] **P1-d — Centralize growth-path classification.** ✅ Done 2026-08-22 — extracted
  `GrowthPaths.IsGrowthTransition`, now used by both `StageGraph` and `Requirements`, fixing the bug
  where a chop path could outrank the real growth path (blank/wrong "waiting on"). **Validated in-game
  2026-08-22** against trees / spreading / regrowing crops.
