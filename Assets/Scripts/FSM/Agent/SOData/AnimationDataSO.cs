using UnityEngine;

[CreateAssetMenu(fileName = "AnimationData", menuName = "Agent/Animation Data")]
public class AnimationDataSO : ScriptableObject
{
    [Header("Animation Int")]
    public string AttackTypeInt = "AttackType";
    [Header("Animation Float")]
    public string MoveSpeedFloat = "MoveSpeed";
    [Header("Animation Trigger")]
    public string IdleTrigger = "IdleTrigger";
    public string MoveTrigger = "MoveTrigger";
    public string JumpTrigger = "JumpTrigger";
    public string FallTrigger = "FallTrigger";
    public string AttackTrigger = "AttackTrigger";
    public string HitTrigger = "HitTrigger";
    public string DeathTrigger = "DeathTrigger";
    [Header("Animation Bool")]
    public string IsIdleBool = "IsIdle";
    public string IsMoveBool = "IsMove";
    public string IsJumpBool = "IsJump";
    public string IsFallBool = "IsFall";
    public string IsAttackBool = "IsAttack";
    public string IsHitBool = "IsHit";
    public string IsDeathBool = "IsDeath";
}
