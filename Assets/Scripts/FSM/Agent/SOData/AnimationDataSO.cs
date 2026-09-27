namespace ProjectRE
{
    using UnityEngine;

    [CreateAssetMenu(fileName = "AnimationData", menuName = "Agent/Animation Data")]
    public class AnimationDataSO : ScriptableObject
    {
        [Header("Animation Int")]
        // 현재 실행할 공격 애니메이션 종류를 선택한다.
        public string AttackTypeInt = "AttackType";
        [Header("Animation Float")]
        // 이동 입력 크기를 전달해 Grounded Blend Tree에서 Idle/Move를 보간한다.
        public string MoveSpeedFloat = "MoveSpeed";
        [Header("Animation Bool")]
        // 지상 기본 상태로 복귀할 수 있음을 알린다. Grounded Blend Tree 진입 조건으로 사용한다.
        public string IsGroundedBool = "IsGrounded";
        // 점프 시작을 알린다. Grounded에서 점프 모션으로 나가는 조건으로 사용한다.
        public string IsJumpBool = "IsJump";
        // 공중 하강 상태를 알린다. Grounded에서 낙하 모션으로 나가는 조건으로 사용한다.
        public string IsFallBool = "IsFall";
        // 공격 상태를 알린다. AttackTypeInt와 함께 공격 모션을 선택한다.
        public string IsAttackBool = "IsAttack";
        // 피격 상태를 알린다. 피격 모션 진입과 종료 후 지상 복귀에 사용한다.
        public string IsHitBool = "IsHit";
        // 사망 상태를 알린다. 사망 모션 진입 후 다른 상태 전이를 차단하는 데 사용한다.
        public string IsDeathBool = "IsDeath";
    }
}
