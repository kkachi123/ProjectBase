namespace ProjectRE
{
    public abstract class AttackState : AgentStateBase
    {
        protected readonly ICombatAnimation Animator;
        protected readonly AgentCombatHandler CombatHandler;

        protected AttackState(ICombatAnimation animator, AgentCombatHandler combatHandler)
        {
            Animator = animator;
            CombatHandler = combatHandler;
        }

        protected override void OnEnter()
        {
            Animator.SetAttackType(CombatHandler.CurrentAttackType);
            Animator.SetAttack(true);
        }

        protected override void OnExecute(float deltaTime) { }

        protected override void OnExit()
        {
            CombatHandler.ResetAttackType();
            Animator.SetAttack(false);
            Animator.SetAttackType(0);
        }

    }
}
