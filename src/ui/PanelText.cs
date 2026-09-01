using System.Text;
using UnityEngine;

namespace PlantPeek
{
    /// <summary>
    /// Turns a <see cref="GrowthReader.PlantInfo"/> into the panel's TMP-markup string, at a
    /// given <see cref="PlantPeekPlugin.DetailLevel"/>.
    ///
    /// Pure formatting: it reads plant info and display config and returns text. It owns no
    /// Unity objects and touches no game state, which is why it lives apart from the
    /// <see cref="PlantHover"/> MonoBehaviour that drives the hover, positions the plate and
    /// talks to the game's nameplate. Extracted from PlantHover so that 795-line component
    /// reads as hover orchestration rather than also being the copywriter.
    /// </summary>
    internal static class PanelText
    {
        // Strictly from GamePalette - 10-visual-integration.md is explicit that invented
        // colours are what make modded UI obvious. There is no green in the game's palette, so
        // met/unmet is carried by weight rather than hue: a satisfied requirement fades back
        // into the panel, an outstanding one is picked out in the gold the game uses for
        // numbers that matter. Gold therefore always means "this is the bit that needs you".
        private static readonly string MetColour = Rgba(GamePalette.NameCream, 0x8C);
        private static readonly string UnmetColour = Rgb(GamePalette.CountGold);
        private static readonly string UnknownColour = Rgba(GamePalette.NameCream, 0x66);
        private static readonly string ReadyColour = Rgb(GamePalette.CountGold);

        private static string Rgb(Color32 colour) =>
            "#" + ColorUtility.ToHtmlStringRGB(colour);

        private static string Rgba(Color32 colour, byte alpha) =>
            "#" + ColorUtility.ToHtmlStringRGB(colour) + alpha.ToString("X2");

        internal static string Format(GrowthReader.PlantInfo info, PlantPeekPlugin.DetailLevel level)
        {
            var builder = new StringBuilder();
            builder.Append(ResolveName(info));

            if (level == PlantPeekPlugin.DetailLevel.NameOnly)
            {
                return builder.ToString();
            }

            var status = FormatStatus(info);
            if (status.Length > 0)
            {
                builder.Append("\n<size=80%>").Append(status).Append("</size>");
            }

            if (level != PlantPeekPlugin.DetailLevel.Full)
            {
                return builder.ToString();
            }

            if (!info.IsFullyGrown && !info.IsStump)
            {
                var outstanding = FormatRequirements(info);
                if (outstanding.Length > 0)
                {
                    builder.Append("\n<size=80%>waiting on ")
                           .Append(outstanding)
                           .Append("</size>");
                }
            }

            var footer = FormatFooter(info);
            if (footer.Length > 0)
            {
                builder.Append("\n<size=72%><alpha=#BB>").Append(footer).Append("</size>");
            }

            return builder.ToString();
        }

        /// <summary>
        /// Which name to show. The planted item is the seed, so PlantedItemName reads "Grape
        /// Seeds" for a vine that is very much not a seed any more. The produce is what the plant
        /// will give you, which is the name a player means when they point at it - so
        /// UseProduceName prefers it whenever the crop has one.
        /// </summary>
        private static string ResolveName(GrowthReader.PlantInfo info)
        {
            if (PlantPeekPlugin.UseProduceName.Value && !string.IsNullOrWhiteSpace(info.ProduceName))
            {
                return info.ProduceName;
            }

            return info.PlantedItemName;
        }

