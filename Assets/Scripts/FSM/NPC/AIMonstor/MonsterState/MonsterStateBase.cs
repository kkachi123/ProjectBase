using UnityEngine;

public abstract class MonsterStateBase : IAgentState
{
    protected MonsterController _monster;
    public MonsterStateBase(MonsterController monsterController)
    {
        _monster = monsterController;
    }
    public abstract void Enter();
    public abstract void Execute();
    public virtual void FixedExecute() { }
    public abstract void Exit();

    public virtual void OnAnimationEvent(AnimEventType type) { }
    public virtual void OnInputEvent(InputKeyType type) { }
}
