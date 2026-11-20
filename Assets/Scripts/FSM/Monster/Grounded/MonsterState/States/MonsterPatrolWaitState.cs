namespace ProjectRE
{
    using UnityEngine;

    /// <summary>순찰 사이 정지·무작위 대기.</summary>
    public class MonsterPatrolWaitState : AgentStateBase
    {
        private readonly GroundedMovementHandler2D _movement;
        private readonly IGroundedAnimation _animator;
        private readonly MonsterBehaviorDataSO _data;
        private float _endTime;
        public bool HasFinished => Time.time >= _endTime;

        public MonsterPatrolWaitState(GroundedMovementHandler2D movement, IGroundedAnimation animator, MonsterBehaviorDataSO data)
        {
            _movement = movement;
            _animator = animator;
            _data = data;
        }

        protected override void OnEnter()
        {
            _movement.Stop();
            _animator.SetMoveSpeed(0f);
            _animator.SetGrounded(true);
            _endTime = Time.time + Random.Range(_data.PatrolWaitMin, _data.PatrolWaitMax);
        }

        protected override void OnExecute(float deltaTime) { }
        protected override void OnExit() => _animator.SetGrounded(false);
    }
}
