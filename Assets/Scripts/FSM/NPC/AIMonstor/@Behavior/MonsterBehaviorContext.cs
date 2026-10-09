namespace ProjectRE
{
    using System;
    using UniRx;
    using Unity.Behavior;
    using UnityEngine;

    /// <summary>Handler 초기화·판정 조합·갱신 순서 관리.</summary>
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

        // 자기 Component 참조
        private MonsterInput _input;
        private PlayerDetector _detector;
        private Health _health;
        private BehaviorGraphAgent _agent;
        private Collider2D _bodyCollider;

        // 런타임 관리 객체
        private MonsterBehaviorBlackboard _blackboard;
        private readonly MonsterTargetBehaviorHandler _targetHandler = new();

        // 구독 수명 관리
        private IDisposable _deathSubscription;

        // 외부 공개 접근자
        public MonsterAttackBehaviorHandler Attack => _attack;
        public MonsterChaseReturnBehaviorHandler ChaseReturn => _chaseReturn;
        public MonsterPatrolBehaviorHandler Patrol => _patrol;
        public MonsterTargetBehaviorHandler Target => _targetHandler;
        public bool IsDead => _health.IsDead.Value;
        // 대상 유효성 판정. 사망·추적 범위·허용 높이 모두 만족 시 true.
        public bool HasValidTarget { get; private set; }

        /// <summary>자기 Component 참조 캐시.</summary>
        private void Awake()
        {
            _input = GetComponent<MonsterInput>();
            _detector = GetComponent<PlayerDetector>();
            _health = GetComponent<Health>();
            _bodyCollider = GetComponent<Collider2D>();
            _agent = GetComponent<BehaviorGraphAgent>();
        }

        /// <summary>실행 Blackboard 연결·Handler 주입·사망 구독·최초 판정.</summary>
        private void Start()
        {
            var blackboard = new MonsterBehaviorBlackboard();
            blackboard.Bind(_agent);
            _blackboard = blackboard;
            _blackboard.Context = this;
            _blackboard.Input = _input;
            _attack.Initialize(_bodyCollider, _blackboard);
            _chaseReturn.Initialize(transform, transform.position.x, _blackboard);
            _patrol.Initialize(_blackboard);
            _targetHandler.Initialize(_detector, _blackboard);
            SubscribeDeath();
            RefreshState();
        }

        /// <summary>재활성화 시 구독 복구 및 Graph 실행 전 판정 갱신.</summary>
        private void OnEnable()
        {
            if (_blackboard == null)
                return;

            SubscribeDeath();
            RefreshState();
        }

        /// <summary>대상·종합 판정·추적 복귀 순서로 갱신.</summary>
        private void Update() => RefreshState();

        /// <summary>대상 생존·추적 범위·허용 높이 조합 후 복귀 판정 전달.</summary>
        private void RefreshState()
        {
            if (IsDead)
                _targetHandler.Clear();
            else
                _targetHandler.UpdateTarget();

            HasValidTarget = _targetHandler.IsAlive
                && _chaseReturn.IsWithinChaseRange(_targetHandler.Root.position.x)
                && _attack.IsWithinHeightRange(_targetHandler.BodyCollider);
            _blackboard.HasValidTarget = HasValidTarget;
            _chaseReturn.UpdateState(HasValidTarget);
        }

        /// <summary>자기 사망 변경을 Blackboard에 구독 전달.</summary>
        private void SubscribeDeath()
        {
            _deathSubscription?.Dispose();
            _deathSubscription = _health.IsDead.Subscribe(isDead => _blackboard.IsDead = isDead);
        }

        /// <summary>비활성화 시 구독·대상·이동 입력 해제. 교전·복귀 기록 유지.</summary>
        private void OnDisable()
        {
            _deathSubscription?.Dispose();
            _deathSubscription = null;
            _targetHandler.Clear();
            HasValidTarget = false;
            if (_blackboard != null)
                _blackboard.HasValidTarget = false;
            if (_input != null)
                _input.SetMovement(Vector2.zero);
        }
    }
}
