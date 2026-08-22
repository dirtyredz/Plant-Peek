using System.Collections.Generic;
using Chicken.Utilities;
using UnityEngine;

namespace PlantPeek
{
    /// <summary>
    /// Reads what a growing plant's state is. Strictly read-only: nothing here writes to
    /// persistence, and nothing here calls a game method that mutates state.
    ///
    /// The whole path is public on the game's own types, so there is no reflection anywhere.
    /// See research/01-growth-system.md - in particular for the two game methods that look
    /// like the obvious ones to call and must not be called.
    /// </summary>
    internal static class GrowthReader
    {
        internal sealed class PlantInfo
        {
            /// <summary>
            /// The planted item's name - the seed ("Grape Seeds"), what the game calls the
            /// growable however mature it is.
            /// </summary>
            internal string PlantedItemName;

            /// <summary>
            /// What the plant will yield ("Grapes"), or null when it has no distinct produce.
            /// The model reports both names; <see cref="PanelText"/> decides which to show.
            /// </summary>
            internal string ProduceName;
            /// <summary>1-based, or 0 when the current stage guid isn't in the container.</summary>
            internal int StageNumber;
            internal int StageCount;
            internal int DaysSincePlanted;
            internal int DaysInStage;

            internal bool IsFullyGrown;
            internal bool ReadyToHarvest;

            /// <summary>A chopped tree. Its stage graph says nothing useful about growth.</summary>
            internal bool IsStump;

            /// <summary>
            /// How far through being chopped down, 0-100. Null when the plant takes no damage
            /// or has taken none - an untouched tree should say nothing about it.
            /// </summary>
            internal int? ChoppedPercent;

            /// <summary>This crop fruits again rather than being replanted.</summary>
            internal bool Regrows;

            /// <summary>
            /// Set only while picked and waiting to fruit again. 0 means it is due.
            /// </summary>
            internal int? DaysUntilRegrow;

            /// <summary>Null when the plant has no waterable tile under it (wild plants).</summary>
            internal bool? WateredToday;

            /// <summary>
            /// Null whenever an estimate would be dishonest - see EstimateDaysLeft.
            /// </summary>
            internal int? EstimatedDaysLeft;

            internal List<Requirements.Entry> NextStageRequirements = new List<Requirements.Entry>();
        }

        internal static PlantInfo Read(GrowableView view)
        {
            var gridPersistence = view?.GridObjectPersistence;
            var growablePersistence = view?.GrowablePersistence;
            if (gridPersistence == null || growablePersistence == null)
            {
                return null;
            }

            var itemAsset = gridPersistence.ItemAsset;
            var addon = itemAsset?.GrowableAddon;
            if (addon == null)
            {
                return null;
            }

            var stages = addon.GrowStageContainer?.CachedGrowStages;
            var stageIndex = IndexOfStage(stages, growablePersistence.GrowStageGuid);
            var currentStage = stageIndex >= 0 ? stages[stageIndex] : null;

            var info = new PlantInfo
            {
                PlantedItemName = itemAsset.Name,
                ProduceName = ReadProduceName(addon),
            };

            var measurement = StageGraph.Measure(stages, currentStage);
            info.StageNumber = measurement.StageNumber;
            info.StageCount = measurement.StageCount;
            info.IsFullyGrown = measurement.IsFullyGrown;

            // Time.TotalDay is the same clock DayPlanted and DayGrowStageChanged are recorded
            // against - see WaterGrowStageRequirement, which compares them directly.
            var time = GamePersistence.Instance?.Time;
            if (time != null)
            {
                info.DaysSincePlanted = time.TotalDay - growablePersistence.DayPlanted;
                info.DaysInStage = time.TotalDay - growablePersistence.DayGrowStageChanged;
            }

            // A chopped tree is still a growable with a stage graph, and its stump stage has no
            // paths out - so without this it reported "fully grown", which is the opposite of
            // what happened to it.
            var tree = view.GetComponentInChildren<ITreeGridComponent>();
            info.IsStump = tree != null && tree.IsStump;

            // IsFullyGrown came from the stage measurement above; a stump is never "fully grown".
            if (info.IsStump)
            {
                info.IsFullyGrown = false;
            }

            ReadHarvestState(view, addon, gridPersistence, growablePersistence, time, info);

            info.WateredToday = ReadWateredToday(view, addon, time);

            if (currentStage != null)
            {
                info.NextStageRequirements = Requirements.Read(
                    currentStage, gridPersistence, growablePersistence, addon);

                info.ChoppedPercent = ReadChoppedPercent(currentStage, gridPersistence);
            }

            // Graph depth, not the container index. The per-stage day costs are authored per
            // growth step, so they line up with how far along the growth path a plant is -
            // not with where its stage happens to sit in the component list.
            info.EstimatedDaysLeft = EstimateDaysLeft(addon, info.StageNumber - 1, info);

            return info;
        }

