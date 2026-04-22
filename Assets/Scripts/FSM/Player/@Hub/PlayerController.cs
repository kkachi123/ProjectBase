public class PlayerController : AgentController<PlayerStateBase>
{
    protected override void Awake()
    {
        base.Awake();

        _stateMachine = new AgentStateMachine<PlayerStateBase>();
        _states = new PlayerStateFactory().CreateStates(this);
    }

    #region State Input Methods - Called by Input Handler
    public override void OnAttackAction(int attackType)
    {
        if (!IsGrounded) attackType = 3;
        _combatHandler.SetAttackType(attackType);
        _stateMachine.CurrentState?.OnInputEvent(InputKeyType.Attack);
    }

    #endregion
}
