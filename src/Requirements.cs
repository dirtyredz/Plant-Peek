using System.Collections.Generic;
using System.Text;

namespace PlantPeek
{
    /// <summary>
    /// Turns a stage's exit conditions into readable lines.
    ///
    /// ⚠️ <b>Evaluation is an allowlist, not a blocklist.</b> Half of the requirement types in
    /// this game have side effects in the method that looks like a query:
    ///
    /// <list type="bullet">
    /// <item>Six call <c>GuidPersistenceList.FindOrCreate</c>, which <b>inserts a record into
    /// the save</b> when the plant does not have one.</item>
    /// <item><c>MusicPlayedStageRequirement</c> goes further and rewrites its own persistence -
    /// appending heard-days and removing transmitters - every time it is asked.</item>
    /// <item><c>RandomChanceGrowStageRequirement</c> consumes Unity's global RNG.</item>
    /// </list>
    ///
    /// At a 12 Hz hover poll any of those is unacceptable, and the first two would make the
    /// "nothing is written to your save" claim untrue. So only types verified to be pure reads
    /// are ever called; anything else is named but left unevaluated, and anything new added by
    /// a game update is unevaluated by default rather than trusted.
    ///
    /// Full audit in research/01-growth-system.md.
    /// </summary>
    internal static class Requirements
    {
        internal enum State
        {
            Met,
            Unmet,
            /// <summary>Cannot be checked without touching the save, so it is not checked.</summary>
            Unknown,
        }

        internal struct Entry
        {
            internal string Label;
            internal State State;

            /// <summary>
            /// Whether this is purely a matter of waiting (or of watering, which the player is
            /// doing anyway). False for anything that can stall a plant indefinitely, which is
            /// what makes a day estimate a lie.
            /// </summary>
            internal bool IsMerelyTime;
        }

        /// <summary>
        /// Verified side-effect free: no FindOrCreate, no collection or field writes, no RNG.
        /// Adding a type here requires reading its IsRequirementCompleted, not assuming.
        /// </summary>
        private static bool CanEvaluateSafely(IGrowStageRequirement requirement)
        {
            // Checked first: it derives from CropsNear but calls FindOrCreate.
            if (requirement is WeepingWiccaGrowStageRequirement)
            {
                return false;
            }

            return requirement is WaterGrowStageRequirement
                || requirement is SeasonGrowStageRequirement
                || requirement is NearWaterGrowStageRequirement
                || requirement is CropsNearGrowStageRequirement
                || requirement is FootprintGrowStageRequirement
                || requirement is WildTreeGrowStageRequirement;
        }

        internal static List<Entry> Read(
            GrowStage stage,
            GridObjectPersistence gridPersistence,
            GrowablePersistence growablePersistence,
            GrowableItemAddon addon)
        {
            List<Entry> best = null;
            var bestScore = int.MaxValue;

            foreach (var path in stage.GrowPaths)
            {
                if (path == null)
                {
                    continue;
                }

                // A stage's paths are alternative routes, not a combined condition, and some of
                // them are not growth at all - spreading and replacement paths hang off the
                // same stage. A path that leads somewhere new is the one the player means by
                // "what is it waiting for"; ranking those first stops a spread path's blocked
                // footprint being reported as the reason a healthy plant is not growing.
                var advances = path.TargetGrowStage != null && path.TargetGrowStage != stage;

                var entries = ReadPath(path, gridPersistence, growablePersistence, addon);

                var outstanding = 0;
                foreach (var entry in entries)
                {
                    if (entry.State != State.Met)
                    {
                        outstanding++;
                    }
                }

                var score = (advances ? 0 : 1000) + outstanding;
                if (score < bestScore)
                {
                    bestScore = score;
                    best = entries;
                }
            }

            return best ?? new List<Entry>();
        }

