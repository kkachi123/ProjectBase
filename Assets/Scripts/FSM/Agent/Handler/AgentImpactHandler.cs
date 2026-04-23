using UnityEngine;
public interface IKnockbackListener
{
    void HandleKnockback(Vector2 knockbackDir);
}

public class AgentImpactHandler : MonoBehaviour, IKnockbackListener
{
    private AgentMotor2D _motor;
    private AgentMotorData _data;

    public void Initialize(AgentMotor2D motor, AgentMotorData data)
    {
        _motor = motor;
        _data = data;
    }

    public void HandleKnockback(Vector2 direction)
    {
         Vector2 finalForce = (direction + Vector2.up * 0.5f).normalized;
        _motor.Knockback(finalForce, _data.knockbackForce);
    }
}
