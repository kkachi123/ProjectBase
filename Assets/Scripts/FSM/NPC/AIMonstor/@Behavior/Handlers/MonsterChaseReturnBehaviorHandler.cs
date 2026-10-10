namespace ProjectRE
{
    using System;
    using UnityEngine;

    /// <summary>최초 복귀 위치·도착 범위와 교전 이력 기반 대상 상실 관리.</summary>
    [Serializable]
    public class MonsterChaseReturnBehaviorHandler
    {
        [Min(0.01f), SerializeField] private float _returnArrivalDistance = 0.15f;
        [Min(0f), SerializeField] private float _lostTargetWait = 2f;

        private MonsterBehaviorBlackboard _blackboard;
        private bool _isEngaged;

        public float HomeX { get; private set; }
        public float ArrivalDistance => _returnArrivalDistance;

        /// <summary>순찰·복귀 기준 초기 X 주입 및 복귀 설정 전달.</summary>
        public void Initialize(float homeX, MonsterBehaviorBlackboard blackboard)
        {
            _blackboard = blackboard;
            HomeX = homeX;
            _blackboard.LostTargetWait = _lostTargetWait;
            _blackboard.LostTarget = false;
        }

        /// <summary>교전 이후 대상 상실만 기록. 대기·복귀 단계는 Graph 담당.</summary>
        public void UpdateState() => _blackboard.LostTarget = _isEngaged && !_blackboard.HasValidTarget;

        /// <summary>일반 순찰과 대상 상실을 구분하는 교전 시작 기록.</summary>
        public void BeginEngagement() => _isEngaged = true;

        /// <summary>복귀 완료 후 교전 이력·대상 상실 초기화.</summary>
        public void CompleteReturn()
        {
            _isEngaged = false;
            _blackboard.LostTarget = false;
        }
    }
}
