namespace ProjectRE
{
    using UnityEngine;

    /// <summary>대상 배후 접근 후 상위 근접 공격 실행 재사용.</summary>
    [CreateAssetMenu(fileName = "MonsterRearMeleeAttack", menuName = "ProjectRE/Monster/Attack/Rear Melee")]
    public class MonsterRearMeleeAttackSO : MonsterMeleeAttackSO
    {
        public float RearApproachTimeout = 1.5f;
        private GroundedMovementHandler2D _movement;
        private float _arrivalDistance;
        private float _targetFacing;
        private float _endTime;

        public override void Initialize(GroundedMotor2D motor, GroundedMovementHandler2D movement,
            MonsterTargetHandler target, MonsterAnimator animator, AgentMeleeHitHandler2D hitHandler,
            float arrivalDistance, int animatorAttackType)
        {
            base.Initialize(motor, movement, target, animator, hitHandler, arrivalDistance, animatorAttackType);
            _movement = movement;
            _arrivalDistance = arrivalDistance;
        }

        /// <summary>대상 방향·제한 시각 고정. 실제 공격 모션은 보류.</summary>
        public override void Begin()
        {
            _targetFacing = Mathf.Sign(Target.Root.localScale.x);
            _endTime = Time.time + RearApproachTimeout;
            Animator.SetGrounded(true);
            Status = MonsterAttackStatus.Preparing;
        }

        /// <summary>배후 도착 시 근접 공격 시작. 상실·시간 초과는 전이용 결과 기록.</summary>
        public override void Execute(float deltaTime)
        {
            if (Status != MonsterAttackStatus.Preparing)
                return;
            if (!Target.HasValidTarget)
            {
                Status = MonsterAttackStatus.TargetLost;
                _movement.Stop();
                Animator.SetMoveSpeed(0f);
                return;
            }

            float deltaX = Target.DeltaX - _targetFacing * AttackDistance * 0.8f;
            if (Mathf.Abs(deltaX) <= _arrivalDistance)
            {
                base.Begin();
                return;
            }
            if (Time.time >= _endTime)
            {
                Status = MonsterAttackStatus.Failed;
                _movement.Stop();
                Animator.SetMoveSpeed(0f);
                return;
            }

            _movement.HandleMove(new Vector2(Mathf.Sign(deltaX), 0f));
            Animator.SetMoveSpeed(1f);
        }

        /// <summary>준비·공격 어느 단계에서도 공통 실행·이동 표현 정리.</summary>
        public override void End()
        {
            base.End();
            Animator.SetGrounded(false);
            Animator.SetMoveSpeed(0f);
        }
    }
}
