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

    [Header("Player Stats")]
    [SerializeField] private PlayerStatData _playerStatData;
    [SerializeField] private Health _playerHealth;

    List<IState> _playerStates;
    public PlayerInput PlayerInput => _playerInput;
    public PlayerMotor PlayerMotor => _playerMotor;
    public PlayerAnimator PlayerAnimator => _playerAnimator;
    public PlayerStateData PlayerStateData => _playerStateData;


    private IState _currentState;
    [SerializeField] Vector2 rayBoxSize = new Vector2(0.5f, 0.1f);
    [SerializeField] Vector3 playerFootPos = new Vector3(0f, 0.6f, 0f);
    public bool IsGrounded { get; private set; }
    private void Awake()
    {
        _playerMotor = GetComponent<PlayerMotor>();
        _playerStateFactory = new PlayerStateFactory();
        _playerInput = GetComponent<PlayerInput>();
        _playerStates = new List<IState>(_playerStateFactory.CreateStates(this));

        _playerHealth.Initialize(_playerStatData.maxHealth);
        _playerAnimator.Initialize(_playerHealth);
        // 플레이어가 죽었을 때 상태를 변경하는 이벤트 구독
    }
    private void Start()
    {
        _playerHealth.IsDead
            .Pairwise() // 이전 값과 현재 값을 쌍으로 묶음 (이전, 현재)
            .Where(pair => pair.Current != pair.Previous) // 값이 실제로 변했을 때만 실행
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
        OnGround();
    }

    public void ChangeState(PlayerStateType type)
    {
        int index = (int)type;
        if (index < 0 || index >= _playerStates.Count)
        {
            Debug.LogError("Invalid state index: " + index);
            return;
        }
        IState newState = _playerStates[index];

        if (_currentState == newState) return;

        _currentState?.Exit();   
        _currentState = newState;
        _currentState.Enter();  
    }

    private void OnGround()
    {
        RaycastHit2D hit = Physics2D.BoxCast(transform.position - playerFootPos, rayBoxSize, 0f, Vector2.down, 0.1f, LayerMask.GetMask("Ground"));
        IsGrounded = hit.collider != null;

        // 공중 점프 금지
        if(!IsGrounded && _playerInput.JumpPressed) _playerInput.AfterUseJump();
    }

    private void OnDrawGizmos()
    {
        // 1. 시작 지점 계산 (코드와 동일)
        Vector3 startPos = transform.position - playerFootPos;

        // 2. 최종 도달 지점 계산
        Vector3 endPos = startPos + Vector3.down * 0.1f;

        // 3. 시작 위치의 박스 (와이어 프레임)
        Gizmos.DrawWireCube(startPos, new Vector3(rayBoxSize.x, rayBoxSize.y, 0.1f));

        // 4. 경로를 보여주는 선
        Gizmos.DrawLine(startPos, endPos);
        Gizmos.color = Color.red;
        if (IsGrounded) Gizmos.color = Color.green; // 땅에 닿으면 초록색으로 변경

        // 시작 위치에 박스 그리기
        Gizmos.DrawWireCube(endPos, new Vector3(rayBoxSize.x, rayBoxSize.y, 0.1f));
    }
}
