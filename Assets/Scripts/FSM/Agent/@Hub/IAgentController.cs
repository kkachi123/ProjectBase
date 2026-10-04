namespace ProjectRE
{
    public interface IAgentAnimationListener
    {
        void OnAnimationEvent(AnimEventType type);
    }

    public interface IAttackStarter
    {
        // 최초 공격 요청의 시작 가능 여부 확인.
        bool TryStartAttack();
    }
}
