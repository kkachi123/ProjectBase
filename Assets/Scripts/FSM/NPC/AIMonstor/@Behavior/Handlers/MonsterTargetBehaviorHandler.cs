namespace ProjectRE
{
    using UnityEngine;

    /// <summary>단일 Player 참조 캐시·생존 판정과 Target Blackboard 관리.</summary>
    public class MonsterTargetBehaviorHandler
    {
        private PlayerDetector _detector;
        private MonsterBehaviorBlackboard _blackboard;

        // 인식 해제 후에도 유지하는 Player 참조
        private Collider2D _cachedCollider;
        private Health _health;

        // 현재 선택 대상
        public Transform Root => BodyCollider != null && _health != null ? _health.transform : null;
        public Collider2D BodyCollider { get; private set; }
        public bool IsAlive => BodyCollider != null && _health != null && !_health.IsDead.Value;

        /// <summary>감지 Component와 개체별 Blackboard 주입.</summary>
        public void Initialize(PlayerDetector detector, MonsterBehaviorBlackboard blackboard)
        {
            _detector = detector;
            _blackboard = blackboard;
            _blackboard.Target = Root != null ? Root.gameObject : null;
        }

        /// <summary>단일 Player 감지·참조 확보·생존과 시야 확인.</summary>
        public void UpdateTarget()
        {
            Collider2D[] targetsInRadius = _detector.FindCandidates(out Vector2 origin);
            Collider2D targetCollider = targetsInRadius.Length > 0 ? targetsInRadius[0] : null;

            if (targetCollider != null && !ReferenceEquals(_cachedCollider, targetCollider))
            {
                _cachedCollider = targetCollider;
                targetCollider.TryGetComponent(out _health);
            }

            bool canTarget = targetCollider != null && _health != null && !_health.IsDead.Value
                && _detector.HasLineOfSight(origin, targetCollider);
            SetTarget(canTarget ? targetCollider : null);
        }

        /// <summary>선택 대상 변경 시 참조와 Blackboard만 갱신. Player 캐시 유지.</summary>
        private void SetTarget(Collider2D targetCollider)
        {
            if (ReferenceEquals(BodyCollider, targetCollider))
                return;

            BodyCollider = targetCollider;
            _blackboard.Target = Root != null ? Root.gameObject : null;
        }

        /// <summary>자기 사망·비활성화 시 선택 대상과 Player 캐시 전체 해제.</summary>
        public void Clear()
        {
            SetTarget(null);
            _cachedCollider = null;
            _health = null;
        }
    }
}
