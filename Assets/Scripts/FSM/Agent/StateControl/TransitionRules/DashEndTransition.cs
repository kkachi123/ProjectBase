namespace ProjectRE
{
    using System;

    /// <summary>
    /// Dash 실행 완료 이후 지상 여부에 따라 GroundedState 또는 FallState로 복귀한다.
    /// </summary>
    public class DashEndTransition : ITransitionRule
    {
        public Type NextStateType => _groundDetector.IsGrounded ? typeof(GroundedState) : typeof(FallState);

        private readonly GroundDetector _groundDetector;
        private readonly AgentDashHandler2D _dashHandler;

        public DashEndTransition(AgentDashHandler2D dashHandler, GroundDetector groundDetector)
        {
            _dashHandler = dashHandler;
            _groundDetector = groundDetector;
        }

        public bool ShouldTransition(float deltaTime)
        {
            return _dashHandler.IsCompleted;
        }
    }
}
