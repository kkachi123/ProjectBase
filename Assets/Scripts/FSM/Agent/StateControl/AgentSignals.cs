public sealed class AgentSignals
{
    public int AttackRequest { get; private set; }
    public bool JumpRequested { get; private set; }
    public bool HitRequested { get; private set; }
    public bool DeathRequested { get; private set; }

    public bool AnimationFinished { get; private set; }
    public int StateVersion { get; private set; }

    public void RequestAttack(int attackType) => AttackRequest = attackType;
    public void RequestJump() => JumpRequested = true;
    public void RequestHit() => HitRequested = true;
    public void RequestDeath() => DeathRequested = true;

    public void BeginState(int version)
    {
        StateVersion = version;
        AnimationFinished = false;
    }

    public void NotifyAnimationFinished(int sourceVersion)
    {
        if (sourceVersion == StateVersion)
            AnimationFinished = true;
    }

    // 이번 평가에서 채택되지 않은 입력은 다음 상태로 넘기지 않는다.
    public void ClearRequests()
    {
        AttackRequest = 0;
        JumpRequested = false;
        HitRequested = false;
        DeathRequested = false;
    }
}
