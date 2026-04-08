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
        if (isIdle) _animationController.SetTrigger(AnimationTriggerType.IdleTrigger);
        _animationController.SetBool(AnimationBoolType.IsIdle, isIdle);
    }
    public void ApplyMovementAnimation(bool isMove)
    {
        if (isMove) _animationController.SetTrigger(AnimationTriggerType.MoveTrigger);
        _animationController.SetBool(AnimationBoolType.IsMove, isMove);
    }

    public void ApplyJumpingAnimation(bool isJump)
    {
        if(isJump) _animationController.SetTrigger(AnimationTriggerType.JumpTrigger);
        _animationController.SetBool(AnimationBoolType.IsJump, isJump);
    }

    public void ApplyFallingAnimation(bool isFalling)
    {
        if(isFalling) _animationController.SetTrigger(AnimationTriggerType.FallTrigger);
        _animationController.SetBool(AnimationBoolType.IsFall, isFalling);
    }
    public void ApplyAttackAnimation(int attackType)
    {
        bool isAttack = attackType > 0; 
        if(isAttack) _animationController.SetTrigger(AnimationTriggerType.AttackTrigger);
        _animationController.SetBool(AnimationBoolType.IsAttack, isAttack);
        _animationController.SetInteger(AnimationIntType.AttackType, attackType);
    }
    public void ApplyHitAnimation(bool isHit)
    {
        if(isHit) _animationController.SetTrigger(AnimationTriggerType.HitTrigger);
        _animationController.SetBool(AnimationBoolType.IsHit, isHit);
    }
    public void ApplyDeathAnimation()
    {
        _animationController.SetTrigger(AnimationTriggerType.DeathTrigger);
        _animationController.SetBool(AnimationBoolType.IsDeath, true);
    }
    #endregion
}
