namespace ProjectRE
{
using UnityEngine;

public class AgentAnimationDataSO : ScriptableObject
{
    [Header("Common Animation Bool")]
    // 피격 상태 진입과 종료를 제어한다.
    public string IsHitBool = "IsHit";
    // 사망 상태 진입을 제어한다.
    public string IsDeathBool = "IsDeath";
}
}
