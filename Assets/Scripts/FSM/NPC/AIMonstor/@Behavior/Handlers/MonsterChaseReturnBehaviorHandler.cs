namespace ProjectRE
{
    using System;
    using UnityEngine;

    /// <summary>추적·복귀 설정과 초기 위치·교전·복귀 기록 관리.</summary>
    [Serializable]
    public class MonsterChaseReturnBehaviorHandler
    {
        [Min(0.01f), SerializeField] private float _maxChaseDistance = 8f;
        [Min(0.01f), SerializeField] private float _returnArrivalDistance = 0.15f;
        [Min(0f), SerializeField] private float _lostTargetWait = 2f;

        private Transform _owner;
        private bool _isEngaged;
        private bool _isReturning;

        public float HomeX { get; private set; }
        public float ArrivalDistance => _returnArrivalDistance;
        public float LostTargetWait => _lostTargetWait;
        public bool NeedsReturn { get; private set; }

        /// <summary>자기 Transform과 초기 X 좌표 주입.</summary>
        public void Initialize(Transform owner, float homeX)
        {
            _owner = owner;
            HomeX = homeX;
        }

        /// <summary>초기 위치 기준 대상 추적 범위 확인.</summary>
        public bool IsWithinChaseRange(float targetX) => Mathf.Abs(targetX - HomeX) <= _maxChaseDistance;

        /// <summary>복귀 진행 또는 자기 추적 범위 초과 확인. 대상 상실은 대기 후 복귀.</summary>
        public void UpdateState()
        {
            NeedsReturn = _isReturning || Mathf.Abs(_owner.position.x - HomeX) > _maxChaseDistance;
        }

        /// <summary>교전 후 대상 상실 대기 필요 여부 확인.</summary>
        public bool NeedsLostTargetWait(bool hasValidTarget) => _isEngaged && !hasValidTarget && !NeedsReturn;

        /// <summary>일반 순찰과 대상 상실을 구분하는 교전 시작 기록.</summary>
        public void BeginEngagement() => _isEngaged = true;

        /// <summary>복귀 완료 전 재추적 방지.</summary>
        public void BeginReturn()
        {
            _isReturning = true;
            NeedsReturn = true;
        }

        /// <summary>복귀 완료 후 교전·복귀 기록 초기화.</summary>
        public void CompleteReturn()
        {
            _isReturning = false;
            _isEngaged = false;
            NeedsReturn = false;
        }
    }
}
