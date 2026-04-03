using UnityEngine;

public class HitState : PlayerStateBase
{
    private bool _isHitFinished;
    public HitState(PlayerController player) : base(player) { _player = player; }
    public override void Enter()
    {
        Debug.Log("HurtState Entered");
        _isHitFinished = false;
    }
    public override void Execute()
    {
        if(_isHitFinished) _player.ChangeState(StateType.Idle);
    }
    public override void Exit() { }

    public override void OnAnimationEvent(AnimEventType type)
    {
        if(type == AnimEventType.End)
        {
            _isHitFinished = true;
        }
    }
    
}