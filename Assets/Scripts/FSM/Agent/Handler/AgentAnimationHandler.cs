using System;
using System.Collections.Generic;

[Serializable]
public class AgentAnimationHandler 
{
    private AgentAnimator _animationController;

    public void Initialize(AgentAnimator animationController)
    {
        _animationController = animationController;
    }

    #region Animation Control
    public void NotifyTransition(List<StateType> types)
    {
        foreach(var type in types)
        {
            switch (type)
            {
                case StateType.Idle:
                    _animationController.SetBool(AnimationBoolType.IsIdle, true);
                    break;
                case StateType.Move:
                    _animationController.SetBool(AnimationBoolType.IsMove, true);
                    break;
                case StateType.Jump:
                    _animationController.SetBool(AnimationBoolType.IsJump, true);
                    break;
                case StateType.Fall:
                    _animationController.SetBool(AnimationBoolType.IsFall, true);
                    break;
                case StateType.Attack:
                    _animationController.SetBool(AnimationBoolType.IsAttack, true);
                    break;
                case StateType.Hit:
                    _animationController.SetBool(AnimationBoolType.IsHit, true);
                    break;
                case StateType.Death:
                    _animationController.SetBool(AnimationBoolType.IsDeath, true);
                    break;
                default:
                    break;
            }
        }
    }

    public void ApplyIdleAnimation(bool isIdle)
    {
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
