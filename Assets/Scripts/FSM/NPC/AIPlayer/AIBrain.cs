using UnityEngine;
using System.Collections;
using Cysharp.Threading.Tasks;
using System.Threading;
using Unity.Behavior;
[RequireComponent (typeof(AIPlayerInput), typeof(JumpFloorDetector))]
public class AIBrain : MonoBehaviour
{
    [SerializeField] private BehaviorGraphAgent _agent;

    private AIPlayerInput _input;
    private GroundDetector _groundDetector;
    private JumpFloorDetector _jumpFloorDetector;
    private PlayerDetector _playerDetector;

    [Header("AI Settings")]
    [SerializeField] private float[] _attackRange = new float[3] { 1.25f, 1.45f, 0.9f };
    [SerializeField] private float _jumpThresholdY = 0.5f; // 대상이 이 높이보다 위에 있으면 점프
    [SerializeField] private float _decisionInterval = 0.1f; // 판단 주기
    [SerializeField] private AgentMotorData _motorData;
    private void Awake()
    {
        _input = GetComponent<AIPlayerInput>();
        _groundDetector = GetComponent<GroundDetector>();
        _jumpFloorDetector = GetComponent<JumpFloorDetector>();
        _playerDetector = GetComponent<PlayerDetector>();
        _jumpFloorDetector?.SetJumpParameters(_motorData.moveSpeed , _motorData.jumpForce , Mathf.Abs(Physics2D.gravity.y));

        _agent.SetVariableValue("Input", _input);
        _agent.SetVariableValue("Jump Floor Detector", _jumpFloorDetector);
    }

    private void Update()
    {
        _agent.SetVariableValue("IsGround", _groundDetector.IsGrounded);
        _agent.SetVariableValue("JumpFloor", _jumpFloorDetector.GetClosedGround());
    }
}
