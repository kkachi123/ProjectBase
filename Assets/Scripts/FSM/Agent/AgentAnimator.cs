using System.Collections.Generic;
using UnityEngine;

public enum AnimationFloatType
{
    MoveSpeed,
}

public enum AnimationTriggerType
{
    JumpTrigger,
    DieTrigger,
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

    private Dictionary<AnimationFloatType, int> _floatParameters;
    private Dictionary<AnimationTriggerType, int> _triggerParameters;
    private Dictionary<AnimationBoolType, int> _boolParameters;

    public void Initialize()
    {
        _floatParameters = new Dictionary<AnimationFloatType, int>()
        {
            { AnimationFloatType.MoveSpeed, Animator.StringToHash(_animationData.MoveSpeedFloat) },
        };
        _triggerParameters = new Dictionary<AnimationTriggerType, int>()
        {
            { AnimationTriggerType.JumpTrigger, Animator.StringToHash(_animationData.JumpTrigger) },
            { AnimationTriggerType.DieTrigger, Animator.StringToHash(_animationData.DieTrigger) },
        };
        _boolParameters = new Dictionary<AnimationBoolType, int>()
        {
            { AnimationBoolType.IsGround, Animator.StringToHash(_animationData.IsGroundBool) },
        };
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