namespace ProjectRE
{
using UnityEngine;

[CreateAssetMenu(fileName = "PlayerAnimationData", menuName = "ProjectRE/Player/Animation Data")]
public class PlayerAnimationDataSO : AgentAnimationDataSO
{
    [Header("Player Animation Int")]
    // 현재 실행할 Player 공격 애니메이션 종류를 선택한다.
    public string AttackTypeInt = "AttackType";

    [Header("Player Animation Float")]
    // 이동 입력 크기를 Grounded Blend Tree에 전달해 Idle/Move를 보간한다.
    public string MoveSpeedFloat = "MoveSpeed";

    [Header("Player Animation Bool")]
    // 지상 기본 Blend Tree로 복귀할 수 있음을 알린다.
    public string IsGroundedBool = "IsGrounded";
    // Grounded에서 점프 모션으로 진입할 때 사용한다.
    public string IsJumpBool = "IsJump";
    // Grounded에서 낙하 모션으로 진입할 때 사용한다.
    public string IsFallBool = "IsFall";
    // AttackTypeInt와 함께 Player 공격 모션을 선택한다.
    public string IsAttackBool = "IsAttack";
    // Grounded 상태에서 Dash 모션으로 진입·종료할 때 사용한다.
    public string IsDashBool = "IsDash";
}
}
