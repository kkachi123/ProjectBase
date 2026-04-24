using UnityEngine;

[CreateAssetMenu(fileName = "AnimationData", menuName = "Agent/Animation Data")]
public class AnimationDataSO : ScriptableObject
{
    [Header("Animation Int")]
    public string AttackTypeInt = "AttackType";
    [Header("Animation Bool")]
    public string IsIdleBool = "IsIdle";
    public string IsMoveBool = "IsMove";
    public string IsJumpBool = "IsJump";
    public string IsFallBool = "IsFall";
    public string IsAttackBool = "IsAttack";
    public string IsHitBool = "IsHit";
    public string IsDeathBool = "IsDeath";
}
