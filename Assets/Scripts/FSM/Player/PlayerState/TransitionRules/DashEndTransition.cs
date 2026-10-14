namespace ProjectRE
{
    using System;

    /// <summary>
    /// Dash 완료 후 지상 여부에 따라 GroundedState·FallState로 복귀.
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
