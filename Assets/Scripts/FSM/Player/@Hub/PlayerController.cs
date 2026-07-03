using UnityEngine;
[RequireComponent(typeof(AgentImpactHandler))]
public class PlayerController : GroundedAgentController
{
    [SerializeField] private AgentImpactHandler _impactHandler;
    private PlayerInput _playerInput;

    protected override void Awake()
    {
        base.Awake();
        _playerInput = GetComponent<PlayerInput>();
        _impactHandler = GetComponent<AgentImpactHandler>();
        _impactHandler.Initialize(_motor, _motorData);

        _states = new PlayerStateFactory().CreateStates(this);
    }

    #region Action Methods - State Operations

    public override void Attack(bool isAttack)
    {
        base.Attack(isAttack);
        if(IsGrounded) _movementHandler.HandleMove(Vector2.zero); 
    }

    #endregion

    #region State Animation Event
    public override void OnDeathFinished()
    {
        Managers.Instance.Game.TriggerGameOver();
    }
    #endregion

    #region State Input Event
    public override void OnAttackAction(int attackType)
    {
        if (!IsGrounded) attackType = 3;
        if (_combatHandler.CurrentAttackType != 0) return;
        if (!Stamina.Use(_statData.attackDatas[attackType - 1].usedStamina)) return;
        _combatHandler.SetAttackType(attackType);
        _stateMachine.CurrentState?.OnInputEvent(InputKeyType.Attack);
    }

    #endregion
}
