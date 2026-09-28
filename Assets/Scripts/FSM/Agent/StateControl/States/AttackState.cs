namespace ProjectRE
{
public class AttackState : AgentStateBase
{
    private AgentMotor2D _motor;
    private ICombatAnimation _animator;
    private AgentCombatHandler _combatHandler;
    private IAgentCombatInput _combatInput;
    private IAttackStarter _attackStarter;

    public AttackState(AgentMotor2D motor, ICombatAnimation animator , AgentCombatHandler combatHandler , IAgentCombatInput combatInput, IAttackStarter attackStarter)
    {
        _motor = motor;
        _animator = animator;
        _combatHandler = combatHandler;
        _combatInput = combatInput;
        _attackStarter = attackStarter;
    }

    protected override void OnEnter()
    {
        int requestedType = _combatInput.HeldAttackType;

        if (!_attackStarter.TryStartAttack(requestedType))
            return;
        _animator.SetAttackType(_combatHandler.CurrentAttackType);
        _animator.SetAttack(true);
        if(_combatHandler.CurrentAttackType != 3)
        {
            _motor.StopHorizontal();
        }
    }
    protected override void OnExecute(float deltatime)
    {
        return;
    }
    
    public override void Exit() 
    { 
        _combatHandler.ResetAttackType();
        _animator.SetAttack(false);
        _animator.SetAttackType(0);
    }
}
}
