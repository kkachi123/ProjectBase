using System.Collections.Generic;

public enum StateType
{
    Idle,
    Move,
    Jump,
    Fall,
    Attack,
    Hit,
    Death,
}

public class PlayerStateFactory 
{
    public Dictionary<StateType, PlayerStateBase> CreateStates(PlayerController playerController)
    {
        return new Dictionary<StateType, PlayerStateBase>
        {
            { StateType.Idle, new IdleState(playerController) },
            { StateType.Move, new MoveState(playerController) },
            { StateType.Jump, new JumpState(playerController) },
            { StateType.Fall, new FallState(playerController) },
            { StateType.Attack, new AttackState(playerController) },
            { StateType.Hit, new HitState(playerController) },
            { StateType.Death, new DeathState(playerController) },
        };
    }
}
