namespace ProjectRE
{
    using System.Collections.Generic;
    using UnityEngine;

    public interface IRoomObjectiveRule
    {
        bool CanApply(RoomObjectiveCandidates candidates);
        void CreateObjectives(RoomObjectiveCandidates candidates, List<RoomObjective> result);
    }

    /// <summary>공유 설정만 보관하며, 목표 상태는 생성된 방에 둔다.</summary>
    public abstract class RoomObjectiveRule : ScriptableObject, IRoomObjectiveRule
    {
        public abstract bool CanApply(RoomObjectiveCandidates candidates);
        public abstract void CreateObjectives(RoomObjectiveCandidates candidates, List<RoomObjective> result);
    }

    public sealed class RoomObjectiveCandidates
    {
        public IReadOnlyList<Health> Monsters { get; }
        public IReadOnlyList<RoomTriggerObjective> Triggers { get; }
        private readonly GameObject owner;
        private readonly ProjectRE.MapGeneration.MapBuildContext context;
        private readonly int targetIndex;

        public RoomObjectiveCandidates(IReadOnlyList<Health> monsters, IReadOnlyList<RoomTriggerObjective> triggers,
            GameObject owner, ProjectRE.MapGeneration.MapBuildContext context, int targetIndex)
        {
            Monsters = monsters;
            Triggers = triggers;
            this.owner = owner;
            this.context = context;
            this.targetIndex = targetIndex;
        }

        public int TargetIndex => Monsters.Count == 0 ? 0 : targetIndex % Monsters.Count;

        public RoomMonsterObjective CreateMonsterObjective(Health target, bool markTarget)
        {
            var objective = (RoomMonsterObjective)context.AddComponent(owner, typeof(RoomMonsterObjective));
            context.RecordComponent(objective);
            objective.SetDescription(markTarget ? "지정 몬스터 처치" : "몬스터 처치");
            objective.SetTarget(target);
            if (markTarget) objective.CreateTargetMarker(context);
            return objective;
        }

        public bool HasTrigger(RoomTriggerObjective.TriggerMode mode)
        {
            foreach (var trigger in Triggers)
                if (trigger.Mode == mode) return true;
            return false;
        }

        public void AddTriggers(RoomTriggerObjective.TriggerMode mode, List<RoomObjective> result)
        {
            foreach (var trigger in Triggers)
                if (trigger.Mode == mode)
                {
                    context.SetActive(trigger.gameObject, true);
                    result.Add(trigger);
                }
        }
    }
}
