namespace ProjectRE
{
    using UnityEngine;

    public enum MonsterAttackStatus { Preparing, Attacking, Completed, Failed, TargetLost }

    /// <summary>Monster 공격 설정·실행 계약. 진행 상태는 개체별 복제본에만 보관.</summary>
    public abstract class MonsterAttackSO : ScriptableObject
    {
        public AttackData Data;
        // 추적 종료·공격 선택에 사용할 수평 거리.
        public float AttackDistance = 1.8f;
        // 실제 공격 모션 정상 종료 후 다음 요청까지의 간격.
        public float AttackInterval = 1f;
        public MonsterAttackStatus Status { get; protected set; }

        /// <summary>복제본에 실행 참조·모션 진입값 주입.</summary>
        public abstract void Initialize(GroundedMotor2D motor, GroundedMovementHandler2D movement,
            MonsterTargetHandler target, MonsterAnimator animator, AgentMeleeHitHandler2D hitHandler,
            float arrivalDistance, int animatorAttackType);
        public abstract void Begin();
        public abstract void Execute(float deltaTime);
        public abstract void OnAttackFrame(int intParameter);
        public abstract void OnAnimationEnd();
        public abstract void End();
    }
}
