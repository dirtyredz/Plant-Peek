# BACKLOG — Plant Peek

Prioritized trough of deferred work. P0 = do next / harmful now · P1 = real debt worth scheduling ·
P2 = nice-to-have. Structural items seeded by the 2026-08-22 full review (see
[../STRUCTURE.md](../STRUCTURE.md) → Structural debt).

## P0
- _(none open)_

## P1 — structural debt worth scheduling
- [ ] **P1-a — Extract `GameNameplateBridge` from `PlantHover`.** Move `ShowGameNameplate`,
  `HideGameNameplate`, `ApplyNameplateTint`, `RestoreNameplateTint`, `UseGameNameplate`,
  `tintedOriginals`, `nameplateAnchor`/`nameplateShownFor` + the anchor block in `EnsureUi`. Give it a
  small surface (`Show/Hide/Reposition`). Moderate: shared static tint state, Unity object ownership,
  nameplate animation. Verify in-game.
- [ ] **P1-b — Extract `PlantTargeting` from `PlantHover`.** Camera resolution + plant raycast/target
  (`ResolveCamera`, `ResolvePlant`, `FindPlantUnderMouse`, `ShouldStandDown`, cache + log-once flags,
  `RaycastDistance`). Mostly code motion; low runtime risk. Pairs with a later `PlantHoverPanel` for the
  fallback plate (`EnsureUi`/`ApplyStyle`/`FitPlateToText`/`Reposition`) — defer that until after, since
  `Reposition` straddles plate + nameplate anchor.
- [ ] **P1-c — Scope the `NameplateGuard` finalizer to our own call.** It currently suppresses
  TypeLoad/TypeInit/MissingMember exceptions for *every* `NameplateScreen.Show`, regardless of caller
  (Codex rated P0). Register our anchor and suppress only when the patched call's `RectTransform` is
  ours — or drop the global patch and try/catch around our own `screen.Show`. **Needs in-game validation
  with and without an incompatible tooltip mod.**
- [ ] **P1-d — Centralize growth-path classification.** `StageGraph.IsGrowth` (excludes
  `DamageTakenRequirement`) and `Requirements.Read`'s `advances` check (any non-self target) disagree, so
  a damage/replacement path can be scored over the real growth path → empty/wrong "waiting on". Extract a
  shared `IsGrowthTransition(GrowPath, GrowStage)` used by both. **Behaviourally risky — validate against
  real crops in-game** (trees, spreading/regrowing crops). Leave the all-paths loops in Diagnostics and
  `ReadChoppedPercent` alone (different purpose).
- [ ] **P1-e — `TryPeek<T>` persistence helper in `GrowthReader`.** Replace the two
  `GamePersistence…CurrentRoom…TryGetByGuid` peeks (chopped %, harvest status), each guarded only by a
  prose "never FindOrCreate" comment, with one named helper that encodes the save-safety invariant.
  Confirm the two lists share a common generic base first.

## P2 — nice-to-have
- [ ] **P2-a — Stop the model leaking presentation/diagnostics config.** `GrowthReader.ResolveName` reads
  `UseProduceName` and `WarnOnce` reads `VerboseLogging`. Store both `PlantedItemName` + `ProduceName` on
  `PlantInfo` and let `PanelText` choose; move `WarnOnce`/`CountWaterables`/`WarnedCrops` into
  `Diagnostics`.
- [ ] **P2-b — `StageGraph.Measure` should return a `StageMeasurement`** (StageNumber/StageCount/
  IsFullyGrown) instead of mutating `GrowthReader.PlantInfo` — removes a wrong-direction dependency of a
  graph utility on one consumer's aggregate. Consider promoting `PlantInfo` to its own file.
- [ ] **P2-c — Latent sub-file seams in `GrowthReader`** (471 lines, cohesive today): a `GrowthTiming`
  (`EstimateDaysLeft` + `ReadStageCosts`, already a 2nd caller in `Diagnostics`) and a `WaterState`
  facet. Do only if the file grows past ~600 lines.
- [ ] **P2-d — Dedup addon/stage resolution** shared by `GrowthReader.Read` and `Diagnostics.LogPlantOnce`
  (`view.GridObjectPersistence.ItemAsset → GrowableAddon → GrowStageContainer.CachedGrowStages`) behind a
  `TryResolveAddon`/`ReadStages` helper.
- [ ] **P2-e — Verify authored `GrowthTime` parameter order** — the day estimate assumes GrowthTime item
  parameters are in stage order (inferred, not proven). Use `VerboseLogging`'s growth dump against real
  crops to confirm/adjust. (Pre-existing known-unknown, see README.)

## Known unknowns (from research)
- Estimated palette values in `GamePalette`/`PanelSprite` are screenshot-derived, not asset-sampled —
  re-check if colours look off.

_Living doc — refresh with /project-docs when it drifts._
