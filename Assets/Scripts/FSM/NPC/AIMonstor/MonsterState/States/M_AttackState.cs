using UnityEngine;

public class M_AttackState : MonsterStateBase
{
    private bool _isAttackFinished;
    public M_AttackState(MonsterController monster) : base(monster) { }
    public override void Enter()
    {
        _isAttackFinished = false;
        _monster.Attack(true);
    }
    public override void Execute()
    {
        if(_isAttackFinished) _monster.ChangeState(StateType.Idle);
    }
    public override void Exit()
    {
        _monster.Attack(false);
    }

    public override void OnAnimationEvent(AnimEventType type)
    {
        if(type == AnimEventType.End)
        {
            _isAttackFinished = true;
        }
    }
}
