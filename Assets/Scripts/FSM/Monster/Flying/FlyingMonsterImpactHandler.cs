namespace ProjectRE
{
    using UnityEngine;

    /// <summary>피격 전환 후 적용할 넉백 보관.</summary>
    public class FlyingMonsterImpactHandler : MonoBehaviour, IKnockbackListener
    {
        private FlyingMotor2D _motor;
        private float _force;
        private Vector2 _direction;
        private bool _pending;

        public void Initialize(FlyingMotor2D motor, float force)
        {
            _motor = motor;
            _force = force;
        }

        public void HandleKnockback(Vector2 direction)
        {
            _direction = (direction + Vector2.up * 0.5f).normalized;
            _pending = true;
        }

        // 이전 State 이동 정리 이후 넉백 적용.
        public void ApplyPending()
        {
            if (!_pending) return;
            _pending = false;
            _motor.Knockback(_direction, _force);
        }

        public void Clear() => _pending = false;
    }
}
