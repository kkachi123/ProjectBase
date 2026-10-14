namespace ProjectRE
{
    /// <summary>
    /// 공통 Dash 이동·애니메이션 수명 관리.
    /// Agent별 규칙은 파생 State에서 확장 가능.
    /// </summary>
    public class DashState : AgentStateBase
    {
        private readonly IDashAnimation _animator;
        private readonly AgentDashHandler2D _dashHandler;

        public DashState(IDashAnimation animator, AgentDashHandler2D dashHandler)
        {
            _animator = animator;
            _dashHandler = dashHandler;
        }

        protected override void OnEnter()
        {
            _dashHandler.BeginDash();
            _animator.SetDash(true);
        }

        protected override void OnExecute(float deltaTime)
        {
            _dashHandler.ExecuteDash();
        }

        protected override void OnExit()
        {
            _dashHandler.EndDash();
            _animator.SetDash(false);
        }
    }
}
