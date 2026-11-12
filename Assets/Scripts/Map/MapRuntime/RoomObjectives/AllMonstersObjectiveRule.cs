namespace ProjectRE
{
    using System.Collections.Generic;
    using UnityEngine;

    [CreateAssetMenu(menuName = "Map/Objectives/All Monsters")]
    public sealed class AllMonstersObjectiveRule : RoomObjectiveRule
    {
        public override bool CanApply(RoomObjectiveCandidates candidates) => candidates.Monsters.Count > 0;
        public override void CreateObjectives(RoomObjectiveCandidates candidates, List<RoomObjective> result)
        {
            foreach (var monster in candidates.Monsters)
                result.Add(candidates.CreateMonsterObjective(monster, false));
        }
    }
}
