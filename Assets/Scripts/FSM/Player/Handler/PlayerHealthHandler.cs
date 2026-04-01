using System;
using UniRx;
[System.Serializable]
public class PlayerHealthHandler
{
    private Health _health;
    private AgentAnimationHandler _animationHandler;
    private PlayerController _controller;

    public void Initialize(Health health,AgentAnimationHandler animationHandler, PlayerController controller)
    {
        _health = health;
        _animationHandler = animationHandler;
        _controller = controller;
        BindEvents();
    }

    private void BindEvents()
    {
        _health.CurrentHealth
            .Pairwise()
            .Where(pair => pair.Current < pair.Previous)
            .Subscribe(_ => _animationHandler.PlayHurtSequence())
            .AddTo(_controller);

        _health.IsDead
            .Pairwise() // IsDead 상태의 이전 값과 현재 값을 쌍으로 만들어서 전달
            .Where(pair => pair.Current != pair.Previous) // 상태가 변경되었을 때만 반응
            .Subscribe(_ => Die())
            .AddTo(_controller);
    }

    private void Die()
    {
        _animationHandler.ApplyDieAnimation();
        _controller.ChangeState(PlayerStateType.Die);
    }
}
