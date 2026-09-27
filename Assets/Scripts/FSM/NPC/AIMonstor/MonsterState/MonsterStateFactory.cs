namespace ProjectRE
{
using System;
using System.Collections.Generic;

public class MonsterStateFactoryData : StateFactoryData
{
}
public class MonsterStateFactory
{
    public Dictionary<Type, AgentStateBase> CreateStates(MonsterStateFactoryData data)
    {
        return new Dictionary<Type, AgentStateBase>
        {
            { typeof(IdleState), new IdleState(data.Animator , data.MovementHandler) },
            { typeof(MoveState), new MoveState(data.Animator, data.MovementHandler, data.MovementInput) },
            //{ typeof(AttackState), new AttackState(data.Animator, data.CombatHandler) },
            { typeof(HitState), new HitState(data.Animator, data.CombatHandler) },
            { typeof(DeathState), new DeathState(data.Animator, data.CombatHandler, data.MovementHandler) }
        };
    }
}
}
