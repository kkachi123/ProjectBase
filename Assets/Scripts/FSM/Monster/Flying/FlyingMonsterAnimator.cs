namespace ProjectRE
{
    using System.Collections.Generic;

    /// <summary>공중 Monster 공격 모션 연결.</summary>
    public class FlyingMonsterAnimator : AgentAnimator, ICombatAnimation
    {
        private enum BoolType { Attack }
        private enum IntType { AttackType }
        private readonly Dictionary<BoolType, int> _bools = new();
        private readonly Dictionary<IntType, int> _ints = new();

        public override void Initialize()
        {
            base.Initialize();
            var data = (FlyingMonsterAnimationDataSO)_animationData;
            Register(_bools, BoolType.Attack, data.IsAttackBool);
            Register(_ints, IntType.AttackType, data.AttackTypeInt);
        }

        public void SetAttack(bool value) => SetBool(_bools, BoolType.Attack, value);
        public void SetAttackType(int value) => SetInteger(_ints, IntType.AttackType, value);
    }
}
