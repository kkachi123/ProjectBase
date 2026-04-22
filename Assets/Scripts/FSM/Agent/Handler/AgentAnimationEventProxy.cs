using UnityEngine;

public class AgentAnimationEventProxy : MonoBehaviour
{
    private IAgentAnimationListener _controller;

    public void Initialize(IAgentAnimationListener controller)
    {
        _controller = controller;
    }
    // Called by Animation Events
    public virtual void OnAttackHitFrame() => _controller?.OnAttackHitFrame();
    public virtual void OnAnimationEnd() => _controller?.OnAnimationEnd(AnimEventType.End);
}
