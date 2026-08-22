# ROADMAP — Plant Peek

Small, published mod. Status: ✅ done · 🚧 in progress · 💤 planned.

## ✅ Phase 1 — Ship (v1.0.0 → v1.0.1, published)
Read-only world hover with layered detail, game-nameplate rendering, honest day estimate, requirement
lines, harvest/regrow/chopping state. Live on Nexus as [mod 120](https://www.nexusmods.com/moonlightpeaks/mods/120).

## ✅ Phase 2 — Workspace tooling alignment (Aug 2026)
Single-sourced version from csproj, unified `pack.ps1`, shared `Directory.Build.props`; structure-review
onboarding + full baseline review (2026-08-22).

## 🚧 Phase 3 — Structural cleanup (as scheduled)
Reduce `PlantHover` further (`GameNameplateBridge`, `PlantTargeting`) and pay down the P1 debt in
[BACKLOG.md](BACKLOG.md). Each item is independently shippable; sequence behind in-game validation for
the behaviourally risky ones (P1-c, P1-d).

## 💤 Later — if warranted
- Verify `GrowthTime` parameter ordering against real crops (P2-e); adjust the estimate if needed.
- Confirm checkmark-glyph rendering in the game font; flip `UseCheckmarks` default if clean.
- Re-sample estimated palette values from shipped assets.

_Living doc — refresh with /project-docs when it drifts._
