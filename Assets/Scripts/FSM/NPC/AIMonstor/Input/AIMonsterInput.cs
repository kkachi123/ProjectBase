using UniRx;
using UnityEngine;
public class AIMonsterInput : MonoBehaviour , IAgentMovementInput , IAgentCombatInput
{
    public Vector2 Horizontal { get; private set; }
    private readonly ReactiveProperty<int> _attackPressed = new ReactiveProperty<int>(0);
    public IReadOnlyReactiveProperty<int> AttackPressed => _attackPressed;

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
        if(value != 0) _attackPressed.Value = 0; 
    }
}
