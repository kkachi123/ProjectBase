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
            Attack();
        }

        protected override void OnExecute(float deltaTime) { }

        protected override void OnExit()
        {
            CombatHandler.ResetAttackType();
            Animator.SetAttack(false);
            Animator.SetAttackType(0);
        }

        protected virtual void Attack()
        {
            int attackType = CombatHandler.CurrentAttackType;
            Animator.SetAttack(true);
            Animator.SetAttackType(attackType);
            CombatHandler.ApplyAttackType(attackType);
        }
    }
}
