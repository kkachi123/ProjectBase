namespace ProjectRE
{
    /// <summary>Player 공격 진입·타격 Event·종료 계약.</summary>
    public interface IPlayerAttackStrategy
    {
        PlayerAttackDataSO StartData { get; }
        void Begin();
        void OnAttackFrame(int attackStep);
        void End();
    }
}
