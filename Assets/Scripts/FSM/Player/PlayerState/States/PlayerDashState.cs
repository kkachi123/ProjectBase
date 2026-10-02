namespace ProjectRE
{
    /// <summary>
    /// Dash의 Animation 수명만 관리하는 Player 전용 State다.
    /// 이동 거리·속도·쿨다운은 후속 Dash 실행 정책에서 추가한다.
    /// </summary>
    public class PlayerDashState : AgentStateBase
    {
        private readonly IDashAnimation _animator;
        private readonly AgentMotor2D _motor;

        public PlayerDashState(IDashAnimation animator, AgentMotor2D motor)
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
