using UnityEngine;
public class IdleState : PlayerStateBase
{
    public IdleState(PlayerController player) : base(player) { }

    public override void Enter()
    {
        Debug.Log("IdleState Entered");
        _input = InputKeyType.None; 
    }

    public override void Execute()
    {
        if (!_player.IsGrounded) _player.ChangeState(StateType.Fall);
        else if (!_player.IsIdle) _player.ChangeState(StateType.Move);
    }

    public override void Exit() { }

    public override void OnInputEvent(InputKeyType type)
    {
        if (!_player.IsGrounded) return;

        switch (type)
        {
            case InputKeyType.Jump:
                _player.ChangeState(StateType.Jump);
                break;
            case InputKeyType.Attack:
                _player.ChangeState(StateType.Attack);
                break;
        }
    }
}
