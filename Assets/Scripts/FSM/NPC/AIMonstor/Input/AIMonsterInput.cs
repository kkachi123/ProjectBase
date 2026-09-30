namespace ProjectRE
{
using UnityEngine;
public class AIMonsterInput : MonoBehaviour , IAgentMovementInput , IAgentCombatInput
{
    public Vector2 Horizontal { get; private set; }
    public event System.Action OnAttackRequested;

    public Vector2 GetMovementInput()
    {
        return Horizontal;
    }
    public void Move(Vector2 horizontal)
    {
        Horizontal = horizontal;
    }

    public void Attack(int value)
    {
        if (value > 0)
            OnAttackRequested?.Invoke();
    }
}
}
