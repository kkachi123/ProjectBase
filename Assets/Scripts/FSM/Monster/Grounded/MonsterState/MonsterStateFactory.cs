namespace ProjectRE
{
    using System;
    using System.Collections.Generic;

    /// <summary>지상 Monster 판단·행동 State 및 순서 기반 전이 구성.</summary>
    public class MonsterStateFactory : AgentStateFactory<MonsterStateFactoryData>
    {
        protected override void AddAgentStates(MonsterStateFactoryData data, Dictionary<Type, AgentStateBase> states)
        {
            states.Add(typeof(MonsterPatrolState), new MonsterPatrolState(data.Owner, data.MovementHandler,
                data.MonsterAnimator, data.BehaviorData, data.HomeX));
            states.Add(typeof(MonsterPatrolWaitState), new MonsterPatrolWaitState(data.MovementHandler, data.MonsterAnimator, data.BehaviorData));
            states.Add(typeof(MonsterChaseState), new MonsterChaseState(data.MovementHandler, data.MonsterAnimator, data.Target, data.Attack));
            states.Add(typeof(MonsterLostTargetWaitState), new MonsterLostTargetWaitState(data.MovementHandler, data.MonsterAnimator, data.BehaviorData));
            states.Add(typeof(MonsterReturnState), new MonsterReturnState(data.Owner, data.MovementHandler,
                data.MonsterAnimator, data.HomeX, data.BehaviorData.ArrivalDistance));
            states.Add(typeof(AttackState), new MonsterAttackState(data.MonsterAnimator, data.Attack));
            states[typeof(HitState)] = new MonsterHitState(data.MonsterAnimator, data.Motor);
        }

        protected override void ConfigureTransitions(MonsterStateFactoryData data, Dictionary<Type, AgentStateBase> states)
        {
            // State별 독립 구독. 사망·피격을 행동 조건보다 먼저 평가.
            foreach (var pair in states)
            {
                if (pair.Key == typeof(DeathState))
                    continue;
                pair.Value.AddTransition(new DeathTransition(data.Health.IsDead));
                if (pair.Key != typeof(HitState))
                    pair.Value.AddTransition(new GetHitTransition(data.Health.CurrentHealth));
            }

            foreach (Type type in new[] { typeof(MonsterPatrolState), typeof(MonsterPatrolWaitState),
                typeof(MonsterLostTargetWaitState), typeof(MonsterReturnState) })
                states[type].AddTransition(new MonsterTargetTransition(data.Target, true, typeof(MonsterChaseState)));

            states[typeof(MonsterPatrolState)].AddTransition(new MonsterPatrolEndTransition((MonsterPatrolState)states[typeof(MonsterPatrolState)]));
            states[typeof(MonsterPatrolWaitState)].AddTransition(new MonsterPatrolWaitEndTransition((MonsterPatrolWaitState)states[typeof(MonsterPatrolWaitState)]));
            states[typeof(MonsterLostTargetWaitState)].AddTransition(new MonsterLostTargetWaitEndTransition((MonsterLostTargetWaitState)states[typeof(MonsterLostTargetWaitState)]));
            states[typeof(MonsterReturnState)].AddTransition(new MonsterReturnEndTransition((MonsterReturnState)states[typeof(MonsterReturnState)]));

            states[typeof(MonsterChaseState)].AddTransition(new MonsterTargetTransition(data.Target, false, typeof(MonsterLostTargetWaitState)));
            states[typeof(MonsterChaseState)].AddTransition(new MonsterAttackTransition(data.Attack));
            states[typeof(AttackState)].AddTransition(new MonsterAttackEndTransition(data.Attack, data.Target));
            states[typeof(HitState)].AddTransition(new MonsterHitEndTransition(data.AnimationEventSource, data.Target, data.Attack));
        }
    }
}
