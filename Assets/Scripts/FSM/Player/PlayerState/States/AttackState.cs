using UnityEngine;

public class AttackState : PlayerStateBase
{
    private bool _isAttackFinished;
    public AttackState(PlayerController player) : base(player) { _player = player; }
    public override void Enter()
    {
        Debug.Log("AttackState Entered");
        _isAttackFinished = false;
        _player.Attack();
    }
    public override void Execute()
    {
        if(_isAttackFinished)
        {
            _player.ChangeState(PlayerStateType.Idle);
        }
    }
    public override void Exit() { }

    public void NotifyAttackEnd()
    {
        _isAttackFinished = true;
    }
}
