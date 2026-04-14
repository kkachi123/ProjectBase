using UnityEngine;
[System.Serializable]
public class AgentMovementHandler2D 
{
    private AgentMotor2D _motor;
    private AgentMotorData _data;

    public void Initialize(AgentMotor2D motor, AgentMotorData data)
    {
        _motor = motor;
        _data = data;
    }

    public void HandleMove(Vector2 moveVec)
    {
        _motor.Move(moveVec, _data.moveSpeed);
    }

    public void HandleJump()
    {
        _motor.Jump(_data.jumpForce);
    }

    public void HandleKnockback(Vector2 direction)
    {
        Vector2 finalForce = (direction + Vector2.up * 0.5f).normalized;
        _motor.Knockback(finalForce, _data.knockbackForce);
    }
}
