using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PlantPeek
{
    /// <summary>
    /// The mod's own fallback plate: a screen-space overlay canvas with a 9-sliced background
    /// and a text label, drawn at the plant's screen position when the game's nameplate banner
    /// is unavailable (or turned off in config).
    ///
    /// Extracted from <see cref="PlantHover"/> so that component reads as hover orchestration
    /// rather than also building and styling UI. It owns the canvas, plate and text objects and
    /// nothing else - it does not decide *when* to show (that is the poll loop's job) nor where
    /// on screen (the orchestrator projects the world point and hands it to <see cref="PositionAt"/>).
    ///
    /// The canvas is also the coordinate host the game-nameplate anchor parents under: it is a
    /// screen-space overlay, so a child RectTransform's <c>position</c> is read in screen pixels.
    /// The bridge borrows it via <see cref="Root"/>; the orchestrator does that wiring, keeping
    /// this class unaware of the nameplate.
    /// </summary>
    internal sealed class PlantHoverPanel
    {
        private const float OutlineWidth = 0.3f;

        private Canvas canvas;
        private RectTransform plateRect;
        private Image plateBackground;
        private TextMeshProUGUI text;

        /// <summary>True once the canvas exists and is showing (either mode of the hover).</summary>
        internal bool IsActive => canvas != null && canvas.gameObject.activeSelf;

        /// <summary>
        /// The screen-space overlay both drawers position against - the plate directly, and the
        /// game-nameplate anchor by parenting under it. Null until <see cref="EnsureBuilt"/> runs.
        /// </summary>
        internal Transform Root => canvas != null ? canvas.transform : null;

        /// <summary>
        /// Build the canvas/plate/text once under the given parent, so <see cref="Root"/> and the
        /// plate exist. Idempotent: a second call is a no-op.
        /// </summary>
        internal void EnsureBuilt(Transform parent)
        {
            if (canvas != null)
            {
                return;
            }

            var canvasGo = new GameObject("PlantPeek_HoverCanvas");
            canvasGo.transform.SetParent(parent, false);
            Object.DontDestroyOnLoad(canvasGo);

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

            canvasGo.SetActive(false);
            PlantPeekPlugin.Log.LogInfo("Plant hover canvas created.");
        }

        /// <summary>Bring the canvas up (both hover modes need it active).</summary>
        internal void Activate()
        {
            if (canvas != null)
            {
                canvas.gameObject.SetActive(true);
            }
        }

        /// <summary>Take the whole hover UI down without destroying it.</summary>
        internal void Deactivate()
        {
            if (canvas != null)
            {
                canvas.gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// Draw the fallback plate with the given text. Style and size are re-applied on every
        /// show so tuning them in the .cfg takes effect without a restart - the UI objects
        /// themselves are only built once.
        /// </summary>
        internal void ShowPlate(string body)
        {
            plateRect.gameObject.SetActive(true);
            text.text = body;
            ApplyStyle();
            FitPlateToText();
        }

        /// <summary>Hide just the plate, leaving the canvas up for the nameplate anchor.</summary>
        internal void HidePlate()
        {
            if (plateRect != null)
            {
                plateRect.gameObject.SetActive(false);
            }
        }

        /// <summary>Park the plate at a screen point the orchestrator projected.</summary>
        internal void PositionAt(Vector3 screenPoint)
        {
            if (plateRect != null)
            {
                plateRect.position = screenPoint;
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
    }
}
