namespace ProjectRE
{
    /// <summary>피격 종료 시 넉백 속도 정리.</summary>
    public class FlyingMonsterHitState : HitState
    {
        private readonly FlyingMovementHandler2D _movement;
        public FlyingMonsterHitState(IHitAnimation animator, FlyingMovementHandler2D movement) : base(animator) => _movement = movement;
        protected override void OnExit()
        {
            _movement.Stop();
            base.OnExit();
        }
    }
}
