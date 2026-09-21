using System.Collections.Generic;

public class MonsterStateFactoryData : StateFactoryData
{
}
public class MonsterStateFactory
{
    public Dictionary<StateType, AgentStateBase> CreateStates(MonsterStateFactoryData data)
    {
        return new Dictionary<StateType, AgentStateBase>
        {
            { StateType.Idle, new IdleState(data.Animator , data.MovementHandler) },
            { StateType.Move, new MoveState(data.Animator, data.MovementHandler, data.MovementInput) },
            { StateType.Attack, new AttackState(data.Animator, data.CombatHandler) },
            { StateType.Hit, new HitState(data.Animator, data.CombatHandler) },
            { StateType.Death, new DeathState(data.Animator, data.CombatHandler, data.MovementHandler) }
        };
    }
}