        private static List<Entry> ReadPath(
            GrowPath path,
            GridObjectPersistence gridPersistence,
            GrowablePersistence growablePersistence,
            GrowableItemAddon addon)
        {
            var entries = new List<Entry>();

            foreach (var requirement in path.GetComponents<IGrowStageRequirement>())
            {
                if (requirement == null)
                {
                    continue;
                }

                // Always true and never actionable - it would only ever render as something
                // the player can do nothing with.
                if (requirement is AutoGrowStageRequirement)
                {
                    continue;
                }

                // Reported as chopping progress on the status line instead - see
                // GrowthReader.ReadChoppedPercent. "waiting on chopping" described a tree the
                // player is felling as though it were stuck.
                if (requirement is DamageTakenRequirement)
                {
                    continue;
                }

                if (!CanEvaluateSafely(requirement))
                {
                    entries.Add(new Entry
                    {
                        Label = Describe(requirement, addon),
                        State = State.Unknown,
                        IsMerelyTime = false,
                    });
                    continue;
                }

                bool met;
                try
                {
                    met = requirement.IsRequirementCompleted(gridPersistence, growablePersistence);
                }
                catch
                {
                    // A requirement that cannot answer right now is not worth losing the
                    // whole panel over.
                    continue;
                }

                entries.Add(new Entry
                {
                    Label = Describe(requirement, addon),
                    State = met ? State.Met : State.Unmet,
                    IsMerelyTime = IsMerelyTime(requirement),
                });
            }

            return entries;
        }

        /// <summary>
        /// Water is the one outstanding requirement that resolves by the player simply doing
        /// the thing they already do each morning, so it does not invalidate a day estimate.
        /// Everything else here can stall a plant for an arbitrary length of time.
        /// </summary>
        private static bool IsMerelyTime(IGrowStageRequirement requirement)
        {
            return requirement is WaterGrowStageRequirement;
        }

        private static string Describe(IGrowStageRequirement requirement, GrowableItemAddon addon)
        {
            switch (requirement)
            {
                case WaterGrowStageRequirement _:
                    return "water";
                case SeasonGrowStageRequirement _:
                    return DescribeSeasons(addon);
                case NearWaterGrowStageRequirement _:
                    return "water nearby";
                case PlantDrankGrowStageRequirement _:
                    return "a drink";
                case PlantFedGrowStageRequirement _:
                    return "feeding";
                case PlantPettedGrowStageRequirement _:
                    return "petting";
                // Checked before its CropsNear base, which it derives from.
                case WeepingWiccaGrowStageRequirement _:
                    return "sadness";
                case CropsNearGrowStageRequirement _:
                    return "the right neighbours";
                // Checks that the *next* stage's footprint cells are clear of other objects.
                // "room to spread" read as a complaint about a plant that was growing fine;
                // this says which space and why.
                case FootprintGrowStageRequirement _:
                    return "clear space to grow into";
                case WildTreeGrowStageRequirement _:
                    return "growing wild";
                case DamageTakenRequirement _:
                    return "chopping";
                case MusicPlayedStageRequirement _:
                    return "music nearby";
                default:
                    return requirement.GetType().Name
                        .Replace("GrowStageRequirement", string.Empty)
                        .Replace("StageRequirement", string.Empty)
                        .Replace("Requirement", string.Empty);
            }
        }

        /// <summary>
        /// "summer" reads far better than "season". GrowingSeasons is empty for crops that
        /// grow year-round, in which case the requirement passes unconditionally.
        /// </summary>
        private static string DescribeSeasons(GrowableItemAddon addon)
        {
            var seasons = addon?.GrowingSeasons;
            if (seasons == null || seasons.Count == 0)
            {
                return "the right season";
            }

            var builder = new StringBuilder();
            foreach (var season in seasons)
            {
                var name = season?.LocalizedName?.GetTranslation();
                if (string.IsNullOrWhiteSpace(name))
                {
                    continue;
                }

                if (builder.Length > 0)
                {
                    builder.Append("/");
                }

                builder.Append(name.ToLower());
            }

            return builder.Length > 0 ? builder.ToString() : "the right season";
        }
    }
}
