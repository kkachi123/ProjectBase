using System;

namespace ProjectRE
{
    public class ComboAttackHandler
    {
        private int _maxGroundComboStep;

        private IAgentCombatInput _combatInput;
        private bool _isInitialized;

        // AttackState 밖에서 첫 공격 시작을 기다리는 입력 여부다.
        private bool _hasPendingStartAttack;
        // 공중 3타와 AttackState 밖을 구분하기 위한 현재 공격 상태다.
        private bool _isAttackActive;
        // 0은 비활성, 1~3은 현재 재생 중인 지상 콤보 타격이다.
        private int _groundComboStep;
        // 현재 타격의 End Event에서 소비할 다음 타격 예약 여부다.
        private bool _hasQueuedComboAttack;
        // true면 다음 Combo를 예약하고, false면 더 이상 연계할 수 없어 입력을 폐기한다.
        private bool _isAcceptingComboInput;

        public void Initialize(IAgentCombatInput combatInput, int maxGroundComboStep)
        {
            Deinitialize();

            // null 체크는 Initialize에서만 수행하고 이후 Deinitialize로 해제한다.
            _combatInput = combatInput ?? throw new ArgumentNullException(nameof(combatInput));
            if (maxGroundComboStep < 1)
                throw new ArgumentOutOfRangeException(nameof(maxGroundComboStep), "콤보 단계는 1 이상이어야 합니다.");

            _maxGroundComboStep = maxGroundComboStep;
            _combatInput.OnAttackRequested += RegisterAttackRequest;
            _isInitialized = true;
        }

        public void Deinitialize()
        {
            if (_isInitialized)
                _combatInput.OnAttackRequested -= RegisterAttackRequest;

            _combatInput = null;
            _isInitialized = false;
            Reset();
        }

        // 다른 State에서 공격 시작을 기다리는 입력을 등록한다. AttackState 진입 시 콤보를 시작할 수 있다.
        private void RegisterAttackRequest()
        {
            if (!_isAttackActive)
            {
                _hasPendingStartAttack = true;
                return;
            }

            if (!_isAcceptingComboInput)
                return;

            _hasQueuedComboAttack = true;
        }

        // ComboAttackTransition에서 호출한다. AttackState 진입 전 공격 시작을 시도하고, 실패하면 콤보 상태를 정리한다.
        public bool TryBeginAttack(bool isGrounded, out int attackType)
        {
            attackType = 0;
            if (!_hasPendingStartAttack)
                return false;

            _hasPendingStartAttack = false;

            // 공중 공격은 지상 콤보와 섞지 않고 항상 3타를 단발로 실행한다.
            if (!isGrounded)
            {
                _groundComboStep = 0;
                _hasQueuedComboAttack = false;
                _isAcceptingComboInput = false;
                attackType = _maxGroundComboStep;
                return true;
            }

            // 지상 콤보는 1타부터 시작한다. AttackState 진입 후 End Event에서 다음 타격을 예약할 수 있다.
            _groundComboStep = 1;
            _hasQueuedComboAttack = false;
            _isAcceptingComboInput = true;
            attackType = _groundComboStep;
            return true;
        }

        public void ActivateAttack()
        {
            _isAttackActive = true;
        }

        public bool TryAdvanceGroundCombo(IAttackComboStarter attackStarter, ICombatAnimation animator)
        {
            _isAcceptingComboInput = false;

            if (_groundComboStep == 0 || _groundComboStep >= _maxGroundComboStep || !_hasQueuedComboAttack)
                return false;

            // End Event 시 다음 타격을 확정해도 FSM을 이탈하지 않고 Animator 내부 전이만 진행한다.
            int nextAttackType = _groundComboStep + 1;
            if (!attackStarter.TryContinueAttack(nextAttackType))
                return false;

            _hasQueuedComboAttack = false;
            _groundComboStep = nextAttackType;
            _isAcceptingComboInput = _groundComboStep < _maxGroundComboStep;
            animator.SetAttackType(nextAttackType);
            return true;
        }

        public void Reset()
        {
            // State Exit, 피격, 공격 시작 실패 등 콤보가 중단되는 모든 경로의 공통 정리 지점이다.
            _hasPendingStartAttack = false;
            _isAttackActive = false;
            _groundComboStep = 0;
            _hasQueuedComboAttack = false;
            _isAcceptingComboInput = false;
        }
    }
}
