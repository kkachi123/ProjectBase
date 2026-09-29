namespace ProjectRE
{
    public class ComboAttackHandler
    {
        private const int MaxGroundComboStep = 3;

        // 0은 비활성, 1~3은 현재 재생 중인 지상 콤보 타격이다.
        private int _groundComboStep;
        // 현재 타격의 End Event에서 소비할 다음 타격 입력 수다.
        private int _queuedAttackCount;
        // true면 다음 Combo를 예약하고, false면 더 이상 연계할 수 없어 입력을 폐기한다.
        private bool _isAcceptingComboInput;

        public bool IsGroundComboActive => _groundComboStep > 0;

        public bool TryBeginAttack(IAgentCombatInput combatInput, bool isGrounded, out int attackType)
        {
            attackType = 0;
            if (!combatInput.TryConsumeAttackRequest())
                return false;

            // 공중 공격은 지상 콤보와 섞지 않고 항상 3타를 단발로 실행한다.
            if (!isGrounded)
            {
                ResetGroundCombo();
                attackType = MaxGroundComboStep;
                return true;
            }

            _groundComboStep = 1;
            _queuedAttackCount = 0;
            _isAcceptingComboInput = true;
            attackType = _groundComboStep;
            return true;
        }

        public void ProcessAttackRequests(IAgentCombatInput combatInput)
        {
            if (!_isAcceptingComboInput)
            {
                // 마지막 지상 3타·공중 3타 중 요청은 다음 1타로 넘기지 않는다.
                while (combatInput.TryConsumeAttackRequest()) { }
                return;
            }

            // 남은 콤보 수만 예약하고, 초과 요청은 소비해 자동 재시작으로 남지 않게 한다.
            int remainingAttackCount = MaxGroundComboStep - _groundComboStep;
            while (combatInput.TryConsumeAttackRequest())
            {
                if (_queuedAttackCount < remainingAttackCount)
                    _queuedAttackCount++;
            }
        }

        public bool TryAdvanceGroundCombo(IAgentCombatInput combatInput, IAttackComboStarter attackStarter, ICombatAnimation animator)
        {
            // End Event 직전 들어온 요청까지 처리해 Update/Event 프레임 경계를 보완한다.
            ProcessAttackRequests(combatInput);
            _isAcceptingComboInput = false;

            if (_groundComboStep == 0 || _groundComboStep >= MaxGroundComboStep || _queuedAttackCount == 0)
                return false;

            // End Event 시 다음 타격을 확정해도 FSM을 이탈하지 않고 Animator 내부 전이만 진행한다.
            int nextAttackType = _groundComboStep + 1;
            if (!attackStarter.TryContinueAttack(nextAttackType))
                return false;

            _queuedAttackCount--;
            _groundComboStep = nextAttackType;
            _isAcceptingComboInput = _groundComboStep < MaxGroundComboStep;
            animator.SetAttackType(nextAttackType);
            return true;
        }

        public void ResetGroundCombo()
        {
            // State Exit, 피격, Input Block 등 콤보가 중단되는 모든 경로의 공통 정리 지점이다.
            _groundComboStep = 0;
            _queuedAttackCount = 0;
            _isAcceptingComboInput = false;
        }
    }
}
