using System.Collections.Generic;

public class MonsterStateFactory
{
    public Dictionary<StateType, MonsterStateBase> CreateStates(MonsterController monsterController)
    {
        return new Dictionary<StateType, MonsterStateBase>
        {
            { StateType.Idle, new M_IdleState(monsterController) },
            { StateType.Move, new M_MoveState(monsterController) },
            { StateType.Attack, new M_AttackState(monsterController) },
            { StateType.Hit, new M_HitState(monsterController) },
            { StateType.Death, new M_DeathState(monsterController) }
        };
    }
}
