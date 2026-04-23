using UnityEngine;

public class AgentAnimationEventProxy : MonoBehaviour
{
    private IAgentAnimationListener _controller;

    public void Initialize(IAgentAnimationListener controller)
    {
        _controller = controller;
    }
    // Called by Animation Events
    public virtual void OnAnimationOnFrame() => _controller?.OnAnimationEvent(AnimEventType.OnFrame);
    public virtual void OnAnimationEnd() => _controller?.OnAnimationEvent(AnimEventType.End);
}
