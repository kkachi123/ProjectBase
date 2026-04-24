using UnityEngine;
public class JumpState : GroundedAgentStateBase
{
    private float _jumpTimer;
    private bool _isJumpFinished;
    private const float MIN_JUMP_TIME = 0.1f;
    public JumpState(GroundedAgentController agent) : base(agent) { }

    public override void Enter() 
    {
        _jumpTimer = 0f;
        _isJumpFinished = false;
        _agent.Jump(true);
    }

    public override void Execute()
    {
        _jumpTimer += Time.deltaTime;
        if(_jumpTimer >= MIN_JUMP_TIME)
        {
            if (_agent.IsGrounded) _agent.ChangeState(StateType.Idle);
            else if(!_agent.IsGrounded && _isJumpFinished) _agent.ChangeState(StateType.Fall);
        }
    }

    public override void Exit() 
    {
        _agent.Jump(false);
    }
    public override void OnAnimationEvent(AnimEventType type)
    {
        if(type == AnimEventType.End)
        {
            _isJumpFinished = true;
        }
    }
    public override void OnInputEvent(InputKeyType type)
    {
        if( _agent.IsGrounded) return;
        switch (type)
        {
            case InputKeyType.Attack:
                _agent.ChangeState(StateType.Attack);
                break;
        }
    }
}
