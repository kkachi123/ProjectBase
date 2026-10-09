namespace ProjectRE
{
    using System;
    using UnityEngine;

    /// <summary>추적·복귀 설정과 초기 위치·교전·복귀 기록 관리.</summary>
    [Serializable]
    public class MonsterChaseReturnBehaviorHandler
    {
        [Min(0.01f), SerializeField] private float _returnArrivalDistance = 0.15f;
        [Min(0f), SerializeField] private float _lostTargetWait = 2f;

        private MonsterBehaviorBlackboard _blackboard;
        private bool _isEngaged;
        private bool _isReturning;

        public float HomeX { get; private set; }
        public float ArrivalDistance => _returnArrivalDistance;
        public float LostTargetWait => _lostTargetWait;
        public bool NeedsReturn { get; private set; }
        public bool NeedsLostTargetWait { get; private set; }

        /// <summary>순찰·복귀 기준 초기 X 주입 및 복귀 설정 전달.</summary>
        public void Initialize(float homeX, MonsterBehaviorBlackboard blackboard)
        {
            HomeX = homeX;
            _blackboard = blackboard;
            _blackboard.HomeX = homeX;
            _blackboard.LostTargetWait = _lostTargetWait;
        }

        /// <summary>복귀 진행 유지. 교전 대상의 시야·유효성 상실은 대기 후 복귀.</summary>
        public void UpdateState(bool hasValidTarget)
        {
            NeedsReturn = _isReturning;
            NeedsLostTargetWait = _isEngaged && !hasValidTarget && !NeedsReturn;
            PublishState();
        }

        /// <summary>일반 순찰과 대상 상실을 구분하는 교전 시작 기록.</summary>
        public void BeginEngagement() => _isEngaged = true;

        /// <summary>복귀 완료 전 재추적 방지.</summary>
        public void BeginReturn()
        {
            _isReturning = true;
            NeedsReturn = true;
            NeedsLostTargetWait = false;
            PublishState();
        }

        /// <summary>복귀 완료 후 교전·복귀 기록 초기화.</summary>
        public void CompleteReturn()
        {
            _isReturning = false;
            _isEngaged = false;
            NeedsReturn = false;
            NeedsLostTargetWait = false;
            PublishState();
        }

        /// <summary>복귀·상실 대기 플래그만 같은 프레임에 Blackboard 반영.</summary>
        private void PublishState()
        {
            _blackboard.NeedsReturn = NeedsReturn;
            _blackboard.NeedsLostTargetWait = NeedsLostTargetWait;
        }
    }
}
