namespace ProjectRE
{
    using System;
    using UnityEngine;

    /// <summary>공격 거리·배후 접근 설정 및 개체별 공격 요청 간격 관리.</summary>
    [Serializable]
    public class MonsterAttackBehaviorHandler
    {
        [Min(0.01f), SerializeField] private float _attackDistance = 1.8f;
        [Min(0.01f), SerializeField] private float _attackRequestInterval = 1f;
        [Min(0.01f), SerializeField] private float _rearApproachTimeout = 1.5f;

        private float _nextAttackRequestTime;

        public float AttackDistance => _attackDistance;
        public float RearApproachTimeout => _rearApproachTimeout;

        /// <summary>수평 공격 거리 도착 확인.</summary>
        public bool IsInAttackRange(float deltaX) => Mathf.Abs(deltaX) <= _attackDistance;

        /// <summary>이전 요청 이후 최소 간격 경과 확인.</summary>
        public bool CanRequestAttack() => Time.time >= _nextAttackRequestTime;

        /// <summary>다음 요청 가능 시각 기록. 행동 재진입 시 유지.</summary>
        public void RecordAttackRequest() => _nextAttackRequestTime = Time.time + _attackRequestInterval;
    }
}
