namespace ProjectRE
{
    using System;
    using UnityEngine;

    /// <summary>공격·배후 접근 설정과 대상 거리·높이 판정 관리.</summary>
    [Serializable]
    public class MonsterAttackBehaviorHandler
    {
        [Min(0.01f), SerializeField] private float _attackDistance = 1.8f;
        [Tooltip("몸통 Collider 바닥 기준 추적·공격 허용 높이 차이.")]
        [Min(0f), SerializeField] private float _maxAttackHeightDifference = 0.75f;
        [Min(0.01f), SerializeField] private float _attackRequestInterval = 1f;
        [Min(0.01f), SerializeField] private float _rearApproachTimeout = 1.5f;

        private Collider2D _bodyCollider;

        public float AttackDistance => _attackDistance;
        public float AttackRequestInterval => _attackRequestInterval;
        public float RearApproachTimeout => _rearApproachTimeout;

        /// <summary>자기 몸통 Collider 주입 및 공격 시간 설정 전달.</summary>
        public void Initialize(Collider2D bodyCollider, MonsterBehaviorBlackboard blackboard)
        {
            _bodyCollider = bodyCollider;
            blackboard.AttackRequestInterval = _attackRequestInterval;
            blackboard.RearApproachTimeout = _rearApproachTimeout;
        }

        /// <summary>수평 공격 거리 내 대상 확인.</summary>
        public bool IsInAttackRange(float deltaX) => Mathf.Abs(deltaX) <= _attackDistance;

        /// <summary>몸통 바닥 기준 대상 높이 차이 확인.</summary>
        public bool IsWithinHeightRange(Collider2D targetBodyCollider)
        {
            return Mathf.Abs(targetBodyCollider.bounds.min.y - _bodyCollider.bounds.min.y)
                <= _maxAttackHeightDifference;
        }
    }
}
