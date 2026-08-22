using System;
using Chicken.UI;
using UnityEngine;

namespace PlantPeek
{
    /// <summary>
    /// Shows a growing plant's state in the world when the mouse is over it.
    ///
    /// Structure and every gotcha handled here were established by Chest Labels' HoverLabel:
    /// the game has no hover system to hook, Camera.main is null, and world UI has to be gated
    /// on PlayerCursorInteractionScreen. See mods/ChestLabels/src/ChestLabels/HoverLabel.cs.
    ///
    /// Orchestration only: it polls, decides when to show and at what detail, projects the
    /// plant's world point to the screen, and hands the result to the two drawers - the mod's
    /// own <see cref="PlantHoverPanel"/> and the <see cref="GameNameplateBridge"/>.
    /// </summary>
    internal sealed class PlantHover : MonoBehaviour
    {
        private const float PollInterval = 0.08f;

        private readonly PlantTargeting targeting = new PlantTargeting();
        private readonly GameNameplateBridge nameplate = new GameNameplateBridge();
        private readonly PlantHoverPanel panel = new PlantHoverPanel();

        private float nextPollTime;

        /// <summary>
        /// The plant under the cursor, whether or not a panel is being drawn for it.
        ///
        /// Deliberately separate from panel visibility. With Detail = Hidden the panel is down
        /// on every unexpanded poll, and folding these together meant hiding the panel also
        /// forgot which plant was hovered - so a click could never latch onto anything.
        /// </summary>
        private GrowableView hoveredPlant;

        private bool warnedNoCamera;

        /// <summary>The plant most recently clicked, for ExpandMode.Click.</summary>
        private GrowableView clickedPlant;

        /// <summary>Latched state for ExpandMode.Toggle.</summary>
        private bool toggledOpen;

        private bool loggedExpanded;

        private void Update()
        {
            if (!PlantPeekPlugin.ShowHover.Value)
            {
                hoveredPlant = null;
                HidePanel();
                return;
            }

            // Input is sampled every frame, not on the poll interval - an 80ms gate drops
            // clicks and key taps that fall between polls.
            TrackExpandInput();

            if (Time.unscaledTime < nextPollTime)
            {
                // Keep the plate on the plant we already found so it tracks a moving camera.
                Reposition();
                return;
            }

            nextPollTime = Time.unscaledTime + PollInterval;

            try
            {
                Poll();
            }
            catch (Exception e)
            {
                PlantPeekPlugin.Log.LogError($"Plant hover failed; disabling it. {e}");
                PlantPeekPlugin.ShowHover.Value = false;
                hoveredPlant = null;
                HidePanel();
            }
        }

        private void TrackExpandInput()
        {
            switch (PlantPeekPlugin.ExpandTrigger.Value)
            {
                case PlantPeekPlugin.ExpandMode.Click:
                    if (UnityEngine.Input.GetMouseButtonDown(0))
                    {
                        // Clicking the same plant again collapses it; clicking elsewhere or on
                        // nothing clears the expansion.
                        clickedPlant = hoveredPlant != null && clickedPlant != hoveredPlant
                            ? hoveredPlant
                            : null;
                    }
                    break;

                case PlantPeekPlugin.ExpandMode.Toggle:
                    if (Hotkey.WasPressed(PlantPeekPlugin.ExpandKey.Value))
                    {
                        toggledOpen = !toggledOpen;
                    }
                    break;
            }
        }

        private bool IsExpanded()
        {
            switch (PlantPeekPlugin.ExpandTrigger.Value)
            {
                case PlantPeekPlugin.ExpandMode.Hold:
                    return Hotkey.IsHeld(PlantPeekPlugin.ExpandKey.Value);
                case PlantPeekPlugin.ExpandMode.Click:
                    return hoveredPlant != null && clickedPlant == hoveredPlant;
                case PlantPeekPlugin.ExpandMode.Toggle:
                    return toggledOpen;
                default:
                    return false;
            }
        }

