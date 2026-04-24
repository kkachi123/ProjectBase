using UnityEngine;
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
