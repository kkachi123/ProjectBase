using UnityEngine;
public class MoveState : PlayerStateBase
{
    public MoveState(PlayerController player) : base(player) { _player = player; }

    public override void Enter()
    {
        Debug.Log("MoveState Entered");
        _player.Move(true);
    }

    public override void Execute()
    {
        _player.HandleMovement();
        if (!_player.IsGrounded) _player.ChangeState(StateType.Fall);
        else if (_player.IsIdle) _player.ChangeState(StateType.Idle);
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
