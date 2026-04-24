using UnityEngine;

public class IdleState<T> : AgentStateBase<T> where T : AgentController
{
    public IdleState(T agent) : base(agent) { }

    public override void Enter()
    {
        _agent.Idle(true);
    }

    public override void Execute()
    {
        if (!_agent.IsIdle) _agent.ChangeState(StateType.Move);
    }

    public override void Exit() 
    {
        _agent.Idle(false);
    }

    public override void OnInputEvent(InputKeyType type)
    {
        switch (type)
        {
            case InputKeyType.Attack:
                _agent.ChangeState(StateType.Attack);
                break;
        }
    }
}

public class IdleState : IdleState<AgentController>
{
    public IdleState(AgentController agent) : base(agent) { }
}