namespace ProjectRE
{
    using UnityEngine;

    /// <summary>비행 Monster 대상 갱신과 FSM 초기화.</summary>
    [RequireComponent(typeof(FlyingMonsterAnimator), typeof(PlayerDetector), typeof(FlyingMonsterImpactHandler))]
    public class FlyingMonsterController : FlyingAgentController
    {
        [SerializeField] private FlyingMonsterBehaviorDataSO _behaviorData;
        private FlyingMonsterAnimator _animator;
        private FlyingMonsterImpactHandler _impact;
        private FlyingMonsterTargetHandler _target;
        private TestPlayerManager _playerManager;
        private bool _started;

        protected override void Awake()
        {
            base.Awake();
            _animator = GetComponent<FlyingMonsterAnimator>();
            _animator.Initialize();
            _impact = GetComponent<FlyingMonsterImpactHandler>();
            _impact.Initialize(_flyingMotor, _motorData.knockbackForce);
            _target = new FlyingMonsterTargetHandler(GetComponent<PlayerDetector>());
        }

        // 최초 복귀 위치와 FSM 연결.
        protected override void Start()
        {
            _states = new FlyingMonsterStateFactory().CreateStates(new FlyingMonsterStateData
            {
                Owner = transform,
                Home = transform.position,
                Behavior = _behaviorData,
                Target = _target,
                Motor = _flyingMotor,
                MovementHandler = _movementHandler,
                MoveSpeed = _motorData.flyMoveSpeed,
                Animator = _animator,
                Health = Health,
                AnimationEventSource = this
            });
            base.Start();
            _started = true;
            ConnectPlayer();
            ChangeState(Health.IsDead.Value ? typeof(DeathState) : typeof(FlyingPatrolWaitState));
        }

        // 현재 등록된 Player 연결.
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

        private void BindPlayer(PlayerController player)
        {
            if (player == null) _target.Clear();
            else _target.BindPlayer(player.Health, player.GetComponent<Collider2D>());
        }

        private void DisconnectPlayer()
        {
            if (_playerManager == null) return;
            _playerManager.OnPlayerChanged -= BindPlayer;
            _playerManager = null;
        }

        // 전이·이동 정리 후 대기 중인 넉백 적용.
        protected override void Update()
        {
            if (!_started) return;
            if (Health.IsDead.Value) _target.Clear();
            else _target.Refresh();
            base.Update();
            if (_currentState is HitState && !Health.IsDead.Value) _impact.ApplyPending();
            else _impact.Clear();
        }

        private void OnEnable()
        {
            if (!_started) return;
            ConnectPlayer();
            ChangeState(Health.IsDead.Value ? typeof(DeathState)
                : _target.HasValidTarget ? typeof(FlyingChaseState) : typeof(FlyingPatrolWaitState));
        }

        private void OnDisable()
        {
            if (!_started) return;
            DisconnectPlayer();
            AgentStateBase exiting = _currentState;
            _currentState = null;
            exiting?.Exit();
            _impact.Clear();
            _movementHandler.Stop();
            _target.Clear();
        }

        public override void OnDeathFinished() => Destroy(gameObject);
    }
}
