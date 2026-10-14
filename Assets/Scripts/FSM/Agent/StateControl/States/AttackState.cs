namespace ProjectRE
{
    public abstract class AttackState : AgentStateBase
    {
        protected readonly ICombatAnimation Animator;

        protected AttackState(ICombatAnimation animator)
        {
            Animator = animator;
        }

        protected override void OnEnter()
        {
            Animator.SetAttack(true);
        }

        protected override void OnExecute(float deltaTime) { }

        protected override void OnExit()
        {
            Animator.SetAttack(false);
            Animator.SetAttackType(0);
        }

        /// <summary>현재 State의 공격 실행에 타격 Event 전달.</summary>
        public abstract void OnAttackFrame(int attackStep);
    }
}
