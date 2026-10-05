---
id: dk-abdbd7ee
type: task
created: 2026-10-05
status: todo
since: 2026-10-05
area:
priority: P2
rank: zy
parent:
fixes: []
blocked_by: []
relates: []
---
# Tighten the three PlantInfo seam leaks

From docs/BACKLOG.md (Placement follow-ups (from the 2026-09-01 structure review))

- **P2 — tighten the three `PlantInfo` seam leaks.** `ui/PlantHover.cs` holds a raw `GrowableView`
  and reads `.transform.position` (could move into `PlantTargeting`); `core/Diagnostics.cs` and
  `core/WaterDiagnostics.cs` both take a `GrowableView`. STRUCTURE.md and README now describe these
  as exceptions rather than claiming they do not exist; fixing them would let the stronger claim
  return.
