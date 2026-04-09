using UnityEngine;
using System.Collections;
using Cysharp.Threading.Tasks;
using System.Threading;
[RequireComponent (typeof(AIPlayerInput), typeof(JumpFloorDetector))]
public class AIBrain : MonoBehaviour
{
    private AIPlayerInput _input;
    private JumpFloorDetector _jumpFloorDetector;
    private PlayerDetector _playerDetector;

    [Header("AI Settings")]
    [SerializeField] private float[] _attackRange = new float[3] { 1.25f, 1.45f, 0.9f };
    [SerializeField] private float _jumpThresholdY = 0.5f; // 대상이 이 높이보다 위에 있으면 점프
    [SerializeField] private float _decisionInterval = 0.1f; // 판단 주기
    [SerializeField] private AgentMotorData _motorData;
    CancellationToken token;
    private void Awake()
    {
        _input = GetComponent<AIPlayerInput>();
        _jumpFloorDetector = GetComponent<JumpFloorDetector>();
        _playerDetector = GetComponent<PlayerDetector>();
        _jumpFloorDetector?.SetJumpParameters(_motorData.moveSpeed , _motorData.jumpForce , Mathf.Abs(Physics2D.gravity.y));
    }

    private void Start()
    {
        token = CancellationTokenSource.CreateLinkedTokenSource(this.GetCancellationTokenOnDestroy()).Token;
        AILoop().Forget();
    }
    private async UniTaskVoid AILoop()
    {
        while (!token.IsCancellationRequested)
        {
            Transform ground = _jumpFloorDetector.GetClosedGround();
            if (ground != null)
            {
                await Jump(ground);
            }
            else
            {
                float patrolDir = Random.Range(0 , 2f) < 1f ? -1f : 1f; 
                float walkDuration = Random.Range(1f, 3f); 
                await Move(new Vector2(patrolDir, 0), walkDuration); // 순찰 이동
            }
            await Move(Vector2.zero, 0.1f); // 잠시 멈춤
            // 판단 주기만큼 대기
            await UniTask.Delay(System.TimeSpan.FromSeconds(_decisionInterval), cancellationToken: token);
        }
    }
    private async UniTask Move(Vector2 dir , float walkDuration)
    {
        float elapsed = 0f;
        while (elapsed < walkDuration && !token.IsCancellationRequested)
        {
            _input.Move(dir);
            elapsed += Time.deltaTime;
            await UniTask.Yield(token);
        }
    }
    private async UniTask Jump(Transform ground)
    {
        // 가까운 땅이 있다면 그 방향으로 점프 탐색
        Vector2 dirToGround = (ground.position - transform.position).normalized;
        _input.Jump(true);
        await UniTask.Yield(token);
        while (ground != null && !token.IsCancellationRequested)
        {
            _input.Move(new Vector2(dirToGround.x > 0 ? 1 : -1, 0));
            if(Mathf.Abs(transform.position.x - ground.position.x) < 0.1f)
            {
                return; 
            }
            await UniTask.Yield(token); 
        }
    }
}
