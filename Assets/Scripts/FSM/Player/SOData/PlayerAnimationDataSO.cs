namespace ProjectRE
{
using UnityEngine;

[CreateAssetMenu(fileName = "PlayerAnimationData", menuName = "ProjectRE/Player/Animation Data")]
public class PlayerAnimationDataSO : AgentAnimationDataSO
{
    [Header("Player Animation Trigger")]
    // Player 공격 모션의 다음 Combo 연결 요청.
    public string ComboTrigger = "ComboTrigger";
    [Header("Player Animation Int")]
    // 현재 실행할 Player 공격 애니메이션 종류 선택.
    public string AttackTypeInt = "AttackType";

    [Header("Player Animation Float")]
    // Grounded Blend Tree의 Idle·Move 보간에 사용할 이동 입력 크기.
    public string MoveSpeedFloat = "MoveSpeed";

    [Header("Player Animation Bool")]
    // 지상 기본 Blend Tree 복귀 조건.
    public string IsGroundedBool = "IsGrounded";
    // 점프 모션 진입·종료 제어.
    public string IsJumpBool = "IsJump";
    // 낙하 모션 진입·종료 제어.
    public string IsFallBool = "IsFall";
    // 공격 모션 진입·종료 제어. 종류 선택은 AttackTypeInt 사용.
    public string IsAttackBool = "IsAttack";
    // Dash 모션 진입·종료 제어.
    public string IsDashBool = "IsDash";
}
}
