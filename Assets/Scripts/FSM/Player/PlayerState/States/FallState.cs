using UnityEngine;

public class FallState : PlayerStateBase
{
    public FallState(PlayerController player) : base(player) { _player = player; }

    public override void Enter()
    {
        Debug.Log("FallState Entered");
        _player.Falling(true);
    }

    public override void Execute()
    {
        if (_player.IsGrounded)
        {
            if(_player.IsIdle) _player.ChangeState(StateType.Idle);
            else _player.ChangeState(StateType.Move);
        }
            
    }

    public override void FixedExecute()
    {
        _player.HandleMovement();
    }
    public override void Exit()
    {
        _player.Falling(false);
    }

    public override void OnInputEvent(InputKeyType type)
    {
        if(_player.IsGrounded) return;
        switch (type)
        {
            case InputKeyType.Attack:
                _player.ChangeState(StateType.Attack);
                break;
        }
    }
}
