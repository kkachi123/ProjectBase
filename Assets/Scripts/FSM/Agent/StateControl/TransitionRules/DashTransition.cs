namespace ProjectRE
{
    using System;

    /// <summary>
    /// Dash를 허용하는 상태에서 요청을 수신하는 이벤트 전이다.
    /// </summary>
    public class DashTransition : IEventTransitionRule
    {
        public Type NextStateType => typeof(DashState);

        private readonly IAgentDashInput _dashInput;
        private readonly AgentDashHandler2D _dashHandler;
        private bool _isSubscribed;
        private bool _shouldTransition;

        public DashTransition(IAgentDashInput dashInput, AgentDashHandler2D dashHandler)
        {
            _dashInput = dashInput;
            _dashHandler = dashHandler;
        }

        public bool ShouldTransition(float deltaTime)
        {
            if (!_shouldTransition)
                return false;

            _shouldTransition = false;
            return _dashHandler.CanStartDash();
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
