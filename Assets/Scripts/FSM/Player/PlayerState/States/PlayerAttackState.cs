namespace ProjectRE
{
    public class PlayerAttackState : AttackState
    {
        private readonly AgentMotor2D _motor;
        private readonly ComboAttackHandler _comboHandler;

        public PlayerAttackState(
            AgentMotor2D motor,
            ICombatAnimation animator,
            AgentCombatHandler combatHandler,
            ComboAttackHandler comboHandler)
            : base(animator, combatHandler)
        {
            _motor = motor;
            _comboHandler = comboHandler;
        }

        protected override void OnEnter()
        {
            base.OnEnter();
            _comboHandler.ActivateAttack();

            if (CombatHandler.CurrentAttackType != 3)
                _motor.StopHorizontal();
        }

        protected override void OnExit()
        {
            base.OnExit();
            _comboHandler.Reset();
        }
    }
}
