using UnityEngine;
public class PlayerController : AgentController
{
    protected override void Awake()
    {
        base.Awake();

        _states = new PlayerStateFactory().CreateStates(this);
    }

    #region Action Methods - State Operations

    public override void Attack(bool isAttack)
    {
        base.Attack(isAttack);
        if(IsGrounded) _movementHandler.HandleMove(Vector2.zero); 
    }

    #endregion

    #region IAgentHealthListener
    public override void OnHit(Vector2 dir)
    {
        _movementHandler.HandleKnockback(dir);
        base.OnHit(dir);
    }
    #endregion


    #region State Input Event
    public override void OnAttackAction(int attackType)
    {
        if (!IsGrounded) attackType = 3;
        _combatHandler.SetAttackType(attackType);
        _stateMachine.CurrentState?.OnInputEvent(InputKeyType.Attack);
    }

    #endregion
}
