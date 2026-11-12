namespace ProjectRE
{
    using System.Collections.Generic;
    using UnityEngine;

    [CreateAssetMenu(menuName = "Map/Objectives/Random Monster")]
    public sealed class RandomMonsterObjectiveRule : RoomObjectiveRule
    {
        public override bool CanApply(RoomObjectiveCandidates candidates) => candidates.Monsters.Count > 0;
        public override void CreateObjectives(RoomObjectiveCandidates candidates, List<RoomObjective> result) =>
            result.Add(candidates.CreateMonsterObjective(candidates.Monsters[candidates.TargetIndex], true));
    }
}
