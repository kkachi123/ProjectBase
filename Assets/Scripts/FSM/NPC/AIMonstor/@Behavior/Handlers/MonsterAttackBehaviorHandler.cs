namespace ProjectRE
{
    using System;
    using UnityEngine;

    /// <summary>공격·배후 접근 설정과 수평 공격 거리 판정 관리.</summary>
    [Serializable]
    public class MonsterAttackBehaviorHandler
    {
        [Min(0.01f), SerializeField] private float _attackDistance = 1.8f;
        [Min(0.01f), SerializeField] private float _attackRequestInterval = 1f;
        [Min(0.01f), SerializeField] private float _rearApproachTimeout = 1.5f;

        public float AttackDistance => _attackDistance;
        public float AttackRequestInterval => _attackRequestInterval;
        public float RearApproachTimeout => _rearApproachTimeout;

        /// <summary>공격 요청 간격·배후 접근 시간 설정 전달.</summary>
        public void Initialize(MonsterBehaviorBlackboard blackboard)
        {
            blackboard.AttackRequestInterval = _attackRequestInterval;
            blackboard.RearApproachTimeout = _rearApproachTimeout;
        }

        /// <summary>수평 공격 거리 내 대상 확인.</summary>
        public bool IsInAttackRange(float deltaX) => Mathf.Abs(deltaX) <= _attackDistance;
    }
}
