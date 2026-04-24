using System.Collections.Generic;
using UnityEngine;

public enum AnimationIntType
{
    AttackType,
}

[RequireComponent(typeof(Animator))]

public class AgentAnimator : MonoBehaviour
{
    [SerializeField] Animator _anim;
    [SerializeField] AnimationDataSO _animationData;
    private Dictionary<AnimationIntType, int> _intParameters;
    private Dictionary<StateType, int> _boolParameters;

    public void Initialize()
    {
        _intParameters = new Dictionary<AnimationIntType, int>();
        _boolParameters = new Dictionary<StateType, int>();

        // Register Integer Parameters
        RegisterIntParam(AnimationIntType.AttackType, _animationData.AttackTypeInt);

        // Register Boolean (State) Parameters
        RegisterBoolParam(StateType.Idle, _animationData.IsIdleBool);
        RegisterBoolParam(StateType.Move, _animationData.IsMoveBool);
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

    public void SetBool(StateType type , bool value)
    {
        if (_boolParameters.TryGetValue(type, out int hash))
        {
            _anim.SetBool(hash, value);
        }
    }
}