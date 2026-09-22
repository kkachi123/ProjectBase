using System;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(AgentMotor2D), typeof(Health), typeof(AgentCombatHandler))]

public abstract class AgentController : MonoBehaviour, IAgentAnimationListener , IAnimationEventSource , IAttackStarter
{
    [Header("Data Assets")]
    [SerializeField] protected AgentStatData _statData;
    public AgentStatData StatData => _statData;
    [SerializeField] protected AgentMotorData _motorData;
    public AgentMotorData MotorData => _motorData;

    [Header("Input Components")]
    protected IAgentMovementInput _moveInput;
    public IAgentCombatInput CombatInput { get; private set; }

    [Header("Core Components")]
    protected AgentMotor2D _motor;
    [SerializeField] protected AgentAnimator _animator;
    [SerializeField] protected AgentAnimationEventProxy _animationEventProxy;
    public Health Health { get; private set; }
    public event Action OnAnimationEnded;

    [Header("Handlers")]
    [SerializeField] protected AgentCombatHandler _combatHandler;
    protected AgentMovementHandler2D _movementHandler;

    [Header("State Machine")]
    protected Dictionary<StateType, AgentStateBase> _states = new();
    protected AgentStateBase _currentState;

    // State Check Properties
    public virtual bool IsIdle => _moveInput.GetMovementInput().sqrMagnitude < 0.0001f;


    protected virtual void Awake()
    {
        // Core Component Initialization
        _motor = GetComponent<AgentMotor2D>();
        _moveInput = GetComponent<IAgentMovementInput>();
        CombatInput = GetComponent<IAgentCombatInput>();

        Health = GetComponent<Health>();
        Health?.Initialize(_statData.maxHealth);

        // Handler Initialization
        _animator?.Initialize();
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
        ChangeState(StateType.Idle);
    }
    protected virtual void Update()
    {
        _currentState?.Execute(Time.deltaTime);
    }
    protected abstract void FixedUpdate();

    public virtual void ChangeState(StateType type)
    {
        if (_states.TryGetValue(type, out AgentStateBase newState))
        {
            _currentState?.Exit();
            _currentState = newState;
            _currentState?.Enter();
        }
        //Debug.Log($"State Changed to: {type}");
    }
    
    public virtual void OnAnimationEvent(AnimEventType type)
    {
        if(_currentState is AttackState)
        {
            if(type == AnimEventType.OnFrame)
            {
                _combatHandler.PerformAttack();
            }
            else if(type == AnimEventType.End)
            {
                OnAnimationEnded?.Invoke();
            }
        }
        else if(_currentState is HitState)
        {
            if(type == AnimEventType.End)
            {
                OnAnimationEnded?.Invoke();
            }
        }
        else if(_currentState is DeathState)
        {
            if(type == AnimEventType.End)
            {
                OnDeathFinished();
            }
        }
    }
    public virtual void OnDeathFinished() { }
    public virtual bool CanStartAttack(int requestedAttackType) { return false; }
    public virtual bool TryStartAttack(int requestedAttackType) { return false; }
}
