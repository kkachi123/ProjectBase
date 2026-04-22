using UnityEngine;

public class M_MoveState : MonsterStateBase
{
    public M_MoveState(MonsterController monster) : base(monster) { }

    public override void Enter()
    {
        _monster.Move(true);
    }

    public override void Execute()
    {
        if (_monster.IsIdle) _monster.ChangeState(StateType.Idle);
    }

    public override void FixedExecute()
    {
        _monster.HandleMovement();
    }

    public override void Exit() 
    {
        _monster.Move(false);
    }

    public override void OnInputEvent(InputKeyType type)
    {
        if (!_monster.IsGrounded) return;

        switch (type)
        {
            case InputKeyType.Attack:
                _monster.ChangeState(StateType.Attack);
                break;
        }
    }
}
