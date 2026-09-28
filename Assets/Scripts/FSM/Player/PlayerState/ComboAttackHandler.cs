namespace ProjectRE
{
public class ComboAttackHandler
{
    private const int MaxGroundComboStep = 3;

    // 0은 비활성, 1~3은 현재 재생 중인 지상 콤보 타격이다.
    private int _groundComboStep;
    // 현재 타격의 End Event에서 소비할 다음 타격 입력 수다.
    private int _queuedAttackCount;
    // 현재 클립의 End Event 전 입력만 콤보 버퍼에 저장하도록 제한한다.
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

    public void CaptureBufferedRequests(IAgentCombatInput combatInput)
    {
        if (!_isAcceptingComboInput)
            return;

        // 남은 콤보 수를 넘는 입력은 소비해 버려 자동 재시작으로 남지 않게 한다.
        int remainingAttackCount = MaxGroundComboStep - _groundComboStep;
        while (combatInput.TryConsumeAttackRequest())
        {
            if (_queuedAttackCount < remainingAttackCount)
                _queuedAttackCount++;
        }
    }

    public bool TryAdvanceGroundCombo(IAgentCombatInput combatInput, IAttackComboStarter attackStarter, ICombatAnimation animator)
    {
        CaptureBufferedRequests(combatInput);
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
