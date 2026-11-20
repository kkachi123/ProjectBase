namespace ProjectRE
{
    public class FlyingChaseState : AgentStateBase
    {
        private readonly FlyingMonsterStateData _data;
        public FlyingChaseState(FlyingMonsterStateData data) => _data = data;
        protected override void OnEnter() { }
        protected override void OnExecute(float deltaTime)
        {
            if (_data.InAttackRange)
                _data.MovementHandler.Stop();
            else
                _data.MoveTowards(_data.Target.Root.position, deltaTime);
        }
        protected override void OnExit() => _data.MovementHandler.Stop();
    }
}
