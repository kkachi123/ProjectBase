using UnityEngine;
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
