using DG.Tweening;
using UnityEngine;
using System;

[Serializable]
public class AgentAnimationHandler 
{
    private AgentAnimator _animationController;
    [SerializeField] private SpriteRenderer _spriteRenderer;
    Sequence _hurtSequence;

    public void Initialize(AgentAnimator animationController)
    {
        _animationController = animationController;
    }

    public void PlayHurtSequence()
    {
        if(_hurtSequence != null && _hurtSequence.IsActive()) _hurtSequence.Kill();
        _hurtSequence = DOTween.Sequence();
        _hurtSequence.Append(_spriteRenderer.DOColor(Color.red, 0.1f))
                    .Append(_spriteRenderer.DOColor(Color.white, 0.1f))
                    .SetLoops(3, LoopType.Yoyo)
                    .SetEase(Ease.InOutQuad);
    }

    #region Animation Control
    public void ApplyMovementAnimation(Vector2 horizontalInput)
    {
        float velocity = horizontalInput.x;
        _animationController.SetFloat(AnimationFloatType.MoveSpeed, Math.Abs(velocity));

        if (Math.Abs(velocity) > 0.01f)
        {
            if (velocity > 0) _spriteRenderer.flipX = false;
            else if (velocity < 0) _spriteRenderer.flipX = true;
        }
    }

    public void ApplyJumpingAnimation()
    {
        _animationController.SetTrigger(AnimationTriggerType.JumpTrigger);
    }

    public void ApplyFallingAnimation(bool isFalling)
    {
        _animationController.SetBool(AnimationBoolType.IsGround, !isFalling);
    }
    public void ApplyAttackAnimation(int attackType)
    {
        _animationController.SetInteger(AnimationIntType.AttackType, attackType);
        _animationController.SetTrigger(AnimationTriggerType.AttackTrigger);
    }

    public void ApplyDieAnimation()
    {
        _animationController.SetTrigger(AnimationTriggerType.DieTrigger);
    }
    #endregion
}
