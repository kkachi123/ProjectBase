using System;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(GroundDetector))]
[RequireComponent(typeof(Health))]
public abstract class AgentController : MonoBehaviour, IAgentHealthListener, IAgentInputListener, IAgentAnimationListener
{
    [Header("Data Assets")]
    [SerializeField] protected AgentMotorData _motorData;
    [SerializeField] protected AgentStatData _statData;

    [Header("Core Components")]
    protected IAgentMovementInput _moveInput;
    protected IAgentCombatInput _combatInput;
    public IAgentMovementInput MoveInput => _moveInput;
    public IAgentCombatInput CombatInput => _combatInput;
    protected AgentMotor2D _motor;
    protected GroundDetector _groundDetector;
    protected Health _health;
    public Health Health => _health;

    [Header("Handlers & Visuals")]
    [SerializeField] protected AgentAnimator _animator;
    [SerializeField] protected AgentAnimationEventProxy _animationEventProxy;
    [SerializeField] protected AgentMovementHandler2D _movementHandler;
    [SerializeField] protected AgentCombatHandler _combatHandler;
    [SerializeField] protected AgentHealthHandler _healthHandler;
    [SerializeField] protected AgentInputHandler _inputHandler;

    [Header("State Machine")]
    protected AgentStateMachine<IAgentState> _stateMachine;
    protected Dictionary<StateType, IAgentState> _states;

    // State Check Properties
    public bool IsGrounded => _groundDetector != null && _groundDetector.IsGrounded;
    public bool IsIdle => _moveInput.GetMovementInput().sqrMagnitude < 0.0001f;


    protected virtual void Awake()
    {
        // Core Component Initialization
        _motor = GetComponent<AgentMotor2D>();
        _moveInput = GetComponent<IAgentMovementInput>();
        _combatInput = GetComponent<IAgentCombatInput>();
        _groundDetector = GetComponent<GroundDetector>();

        _health = GetComponent<Health>();
        _health.Initialize(_statData.maxHealth);

        // Handler Initialization
        _animator?.Initialize();
        _animationEventProxy?.Initialize(this);
        _movementHandler?.Initialize(_motor, _motorData);
        _combatHandler?.Initialize(_statData.attackDatas);
        _healthHandler?.Initialize(this);
        _inputHandler?.Initialize(this);

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
        _groundDetector.UpdateGroundedStatus();
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

    public virtual void Jump(bool isJump)
    {
        _animator.SetBool(StateType.Jump, isJump);
        if (isJump) _movementHandler.HandleJump();
    }
    public virtual void Falling(bool isFalling)
    {
        _animator.SetBool(StateType.Fall, isFalling);
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
    }
    #endregion
    
    public virtual void HandleMovement()
    {
        _movementHandler.HandleMove(_moveInput.GetMovementInput());
    }
    #region IAgentHealthListener
    public virtual void OnHit() => ChangeState(StateType.Hit);

    public virtual void OnDeath() => ChangeState(StateType.Death);
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

    #region  State Input Event
    public virtual void OnJumpAction()
    {
        _stateMachine.CurrentState?.OnInputEvent(InputKeyType.Jump);
    }

    public virtual void OnAttackAction(int attackType) { }
    #endregion
}
