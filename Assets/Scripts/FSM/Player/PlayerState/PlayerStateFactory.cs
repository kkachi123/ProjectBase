namespace ProjectRE
{
    using System;
    using System.Collections.Generic;

    public class PlayerStateFactoryData : StateFactoryData
    {
        public PlayerAnimator PlayerAnimator { get; set; }
        public GroundDetector GroundDetector { get; set; }
        public IAgentJumpInput JumpInput { get; set; }
        public IAgentDashInput DashInput { get; set; }
        public AgentDashHandler2D DashHandler { get; set; }
        public Stamina Stamina { get; set; }
        public AgentStatData StatData { get; set; }
    }

    public class PlayerStateFactory
    {
        public Dictionary<Type, AgentStateBase> CreateStates(PlayerStateFactoryData data)
        {
            Dictionary<Type, AgentStateBase> states = new Dictionary<Type, AgentStateBase>
        {
            { typeof(GroundedState), new GroundedState(data.PlayerAnimator, data.MovementHandler, data.MovementInput) },
            { typeof(JumpState), new JumpState(data.PlayerAnimator, data.PlayerAnimator, data.MovementHandler, data.MovementInput) },
            { typeof(FallState), new FallState(data.PlayerAnimator, data.PlayerAnimator, data.MovementHandler, data.MovementInput) },
            { typeof(DashState), new DashState(data.PlayerAnimator, data.DashHandler) },
            { typeof(AttackState), new PlayerAttackState(data.PlayerAnimator, data.CombatHandler,data.Motor,  data.Stamina, data.StatData, data.CombatInput) },
            { typeof(HitState), new HitState(data.PlayerAnimator, data.CombatHandler) },
            { typeof(DeathState), new DeathState(data.PlayerAnimator, data.CombatHandler, data.MovementHandler) }
        };

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


            return states;
        }
    }
}
