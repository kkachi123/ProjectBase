namespace ProjectRE
{
    public interface IAgentAnimationListener
    {
        void OnAnimationEvent(AnimEventType type);
    }

    public interface IAttackStarter
    {
        bool TryStartAttack(int requestedAttackType);
    }

    public interface IAttackComboStarter
    {
        bool TryContinueAttack(int nextAttackType);
    }
}
