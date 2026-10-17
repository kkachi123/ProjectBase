namespace ProjectRE
{
    using System;

    /// <summary>攻撃結果確認後、次の攻撃・追跡・復帰先を選択。</summary>
    public class MonsterAttackEndTransition : ITransitionRule
    {
        private readonly MonsterAttackHandler _attack;
        private readonly MonsterTargetHandler _target;
        public Type NextStateType { get; private set; }

        public MonsterAttackEndTransition(MonsterAttackHandler attack, MonsterTargetHandler target)
        {
            _attack = attack;
            _target = target;
        }

        public bool ShouldTransition(float deltaTime)
        {
            MonsterAttackStatus status = _attack.Status;
            if (status == MonsterAttackStatus.Preparing || status == MonsterAttackStatus.Attacking)
                return false;
            if (status == MonsterAttackStatus.TargetLost && !_target.HasValidTarget)
            {
                NextStateType = typeof(MonsterLostTargetWaitState);
                return true;
            }

            NextStateType = !_target.HasValidTarget ? typeof(MonsterReturnState)
                : _attack.TryPrepareAttack() ? typeof(AttackState) : typeof(MonsterChaseState);
            return true;
        }
    }
}
