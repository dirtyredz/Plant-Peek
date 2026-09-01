using System;
using HarmonyLib;
using UnityEngine;

namespace PlantPeek
{
    /// <summary>
    /// Keeps the game nameplate usable when another mod has a broken Harmony patch on it.
    ///
    /// An outdated tooltip mod (e.g. ExtraTooltip after a game update that renamed a type its
    /// postfix references) throws from its own NameplateScreen.Show postfix. Because the game's
    /// original Show has already drawn the nameplate by the time postfixes run, a finalizer can
    /// swallow that foreign failure and our label still shows - without falling back to a
    /// self-drawn plate. Only the stale-reference exception kinds are suppressed, and only the
    /// first is logged.
    /// </summary>
    internal static class NameplateGuard
    {
        private static bool warned;

        [HarmonyPatch(typeof(NameplateScreen), "Show", new[] { typeof(RectTransform), typeof(INameplateData) })]
        [HarmonyFinalizer]
        private static void NameplateScreen_Show_Finalizer(ref Exception __exception)
        {
            if (__exception == null)
            {
                return;
            }

            if (__exception is TypeLoadException
                || __exception is TypeInitializationException
                || __exception is MissingMemberException)
            {
                if (!warned)
                {
                    warned = true;
                    PlantPeekPlugin.Log.LogWarning(
                        "Another mod's NameplateScreen.Show patch threw (" + __exception.GetType().Name +
                        "); suppressing it so the game nameplate still works. Usually an outdated " +
                        "tooltip mod after a game update.");
                }

                // Base nameplate already rendered before postfixes ran - safe to drop this.
                __exception = null;
            }
        }
    }
}
