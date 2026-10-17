namespace ProjectRE
{
    using UnityEngine;

    /// <summary>대상 배후 접근 후 일반 근접 공격 재사용.</summary>
    public class MonsterRearMeleeAttack : IMonsterAttackStrategy
    {
        private readonly MonsterMeleeAttack _melee;
        private readonly MonsterTargetHandler _target;
        private readonly GroundedMovementHandler2D _movement;
        private readonly MonsterAnimator _animator;
        private readonly float _arrivalDistance;
        private float _targetFacing;
        private float _endTime;
        public MonsterAttackDataSO Data => _melee.Data;
        public MonsterAttackStatus Status { get; private set; }

        public MonsterRearMeleeAttack(MonsterMeleeAttack melee, MonsterTargetHandler target,
            GroundedMovementHandler2D movement, MonsterAnimator animator, float arrivalDistance)
        {
            _melee = melee;
            _target = target;
            _movement = movement;
            _animator = animator;
            _arrivalDistance = arrivalDistance;
        }

        /// <summary>접근 시작 시 대상 방향·제한 시각 고정. 공격 모션은 보류.</summary>
        public void Begin()
        {
            _targetFacing = Mathf.Sign(_target.Root.localScale.x);
            _endTime = Time.time + Data.RearApproachTimeout;
            _animator.SetGrounded(true);
            Status = MonsterAttackStatus.Preparing;
        }

        /// <summary>배후 목적지 접근. 대상 상실·시간 초과는 전이용 결과만 기록.</summary>
        public void Execute(float deltaTime)
        {
            if (Status != MonsterAttackStatus.Preparing)
                return;
            if (!_target.HasValidTarget)
            {
                Status = MonsterAttackStatus.TargetLost;
                _movement.Stop();
                _animator.SetMoveSpeed(0f);
                return;
            }

            float deltaX = _target.DeltaX - _targetFacing * Data.AttackDistance * 0.8f;
            if (Mathf.Abs(deltaX) <= _arrivalDistance)
            {
                _melee.Begin();
                Status = MonsterAttackStatus.Attacking;
                return;
            }
            if (Time.time >= _endTime)
            {
                Status = MonsterAttackStatus.Failed;
                _movement.Stop();
                _animator.SetMoveSpeed(0f);
                return;
            }

            _movement.HandleMove(new Vector2(Mathf.Sign(deltaX), 0f));
            _animator.SetMoveSpeed(1f);
        }

        public void OnAttackFrame(int intParameter)
        {
            if (Status == MonsterAttackStatus.Attacking)
                _melee.OnAttackFrame(intParameter);
        }

        /// <summary>준비 중의 종료 Event 무시. 실제 공격 완료만 기록.</summary>
        public void OnAnimationEnd()
        {
            if (Status != MonsterAttackStatus.Attacking)
                return;
            _melee.OnAnimationEnd();
            Status = _melee.Status;
        }

        /// <summary>접근·공격 어느 단계에서도 이동·표현 정리.</summary>
        public void End()
        {
            _melee.End();
            _animator.SetGrounded(false);
            _animator.SetMoveSpeed(0f);
        }
    }
}
