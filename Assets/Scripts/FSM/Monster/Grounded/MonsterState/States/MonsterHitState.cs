namespace ProjectRE
{
    /// <summary>공용 피격 표현 및 진입 시 잔여 수평 이동 정지.</summary>
    public class MonsterHitState : HitState
    {
        private readonly GroundedMotor2D _motor;

        public MonsterHitState(IHitAnimation animator, GroundedMotor2D motor)
            : base(animator)
        {
            _motor = motor;
        }

        protected override void OnEnter()
        {
            base.OnEnter();
            _motor.StopHorizontal();
        }
    }
}