        /// <summary>
        /// The name of what this plant yields, or null when it has none or it reads blank.
        /// Whether to prefer this over the seed name is a presentation choice PanelText makes
        /// (see UseProduceName) - the model just reports the fact.
        /// </summary>
        private static string ReadProduceName(GrowableItemAddon addon)
        {
            var produceName = addon.GetProduceItemAsset()?.Name;
            return string.IsNullOrWhiteSpace(produceName) ? null : produceName;
        }

        /// <summary>
        /// Chopping progress as a percentage, or null when there is nothing to say.
        ///
        /// "waiting on chopping" was a poor way to describe this - it is progress, not a
        /// condition the player is stuck behind, so it belongs on the status line. An untouched
        /// tree returns null rather than "0%", since a tree nobody has swung at does not need
        /// to mention axes at all.
        ///
        /// Computed here rather than by calling DamageTakenRequirement.IsRequirementCompleted,
        /// which uses FindOrCreate and would write a damage record into the save for every
        /// tree the mouse passes over. Both halves are public: the requirement's DamageAmount
        /// and DamagePersistence.DamageValue.
        /// </summary>
        private static int? ReadChoppedPercent(GrowStage stage, GridObjectPersistence gridPersistence)
        {
            DamageTakenRequirement requirement = null;
            foreach (var path in stage.GrowPaths)
            {
                if (path == null)
                {
                    continue;
                }

                requirement = path.GetComponent<DamageTakenRequirement>();
                if (requirement != null)
                {
                    break;
                }
            }

            if (requirement == null)
            {
                return null;
            }

            float needed;
            try
            {
                needed = requirement.DamageAmount.GetValue(requirement, throwError: false);
            }
            catch
            {
                return null;
            }

            if (needed <= 0f)
            {
                return null;
            }

            // TryGetByGuid, never FindOrCreate. No record simply means nothing has hit it.
            var damages = GamePersistence.Instance?.CurrentRoom?.DamagePersistences;
            if (damages == null || !damages.TryGetByGuid(gridPersistence.Guid, out var damage) || damage.DamageValue <= 0)
            {
                return null;
            }

            return Mathf.Clamp(Mathf.RoundToInt(damage.DamageValue / needed * 100f), 1, 100);
        }


        /// <summary>
        /// Harvestability, and for a regrowing crop how long until it fruits again.
        ///
        /// Reads PlantHarvestableView rather than PlantHarvestInteractable. The interactable
        /// additionally requires the player to be standing at the right height next to it, so
        /// it answers "can you pick this right now", not "is there fruit on it" - which made a
        /// ready crop look unready from three tiles away.
        ///
        /// Regrowth is the other half: after picking, a vine keeps its final stage and simply
        /// waits, so the stage graph alone still says "fully grown" while the player is looking
        /// at a bare vine.
        /// </summary>
        private static void ReadHarvestState(
            GrowableView view,
            GrowableItemAddon addon,
            GridObjectPersistence gridPersistence,
            GrowablePersistence growablePersistence,
            TimePersistence time,
            PlantInfo info)
        {
            var harvestable = view.GetComponentInChildren<PlantHarvestableView>();
            if (harvestable == null)
            {
                return;
            }

            try
            {
                info.ReadyToHarvest = harvestable.IsHarvestable;
                info.Regrows = harvestable.RegrowsCrops;
            }
            catch
            {
                // An unanswerable question is better than losing the whole panel.
                return;
            }

            // TimesHarvested is a regrow-cycle flag, not a lifetime tally - PlantHarvestableView
            // increments it on harvest and Regrow() resets it to zero. Non-zero means "picked,
            // waiting to fruit again".
            if (info.ReadyToHarvest || !info.Regrows || growablePersistence.TimesHarvested <= 0 || time == null)
            {
                return;
            }

            var regrowDays = addon.GetRegrowthTime();
            if (regrowDays <= 0)
            {
                return;
            }

            // TryGetByGuid, never FindOrCreate - the latter writes a new record into the save
            // for anything that does not have one, which is not something a hover should do.
            var statuses = GamePersistence.Instance?.CurrentRoom?.HarvestStatusGuidPersistences;
            if (statuses != null && statuses.TryGetByGuid(gridPersistence.Guid, out var status))
            {
                info.DaysUntilRegrow = Mathf.Max(
                    status.DayLastHarvested + regrowDays - time.TotalDay, 0);
            }
        }

