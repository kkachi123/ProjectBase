public class PlayerController : AgentController<PlayerStateBase>
{
    protected override void Awake()
    {
        base.Awake();

        _stateMachine = new StateMachine<PlayerStateBase>();
        _states = new PlayerStateFactory().CreateStates(this);

    }
    private void Start()
    {
        ChangeState(StateType.Idle);
    }
    private void Update()
    {
        _stateMachine.Operate();
        _groundDetector.UpdateGroundedStatus();
    }

    public override void ChangeState(StateType type)
    {
        if(_states.TryGetValue(type, out PlayerStateBase newState))
        {
            _stateMachine.ChangeState(newState);
        }
    }

    #region State Animation Event 
    public void OnAttackHitFrame()
    {
        _combatHandler.PerformAttack();
    }
    public void OnAttackAnimationEnd()
    {
        _stateMachine.CurrentState.OnAnimationEvent(AnimEventType.End);
        _animationHandler.ApplyAttackAnimation(0); // Reset to default state animation
    }
    public void OnHitAnimationEnd()
    {
        _stateMachine.CurrentState.OnAnimationEvent(AnimEventType.End);
        _animationHandler.ApplyHitAnimation(false); // Reset to default state animation
    }
    #endregion

    #region State Input Methods - Called by Input Handler
    public override void OnJumpAction()
    {
        _stateMachine.CurrentState?.OnInputEvent(InputKeyType.Jump);
    }
    public override void OnAttackAction(int attackType)
    {
        _combatHandler.SetAttackType(attackType);
        _stateMachine.CurrentState?.OnInputEvent(InputKeyType.Attack);
    }

    #endregion
}
