using UnityEngine;

public class PlayerAnimationEventProxy : MonoBehaviour
{
    private IPlayerAnimationListener _controller;

    public void Initialize(IPlayerAnimationListener controller)
    {
        _controller = controller;
    }

    // 애니메이션 이벤트에서 이 함수들을 호출하도록 설정
    public void OnAttackHitFrame() => _controller?.OnAttackHitFrame();
    public void OnAttackAnimationEnd() => _controller?.OnAttackAnimationEnd();
    public void OnHitAnimationEnd() => _controller?.OnHitAnimationEnd();
}
