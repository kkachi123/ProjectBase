using System;

[Serializable]
public class AgentAnimationHandler 
{
    private AgentAnimator _animationController;

    public void Initialize(AgentAnimator animationController)
    {
        _animationController = animationController;
    }

    #region Animation Control

    public void ApplyIdleAnimation(bool isIdle)
    {
        _animationController.SetBool(AnimationBoolType.IsIdle, isIdle);
    }
    public void ApplyMovementAnimation(bool isMove)
    {
        _animationController.SetBool(AnimationBoolType.IsMove, isMove);
    }

    public void ApplyJumpingAnimation(bool isJump)
    {
        _animationController.SetBool(AnimationBoolType.IsJump, isJump);
    }

    public void ApplyFallingAnimation(bool isFalling)
    {
        _animationController.SetBool(AnimationBoolType.IsFall, isFalling);
    }
    public void ApplyAttackAnimation(int attackType)
    {
        bool isAttack = attackType > 0; 
        _animationController.SetBool(AnimationBoolType.IsAttack, isAttack);
        _animationController.SetInteger(AnimationIntType.AttackType, attackType);
    }
    public void ApplyHitAnimation(bool isHit)
    {
        _animationController.SetBool(AnimationBoolType.IsHit, isHit);
    }
    public void ApplyDeathAnimation()
    {
        _animationController.SetBool(AnimationBoolType.IsDeath, true);
    }
    #endregion
}
