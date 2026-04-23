using UnityEngine;

public class HitState : AgentStateBase
{
    private bool _isHitFinished;
    public HitState(AgentController agent) : base(agent) { }
    public override void Enter()
    {
        Debug.Log("P_HitState Entered");
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
            Debug.Log("Hit animation ended");
            _isHitFinished = true;
        }
    }
    
}