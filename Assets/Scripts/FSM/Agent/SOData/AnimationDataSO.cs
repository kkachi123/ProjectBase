using UnityEngine;

[CreateAssetMenu(fileName = "AnimationData", menuName = "Agent/Animation Data")]
public class AnimationDataSO : ScriptableObject
{
    [Header("Animation Float")]
    public string MoveSpeedFloat = "MoveSpeed";
    [Header("Animation Trigger")]
    public string JumpTrigger = "JumpTrigger";
    public string DieTrigger = "DieTrigger";
    [Header("Animation Bool")]
    public string IsGroundBool = "IsGround";
}
