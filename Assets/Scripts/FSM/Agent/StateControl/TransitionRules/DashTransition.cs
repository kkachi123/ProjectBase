namespace ProjectRE
{
    using System;

    /// <summary>
    /// GroundedState에서만 Dash 요청을 수신하는 이벤트 전이다.
    /// </summary>
    public class DashTransition : IEventTransitionRule
    {
        public Type NextStateType => typeof(PlayerDashState);

        private readonly IAgentDashInput _dashInput;
        private readonly GroundDetector _groundDetector;
        private bool _isSubscribed;
        private bool _shouldTransition;

        public DashTransition(IAgentDashInput dashInput, GroundDetector groundDetector)
        {
            _dashInput = dashInput;
            _groundDetector = groundDetector;
        }

        public bool ShouldTransition(float deltaTime)
        {
            if (!_shouldTransition)
                return false;

            _shouldTransition = false;
            return _groundDetector.IsGrounded;
        }

        public void Subscribe()
        {
            if (_isSubscribed)
                return;

            _dashInput.OnDashRequested += TriggerTransition;
            _isSubscribed = true;
        }

        public void Unsubscribe()
        {
            if (_isSubscribed)
            {
                _dashInput.OnDashRequested -= TriggerTransition;
                _isSubscribed = false;
            }

            _shouldTransition = false;
        }

        private void TriggerTransition()
        {
            _shouldTransition = true;
        }
    }
}
