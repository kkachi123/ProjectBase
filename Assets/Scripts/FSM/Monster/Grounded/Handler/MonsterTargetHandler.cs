namespace ProjectRE
{
    using UnityEngine;
    /// <summary>주입한 단일 Player의 거리·시야·생존·허용 높이 판정.</summary>
    public class MonsterTargetHandler
    {
        private readonly PlayerDetector _detector;
        private readonly Collider2D _bodyCollider;
        private readonly float _maxHeightDifference;
        private Collider2D _cachedCollider;
        private Health _health;

        public Transform Root { get; private set; }
        public bool HasValidTarget { get; private set; }
        public float DeltaX => Root.position.x - _bodyCollider.transform.position.x;

        public MonsterTargetHandler(PlayerDetector detector, Collider2D bodyCollider, float maxHeightDifference)
        {
            _detector = detector;
            _bodyCollider = bodyCollider;
            _maxHeightDifference = maxHeightDifference;
        }

        /// <summary>Player 생존·몸통 참조 주입.</summary>
        public void BindPlayer(Health health, Collider2D bodyCollider)
        {
            Clear();
            _health = health;
            _cachedCollider = bodyCollider;
        }

        /// <summary>거리 밖 Linecast 생략 후 현재 감지·추적 유효 여부 갱신.</summary>
        public void UpdateTarget()
        {
            if (_cachedCollider == null)
            {
                Root = null;
                HasValidTarget = false;
                return;
            }

            bool detected = !_health.IsDead.Value
                && _cachedCollider.enabled && _cachedCollider.gameObject.activeInHierarchy
                && _detector.IsWithinRange(_cachedCollider, out Vector2 origin)
                && _detector.HasLineOfSight(origin, _cachedCollider);
            Root = detected ? _health.transform : null;
            HasValidTarget = detected && Mathf.Abs(_cachedCollider.bounds.min.y - _bodyCollider.bounds.min.y)
                <= _maxHeightDifference;
        }

        /// <summary>선택 결과·주입 참조 전체 해제.</summary>
        public void Clear()
        {
            Root = null;
            HasValidTarget = false;
            _cachedCollider = null;
            _health = null;
        }
    }
}
