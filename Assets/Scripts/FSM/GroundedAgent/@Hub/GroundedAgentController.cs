namespace ProjectRE
{
using UnityEngine;

[RequireComponent(typeof(GroundDetector))]
/// <summary>
/// 지면 감지·점프 입력을 사용하는 AgentController 공통 기반.
/// </summary>
public abstract class GroundedAgentController : AgentController 
{
    [SerializeField] protected GroundDetector _groundDetector;
    protected IAgentJumpInput _jumpInput;

    // State Check Properties
    /// <summary>현재 지면 접촉 여부.</summary>
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
}
