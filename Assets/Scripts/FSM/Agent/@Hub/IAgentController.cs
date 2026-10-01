namespace ProjectRE
{
    public interface IAgentAnimationListener
    {
        void OnAnimationEvent(AnimEventType type);
    }

    public interface IAttackStarter
    {
        // 최초 공격 요청을 수신하고 실제 공격 시작이 가능한 경우 AttackState로 진입한다.
        bool TryStartAttack();
    }

    public interface IAttackComboStarter
    {
        bool TryContinueAttack(int nextAttackType);
    }
}
