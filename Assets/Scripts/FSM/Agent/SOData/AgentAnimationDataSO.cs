namespace ProjectRE
{
using UnityEngine;

public class AgentAnimationDataSO : ScriptableObject
{
    [Header("Common Animation Bool")]
    // 피격 상태 진입·종료 제어.
    public string IsHitBool = "IsHit";
    // 사망 상태 진입 제어.
    public string IsDeathBool = "IsDeath";
}
}
