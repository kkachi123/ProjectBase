using UnityEngine;
[RequireComponent(typeof(AgentImpactHandler))]
public class PlayerController : GroundedAgentController
{
    [SerializeField] private AgentImpactHandler _impactHandler;

    public Stamina Stamina { get; private set; }

    private PlayerInput _playerInput;

    protected override void Awake()
    {
        base.Awake();
        Stamina = GetComponent<Stamina>();
        Stamina?.Initialize(_statData.maxStamina, _statData.staminaRegenRate);


        _playerInput = GetComponent<PlayerInput>();
        _impactHandler = GetComponent<AgentImpactHandler>();
        _impactHandler.Initialize(_motor, _motorData);

        _states = new PlayerStateFactory().CreateStates(
            new PlayerStateFactoryData
            {
                Animator = _animator,
                MovementHandler = _movementHandler,
                MovementInput = _moveInput,
                CombatHandler = _combatHandler,
                Health = this.Health,
                GroundDetector = _groundDetector,
                JumpInput = _playerInput,
                CombatInput = CombatInput,
                AnimationEventSource = this,
                AttackStarter = this
            }
        );
    }

    #region State Animation Event
    public override void OnDeathFinished()
    {
        Managers.Instance.AdventureRun.FailRun();
    }
    #endregion

    #region State Input Event
    public override bool CanStartAttack(int requestedAttackType)
    {
        int attackType = IsGrounded ? requestedAttackType : 3;

        return _combatHandler.CurrentAttackType == 0
            && Stamina.CanUse(_statData.attackDatas[attackType - 1].usedStamina);
    }
    
    public override bool TryStartAttack(int requestedAttackType)
    {
        int attackType = IsGrounded ? requestedAttackType : 3;

        if (!Stamina.Use(_statData.attackDatas[attackType - 1].usedStamina))
            return false;

        return _combatHandler.SetAttackType(attackType);
    }
    #endregion
}
