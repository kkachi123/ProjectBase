using System.Collections.Generic;

public class PlayerStateFactory 
{
    public Dictionary<StateType, PlayerStateBase> CreateStates(PlayerController playerController)
    {
        return new Dictionary<StateType, PlayerStateBase>
        {
            { StateType.Idle, new P_IdleState(playerController) },
            { StateType.Move, new P_MoveState(playerController) },
            { StateType.Jump, new P_JumpState(playerController) },
            { StateType.Fall, new P_FallState(playerController) },
            { StateType.Attack, new P_AttackState(playerController) },
            { StateType.Hit, new P_HitState(playerController) },
            { StateType.Death, new P_DeathState(playerController) },
        };
    }
}
