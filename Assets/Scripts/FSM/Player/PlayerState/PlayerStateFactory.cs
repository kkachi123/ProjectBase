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
            { StateType.Attack, new AttackState(data.Animator, data.CombatHandler) },
            { StateType.Hit, new HitState(data.Animator, data.CombatHandler) },
            { StateType.Death, new DeathState(data.Animator, data.CombatHandler, data.MovementHandler) }
        };

        states[StateType.Idle].AddTransition(new AttackTransition(data.CombatInput));
        states[StateType.Idle].AddTransition(new JumpTransition(data.JumpInput));

        return states;
    }
}
