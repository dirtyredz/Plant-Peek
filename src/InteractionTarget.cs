using System;
using Chicken.UI;
using HarmonyLib;
using UnityEngine;

namespace PlantPeek
{
    /// <summary>
    /// The plant the game's own cursor-interaction screen is currently pointing at.
    ///
    /// Chest Labels learned this the hard way: a private mouse raycast disagrees with the
    /// game's interaction range, so the interaction arrow appears before the mod's label and
    /// vanishes as the label catches up. Reading the game's own target gives one source of
    /// truth and no boundary mismatch.
    ///
    /// **But it cannot be the only source here.** A chest is always interactable, so Chest
    /// Labels can rely on this. A crop halfway through growing frequently has nothing to do to
    /// it - which is exactly the plant this mod exists to describe. So this is tried first and
    /// the caller falls back to a raycast, rather than the other way round.
    /// </summary>
    internal static class InteractionTarget
    {
        private static System.Reflection.FieldInfo showingSourceField;
        private static System.Reflection.FieldInfo sourceContextField;
        private static bool unavailable;

        internal static GrowableView FindPlant()
        {
            if (unavailable)
            {
                return null;
            }

            try
            {
                var screen = UIScreen<PlayerCursorInteractionScreen>.Instance;
                if (screen == null)
                {
                    return null;
                }

                if (showingSourceField == null)
                {
                    // Declared protected on the generic base BaseInteractionScreen<T>.
                    showingSourceField = AccessTools.Field(
                        typeof(PlayerCursorInteractionScreen), "showingSource");

                    if (showingSourceField == null)
                    {
                        unavailable = true;
                        PlantPeekPlugin.Log.LogWarning(
                            "Interaction source unavailable; using mouse raycast only.");
                        return null;
                    }
                }

                var source = showingSourceField.GetValue(screen);
                if (source == null)
                {
                    return null;
                }

                if (sourceContextField == null)
                {
                    sourceContextField = AccessTools.Field(source.GetType(), "Context");
                    if (sourceContextField == null)
                    {
                        unavailable = true;
                        return null;
                    }
                }

                // For a plant the Context is the interactable component - PlantHarvest,
                // PlantFeed, PlantScythe and so on - which sits under the GrowableView.
                switch (sourceContextField.GetValue(source))
                {
                    case GrowableView view:
                        return view;
                    case Component component:
                        return component.GetComponentInParent<GrowableView>();
                    default:
                        return null;
                }
            }
            catch (Exception e)
            {
                unavailable = true;
                PlantPeekPlugin.Log.LogWarning(
                    $"Interaction source lookup failed, using mouse raycast instead: {e.Message}");
                return null;
            }
        }
    }
}
