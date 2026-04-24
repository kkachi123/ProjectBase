using System.Collections.Generic;

public class PlayerStateFactory 
{
    public Dictionary<StateType, IAgentState> CreateStates(PlayerController playerController)
    {
        return new Dictionary<StateType, IAgentState>
        {
            { StateType.Idle, new GroundedIdleState(playerController) },
            { StateType.Move, new GroundedMoveState(playerController) },
            { StateType.Jump, new JumpState(playerController) },
            { StateType.Fall, new FallState(playerController) },
            { StateType.Attack, new AttackState(playerController) },
            { StateType.Hit, new HitState(playerController) },
            { StateType.Death, new DeathState(playerController) },
        };
    }
}
