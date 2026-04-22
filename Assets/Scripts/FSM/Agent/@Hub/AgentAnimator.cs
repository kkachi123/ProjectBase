using System.Collections.Generic;
using UnityEngine;

public enum AnimationIntType
{
    AttackType,
}
public enum AnimationFloatType
{
    MoveSpeed,
}
public enum AnimationBoolType
{
    IsIdle,
    IsMove,
    IsJump,
    IsFall,
    IsAttack,
    IsHit,
    IsDeath,
}

[RequireComponent(typeof(Animator))]

public class AgentAnimator : MonoBehaviour
{
    [SerializeField] Animator _anim;
    [SerializeField] AnimationDataSO _animationData;
    private Dictionary<AnimationIntType, int> _intParameters;
    private Dictionary<AnimationFloatType, int> _floatParameters;
    private Dictionary<AnimationBoolType, int> _boolParameters;

    public void Initialize()
    {
        _intParameters = new Dictionary<AnimationIntType, int>()
        {
            { AnimationIntType.AttackType, Animator.StringToHash(_animationData.AttackTypeInt) },
        };

        _floatParameters = new Dictionary<AnimationFloatType, int>()
        {
            { AnimationFloatType.MoveSpeed, Animator.StringToHash(_animationData.MoveSpeedFloat) },
        };
        _boolParameters = new Dictionary<AnimationBoolType, int>()
        {
            { AnimationBoolType.IsIdle, Animator.StringToHash(_animationData.IsIdleBool) },
            { AnimationBoolType.IsMove, Animator.StringToHash(_animationData.IsMoveBool) },
            { AnimationBoolType.IsJump, Animator.StringToHash(_animationData.IsJumpBool) },
            { AnimationBoolType.IsFall, Animator.StringToHash(_animationData.IsFallBool) },
            { AnimationBoolType.IsAttack, Animator.StringToHash(_animationData.IsAttackBool) },
            { AnimationBoolType.IsHit, Animator.StringToHash(_animationData.IsHitBool) },
            { AnimationBoolType.IsDeath, Animator.StringToHash(_animationData.IsDeathBool) },
        };
    }
    public void SetInteger(AnimationIntType type, int value)
    {
        if (_intParameters.TryGetValue(type, out int hash))
        {
            _anim.SetInteger(hash, value);
        }
    }

    public void SetFloat(AnimationFloatType type , float value)
    {
        if (_floatParameters.TryGetValue(type, out int hash))
        {
            _anim.SetFloat(hash, value);
        }
    }

    public void SetBool(AnimationBoolType type , bool value)
    {
        if (_boolParameters.TryGetValue(type, out int hash))
        {
            _anim.SetBool(hash, value);
        }
    }
}