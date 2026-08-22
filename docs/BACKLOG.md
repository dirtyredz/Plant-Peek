# BACKLOG — Plant Peek

Prioritized trough of deferred work. P0 = do next / harmful now · P1 = real debt worth scheduling ·
P2 = nice-to-have. Structural items seeded by the 2026-08-22 full review (see
[../STRUCTURE.md](../STRUCTURE.md) → Structural debt).

## P0
- _(none open)_

## P1 — structural debt worth scheduling
- [x] **P1-a — Extract `GameNameplateBridge` from `PlantHover`.** ✅ Done 2026-08-22 — anchor object,
  Show/Hide reveal-keying, and shared-bubble tint cache/restore now live in `GameNameplateBridge`;
  `PlantHover` delegates. Compile-verified. **Still needs in-game validation** (nameplate show/hide,
  reveal animation, tint restore can't be confirmed by a compile).
- [x] **P1-b — Extract `PlantTargeting` from `PlantHover`.** ✅ Done 2026-08-22 — camera resolution +
  plant raycast/interaction-target selection moved out; `PlantHover` keeps the stand-down gate and
  no-camera warning. Behaviour-preserving, compile-verified.
- [x] **P1-c — `NameplateGuard` finalizer scope.** ✅ Resolved 2026-08-22 — **decision: keep it global**
  (deliberate good-citizen design). It suppresses TypeLoad/TypeInit/MissingMember exceptions for every
  `NameplateScreen.Show`, which incidentally keeps the game's own and other mods' nameplates working when
  a broken tooltip mod's postfix throws. Codex rated the broad scope P0, but the owner chose the safety
  net over scoping to our own panel. Not a bug; documented in DECISIONS + GOTCHAS. Revisit only if the
  global suppression is ever seen to mask a real failure.
- [x] **P1-d — Centralize growth-path classification.** ✅ Done 2026-08-22 — extracted
  `GrowthPaths.IsGrowthTransition`, now used by both `StageGraph` and `Requirements`, fixing the bug
  where a chop path could outrank the real growth path (blank/wrong "waiting on"). **Still needs in-game
  validation** against trees / spreading / regrowing crops.
- [~] **P1-e — `TryPeek<T>` persistence helper. Abandoned (investigated).** The shared
  `GuidPersistenceList<T>` base does not resolve without deeper assembly spelunking than the payoff
  justifies; forcing a helper of unknown signature would be over-abstraction. The two `TryGetByGuid`
  peeks stay as-is, each with its "never FindOrCreate" comment (also captured in GOTCHAS). Revisit only
  if the base type is confirmed cheaply.

## P2 — nice-to-have
- [ ] **P2-f — Extract `PlantHoverPanel` (the fallback plate) from `PlantHover`.** The last sub-seam:
  `EnsureUi` (canvas/plate/text build), `ApplyStyle`, `FitPlateToText`, and the plate half of
  `Reposition`. Deferred behind P1-a because `Reposition` drives both the plate and the (now bridge-owned)
  nameplate anchor — decide who owns positioning. `PlantHover` is 358 lines now, so this is polish, not
  urgent.
- [x] **P2-a — Stop the model leaking presentation/diagnostics config.** ✅ Done 2026-08-22 — `PlantInfo`
  now carries both `PlantedItemName` + `ProduceName` and `PanelText.ResolveName` makes the `UseProduceName`
  choice; `WarnOnce`/`CountWaterables`/`WarnedCrops` moved into `WaterDiagnostics.WarnMissingWaterOnce`
  (self-gated on `VerboseLogging`). Landed in its own file rather than `Diagnostics` because `Diagnostics`
  already calls `GrowthReader.ReadStageCosts`, so co-locating would have made model↔diagnostics a cycle
  (caught by the componentization review). `GrowthReader` now reads no presentation/diagnostics config.
  Compile-verified.
- [x] **P2-b — `StageGraph.Measure` returns a `StageMeasurement`.** ✅ Done 2026-08-22 — `Measure` now
  returns a `StageMeasurement` (StageNumber/StageCount/IsFullyGrown) and `GrowthReader.Read` copies it
  onto `PlantInfo`, removing the wrong-direction dependency of the graph utility on one consumer's
  aggregate. Compile-verified. (Promoting `PlantInfo` to its own file was *not* done here and is still
  deferred — folded into P2-c, which already eyes `GrowthReader` sub-file seams.)
- [ ] **P2-c — Latent sub-file seams in `GrowthReader`** (438 lines, cohesive today): a `GrowthTiming`
  (`EstimateDaysLeft` + `ReadStageCosts`, already a 2nd caller in `Diagnostics`) and a `WaterState`
  facet; also the `PlantInfo` model type could move to its own file here. Do only if the file grows past
  ~600 lines.
- [ ] **P2-d — Dedup addon/stage resolution** shared by `GrowthReader.Read` and `Diagnostics.LogPlantOnce`
  (`view.GridObjectPersistence.ItemAsset → GrowableAddon → GrowStageContainer.CachedGrowStages`) behind a
  `TryResolveAddon`/`ReadStages` helper.
- [ ] **P2-g — "log once per crop" idiom** (`HashSet<string>` keyed by item name, add-then-log) now
  appears in both `Diagnostics.LogPlantOnce` and `WaterDiagnostics.WarnMissingWaterOnce`. Two ~3-line
  copies in separate files — a shared `LogOncePerCrop(set, key, Action)` helper is tempting but borders
  on over-abstraction for two callers, and a shared home would re-couple the two diagnostics. Left as-is;
  revisit only if a third caller appears.
- [ ] **P2-e — Verify authored `GrowthTime` parameter order** — the day estimate assumes GrowthTime item
  parameters are in stage order (inferred, not proven). Use `VerboseLogging`'s growth dump against real
  crops to confirm/adjust. (Pre-existing known-unknown, see README.)

## Known unknowns (from research)
- Estimated palette values in `GamePalette`/`PanelSprite` are screenshot-derived, not asset-sampled —
  re-check if colours look off.

_Living doc — refresh with /project-docs when it drifts._
