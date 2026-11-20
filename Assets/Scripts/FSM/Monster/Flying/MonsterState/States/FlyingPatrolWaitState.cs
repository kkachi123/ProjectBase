namespace ProjectRE
{
    using UnityEngine;

    public class FlyingPatrolWaitState : AgentStateBase
    {
        private readonly FlyingMonsterStateData _data;
        private float _endTime;
        public bool Finished => Time.time >= _endTime;
        public FlyingPatrolWaitState(FlyingMonsterStateData data) => _data = data;
        protected override void OnEnter()
        {
            _data.MovementHandler.Stop();
            _endTime = Time.time + UnityEngine.Random.Range(_data.Behavior.PatrolWaitMin, _data.Behavior.PatrolWaitMax);
        }
        protected override void OnExecute(float deltaTime) { }
    }
}
