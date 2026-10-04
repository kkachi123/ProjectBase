namespace ProjectRE
{
using UnityEngine;

[RequireComponent(typeof(GroundDetector))]
/// <summary>
/// 지면 감지와 점프 입력이 필요한 AgentController의 공통 기반 클래스입니다.
/// </summary>
public abstract class GroundedAgentController : AgentController 
{
    [SerializeField] protected GroundDetector _groundDetector;
    protected IAgentJumpInput _jumpInput;

    // State Check Properties
    /// <summary>현재 지면에 접촉해 있는지 여부입니다.</summary>
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
