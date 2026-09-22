using System.Collections.Generic;

public class PlayerStateFactoryData : StateFactoryData
{
    public GroundDetector GroundDetector { get; set; }
    public IAgentJumpInput JumpInput { get; set; }
}

public class PlayerStateFactory 
{
    public Dictionary<StateType, AgentStateBase> CreateStates(PlayerStateFactoryData data)
    {
        Dictionary<StateType, AgentStateBase> states = new Dictionary<StateType, AgentStateBase>
        {
            { StateType.Idle, new IdleState(data.Animator , data.MovementHandler) },
            { StateType.Move, new MoveState(data.Animator, data.MovementHandler, data.MovementInput) },
            { StateType.Jump, new JumpState(data.Animator, data.MovementHandler, data.MovementInput) },
            { StateType.Fall, new FallState(data.Animator, data.MovementHandler, data.MovementInput) },
            { StateType.Attack, new AttackState(data.Animator, data.CombatHandler, data.CombatInput, data.AttackStarter) },
            { StateType.Hit, new HitState(data.Animator, data.CombatHandler) },
            { StateType.Death, new DeathState(data.Animator, data.CombatHandler, data.MovementHandler) }
        };

        states[StateType.Idle].AddTransition(new GetHitTransition(data.Health.CurrentHealth));
        states[StateType.Idle].AddTransition(new GroundedFallTransition(data.GroundDetector));
        states[StateType.Idle].AddTransition(new IdleToMoveTransition(data.MovementInput, true));
        states[StateType.Idle].AddTransition(new JumpTransition(data.JumpInput));
        states[StateType.Idle].AddTransition(new AttackTransition(data.CombatInput , data.AttackStarter));

        states[StateType.Move].AddTransition(new GetHitTransition(data.Health.CurrentHealth));
        states[StateType.Move].AddTransition(new GroundedFallTransition(data.GroundDetector));
        states[StateType.Move].AddTransition(new IdleToMoveTransition(data.MovementInput, false));
        states[StateType.Move].AddTransition(new JumpTransition(data.JumpInput));

        states[StateType.Jump].AddTransition(new GetHitTransition(data.Health.CurrentHealth));
        states[StateType.Jump].AddTransition(new JumpFallTransition(data.GroundDetector));
        states[StateType.Jump].AddTransition(new AttackTransition(data.CombatInput , data.AttackStarter));


        states[StateType.Fall].AddTransition(new GetHitTransition(data.Health.CurrentHealth));
        states[StateType.Fall].AddTransition(new LandTransition(data.MovementInput, data.GroundDetector));
        states[StateType.Fall].AddTransition(new AttackTransition(data.CombatInput , data.AttackStarter));

        states[StateType.Attack].AddTransition(new GetHitTransition(data.Health.CurrentHealth));
        states[StateType.Attack].AddTransition(new AttackEndTransition(data.AnimationEventSource, data.CombatInput, data.GroundDetector));

        states[StateType.Hit].AddTransition(new DeathTransition(data.Health.IsDead));
        states[StateType.Hit].AddTransition(new GetHitEndTransition(data.AnimationEventSource));


        return states;
    }
}
