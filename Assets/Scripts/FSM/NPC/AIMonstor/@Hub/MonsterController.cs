public class MonsterController : AgentController<MonsterStateBase>
{
    protected override void Awake()
    {
        base.Awake();

        _stateMachine = new AgentStateMachine<MonsterStateBase>();
        _states = new MonsterStateFactory().CreateStates(this);
    }
    public override void OnAttackAction(int attackType)
    {
        _combatHandler.SetAttackType(attackType);
        _stateMachine.CurrentState?.OnInputEvent(InputKeyType.Attack);
    }
}
