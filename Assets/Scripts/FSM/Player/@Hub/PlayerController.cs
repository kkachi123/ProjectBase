using UnityEngine;
public class PlayerController : AgentController<PlayerStateBase> , IPlayerAnimationListener
{
    [SerializeField] private PlayerAnimationEventProxy _animationEventProxy;
    protected override void Awake()
    {
        base.Awake();

        _stateMachine = new AgentStateMachine<PlayerStateBase>();
        _states = new PlayerStateFactory().CreateStates(this);

        _animationEventProxy?.Initialize(this);
    }
    private void Start()
    {
        ChangeState(StateType.Idle);
    }
    private void Update()
    {
        _stateMachine.Operate();
    }
    private void FixedUpdate()
    {
        _groundDetector.UpdateGroundedStatus();
        _stateMachine.FixedOperate();
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
    }
    #endregion

    #region State Input Methods - Called by Input Handler
    public override void OnJumpAction()
    {
        _stateMachine.CurrentState?.OnInputEvent(InputKeyType.Jump);
    }
    public override void OnAttackAction(int attackType)
    {
        if (!IsGrounded) attackType = 3;
        _combatHandler.SetAttackType(attackType);
        _stateMachine.CurrentState?.OnInputEvent(InputKeyType.Attack);
    }

    #endregion
}
