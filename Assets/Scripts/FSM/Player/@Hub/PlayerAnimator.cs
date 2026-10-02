namespace ProjectRE
{
using System.Collections.Generic;
using UnityEngine;

public enum PlayerAnimationBoolType
{
    Grounded,
    Jump,
    Fall,
    Attack,
    Dash,
}

public enum PlayerAnimationFloatType
{
    MoveSpeed,
}

public enum PlayerAnimationIntType
{
    AttackType,
}

public enum PlayerAnimationTriggerType
{
    Combo,
}

public class PlayerAnimator : AgentAnimator, IGroundedAnimation, IAirborneAnimation, IComboAnimation, IDashAnimation
{
    private readonly Dictionary<PlayerAnimationBoolType, int> _boolParameters = new();
    private readonly Dictionary<PlayerAnimationFloatType, int> _floatParameters = new();
    private readonly Dictionary<PlayerAnimationIntType, int> _intParameters = new();

    private readonly Dictionary<PlayerAnimationTriggerType, int> _triggerParameters = new();

    public override void Initialize()
    {
        base.Initialize();
        PlayerAnimationDataSO playerData = _animationData as PlayerAnimationDataSO;
        if (playerData == null)
        {
            Debug.LogError("PlayerAnimator requires PlayerAnimationDataSO.", this);
            return;
        }

        _boolParameters.Clear();
        _floatParameters.Clear();
        _intParameters.Clear();
        _triggerParameters.Clear();

        Register(_boolParameters, PlayerAnimationBoolType.Grounded, playerData.IsGroundedBool);
        Register(_boolParameters, PlayerAnimationBoolType.Jump, playerData.IsJumpBool);
        Register(_boolParameters, PlayerAnimationBoolType.Fall, playerData.IsFallBool);
        Register(_boolParameters, PlayerAnimationBoolType.Attack, playerData.IsAttackBool);
        Register(_boolParameters, PlayerAnimationBoolType.Dash, playerData.IsDashBool);
        Register(_floatParameters, PlayerAnimationFloatType.MoveSpeed, playerData.MoveSpeedFloat);
        Register(_intParameters, PlayerAnimationIntType.AttackType, playerData.AttackTypeInt);
        Register(_triggerParameters, PlayerAnimationTriggerType.Combo, playerData.ComboTrigger);
    }

    public void SetGrounded(bool value) => SetBool(_boolParameters, PlayerAnimationBoolType.Grounded, value);
    public void SetMoveSpeed(float value) => SetFloat(_floatParameters, PlayerAnimationFloatType.MoveSpeed, value);
    public void SetJump(bool value) => SetBool(_boolParameters, PlayerAnimationBoolType.Jump, value);
    public void SetFall(bool value) => SetBool(_boolParameters, PlayerAnimationBoolType.Fall, value);
    public void SetAttack(bool value) => SetBool(_boolParameters, PlayerAnimationBoolType.Attack, value);
    public void SetAttackType(int attackType) => SetInteger(_intParameters, PlayerAnimationIntType.AttackType, attackType);

    public void SetComboTrigger() => Trigger(_triggerParameters, PlayerAnimationTriggerType.Combo);
    public void ResetComboTrigger() => ResetTrigger(_triggerParameters, PlayerAnimationTriggerType.Combo);
    public void SetDash(bool value) => SetBool(_boolParameters, PlayerAnimationBoolType.Dash, value);
}
}
