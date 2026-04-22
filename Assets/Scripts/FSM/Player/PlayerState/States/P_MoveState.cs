using UnityEngine;
public class P_MoveState : PlayerStateBase
{
    public P_MoveState(PlayerController player) : base(player) { }

    public override void Enter()
    {
        Debug.Log("P_MoveState Entered");
        _player.Move(true);
    }

    public override void Execute()
    {
        if (!_player.IsGrounded) _player.ChangeState(StateType.Fall);
        else if (_player.IsIdle) _player.ChangeState(StateType.Idle);
    }

    public override void FixedExecute()
    {
        _player.HandleMovement();
    }

    public override void Exit() 
    {
        _player.Move(false);
    }
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
