namespace ProjectRE
{
    using System;

    /// <summary>입력 요청 없이 거리·공격 간격으로 다음 공격 준비.</summary>
    public class MonsterAttackTransition : ITransitionRule
    {
        private readonly MonsterAttackHandler _attack;
        public Type NextStateType => typeof(AttackState);
        public MonsterAttackTransition(MonsterAttackHandler attack) => _attack = attack;
        public bool ShouldTransition(float deltaTime) => _attack.TryPrepareAttack();
    }
}
