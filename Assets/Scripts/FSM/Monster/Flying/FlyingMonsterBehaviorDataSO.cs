namespace ProjectRE
{
    using UnityEngine;

    /// <summary>비행 Monster 행동 설정.</summary>
    [CreateAssetMenu(fileName = "FlyingMonsterBehaviorData", menuName = "ProjectRE/Monster/Flying Behavior Data")]
    public class FlyingMonsterBehaviorDataSO : ScriptableObject
    {
        public float PatrolRadius = 2f;
        public float PatrolWaitMin = 1f;
        public float PatrolWaitMax = 2f;
        public float LostTargetWait = 1.5f;
        public float ArrivalDistance = 0.15f;
        public float AttackDistance = 1f;
        public float AttackInterval = 1.5f;
    }
}
