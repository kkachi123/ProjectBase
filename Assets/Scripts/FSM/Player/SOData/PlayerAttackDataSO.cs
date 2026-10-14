namespace ProjectRE
{
    using UnityEngine;

    /// <summary>Player 공격의 피해·범위·비용 설정.</summary>
    [CreateAssetMenu(fileName = "PlayerAttackData", menuName = "ProjectRE/Player/Attack Data")]
    public class PlayerAttackDataSO : ScriptableObject
    {
        // 타격 피해·범위 및 시작 SO에서 사용할 Stamina 비용.
        public AttackData Data;
    }
}
