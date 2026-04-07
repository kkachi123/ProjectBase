using System;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(AgentMover2D))]
[RequireComponent(typeof(GroundDetector))]
[RequireComponent(typeof(Health))]
public abstract class AgentController<T> : 
    MonoBehaviour , IAgentHealthListener , IAgentInputListener
    where T : class, IState
{
    [Header("Data Assets")]
    [SerializeField] protected AgentMotorData _motorData;
    [SerializeField] protected AgentStatData _statData;

    [Header("Core Components")]
    protected IAgentMovementInput _moveInput;
    protected IAgentCombatInput _combatInput;
    public IAgentMovementInput MoveInput => _moveInput;
    public IAgentCombatInput CombatInput => _combatInput;
    protected AgentMover2D _mover;
    protected GroundDetector _groundDetector;
    protected Health _health;
    public Health Health => _health;

    [Header("Handlers & Visuals")]
    [SerializeField] protected AgentAnimator _animator;
    [SerializeField] protected AgentAnimationHandler _animationHandler;
    [SerializeField] protected AgentMovementHandler _movementHandler;
    [SerializeField] protected AgentCombatHandler _combatHandler;
    [SerializeField] protected AgentHealthHandler _healthHandler;
    [SerializeField] protected AgentInputHandler _inputHandler;

    [Header("State Machine")]
    protected StateMachine<T> _stateMachine;
    protected Dictionary<StateType, T> _states;

    // State Check Properties
    public bool IsGrounded => _groundDetector != null && _groundDetector.IsGrounded;
    public bool IsIdle => _moveInput.GetMovementInput().sqrMagnitude < 0.0001f;


    protected virtual void Awake()
    {
        // Core Component Initialization
        _mover = GetComponent<AgentMover2D>();
        _moveInput = GetComponent<IAgentMovementInput>();
        _combatInput = GetComponent<IAgentCombatInput>();
        _groundDetector = GetComponent<GroundDetector>();

        _health = GetComponent<Health>();
        _health.Initialize(_statData.maxHealth);

        // Handler Initialization
        _animator?.Initialize();
        _animationHandler?.Initialize(_animator);
        _movementHandler?.Initialize(_mover, _motorData);
        _combatHandler?.Initialize(_statData.attackDamage);
        _healthHandler?.Initialize(this);
        _inputHandler?.Initialize(this);
    }

    public abstract void ChangeState(StateType type);

    #region Action Methods - State Operations
    public virtual void Move(bool isMove)
    {
        Vector2 moveVec = isMove ? _moveInput.GetMovementInput() : Vector2.zero;
        _movementHandler.HandleMove(moveVec);
        _animationHandler.ApplyMovementAnimation(isMove);
    }

    public virtual void Jump()
    {
        _movementHandler.HandleJump(_moveInput.GetMovementInput());
        _animationHandler.ApplyJumpingAnimation();
    }
    public virtual void Falling(bool isFalling)
    {
        _animationHandler.ApplyFallingAnimation(isFalling);
    }

    public virtual void Attack()
    {
        _animationHandler.ApplyAttackAnimation(_combatHandler.CurrentAttackType);
    }

    #endregion
    public virtual void OnHit()
    {
        _animationHandler.ApplyHitAnimation(true);
        ChangeState(StateType.Hit);
    }

    public virtual void OnDeath()
    {
        _animationHandler.ApplyDeathAnimation();
        ChangeState(StateType.Death);
    }

    public virtual void OnJumpAction() { }

    public virtual void OnAttackAction(int attackType) { }
}
