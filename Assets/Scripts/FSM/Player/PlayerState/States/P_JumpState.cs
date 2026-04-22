using UnityEngine;
public class P_JumpState : PlayerStateBase
{
    private float _jumpTimer;
    private const float MIN_JUMP_TIME = 0.1f;
    public P_JumpState(PlayerController player) : base(player) { }

    public override void Enter() 
    {
        _jumpTimer = 0f;
        Debug.Log("P_JumpState Entered");
        _player.Jump(true);
    }

    public override void Execute()
    {
        _jumpTimer += Time.deltaTime;
        if(_jumpTimer >= MIN_JUMP_TIME)
        {
            if (_player.IsGrounded) _player.ChangeState(StateType.Idle);
            else _player.ChangeState(StateType.Fall);
        }
    }

    public override void Exit() 
    {
        _player.Jump(false);
    }

    public override void OnInputEvent(InputKeyType type)
    {
        if( _player.IsGrounded) return;
        switch (type)
        {
            case InputKeyType.Attack:
                _player.ChangeState(StateType.Attack);
                break;
        }
    }
}
