namespace ProjectRE
{
    using UnityEngine;

    /// <summary>이동 프로필과 무관한 전체 정지·넉백 계약.</summary>
    public interface IAgentMotor2D
    {
        void Stop();
        void Knockback(Vector3 direction, float force);
    }
}
