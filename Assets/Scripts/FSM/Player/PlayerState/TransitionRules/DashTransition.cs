namespace ProjectRE
{
    using System;

    /// <summary>
    /// Dash 허용 상태에서 요청 수신 및 시작 조건 확인 후 DashState 진입.
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
