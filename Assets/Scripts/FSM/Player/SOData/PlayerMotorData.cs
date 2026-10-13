namespace ProjectRE
{
    using UnityEngine;

    /// <summary>
    /// Player 전용 이동 수치 정의.
    /// GroundedMotorData의 지상 이동·점프 값에 Dash 수치 추가.
    /// </summary>
    [CreateAssetMenu(fileName = "PlayerMotorData", menuName = "Player/Motor Data")]
    public class PlayerMotorData : GroundedMotorData
    {
        [Header("Dash Settings")]
        [Min(0f)] public float dashSpeed = 12f;
        [Min(0f)] public float dashDistance = 5f;
        [Min(0)] public int maxAirDashCount = 1;
    }
}
