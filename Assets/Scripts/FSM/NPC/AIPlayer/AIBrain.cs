using UnityEngine;
using System.Collections;
using Cysharp.Threading.Tasks;
using System.Threading;
[RequireComponent (typeof(AIPlayerInput), typeof(JumpFloorDetector))]
public class AIBrain : MonoBehaviour
{
    private AIPlayerInput _input;
    private JumpFloorDetector _jumpFloorDetector;
    private PlayerSearchDetector _playerSearchDetector;

    [Header("AI Settings")]
    [SerializeField] private float[] _attackRange = new float[3] { 1.25f, 1.45f, 0.9f };
    [SerializeField] private float _jumpThresholdY = 0.5f; // 대상이 이 높이보다 위에 있으면 점프
    [SerializeField] private float _decisionInterval = 0.1f; // 판단 주기
    [SerializeField] private AgentMotorData _motorData;
    private void Awake()
    {
        _input = GetComponent<AIPlayerInput>();
        _jumpFloorDetector = GetComponent<JumpFloorDetector>();
        _playerSearchDetector = GetComponent<PlayerSearchDetector>();
        _jumpFloorDetector?.SetJumpParameters(_motorData.moveSpeed , _motorData.jumpForce , Mathf.Abs(Physics2D.gravity.y));
    }

    private void Start()
    {
    }


    private IEnumerator JumpAction(Transform ground)
    {
        // 가까운 땅이 있다면 그 방향으로 점프 탐색
        Vector2 dirToGround = (ground.position - transform.position).normalized;
        _input.Jump(true);
        yield return null;

        while (ground != null)
        {
            _input.Move(new Vector2(dirToGround.x > 0 ? 1 : -1, 0));

            if(Mathf.Abs(transform.position.x - ground.position.x) < 0.2f)
            {
                yield break; 
            }
            yield return null; 
        }
    }
}
