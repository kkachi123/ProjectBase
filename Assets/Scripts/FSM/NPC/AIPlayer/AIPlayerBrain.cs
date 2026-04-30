using UnityEngine;
using Unity.Behavior;
using System;
using System.Collections.Generic;
[RequireComponent(typeof(AIPlayerInput))]
[RequireComponent(typeof(JumpFloorDetector), typeof(PlayerDetector))]
[RequireComponent(typeof(GroundDetector), typeof(WallDetector))]

[Serializable]
public class AIPlayerBlackboardValue
{
    [Header("Component")]
    public string Input = "Input";
    public string WallDetector = "WallDetector";

    [Header("Transform")]
    public string JumpFloorPos = "JumpFloorPos";
    public string PlayerPos = "PlayerPos";

    [Header("Boolean")]
    public string IsPlayerInView = "IsPlayerInView";
    public string IsGround = "IsGround";

    [Header("Integer")]
    public string AttackTypeCount = "AttackTypeCount";
}

public class AIPlayerBrain : MonoBehaviour
{
    [Header("Behavior Tree")]
    [SerializeField] private BehaviorGraphAgent _agent;
    [SerializeField] private AIPlayerBlackboardValue _blackboardValue;

    [Header("Detector Settings")]
    [SerializeField] private AIPlayerInput _input;
    [SerializeField] private GroundDetector _groundDetector;
    [SerializeField] private JumpFloorDetector _jumpFloorDetector;
    [SerializeField] private PlayerDetector _playerDetector;
    [SerializeField] private WallDetector _wallDetector;
    [Header("Datas")]
    [SerializeField] private AgentMotorData _motorData;
    [SerializeField] private AgentStatData _statData;

    private void Awake()
    {
        _input = GetComponent<AIPlayerInput>();
        _groundDetector = GetComponent<GroundDetector>();
        _jumpFloorDetector = GetComponent<JumpFloorDetector>();
        _playerDetector = GetComponent<PlayerDetector>();
        _wallDetector = GetComponent<WallDetector>();

        _jumpFloorDetector?.SetJumpParameters(_motorData.moveSpeed, _motorData.jumpForce, Mathf.Abs(Physics2D.gravity.y));

        _agent.SetVariableValue(_blackboardValue.Input, _input);
        _agent.SetVariableValue(_blackboardValue.WallDetector, _wallDetector);
        _agent.SetVariableValue(_blackboardValue.AttackTypeCount, _statData.attackDatas.Count - 1);
    }

    private void Update()
    {
        _agent.SetVariableValue(_blackboardValue.JumpFloorPos, _jumpFloorDetector.GetClosedGround());
        _agent.SetVariableValue(_blackboardValue.PlayerPos, _playerDetector.Target);
        _agent.SetVariableValue(_blackboardValue.IsPlayerInView, _playerDetector.IsTargetInView());
        _agent.SetVariableValue(_blackboardValue.IsGround, _groundDetector.IsGrounded);
    }
}
