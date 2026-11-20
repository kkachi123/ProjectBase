namespace ProjectRE
{
    using UnityEngine;

    public class FlyingReturnState : AgentStateBase
    {
        private readonly FlyingMonsterStateData _data;
        public bool Arrived => Vector2.Distance(_data.Owner.position, _data.Home) <= _data.Behavior.ArrivalDistance;
        public FlyingReturnState(FlyingMonsterStateData data) => _data = data;
        protected override void OnEnter() { }
        protected override void OnExecute(float deltaTime) => _data.MoveTowards(_data.Home, deltaTime);
        protected override void OnExit() => _data.MovementHandler.Stop();
    }
}
