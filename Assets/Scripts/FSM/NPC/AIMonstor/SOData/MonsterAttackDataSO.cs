namespace ProjectRE
{
    using UnityEngine;

    /// <summary>Monster 전략별 타격·시작 거리·공격 종료 후 간격·배후 접근 제한 설정.</summary>
    [CreateAssetMenu(fileName = "MonsterAttackData", menuName = "ProjectRE/Monster/Attack Data")]
    public class MonsterAttackDataSO : ScriptableObject
    {
        public AttackData Data;
        public float AttackDistance = 1.8f;
        public float AttackInterval = 1f;
        // 배후 전략의 이동 준비에만 적용.
        public float RearApproachTimeout = 1.5f;
    }
}
