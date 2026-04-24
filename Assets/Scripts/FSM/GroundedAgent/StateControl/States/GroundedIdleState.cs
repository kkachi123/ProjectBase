using UnityEngine;

public class GroundedIdleState : IdleState<GroundedAgentController>
{
    public GroundedIdleState(GroundedAgentController agent) : base(agent) { }

    public override void Execute()
    {
        if (!_agent.IsGrounded) _agent.ChangeState(StateType.Fall);
        else if (!_agent.IsIdle) _agent.ChangeState(StateType.Move);
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
