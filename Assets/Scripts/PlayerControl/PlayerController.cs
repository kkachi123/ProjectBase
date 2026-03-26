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
    private StateMachine _stateMachine;
    private PlayerStateFactory _playerStateFactory;
    Dictionary<PlayerStateType, IState> _playerStates;

    [Header("Player Stats")]
    [SerializeField] private PlayerStatData _playerStatData;
    [SerializeField] private Health _playerHealth;

    public PlayerAnimator PlayerAnimator => _playerAnimator;
    public bool IsGrounded => _groundDetector.IsGrounded;

    private void Awake()
    {
        _playerMover = GetComponent<AgentMover2D>();
        _playerInput = GetComponent<IAgentMovementInput>();
        _groundDetector = GetComponent<GroundDetector>();

        _stateMachine = new StateMachine();
        _playerStateFactory = new PlayerStateFactory();
        _playerStates = _playerStateFactory.CreateStates(this);

        _playerHealth.Initialize(_playerStatData.maxHealth);
        _playerAnimator.Initialize(_playerHealth);
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
        IState newState = _playerStates[type];
        _stateMachine.ChangeState(newState);
    }

    public void Move()
    {
        bool isIdle = _playerInput.GetMovementInput().magnitude < 0.01f;
        if (IsGrounded && isIdle)
        {
            ChangeState(PlayerStateType.Idle);
            _playerMover.Move(Vector2.zero, 0);
        }
        else
        {
            if (IsGrounded) ChangeState(PlayerStateType.Move);
            _playerMover.Move(_playerInput.GetMovementInput(), _playerStateData.moveSpeed);
            _playerAnimator.ApplyMovementAnimation(_playerInput.GetMovementInput());
        }
    }

    public void Jump()
    {
        if (!_playerInput.IsJumpPressed()) return;
        if (!IsGrounded) return;
        ChangeState(PlayerStateType.Jump);
        _playerMover.Jump(_playerInput.GetMovementInput(), _playerStateData.jumpForce);
        _playerAnimator.ApplyJumpingAnimation(true, false);
    }

    private void Die()
    {
        _playerAnimator.ApplyDieAnimation();
        ChangeState(PlayerStateType.Die);
    }
}
