using UnityEngine;
public class P_IdleState : PlayerStateBase
{
    public P_IdleState(PlayerController player) : base(player) { }

    public override void Enter()
    {
        _player.Idle(true);
    }

    public override void Execute()
    {
        if (!_player.IsGrounded) _player.ChangeState(StateType.Fall);
        else if (!_player.IsIdle) _player.ChangeState(StateType.Move);
    }

    public override void Exit() 
    {
        _player.Idle(false);
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
