namespace ProjectRE
{
    using System;
    using UnityEngine;

    [RequireComponent(typeof(MonsterAnimator), typeof(AgentMeleeHitHandler2D))]
    [RequireComponent(typeof(GroundDetector), typeof(GroundedMotor2D), typeof(PlayerDetector))]
    /// <summary>입력·BT 없이 지상 Monster 대상 갱신·판단 FSM·공격 초기화.</summary>
    public class MonsterController : AgentController
    {
        [SerializeField] private GroundedMotorData _motorData;
        [SerializeField] private GroundDetector _groundDetector;
        [SerializeField] private AgentMeleeHitHandler2D _combatHandler;
        [SerializeField] private MonsterBehaviorDataSO _behaviorData;
        [SerializeField] private MonsterAttackHandler _attackHandler = new();
        public MonsterAttackHandler AttackHandler => _attackHandler;
        public Type CurrentStateType => _currentState?.GetType();

        private MonsterAnimator _monsterAnimator;
        private GroundedMotor2D _motor;
        private GroundedMovementHandler2D _movement;
        private PlayerDetector _detector;
        private Collider2D _bodyCollider;
        private MonsterTargetHandler _target;
        private TestPlayerManager _playerManager;
        // 최초 Start 전의 OnEnable·OnDisable만 구분.
        private bool _started;

        protected override void Awake()
        {
            base.Awake();
            _monsterAnimator = GetComponent<MonsterAnimator>();
            _monsterAnimator.Initialize();
            _groundDetector = GetComponent<GroundDetector>();
            _motor = GetComponent<GroundedMotor2D>();
            _movement = new GroundedMovementHandler2D(_motor, _motorData);
            _combatHandler = GetComponent<AgentMeleeHitHandler2D>();
            _detector = GetComponent<PlayerDetector>();
            _bodyCollider = GetComponent<Collider2D>();
        }

        /// <summary>대상 유무와 무관하게 최초 위치·전략·FSM 구성 후 Player 연결.</summary>
        protected override void Start()
        {
            _target = new MonsterTargetHandler(_detector, _bodyCollider, _behaviorData.MaxTargetHeightDifference);
            _attackHandler.Initialize(_motor, _movement, _target, _monsterAnimator, _combatHandler, _behaviorData.ArrivalDistance);
            _states = new MonsterStateFactory().CreateStates(new MonsterStateFactoryData
            {
                Owner = transform,
                HomeX = transform.position.x,
                MonsterAnimator = _monsterAnimator,
                Motor = _motor,
                MovementHandler = _movement,
                BehaviorData = _behaviorData,
                Target = _target,
                Attack = _attackHandler,
                Health = Health,
                AnimationEventSource = this
            });
            base.Start();
            _started = true;
            ConnectPlayer();
            ChangeState(Health.IsDead.Value ? typeof(DeathState) : typeof(MonsterPatrolWaitState));
        }

        /// <summary>직접 배치 Player 우선 사용. 그 외에는 생성·등록 이벤트 연결.</summary>
        private void ConnectPlayer()
        {
            ScenePlayerManager sceneManager = ScenePlayerManager.Instance;
            if (sceneManager != null && sceneManager.Player != null)
            {
                BindPlayer(sceneManager.Player);
                return;
            }

            _playerManager = Managers.Instance?.Player;
            if (_playerManager != null)
            {
                _playerManager.OnPlayerChanged += BindPlayer;
                BindPlayer(_playerManager.CurrentPlayer);
            }
        }

        /// <summary>등록·해제 시 대상 참조와 인식 결과 즉시 갱신.</summary>
        private void BindPlayer(PlayerController player)
        {
            if (player == null)
            {
                _target.Clear();
                return;
            }
            _target.BindPlayer(player.Health, player.GetComponent<Collider2D>());
            _target.UpdateTarget();
        }

        /// <summary>등록 이벤트 구독 해제.</summary>
        private void DisconnectPlayer()
        {
            if (_playerManager == null)
                return;
            _playerManager.OnPlayerChanged -= BindPlayer;
            _playerManager = null;
        }

        /// <summary>대상 판정 갱신 후 현재 State 전이·행동 실행.</summary>
        protected override void Update()
        {
            if (!_started)
                return;
            if (Health.IsDead.Value)
                _target.Clear();
            else
                _target.UpdateTarget();
            base.Update();
        }

        protected override void FixedUpdate() => _groundDetector.UpdateGroundedStatus();

        /// <summary>HomeX·공격 간격 보존 후 대상 재주입·행동 재개.</summary>
        private void OnEnable()
        {
            if (!_started)
                return;
            ConnectPlayer();
            ChangeState(Health.IsDead.Value ? typeof(DeathState)
                : _target.HasValidTarget ? typeof(MonsterChaseState) : typeof(MonsterPatrolWaitState));
        }

        /// <summary>현재 State를 한 번 종료하고 이동·대상 정리.</summary>
        private void OnDisable()
        {
            if (!_started)
                return;
            DisconnectPlayer();
            AgentStateBase exitingState = _currentState;
            _currentState = null;
            exitingState?.Exit();
            _movement.Stop();
            _target.Clear();
        }

        /// <summary>현재 State 종료 후 개체별 공격 SO 복제본 제거.</summary>
        protected override void OnDestroy()
        {
            base.OnDestroy();
            _attackHandler.Dispose();
        }

        /// <summary>공격 종료는 전략에 전달. Hit·Death는 공용 Event 흐름 유지.</summary>
        public override void OnAnimationEvent(AnimEventType type, int intParameter = 0)
        {
            if (type == AnimEventType.End && _currentState is MonsterAttackState attackState)
            {
                attackState.OnAnimationEnd();
                return;
            }
            base.OnAnimationEvent(type, intParameter);
        }

        public override void OnDeathFinished() => Destroy(gameObject);
    }
}
