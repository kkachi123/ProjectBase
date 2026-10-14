namespace ProjectRE
{
    using UnityEngine;

    /// <summary>Player 공격의 피해·범위·비용 및 모션 선택 설정.</summary>
    [CreateAssetMenu(fileName = "PlayerAttackData", menuName = "ProjectRE/Player/Attack Data")]
    public class PlayerAttackDataSO : ScriptableObject
    {
        // 타격 피해·범위 및 시작 SO에서 사용할 Stamina 비용.
        public AttackData Data;
        // 공격 시작 모션 선택. 지상 콤보 단계와 별개.
        public int AnimatorAttackType;
    }
}
