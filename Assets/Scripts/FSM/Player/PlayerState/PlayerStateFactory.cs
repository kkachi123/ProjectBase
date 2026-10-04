namespace ProjectRE
{
    using System;
    using System.Collections.Generic;

    /// <summary>Player 전용 State 구성에 필요한 공통·전용 의존성 전달.</summary>
    public class PlayerStateFactoryData : StateFactoryData
    {
        /// <summary>공통 Animator 참조의 PlayerAnimator 타입 접근·주입.</summary>
        public PlayerAnimator PlayerAnimator
        {
            get => (PlayerAnimator)Animator;
            set => Animator = value;
        }
        public GroundDetector GroundDetector { get; set; }
        public IAgentJumpInput JumpInput { get; set; }
        public IAgentDashInput DashInput { get; set; }
        public AgentDashHandler2D DashHandler { get; set; }
        public Stamina Stamina { get; set; }
        public AgentStatData StatData { get; set; }
    }

    /// <summary>공통 Hit·Death 기반 Player 행동 State 및 전이 순서 구성.</summary>
    public class PlayerStateFactory : AgentStateFactory<PlayerStateFactoryData>
    {
        protected override void AddAgentStates(PlayerStateFactoryData data, Dictionary<Type, AgentStateBase> states)
        {
            states.Add(typeof(GroundedState), new GroundedState(data.PlayerAnimator, data.MovementHandler, data.MovementInput));
            states.Add(typeof(JumpState), new JumpState(data.PlayerAnimator, data.PlayerAnimator, data.MovementHandler, data.MovementInput));
            states.Add(typeof(FallState), new FallState(data.PlayerAnimator, data.PlayerAnimator, data.MovementHandler, data.MovementInput));
            states.Add(typeof(DashState), new DashState(data.PlayerAnimator, data.DashHandler));
            states.Add(typeof(AttackState), new PlayerAttackState(data.PlayerAnimator, data.CombatHandler, data.Motor, data.Stamina, data.StatData, data.CombatInput));
        }

        protected override void ConfigureTransitions(PlayerStateFactoryData data, Dictionary<Type, AgentStateBase> states)
        {
            states[typeof(GroundedState)].AddTransition(new GetHitTransition(data.Health.CurrentHealth));
            states[typeof(GroundedState)].AddTransition(new DashTransition(data.DashInput, data.DashHandler));
            states[typeof(GroundedState)].AddTransition(new JumpTransition(data.JumpInput, data.GroundDetector));
            states[typeof(GroundedState)].AddTransition(new AttackTransition(data.CombatInput, data.AttackStarter));
            states[typeof(GroundedState)].AddTransition(new GroundedFallTransition(data.GroundDetector, data.Motor));

            states[typeof(JumpState)].AddTransition(new GetHitTransition(data.Health.CurrentHealth));
            states[typeof(JumpState)].AddTransition(new DashTransition(data.DashInput, data.DashHandler));
            states[typeof(JumpState)].AddTransition(new JumpFallTransition(data.Motor));
            states[typeof(JumpState)].AddTransition(new AttackTransition(data.CombatInput, data.AttackStarter));


            states[typeof(FallState)].AddTransition(new GetHitTransition(data.Health.CurrentHealth));
            states[typeof(FallState)].AddTransition(new DashTransition(data.DashInput, data.DashHandler));
            states[typeof(FallState)].AddTransition(new LandTransition(data.GroundDetector, data.Motor));
            states[typeof(FallState)].AddTransition(new AttackTransition(data.CombatInput, data.AttackStarter));

            states[typeof(AttackState)].AddTransition(new GetHitTransition(data.Health.CurrentHealth));
            states[typeof(AttackState)].AddTransition(new AttackEndTransition(data.AnimationEventSource, data.GroundDetector));

            states[typeof(DashState)].AddTransition(new GetHitTransition(data.Health.CurrentHealth));
            states[typeof(DashState)].AddTransition(new DashEndTransition(data.DashHandler, data.GroundDetector));

            states[typeof(HitState)].AddTransition(new DeathTransition(data.Health.IsDead));
            states[typeof(HitState)].AddTransition(new GetHitEndTransition(data.AnimationEventSource));
        }
    }
}
