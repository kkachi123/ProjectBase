namespace ProjectRE
{
    using UnityEngine;

    /// <summary>지상 콤보 단계별 수치와 입력 구독·타격 실행.</summary>
    [CreateAssetMenu(fileName = "PlayerGroundComboAttack", menuName = "ProjectRE/Player/Attack/Ground Combo")]
    public class PlayerGroundComboAttackSO : PlayerAttackSO
    {
        private GroundedMotor2D _motor;
        private IAgentCombatInput _combatInput;
        private IComboAnimation _animator;
        private AgentMeleeHitHandler2D _hitHandler;

        public override void Initialize(GroundedMotor2D motor, IAgentCombatInput combatInput,
            IComboAnimation animator, AgentMeleeHitHandler2D hitHandler)
        {
            _motor = motor;
            _combatInput = combatInput;
            _animator = animator;
            _hitHandler = hitHandler;
        }

        /// <summary>수평 이동 정지 및 공격 중 콤보 입력 구독.</summary>
        public override void Begin()
        {
            _motor.StopHorizontal();
            _combatInput.OnAttackRequested += HandleComboInput;
        }

        /// <summary>Animation Event의 유효한 1부터 시작하는 단계로 타격.</summary>
        public override void OnAttackFrame(int attackStep)
        {
            if (attackStep <= 0 || attackStep > Attacks.Count)
                return;
            AttackData data = Attacks[attackStep - 1];
            _hitHandler.SetPreviewData(data);
            _hitHandler.PerformAttack(data);
        }

        /// <summary>콤보 입력·미소비 Trigger·범위 표시 정리.</summary>
        public override void End()
        {
            _combatInput.OnAttackRequested -= HandleComboInput;
            _hitHandler.ClearPreview();
            _animator.ResetComboTrigger();
        }

        // 다음 콤보 수락과 순서는 Animator에서 결정.
        private void HandleComboInput() => _animator.SetComboTrigger();
    }
}
