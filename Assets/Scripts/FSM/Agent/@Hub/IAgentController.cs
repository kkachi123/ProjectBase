using UnityEngine;
public interface IAgentAnimationListener
{
    void OnAnimationEvent(AnimEventType type);
}

public interface IAttackStarter
{
    bool CanStartAttack(int requestedAttackType);
    bool TryStartAttack(int requestedAttackType);
}