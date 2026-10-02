namespace ProjectRE
{
    /// <summary>
    /// 공통 Dash Animation 수명을 관리하는 State다.
    /// Agent별 이동 거리·속도·쿨다운 규칙은 파생 State에서 확장한다.
    /// </summary>
    public class DashState : AgentStateBase
    {
        private readonly IDashAnimation _animator;
        private readonly AgentMotor2D _motor;

        public DashState(IDashAnimation animator, AgentMotor2D motor)
        {
            _animator = animator;
            _motor = motor;
        }

        protected override void OnEnter()
        {
            _motor.StopHorizontal();
            _animator.SetDash(true);
        }

        protected override void OnExecute(float deltaTime) { }

        protected override void OnExit()
        {
            _animator.SetDash(false);
        }
    }
}
