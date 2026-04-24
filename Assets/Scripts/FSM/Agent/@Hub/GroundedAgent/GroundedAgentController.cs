using UnityEngine;

[RequireComponent(typeof(GroundDetector))]
public abstract class GroundedAgentController : AgentController
{
    protected GroundDetector _groundDetector;

    // State Check Properties
    public bool IsGrounded => _groundDetector != null && _groundDetector.IsGrounded;

    protected override void Awake()
    {
        base.Awake();
        _groundDetector = GetComponent<GroundDetector>();
    }

    protected override void FixedUpdate()
    {
        _groundDetector.UpdateGroundedStatus();
        base.FixedUpdate();
    }

    #region Action Methods - State Operations
    public virtual void Jump(bool isJump)
    {
        _animator.SetBool(StateType.Jump, isJump);
        if (isJump) _movementHandler.HandleJump();
    }

    public virtual void Falling(bool isFalling)
    {
        _animator.SetBool(StateType.Fall, isFalling);
    }
    #endregion

    #region State Input Event
    public override void OnJumpAction()
    {
        _stateMachine.CurrentState?.OnInputEvent(InputKeyType.Jump);
    }
    #endregion
}
