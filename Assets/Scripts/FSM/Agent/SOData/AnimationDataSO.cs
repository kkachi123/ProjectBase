using UnityEngine;

[CreateAssetMenu(fileName = "AnimationData", menuName = "Agent/Animation Data")]
public class AnimationDataSO : ScriptableObject
{
    [Header("Animation Int")]
    public string AttackTypeInt = "AttackType";
    [Header("Animation Float")]
    public string MoveSpeedFloat = "MoveSpeed";
    [Header("Animation Trigger")]
    public string JumpTrigger = "JumpTrigger";
    public string AttackTrigger = "AttackTrigger";
    public string DieTrigger = "DieTrigger";
    [Header("Animation Bool")]
    public string IsGroundBool = "IsGround";
}
