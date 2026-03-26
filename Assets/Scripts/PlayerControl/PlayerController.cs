using System.Collections.Generic;
using UniRx;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("References")]
    private PlayerStateFactory _playerStateFactory;
    private PlayerInput _playerInput;
    private PlayerMotor _playerMotor;
    [SerializeField] private PlayerAnimator _playerAnimator;
    [SerializeField] private PlayerStateData _playerStateData;
    private GroundDetector _groundDetector;


    [Header("Player Stats")]
    [SerializeField] private PlayerStatData _playerStatData;
    [SerializeField] private Health _playerHealth;

    Dictionary<PlayerStateType, IState> _playerStates;
    public PlayerInput PlayerInput => _playerInput;
    public PlayerMotor PlayerMotor => _playerMotor;
    public PlayerAnimator PlayerAnimator => _playerAnimator;
    public PlayerStateData PlayerStateData => _playerStateData;
    public bool IsGrounded => _groundDetector.IsGrounded;


    private IState _currentState;
    private void Awake()
    {
        _playerMotor = GetComponent<PlayerMotor>();
        _playerStateFactory = new PlayerStateFactory();
        _playerInput = GetComponent<PlayerInput>();
        _groundDetector = GetComponent<GroundDetector>();

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
                ChangeState(PlayerStateType.Die);
            })
            .AddTo(this);

        ChangeState(PlayerStateType.Idle);
    }
    private void Update()
    {
        _currentState?.Execute();
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

        if (_currentState == newState) return;

        _currentState?.Exit();   
        _currentState = newState;
        _currentState.Enter();  
    }
}
