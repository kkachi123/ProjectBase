using UnityEngine;
public class JumpState : PlayerStateBase
{
    private float _jumpTimer;
    private const float MIN_JUMP_TIME = 0.1f;
    public JumpState(PlayerController player) : base(player) { _player = player; }

    public override void Enter() 
    {
        _jumpTimer = 0f;
        Debug.Log("JumpState Entered");
        _player.Jump();
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

    public override void Exit() {}

    public override void OnInputEvent(InputKeyType type)
    {
        switch (type)
        {
            case InputKeyType.Attack:
                _player.ChangeState(StateType.Attack);
                break;
        }
    }
}
