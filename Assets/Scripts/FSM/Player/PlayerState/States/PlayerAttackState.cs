namespace ProjectRE
{
    using System.Collections.Generic;
    public class PlayerAttackState : AttackState
    {
        private readonly AgentMotor2D _motor;
        private readonly Stamina _stamina;
        private readonly List<AttackData> _attackDatas;
        private readonly IComboAnimation _comboAnimator;

        private IAgentCombatInput _combatInput;
        public PlayerAttackState(
            IComboAnimation comboAnimator,
            AgentCombatHandler combatHandler,
            AgentMotor2D motor,
            Stamina stamina,
            AgentStatData statData,
            IAgentCombatInput combatInput
            )
            : base(comboAnimator, combatHandler)
        {
            _motor = motor;
            _stamina = stamina;
            _attackDatas = statData.attackDatas;
            _comboAnimator = comboAnimator;
            _combatInput = combatInput;
        }

        protected override void OnEnter()
        {
            _combatInput.OnAttackRequested += HandleComboInput;
            base.OnEnter();

            if (CombatHandler.CurrentAttackType != 3)
                _motor.StopHorizontal();
        }

        protected override void OnExit()
        {
            _combatInput.OnAttackRequested -= HandleComboInput;
            // 마지막 지상 3타나 공중 공격 중 소비되지 않은 Trigger가 다음 Attack에 남지 않게 한다.
            _comboAnimator.ResetComboTrigger();
            base.OnExit();
        }


        protected override void Attack()
        {
            base.Attack();
            _stamina.Use(_attackDatas[CombatHandler.CurrentAttackType - 1].usedStamina);
        }

        private void HandleComboInput()
        {
            _comboAnimator.SetComboTrigger();
        }
    }
}
