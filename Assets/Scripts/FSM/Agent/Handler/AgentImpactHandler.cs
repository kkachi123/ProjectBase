namespace ProjectRE
{
using UnityEngine;
public interface IKnockbackListener
{
    void HandleKnockback(Vector2 knockbackDir);
}

public class AgentImpactHandler : MonoBehaviour, IKnockbackListener
{
    private IAgentMotor2D _motor;
    private float _knockbackForce;

    public void Initialize(IAgentMotor2D motor, float knockbackForce)
    {
        _motor = motor;
        _knockbackForce = knockbackForce;
    }

    public void HandleKnockback(Vector2 direction)
    {
         Vector2 finalForce = (direction + Vector2.up * 0.5f).normalized;
        _motor.Knockback(finalForce, _knockbackForce);
    }
}
}
