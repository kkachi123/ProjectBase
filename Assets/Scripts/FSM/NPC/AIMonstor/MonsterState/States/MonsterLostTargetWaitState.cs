namespace ProjectRE
{
    using UnityEngine;

    /// <summary>교전 중 대상 상실 후 재인식 대기.</summary>
    public class MonsterLostTargetWaitState : AgentStateBase
    {
        private readonly GroundedMovementHandler2D _movement;
        private readonly IGroundedAnimation _animator;
        private readonly MonsterBehaviorDataSO _data;
        private float _endTime;
        public bool HasFinished => Time.time >= _endTime;

        public MonsterLostTargetWaitState(GroundedMovementHandler2D movement, IGroundedAnimation animator, MonsterBehaviorDataSO data)
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
            _endTime = Time.time + _data.LostTargetWait;
        }

        protected override void OnExecute(float deltaTime) { }
        protected override void OnExit() => _animator.SetGrounded(false);
    }
}
