using UnityEngine;

public class P_FallState : PlayerStateBase
{
    public P_FallState(PlayerController player) : base(player) { }

    public override void Enter()
    {
        Debug.Log("P_FallState Entered");
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
