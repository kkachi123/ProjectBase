namespace ProjectRE
{
using System;
using UniRx;
public class GetHitTransition : IEventTransitionRule
{
    public Type NextStateType => typeof(HitState);
    private readonly IReadOnlyReactiveProperty<float> CurrentHealth;
    private IDisposable _subscription;
    private bool m_shouldTransition = false;

    public GetHitTransition(IReadOnlyReactiveProperty<float> currentHealth)
    {
        CurrentHealth = currentHealth;
    }

    public void Subscribe()
    {
        if (CurrentHealth == null || _subscription != null)
            return;

        _subscription = CurrentHealth
            .Pairwise()
            .Where(pair => pair.Current < pair.Previous)
            .Subscribe(_ => TriggerTransition());
    }

    public void Unsubscribe()
    {
        _subscription?.Dispose();
        _subscription = null;
        m_shouldTransition = false;
    }

    private void TriggerTransition()
    {
        m_shouldTransition = true;
    }

    public bool ShouldTransition(float deltatime)
    {
        return m_shouldTransition;
    }
}
}
