using UnityEngine;

public class HitState : AgentStateBase
{
    private bool _isHitFinished;
    public HitState(AgentController agent) : base(agent) { }
    public override void Enter()
    {
        _isHitFinished = false;
        _agent.Hit(true);
    }
    public override void Execute()
    {
        if(_isHitFinished) _agent.ChangeState(StateType.Idle);
    }
    public override void Exit() 
    {
        _agent.Hit(false);
    }

    public override void OnAnimationEvent(AnimEventType type)
    {
        if(type == AnimEventType.End)
        {
            _isHitFinished = true;
        }
    }
    
}