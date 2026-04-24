using UnityEngine;

[RequireComponent(typeof(GroundDetector))]
public abstract class GroundedAgentController : AgentController , IGroundedAgentInputListener
{
    protected GroundDetector _groundDetector;
    protected IAgentJumpInput _jumpInput;
    public IAgentJumpInput JumpInput => _jumpInput;
    [SerializeField] protected GroundedAgentInputHandler _groundedInputHandler;

    // State Check Properties
    public bool IsGrounded => _groundDetector != null && _groundDetector.IsGrounded;

    protected override void Awake()
    {
        base.Awake();
        _groundDetector = GetComponent<GroundDetector>();

        _jumpInput = GetComponent<IAgentJumpInput>();

        _groundedInputHandler?.Initialize(this);
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
    public virtual void OnJumpAction()
    {
        _stateMachine.CurrentState?.OnInputEvent(InputKeyType.Jump);
    }
    #endregion
}
