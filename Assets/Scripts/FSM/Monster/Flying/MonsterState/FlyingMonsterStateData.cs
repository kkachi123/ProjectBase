namespace ProjectRE
{
    using UnityEngine;

    /// <summary>개체별 비행 행동 참조와 공격 간격.</summary>
    public class FlyingMonsterStateData : FlyingStateFactoryData
    {
        public Transform Owner { get; set; }
        public Vector2 Home { get; set; }
        public FlyingMonsterBehaviorDataSO Behavior { get; set; }
        public FlyingMonsterTargetHandler Target { get; set; }
        public float MoveSpeed { get; set; }
        public float NextAttackTime { get; set; }
        public FlyingMonsterAnimator FlyingAnimator => (FlyingMonsterAnimator)Animator;
        public bool InAttackRange => Target.HasValidTarget &&
            ((Vector2)Target.Root.position - (Vector2)Owner.position).sqrMagnitude <= Behavior.AttackDistance * Behavior.AttackDistance;

        // 목적지까지 마지막 이동 속도 제한.
        public void MoveTowards(Vector2 destination, float deltaTime)
        {
            Vector2 delta = destination - (Vector2)Owner.position;
            float distance = delta.magnitude;
            float maxDistance = MoveSpeed * deltaTime;
            MovementHandler.HandleMove(distance > 0f && maxDistance > 0f
                ? delta / distance * Mathf.Min(1f, distance / maxDistance) : Vector2.zero);
        }
    }
}