        /// <summary>
        /// Whether the tile under the plant has had today's water of the type this crop needs.
        /// Null when the plant has no waterable tile at all - wild plants and trees.
        /// </summary>
        private static bool? ReadWateredToday(GrowableView view, GrowableItemAddon addon, TimePersistence time)
        {
            if (time == null)
            {
                return null;
            }

            var waterable = FindWaterable(view);
            if (waterable?.WaterablePersistence == null)
            {
                Diagnostics.WarnMissingWaterOnce(view, "no waterable tile found at the plant's position");
                return null;
            }

            // The crop states the type it needs; magic-water crops are not satisfied by normal
            // water. WaterableView.RequiredWaterType is the tile's own answer and falls back to
            // normal water, so it is only used when the crop does not specify.
            var waterType = addon.RequiredWaterType ?? waterable.RequiredWaterType;
            if (waterType == null)
            {
                Diagnostics.WarnMissingWaterOnce(view, "no required water type on the crop or the tile");
                return null;
            }

            return waterable.WaterablePersistence.IsWatered(time.TotalDay, waterType);
        }

        /// <summary>
        /// The waterable is a separate grid object sharing the plant's cell.
        ///
        /// Matched on position against the public ViewsCollection, which every WaterableView
        /// adds itself to in Load. An earlier version walked
        /// GridSurface.GetGridObjectsEnumerable(pos.To3DCell()) the way
        /// GrowableView.FindWaterableView does; this is fewer moving parts and does not depend
        /// on what that enumerable chooses to include.
        /// </summary>
        private static WaterableView FindWaterable(GrowableView view)
        {
            var position = view.GridObjectPersistence.Position;

            foreach (var waterable in ViewsCollection.WaterableViews.All)
            {
                if (waterable != null
                    && waterable.GridObjectPersistence != null
                    && waterable.Position == position)
                {
                    return waterable;
                }
            }

            return null;
        }

        /// <summary>
        /// Days to fully grown, or null when saying a number would be a lie.
        ///
        /// The game stores per-stage day costs as item parameters in the GrowthTime category
        /// (GetTotalGrowthTime sums exactly these). Parameter i is read as the cost of leaving
        /// stage i, which makes the remainder the sum from the current stage onwards.
        ///
        /// Suppressed whenever the current transition is gated on something that is not a
        /// matter of waiting - a season, a random roll, feeding, petting - because then the
        /// plant can sit at this stage indefinitely and any number is fiction.
        /// </summary>
        private static int? EstimateDaysLeft(GrowableItemAddon addon, int stageIndex, PlantInfo info)
        {
            if (info.IsFullyGrown || info.IsStump || info.ReadyToHarvest || stageIndex < 0)
            {
                return null;
            }

            foreach (var requirement in info.NextStageRequirements)
            {
                // Only *outstanding* requirements can invalidate an estimate. A season that is
                // already satisfied is not a reason to withhold a number - the earlier version
                // treated met and unmet alike and hid the estimate on almost everything.
                if (requirement.State == Requirements.State.Met)
                {
                    continue;
                }

                if (!requirement.IsMerelyTime)
                {
                    return null;
                }
            }

            var costs = ReadStageCosts(addon);
            if (costs.Count == 0 || stageIndex >= costs.Count)
            {
                return null;
            }

            var remaining = 0;
            for (var i = stageIndex; i < costs.Count; i++)
            {
                remaining += costs[i];
            }

            if (remaining <= 0)
            {
                return null;
            }

            // Credit the days already banked at this stage.
            return Mathf.Max(remaining - info.DaysInStage, 0);
        }

        /// <summary>
        /// The per-stage day costs, in authored order. "regrowth" is excluded the same way
        /// GrowableItemAddon.GetTotalGrowthTime excludes it.
        /// </summary>
        internal static List<int> ReadStageCosts(GrowableItemAddon addon)
        {
            var costs = new List<int>();

            var parameters = addon?.Item?.ParametersAddon?.Parameters;
            var category = AddressableLibrary<ItemParameterCategoryLibrary>.Instance?.GrowthTime;
            if (parameters == null || category == null)
            {
                return costs;
            }

            foreach (var parameter in parameters)
            {
                if (parameter == null || parameter.Category != category)
                {
                    continue;
                }

                if (parameter.Name != null && parameter.Name.ToLower() == "regrowth")
                {
                    continue;
                }

                if (parameter.TryReadValue<int>(out var days))
                {
                    costs.Add(days);
                }
            }

            return costs;
        }

        private static int IndexOfStage(List<GrowStage> stages, string guid)
        {
            if (stages == null || string.IsNullOrEmpty(guid))
            {
                return -1;
            }

            for (var i = 0; i < stages.Count; i++)
            {
                if (stages[i] != null && stages[i].Guid == guid)
                {
                    return i;
                }
            }

            return -1;
        }
    }
}
