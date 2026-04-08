using UnityEngine;
public class PlayerController : AgentController<PlayerStateBase> , IPlayerAnimationListener
{
    [SerializeField] private PlayerAnimationEventProxy _animationEventProxy;
    protected override void Awake()
    {
        base.Awake();

        _stateMachine = new StateMachine<PlayerStateBase>();
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
        // 2. 물리 연산 주기와 맞춰서 지면 상태 업데이트
        _groundDetector.UpdateGroundedStatus();

        // (선택 사항) 만약 물리적인 이동 처리를 State에서 하고 있다면 
        // 물리 관련 로직만 따로 FixedOperate() 같은 메서드를 만들어 호출하기도 합니다.
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
        if (!IsGrounded) attackType = 3;
        _combatHandler.SetAttackType(attackType);
        _stateMachine.CurrentState?.OnInputEvent(InputKeyType.Attack);
    }

    #endregion
}
