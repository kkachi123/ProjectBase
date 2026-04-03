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
    IAgentMovementInput MoveInput { get; }
    IAgentCombatInput CombatInput { get; }
    GameObject gameObject { get; }
    void OnJumpAction();
    void OnAttackAction(int attackType);
}