namespace ProjectRE
{
    using System;
    using System.Collections.Generic;
    using UnityEngine;

    /// <summary>Player 공격 수치·실행 계약. 원본은 설정, 복제본은 개체별 실행에 사용.</summary>
    public abstract class PlayerAttackSO : ScriptableObject
    {
        [SerializeField] private AttackData[] _attacks = Array.Empty<AttackData>();
        /// <summary>Combat Editor의 단계별 원본 설정 조회.</summary>
        public IReadOnlyList<AttackData> Attacks => _attacks;
        public AttackData StartData => _attacks[0];

        /// <summary>복제본에 실행 의존성 주입. 사용하는 참조만 구현별 보관.</summary>
        public abstract void Initialize(GroundedMotor2D motor, IAgentCombatInput combatInput,
            IComboAnimation animator, AgentMeleeHitHandler2D hitHandler);
        public abstract void Begin();
        public abstract void OnAttackFrame(int attackStep);
        public abstract void End();
    }
}
