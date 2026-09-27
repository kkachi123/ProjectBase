namespace ProjectRE
{
using System;
using System.Collections.Generic;

public class PlayerStateFactoryData : StateFactoryData
{
    public GroundDetector GroundDetector { get; set; }
    public IAgentJumpInput JumpInput { get; set; }
}

public class PlayerStateFactory 
{
    public Dictionary<Type, AgentStateBase> CreateStates(PlayerStateFactoryData data)
    {
        Dictionary<Type, AgentStateBase> states = new Dictionary<Type, AgentStateBase>
        {
            { typeof(GroundedState), new GroundedState(data.Animator, data.MovementHandler, data.MovementInput) },
            { typeof(JumpState), new JumpState(data.Animator, data.MovementHandler, data.MovementInput) },
            { typeof(FallState), new FallState(data.Animator, data.MovementHandler, data.MovementInput) },
            { typeof(AttackState), new AttackState(data.Animator, data.CombatHandler, data.CombatInput, data.AttackStarter) },
            { typeof(HitState), new HitState(data.Animator, data.CombatHandler) },
            { typeof(DeathState), new DeathState(data.Animator, data.CombatHandler, data.MovementHandler) }
        };

        states[typeof(GroundedState)].AddTransition(new GetHitTransition(data.Health.CurrentHealth));
        states[typeof(GroundedState)].AddTransition(new JumpTransition(data.JumpInput, data.GroundDetector));
        states[typeof(GroundedState)].AddTransition(new GroundedFallTransition(data.GroundDetector));
        states[typeof(GroundedState)].AddTransition(new AttackTransition(data.CombatInput, data.AttackStarter));

        states[typeof(JumpState)].AddTransition(new GetHitTransition(data.Health.CurrentHealth));
        states[typeof(JumpState)].AddTransition(new JumpFallTransition(data.Motor));
        states[typeof(JumpState)].AddTransition(new AttackTransition(data.CombatInput , data.AttackStarter));


        states[typeof(FallState)].AddTransition(new GetHitTransition(data.Health.CurrentHealth));
        states[typeof(FallState)].AddTransition(new LandTransition(data.GroundDetector, data.Motor));
        states[typeof(FallState)].AddTransition(new AttackTransition(data.CombatInput , data.AttackStarter));

        states[typeof(AttackState)].AddTransition(new GetHitTransition(data.Health.CurrentHealth));
        states[typeof(AttackState)].AddTransition(new AttackEndTransition(data.AnimationEventSource, data.CombatInput, data.GroundDetector));

        states[typeof(HitState)].AddTransition(new DeathTransition(data.Health.IsDead));
        states[typeof(HitState)].AddTransition(new GetHitEndTransition(data.AnimationEventSource));


        return states;
    }
}
}
