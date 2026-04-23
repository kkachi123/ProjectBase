using UnityEngine;

public class FallState : AgentStateBase
{
    public FallState(AgentController agent) : base(agent) { }

    public override void Enter()
    {
        _agent.Falling(true);
    }

    public override void Execute()
    {
        if (_agent.IsGrounded)
        {
            if(_agent.IsIdle) _agent.ChangeState(StateType.Idle);
            else _agent.ChangeState(StateType.Move);
        }
            
    }

    public override void FixedExecute()
    {
        _agent.HandleMovement();
    }
    public override void Exit()
    {
        _agent.Falling(false);
    }

    public override void OnInputEvent(InputKeyType type)
    {
        if(_agent.IsGrounded) return;
        switch (type)
        {
            case InputKeyType.Attack:
                _agent.ChangeState(StateType.Attack);
                break;
        }
    }
}
