namespace ProjectRE
{
using UnityEngine;
[RequireComponent(typeof(AgentImpactHandler))]
[RequireComponent(typeof(PlayerAnimator))]
public class PlayerController : GroundedAgentController, IAttackComboStarter
{
    [SerializeField] private PlayerAnimator _playerAnimator;
    [SerializeField] private AgentImpactHandler _impactHandler;

    public Stamina Stamina { get; private set; }

    private PlayerInput _playerInput;
    private readonly ComboAttackHandler _comboAttackHandler = new();

    protected override void Awake()
    {
        base.Awake();
        _playerAnimator = GetComponent<PlayerAnimator>();
        _playerAnimator.Initialize();
        Stamina = GetComponent<Stamina>();
        Stamina?.Initialize(_statData.maxStamina, _statData.staminaRegenRate);


        _playerInput = GetComponent<PlayerInput>();
        _playerInput.OnAttackRequestsCleared += _comboAttackHandler.ResetGroundCombo;
        _impactHandler = GetComponent<AgentImpactHandler>();
        _impactHandler.Initialize(_motor, _motorData);

        _states = new PlayerStateFactory().CreateStates(
            new PlayerStateFactoryData
            {
                PlayerAnimator = _playerAnimator,
                Motor = _motor,
                MovementHandler = _movementHandler,
                MovementInput = _moveInput,
                CombatHandler = _combatHandler,
                Health = this.Health,
                GroundDetector = _groundDetector,
                JumpInput = _playerInput,
                CombatInput = CombatInput,
                AnimationEventSource = this,
                AttackStarter = this,
                ComboAttackHandler = _comboAttackHandler,
                ComboAttackStarter = this
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
    public override bool TryStartAttack(int requestedAttackType)
    {
        int attackType = IsGrounded ? requestedAttackType : 3;

        if (_combatHandler.CurrentAttackType != 0)
            return false;

        return TryApplyPlayerAttack(attackType);
    }

    public bool TryContinueAttack(int nextAttackType)
    {
        if (!IsGrounded
            || _combatHandler.CurrentAttackType == 0
            || nextAttackType != _combatHandler.CurrentAttackType + 1)
            return false;

        return TryApplyPlayerAttack(nextAttackType);
    }

    private bool TryApplyPlayerAttack(int attackType)
    {
        if (!_combatHandler.CanApplyAttackType(attackType))
            return false;

        if (!Stamina.Use(_statData.attackDatas[attackType - 1].usedStamina))
            return false;

        _combatHandler.ApplyAttackType(attackType);
        return true;
    }

    private void OnDestroy()
    {
        if (_playerInput != null)
            _playerInput.OnAttackRequestsCleared -= _comboAttackHandler.ResetGroundCombo;
    }
    #endregion
}
}
