namespace ProjectRE
{
    using System;

    /// <summary>현재 대상 유효 여부에 따른 추적·상실 전환.</summary>
    public class MonsterTargetTransition : ITransitionRule
    {
        private readonly MonsterTargetHandler _target;
        private readonly bool _expected;
        public Type NextStateType { get; }

        public MonsterTargetTransition(MonsterTargetHandler target, bool expected, Type nextStateType)
        {
            _target = target;
            _expected = expected;
            NextStateType = nextStateType;
        }

        public bool ShouldTransition(float deltaTime) => _target.HasValidTarget == _expected;
    }
}
