namespace ProjectRE
{
    using System;

    /// <summary>조건식 기반 비행 Monster 전이.</summary>
    public class FlyingMonsterTransition : ITransitionRule
    {
        private readonly Func<bool> _condition;
        public Type NextStateType { get; }
        public FlyingMonsterTransition(Type nextStateType, Func<bool> condition)
        {
            NextStateType = nextStateType;
            _condition = condition;
        }
        public bool ShouldTransition(float deltaTime) => _condition();
    }
}
