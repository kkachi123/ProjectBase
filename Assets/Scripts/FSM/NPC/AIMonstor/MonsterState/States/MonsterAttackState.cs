namespace ProjectRE
{
    /// <summary>공용 단발 공격 실행 및 진입 시 수평 이동 정지.</summary>
    public class MonsterAttackState : AttackState
    {
        private readonly GroundedMotor2D _motor;
        private readonly AgentMeleeHitHandler2D _hitHandler;
        private readonly AgentStatData _statData;

        public MonsterAttackState(ICombatAnimation animator, AgentMeleeHitHandler2D hitHandler, GroundedMotor2D motor, AgentStatData statData)
            : base(animator)
        {
            _motor = motor;
            _hitHandler = hitHandler;
            _statData = statData;
        }

        protected override void OnEnter()
        {
            Animator.SetAttackType(1);
            base.OnEnter();
            _motor.StopHorizontal();
            _hitHandler.SetPreviewData(_statData.attackDatas[0]);
        }

        /// <summary>기존 StatData의 첫 공격 데이터로 단발 타격.</summary>
        public override void OnAttackFrame(int attackStep) => _hitHandler.PerformAttack(_statData.attackDatas[0]);

        protected override void OnExit()
        {
            _hitHandler.ClearPreview();
            base.OnExit();
        }
    }
}
