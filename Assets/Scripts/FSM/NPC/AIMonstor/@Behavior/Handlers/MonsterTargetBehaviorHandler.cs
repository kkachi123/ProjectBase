namespace ProjectRE
{
    using System;
    using UnityEngine;

    /// <summary>주입한 Player의 감지·추적 가능 판정과 대상 Blackboard 관리.</summary>
    [Serializable]
    public class MonsterTargetBehaviorHandler
    {
        [Tooltip("자기·대상 몸통 Collider 바닥 기준 추적 허용 높이 차이.")]
        [Min(0f), SerializeField] private float _maxTargetHeightDifference = 0.75f;

        private PlayerDetector _detector;
        private Collider2D _bodyCollider;
        private MonsterBehaviorBlackboard _blackboard;

        // 인식 해제 후에도 유지하는 Player 참조
        private Collider2D _cachedCollider;
        private Health _health;

        // 현재 선택 대상
        public Transform Root { get; private set; }
        public float DeltaX => Root.position.x - _bodyCollider.transform.position.x;

        /// <summary>감지 Component·자기 몸통·개체별 Blackboard 주입.</summary>
        public void Initialize(PlayerDetector detector, Collider2D bodyCollider, MonsterBehaviorBlackboard blackboard)
        {
            _detector = detector;
            _bodyCollider = bodyCollider;
            _blackboard = blackboard;
        }

        /// <summary>기존 대상 초기화 후 유효한 Player 생존·몸통 참조 주입.</summary>
        public void BindPlayer(Health health, Collider2D bodyCollider)
        {
            Clear();
            if (health == null || bodyCollider == null)
            {
                Debug.LogError("MonsterTargetBehaviorHandler: Player Health 또는 몸통 Collider 누락.");
                return;
            }

            _health = health;
            _cachedCollider = bodyCollider;
        }

        /// <summary>현재 몬스터 시야·대상 생존·허용 높이로 추적 가능 여부 기록.</summary>
        public void UpdateTarget()
        {
            if (_cachedCollider == null)
            {
                Clear();
                return;
            }

            bool isDetected = !_health.IsDead.Value
                && _cachedCollider.enabled && _cachedCollider.gameObject.activeInHierarchy
                && _detector.IsWithinRange(_cachedCollider, out Vector2 origin)
                && _detector.HasLineOfSight(origin, _cachedCollider);
            Root = isDetected ? _health.transform : null;
            _blackboard.HasValidTarget = isDetected && IsWithinHeightRange();
        }

        /// <summary>자기·대상 몸통 바닥의 허용 높이 차이 확인.</summary>
        private bool IsWithinHeightRange()
        {
            return Mathf.Abs(_cachedCollider.bounds.min.y - _bodyCollider.bounds.min.y)
                <= _maxTargetHeightDifference;
        }

        /// <summary>초기화 후 선택 대상·추적 판정·Player 캐시 전체 해제.</summary>
        public void Clear()
        {
            Root = null;
            _blackboard.HasValidTarget = false;
            _cachedCollider = null;
            _health = null;
        }
    }
}
