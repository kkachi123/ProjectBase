namespace ProjectRE
{
using System.Collections.Generic;
using UnityEngine;

public enum AnimationIntType
{
    AttackType,
}

public enum AnimationFloatType
{
    Speed,
}

[RequireComponent(typeof(Animator))]

public class AgentAnimator : MonoBehaviour
{
    [SerializeField] Animator _anim;
    [SerializeField] AnimationDataSO _animationData;
    private Dictionary<AnimationIntType, int> _intParameters;
    private Dictionary<AnimationFloatType, int> _floatParameters;
    private Dictionary<StateType, int> _boolParameters;

    public void Initialize()
    {
        _intParameters = new Dictionary<AnimationIntType, int>();
        _floatParameters = new Dictionary<AnimationFloatType, int>();
        _boolParameters = new Dictionary<StateType, int>();

        RegisterIntParam(AnimationIntType.AttackType, _animationData.AttackTypeInt);
        
        RegisterFloatParam(AnimationFloatType.Speed, _animationData.MoveSpeedFloat);

        RegisterBoolParam(StateType.Grounded, _animationData.IsGroundedBool);
        RegisterBoolParam(StateType.Jump, _animationData.IsJumpBool);
        RegisterBoolParam(StateType.Fall, _animationData.IsFallBool);
        RegisterBoolParam(StateType.Attack, _animationData.IsAttackBool);
        RegisterBoolParam(StateType.Hit, _animationData.IsHitBool);
        RegisterBoolParam(StateType.Death, _animationData.IsDeathBool);
    }

    private void RegisterIntParam(AnimationIntType type, string paramName)
    {
        // Only register if the parameter name is valid (not null or empty)
        if (!string.IsNullOrWhiteSpace(paramName))
        {
            _intParameters[type] = Animator.StringToHash(paramName);
        }
    }

    private void RegisterFloatParam(AnimationFloatType type, string paramName)
    {
        if (!string.IsNullOrWhiteSpace(paramName))
        {
            _floatParameters[type] = Animator.StringToHash(paramName);
        }
    }

    private void RegisterBoolParam(StateType type, string paramName)
    {
        if (!string.IsNullOrWhiteSpace(paramName))
        {
            _boolParameters[type] = Animator.StringToHash(paramName);
        }
    }

    public void SetInteger(AnimationIntType type, int value)
    {
        if (_intParameters.TryGetValue(type, out int hash))
        {
            _anim.SetInteger(hash, value);
        }
    }

    public void SetFloat(AnimationFloatType type, float value)
    {
        if (_floatParameters.TryGetValue(type, out int hash))
        {
            _anim.SetFloat(hash, value);
        }
    }

    public void SetBool(StateType type , bool value)
    {
        if (_boolParameters.TryGetValue(type, out int hash))
        {
            _anim.SetBool(hash, value);
        }
    }
}
}
