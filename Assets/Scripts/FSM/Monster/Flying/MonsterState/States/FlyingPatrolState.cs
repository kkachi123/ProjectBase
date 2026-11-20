namespace ProjectRE
{
    using UnityEngine;

    public class FlyingPatrolState : AgentStateBase
    {
        private readonly FlyingMonsterStateData _data;
        private Vector2 _destination;
        public bool Arrived => Vector2.Distance(_data.Owner.position, _destination) <= _data.Behavior.ArrivalDistance;
        public FlyingPatrolState(FlyingMonsterStateData data) => _data = data;
        protected override void OnEnter() => _destination = _data.Home + UnityEngine.Random.insideUnitCircle * _data.Behavior.PatrolRadius;
        protected override void OnExecute(float deltaTime) => _data.MoveTowards(_destination, deltaTime);
        protected override void OnExit() => _data.MovementHandler.Stop();
    }
}
