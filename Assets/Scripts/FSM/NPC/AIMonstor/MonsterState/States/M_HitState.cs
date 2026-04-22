using UnityEngine;

public class M_HitState : MonsterStateBase
{
    private bool _isHitFinished;
    public M_HitState(MonsterController monster) : base(monster) { }

    public override void Enter()
    {
        _isHitFinished = false;
        _monster.Hit(true);
    }

    public override void Execute()
    {
        if (_isHitFinished) _monster.ChangeState(StateType.Idle);
    }

    public override void Exit() 
    {
        _monster.Hit(false);
    }

    public override void OnAnimationEvent(AnimEventType type)
    {
        if (type == AnimEventType.End)
        {
            _isHitFinished = true;
        }
    }
}