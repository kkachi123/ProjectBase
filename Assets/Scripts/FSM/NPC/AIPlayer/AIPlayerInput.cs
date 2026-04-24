using UniRx;
using UnityEngine;
public class AIPlayerInput : MonoBehaviour , IAgentMovementInput , IAgentJumpInput , IAgentCombatInput
{
    public Vector2 Horizontal { get; private set; }
    private readonly ReactiveProperty<bool> _jumpPressed = new ReactiveProperty<bool>(false);
    private readonly ReactiveProperty<int> _attackPressed = new ReactiveProperty<int>(0);
    public IReadOnlyReactiveProperty<bool> JumpPressed => _jumpPressed;
    public IReadOnlyReactiveProperty<int> AttackPressed => _attackPressed;

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

    public void Attack(int value)
    {
        _attackPressed.Value = value;
        if(value != 0) _attackPressed.Value = 0; 
    }
}
