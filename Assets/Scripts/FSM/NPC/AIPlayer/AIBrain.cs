using UnityEngine;
using Unity.Behavior;
using System;
[RequireComponent (typeof(AIPlayerInput))]
[RequireComponent (typeof(JumpFloorDetector) ,typeof(PlayerDetector))]
[RequireComponent (typeof(GroundDetector), typeof(WallDetector))]

[Serializable]
public class AgentBlackboardValue
{
    [Header("component")]
    public string Input = "Input";

    [Header("Transform")]
    public string JumpFloorPos = "JumpFloorPos";
    public string PlayerPos = "PlayerPos";

    [Header("Boolean")]
    public string IsPlayerInView = "IsPlayerInView";
    public string IsGround = "IsGround";
    public string IsWallInFront = "IsWallInFront";
}

public class AIBrain : MonoBehaviour
{
    [Header("Behavior Tree")]
    [SerializeField] private BehaviorGraphAgent _agent;
    [SerializeField] private AgentBlackboardValue _blackboardValue;

    private AIPlayerInput _input;
    private GroundDetector _groundDetector;
    private JumpFloorDetector _jumpFloorDetector;
    private PlayerDetector _playerDetector;
    private WallDetector _wallDetector;

    [Header("Detector Settings")]
    [SerializeField] private AgentMotorData _motorData;
    [SerializeField] private AgentStatData _statData;
    
    private void Awake()
    {
        _input = GetComponent<AIPlayerInput>();
        _groundDetector = GetComponent<GroundDetector>();
        _jumpFloorDetector = GetComponent<JumpFloorDetector>();
        _playerDetector = GetComponent<PlayerDetector>();
        _wallDetector = GetComponent<WallDetector>();

        _jumpFloorDetector?.SetJumpParameters(_motorData.moveSpeed , _motorData.jumpForce , Mathf.Abs(Physics2D.gravity.y));

        _agent.SetVariableValue(_blackboardValue.Input, _input);
    }

    private void Update()
    {
        _agent.SetVariableValue(_blackboardValue.JumpFloorPos , _jumpFloorDetector.GetClosedGround());
        _agent.SetVariableValue(_blackboardValue.PlayerPos , _playerDetector.Target);
        _agent.SetVariableValue(_blackboardValue.IsPlayerInView , _playerDetector.IsTargetInView());
        _agent.SetVariableValue(_blackboardValue.IsGround, _groundDetector.IsGrounded);
        _agent.SetVariableValue(_blackboardValue.IsWallInFront , _wallDetector.IsWallInFront());    
    }
}
