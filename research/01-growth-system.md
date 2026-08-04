# The Growth System

Decompiled from `Vampire.Runtime.dll` on 2026-08-03, before writing any code. Regenerate with
the commands in [09-exploring-the-assembly.md](https://github.com/dirtyredz/chest-labels/blob/main/09-exploring-the-assembly.md).

## Verdict on the five-minute feasibility check

**Weekend, not a slog.** Every value this mod needs is a public property or a public method on
the game's own types. There is no reflection anywhere in the mod and no Harmony patch is
required at all — the whole feature is a read.

## The read path

```
hit.collider.GetComponentInParent<GrowableView>()
  ├── GridObjectPersistence          public  → Position, ItemAsset
  ├── GrowablePersistence            public  → GrowStageGuid, DayPlanted,
  │                                            DayGrowStageChanged, DayProcessed,
  │                                            TimesHarvested, IsPlayerPlanted
  ├── DesiredGrowStage               public  → see caveat below
  ├── InteractionCollider            public
  └── ItemAsset                      public  → .Name (localized), .GrowableAddon
        └── GrowableItemAddon
              ├── GrowStageContainer public
              │     ├── CachedGrowStages            List<GrowStage>, public
              │     ├── GetGrowStage(guid|index)    public
              │     └── GetDesiredGrowPath(...)     public — DO NOT CALL, see below
              ├── GetTotalGrowthTime(out int stages) public
              ├── GetRegrowthTime()                  public
              ├── GrowingSeasons, RequiredWaterType  public
              └── CanBeEatenByCrow                   public
```

Current day is `GamePersistence.Instance.Time.TotalDay` — the same clock `DayPlanted` and
`DayGrowStageChanged` are recorded against, confirmed by `WaterGrowStageRequirement` comparing
them directly.

## Growth is a graph, not a timer

`GrowStage` (a MonoBehaviour) exposes `GrowPaths`. Each `GrowPath` has a `TargetGrowStage` and:

```csharp
public bool CheckIfRequirementsMet(GridObjectPersistence g, GrowablePersistence p)
{
    IGrowStageRequirement[] components = GetComponents<IGrowStageRequirement>();
    for (int i = 0; i < components.Length; i++)
        if (!components[i].IsRequirementCompleted(g, p)) return false;
    return true;
}
```

`IGrowStageRequirement.IsRequirementCompleted(...)` is **public on every requirement type**, so
each requirement can be evaluated individually and rendered as its own ✓/✗ line. This is what
makes the honest UI ("next stage needs: water ✗ · summer ✓") cheap rather than a reimplementation.

Because we call the game's own method, requirement-patching mods are inherited for free —
[Endless Harvest](https://www.nexusmods.com/moonlightpeaks/mods/112) makes season checks pass,
and our season line will correctly read ✓ when it is installed. Reimplementing the checks
ourselves would have quietly disagreed with the player's actual game.

## ⚠️ The landmine: never call CheckIfRequirementsMet

```csharp
public class RandomChanceGrowStageRequirement : BaseGrowStageRequirement
{
    public override bool IsRequirementCompleted(GridObjectPersistence g, GrowablePersistence p)
    {
        return Random.value < baseChance.GetValue(this)
             + extraChancePerDay.GetValue(this) * (float)(p.DayProcessed - p.DayGrowStageChanged);
    }
}
```

It calls `Random.value`, which **consumes Unity's global RNG**. A hover polls ~12×/second, so
calling this would churn the shared random stream continuously while the mouse rests on a plant
— a real side effect in a game whose crop quality, spreading and drops all roll against it.
It is also meaningless to display: the answer is a fresh coin flip every poll, so the ✓/✗ would
flicker.

**Therefore:** do not call `GrowPath.CheckIfRequirementsMet` (it iterates *all* requirements,
including this one) and do not call `GrowStageContainer.GetDesiredGrowPath` (same, plus a
`GetRandom()` path selection). Enumerate `GetComponents<IGrowStageRequirement>()` on the path
ourselves and type-switch: render `RandomChanceGrowStageRequirement` as "chance each day"
**without calling it**, and call `IsRequirementCompleted` on everything else.

This is exactly the kind of thing the decompile-first rule exists to catch. Nothing in the
class name suggests a read is destructive.

## ⚠️ The second landmine: never call GetFinalGrowStage

```csharp
public GrowStage GetFinalGrowStage()
{
    if (overrideFinalGrowStage != null) return overrideFinalGrowStage;
    GrowStage growStage = GetGrowStage(0);
    while (growStage != null && growStage.GrowPaths.Count() != 0)
        growStage = growStage?.GrowPaths.FirstOrDefault()?.TargetGrowStage;
    return growStage;
}
```

It walks the first path forward until it finds a stage with no paths. A **regrowing** crop's
harvest stage links back to an earlier stage, so unless `overrideFinalGrowStage` is authored
this never terminates — it hangs the calling thread, which for a hover poll is the main thread.

Ask "is this the last stage?" instead of "which is the last stage?": a stage with no
`GrowPaths` is the end of the line. That is a local check with no traversal.

### ⚠️ Half of the requirements are NOT pure reads — audit before calling

**There are fourteen, not twelve.** `DamageTakenRequirement` and `MusicPlayedStageRequirement`
do not end in `GrowStageRequirement`, so a name-based search misses them. Find implementors of
`IGrowStageRequirement`, not types matching a suffix.

Worse, six call `GuidPersistenceList.FindOrCreate`, which **inserts a record into the save**
when the plant does not already have one, and one rewrites its own persistence outright:

| Requirement | Safe to call? | Why |
|---|---|---|
| `WaterGrowStageRequirement` | ✅ | days watered vs. needed; pooled local list only |
| `SeasonGrowStageRequirement` | ✅ | `Time.Season` against `GrowingSeasons` |
| `NearWaterGrowStageRequirement` | ✅ | `GridSurface.TileHasState` |
| `CropsNearGrowStageRequirement` | ✅ | neighbour growables in range |
| `FootprintGrowStageRequirement` | ✅ | target stage's footprint cells vs. other grid objects |
| `WildTreeGrowStageRequirement` | ✅ | `!IsPlayerPlanted` |
| `AutoGrowStageRequirement` | ✅ | always true (skipped anyway — not actionable) |
| `PlantDrankGrowStageRequirement` | ❌ | `FindOrCreate` on `DrinkingPlants` |
| `PlantFedGrowStageRequirement` | ❌ | `FindOrCreate` on `FeedablePlants` |
| `PlantPettedGrowStageRequirement` | ❌ | `FindOrCreate` on `PettablePlants` |
| `WeepingWiccaGrowStageRequirement` | ❌ | `FindOrCreate` — **derives from CropsNear**, so test it first |
| `DamageTakenRequirement` | ❌ | `FindOrCreate` on `DamagePersistences` |
| `MusicPlayedStageRequirement` | ❌ | `FindOrCreate` **and** appends to `DaysTransmittionHeard` and removes `Transmitters` — a state machine wearing a query's clothes |
| `RandomChanceGrowStageRequirement` | ❌ | `Random.value` — consumes Unity's global RNG |

**Therefore evaluation must be an allowlist.** A blocklist was wrong twice over: it missed the
two oddly-named types entirely, and it assumed anything not obviously random was a read. An
unrecognised requirement — including anything a future game update adds — must be named and
left unevaluated.

`Requirements.CanEvaluateSafely` is that allowlist. Requirements outside it render with a
trailing `?`.

> Note the ordering trap: `WeepingWiccaGrowStageRequirement : CropsNearGrowStageRequirement`,
> so a plain `is CropsNearGrowStageRequirement` test matches it and would call the unsafe one.

### Not every path out of a stage is growth

A stage's `GrowPaths` include spreading and replacement routes, not just advancement. Reporting
the closest path by unmet count alone surfaced a *spread* path's blocked
`FootprintGrowStageRequirement` as the reason a visibly healthy tree "needs room" — it had all
the room it needed to grow, just not to spread. Rank paths whose `TargetGrowStage` is non-null
and different from the current stage first.

## Other things worth knowing

- **The planted item is the seed.** `ItemAsset.Name` on a growing vine is "Grape Seeds".
  `GrowableItemAddon.GetProduceItemAsset()?.Name` gives "Grapes" — the name a player means when
  pointing at the plant. `GetSeedsItemAsset()` is the reverse lookup.
- **Watered state is directly readable.** `WaterablePersistence.IsWatered(day, waterTypeAsset)`
  is public. The waterable is a *separate grid object sharing the plant's cell*, reached the way
  `GrowableView.FindWaterableView` does it — walk
  `GridSurface.Instance.GetGridObjectsEnumerable(pos.To3DCell())` and look each guid up in the
  public `ViewsCollection.WaterableViews`. `GrowableItemAddon.RequiredWaterType` supplies the
  type, which matters because magic-water crops are not satisfied by normal water.
- **Season names for display**: `GrowableItemAddon.GrowingSeasons` →
  `SeasonAsset.LocalizedName.GetTranslation()`. An empty list means the crop grows year-round
  and the season requirement passes unconditionally.
- **`Director.Nodes.GetGrowableGrowDurationNode` adds nothing** — it is a one-line wrapper over
  `GetTotalGrowthTime`. There is no per-stage remaining-time helper in the assembly.

## Harvest, regrowth and stumps

The stage graph alone is not enough to describe a plant. Two states it gets wrong:

### A picked vine is still at its final stage

`PlantHarvestableView` (a component under the `GrowableView`) owns harvest state:

| Member | |
|---|---|
| `IsHarvestable` | public — is there fruit on it right now |
| `RegrowsCrops` | public — does it fruit again rather than being replanted |
| `HarvestAndDestroyIfNeeded()` | increments `GrowablePersistence.TimesHarvested` |
| `Regrow()` | resets `TimesHarvested` to **0** |

So **`TimesHarvested` is a regrow-cycle flag, not a lifetime tally** — it is only ever 0 or 1.
Reporting it as "harvested 3x" was simply wrong, and that line is gone.

Regrowth is due when `TotalDay - daysToRegrowHarvest >= DayLastHarvested`.
`daysToRegrowHarvest` is a private `ItemParameterRef<int>`, but the same number is public as
`GrowableItemAddon.GetRegrowthTime()` (the `GrowthTime` parameter named `regrowth`).
`DayLastHarvested` lives in `HarvestStatusPersistence`, in
`GamePersistence.Instance.CurrentRoom.HarvestStatusGuidPersistences`, keyed by the grid
object's guid.

> ⚠️ Look it up with **`TryGetByGuid`**, never `FindOrCreate`. The latter inserts a new record
> into the save for anything that lacks one — not something a hover poll should do.

### Read PlantHarvestableView, not PlantHarvestInteractable

`PlantHarvestInteractable.IsInteractionAllowed()` also requires the player to be standing
within 0.5 world units of the plant's height. That answers "can you pick this right now", not
"is there fruit on it", and made ready crops look unready from a few tiles away.

### A chopped tree is a growable whose stump stage has no exits

Which made it report "fully grown" — the opposite of what happened to it. `ITreeGridComponent`
exposes a public `IsStump`, and `ChopStumpGridComponent` returns true for it, so
`GetComponentInChildren<ITreeGridComponent>()?.IsStump` is a direct answer.

Related guard: "fully grown" now also requires `StageCount > 1`. Single-stage growables — wild
vegetation and the like — have no paths by definition, so the no-exits test alone called all of
them fully grown.

## Open questions to answer in-game

- **Is `CachedGrowStages` order meaningful?** It is `GetComponentsInChildren<GrowStage>()`, so
  it is hierarchy order, which is *probably* authored in growth order but is not guaranteed to
  be, and the stage set is a graph. "Stage 2 of 4" may therefore be misleading for a crop with
  branching paths. Verify against a few real crops before trusting the denominator; the
  alternative is walking `TargetGrowStage` from stage 0 to get true depth, the way
  `GetFinalGrowStage()` does.
- ~~**Is `CachedGrowStages` order meaningful?**~~ **Answered: it is not a growth chain.** It is
  `GetComponentsInChildren<GrowStage>()`, so it contains every stage in the prefab — including
  ones growth never reaches. A mature tree reported "stage 5 of 6" because the sixth entry was
  the **stump** it becomes when chopped. Counting container entries is wrong; walk the graph
  from stage 0 following growth paths only (`StageGraph.cs`), breadth-first with a visited set
  so a regrowing crop's loop back terminates.

  The same mistake made "fully grown" unreachable for trees: it meant "no paths out at all",
  and a mature tree always has one left — being felled. A path carrying
  `DamageTakenRequirement` is destruction, not growth, and is excluded from both the count and
  the fully-grown test.

- **Are the `GrowthTime` parameters in stage order?** `GetTotalGrowthTime(out stages)` counts
  item *parameters*, not graph nodes — it sums the per-stage day values in the `GrowthTime`
  category (excluding one named `regrowth`). The individual values are readable via
  `ItemAsset.ParametersAddon.Parameters`, and v0.2.0's day estimate reads parameter *i* as the
  cost of leaving stage *i*. **That ordering is inferred, not proven** — the game only ever sums
  them, so nothing in the assembly confirms it. `Diagnostics.cs` dumps stage count, per-stage
  costs and each path's requirements per crop under `VerboseLogging`; hover one of each crop and
  compare.
- **`DesiredGrowStage`** is written during processing and is sometimes the *next* stage rather
  than the current one. Prefer `GrowStageContainer.GetGrowStage(GrowablePersistence.GrowStageGuid)`
  as the authoritative current stage.

## Reused from Chest Labels, unchanged

`PlantHover.cs` is `HoverLabel.cs` with `GrowableView` in place of `Chest`. Everything that made
that file hard still applies and is already solved there: `Camera.main` is null, interaction
colliders are triggers so the raycast needs `QueryTriggerInteraction.Collide`, and world UI must
be gated on `PlayerCursorInteractionScreen` rather than on `UIScreen.ShowStack` being empty.
