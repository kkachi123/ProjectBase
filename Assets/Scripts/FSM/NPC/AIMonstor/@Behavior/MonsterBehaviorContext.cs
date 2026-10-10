namespace ProjectRE
{
    using System;
    using UniRx;
    using Unity.Behavior;
    using UnityEngine;

    /// <summary>씬 Player 연결·Handler 초기화·갱신 순서 관리.</summary>
    [DisallowMultipleComponent, DefaultExecutionOrder(-100)] // 컴포넌트 중복 방지 , 다른 컴포넌트보다 먼저 Awake() 호출
    [RequireComponent(typeof(MonsterInput), typeof(PlayerDetector), typeof(Health))]
    [RequireComponent(typeof(BehaviorGraphAgent))]
    public class MonsterBehaviorContext : MonoBehaviour
    {
        // Inspector 행동 설정
        [Header("Attack Requests")]
        [SerializeField] private MonsterAttackBehaviorHandler _attack = new();

        [Header("Chase and Return")]
        [SerializeField] private MonsterChaseReturnBehaviorHandler _chaseReturn = new();

        [Header("Patrol")]
        [SerializeField] private MonsterPatrolBehaviorHandler _patrol = new();

        [Header("Target Recognition")]
        [SerializeField] private MonsterTargetBehaviorHandler _targetHandler = new();

        // 자기 Component 참조
        private MonsterInput _input;
        private PlayerDetector _detector;
        private Health _health;
        private BehaviorGraphAgent _agent;
        private Collider2D _bodyCollider;

        // 런타임 관리 객체
        private MonsterBehaviorBlackboard _blackboard;

        // 구독 수명 관리
        private IDisposable _deathSubscription;

        // 외부 공개 접근자
        public MonsterInput Input => _input;
        public MonsterAttackBehaviorHandler Attack => _attack;
        public MonsterChaseReturnBehaviorHandler ChaseReturn => _chaseReturn;
        public MonsterPatrolBehaviorHandler Patrol => _patrol;
        public MonsterTargetBehaviorHandler Target => _targetHandler;

        /// <summary>자기 Component 참조 캐시.</summary>
        private void Awake()
        {
            _input = GetComponent<MonsterInput>();
            _detector = GetComponent<PlayerDetector>();
            _health = GetComponent<Health>();
            _bodyCollider = GetComponent<Collider2D>();
            _agent = GetComponent<BehaviorGraphAgent>();
        }

        /// <summary>실행 Blackboard·Handler 초기화 후 사망 구독·씬 Player 연결.</summary>
        private void Start()
        {
            var blackboard = new MonsterBehaviorBlackboard();
            blackboard.Bind(_agent);
            _blackboard = blackboard;
            _blackboard.Context = this;
            _chaseReturn.Initialize(transform.position.x, _blackboard);
            _patrol.Initialize(_blackboard);
            _targetHandler.Initialize(_detector, _bodyCollider, _blackboard);
            SubscribeDeath();
            BindPlayer();
        }

        /// <summary>재활성화 시 구독 복구 및 Graph 실행 전 판정 갱신.</summary>
        private void OnEnable()
        {
            if (_blackboard == null)
                return;

            SubscribeDeath();
            BindPlayer();
        }

        /// <summary>씬 Player 참조 주입 후 최초 판정 갱신. 구성 누락 시 비활성화.</summary>
        private void BindPlayer()
        {
            var manager = ScenePlayerManager.Instance;
            if (manager == null || manager.Player == null)
            {
                Debug.LogError($"{name}: ScenePlayerManager와 Inspector Player 할당 필요.", this);
                enabled = false;
                return;
            }

            var player = manager.Player;
            _targetHandler.BindPlayer(player.Health, player.GetComponent<Collider2D>());
            RefreshState();
        }

        /// <summary>대상·종합 판정·추적 복귀 순서로 갱신.</summary>
        private void Update() => RefreshState();

        /// <summary>대상 판정 갱신 후 추적·복귀 상태 전달.</summary>
        private void RefreshState()
        {
            if (_blackboard.IsDead)
                _targetHandler.Clear();
            else
                _targetHandler.UpdateTarget();
            _chaseReturn.UpdateState();
        }

        /// <summary>자기 사망 변경을 Blackboard에 구독 전달.</summary>
        private void SubscribeDeath()
        {
            _deathSubscription?.Dispose();
            _deathSubscription = _health.IsDead.Subscribe(isDead => _blackboard.IsDead = isDead);
        }

        /// <summary>초기화 후 사망 구독·대상·이동 입력 해제. 행동 기록 유지.</summary>
        private void OnDisable()
        {
            if (_blackboard == null)
                return;

            _deathSubscription?.Dispose();
            _deathSubscription = null;
            _targetHandler.Clear();
            _input.SetMovement(Vector2.zero);
        }
    }
}
