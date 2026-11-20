namespace ProjectRE
{
    using System;
    using UnityEngine;

    /// <summary>모션 종료 후 대상 상태를 다시 판정.</summary>
    public class FlyingMonsterAnimationEndTransition : IEventTransitionRule
    {
        private readonly GetHitEndTransition _end;
        private readonly FlyingMonsterStateData _data;
        private readonly bool _attack;
        public Type NextStateType { get; private set; }

        public FlyingMonsterAnimationEndTransition(FlyingMonsterStateData data, bool attack)
        {
            _data = data;
            _attack = attack;
            _end = new GetHitEndTransition(data.AnimationEventSource, typeof(FlyingReturnState));
        }
        public void Subscribe() => _end.Subscribe();
        public void Unsubscribe() => _end.Unsubscribe();
        public bool ShouldTransition(float deltaTime)
        {
            if (!_end.ShouldTransition(deltaTime)) return false;
            if (_attack) _data.NextAttackTime = Time.time + _data.Behavior.AttackInterval;
            NextStateType = _data.Target.HasValidTarget ? typeof(FlyingChaseState)
                : _attack ? typeof(FlyingLostTargetWaitState) : typeof(FlyingReturnState);
            return true;
        }
    }
}
