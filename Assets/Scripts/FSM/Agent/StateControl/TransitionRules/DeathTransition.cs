using UniRx;
public class DeathTransition : IEventTransitionRule
{
    public StateType NextState => StateType.Death;
    private IReadOnlyReactiveProperty<bool> IsDead;
    private bool m_shouldTransition = false;

    public DeathTransition(IReadOnlyReactiveProperty<bool> isDead)
    {
        IsDead = isDead;
    }

    public void Subscribe()
    {
        if(IsDead != null) return;
        IsDead
            .Pairwise() 
            .Where(pair => pair.Current != pair.Previous)
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
