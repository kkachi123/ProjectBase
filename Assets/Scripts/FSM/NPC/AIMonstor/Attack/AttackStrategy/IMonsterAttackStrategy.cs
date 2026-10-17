namespace ProjectRE
{
    public enum MonsterAttackStatus { Preparing, Attacking, Completed, Failed, TargetLost }

    /// <summary>Monster 개체별 공격 준비·실행·Event·종료 계약.</summary>
    public interface IMonsterAttackStrategy
    {
        MonsterAttackDataSO Data { get; }
        MonsterAttackStatus Status { get; }
        void Begin();
        void Execute(float deltaTime);
        void OnAttackFrame(int intParameter);
        void OnAnimationEnd();
        void End();
    }
}
