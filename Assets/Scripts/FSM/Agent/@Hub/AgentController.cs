namespace ProjectRE
{
using System;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(AgentMotor2D), typeof(Health), typeof(AgentCombatHandler))]

/// <summary>
/// Agent 공통 컴포넌트를 초기화하고 Type 기반 FSM의 현재 상태 전환을 관리합니다.
/// </summary>
public abstract class AgentController : MonoBehaviour, IAgentAnimationListener , IAnimationEventSource , IAttackStarter
{
    [Header("Data Assets")]
    [SerializeField] protected AgentStatData _statData;
    public AgentStatData StatData => _statData;
    [SerializeField] protected AgentMotorData _motorData;

    [Header("Input Components")]
    protected IAgentMovementInput _moveInput;
    public IAgentCombatInput CombatInput { get; private set; }

    [Header("Core Components")]
    protected AgentMotor2D _motor;
    [SerializeField] protected AgentAnimationEventProxy _animationEventProxy;
    public Health Health { get; private set; }
    public event Action OnAnimationEnded;

    [Header("Handlers")]
    [SerializeField] protected AgentCombatHandler _combatHandler;
    protected AgentMovementHandler2D _movementHandler;

    [Header("State Machine")]
    protected Dictionary<Type, AgentStateBase> _states = new();
    protected AgentStateBase _currentState;

    protected virtual void Awake()
    {
        // Core Component Initialization
        _motor = GetComponent<AgentMotor2D>();
        _moveInput = GetComponent<IAgentMovementInput>();
        CombatInput = GetComponent<IAgentCombatInput>();

        Health = GetComponent<Health>();
        Health?.Initialize(_statData.maxHealth);

        // Handler Initialization
        _animationEventProxy?.Initialize(this);
        _combatHandler = GetComponent<AgentCombatHandler>();
        _combatHandler?.Initialize(_statData.attackDatas);

        _movementHandler =  new AgentMovementHandler2D(_motor, _motorData);
    }

    protected virtual void Start()
    {
        foreach (AgentStateBase state in _states.Values)
        {
            state.OnTransition += ChangeState;
        }
        ChangeState(typeof(GroundedState));
    }
    protected virtual void Update()
    {
        _currentState?.Execute(Time.deltaTime);
    }
    protected abstract void FixedUpdate();

    /// <summary>등록된 상태 타입으로 전환하며, 이전 상태를 종료한 뒤 새 상태를 진입시킵니다.</summary>
    public virtual void ChangeState(Type stateType)
    {
        if (_states.TryGetValue(stateType, out AgentStateBase newState))
        {
            _currentState?.Exit();
            _currentState = newState;
            _currentState?.Enter();
        }
        Debug.Log($"State changed to: {stateType.Name}");
    }
    
    /// <summary>Animation Event를 현재 State 또는 공용 애니메이션 종료 이벤트로 전달합니다.</summary>
    public virtual void OnAnimationEvent(AnimEventType type)
    {
        if (type == AnimEventType.OnFrame)
        {
            if (_currentState is AttackState)
                _combatHandler.PerformAttack();
            return;
        }

        if (type != AnimEventType.End)
            return;

        if (_currentState is DeathState)
        {
            OnDeathFinished();
            return;
        }

        //Attack, HitState와 DashState 등 Animation End로 종료되는 State가 공용 종료 Event를 사용한다.
        OnAnimationEnded?.Invoke();
    }
    public virtual void OnDeathFinished() { }
    public virtual bool TryStartAttack() { return false; }
}
}
