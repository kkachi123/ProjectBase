using UnityEngine;
[RequireComponent(typeof(PlayerInput))]
public class PlayerController : AgentController<PlayerStateBase, PlayerStateType>
{
    [Header("Player Specific Handlers")]
    [SerializeField] private PlayerInputHandler _playerInputHandler;
    [SerializeField] private PlayerHealthHandler _playerHealthHandler;

    // State Check Properties
    public bool CanJump => _stateMachine != null && _stateMachine.CurrentState.CanJump;

    protected override void Awake()
    {
        base.Awake();

        _stateMachine = new StateMachine<PlayerStateBase>();
        _states = new PlayerStateFactory().CreateStates(this);

        _playerInputHandler?
        .SetController(_moveInput, this)
        .SetCombat(_combatInput, _combatHandler);
        _playerHealthHandler?.Initialize(_health, _animationHandler , this);
    }
    private void Start()
    {
        ChangeState(PlayerStateType.Idle);
    }
    private void Update()
    {
        _stateMachine.Operate();
        _groundDetector.UpdateGroundedStatus();
    }

    public override void ChangeState(PlayerStateType type)
    {
        if(_states.TryGetValue(type, out PlayerStateBase newState))
        {
            _stateMachine.ChangeState(newState);
        }
    }

    #region Action Methods - State Operations
    public void OnAttackHitFrame()
    {
        _combatHandler.PerformAttack();
    }
    public void OnAttackAnimationEnd()
    {
        if(_stateMachine.CurrentState is AttackState attackState)
        {
            attackState.NotifyAttackEnd();
            _animationHandler.ApplyAttackAnimation(0); // Reset to default attack animation
        }
    }

    public void OnHitAnimationEnd()
    {
        if(_stateMachine.CurrentState is HitState hitState)
        {
            hitState.NotifyHitEnd();
            _animationHandler.ApplyHitAnimation(false); // Reset to default state animation
        }
    }
    #endregion
}
