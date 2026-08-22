using System.Collections.Generic;

namespace PlantPeek
{
    /// <summary>
    /// Works out where a plant is in its growth, by walking the stage graph.
    ///
    /// <b>The stage container is not a growth chain.</b> `CachedGrowStages` is just
    /// `GetComponentsInChildren&lt;GrowStage&gt;()`, so it includes stages that are not part of
    /// growing at all - most obviously the stump a tree becomes when chopped. Counting its
    /// entries reported mature trees as "stage 5 of 6", implying another stage of growth to
    /// come when stage 6 was the stump.
    ///
    /// So the count comes from a breadth-first walk from the first stage following only paths
    /// that represent growth. Destruction paths are excluded, which also fixes the companion
    /// bug: "fully grown" used to mean "no paths out at all", and a mature tree always has one
    /// path left - being chopped down.
    ///
    /// The walk is breadth-first with a visited set rather than the game's own
    /// `GetFinalGrowStage`, which follows the first path in a `while` loop and never
    /// terminates on a regrowing crop whose harvest stage links back to an earlier one.
    /// </summary>
    internal static class StageGraph
    {
        /// <summary>
        /// Where a plant sits in its growth. Returned by <see cref="Measure"/> rather than
        /// written into <see cref="GrowthReader.PlantInfo"/>, so a graph utility does not
        /// depend on one consumer's aggregate. A default value (all zero/false) is the
        /// "nothing to say" answer for a missing or empty stage container.
        /// </summary>
        internal readonly struct StageMeasurement
        {
            /// <summary>1-based, or 0 when the current stage guid isn't in the growth graph.</summary>
            internal readonly int StageNumber;
            internal readonly int StageCount;
            internal readonly bool IsFullyGrown;

            internal StageMeasurement(int stageNumber, int stageCount, bool isFullyGrown)
            {
                StageNumber = stageNumber;
                StageCount = stageCount;
                IsFullyGrown = isFullyGrown;
            }
        }

        internal static StageMeasurement Measure(List<GrowStage> stages, GrowStage current)
        {
            if (stages == null || stages.Count == 0)
            {
                return default;
            }

            var first = stages[0];
            if (first == null)
            {
                return default;
            }

            var depths = new Dictionary<GrowStage, int> { { first, 0 } };
            var queue = new Queue<GrowStage>();
            queue.Enqueue(first);

            while (queue.Count > 0)
            {
                var stage = queue.Dequeue();

                foreach (var path in stage.GrowPaths)
                {
                    if (!GrowthPaths.IsGrowthTransition(path, stage))
                    {
                        continue;
                    }

                    var target = path.TargetGrowStage;
                    // The visited check is what makes a regrowing crop's loop back to an
                    // earlier stage terminate.
                    if (depths.ContainsKey(target))
                    {
                        continue;
                    }

                    depths[target] = depths[stage] + 1;
                    queue.Enqueue(target);
                }
            }

            var stageCount = depths.Count;

            var stageNumber = 0;
            if (current != null && depths.TryGetValue(current, out var depth))
            {
                stageNumber = depth + 1;
            }

            // Fully grown means nothing further to grow *into* - not "no paths at all", which
            // was never true of a tree that can still be felled.
            var isFullyGrown = current != null && !HasGrowthPath(current) && stageCount > 1;

            return new StageMeasurement(stageNumber, stageCount, isFullyGrown);
        }

        internal static bool HasGrowthPath(GrowStage stage)
        {
            foreach (var path in stage.GrowPaths)
            {
                if (GrowthPaths.IsGrowthTransition(path, stage))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
