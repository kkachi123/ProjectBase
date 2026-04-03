using UnityEngine;
using System;

[Serializable]
public class AgentAnimationHandler 
{
    private AgentAnimator _animationController;
    [SerializeField] private SpriteRenderer _spriteRenderer;

    public void Initialize(AgentAnimator animationController)
    {
        _animationController = animationController;
    }

    #region Animation Control
    public void ApplyMovementAnimation(Vector2 horizontalInput)
    {
        _animationController.SetFloat(AnimationFloatType.MoveSpeed, Math.Abs(horizontalInput.x));
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
    public void ApplyHitAnimation(bool isHit)
    {
        if(isHit) _animationController.SetTrigger(AnimationTriggerType.HitTrigger);
        _animationController.SetBool(AnimationBoolType.IsHit, isHit);
    }
    public void ApplyDieAnimation()
    {
        _animationController.SetTrigger(AnimationTriggerType.DieTrigger);
        _animationController.SetBool(AnimationBoolType.IsDead, true);
    }
    #endregion
}
