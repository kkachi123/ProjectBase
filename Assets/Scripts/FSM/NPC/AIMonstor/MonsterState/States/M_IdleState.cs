using UnityEngine;

public class M_IdleState : MonsterStateBase
{
    public M_IdleState(MonsterController monster) : base(monster) { }

    public override void Enter()
    {
        _monster.Idle(true);
    }

    public override void Execute()
    {
         if (!_monster.IsIdle) _monster.ChangeState(StateType.Move);
    }

    public override void Exit() 
    {
        _monster.Idle(false);
    }
}
