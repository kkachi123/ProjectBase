using UnityEngine;

public class PlayerController : AgentController<PlayerStateBase, PlayerStateType>
{
    // PlayerHandlers
    [SerializeField] private PlayerInputHandler _playerInputHandler;
    [SerializeField] private PlayerHealthHandler _playerHealthHandler;
    //[SerializeField] private PlayerCombatHandler _playerCombatHandler; // 전투 로직 핸들러 (추후 추가 예정)

    // State Check Properties
    public bool CanJump => _stateMachine.CurrentState.CanJump;

    private void Awake()
    {
        _mover = GetComponent<AgentMover2D>();
        _Input = GetComponent<IAgentMovementInput>();
        _groundDetector = GetComponent<GroundDetector>();

        _stateMachine = new StateMachine<PlayerStateBase>();
        _states = new PlayerStateFactory().CreateStates(this);

        _health = GetComponent<Health>();
        _health.Initialize(_statData.maxHealth);
        _animator.Initialize();

        _playerInputHandler.Initialize(_Input, this);
        _animationHandler.Initialize(_animator);
        _playerHealthHandler.Initialize(_health, _animationHandler , this);
        _movementHandler.Initialize(_mover , _stateData);
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
        if (!_states.ContainsKey(type)) return;
        _stateMachine.ChangeState(_states[type]);
    }
}
