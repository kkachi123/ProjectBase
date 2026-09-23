using UnityEngine;

[RequireComponent(typeof(GroundDetector))]
public abstract class GroundedAgentController : AgentController 
{
    [SerializeField] protected GroundDetector _groundDetector;
    protected IAgentJumpInput _jumpInput;

    // State Check Properties
    public bool IsGrounded => _groundDetector != null && _groundDetector.IsGrounded;

    protected override void Awake()
    {
        base.Awake();
        _groundDetector = GetComponent<GroundDetector>();

        _jumpInput = GetComponent<IAgentJumpInput>();
    }

    protected override void FixedUpdate()
    {
        _groundDetector.UpdateGroundedStatus();
    }
}
