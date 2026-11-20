namespace ProjectRE
{
    using System;
    using System.Collections.Generic;

    /// <summary>비행 Monster State와 전이 우선순위 구성.</summary>
    public class FlyingMonsterStateFactory : AgentStateFactory<FlyingMonsterStateData>
    {
        protected override void AddAgentStates(FlyingMonsterStateData data, Dictionary<Type, AgentStateBase> states)
        {
            states.Add(typeof(FlyingPatrolWaitState), new FlyingPatrolWaitState(data));
            states.Add(typeof(FlyingPatrolState), new FlyingPatrolState(data));
            states.Add(typeof(FlyingChaseState), new FlyingChaseState(data));
            states.Add(typeof(FlyingLostTargetWaitState), new FlyingLostTargetWaitState(data));
            states.Add(typeof(FlyingReturnState), new FlyingReturnState(data));
            states.Add(typeof(AttackState), new FlyingMonsterAttackState(data));
            states[typeof(HitState)] = new FlyingMonsterHitState(data.FlyingAnimator, data.MovementHandler);
        }

        protected override void ConfigureTransitions(FlyingMonsterStateData data, Dictionary<Type, AgentStateBase> states)
        {
            foreach (var pair in states)
            {
                if (pair.Key == typeof(DeathState)) continue;
                pair.Value.AddTransition(new DeathTransition(data.Health.IsDead));
                if (pair.Key != typeof(HitState))
                    pair.Value.AddTransition(new GetHitTransition(data.Health.CurrentHealth));
            }
            foreach (Type type in new[] { typeof(FlyingPatrolWaitState), typeof(FlyingPatrolState),
                typeof(FlyingLostTargetWaitState), typeof(FlyingReturnState) })
                states[type].AddTransition(new FlyingMonsterTransition(typeof(FlyingChaseState), () => data.Target.HasValidTarget));

            var patrol = (FlyingPatrolState)states[typeof(FlyingPatrolState)];
            var wait = (FlyingPatrolWaitState)states[typeof(FlyingPatrolWaitState)];
            var lost = (FlyingLostTargetWaitState)states[typeof(FlyingLostTargetWaitState)];
            var returning = (FlyingReturnState)states[typeof(FlyingReturnState)];
            patrol.AddTransition(new FlyingMonsterTransition(typeof(FlyingPatrolWaitState), () => patrol.Arrived));
            wait.AddTransition(new FlyingMonsterTransition(typeof(FlyingPatrolState), () => wait.Finished));
            lost.AddTransition(new FlyingMonsterTransition(typeof(FlyingReturnState), () => lost.Finished));
            returning.AddTransition(new FlyingMonsterTransition(typeof(FlyingPatrolState), () => returning.Arrived));
            states[typeof(FlyingChaseState)].AddTransition(new FlyingMonsterTransition(typeof(FlyingLostTargetWaitState), () => !data.Target.HasValidTarget));
            states[typeof(FlyingChaseState)].AddTransition(new FlyingMonsterTransition(typeof(AttackState), () => data.InAttackRange && UnityEngine.Time.time >= data.NextAttackTime));
            states[typeof(AttackState)].AddTransition(new FlyingMonsterAnimationEndTransition(data, true));
            states[typeof(HitState)].AddTransition(new FlyingMonsterAnimationEndTransition(data, false));
        }
    }
}
