namespace ProjectRE
{
    using UnityEngine;

    /// <summary>Player 생존·거리·시야 확인.</summary>
    public class FlyingMonsterTargetHandler
    {
        private readonly PlayerDetector _detector;
        private Health _health;
        private Collider2D _collider;
        public Transform Root { get; private set; }
        public bool HasValidTarget => Root != null;

        public FlyingMonsterTargetHandler(PlayerDetector detector) => _detector = detector;

        // Player 몸통과 생존 정보 연결.
        public void BindPlayer(Health health, Collider2D bodyCollider)
        {
            _health = health;
            _collider = bodyCollider;
            Refresh();
        }

        // 거리 안의 대상만 시야 검사.
        public void Refresh()
        {
            bool detected = _collider != null && !_health.IsDead.Value && _collider.enabled
                && _collider.gameObject.activeInHierarchy
                && _detector.IsWithinRange(_collider, out Vector2 origin)
                && _detector.HasLineOfSight(origin, _collider);
            Root = detected ? _health.transform : null;
        }

        public void Clear()
        {
            Root = null;
            _health = null;
            _collider = null;
        }
    }
}
