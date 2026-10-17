namespace ProjectRE
{
    /// <summary>선택 전략의 준비·실행 및 Animation Event 전달.</summary>
    public class MonsterAttackState : AttackState
    {
        private readonly MonsterAttackHandler _handler;
        public MonsterAttackState(ICombatAnimation animator, MonsterAttackHandler handler) : base(animator) => _handler = handler;

        /// <summary>배후 준비 중 공격 모션을 켜지 않도록 전략이 시작 시점 결정.</summary>
        protected override void OnEnter() => _handler.BeginAttack();
        protected override void OnExecute(float deltaTime) => _handler.Execute(deltaTime);
        public override void OnAttackFrame(int intParameter) => _handler.OnAttackFrame(intParameter);
        public void OnAnimationEnd() => _handler.OnAnimationEnd();
        protected override void OnExit()
        {
            _handler.EndAttack();
            base.OnExit();
        }
    }
}
