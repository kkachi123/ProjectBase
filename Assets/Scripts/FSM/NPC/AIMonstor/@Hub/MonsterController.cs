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

        /// <summary>최초 위치 기록·Player 주입·전략 구성 후 전환 이벤트 연결.</summary>
        protected override void Start()
        {
            _target = new MonsterTargetHandler(_detector, _bodyCollider, _behaviorData.MaxTargetHeightDifference);
            if (!BindPlayer())
                return;
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
            ChangeState(Health.IsDead.Value ? typeof(DeathState) : typeof(MonsterPatrolWaitState));
        }

        /// <summary>씬 Player를 시작·재활성화 때만 확보.</summary>
        private bool BindPlayer()
        {
            var manager = ScenePlayerManager.Instance;
            if (manager == null || manager.Player == null)
            {
                Debug.LogError($"{name}: ScenePlayerManager와 Inspector Player 할당 필요.", this);
                enabled = false;
                return false;
            }
            var player = manager.Player;
            _target.BindPlayer(player.Health, player.GetComponent<Collider2D>());
            _target.UpdateTarget();
            return true;
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
            if (!_started || !BindPlayer())
                return;
            ChangeState(Health.IsDead.Value ? typeof(DeathState)
                : _target.HasValidTarget ? typeof(MonsterChaseState) : typeof(MonsterPatrolWaitState));
        }

        /// <summary>현재 State를 한 번 종료하고 이동·대상 정리.</summary>
        private void OnDisable()
        {
            if (!_started)
                return;
            AgentStateBase exitingState = _currentState;
            _currentState = null;
            exitingState?.Exit();
            _movement.Stop();
            _target.Clear();
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
