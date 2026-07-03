using UnityEngine;

public class MonsterController : AgentController
{
    private Collider2D _collider;

    protected override void Awake()
    {
        base.Awake();
        _collider = GetComponent<Collider2D>();
        _states = new MonsterStateFactory().CreateStates(this);
    }
    #region State Animation Event
    public override void OnDeathFinished()
    {
        Destroy(gameObject);
    }
    #endregion

    #region State Input Event
    public override void OnAttackAction(int attackType)
    {
        if (!_combatHandler.SetAttackType(attackType)) return;
        _stateMachine.CurrentState?.OnInputEvent(InputKeyType.Attack);
    }
    #endregion
}
