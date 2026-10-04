namespace ProjectRE
{
    using System;
    using System.Collections.Generic;

    /// <summary>State 생성·전이 연결에 필요한 Agent 공통 의존성 전달.</summary>
    public class StateFactoryData
    {
        public AgentAnimator Animator { get; set; }
        public AgentMotor2D Motor { get; set; }
        public AgentMovementHandler2D MovementHandler { get; set; }
        public IAgentMovementInput MovementInput { get; set; }
        public AgentCombatHandler CombatHandler { get; set; }
        public IAgentCombatInput CombatInput { get; set; }
        public IAttackStarter AttackStarter { get; set; }
        public IAnimationEventSource AnimationEventSource { get; set; }
        public Health Health { get; set; }
    }

    /// <summary>공통·Agent별 State 생성 및 전이 연결 순서 관리.</summary>
    public abstract class AgentStateFactory<TData> where TData : StateFactoryData
    {
        /// <summary>공통 State 생성 → Agent별 생성·교체 → 전이 연결 순서로 FSM 구성.</summary>
        public Dictionary<Type, AgentStateBase> CreateStates(TData data)
        {
            var states = new Dictionary<Type, AgentStateBase>();

            AddCommonStates(data, states);
            AddAgentStates(data, states);
            ConfigureTransitions(data, states);

            return states;
        }

        /// <summary>공통 Hit·Death State 등록. 파생 Factory에서 구성 확장 가능.</summary>
        protected virtual void AddCommonStates(TData data, Dictionary<Type, AgentStateBase> states)
        {
            states.Add(typeof(HitState), new HitState(data.Animator, data.CombatHandler));
            states.Add(typeof(DeathState), new DeathState(data.Animator, data.CombatHandler, data.MovementHandler));
        }

        /// <summary>Agent별 State 추가 또는 공통 State의 전용 구현 교체.</summary>
        protected abstract void AddAgentStates(TData data, Dictionary<Type, AgentStateBase> states);

        /// <summary>최종 State 객체에 전이 연결. 등록 순서대로 우선 평가.</summary>
        protected abstract void ConfigureTransitions(TData data, Dictionary<Type, AgentStateBase> states);
    }
}
