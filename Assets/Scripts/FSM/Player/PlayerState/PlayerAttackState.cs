namespace ProjectRE
{
    public class PlayerAttackState : AttackState
    {
        private readonly AgentMotor2D _motor;
        private readonly IAgentCombatInput _combatInput;
        private readonly ComboAttackHandler _comboHandler;

        public PlayerAttackState(
            AgentMotor2D motor,
            ICombatAnimation animator,
            AgentCombatHandler combatHandler,
            IAgentCombatInput combatInput,
            ComboAttackHandler comboHandler)
            : base(animator, combatHandler)
        {
            _motor = motor;
            _combatInput = combatInput;
            _comboHandler = comboHandler;
        }

        protected override void OnEnter()
        {
            base.OnEnter();

            if (CombatHandler.CurrentAttackType != 3)
                _motor.StopHorizontal();
        }

        protected override void OnExecute(float deltaTime)
        {
            _comboHandler.CaptureBufferedRequests(_combatInput);
        }

        protected override void OnExit()
        {
            base.OnExit();
            _comboHandler.ResetGroundCombo();
        }
    }
}
