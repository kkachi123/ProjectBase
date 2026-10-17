namespace ProjectRE
{
    using System;

    /// <summary>入力要求を介さず距離・攻撃間隔から次の攻撃を準備。</summary>
    public class MonsterAttackTransition : ITransitionRule
    {
        private readonly MonsterAttackHandler _attack;
        public Type NextStateType => typeof(AttackState);
        public MonsterAttackTransition(MonsterAttackHandler attack) => _attack = attack;
        public bool ShouldTransition(float deltaTime) => _attack.TryPrepareAttack();
    }
}
