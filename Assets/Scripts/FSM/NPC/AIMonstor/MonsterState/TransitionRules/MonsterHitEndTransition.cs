namespace ProjectRE
{
    using System;

    /// <summary>피격 종료 Event 후 현재 대상·공격 간격으로 다음 행동 판정.</summary>
    public class MonsterHitEndTransition : IEventTransitionRule
    {
        private readonly GetHitEndTransition _end;
        private readonly MonsterTargetHandler _target;
        private readonly MonsterAttackHandler _attack;
        public Type NextStateType { get; private set; }

        public MonsterHitEndTransition(IAnimationEventSource source, MonsterTargetHandler target, MonsterAttackHandler attack)
        {
            _end = new GetHitEndTransition(source, typeof(MonsterReturnState));
            _target = target;
            _attack = attack;
        }

        public void Subscribe() => _end.Subscribe();
        public void Unsubscribe() => _end.Unsubscribe();

        public bool ShouldTransition(float deltaTime)
        {
            if (!_end.ShouldTransition(deltaTime))
                return false;
            NextStateType = !_target.HasValidTarget ? typeof(MonsterReturnState)
                : _attack.TryPrepareAttack() ? typeof(AttackState) : typeof(MonsterChaseState);
            return true;
        }
    }
}
