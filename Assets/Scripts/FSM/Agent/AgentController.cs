using System;
using System.Collections.Generic;
using UnityEngine;

public abstract class AgentController<T , U> : MonoBehaviour where T : class, IState where U : Enum
{
    [Header("Data")]
    [SerializeField] protected AgentStateData _stateData;
    [SerializeField] protected AgentStatData _statData;

    [Header("Monothic")]
    [SerializeField] protected AgentAnimator _animator;
    protected IAgentMovementInput _Input;
    protected AgentMover2D _mover;
    protected GroundDetector _groundDetector;

    [Header("Player Stats")]
    protected Health _health;

    [Header("Handlers")]
    [SerializeField] protected AgentAnimationHandler _animationHandler;
    [SerializeField] protected AgentMovementHandler _movementHandler;

    [Header("State Controller")]
    protected StateMachine<T> _stateMachine;
    protected Dictionary<U, T> _states;

    // State Check Properties
    public bool IsGrounded => _groundDetector.IsGrounded;
    public bool IsIdle => _Input.GetMovementInput().magnitude < 0.01f;

    public abstract void ChangeState(U type);

    #region State Operations
    public void Move()
    {
        Vector2 moveVec = IsIdle ? Vector2.zero : _Input.GetMovementInput();
        _movementHandler.HandleMove(moveVec);
        _animationHandler.ApplyMovementAnimation(moveVec);
    }

    public void Jump()
    {
        _movementHandler.HandleJump(_Input.GetMovementInput());
        _animationHandler.ApplyJumpingAnimation();
    }
    public void Falling(bool isFalling) => _animationHandler.ApplyFallingAnimation(isFalling);
    #endregion
}
