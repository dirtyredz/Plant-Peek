using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using UnityEngine;

namespace PlantPeek
{
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    [BepInProcess("Moonlight Peaks.exe")]
    public sealed class PlantPeekPlugin : BaseUnityPlugin
    {
        public const string PluginGuid = "com.dirtyredz.moonlightpeaks.plantpeek";
        public const string PluginName = "Plant Peek";
        // Keep in step with <Version> in the csproj - pack.ps1 names the archive from that one
        // and BepInEx reports this one. See 12-versioning-and-release.md.
        public const string PluginVersion = "1.0.0";

        /// <summary>How much a hovered plant says.</summary>
        public enum DetailLevel
        {
            Hidden,
            NameOnly,
            Standard,
            Full,
        }

        /// <summary>What makes a plant say more than its resting detail level.</summary>
        public enum ExpandMode
        {
            Never,
            /// <summary>Expanded only while ExpandKey is held down.</summary>
            Hold,
            /// <summary>ExpandKey flips it on and off.</summary>
            Toggle,
            /// <summary>Left-click a plant to expand it; click it again, or elsewhere, to collapse.</summary>
            Click,
        }

        internal static ManualLogSource Log;

        internal static ConfigEntry<bool> ShowHover;
        internal static ConfigEntry<DetailLevel> Detail;
        internal static ConfigEntry<DetailLevel> ExpandedDetail;
        internal static ConfigEntry<ExpandMode> ExpandTrigger;
        internal static ConfigEntry<KeyboardShortcut> ExpandKey;

        internal static ConfigEntry<bool> UseProduceName;
        internal static ConfigEntry<bool> ShowWatered;
        internal static ConfigEntry<bool> ShowDaysLeft;
        internal static ConfigEntry<bool> ShowMetRequirements;
        internal static ConfigEntry<bool> UseCheckmarks;

        internal static ConfigEntry<bool> UseGameNameplate;
        internal static ConfigEntry<string> NameplateTint;
        internal static ConfigEntry<bool> PreferInteractionTarget;
        internal static ConfigEntry<float> HoverHeight;
        internal static ConfigEntry<float> HoverFontSize;
        internal static ConfigEntry<float> HoverBackgroundAlpha;
        internal static ConfigEntry<bool> VerboseLogging;

        // Mod Menu reads these out of ConfigDescription.Tags to title its sections. Display
        // names only - the .cfg section keys are untouched, since renaming one there would
        // orphan every saved value.
        private const string HoverSection = "ModMenu.Section=Hover panel";
        private const string DiagnosticsSection = "ModMenu.Section=Diagnostics";

