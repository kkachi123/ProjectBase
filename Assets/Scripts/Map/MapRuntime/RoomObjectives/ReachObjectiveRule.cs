namespace ProjectRE
{
    using System.Collections.Generic;
    using UnityEngine;

    [CreateAssetMenu(menuName = "Map/Objectives/Reach")]
    public sealed class ReachObjectiveRule : RoomObjectiveRule
    {
        public override bool CanApply(RoomObjectiveCandidates candidates) => candidates.HasTrigger(RoomTriggerObjective.TriggerMode.Reach);
        public override void CreateObjectives(RoomObjectiveCandidates candidates, List<RoomObjective> result) =>
            candidates.AddTriggers(RoomTriggerObjective.TriggerMode.Reach, result);
    }
}
