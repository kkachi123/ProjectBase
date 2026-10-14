namespace ProjectRE
{
    /// <summary>이동·중력을 유지하며 각 타격 Event에 공중 공격 적용.</summary>
    public class PlayerAirAttack : IPlayerAttackStrategy
    {
        private readonly AgentMeleeHitHandler2D _hitHandler;
        public PlayerAttackDataSO StartData { get; }

        public PlayerAirAttack(PlayerAttackDataSO data, AgentMeleeHitHandler2D hitHandler)
        {
            StartData = data;
            _hitHandler = hitHandler;
        }

        public void Begin() { }

        /// <summary>단계 인자와 무관하게 공중 공격 데이터로 타격.</summary>
        public void OnAttackFrame(int attackStep)
        {
            _hitHandler.SetPreviewData(StartData.Data);
            _hitHandler.PerformAttack(StartData.Data);
        }

        /// <summary>공격 종료·중단 시 범위 표시 정리.</summary>
        public void End() => _hitHandler.ClearPreview();
    }
}
