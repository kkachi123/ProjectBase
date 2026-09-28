namespace ProjectRE
{
using System;
using System.Collections.Generic;

public class PlayerStateFactoryData : StateFactoryData
{
    public PlayerAnimator PlayerAnimator { get; set; }
    public GroundDetector GroundDetector { get; set; }
    public IAgentJumpInput JumpInput { get; set; }
    public ComboAttackHandler ComboAttackHandler { get; set; }
    public IAttackComboStarter ComboAttackStarter { get; set; }
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
            { typeof(AttackState), new PlayerAttackState(data.Motor, data.PlayerAnimator, data.CombatHandler, data.CombatInput, data.ComboAttackHandler) },
            { typeof(HitState), new HitState(data.PlayerAnimator, data.CombatHandler) },
            { typeof(DeathState), new DeathState(data.PlayerAnimator, data.CombatHandler, data.MovementHandler) }
        };

        states[typeof(GroundedState)].AddTransition(new GetHitTransition(data.Health.CurrentHealth));
        states[typeof(GroundedState)].AddTransition(new JumpTransition(data.JumpInput, data.GroundDetector));
        states[typeof(GroundedState)].AddTransition(new GroundedFallTransition(data.GroundDetector, data.Motor));
        states[typeof(GroundedState)].AddTransition(new ComboAttackTransition(data.CombatInput, data.ComboAttackHandler, data.GroundDetector, data.AttackStarter));

        states[typeof(JumpState)].AddTransition(new GetHitTransition(data.Health.CurrentHealth));
        states[typeof(JumpState)].AddTransition(new JumpFallTransition(data.Motor));
        states[typeof(JumpState)].AddTransition(new ComboAttackTransition(data.CombatInput, data.ComboAttackHandler, data.GroundDetector, data.AttackStarter));


        states[typeof(FallState)].AddTransition(new GetHitTransition(data.Health.CurrentHealth));
        states[typeof(FallState)].AddTransition(new LandTransition(data.GroundDetector, data.Motor));
        states[typeof(FallState)].AddTransition(new ComboAttackTransition(data.CombatInput, data.ComboAttackHandler, data.GroundDetector, data.AttackStarter));

        states[typeof(AttackState)].AddTransition(new GetHitTransition(data.Health.CurrentHealth));
        states[typeof(AttackState)].AddTransition(new AttackEndTransition(data.AnimationEventSource, data.GroundDetector, data.CombatInput, data.ComboAttackStarter, data.PlayerAnimator, data.ComboAttackHandler));

        states[typeof(HitState)].AddTransition(new DeathTransition(data.Health.IsDead));
        states[typeof(HitState)].AddTransition(new GetHitEndTransition(data.AnimationEventSource));


        return states;
    }
}
}
