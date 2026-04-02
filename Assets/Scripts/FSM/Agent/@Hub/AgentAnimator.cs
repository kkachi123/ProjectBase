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

public enum AnimationTriggerType
{
    JumpTrigger,
    DieTrigger,
    AttackTrigger
}
public enum AnimationBoolType
{
    IsGround,
}

[RequireComponent(typeof(Animator))]

public class AgentAnimator : MonoBehaviour
{
    [SerializeField] Animator _anim;
    [SerializeField] AnimationDataSO _animationData;
    private Dictionary<AnimationIntType, int> _intParameters;

    private Dictionary<AnimationFloatType, int> _floatParameters;
    private Dictionary<AnimationTriggerType, int> _triggerParameters;
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
        _triggerParameters = new Dictionary<AnimationTriggerType, int>()
        {
            { AnimationTriggerType.JumpTrigger, Animator.StringToHash(_animationData.JumpTrigger) },
            { AnimationTriggerType.DieTrigger, Animator.StringToHash(_animationData.DieTrigger) },
            { AnimationTriggerType.AttackTrigger, Animator.StringToHash(_animationData.AttackTrigger) },
        };
        _boolParameters = new Dictionary<AnimationBoolType, int>()
        {
            { AnimationBoolType.IsGround, Animator.StringToHash(_animationData.IsGroundBool) },
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

    public void SetTrigger(AnimationTriggerType type)
    {
        if (_triggerParameters.TryGetValue(type, out int hash))
        {
            _anim.SetTrigger(hash);
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