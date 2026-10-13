namespace ProjectRE
{
    using UnityEngine;

    /// <summary>지상 수평 이동과 분리된 자유 XY 비행 실행.</summary>
    public class FlyingMovementHandler2D : IAgentMovementStop
    {
        private readonly FlyingMotor2D _motor;
        private readonly FlyingMotorData _data;

        public FlyingMovementHandler2D(FlyingMotor2D motor, FlyingMotorData data)
        {
            _motor = motor;
            _data = data;
        }

        // 대각선 속도를 제한한 XY 이동.
        public void HandleMove(Vector2 movement) => _motor.Move2D(Vector2.ClampMagnitude(movement, 1f), _data.flyMoveSpeed);

        public void Stop() => _motor.Stop();
    }
}
