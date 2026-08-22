namespace PlantPeek
{
    /// <summary>
    /// One place that answers "is this grow path the plant <i>growing</i>?" — as opposed to being
    /// destroyed (a damage/chop path to a stump) or spreading in place.
    ///
    /// This used to be decided independently in two files with two different rules:
    /// <see cref="StageGraph"/> excluded damage paths, while <see cref="Requirements"/> treated any
    /// non-self target as advancing. They disagreed on exactly the plants that have both a growth
    /// path and a chop path from one stage (trees): because <see cref="Requirements.ReadPath"/>
    /// strips the <c>DamageTakenRequirement</c> off a chop path, that path scored as a
    /// zero-outstanding "advance" and could be preferred over the real growth path — blanking or
    /// mis-reporting the "waiting on" line. Both now share this classifier so the definition of
    /// "growth" cannot drift again.
    /// </summary>
    internal static class GrowthPaths
    {
        /// <summary>
        /// A path is growth when it leads to a different stage and is not gated on damage. A path
        /// gated on <c>DamageTakenRequirement</c> is the player chopping the plant down - the stage
        /// it leads to is a stump, not a further stage of the plant.
        /// </summary>
        internal static bool IsGrowthTransition(GrowPath path, GrowStage from)
        {
            if (path == null || path.TargetGrowStage == null || path.TargetGrowStage == from)
            {
                return false;
            }

            return path.GetComponent<DamageTakenRequirement>() == null;
        }
    }
}
