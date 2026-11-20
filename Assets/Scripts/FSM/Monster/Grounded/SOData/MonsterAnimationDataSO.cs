namespace ProjectRE
{
    using UnityEngine;

    /// <summary>기본 Monster Animator의 공통·전용 parameter 이름.</summary>
    [CreateAssetMenu(fileName = "MonsterAnimationData", menuName = "ProjectRE/Monster/Animation Data")]
    public class MonsterAnimationDataSO : AgentAnimationDataSO
    {
        [Header("Monster Animation Bool")]
        // 기본 이동 상태 진입·종료 제어. 지면 센서 값과 별개.
        public string IsGroundedBool = "IsGrounded";
        // 단발 공격 모션 진입·종료 제어.
        public string IsAttackBool = "IsAttack";

        [Header("Monster Animation Float")]
        // Grounded Blend Tree의 Idle·Move 보간에 사용할 이동 입력 크기.
        public string MoveSpeedFloat = "MoveSpeed";

        [Header("Monster Animation Int")]
        // 공용 공격 계약의 공격 타입. 기본 Monster는 1번 사용.
        public string AttackTypeInt = "AttackType";
    }
}
