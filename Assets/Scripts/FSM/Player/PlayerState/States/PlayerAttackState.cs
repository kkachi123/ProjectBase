namespace ProjectRE
{
    /// <summary>Player 공격 모션 및 Handler 실행 수명 연결.</summary>
    public class PlayerAttackState : AttackState
    {
        private readonly PlayerAttackHandler _attackHandler;

        public PlayerAttackState(ICombatAnimation animator, PlayerAttackHandler attackHandler)
            : base(animator)
        {
            _attackHandler = attackHandler;
        }

        protected override void OnEnter()
        {
            _attackHandler.BeginAttack();
            base.OnEnter();
        }

        protected override void OnExit()
        {
            _attackHandler.EndAttack();
            base.OnExit();
        }
        public override void OnAttackFrame(int attackStep) => _attackHandler.OnAttackFrame(attackStep);
    }
}