        private static string FormatStatus(GrowthReader.PlantInfo info)
        {
            // A chopped tree has a stage graph like anything else, but none of it describes
            // something that is going to grow.
            if (info.IsStump)
            {
                return "chopped";
            }

            if (info.ReadyToHarvest)
            {
                return $"<color={ReadyColour}>ready to harvest</color>";
            }

            var parts = new StringBuilder();

            // Checked before IsFullyGrown: a picked vine is still at its final stage, so
            // without this it reads "fully grown" while the player looks at bare branches.
            if (info.DaysUntilRegrow.HasValue)
            {
                var days = info.DaysUntilRegrow.Value;
                parts.Append(days <= 0
                    ? "picked · fruits again tomorrow"
                    : $"picked · fruits again in {days}d");
            }
            else if (info.Regrows && info.IsFullyGrown)
            {
                // Regrowing, but the harvest record has gone - say the true part only.
                parts.Append("picked");
            }
            else if (info.IsFullyGrown)
            {
                parts.Append("fully grown");
            }
            // StageCount > 1, not > 0: weeds and other single-stage growables were reporting
            // "stage 1 of 1", which is a fact about the data model rather than about the plant.
            else if (info.StageNumber > 0 && info.StageCount > 1)
            {
                parts.Append($"stage {info.StageNumber} of {info.StageCount}");
            }

            if (PlantPeekPlugin.ShowDaysLeft.Value && info.EstimatedDaysLeft.HasValue)
            {
                Separate(parts);
                var days = info.EstimatedDaysLeft.Value;
                parts.Append(days <= 0 ? "ready tomorrow" : $"~{days}d left");
            }

            // Progress, not a blocked condition - and silent on a tree nobody has swung at.
            if (info.ChoppedPercent.HasValue)
            {
                Separate(parts);
                parts.Append($"<color={UnmetColour}>chopped {info.ChoppedPercent.Value}%</color>");
            }

            // Not gated on IsFullyGrown - a regrowing crop still wants watering between
            // harvests. Ready-to-harvest plants return above and never reach here.
            if (PlantPeekPlugin.ShowWatered.Value && info.WateredToday.HasValue)
            {
                Separate(parts);
                parts.Append(info.WateredToday.Value
                    ? $"<color={MetColour}>watered</color>"
                    : $"<color={UnmetColour}>dry</color>");
            }

            return parts.ToString();
        }

        /// <summary>
        /// Only what is actually outstanding.
        ///
        /// Listing satisfied requirements after the word "needs" made a healthy plant read as
        /// if it were short of something - "needs clear space to grow into" on a tree that was
        /// visibly growing. A satisfied requirement is not information the player has to act
        /// on, so it is dropped unless ShowMetRequirements asks for the full picture.
        /// </summary>
        private static string FormatRequirements(GrowthReader.PlantInfo info)
        {
            var parts = new StringBuilder();

            foreach (var entry in info.NextStageRequirements)
            {
                if (entry.State == Requirements.State.Met && !PlantPeekPlugin.ShowMetRequirements.Value)
                {
                    continue;
                }

                Separate(parts);

                switch (entry.State)
                {
                    case Requirements.State.Met:
                        parts.Append($"<color={MetColour}>{entry.Label}{Tick()}</color>");
                        break;
                    case Requirements.State.Unknown:
                        // Named but never evaluated - checking it would write to the save.
                        parts.Append($"<color={UnknownColour}><i>{entry.Label}?</i></color>");
                        break;
                    default:
                        parts.Append($"<color={UnmetColour}>{entry.Label}{Cross()}</color>");
                        break;
                }
            }

            return parts.ToString();
        }

        private static string FormatFooter(GrowthReader.PlantInfo info)
        {
            var parts = new StringBuilder();

            if (info.DaysSincePlanted > 0)
            {
                parts.Append($"planted {info.DaysSincePlanted}d ago");
            }

            if (info.DaysInStage > 0 && !info.IsFullyGrown)
            {
                Separate(parts);
                parts.Append($"{info.DaysInStage}d at this stage");
            }

            // No "harvested Nx" line. TimesHarvested reads like a lifetime tally and is not
            // one - Regrow() zeroes it every cycle, so it is only ever 0 or 1 and reporting it
            // as a count was simply wrong.
            return parts.ToString();
        }

        // Colour already carries met/unmet. The glyphs are opt-in because the game's font has
        // known gaps - it has no pencil glyph either, which is why Chest Labels draws its own
        // icon - so a tick could land as tofu. Turn them on in the .cfg if they render.
        private static string Tick() => PlantPeekPlugin.UseCheckmarks.Value ? " ✓" : string.Empty;

        private static string Cross() => PlantPeekPlugin.UseCheckmarks.Value ? " ✗" : string.Empty;

        private static void Separate(StringBuilder builder)
        {
            if (builder.Length > 0)
            {
                builder.Append(" · ");
            }
        }
    }
}
