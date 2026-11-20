namespace ProjectRE
{
    using UnityEngine;

    /// <summary>지상 일반 Monster의 순찰·대상 상실·도착·높이 설정.</summary>
    [CreateAssetMenu(fileName = "MonsterBehaviorData", menuName = "ProjectRE/Monster/Behavior Data")]
    public class MonsterBehaviorDataSO : ScriptableObject
    {
        public float PatrolRadius = 2f;
        public float PatrolWaitMin = 1f;
        public float PatrolWaitMax = 2f;
        public float LostTargetWait = 1.5f;
        public float ArrivalDistance = 0.15f;
        // 자기·대상 몸통 바닥 사이 추적 허용 높이.
        public float MaxTargetHeightDifference = 1f;
    }
}
