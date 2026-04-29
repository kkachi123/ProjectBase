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

    [Header("List")]
    public string AttackRanges = "AttackRanges";

    [Header("Float")]
    public string maxAttackRange = "MaxAttackRange";
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
       
        UpdateAttackRange();
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

    private void UpdateAttackRange()
    {
        List<float> _attackRanges = new List<float>();

        float maxRange = 0f;
        for (int i = 0; i < _statData.attackDatas.Count; i++)
        {
            float range = Calculators.CalcAttackRange(_statData.attackDatas[i].offset, _statData.attackDatas[i].size);
            _attackRanges.Add(range);
            maxRange = Mathf.Max(maxRange, range);
        }
        _agent.SetVariableValue(_blackboardValue.AttackRanges, _attackRanges);
        _agent.SetVariableValue(_blackboardValue.maxAttackRange, maxRange);
    }
}
