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
        _intParameters = new Dictionary<AnimationIntType, int>()
        {
            { AnimationIntType.AttackType, Animator.StringToHash(_animationData.AttackTypeInt) },
        };

        _boolParameters = new Dictionary<StateType, int>()
        {
            { StateType.Idle, Animator.StringToHash(_animationData.IsIdleBool) },
            { StateType.Move, Animator.StringToHash(_animationData.IsMoveBool) },
            { StateType.Jump, Animator.StringToHash(_animationData.IsJumpBool) },
            { StateType.Fall, Animator.StringToHash(_animationData.IsFallBool) },
            { StateType.Attack, Animator.StringToHash(_animationData.IsAttackBool) },
            { StateType.Hit, Animator.StringToHash(_animationData.IsHitBool) },
            { StateType.Death, Animator.StringToHash(_animationData.IsDeathBool) },
        };
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