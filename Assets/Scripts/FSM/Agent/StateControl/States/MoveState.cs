using UnityEngine;

public class MoveState<T> : AgentStateBase<T> where T : AgentController
{
    public MoveState(T agent) : base(agent) { }

    public override void Enter()
    {
        _agent.Move(true);
    }

    public override void Execute()
    {
        if (_agent.IsIdle) _agent.ChangeState(StateType.Idle);
    }

    public override void FixedExecute()
    {
        _agent.HandleMovement();
    }

    public override void Exit() 
    {
        _agent.Move(false);
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

public class MoveState : MoveState<AgentController>
{
    public MoveState(AgentController agent) : base(agent) { }
}

public class GroundedMoveState : MoveState<GroundedAgentController>
{
    public GroundedMoveState(GroundedAgentController agent) : base(agent) { }

    public override void Execute()
    {
        if (!_agent.IsGrounded) _agent.ChangeState(StateType.Fall);
        else base.Execute();
    }

    public override void OnInputEvent(InputKeyType type)
    {
        if (!_agent.IsGrounded) return;

        switch (type)
        {
            case InputKeyType.Jump:
                _agent.ChangeState(StateType.Jump);
                break;
            default:
                base.OnInputEvent(type);
                break;
        }
    }
}