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
            { typeof(IdleState), new IdleState(data.Animator , data.MovementHandler) },
            { typeof(MoveState), new MoveState(data.Animator, data.MovementHandler, data.MovementInput) },
            { typeof(JumpState), new JumpState(data.Animator, data.MovementHandler, data.MovementInput) },
            { typeof(FallState), new FallState(data.Animator, data.MovementHandler, data.MovementInput) },
            { typeof(AttackState), new AttackState(data.Animator, data.CombatHandler, data.CombatInput, data.AttackStarter) },
            { typeof(HitState), new HitState(data.Animator, data.CombatHandler) },
            { typeof(DeathState), new DeathState(data.Animator, data.CombatHandler, data.MovementHandler) }
        };

        states[typeof(IdleState)].AddTransition(new GetHitTransition(data.Health.CurrentHealth));
        states[typeof(IdleState)].AddTransition(new JumpTransition(data.JumpInput , data.GroundDetector));
        states[typeof(IdleState)].AddTransition(new GroundedFallTransition(data.GroundDetector));
        states[typeof(IdleState)].AddTransition(new IdleToMoveTransition(data.MovementInput, true));
        states[typeof(IdleState)].AddTransition(new AttackTransition(data.CombatInput , data.AttackStarter));

        states[typeof(MoveState)].AddTransition(new GetHitTransition(data.Health.CurrentHealth));
        states[typeof(MoveState)].AddTransition(new JumpTransition(data.JumpInput, data.GroundDetector));
        states[typeof(MoveState)].AddTransition(new GroundedFallTransition(data.GroundDetector));
        states[typeof(MoveState)].AddTransition(new IdleToMoveTransition(data.MovementInput, false));

        states[typeof(JumpState)].AddTransition(new GetHitTransition(data.Health.CurrentHealth));
        states[typeof(JumpState)].AddTransition(new JumpFallTransition(data.Motor));
        states[typeof(JumpState)].AddTransition(new AttackTransition(data.CombatInput , data.AttackStarter));


        states[typeof(FallState)].AddTransition(new GetHitTransition(data.Health.CurrentHealth));
        states[typeof(FallState)].AddTransition(new LandTransition(data.MovementInput, data.GroundDetector, data.Motor));
        states[typeof(FallState)].AddTransition(new AttackTransition(data.CombatInput , data.AttackStarter));

        states[typeof(AttackState)].AddTransition(new GetHitTransition(data.Health.CurrentHealth));
        states[typeof(AttackState)].AddTransition(new AttackEndTransition(data.AnimationEventSource, data.CombatInput, data.GroundDetector));

        states[typeof(HitState)].AddTransition(new DeathTransition(data.Health.IsDead));
        states[typeof(HitState)].AddTransition(new GetHitEndTransition(data.AnimationEventSource));


        return states;
    }
}
