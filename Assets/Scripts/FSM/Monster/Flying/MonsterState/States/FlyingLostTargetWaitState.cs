namespace ProjectRE
{
    using UnityEngine;

    public class FlyingLostTargetWaitState : AgentStateBase
    {
        private readonly FlyingMonsterStateData _data;
        private float _endTime;
        public bool Finished => Time.time >= _endTime;
        public FlyingLostTargetWaitState(FlyingMonsterStateData data) => _data = data;
        protected override void OnEnter()
        {
            _data.MovementHandler.Stop();
            _endTime = Time.time + _data.Behavior.LostTargetWait;
        }
        protected override void OnExecute(float deltaTime) { }
    }
}
