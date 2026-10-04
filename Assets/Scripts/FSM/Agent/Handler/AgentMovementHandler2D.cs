namespace ProjectRE
{
using UnityEngine;
/// <summary>
/// AgentMotor2D와 이동 데이터 사이에서 지상·공중 이동, 점프, 넉백 동작을 실행합니다.
/// </summary>
public class AgentMovementHandler2D 
{
    private AgentMotor2D _motor;
    private AgentMotorData _data;

    public AgentMovementHandler2D(AgentMotor2D motor, AgentMotorData data)
    {
        _motor = motor;
        _data = data;
    }

    /// <summary>지상 이동 속도로 입력 방향 이동을 적용합니다.</summary>
    public void HandleMove(Vector2 moveVec)
    {
        _motor.Move(moveVec, _data.moveSpeed);
    }
    /// <summary>공중 이동 속도로 입력 방향 이동을 적용합니다.</summary>
    public void HandleAirMove(Vector2 moveVec)
    {
        _motor.Move(moveVec, _data.airMoveSpeed);
    }

    /// <summary>설정된 점프 힘을 적용합니다.</summary>
    public void HandleJump()
    {
        _motor.Jump(_data.jumpForce);
    }

    /// <summary>위쪽 보정이 포함된 넉백 힘을 적용합니다.</summary>
    public void HandleKnockback(Vector2 direction)
    {
        Vector2 finalForce = (direction + Vector2.up * 0.5f).normalized;
        _motor.Knockback(finalForce, _data.knockbackForce);
    }
}
}
