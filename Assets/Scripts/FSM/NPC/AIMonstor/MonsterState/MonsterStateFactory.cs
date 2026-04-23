using System.Collections.Generic;

public class MonsterStateFactory
{
    public Dictionary<StateType, IAgentState> CreateStates(MonsterController monsterController)
    {
        return new Dictionary<StateType, IAgentState>
        {
            { StateType.Idle, new IdleState(monsterController) },
            { StateType.Move, new MoveState(monsterController) },
            { StateType.Attack, new AttackState(monsterController) },
            { StateType.Hit, new HitState(monsterController) },
            { StateType.Death, new DeathState(monsterController) }
        };
    }
}
