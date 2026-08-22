using UnityEngine;

namespace PlantPeek
{
    /// <summary>
    /// Resolves the camera to project through and the plant the player means, each poll.
    ///
    /// Extracted from <see cref="PlantHover"/>: neither job touches the panel or its text - they
    /// hand back a <see cref="Camera"/> and a <c>GrowableView</c>. Holds only a cached camera and
    /// a couple of log-once flags, so it lives as a plain field on the hover component rather than
    /// a MonoBehaviour of its own.
    /// </summary>
    internal sealed class PlantTargeting
    {
        private const float RaycastDistance = 200f;

        private Camera cachedCamera;
        private bool loggedFirstHit;
        private bool loggedInteractionSource;

        /// <summary>
        /// Camera.main is null in this game - the gameplay camera is not tagged "MainCamera",
        /// which is normal for a Cinemachine setup. Fall back to the highest-depth active
        /// camera that renders to the screen. Returns null when none is usable; the caller
        /// decides how to warn and hide.
        /// </summary>
        internal Camera ResolveCamera()
        {
            if (cachedCamera != null && cachedCamera.isActiveAndEnabled)
            {
                return cachedCamera;
            }

            var main = Camera.main;
            if (main != null)
            {
                cachedCamera = main;
                return cachedCamera;
            }

            Camera best = null;
            foreach (var candidate in Camera.allCameras)
            {
                if (candidate == null || !candidate.isActiveAndEnabled || candidate.targetTexture != null)
                {
                    continue;
                }

                if (best == null || candidate.depth > best.depth)
                {
                    best = candidate;
                }
            }

            if (best != null && cachedCamera == null)
            {
                PlantPeekPlugin.Log.LogInfo($"Plant hover using camera '{best.name}'.");
            }

            cachedCamera = best;
            return cachedCamera;
        }

        /// <summary>
        /// Which plant the panel is about.
        ///
        /// The game's own interaction target is preferred: it puts the panel on exactly the
        /// plant the interaction arrow is on, with no boundary mismatch between the mod's
        /// reach and the game's. But a growing crop often has no interaction available - and
        /// describing that plant is the whole point - so a miss falls through to the raycast
        /// rather than showing nothing.
        /// </summary>
        internal GrowableView ResolvePlant(Camera camera)
        {
            if (PlantPeekPlugin.PreferInteractionTarget.Value)
            {
                var target = InteractionTarget.FindPlant();
                if (target != null)
                {
                    if (PlantPeekPlugin.VerboseLogging.Value && !loggedInteractionSource)
                    {
                        loggedInteractionSource = true;
                        PlantPeekPlugin.Log.LogInfo(
                            "Using the game's interaction target to pick the hovered plant.");
                    }

                    return target;
                }
            }

            return FindPlantUnderMouse(camera);
        }

        /// <summary>
        /// Interaction colliders are frequently triggers, which a plain raycast skips - hence
        /// QueryTriggerInteraction.Collide and RaycastAll rather than the first hit only.
        /// </summary>
        private GrowableView FindPlantUnderMouse(Camera camera)
        {
            // Fully qualified: an `Input` type in one of the game's own namespaces would
            // otherwise shadow UnityEngine's.
            var ray = camera.ScreenPointToRay(UnityEngine.Input.mousePosition);
            var hits = Physics.RaycastAll(ray, RaycastDistance, ~0, QueryTriggerInteraction.Collide);

            GrowableView best = null;
            var bestDistance = float.MaxValue;

            foreach (var hit in hits)
            {
                var plant = hit.collider.GetComponentInParent<GrowableView>();
                if (plant != null && hit.distance < bestDistance)
                {
                    best = plant;
                    bestDistance = hit.distance;
                }
            }

            if (!loggedFirstHit && hits.Length > 0 && PlantPeekPlugin.VerboseLogging.Value)
            {
                loggedFirstHit = true;
                PlantPeekPlugin.Log.LogInfo(
                    $"Hover raycast working: {hits.Length} collider(s) under cursor, " +
                    $"first = '{hits[0].collider.name}', plant found = {best != null}");
            }

            if (best != null && PlantPeekPlugin.VerboseLogging.Value)
            {
                Diagnostics.LogPlantOnce(best);
            }

            return best;
        }
    }
}
