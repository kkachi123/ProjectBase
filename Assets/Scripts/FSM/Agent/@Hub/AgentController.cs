using System;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(AgentMotor2D), typeof(Health), typeof(AgentCombatHandler))]

public abstract class AgentController : MonoBehaviour, IAgentHealthListener, IAgentInputListener, IAgentAnimationListener
{
    [Header("Data Assets")]
    [SerializeField] protected AgentStatData _statData;
    [SerializeField] protected AgentMotorData _motorData;

    [Header("Core Components")]
    protected AgentMotor2D _motor;
    protected IAgentMovementInput _moveInput;
    public IAgentCombatInput CombatInput { get; private set; }
    public Health Health { get; private set; }

    [Header("Handlers & Visuals")]
    [SerializeField] protected AgentAnimator _animator;
    [SerializeField] protected AgentAnimationEventProxy _animationEventProxy;
    [SerializeField] protected AgentCombatHandler _combatHandler;
    protected AgentMovementHandler2D _movementHandler;
    protected AgentHealthHandler _healthHandler;
    protected AgentInputHandler _inputHandler;

    [Header("State Machine")]
    protected AgentStateMachine<IAgentState> _stateMachine;
    protected Dictionary<StateType, IAgentState> _states;

    // State Check Properties
    public virtual bool IsIdle => _moveInput.GetMovementInput().sqrMagnitude < 0.0001f;


    protected virtual void Awake()
    {
        // Core Component Initialization
        _motor = GetComponent<AgentMotor2D>();
        _moveInput = GetComponent<IAgentMovementInput>();
        CombatInput = GetComponent<IAgentCombatInput>();

        Health = GetComponent<Health>();
        Health.Initialize(_statData.maxHealth);

        // Handler Initialization
        _animator?.Initialize();
        _animationEventProxy?.Initialize(this);
        _combatHandler = GetComponent<AgentCombatHandler>();
        _combatHandler?.Initialize(_statData.attackDatas);

        _movementHandler =  new AgentMovementHandler2D(_motor, _motorData);
        _healthHandler = new AgentHealthHandler(this);
        _inputHandler = new AgentInputHandler(this);

        _stateMachine = new AgentStateMachine<IAgentState>();
    }

    protected virtual void Start()
    {
        ChangeState(StateType.Idle);
    }
    protected virtual void Update()
    {
        _stateMachine.Operate();
    }
    protected virtual void FixedUpdate()
    {
        _stateMachine.FixedOperate();
    }

    public virtual void ChangeState(StateType type)
    {
        if (_states.TryGetValue(type, out IAgentState newState))
        {
            _stateMachine.ChangeState(newState);
        }
    }

    #region Action Methods - State Operations
    public virtual void Idle(bool isIdle)
    {
        _animator.SetBool(StateType.Idle, isIdle);
        if (isIdle) _movementHandler.HandleMove(Vector2.zero);
    }
    public virtual void Move(bool isMove)
    {
        _animator.SetBool(StateType.Move, isMove);
    }

    public virtual void Attack(bool isAttack)
    {
        _animator.SetBool(StateType.Attack, isAttack);
        if (isAttack) _animator.SetInteger(AnimationIntType.AttackType, _combatHandler.CurrentAttackType);
        else _animator.SetInteger(AnimationIntType.AttackType, 0); // Reset to default state animation
    }
    public virtual void Hit(bool isHit)
    {
        _animator.SetBool(StateType.Hit, isHit);
    }
    public virtual void Death(bool isDeath)
    {
        _animator.SetBool(StateType.Death, isDeath);
        if (isDeath) _movementHandler.HandleMove(Vector2.zero);
    }
    #endregion
    
    #region State Animation Event 
    public virtual void OnAttackHitFrame()
    {
        _combatHandler.PerformAttack();
    }
    public virtual void OnAnimationEvent(AnimEventType type)
    {
        _stateMachine.CurrentState.OnAnimationEvent(type);
    }
    #endregion
    
    #region IAgentHealthListener
    public virtual void OnHit() => ChangeState(StateType.Hit);

    public virtual void OnDeath() => ChangeState(StateType.Death);
    #endregion

    #region  State Input Event
    public virtual void HandleMovement()
    {
        _movementHandler.HandleMove(_moveInput.GetMovementInput());
    }
    public virtual void OnAttackAction(int attackType) { }
    #endregion
}
