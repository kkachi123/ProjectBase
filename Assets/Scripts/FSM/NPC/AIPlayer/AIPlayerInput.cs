namespace ProjectRE
{
using UniRx;
using UnityEngine;
public class AIPlayerInput : MonoBehaviour , IAgentMovementInput , IAgentJumpInput , IAgentCombatInput
{
    public Vector2 Horizontal { get; private set; }
    private readonly ReactiveProperty<bool> _jumpPressed = new ReactiveProperty<bool>(false);
    public bool IsJumpHeld => _jumpPressed.Value;
    public event System.Action OnAttackRequested;

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
        if (value > 0)
            OnAttackRequested?.Invoke();
    }
}
}
