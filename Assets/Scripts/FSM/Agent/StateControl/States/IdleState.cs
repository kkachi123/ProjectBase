using UnityEngine;
public class IdleState : AgentStateBase
{
    public IdleState(AgentController agent) : base(agent) { }

    public override void Enter()
    {
        _agent.Idle(true);
    }

    public override void Execute()
    {
        if (!_agent.IsGrounded) _agent.ChangeState(StateType.Fall);
        else if (!_agent.IsIdle) _agent.ChangeState(StateType.Move);
    }

    public override void Exit() 
    {
        _agent.Idle(false);
    }

    public override void OnInputEvent(InputKeyType type)
    {
        if (!_agent.IsGrounded) return;

        switch (type)
        {
            case InputKeyType.Jump:
                _agent.ChangeState(StateType.Jump);
                break;
            case InputKeyType.Attack:
                _agent.ChangeState(StateType.Attack);
                break;
        }
    }
}
