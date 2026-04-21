using UnityEngine;
using Unity.Behavior;
using System;
using System.Collections.Generic;
[RequireComponent(typeof(AIPlayerInput))]
[RequireComponent(typeof(JumpFloorDetector), typeof(PlayerDetector))]
[RequireComponent(typeof(GroundDetector), typeof(WallDetector))]

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

    [Header("List")]
    public string AttackRanges = "AttackRanges";

    [Header("Component")]
    public string WallDetector = "WallDetector";
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

        _jumpFloorDetector?.SetJumpParameters(_motorData.moveSpeed, _motorData.jumpForce, Mathf.Abs(Physics2D.gravity.y));

        _agent.SetVariableValue(_blackboardValue.Input, _input);
        List<float> attackRanges = new List<float>();
        for (int i = 0; i < _statData.attackDatas.Count; i++)
        {
            float range = CalcAttackRange(_statData.attackDatas[i].offset, _statData.attackDatas[i].size);
            attackRanges.Add(range);
        }
        _agent.SetVariableValue(_blackboardValue.AttackRanges, attackRanges);
        _agent.SetVariableValue(_blackboardValue.WallDetector, _wallDetector);
    }

    private void Update()
    {
        _agent.SetVariableValue(_blackboardValue.JumpFloorPos, _jumpFloorDetector.GetClosedGround());
        _agent.SetVariableValue(_blackboardValue.PlayerPos, _playerDetector.Target);
        _agent.SetVariableValue(_blackboardValue.IsPlayerInView, _playerDetector.IsTargetInView());
        _agent.SetVariableValue(_blackboardValue.IsGround, _groundDetector.IsGrounded);
    }

    private float CalcAttackRange(Vector2 offset, Vector2 size)
    {
        // 공격 범위는 offset과 size의 대각선 길이의 합으로 계산
        return Mathf.Sqrt(offset.x * offset.x + offset.y * offset.y) + Mathf.Sqrt(size.x * size.x + size.y * size.y) / 2f;
    }
}
