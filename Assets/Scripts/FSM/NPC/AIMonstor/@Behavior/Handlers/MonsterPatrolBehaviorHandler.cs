namespace ProjectRE
{
    using System;
    using UnityEngine;

    /// <summary>순찰 반경과 정지 시간 설정 보관. 실행·타이머는 Action 담당.</summary>
    [Serializable]
    public class MonsterPatrolBehaviorHandler
    {
        [Min(0f), SerializeField] private float _patrolRadius = 2f;
        [Min(0f), SerializeField] private float _patrolWaitMin = 1f;
        [Min(0f), SerializeField] private float _patrolWaitMax = 2f;

        public float Radius => _patrolRadius;

        /// <summary>기본 WaitRange에 필요한 정지 시간만 Blackboard에 전달.</summary>
        public void Initialize(MonsterBehaviorBlackboard blackboard)
        {
            blackboard.PatrolWaitMin = _patrolWaitMin;
            blackboard.PatrolWaitMax = _patrolWaitMax;
        }
    }
}
