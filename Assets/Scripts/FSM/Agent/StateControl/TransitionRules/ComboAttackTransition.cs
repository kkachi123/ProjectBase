namespace ProjectRE
{
using System;

public class ComboAttackTransition : ITransitionRule
{
    public Type NextStateType => typeof(AttackState);

    private readonly ComboAttackHandler _comboHandler;
    private readonly GroundDetector _groundDetector;
    private readonly IAttackStarter _attackStarter;

    public ComboAttackTransition(
        ComboAttackHandler comboHandler,
        GroundDetector groundDetector,
        IAttackStarter attackStarter)
    {
        _comboHandler = comboHandler;
        _groundDetector = groundDetector;
        _attackStarter = attackStarter;
    }

    public bool ShouldTransition(float deltaTime)
    {
        // 입력 소비·AttackType 선택은 Handler, 실제 스태미나 소비·타입 적용은 Starter가 담당한다.
        if (!_comboHandler.TryBeginAttack(_groundDetector.IsGrounded, out int attackType))
            return false;

        if (_attackStarter.TryStartAttack(attackType))
            return true;

        // State 진입 전 실패했으므로 현재 State를 유지하고 소비한 콤보 상태만 정리한다.
        _comboHandler.Reset();
        return false;
    }
}
}
