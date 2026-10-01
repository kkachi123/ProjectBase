using System;

namespace ProjectRE
{
    public class ComboAttackHandler
    {
        // 초기화, 입력과 최대 콤보 단계 설정을 위해 외부에서 호출한다.
        private int _maxGroundComboStep;
        private IAgentCombatInput _combatInput;

        // AttackState 활성 여부 확인. 
        private bool _isAttackActive;
        // AttackState 활성 중 들어온 다음 콤보 요청 1회를 보관한다.
        private bool _hasQueuedComboInput;

        public void Initialize(IAgentCombatInput combatInput, int maxGroundComboStep)
        {
            // null 체크는 Initialize에서만 수행하고 이후 Deinitialize로 해제한다.
            _combatInput = combatInput ?? throw new ArgumentNullException(nameof(combatInput));
            if (maxGroundComboStep < 1)
                throw new ArgumentOutOfRangeException(nameof(maxGroundComboStep), "콤보 단계는 1 이상이어야 합니다.");

            _maxGroundComboStep = maxGroundComboStep;
            _combatInput.OnAttackRequested += RegisterAttackRequest;
        }

        public void Deinitialize()
        {
            _combatInput.OnAttackRequested -= RegisterAttackRequest;
            _combatInput = null;
            Reset();
        }
        private void Reset()
        {
            // State Exit와 Destroy에서 사용하는 공통 정리 지점이다.
            _isAttackActive = false;
            _hasQueuedComboInput = false;
        }

        // 다른 State에서 공격 시작을 기다리는 입력을 등록한다. AttackState 진입 시 콤보를 시작할 수 있다.
        private void RegisterAttackRequest()
        {
            if (!_isAttackActive)
                return;

            _hasQueuedComboInput = true;
        }
        // PlayerAttackState OnEnter에서 호출한다. 최초 공격 입력은 AttackTransition이 소비하므로 남은 예약을 비운다.
        public void BeginAttack()
        {
            _isAttackActive = true;
            _hasQueuedComboInput = false;
        }

        // PlayerAttackState TryHandleAttackFinished에 호출한다.
        // AttackState 종료 시점에 콤보 공격을 이어갈 수 있는지 확인하고, 이어갈 수 있다면 다음 공격으로 전환한다.
        public bool TryConsumeNextAttack(int currentAttackType, out int nextAttackType)
        {
            nextAttackType = 0;
            if (!_isAttackActive || !_hasQueuedComboInput)
                return false;

            _hasQueuedComboInput = false;
            if (currentAttackType < 1 || currentAttackType >= _maxGroundComboStep)
                return false;

            nextAttackType = currentAttackType + 1;
            return true;
        }

        // PlayerAttackState OnExit에서 호출한다. State 밖으로 남는 예약 입력을 방지한다.
        public void EndAttack()
        {
            Reset();
        }
    }
}
