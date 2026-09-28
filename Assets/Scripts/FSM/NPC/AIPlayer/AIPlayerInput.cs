namespace ProjectRE
{
using UniRx;
using UnityEngine;
public class AIPlayerInput : MonoBehaviour , IAgentMovementInput , IAgentJumpInput , IAgentCombatInput
{
    public Vector2 Horizontal { get; private set; }
    private readonly ReactiveProperty<bool> _jumpPressed = new ReactiveProperty<bool>(false);
    private readonly ReactiveProperty<int> _attackPressed = new ReactiveProperty<int>(0);
    public bool IsJumpHeld => _jumpPressed.Value;
    public bool HasAttackRequest => _attackPressed.Value > 0;

    public Vector2 GetMovementInput()
    {
        return Horizontal;
    }
    public void Move(Vector2 horizontal)
    {
        Horizontal = horizontal;
    }
    public void Jump(bool value)
    {
        _jumpPressed.Value = value;
        if(value) _jumpPressed.Value = false;
    }

    public bool TryConsumeJumpRequest()
    {
        bool requested = _jumpPressed.Value;
        _jumpPressed.Value = false;
        return requested;
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
