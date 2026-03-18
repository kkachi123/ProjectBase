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
    public List<IState> CreateStates(PlayerController playerController)
    {
        return new List<IState>
        {
            new IdleState(playerController),
            new MoveState(playerController),
            new JumpState(playerController),
            new FallState(playerController),
            new DieState(playerController),
        };
    }
}