        private void Awake()
        {
            Log = Logger;

            ShowHover = Config.Bind(
                "Hover", "ShowHover", true,
                "Show a growing plant's state in the world when the mouse is over it.");

            // Descriptions are kept to one short line: Mod Menu renders them in a settings row
            // and overflows on anything longer. The reasoning behind each one lives in the
            // mod's README rather than here.

            Detail = Config.Bind(
                "Display", "Detail", DetailLevel.Hidden,
                "Detail shown at rest, before expanding.");

            ExpandedDetail = Config.Bind(
                "Display", "ExpandedDetail", DetailLevel.Full,
                "Detail shown while expanded. Match Detail to disable expanding.");

            ExpandTrigger = Config.Bind(
                "Display", "ExpandTrigger", ExpandMode.Hold,
                "What reveals the extra detail. Click does not block the game's own click.");

            ExpandKey = Config.Bind(
                "Display", "ExpandKey", new KeyboardShortcut(KeyCode.LeftAlt),
                "Key for the Hold and Toggle modes. Avoid F1.");

            UseProduceName = Config.Bind(
                "Display", "UseProduceName", true,
                "Name plants after their crop, so a vine reads Grapes not Grape Seeds.");

            ShowWatered = Config.Bind(
                "Display", "ShowWatered", true,
                "Show whether the plant has had today's water.");

            ShowDaysLeft = Config.Bind(
                "Display", "ShowDaysLeft", true,
                "Show days to grown, where an honest estimate is possible.");

            ShowMetRequirements = Config.Bind(
                "Display", "ShowMetRequirements", false,
                "Also list requirements already satisfied, not just outstanding ones.");

            UseCheckmarks = Config.Bind(
                "Display", "UseCheckmarks", false,
                "Add tick and cross glyphs. May show as boxes if the font lacks them.");

            UseGameNameplate = Config.Bind(
                "Hover", "UseGameNameplate", true,
                "Draw in the game's nameplate banner. Off uses the mod's own plate.");

            NameplateTint = Config.Bind(
                "Hover", "NameplateTint", "#4A2E8F",
                "Banner colour as hex. Blank leaves the game's NPC-name orange.");

            PreferInteractionTarget = Config.Bind(
                "Hover", "PreferInteractionTarget", true,
                "Target the plant the game is pointing at, falling back to a raycast.");

            HoverHeight = Config.Bind(
                "Hover", "HoverHeight", 0.8f,
                new ConfigDescription(
                    "Height above the plant, in world units.",
                    new AcceptableValueRange<float>(0f, 3f),
                    HoverSection, "ModMenu.Label=Panel height"));

            HoverFontSize = Config.Bind(
                "Hover", "HoverFontSize", 22f,
                new ConfigDescription(
                    "Font size of the panel's first line.",
                    new AcceptableValueRange<float>(10f, 48f),
                    HoverSection, "ModMenu.Label=Text size"));

            HoverBackgroundAlpha = Config.Bind(
                "Hover", "HoverBackgroundAlpha", 0.3f,
                new ConfigDescription(
                    "Opacity of the mod's own plate, 0-1. Unused with the game nameplate.",
                    new AcceptableValueRange<float>(0f, 1f),
                    HoverSection, "ModMenu.Label=Background opacity"));

            VerboseLogging = Config.Bind(
                "Diagnostics", "VerboseLogging", false,
                new ConfigDescription(
                    "Log hover diagnostics and a one-off growth dump per crop.",
                    null,
                    DiagnosticsSection, "ModMenu.Label=Verbose logging"));

            gameObject.AddComponent<PlantHover>();

            Log.LogInfo($"{PluginName} {PluginVersion} loaded. Read-only: nothing is written to your save.");
            Log.LogInfo($"Hovering a plant shows: {Detail.Value}. {DescribeExpandTrigger()}");

            // Resolved here rather than lazily on the first hover, so the log answers "did the
            // game's font load?" at launch instead of only after the player happens to point
            // at a plant. Cheap - it is one pass over already-loaded assets, then cached.
            Log.LogInfo(
                $"Font: {(GameFonts.Font == null ? "Gelica NOT FOUND - using TMP default" : GameFonts.Font.name)}; " +
                $"outline preset: {(GameFonts.OutlineMaterial == null ? "none" : GameFonts.OutlineMaterial.name)}");
        }

        /// <summary>
        /// Says out loud how to get the extra detail. The resting panel is a name and nothing
        /// else, so without this the only clue that there is more is the config file.
        /// </summary>
        private static string DescribeExpandTrigger()
        {
            if (ExpandTrigger.Value == ExpandMode.Never || ExpandedDetail.Value == Detail.Value)
            {
                return "Expanding is off.";
            }

            switch (ExpandTrigger.Value)
            {
                case ExpandMode.Hold:
                    return $"Hold {ExpandKey.Value} over a plant for: {ExpandedDetail.Value}.";
                case ExpandMode.Toggle:
                    return $"Press {ExpandKey.Value} to toggle: {ExpandedDetail.Value}.";
                case ExpandMode.Click:
                    return $"Left-click a plant for: {ExpandedDetail.Value}.";
                default:
                    return string.Empty;
            }
        }
    }
}
