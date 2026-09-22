using UniRx;
public class GetHitTransition : IEventTransitionRule
{
    public StateType NextState => StateType.Hit;
    private IReadOnlyReactiveProperty<float> CurrentHealth;
    private bool m_shouldTransition = false;

    public GetHitTransition(IReadOnlyReactiveProperty<float> currentHealth)
    {
        CurrentHealth = currentHealth;
    }

    public void Subscribe()
    {
        if(CurrentHealth == null) return;
        CurrentHealth
            .Pairwise()
            .Where(pair => pair.Current < pair.Previous)
            .Subscribe(_ => TriggerTransition());
    }

    public void Unsubscribe()
    {
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
