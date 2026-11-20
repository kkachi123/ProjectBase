namespace ProjectRE
{
    using UnityEngine;

    /// <summary>방향 정렬·수평 정지 후 Animation Event 단발 근접 공격.</summary>
    [CreateAssetMenu(fileName = "MonsterMeleeAttack", menuName = "ProjectRE/Monster/Attack/Melee")]
    public class MonsterMeleeAttackSO : MonsterAttackSO
    {
        protected MonsterTargetHandler Target;
        protected MonsterAnimator Animator;
        private GroundedMotor2D _motor;
        private AgentMeleeHitHandler2D _hitHandler;
        private int _animatorAttackType;

        public override void Initialize(GroundedMotor2D motor, GroundedMovementHandler2D movement,
            MonsterTargetHandler target, MonsterAnimator animator, AgentMeleeHitHandler2D hitHandler,
            float arrivalDistance, int animatorAttackType)
        {
            _motor = motor;
            Target = target;
            Animator = animator;
            _hitHandler = hitHandler;
            _animatorAttackType = animatorAttackType;
        }

        /// <summary>이동 없이 방향 정렬 후 공격 모션·범위 표시 시작.</summary>
        public override void Begin()
        {
            _motor.Move(new Vector2(Mathf.Sign(Target.DeltaX), 0f), 0f);
            Animator.SetGrounded(false);
            Animator.SetMoveSpeed(0f);
            Animator.SetAttackType(_animatorAttackType);
            Animator.SetAttack(true);
            _hitHandler.SetPreviewData(Data);
            Status = MonsterAttackStatus.Attacking;
        }

        public override void Execute(float deltaTime) { }

        /// <summary>공격 모션의 타격 Event에서 설정 데이터 적용.</summary>
        public override void OnAttackFrame(int intParameter)
        {
            if (Status == MonsterAttackStatus.Attacking)
                _hitHandler.PerformAttack(Data);
        }

        /// <summary>준비 중 Event는 무시하고 실제 공격 모션 종료만 기록.</summary>
        public override void OnAnimationEnd()
        {
            if (Status == MonsterAttackStatus.Attacking)
                Status = MonsterAttackStatus.Completed;
        }

        /// <summary>정상 종료·중단 공통 이동·모션·범위 정리.</summary>
        public override void End()
        {
            _motor.StopHorizontal();
            _hitHandler.ClearPreview();
            Animator.SetAttack(false);
            Animator.SetAttackType(0);
        }
    }
}
