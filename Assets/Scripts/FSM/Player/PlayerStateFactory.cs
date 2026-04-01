using System.Collections.Generic;

public enum PlayerStateType
{
    Idle,
    Move,
    Jump,
    Fall,
    Die,
}

public class PlayerStateFactory 
{
    public Dictionary<PlayerStateType, PlayerStateBase> CreateStates(PlayerController playerController)
    {
        return new Dictionary<PlayerStateType, PlayerStateBase>
        {
            { PlayerStateType.Idle, new IdleState(playerController) },
            { PlayerStateType.Move, new MoveState(playerController) },
            { PlayerStateType.Jump, new JumpState(playerController) },
            { PlayerStateType.Fall, new FallState(playerController) },
            { PlayerStateType.Die, new DieState(playerController) },
        };
    }
}
