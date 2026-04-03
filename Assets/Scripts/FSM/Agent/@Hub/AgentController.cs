using System;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(AgentMover2D))]
[RequireComponent(typeof(GroundDetector))]
[RequireComponent(typeof(Health))]
public abstract class AgentController<T , U> : MonoBehaviour where T : class, IState where U : Enum
{
    [Header("Data Assets")]
    [SerializeField] protected AgentStateData _stateData;
    [SerializeField] protected AgentStatData _statData;

    [Header("Core Components")]
    protected IAgentMovementInput _moveInput;
    protected IAgentCombatInput _combatInput;
    protected AgentMover2D _mover;
    protected GroundDetector _groundDetector;

    [Header("Player Stats")]
    protected Health _health;

    [Header("Handlers & Visuals")]
    [SerializeField] protected AgentAnimator _animator;
    [SerializeField] protected AgentAnimationHandler _animationHandler;
    [SerializeField] protected AgentMovementHandler _movementHandler;
    [SerializeField] protected AgentCombatHandler _combatHandler;

    [Header("State Machine")]
    protected StateMachine<T> _stateMachine;
    protected Dictionary<U, T> _states;

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
        _movementHandler?.Initialize(_mover , _stateData);
        _combatHandler?.Initialize(_statData.attackDamage);
    }

    public abstract void ChangeState(U type);

    #region Action Methods - State Operations
    public virtual void Move(bool isMove)
    {
        Vector2 moveVec = isMove ? _moveInput.GetMovementInput() : Vector2.zero;
        _movementHandler.HandleMove(moveVec);
        _animationHandler.ApplyMovementAnimation(moveVec);
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
}
