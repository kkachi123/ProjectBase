namespace ProjectRE
{
using System.Collections.Generic;
using UnityEngine;

public class AgentAnimator : MonoBehaviour, IHitAnimation, IDeathAnimation
{
    protected enum AgentAnimationBoolType
    {
        Hit,
        Death,
    }

    [SerializeField] protected AgentAnimationDataSO _animationData;
    [SerializeField] protected Animator _anim;
    private readonly Dictionary<AgentAnimationBoolType, int> _boolParameters = new();

    public virtual void Initialize()
    {
        if (_animationData == null)
            return;

        Register(_boolParameters, AgentAnimationBoolType.Hit, _animationData.IsHitBool);
        Register(_boolParameters, AgentAnimationBoolType.Death, _animationData.IsDeathBool);
    }

    public void SetHit(bool value) => SetBool(_boolParameters, AgentAnimationBoolType.Hit, value);
    public void SetDeath(bool value) => SetBool(_boolParameters, AgentAnimationBoolType.Death, value);

    protected void Register<T>(Dictionary<T, int> parameters, T type, string parameterName)
    {
        if (!string.IsNullOrWhiteSpace(parameterName))
            parameters[type] = Animator.StringToHash(parameterName);
    }

    protected bool TryGetHash<T>(Dictionary<T, int> parameters, T type, out int hash) =>
        parameters.TryGetValue(type, out hash);

    protected void SetBool<T>(Dictionary<T, int> parameters, T type, bool value)
    {
        if (!TryGetHash(parameters, type, out int hash))
        {
            Debug.LogError($"{GetType().Name}: Animator bool parameter '{type}' is not registered.", this);
            return;
        }

        if (_anim == null)
        {
            Debug.LogError($"{GetType().Name}: Animator reference is missing.", this);
            return;
        }

        _anim.SetBool(hash, value);
    }

    protected void SetFloat<T>(Dictionary<T, int> parameters, T type, float value)
    {
        if (TryGetHash(parameters, type, out int hash))
            _anim?.SetFloat(hash, value);
    }

    protected void SetInteger<T>(Dictionary<T, int> parameters, T type, int value)
    {
        if (TryGetHash(parameters, type, out int hash))
            _anim?.SetInteger(hash, value);
    }
}
}
