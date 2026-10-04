namespace ProjectRE
{
    using UnityEngine;
    [RequireComponent(typeof(AgentImpactHandler))]
    [RequireComponent(typeof(PlayerAnimator))]
    /// <summary>
    /// Player 전용 컴포넌트·FSM 초기화 및 공격 시작 조건 판정.
    /// </summary>
    public class PlayerController : GroundedAgentController
    {

        [SerializeField] private PlayerAnimator _playerAnimator;
        [SerializeField] private AgentImpactHandler _impactHandler;

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
            _dashHandler = GetComponent<AgentDashHandler2D>();
            _wallDetector = GetComponent<WallDetector>();
            if (_motorData is PlayerMotorData playerMotorData)
                _dashHandler.Initialize(playerMotorData, _groundDetector, _wallDetector);
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
                    DashHandler = _dashHandler,
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
        /// <summary>지상 여부에 따른 공격 타입 적용 및 공격 시작 가능 여부 반환.</summary>
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
            // 설정 범위를 벗어난 공격 타입 차단.
            if (!_combatHandler.CanApplyAttackType(attackType))
                return false;

            // Stamina 부족 시 공격 시작 차단.
            if (!Stamina.CanUse(_statData.attackDatas[attackType - 1].usedStamina))
                return false;
            return true;
        }

        #endregion
    }
}
