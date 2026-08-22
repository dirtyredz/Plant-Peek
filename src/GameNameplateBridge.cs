using System.Collections.Generic;
using Chicken.UI;
using UnityEngine;
using UnityEngine.UI;

namespace PlantPeek
{
    /// <summary>
    /// Draws the hover panel in the game's own nameplate banner - the same one it uses for
    /// character names - and tints it while ours is showing.
    ///
    /// Extracted from <see cref="PlantHover"/>: this is a self-contained integration with the
    /// shared <c>NameplateScreen</c> widget, with its own anchor object, its own "only restart
    /// the reveal on a real change" keying, and its own tint cache/restore lifecycle. Font,
    /// colour, banner shape and reveal animation are all the game's, and stay right even if a
    /// patch restyles them. NameplateScreen keys Show/Hide by target RectTransform, so parking
    /// an invisible anchor at the plant's screen position is enough and we can never disturb a
    /// nameplate the game is showing for something else.
    /// </summary>
    internal sealed class GameNameplateBridge
    {
        /// <summary>Invisible RectTransform the game's nameplate anchors to.</summary>
        private RectTransform anchor;

        /// <summary>Plant+text the nameplate is currently showing, to avoid restarting it.</summary>
        private string shownFor;

        private readonly Dictionary<Image, Color> tintedOriginals =
            new Dictionary<Image, Color>();

        /// <summary>True when the game nameplate is both wanted and currently available.</summary>
        internal static bool Available =>
            PlantPeekPlugin.UseGameNameplate.Value && UIScreen<NameplateScreen>.Instance != null;

        /// <summary>
        /// Create the invisible anchor under the hover canvas. Called once from the hover's UI
        /// build; the game's nameplate anchors to a RectTransform rather than a screen point, so
        /// this is parked at the plant's screen position each frame via <see cref="Reposition"/>.
        /// </summary>
        internal void EnsureAnchor(Transform canvasParent)
        {
            if (anchor != null)
            {
                return;
            }

            var anchorGo = new GameObject("NameplateAnchor");
            anchorGo.transform.SetParent(canvasParent, false);
            anchor = anchorGo.AddComponent<RectTransform>();
            anchor.sizeDelta = new Vector2(1f, 1f);
            anchor.pivot = new Vector2(0.5f, 0.5f);
        }

        internal void Reposition(Vector3 screenPoint)
        {
            if (anchor != null)
            {
                anchor.position = screenPoint;
            }
        }

        /// <summary>
        /// Show the given text in the game nameplate for the given plant.
        ///
        /// CustomNameplateData.Text goes straight to SetAndRenderText, so multi-line content is
        /// fine.
        /// </summary>
        internal void Show(GrowableView plant, string body)
        {
            var screen = UIScreen<NameplateScreen>.Instance;
            if (screen == null || anchor == null)
            {
                return;
            }

            // Only call Show when the plant or its text actually changes; calling it every poll
            // would restart the reveal animation twelve times a second.
            var key = plant.GetInstanceID() + "" + body;
            if (key == shownFor)
            {
                return;
            }

            shownFor = key;
            screen.Show(anchor, new CustomNameplateData(body));
            ApplyTint(screen);
        }

        internal void Hide()
        {
            if (shownFor == null)
            {
                RestoreTint();
                return;
            }

            shownFor = null;
            RestoreTint();

            var screen = UIScreen<NameplateScreen>.Instance;
            if (screen != null && anchor != null)
            {
                screen.Hide(anchor, true);
            }
        }

        /// <summary>
        /// Recolour the game's bubble while it is showing our panel.
        ///
        /// NameplateScreen is shared - the game uses the same bubble for its own tooltips - so
        /// the original colour of every image touched is cached and restored the moment the
        /// panel goes away. Without that, the game's own nameplates inherit our tint.
        /// </summary>
        private void ApplyTint(NameplateScreen screen)
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

        private void RestoreTint()
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
    }
}
