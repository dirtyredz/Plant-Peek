using System;
using Chicken.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PlantPeek
{
    /// <summary>
    /// Shows a growing plant's state in the world when the mouse is over it.
    ///
    /// Structure and every gotcha handled here were established by Chest Labels' HoverLabel:
    /// the game has no hover system to hook, Camera.main is null, and world UI has to be gated
    /// on PlayerCursorInteractionScreen. See mods/ChestLabels/src/ChestLabels/HoverLabel.cs.
    /// </summary>
    internal sealed class PlantHover : MonoBehaviour
    {
        private const float PollInterval = 0.08f;
        private const float OutlineWidth = 0.3f;

        private readonly PlantTargeting targeting = new PlantTargeting();
        private readonly GameNameplateBridge nameplate = new GameNameplateBridge();

        private Canvas canvas;
        private RectTransform plateRect;
        private Image plateBackground;
        private TextMeshProUGUI text;

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

            canvas.gameObject.SetActive(true);

            if (GameNameplateBridge.Available)
            {
                // The game nameplate draws itself; take our own fallback plate down while it does.
                plateRect.gameObject.SetActive(false);
                nameplate.Show(plant, body);
            }
            else
            {
                nameplate.Hide();
                plateRect.gameObject.SetActive(true);
                text.text = body;

                // Re-applied on every show so tuning these in the .cfg takes effect without a
                // restart - the UI objects themselves are only built once.
                ApplyStyle();
                FitPlateToText();
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
            if (canvas == null || hoveredPlant == null || !canvas.gameObject.activeSelf)
            {
                return;
            }

            var camera = targeting.ResolveCamera();
            if (camera == null)
            {
                return;
            }

            var anchor = hoveredPlant.transform.position + Vector3.up * PlantPeekPlugin.HoverHeight.Value;
            var screenPoint = camera.WorldToScreenPoint(anchor);

            if (screenPoint.z < 0f)
            {
                // Behind the camera.
                canvas.gameObject.SetActive(false);
                return;
            }

            plateRect.position = screenPoint;
            nameplate.Reposition(screenPoint);
        }

        private void ApplyStyle()
        {
            // The plate's colours live in the generated sprite, so tinting it white keeps them
            // as authored; only the alpha is a user setting.
            var alpha = Mathf.Clamp01(PlantPeekPlugin.HoverBackgroundAlpha.Value);
            plateBackground.color = new Color(1f, 1f, 1f, alpha);
            plateBackground.enabled = alpha > 0.003f;

            text.fontSize = PlantPeekPlugin.HoverFontSize.Value;

            // Only hand-roll an outline when the game's outline preset was not found -
            // otherwise this would fight the material GameFonts just applied.
            if (GameFonts.OutlineMaterial == null)
            {
                text.outlineWidth = OutlineWidth;
            }
        }

        /// <summary>
        /// The panel is one to five lines depending on detail level, so the plate is measured
        /// from the text rather than given a fixed size. Auto-sizing is off for the same
        /// reason - it would shrink the expanded panel to fit a box built for the short one.
        /// </summary>
        private void FitPlateToText()
        {
            text.ForceMeshUpdate();
            plateRect.sizeDelta = new Vector2(
                text.preferredWidth + 24f,
                text.preferredHeight + 12f);
        }

        /// <summary>
        /// Takes the panel down without forgetting what is hovered - see hoveredPlant.
        /// </summary>
        private void HidePanel()
        {
            nameplate.Hide();

            if (canvas != null)
            {
                canvas.gameObject.SetActive(false);
            }
        }

        private void EnsureUi()
        {
            if (canvas != null)
            {
                return;
            }

            var canvasGo = new GameObject("PlantPeek_HoverCanvas");
            canvasGo.transform.SetParent(transform, false);
            DontDestroyOnLoad(canvasGo);

            canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            // Above normal UI but below anything that deliberately claims the top.
            canvas.sortingOrder = 500;
            canvasGo.AddComponent<CanvasScaler>();

            var plate = new GameObject("Plate");
            plate.transform.SetParent(canvasGo.transform, false);

            plateRect = plate.AddComponent<RectTransform>();
            plateRect.sizeDelta = new Vector2(260f, 64f);
            plateRect.pivot = new Vector2(0.5f, 0.5f);

            plateBackground = plate.AddComponent<Image>();
            // A flat rectangle is the single thing that reads as bolted-on; the game's panels
            // are all rounded with a lighter rim. PanelSprite generates a 9-sliced one in the
            // game's palette, so the corners hold their radius at any panel size.
            plateBackground.sprite = PanelSprite.Get();
            plateBackground.type = Image.Type.Sliced;
            plateBackground.color = new Color(1f, 1f, 1f, 0f);
            plateBackground.raycastTarget = false;

            var textGo = new GameObject("Text");
            textGo.transform.SetParent(plate.transform, false);

            var textRect = textGo.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(8f, 3f);
            textRect.offsetMax = new Vector2(-8f, -3f);

            text = textGo.AddComponent<TextMeshProUGUI>();
            text.alignment = TextAlignmentOptions.Center;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.raycastTarget = false;
            text.color = GamePalette.NameCream;
            text.outlineColor = GamePalette.Ink;
            text.enableAutoSizing = false;
            text.fontSize = PlantPeekPlugin.HoverFontSize.Value;

            // Gelica plus the game's own outline preset. TMP_Settings.defaultFontAsset is a
            // last-resort fallback inside Apply, never the intended path - Chest Labels
            // shipped to Nexus with the stock TMP font on exactly this element because its
            // own canvas has no neighbour to inherit from. See 10-visual-integration.md.
            GameFonts.Apply(text, preferOutline: true);

            // The game's nameplate anchors to a RectTransform rather than a screen point; the
            // bridge parks an invisible one under this canvas, repositioned each frame.
            nameplate.EnsureAnchor(canvasGo.transform);

            canvasGo.SetActive(false);
            PlantPeekPlugin.Log.LogInfo("Plant hover canvas created.");
        }
    }
}
