namespace ProjectRE
{
    using UnityEngine;
    [RequireComponent(typeof(AgentImpactHandler))]
    [RequireComponent(typeof(PlayerAnimator))]
    public class PlayerController : GroundedAgentController
    {

        [SerializeField] private PlayerAnimator _playerAnimator;
        [SerializeField] private AgentImpactHandler _impactHandler;

        public Stamina Stamina { get; private set; }

        private PlayerInput _playerInput;

        protected override void Awake()
        {
            base.Awake();

            _playerAnimator = GetComponent<PlayerAnimator>();
            _playerAnimator.Initialize();
            Stamina = GetComponent<Stamina>();
            Stamina?.Initialize(_statData.maxStamina, _statData.staminaRegenRate);


            _playerInput = GetComponent<PlayerInput>();
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
                    CombatInput = CombatInput,
                    Health = this.Health,
                    GroundDetector = _groundDetector,
                    JumpInput = _playerInput,
                    DashInput = _playerInput,
                    Stamina = Stamina,
                    StatData = _statData,
                    AnimationEventSource = this,
                    AttackStarter = this,
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
        public override bool TryStartAttack()
        {
            int attackType = IsGrounded ? 1 : 3;
            if (CheckCanPlayerAttack(attackType))
            {
                _combatHandler.ApplyAttackType(attackType);
                return true;
            }
            return false;
        }

        private bool CheckCanPlayerAttack(int attackType)
        {
            // 공격 타입이 설정한 범위를 벗어나면 공격을 시작하지 않는다.
            if (!_combatHandler.CanApplyAttackType(attackType))
                return false;

            // Stamina가 부족하면 공격을 시작하지 않는다.
            if (!Stamina.CanUse(_statData.attackDatas[attackType - 1].usedStamina))
                return false;
            return true;
        }

        #endregion
    }
}
