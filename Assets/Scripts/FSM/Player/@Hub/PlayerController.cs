namespace ProjectRE
{
    using UnityEngine;
    [RequireComponent(typeof(AgentImpactHandler))]
    [RequireComponent(typeof(PlayerAnimator))]
    [RequireComponent(typeof(PlayerInput), typeof(AgentMeleeHitHandler2D))]
    /// <summary>
    /// Player 전용 컴포넌트·공격 Handler·FSM 초기화.
    /// </summary>
    public class PlayerController : GroundedAgentController
    {

        [SerializeField] private PlayerAnimator _playerAnimator;
        [SerializeField] private AgentImpactHandler _impactHandler;
        [SerializeField] private PlayerMotorData _motorData;
        [SerializeField] private PlayerAttackHandler _attackHandler = new();
        [SerializeField] private AgentMeleeHitHandler2D _combatHandler;
        public PlayerAttackHandler AttackHandler => _attackHandler;
        protected override GroundedMotorData MovementData => _motorData;

        public Stamina Stamina { get; private set; }

        private PlayerInput _playerInput;
        private AgentDashHandler2D _dashHandler;
        private WallDetector _wallDetector;

        protected override void Awake()
        {
            base.Awake();

            _playerAnimator = GetComponent<PlayerAnimator>();
            _playerAnimator.Initialize();
            Stamina = GetComponent<Stamina>();
            Stamina?.Initialize(_statData.maxStamina, _statData.staminaRegenRate);


            _playerInput = GetComponent<PlayerInput>();
            _combatHandler = GetComponent<AgentMeleeHitHandler2D>();
            _dashHandler = GetComponent<AgentDashHandler2D>();
            _wallDetector = GetComponent<WallDetector>();
            _dashHandler.Initialize(_motorData, _groundDetector, _wallDetector);
            _impactHandler = GetComponent<AgentImpactHandler>();
            _impactHandler.Initialize(_groundedMotor, _motorData.knockbackForce);
            _attackHandler.Initialize(_groundDetector, Stamina, _groundedMotor, _playerInput, _playerAnimator, _combatHandler);

            _states = new PlayerStateFactory().CreateStates(
                new PlayerStateFactoryData
                {
                    PlayerAnimator = _playerAnimator,
                    Motor = _groundedMotor,
                    MovementHandler = _movementHandler,
                    MovementInput = _playerInput,
                    CombatInput = _playerInput,
                    Health = this.Health,
                    GroundDetector = _groundDetector,
                    JumpInput = _playerInput,
                    DashInput = _playerInput,
                    DashHandler = _dashHandler,
                    AttackHandler = _attackHandler,
                    AnimationEventSource = this,
                    AttackStarter = _attackHandler,
                }
            );
        }

        protected override void Start()
        {
            base.Start();
            ChangeState(typeof(GroundedState));
        }

        #region State Animation Event
        public override void OnDeathFinished()
        {
            Managers.Instance.AdventureRun.FailRun();
        }
        #endregion

    }
}
