namespace ProjectRE
{
    using UnityEngine;

    /// <summary>비행 Monster Animator parameter 이름.</summary>
    [CreateAssetMenu(fileName = "FlyingMonsterAnimationData", menuName = "ProjectRE/Monster/Flying Animation Data")]
    public class FlyingMonsterAnimationDataSO : AgentAnimationDataSO
    {
        public string IsAttackBool = "IsAttack";
        public string AttackTypeInt = "AttackType";
    }
}
