namespace ProjectRE
{
    using System;

    /// <summary>
    /// Dash Animation End 이후 지상 여부에 따라 GroundedState 또는 FallState로 복귀한다.
    /// </summary>
    public class DashEndTransition : IEventTransitionRule
    {
        public Type NextStateType => _groundDetector.IsGrounded ? typeof(GroundedState) : typeof(FallState);

        private readonly IAnimationEventSource _eventSource;
        private readonly GroundDetector _groundDetector;
        private bool _isSubscribed;
        private bool _shouldTransition;

        public DashEndTransition(IAnimationEventSource eventSource, GroundDetector groundDetector)
        {
            _eventSource = eventSource;
            _groundDetector = groundDetector;
        }

        public bool ShouldTransition(float deltaTime)
        {
            if (!_shouldTransition)
                return false;

            _shouldTransition = false;
            return true;
        }

        public void Subscribe()
        {
            if (_isSubscribed)
                return;

            _eventSource.OnAnimationEnded += TriggerTransition;
            _isSubscribed = true;
        }

        public void Unsubscribe()
        {
            if (_isSubscribed)
            {
                _eventSource.OnAnimationEnded -= TriggerTransition;
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
