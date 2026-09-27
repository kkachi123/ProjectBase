namespace ProjectRE
{
public interface IHitAnimation
{
    void SetHit(bool value);
}

public interface IDeathAnimation
{
    void SetDeath(bool value);
}

public interface IGroundedAnimation
{
    void SetGrounded(bool value);
    void SetMoveSpeed(float value);
}

public interface IAirborneAnimation
{
    void SetJump(bool value);
    void SetFall(bool value);
}

public interface ICombatAnimation
{
    void SetAttack(bool value);
    void SetAttackType(int attackType);
}
}
