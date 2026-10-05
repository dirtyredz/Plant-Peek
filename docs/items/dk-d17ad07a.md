---
id: dk-d17ad07a
type: task
created: 2026-10-05
status: dropped
since: 2026-10-05
area:
priority: P1
rank: z
parent:
fixes: []
blocked_by: []
relates: []
---
# TryPeek<T> persistence helper (abandoned)

From docs/BACKLOG.md (P1 — structural debt worth scheduling)

- [~] **P1-e — `TryPeek<T>` persistence helper. Abandoned (investigated).** The shared
  `GuidPersistenceList<T>` base does not resolve without deeper assembly spelunking than the payoff
  justifies; forcing a helper of unknown signature would be over-abstraction. The two `TryGetByGuid`
  peeks stay as-is, each with its "never FindOrCreate" comment (also captured in GOTCHAS). Revisit only
  if the base type is confirmed cheaply.
