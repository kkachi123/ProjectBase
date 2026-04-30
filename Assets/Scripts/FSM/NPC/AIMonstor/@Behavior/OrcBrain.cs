using UnityEngine;
using Unity.Behavior;
using System;
using System.Collections.Generic;
[Serializable]
public class AIMonsterBlackboardValue
{
    [Header("component")]
    public string Input = "Input";

    [Header("Transform")]
    public string PlayerPos = "PlayerPos";

    [Header("Boolean")]
    public string IsLeftGrounded = "IsLeftGrounded";
    public string IsRightGrounded = "IsRightGrounded";
    public string IsPlayerInView = "IsPlayerInView";

    [Header("Integer")]
    public string AttackTypeCount = "AttackTypeCount";
    [Header("Float")]
    public string MaxAttackRange = "MaxAttackRange";
}

[RequireComponent(typeof(AIMonsterInput))]
public class OrcBrain : MonoBehaviour
{
    [Header("Behavior Tree")]
    [SerializeField] private BehaviorGraphAgent _agent;
    [SerializeField] private AIMonsterBlackboardValue _blackboardValue;

    [Header("Detector Settings")]
    AIMonsterInput _input;
    [SerializeField] private GroundDetector _leftGroundDetector;
    [SerializeField] private GroundDetector _rightGroundDetector;
    [SerializeField] private PlayerDetector _playerDetector;
    [SerializeField] private AgentStatData _statData;
    private void Start()
    {
        _input = GetComponent<AIMonsterInput>();
        _playerDetector = GetComponent<PlayerDetector>();
        _agent.SetVariableValue(_blackboardValue.Input, _input);
        _agent.SetVariableValue(_blackboardValue.AttackTypeCount, _statData.attackDatas.Count);
        
        float maxAttackRange = 0f;
        foreach (var attackData in _statData.attackDatas)
        {
            float attackRange = Calculators.CalcAttackRange(attackData.offset, attackData.size);
            if (attackRange > maxAttackRange) maxAttackRange = attackRange;
        }
        _agent.SetVariableValue(_blackboardValue.MaxAttackRange, maxAttackRange);
    }


    private void Update()
    {
        _leftGroundDetector.UpdateGroundedStatus();
        _rightGroundDetector.UpdateGroundedStatus();

        _agent.SetVariableValue(_blackboardValue.IsLeftGrounded, _leftGroundDetector.IsGrounded);
        _agent.SetVariableValue(_blackboardValue.IsRightGrounded, _rightGroundDetector.IsGrounded);

        _agent.SetVariableValue(_blackboardValue.PlayerPos, _playerDetector.Target);
        _agent.SetVariableValue(_blackboardValue.IsPlayerInView, _playerDetector.IsTargetInView());
    }
}
