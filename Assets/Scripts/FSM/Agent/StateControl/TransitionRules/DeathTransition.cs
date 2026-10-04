namespace ProjectRE
{
using System;
using UniRx;
/// <summary>현재 사망 값에 따른 DeathState 전이 판정.</summary>
public class DeathTransition : ITransitionRule
{
    public Type NextStateType => typeof(DeathState);
    private readonly IReadOnlyReactiveProperty<bool> IsDead;

    public DeathTransition(IReadOnlyReactiveProperty<bool> isDead)
    {
        IsDead = isDead ?? throw new ArgumentNullException(nameof(isDead));
    }

    public bool ShouldTransition(float deltatime)
    {
        return IsDead.Value;
    }
}
}
