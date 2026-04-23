using UnityEngine;

public class P_HitState : PlayerStateBase
{
    private bool _isHitFinished;
    public P_HitState(PlayerController player) : base(player) { }
    public override void Enter()
    {
        Debug.Log("P_HitState Entered");
        _isHitFinished = false;
        _player.Hit(true);
    }
    public override void Execute()
    {
        if(_isHitFinished) _player.ChangeState(StateType.Idle);
    }
    public override void Exit() 
    {
        _player.Hit(false);
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