        private void Poll()
        {
            if (ShouldStandDown())
            {
                hoveredPlant = null;
                HidePanel();
                return;
            }

            var camera = targeting.ResolveCamera();
            if (camera == null)
            {
                if (!warnedNoCamera)
                {
                    warnedNoCamera = true;
                    PlantPeekPlugin.Log.LogWarning(
                        "No usable camera found - plant hover cannot position itself.");
                }
                hoveredPlant = null;
                HidePanel();
                return;
            }

            var plant = targeting.ResolvePlant(camera);
            hoveredPlant = plant;

            if (plant == null)
            {
                HidePanel();
                return;
            }

            var expanded = IsExpanded();
            if (PlantPeekPlugin.VerboseLogging.Value && expanded != loggedExpanded)
            {
                loggedExpanded = expanded;
                PlantPeekPlugin.Log.LogInfo(
                    $"Expand {(expanded ? "ON" : "off")} " +
                    $"(trigger {PlantPeekPlugin.ExpandTrigger.Value}, key {PlantPeekPlugin.ExpandKey.Value}).");
            }

            var level = expanded
                ? PlantPeekPlugin.ExpandedDetail.Value
                : PlantPeekPlugin.Detail.Value;

            if (level == PlantPeekPlugin.DetailLevel.Hidden)
            {
                HidePanel();
                return;
            }

            var info = GrowthReader.Read(plant);
            if (info == null)
            {
                HidePanel();
                return;
            }

            EnsureUi();
            var body = PanelText.Format(info, level);

            panel.Activate();

            if (GameNameplateBridge.Available)
            {
                // The game nameplate draws itself; take our own fallback plate down while it does.
                panel.HidePlate();
                nameplate.Show(plant, body);
            }
            else
            {
                nameplate.Hide();
                panel.ShowPlate(body);
            }

            Reposition();
        }

        /// <summary>
        /// Whether the hover should keep quiet.
        ///
        /// A positive gate, not a blocklist: PlayerCursorInteractionScreen is showing exactly
        /// when the player can point at the world, and is absent during cutscenes, menus,
        /// pause and full-screen windows. Testing UIScreen.ShowStack for emptiness does NOT
        /// work - EnergyScreen and ManaScreen sit in it permanently during normal play.
        /// </summary>
        private static bool ShouldStandDown()
        {
            var cursorScreen = UIScreen<PlayerCursorInteractionScreen>.Instance;
            return cursorScreen == null || !cursorScreen.IsShowing;
        }

        private void Reposition()
        {
            if (!panel.IsActive || hoveredPlant == null)
            {
                return;
            }

            var camera = targeting.ResolveCamera();
            if (camera == null)
            {
                return;
            }

            var worldPoint = hoveredPlant.transform.position + Vector3.up * PlantPeekPlugin.HoverHeight.Value;
            var screenPoint = camera.WorldToScreenPoint(worldPoint);

            if (screenPoint.z < 0f)
            {
                // Behind the camera.
                panel.Deactivate();
                return;
            }

            // Projection is the orchestrator's job (only it has the camera and the plant); each
            // drawer applies the screen point to its own object.
            panel.PositionAt(screenPoint);
            nameplate.Reposition(screenPoint);
        }

        /// <summary>
        /// Takes the panel down without forgetting what is hovered - see hoveredPlant.
        /// </summary>
        private void HidePanel()
        {
            nameplate.Hide();
            panel.Deactivate();
        }

        /// <summary>
        /// Builds the fallback-plate canvas once, then parks the game-nameplate anchor under the
        /// same screen-space overlay so both draw against the same coordinate host.
        /// </summary>
        private void EnsureUi()
        {
            var canvasRoot = panel.EnsureBuilt(transform);
            nameplate.EnsureAnchor(canvasRoot);
        }
    }
}
