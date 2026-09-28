namespace ProjectRE
{
using System;

/// <summary>
/// 콤보 규칙이 없는 Agent가 사용할 기본 공격 요청 전이다.
/// 실제 공격 시작·스태미나 소비·AttackType 적용은 대상 State 또는 전용 확장 전이가 담당한다.
/// </summary>
public class AttackTransition : ITransitionRule
{
    public Type NextStateType => typeof(AttackState);

    protected readonly IAgentCombatInput CombatInput;

    public AttackTransition(IAgentCombatInput combatInput)
    {
        CombatInput = combatInput;
    }

    public virtual bool ShouldTransition(float deltaTime)
    {
        return CombatInput.HasAttackRequest;
    }
}
}
