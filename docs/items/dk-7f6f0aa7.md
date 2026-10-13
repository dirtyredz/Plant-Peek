---
id: dk-7f6f0aa7
type: task
created: 2026-10-12
status: todo
since: 2026-10-12
area:
priority: P2
rank: zz
parent:
fixes: []
blocked_by: []
relates: []
---
# Move Diagnostics.cs from core/ to game/


`src/core/Diagnostics.cs` traverses live game types (`GrowableView`, `GrowStage`, requirement types). The workspace mod-layout rule puts files whose primary job is reading live game types in `src/game/`, and keeps `core/` free of game types.

Found by the push-time sign-off review on 2026-10-12.

Direction: move it to `src/game/`, beside the growth model it inspects (GrowthReader/StageGraph). The namespace stays flat.
