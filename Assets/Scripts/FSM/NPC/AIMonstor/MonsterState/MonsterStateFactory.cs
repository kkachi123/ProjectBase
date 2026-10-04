namespace ProjectRE
{
using System;
using System.Collections.Generic;

/// <summary>기본 Monster 네 State 생성 및 사망·피격·행동 순서의 전이 구성.</summary>
public class MonsterStateFactory : AgentStateFactory<MonsterStateFactoryData>
{
    protected override void AddAgentStates(MonsterStateFactoryData data, Dictionary<Type, AgentStateBase> states)
    {
        states.Add(typeof(GroundedState), new GroundedState(data.MonsterAnimator, data.MovementHandler, data.MovementInput));
        states.Add(typeof(AttackState), new MonsterAttackState(data.MonsterAnimator, data.CombatHandler, data.Motor));
        states[typeof(HitState)] = new MonsterHitState(data.MonsterAnimator, data.CombatHandler, data.Motor);
    }

    protected override void ConfigureTransitions(MonsterStateFactoryData data, Dictionary<Type, AgentStateBase> states)
    {
        states[typeof(GroundedState)].AddTransition(new DeathTransition(data.Health.IsDead));
        states[typeof(GroundedState)].AddTransition(new GetHitTransition(data.Health.CurrentHealth));
        states[typeof(GroundedState)].AddTransition(new AttackTransition(data.CombatInput, data.AttackStarter));

        states[typeof(AttackState)].AddTransition(new DeathTransition(data.Health.IsDead));
        states[typeof(AttackState)].AddTransition(new GetHitTransition(data.Health.CurrentHealth));
        states[typeof(AttackState)].AddTransition(new AttackEndTransition(data.AnimationEventSource, typeof(GroundedState)));

        states[typeof(HitState)].AddTransition(new DeathTransition(data.Health.IsDead));
        states[typeof(HitState)].AddTransition(new GetHitEndTransition(data.AnimationEventSource));

        foreach (AgentStateBase state in states.Values)
        {
            foreach (ITransitionRule rule in state._transitionRules)
            {
                if (!states.ContainsKey(rule.NextStateType))
                    throw new InvalidOperationException($"Monster transition destination is not registered: {rule.NextStateType.Name}");
            }
        }
    }
}
}
