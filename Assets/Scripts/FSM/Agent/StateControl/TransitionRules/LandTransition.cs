namespace ProjectRE
{
using System;

/// <summary>
/// 지면 감지·수직 속도 확인 후 공중 State에서 GroundedState로 전환.
/// </summary>
public class LandTransition : ITransitionRule
{
    public Type NextStateType => typeof(GroundedState);
    private GroundDetector _groundDetector;
    private AgentMotor2D _motor;

    public LandTransition(GroundDetector groundDetector, AgentMotor2D motor)
    {
        _groundDetector = groundDetector;
        _motor = motor;
    }

    public bool ShouldTransition(float deltatime)
    {
        // 지상착지 + 낙하속도 <= 0.01f (부동소수점 오차 고려 , 0이하 설정 시 착지인식을 못하는 경우 존재)
        return _groundDetector.IsGrounded && _motor.VerticalVelocity <= 0.01f;
    }
}
}
