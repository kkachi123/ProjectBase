namespace ProjectRE
{
    using UnityEngine;

    /// <summary>수평 정지·방향 정렬 후 Animation Event 단발 근접 공격.</summary>
    public class MonsterMeleeAttack : IMonsterAttackStrategy
    {
        private readonly GroundedMotor2D _motor;
        private readonly MonsterTargetHandler _target;
        private readonly MonsterAnimator _animator;
        private readonly AgentMeleeHitHandler2D _hitHandler;
        private readonly int _animatorAttackType;
        public MonsterAttackDataSO Data { get; }
        public MonsterAttackStatus Status { get; private set; }

        public MonsterMeleeAttack(MonsterAttackDataSO data, int animatorAttackType,
            GroundedMotor2D motor, MonsterTargetHandler target, MonsterAnimator animator, AgentMeleeHitHandler2D hitHandler)
        {
            Data = data;
            _animatorAttackType = animatorAttackType;
            _motor = motor;
            _target = target;
            _animator = animator;
            _hitHandler = hitHandler;
        }

        /// <summary>이동 없이 시선 정렬 후 공격 모션·범위 표시 시작.</summary>
        public void Begin()
        {
            _motor.Move(new Vector2(Mathf.Sign(_target.DeltaX), 0f), 0f);
            _animator.SetGrounded(false);
            _animator.SetMoveSpeed(0f);
            _animator.SetAttackType(_animatorAttackType);
            _animator.SetAttack(true);
            _hitHandler.SetPreviewData(Data.Data);
            Status = MonsterAttackStatus.Attacking;
        }

        public void Execute(float deltaTime) { }

        /// <summary>공격 모션의 타격 Event에서 설정 데이터 적용.</summary>
        public void OnAttackFrame(int intParameter)
        {
            if (Status == MonsterAttackStatus.Attacking)
                _hitHandler.PerformAttack(Data.Data);
        }

        /// <summary>실제 공격 모션 종료 기록.</summary>
        public void OnAnimationEnd()
        {
            if (Status == MonsterAttackStatus.Attacking)
                Status = MonsterAttackStatus.Completed;
        }

        /// <summary>정상 종료·중단 공통 이동·모션·범위 정리.</summary>
        public void End()
        {
            _motor.StopHorizontal();
            _hitHandler.ClearPreview();
            _animator.SetAttack(false);
            _animator.SetAttackType(0);
        }
    }
}
