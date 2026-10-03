namespace ProjectRE
{
    using UnityEngine;

    /// <summary>
    /// Player 전용 이동 수치를 정의한다.
    /// AgentMotorData의 일반 이동·점프 값에 Dash 수치를 추가한다.
    /// </summary>
    [CreateAssetMenu(fileName = "PlayerMotorData", menuName = "Player/Motor Data")]
    public class PlayerMotorData : AgentMotorData
    {
        [Header("Dash Settings")]
        [Min(0f)] public float dashSpeed = 12f;
        [Min(0f)] public float dashDistance = 5f;
        [Min(0)] public int maxAirDashCount = 1;
    }
}
