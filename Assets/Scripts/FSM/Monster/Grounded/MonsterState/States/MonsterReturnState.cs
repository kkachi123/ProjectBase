namespace ProjectRE
{
    using UnityEngine;

    /// <summary>최초 Start 위치로 수평 복귀.</summary>
    public class MonsterReturnState : AgentStateBase
    {
        private readonly Transform _owner;
        private readonly GroundedMovementHandler2D _movement;
        private readonly IGroundedAnimation _animator;
        private readonly float _homeX;
        private readonly float _arrivalDistance;
        public bool HasArrived => Mathf.Abs(_homeX - _owner.position.x) <= _arrivalDistance;

        public MonsterReturnState(Transform owner, GroundedMovementHandler2D movement, IGroundedAnimation animator,
            float homeX, float arrivalDistance)
        {
            _owner = owner;
            _movement = movement;
            _animator = animator;
            _homeX = homeX;
            _arrivalDistance = arrivalDistance;
        }

        protected override void OnEnter() => _animator.SetGrounded(true);

        protected override void OnExecute(float deltaTime)
        {
            _movement.HandleMove(new Vector2(Mathf.Sign(_homeX - _owner.position.x), 0f));
            _animator.SetMoveSpeed(1f);
        }

        protected override void OnExit()
        {
            _movement.Stop();
            _animator.SetMoveSpeed(0f);
            _animator.SetGrounded(false);
        }
    }
}
