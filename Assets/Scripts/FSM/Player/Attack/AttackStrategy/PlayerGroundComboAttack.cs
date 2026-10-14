namespace ProjectRE
{
    /// <summary>지상 콤보 입력 연결 및 단계별 근접 타격.</summary>
    public class PlayerGroundComboAttack : IPlayerAttackStrategy
    {
        private readonly PlayerAttackDataSO[] _attacks;
        private readonly GroundedMotor2D _motor;
        private readonly IAgentCombatInput _combatInput;
        private readonly IComboAnimation _animator;
        private readonly AgentMeleeHitHandler2D _hitHandler;
        public PlayerAttackDataSO StartData => _attacks[0];

        public PlayerGroundComboAttack(PlayerAttackDataSO[] attacks, GroundedMotor2D motor,
            IAgentCombatInput combatInput, IComboAnimation animator, AgentMeleeHitHandler2D hitHandler)
        {
            _attacks = attacks;
            _motor = motor;
            _combatInput = combatInput;
            _animator = animator;
            _hitHandler = hitHandler;
        }

        /// <summary>수평 이동 정지 및 공격 중 콤보 입력 구독.</summary>
        public void Begin()
        {
            _motor.StopHorizontal();
            _combatInput.OnAttackRequested += HandleComboInput;
        }

        /// <summary>Animation Event 단계의 데이터로 타격. 유효한 1부터 시작하는 단계만 처리.</summary>
        public void OnAttackFrame(int attackStep)
        {
            if(attackStep <= 0 || attackStep > _attacks.Length)
                return;
            AttackData data = _attacks[attackStep - 1].Data;
            _hitHandler.SetPreviewData(data);
            _hitHandler.PerformAttack(data);
        }

        /// <summary>콤보 입력 해제 및 다음 공격에 남을 Trigger 초기화.</summary>
        public void End()
        {
            _combatInput.OnAttackRequested -= HandleComboInput;
            _hitHandler.ClearPreview();
            _animator.ResetComboTrigger();
        }

        // 다음 콤보 수락 여부와 순서는 Animator에서 결정.
        private void HandleComboInput() => _animator.SetComboTrigger();
    }
}
