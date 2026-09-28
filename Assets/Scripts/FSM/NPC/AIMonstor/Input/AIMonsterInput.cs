namespace ProjectRE
{
using UniRx;
using UnityEngine;
public class AIMonsterInput : MonoBehaviour , IAgentMovementInput , IAgentCombatInput
{
    public Vector2 Horizontal { get; private set; }
    private readonly ReactiveProperty<int> _attackPressed = new ReactiveProperty<int>(0);
    public bool HasAttackRequest => _attackPressed.Value > 0;

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
        _attackPressed.Value = value;
    }

    public bool TryConsumeAttackRequest()
    {
        if (!HasAttackRequest) return false;
        _attackPressed.Value = 0;
        return true;
    }

    public void ClearAttackRequests() => _attackPressed.Value = 0;
}
}
