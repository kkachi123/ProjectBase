namespace ProjectRE
{
    using System.Collections.Generic;
    public class PlayerAttackState : AttackState
    {
        private readonly AgentMotor2D _motor;
        private readonly Stamina _stamina;
        private readonly List<AttackData> _attackDatas;
        private readonly ComboAttackHandler _comboHandler;
        private readonly IAttackComboStarter _comboAttackStarter;

        public PlayerAttackState(
            ICombatAnimation animator,
            AgentCombatHandler combatHandler,
            AgentMotor2D motor,
            Stamina stamina,
            AgentStatData statData,
            ComboAttackHandler comboHandler,
            IAttackComboStarter comboAttackStarter)
            : base(animator, combatHandler)
        {
            _motor = motor;
            _comboHandler = comboHandler;
            _stamina = stamina;
            _attackDatas = statData.attackDatas;
            _comboAttackStarter = comboAttackStarter;
        }

        protected override void OnEnter()
        {
            base.OnEnter();
            _comboHandler.BeginAttack();

            if (CombatHandler.CurrentAttackType != 3)
                _motor.StopHorizontal();
        }

        protected override void OnExit()
        {
            base.OnExit();
            _comboHandler.EndAttack();
        }

        protected override void Attack()
        {
            base.Attack();
            _stamina.Use(_attackDatas[CombatHandler.CurrentAttackType - 1].usedStamina);
        }

        // Animation Event 발생 시점에 호출되어 콤보 공격을 이어갈 수 있는지 확인하고, 이어갈 수 있다면 다음 공격으로 전환한다.
        public override bool TryHandleAttackFinished()
        {
            if (!_comboHandler.TryConsumeNextAttack(
                    CombatHandler.CurrentAttackType,
                    out int nextAttackType))
                return false;

            if (!_comboAttackStarter.TryContinueAttack(nextAttackType))
                return false;

            Attack();
            return true;
        }
    }
}
