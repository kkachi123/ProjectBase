namespace ProjectRE
{
    using UnityEngine;

    /// <summary>자유 비행 이동 수치.</summary>
    [CreateAssetMenu(fileName = "FlyingMotorData", menuName = "Agent/Flying Motor Data")]
    public class FlyingMotorData : ScriptableObject
    {
        public float knockbackForce = 3f;
        public float flyMoveSpeed = 3f;
    }
}
