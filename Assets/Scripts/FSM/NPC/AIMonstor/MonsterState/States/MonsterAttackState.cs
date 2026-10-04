namespace ProjectRE
{
    /// <summary>공용 단발 공격 실행 및 진입 시 수평 이동 정지.</summary>
    public class MonsterAttackState : AttackState
    {
        private readonly AgentMotor2D _motor;

        public MonsterAttackState(ICombatAnimation animator, AgentCombatHandler combatHandler, AgentMotor2D motor)
            : base(animator, combatHandler)
        {
            _motor = motor;
        }

        protected override void OnEnter()
        {
            base.OnEnter();
            _motor.StopHorizontal();
        }
    }
}
