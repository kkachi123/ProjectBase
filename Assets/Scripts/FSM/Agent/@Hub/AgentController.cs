namespace ProjectRE
{
using System;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Health))]

/// <summary>
/// Agent 공통 컴포넌트 초기화 및 Type 기반 FSM 상태 전환 관리.
/// </summary>
public abstract class AgentController : MonoBehaviour, IAgentAnimationListener , IAnimationEventSource
{
    [Header("Data Assets")]
    [SerializeField] protected AgentStatData _statData;
    public AgentStatData StatData => _statData;

    [Header("Core Components")]
    [SerializeField] protected AgentAnimationEventProxy _animationEventProxy;
    public Health Health { get; private set; }
    public event Action OnAnimationEnded;

    [Header("State Machine")]
    protected Dictionary<Type, AgentStateBase> _states = new();
    protected AgentStateBase _currentState;

    protected virtual void Awake()
    {
        // Core Component Initialization
        Health = GetComponent<Health>();
        Health?.Initialize(_statData.maxHealth);

        // Handler Initialization
        _animationEventProxy?.Initialize(this);
    }

    protected virtual void Start()
    {
        foreach (AgentStateBase state in _states.Values)
        {
            state.OnTransition += ChangeState;
        }
    }
    protected virtual void Update()
    {
        _currentState?.Execute(Time.deltaTime);
    }
    protected abstract void FixedUpdate();

    /// <summary>오브젝트 파괴 시 현재 State 구독과 상태 전환 연결 해제.</summary>
    protected virtual void OnDestroy()
    {
        AgentStateBase exitingState = _currentState;
        _currentState = null;

        try
        {
            exitingState?.Exit();
        }
        finally
        {
            foreach (AgentStateBase state in _states.Values)
                state.OnTransition -= ChangeState;
        }
    }

    /// <summary>등록된 상태 타입으로 전환. 이전 State 종료 후 새 State 진입.</summary>
    public virtual void ChangeState(Type stateType)
    {
        if (_states.TryGetValue(stateType, out AgentStateBase newState))
        {
            _currentState?.Exit();
            _currentState = newState;
            _currentState?.Enter();
        }
        //Debug.Log($"State changed to: {stateType.Name}");
    }
    
    /// <summary>Animation Event를 현재 State 동작 또는 공용 종료 이벤트로 전달.</summary>
    public virtual void OnAnimationEvent(AnimEventType type, int intParameter = 0)
    {
        if (type == AnimEventType.OnFrame)
        {
            if (_currentState is AttackState attackState)
                attackState.OnAttackFrame(intParameter);
            return;
        }

        if (type != AnimEventType.End)
            return;

        if (_currentState is DeathState)
        {
            OnDeathFinished();
            return;
        }

        // Animation End 수신 시 공용 종료 이벤트 발행.
        OnAnimationEnded?.Invoke();
    }
    public virtual void OnDeathFinished() { }
}
}
