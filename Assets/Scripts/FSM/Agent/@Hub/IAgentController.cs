using UnityEngine;
public interface IAgentHealthListener
{
    Health Health { get; }
    GameObject gameObject { get; }
    void OnHit();
    void OnDeath();
}
public interface IAgentInputListener
{
    GameObject gameObject { get; }
    IAgentCombatInput CombatInput { get; }
    void OnAttackAction(int attackType);
}

public interface IAgentAnimationListener
{
    void OnAnimationEvent(AnimEventType type);
}
