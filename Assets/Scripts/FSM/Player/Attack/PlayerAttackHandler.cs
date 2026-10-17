namespace ProjectRE
{
    using System;
    using System.Collections.Generic;
    using UnityEngine;

    public enum PlayerAttackRole
    {
        GroundCombo,
        AirAttack
    }

    /// <summary>Player 공격 설정 보관·전략 선택 및 실행 수명 관리.</summary>
    [Serializable]
    public class PlayerAttackHandler : IAttackStarter
    {
        [Serializable]
        public struct AttackDefinition
        {
            public PlayerAttackRole Role;
            // 역할별 Animator 공격 진입점. 콤보 단계와 별개.
            public int AnimatorAttackType;
            public PlayerAttackSO Attack;
        }

        [SerializeField] private AttackDefinition[] _attackDefinitions = Array.Empty<AttackDefinition>();
        /// <summary>Combat Editor의 역할별 공격 SO 목록 조회용 접근자.</summary>
        public IReadOnlyList<AttackDefinition> AttackDefinitions => _attackDefinitions;

        private readonly Dictionary<PlayerAttackRole, (PlayerAttackSO Strategy, int AnimatorAttackType)> _strategies = new();
        private GroundDetector _groundDetector;
        private Stamina _stamina;
        private ICombatAnimation _animator;
        // 이번 State에서 고정할 전략·모션 선택값.
        private (PlayerAttackSO Strategy, int AnimatorAttackType) _selectedAttack;

        /// <summary>장착 SO를 개체별로 복제·주입 후 역할 등록. 원본 설정 변경은 다음 초기화부터 반영.</summary>
        public void Initialize(
            GroundDetector groundDetector, Stamina stamina, GroundedMotor2D motor,
            IAgentCombatInput combatInput, IComboAnimation animator, AgentMeleeHitHandler2D hitHandler)
        {
            _groundDetector = groundDetector;
            _stamina = stamina;
            _animator = animator;
            _strategies.Clear();

            foreach (AttackDefinition definition in _attackDefinitions)
            {
                PlayerAttackSO strategy = UnityEngine.Object.Instantiate(definition.Attack);
                strategy.name = definition.Attack.name + " (Runtime)";
                strategy.hideFlags = HideFlags.DontSave;
                strategy.Initialize(motor, combatInput, animator, hitHandler);
                _strategies.Add(definition.Role, (strategy, definition.AnimatorAttackType));
            }
        }

        /// <summary>지면·시작 비용 확인 후 이번 State의 전략·모션 선택.</summary>
        public bool TryStartAttack()
        {
            PlayerAttackRole role = _groundDetector.IsGrounded ? PlayerAttackRole.GroundCombo : PlayerAttackRole.AirAttack;
            var attack = _strategies[role];
            if (!_stamina.CanUse(attack.Strategy.StartData.usedStamina))
                return false;

            _selectedAttack = attack;
            return true;
        }

        /// <summary>선택 모션·전략 시작 및 State 진입 비용 한 번 소비.</summary>
        public void BeginAttack()
        {
            _animator.SetAttackType(_selectedAttack.AnimatorAttackType);
            _selectedAttack.Strategy.Begin();
            _stamina.Use(_selectedAttack.Strategy.StartData.usedStamina);
        }

        /// <summary>현재 전략에 타격 단계 전달.</summary>
        public void OnAttackFrame(int attackStep) => _selectedAttack.Strategy.OnAttackFrame(attackStep);

        /// <summary>입력 구독·Trigger·범위 표시 및 선택 결과 정리.</summary>
        public void EndAttack()
        {
            _selectedAttack.Strategy.End();
            _selectedAttack = default;
        }

        /// <summary>현재 State Exit 이후 실행 복제본·보관 참조 제거.</summary>
        public void Dispose()
        {
            foreach (var attack in _strategies.Values)
                UnityEngine.Object.Destroy(attack.Strategy);
            _strategies.Clear();
            _selectedAttack = default;
            _groundDetector = null;
            _stamina = null;
            _animator = null;
        }
    }
}
