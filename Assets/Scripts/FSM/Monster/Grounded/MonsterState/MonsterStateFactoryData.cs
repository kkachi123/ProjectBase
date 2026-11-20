namespace ProjectRE
{
    using UnityEngine;

    /// <summary>지상 Monster 판단·실행 State의 의존성 전달.</summary>
    public class MonsterStateFactoryData : GroundedStateFactoryData
    {
        public Transform Owner { get; set; }
        public float HomeX { get; set; }
        public MonsterBehaviorDataSO BehaviorData { get; set; }
        public MonsterTargetHandler Target { get; set; }
        public MonsterAttackHandler Attack { get; set; }
        public MonsterAnimator MonsterAnimator
        {
            get => (MonsterAnimator)Animator;
            set => Animator = value;
        }
    }
}
