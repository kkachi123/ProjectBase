using UnityEngine;

[CreateAssetMenu(fileName = "DialogueData", menuName = "Game/Dialogue Data")]
public class DialogueDataSO : ScriptableObject
{
    public string SpeakerName;
    [TextArea(2, 5)]
    public string[] Lines;
}
