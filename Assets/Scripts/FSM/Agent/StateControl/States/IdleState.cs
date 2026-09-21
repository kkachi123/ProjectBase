using UnityEngine;

public class IdleState : AgentStateBase
{
    private AgentAnimator _animator;
    private AgentMovementHandler2D _movementHandler;
    
    public IdleState(AgentAnimator animator, AgentMovementHandler2D movementHandler)
    {
        _animator = animator;
        _movementHandler = movementHandler;
    }

    protected override void OnEnter()
    {
        _animator.SetBool(StateType.Idle, true);
        _movementHandler.HandleMove(Vector2.zero);
    }

    protected override void OnExecute(float deltaTime)
    {
        //if (!_agent.IsIdle) _agent.ChangeState(StateType.Move);
    }

    public override void Exit() 
    {
        _animator.SetBool(StateType.Idle, false);
    }
}