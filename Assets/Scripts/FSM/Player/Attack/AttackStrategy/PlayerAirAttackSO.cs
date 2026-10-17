namespace ProjectRE
{
    using UnityEngine;

    /// <summary>이동·중력을 유지하며 각 타격 Event에 공중 공격 적용.</summary>
    [CreateAssetMenu(fileName = "PlayerAirAttack", menuName = "ProjectRE/Player/Attack/Air")]
    public class PlayerAirAttackSO : PlayerAttackSO
    {
        private AgentMeleeHitHandler2D _hitHandler;

        public override void Initialize(GroundedMotor2D motor, IAgentCombatInput combatInput,
            IComboAnimation animator, AgentMeleeHitHandler2D hitHandler) => _hitHandler = hitHandler;

        public override void Begin() { }

        /// <summary>단계 인자와 무관하게 첫 데이터로 모든 공중 타격 처리.</summary>
        public override void OnAttackFrame(int attackStep)
        {
            _hitHandler.SetPreviewData(StartData);
            _hitHandler.PerformAttack(StartData);
        }

        /// <summary>공격 종료·중단 시 범위 표시 정리.</summary>
        public override void End() => _hitHandler.ClearPreview();
    }
}
