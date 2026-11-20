namespace ProjectRE
{
    using System;

    /// <summary>순찰 목적지 도착 후 정지 대기로 전환.</summary>
    public class MonsterPatrolEndTransition : ITransitionRule
    {
        private readonly MonsterPatrolState _state;
        public Type NextStateType => typeof(MonsterPatrolWaitState);
        public MonsterPatrolEndTransition(MonsterPatrolState state) => _state = state;
        public bool ShouldTransition(float deltaTime) => _state.HasArrived;
    }

    /// <summary>순찰 대기 시간 종료 후 새 목적지 선택.</summary>
    public class MonsterPatrolWaitEndTransition : ITransitionRule
    {
        private readonly MonsterPatrolWaitState _state;
        public Type NextStateType => typeof(MonsterPatrolState);
        public MonsterPatrolWaitEndTransition(MonsterPatrolWaitState state) => _state = state;
        public bool ShouldTransition(float deltaTime) => _state.HasFinished;
    }

    /// <summary>대상 재인식 없이 상실 대기 종료 시 복귀.</summary>
    public class MonsterLostTargetWaitEndTransition : ITransitionRule
    {
        private readonly MonsterLostTargetWaitState _state;
        public Type NextStateType => typeof(MonsterReturnState);
        public MonsterLostTargetWaitEndTransition(MonsterLostTargetWaitState state) => _state = state;
        public bool ShouldTransition(float deltaTime) => _state.HasFinished;
    }

    /// <summary>초기 위치 도착 후 순찰 재개.</summary>
    public class MonsterReturnEndTransition : ITransitionRule
    {
        private readonly MonsterReturnState _state;
        public Type NextStateType => typeof(MonsterPatrolState);
        public MonsterReturnEndTransition(MonsterReturnState state) => _state = state;
        public bool ShouldTransition(float deltaTime) => _state.HasArrived;
    }
}
