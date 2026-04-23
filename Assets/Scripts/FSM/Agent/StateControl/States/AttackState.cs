using UnityEngine;

public class AttackState : AgentStateBase
{
    private bool _isAttackFinished;
    public AttackState(AgentController agent) : base(agent) { }
    public override void Enter()
    {
        _isAttackFinished = false;
        _agent.Attack(true);
    }
    public override void Execute()
    {
        if(_isAttackFinished) _agent.ChangeState(StateType.Idle);
    }
    public override void Exit() 
    { 
        _agent.Attack(false); 
    }

    public override void OnAnimationEvent(AnimEventType type)
    {
        if(type == AnimEventType.OnFrame)
        {
            _agent.OnAttackHitFrame();
        }
        if(type == AnimEventType.End)
        {
            _isAttackFinished = true;
        }
    }
}
