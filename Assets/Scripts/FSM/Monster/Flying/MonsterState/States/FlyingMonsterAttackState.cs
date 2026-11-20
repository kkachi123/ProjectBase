namespace ProjectRE
{
    /// <summary>타격 없이 기본 공격 모션 재생.</summary>
    public class FlyingMonsterAttackState : AttackState
    {
        private readonly FlyingMonsterStateData _data;
        public FlyingMonsterAttackState(FlyingMonsterStateData data) : base(data.FlyingAnimator) => _data = data;
        protected override void OnEnter()
        {
            _data.MovementHandler.Stop();
            if (_data.Target.HasValidTarget)
                _data.Motor.Face(_data.Target.Root.position.x - _data.Owner.position.x);
            Animator.SetAttackType(1);
            base.OnEnter();
        }
        public override void OnAttackFrame(int attackStep) { }
    }
}
