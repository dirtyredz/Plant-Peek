using System.Collections.Generic;
using System.Text;

namespace PlantPeek
{
    /// <summary>
    /// One-off dump of a crop's growth data, to settle the assumption the day estimate rests
    /// on: that the GrowthTime item parameters are in stage order, so parameter i is the cost
    /// of leaving stage i.
    ///
    /// That is inferred, not proven - GetTotalGrowthTime only ever sums them, so nothing in
    /// the assembly reveals their order. Hover one of each crop with VerboseLogging on and the
    /// log says whether the counts line up.
    /// </summary>
    internal static class Diagnostics
    {
        private static readonly HashSet<string> Logged = new HashSet<string>();

        internal static void LogPlantOnce(GrowableView view)
        {
            var itemAsset = view?.GridObjectPersistence?.ItemAsset;
            var addon = itemAsset?.GrowableAddon;
            if (addon == null)
            {
                return;
            }

            var key = itemAsset.name;
            if (string.IsNullOrEmpty(key) || !Logged.Add(key))
            {
                return;
            }

            var stages = addon.GrowStageContainer?.CachedGrowStages;
            var costs = GrowthReader.ReadStageCosts(addon);

            var builder = new StringBuilder();
            builder.Append($"[growth data] {itemAsset.Name} ({key})");
            builder.Append($"\n  produce      : {addon.GetProduceItemAsset()?.Name ?? "(none)"}");
            builder.Append($"\n  stages       : {stages?.Count ?? 0}");
            builder.Append($"\n  stage costs  : [{string.Join(", ", costs)}]  (sum {Sum(costs)})");
            builder.Append($"\n  total growth : {addon.GetTotalGrowthTime(out var paramStages)} over {paramStages} parameter(s)");
            builder.Append($"\n  regrowth     : {addon.GetRegrowthTime()}");
            builder.Append($"\n  water type   : {(addon.RequiredWaterType != null ? addon.RequiredWaterType.name : "(none)")}");

            if (stages != null)
            {
                for (var i = 0; i < stages.Count; i++)
                {
                    var stage = stages[i];
                    if (stage == null)
                    {
                        continue;
                    }

                    builder.Append($"\n  stage {i}: paths ->");
                    var any = false;
                    foreach (var path in stage.GrowPaths)
                    {
                        if (path == null)
                        {
                            continue;
                        }

                        any = true;
                        builder.Append($" [{DescribePath(path)} => {path.TargetGrowStage?.name ?? "(end)"}]");
                    }

                    if (!any)
                    {
                        builder.Append(" (none - final stage)");
                    }
                }
            }

            PlantPeekPlugin.Log.LogInfo(builder.ToString());
        }

        /// <summary>
        /// Names the requirement components on a path. Deliberately does not evaluate them -
        /// RandomChanceGrowStageRequirement's check consumes Unity's global RNG.
        /// </summary>
        private static string DescribePath(GrowPath path)
        {
            var names = new List<string>();
            foreach (var requirement in path.GetComponents<IGrowStageRequirement>())
            {
                if (requirement != null)
                {
                    names.Add(requirement.GetType().Name.Replace("GrowStageRequirement", string.Empty));
                }
            }

            return names.Count == 0 ? "unconditional" : string.Join("+", names);
        }

        private static int Sum(List<int> values)
        {
            var total = 0;
            foreach (var value in values)
            {
                total += value;
            }

            return total;
        }
    }
}
