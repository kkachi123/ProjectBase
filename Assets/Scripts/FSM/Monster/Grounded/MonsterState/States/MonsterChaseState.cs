namespace ProjectRE
{
    using UnityEngine;

    /// <summary>실행 가능한 공격 거리까지 접근. 간격 대기 중에는 정지.</summary>
    public class MonsterChaseState : AgentStateBase
    {
        private readonly GroundedMovementHandler2D _movement;
        private readonly IGroundedAnimation _animator;
        private readonly MonsterTargetHandler _target;
        private readonly MonsterAttackHandler _attack;

        public MonsterChaseState(GroundedMovementHandler2D movement, IGroundedAnimation animator,
            MonsterTargetHandler target, MonsterAttackHandler attack)
        {
            _movement = movement;
            _animator = animator;
            _target = target;
            _attack = attack;
        }

        protected override void OnEnter() => _animator.SetGrounded(true);

        protected override void OnExecute(float deltaTime)
        {
            float direction = _attack.IsInAttackRange(_target.DeltaX) ? 0f : Mathf.Sign(_target.DeltaX);
            _movement.HandleMove(new Vector2(direction, 0f));
            _animator.SetMoveSpeed(Mathf.Abs(direction));
        }

        protected override void OnExit()
        {
            _movement.Stop();
            _animator.SetMoveSpeed(0f);
            _animator.SetGrounded(false);
        }
    }
}
