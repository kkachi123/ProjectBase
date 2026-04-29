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
    public string IsFrontGrounded = "IsFrontGrounded";
    public string IsPlayerInView = "IsPlayerInView";

    [Header("List")]
    public string AttackRanges = "AttackRanges";

    [Header("Integer")]
    public string Direction = "Direction";

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
    public bool IsFrontGrounded =>
    _leftGroundDetector != null && _direction == -1 && _leftGroundDetector.IsGrounded
    || _rightGroundDetector != null && _direction == 1 && _rightGroundDetector.IsGrounded;

    [SerializeField] private AgentStatData _statData;
    [SerializeField] private int _direction;
    private List<float> _attackRanges;
    private void Start()
    {
        _input = GetComponent<AIMonsterInput>();
        _playerDetector = GetComponent<PlayerDetector>();
        _agent.SetVariableValue(_blackboardValue.Input, _input);
        _attackRanges = new List<float>(_statData.attackDatas.Count);
        UpdateAttackRange();
    }


    private void Update()
    {
        _leftGroundDetector.UpdateGroundedStatus();
        _rightGroundDetector.UpdateGroundedStatus();

        _agent.SetVariableValue(_blackboardValue.PlayerPos, _playerDetector.Target);
        _agent.SetVariableValue(_blackboardValue.IsPlayerInView, _playerDetector.IsTargetInView());
        _agent.SetVariableValue(_blackboardValue.IsFrontGrounded, IsFrontGrounded);
        _agent.SetVariableValue(_blackboardValue.Direction, _direction);
    }

    private void UpdateAttackRange()
    {
        float maxRange = 0f;
        for (int i = 0; i < _attackRanges.Count; i++)
        {
            float range = Calculators.CalcAttackRange(_statData.attackDatas[i].offset, _statData.attackDatas[i].size);
            _attackRanges[i] = range;
            maxRange = Mathf.Max(maxRange, range);
        }
        _agent.SetVariableValue(_blackboardValue.AttackRanges, _attackRanges);
        _agent.SetVariableValue(_blackboardValue.maxAttackRange, maxRange);
    }
}
