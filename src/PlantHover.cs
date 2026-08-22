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

        /// <summary>Invisible RectTransform the game's nameplate anchors to.</summary>
        private RectTransform nameplateAnchor;

        /// <summary>Plant+text the nameplate is currently showing, to avoid restarting it.</summary>
        private string nameplateShownFor;

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

            if (UseGameNameplate)
            {
                ShowGameNameplate(plant, body);
            }
            else
            {
                HideGameNameplate();
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

            if (nameplateAnchor != null)
            {
                nameplateAnchor.position = screenPoint;
            }
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

        private static bool UseGameNameplate =>
            PlantPeekPlugin.UseGameNameplate.Value && UIScreen<NameplateScreen>.Instance != null;

        /// <summary>
        /// Show the panel in the game's own nameplate banner - the same one it uses for
        /// character names.
        ///
        /// This is what Chest Labels ships, and the reason a self-drawn plate never quite
        /// matched: font, colour, banner shape and reveal animation are all the game's, and
        /// stay right even if a patch restyles them. NameplateScreen keys Show/Hide by target
        /// RectTransform, so parking an invisible anchor at the plant's screen position is
        /// enough and we can never disturb a nameplate the game is showing for something else.
        ///
        /// CustomNameplateData.Text goes straight to SetAndRenderText, so multi-line content
        /// is fine.
        /// </summary>
        private void ShowGameNameplate(GrowableView plant, string body)
        {
            var screen = UIScreen<NameplateScreen>.Instance;
            if (screen == null || nameplateAnchor == null)
            {
                return;
            }

            plateRect.gameObject.SetActive(false);

            // Only call Show when the plant or its text actually changes; calling it every poll
            // would restart the reveal animation twelve times a second.
            var key = plant.GetInstanceID() + "" + body;
            if (key == nameplateShownFor)
            {
                return;
            }

            nameplateShownFor = key;
            screen.Show(nameplateAnchor, new CustomNameplateData(body));
            ApplyNameplateTint(screen);
        }

        private static readonly System.Collections.Generic.Dictionary<Image, Color> tintedOriginals =
            new System.Collections.Generic.Dictionary<Image, Color>();

        /// <summary>
        /// Recolour the game's bubble while it is showing our panel.
        ///
        /// NameplateScreen is shared - the game uses the same bubble for its own tooltips - so
        /// the original colour of every image touched is cached and restored the moment the
        /// panel goes away. Without that, the game's own nameplates inherit our tint.
        /// </summary>
        private static void ApplyNameplateTint(NameplateScreen screen)
        {
            var hex = PlantPeekPlugin.NameplateTint.Value;
            if (string.IsNullOrWhiteSpace(hex) || !ColorUtility.TryParseHtmlString(hex, out var tint))
            {
                return;
            }

            foreach (var image in screen.GetComponentsInChildren<Image>(true))
            {
                if (image == null)
                {
                    continue;
                }

                if (!tintedOriginals.ContainsKey(image))
                {
                    tintedOriginals[image] = image.color;
                }

                // Preserve the bubble's own alpha so a fade-in still fades.
                tint.a = image.color.a;
                image.color = tint;
            }
        }

        private static void RestoreNameplateTint()
        {
            if (tintedOriginals.Count == 0)
            {
                return;
            }

            foreach (var pair in tintedOriginals)
            {
                if (pair.Key != null)
                {
                    pair.Key.color = pair.Value;
                }
            }

            tintedOriginals.Clear();
        }

        private void HideGameNameplate()
        {
            if (nameplateShownFor == null)
            {
                RestoreNameplateTint();
                return;
            }

            nameplateShownFor = null;
            RestoreNameplateTint();

            var screen = UIScreen<NameplateScreen>.Instance;
            if (screen != null && nameplateAnchor != null)
            {
                screen.Hide(nameplateAnchor, true);
            }
        }

        /// <summary>
        /// Takes the panel down without forgetting what is hovered - see hoveredPlant.
        /// </summary>
        private void HidePanel()
        {
            HideGameNameplate();

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

            // The game's nameplate anchors to a RectTransform rather than a screen point, so
            // this invisible one is parked at the plant's screen position each frame.
            var anchorGo = new GameObject("NameplateAnchor");
            anchorGo.transform.SetParent(canvasGo.transform, false);
            nameplateAnchor = anchorGo.AddComponent<RectTransform>();
            nameplateAnchor.sizeDelta = new Vector2(1f, 1f);
            nameplateAnchor.pivot = new Vector2(0.5f, 0.5f);

            canvasGo.SetActive(false);
            PlantPeekPlugin.Log.LogInfo("Plant hover canvas created.");
        }
    }
}
