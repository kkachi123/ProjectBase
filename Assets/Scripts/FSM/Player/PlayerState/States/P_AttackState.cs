using UnityEngine;

public class P_AttackState : PlayerStateBase
{
    private bool _isAttackFinished;
    public P_AttackState(PlayerController player) : base(player) { }
    public override void Enter()
    {
        _isAttackFinished = false;
        _player.Attack(true);
    }
    public override void Execute()
    {
        if(_isAttackFinished) _player.ChangeState(StateType.Idle);
    }
    public override void Exit() 
    { 
        _player.Attack(false); 
    }

    public override void OnAnimationEvent(AnimEventType type)
    {
        if(type == AnimEventType.OnFrame)
        {
            _player.OnAttackHitFrame();
        }
        if(type == AnimEventType.End)
        {
            _isAttackFinished = true;
        }
    }
}
