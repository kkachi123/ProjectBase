using System.Collections.Generic;
using UniRx;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Monothic")]
    private IAgentMovementInput _playerInput;
    private AgentMover2D _playerMover;
    [SerializeField] private PlayerAnimator _playerAnimator;
    [SerializeField] private PlayerStateData _playerStateData;
    private GroundDetector _groundDetector;

    [Header("State Controller")]
    private StateMachine<PlayerStateBase> _stateMachine;
    private PlayerStateFactory _playerStateFactory;
    Dictionary<PlayerStateType, PlayerStateBase> _playerStates;

    [Header("Player Stats")]
    [SerializeField] private PlayerStatData _playerStatData;
    [SerializeField] private Health _playerHealth;

    [Header("Controller Handler")]
    private PlayerInputHandler _playerInputHandler;

    // State Check Properties
    public bool IsGrounded => _groundDetector.IsGrounded;
    public bool IsIdle => _playerInput.GetMovementInput().magnitude < 0.01f;

    public bool CanJump => _stateMachine.CurrentState.CanJump;

    private void Awake()
    {
        _playerMover = GetComponent<AgentMover2D>();
        _playerInput = GetComponent<IAgentMovementInput>();
        _groundDetector = GetComponent<GroundDetector>();

        _stateMachine = new StateMachine<PlayerStateBase>();
        _playerStateFactory = new PlayerStateFactory();
        _playerStates = _playerStateFactory.CreateStates(this);

        _playerHealth.Initialize(_playerStatData.maxHealth);
        _playerAnimator.Initialize(_playerHealth);

        _playerInputHandler = new PlayerInputHandler(_playerInput, this);
    }
    private void Start()
    {
        _playerHealth.IsDead
            .Pairwise() // IsDead 상태의 이전 값과 현재 값을 쌍으로 만들어서 전달
            .Where(pair => pair.Current != pair.Previous) // 상태가 변경되었을 때만 반응
            .Subscribe(_ =>
            {
                Die();
            })
            .AddTo(this);

        ChangeState(PlayerStateType.Idle);
    }
    private void Update()
    {
        _stateMachine.Operate();
        _groundDetector.UpdateGroundedStatus();
    }

    public void ChangeState(PlayerStateType type)
    {
        if (!_playerStates.ContainsKey(type))
        {
            Debug.LogError("Invalid state type: " + type);
            return;
        }
        PlayerStateBase newState = _playerStates[type];
        _stateMachine.ChangeState(newState);
    }

    public void Move()
    {
        Vector2 movementInput = _playerInput.GetMovementInput();
        if(IsIdle) movementInput = Vector2.zero;

        _playerMover.Move(movementInput, _playerStateData.moveSpeed);
        _playerAnimator.ApplyMovementAnimation(movementInput);
    }

    public void Jump()
    {
        _playerMover.Jump(_playerInput.GetMovementInput(), _playerStateData.jumpForce);
        _playerAnimator.ApplyJumpingAnimation();  
    }
    public void Falling(bool isFalling)
    {
        _playerAnimator.ApplyFallingAnimation(isFalling);
    }

    private void Die()
    {
        _playerAnimator.ApplyDieAnimation();
        ChangeState(PlayerStateType.Die);
    }
}
