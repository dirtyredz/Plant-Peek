using Chicken.Utilities;

namespace PlantPeek
{
    /// <summary>
    /// Explains, once per crop, why the watered line is missing from the panel - rather than
    /// leaving a silent gap that looks like the feature simply does not work.
    ///
    /// Lives apart from <see cref="Diagnostics"/> on purpose: that class reaches back into
    /// <see cref="GrowthReader"/> (for the growth-cost dump), whereas this warning is *called by*
    /// GrowthReader. Keeping them in one file would make GrowthReader and its diagnostics
    /// mutually dependent; split by dependency direction, each edge points one way. Self-gated on
    /// VerboseLogging so the read-only model can call it without knowing about diagnostics config.
    /// </summary>
    internal static class WaterDiagnostics
    {
        private static readonly System.Collections.Generic.HashSet<string> WarnedCrops =
            new System.Collections.Generic.HashSet<string>();

        internal static void WarnMissingWaterOnce(GrowableView view, string reason)
        {
            if (!PlantPeekPlugin.VerboseLogging.Value)
            {
                return;
            }

            var persistence = view?.GridObjectPersistence;
            var key = persistence?.ItemAsset?.name;
            if (string.IsNullOrEmpty(key) || !WarnedCrops.Add(key))
            {
                return;
            }

            PlantPeekPlugin.Log.LogInfo(
                $"[water] {key} at {persistence.Position}: no watered state - {reason}. " +
                $"Waterables known: {CountWaterables()}.");
        }

        private static int CountWaterables()
        {
            var count = 0;
            foreach (var waterable in ViewsCollection.WaterableViews.All)
            {
                if (waterable != null)
                {
                    count++;
                }
            }

            return count;
        }
    }
}
