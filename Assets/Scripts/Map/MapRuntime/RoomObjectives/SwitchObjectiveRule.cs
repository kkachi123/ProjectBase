namespace ProjectRE
{
    using System.Collections.Generic;
    using UnityEngine;

    [CreateAssetMenu(menuName = "Map/Objectives/Switch")]
    public sealed class SwitchObjectiveRule : RoomObjectiveRule
    {
        public override bool CanApply(RoomObjectiveCandidates candidates) => candidates.HasTrigger(RoomTriggerObjective.TriggerMode.Interact);
        public override void CreateObjectives(RoomObjectiveCandidates candidates, List<RoomObjective> result) =>
            candidates.AddTriggers(RoomTriggerObjective.TriggerMode.Interact, result);
    }
}
