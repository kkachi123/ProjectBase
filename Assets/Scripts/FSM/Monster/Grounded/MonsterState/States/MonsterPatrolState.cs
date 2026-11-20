namespace ProjectRE
{
    using UnityEngine;

    /// <summary>최초 위치 주변의 목적지 선택·수평 순찰.</summary>
    public class MonsterPatrolState : AgentStateBase
    {
        private readonly Transform _owner;
        private readonly GroundedMovementHandler2D _movement;
        private readonly IGroundedAnimation _animator;
        private readonly MonsterBehaviorDataSO _data;
        private readonly float _homeX;
        private float _destinationX;
        public bool HasArrived => Mathf.Abs(_destinationX - _owner.position.x) <= _data.ArrivalDistance;

        public MonsterPatrolState(Transform owner, GroundedMovementHandler2D movement, IGroundedAnimation animator,
            MonsterBehaviorDataSO data, float homeX)
        {
            _owner = owner;
            _movement = movement;
            _animator = animator;
            _data = data;
            _homeX = homeX;
        }

        /// <summary>진입당 순찰 목적지 한 번 선택.</summary>
        protected override void OnEnter()
        {
            _destinationX = _homeX + Random.Range(-_data.PatrolRadius, _data.PatrolRadius);
            _animator.SetGrounded(true);
        }

        protected override void OnExecute(float deltaTime)
        {
            _movement.HandleMove(new Vector2(Mathf.Sign(_destinationX - _owner.position.x), 0f));
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
