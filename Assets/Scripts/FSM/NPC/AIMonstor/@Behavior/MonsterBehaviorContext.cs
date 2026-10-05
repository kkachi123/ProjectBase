namespace ProjectRE
{
    using Unity.Behavior;
    using UnityEngine;

    /// <summary>감지 결과·초기 위치·행동 설정을 개체별 Blackboard에 전달.</summary>
    [DisallowMultipleComponent, DefaultExecutionOrder(-100)]
    [RequireComponent(typeof(MonsterInput), typeof(PlayerDetector), typeof(Health))]
    [RequireComponent(typeof(BehaviorGraphAgent))]
    public class MonsterBehaviorContext : MonoBehaviour
    {
        [Header("Attack Requests")]
        [SerializeField] private MonsterAttackBehaviorHandler _attack = new();

        [Header("Chase and Return")]
        [SerializeField] private MonsterChaseReturnBehaviorHandler _chaseReturn = new();

        [Header("Patrol")]
        [SerializeField] private MonsterPatrolBehaviorHandler _patrol = new();

        private MonsterInput _input;
        private PlayerDetector _detector;
        private Health _health;
        private BehaviorGraphAgent _agent;
        private Transform _target;
        private Health _targetHealth;
        private Collider2D _targetBodyCollider;
        private BlackboardVariable<bool> _deadVariable;
        private BlackboardVariable<bool> _returnVariable;
        private BlackboardVariable<bool> _lostVariable;
        private BlackboardVariable<bool> _targetValidVariable;
        private BlackboardVariable<GameObject> _targetVariable;

        public float HomeX => _chaseReturn.HomeX;
        public float AttackDistance => _attack.AttackDistance;
        public float ArrivalDistance => _chaseReturn.ArrivalDistance;
        public bool IsDead => _health.IsDead.Value;
        public bool HasValidTarget { get; private set; }
        public bool NeedsReturn => _chaseReturn.NeedsReturn;
        public bool NeedsLostTargetWait => _chaseReturn.NeedsLostTargetWait(HasValidTarget);
        public Transform TargetRoot => _targetHealth != null ? _targetHealth.transform : null;

        /// <summary>자기 참조 캐시 및 그래프 초기 설정 전달.</summary>
        private void Awake()
        {
            _input = GetComponent<MonsterInput>();
            _detector = GetComponent<PlayerDetector>();
            _health = GetComponent<Health>();
            _attack.Initialize(GetComponent<Collider2D>());
            _agent = GetComponent<BehaviorGraphAgent>();
            _agent.SetVariableValue("Context", this);
            _agent.SetVariableValue("Input", _input);
            _agent.SetVariableValue("AttackRequestInterval", _attack.AttackRequestInterval);
            _agent.SetVariableValue("LostTargetWait", _chaseReturn.LostTargetWait);
            _agent.SetVariableValue("PatrolRadius", _patrol.PatrolRadius);
            _agent.SetVariableValue("PatrolWaitMin", _patrol.PatrolWaitMin);
            _agent.SetVariableValue("PatrolWaitMax", _patrol.PatrolWaitMax);
            _agent.SetVariableValue("RearApproachTimeout", _attack.RearApproachTimeout);
        }

        /// <summary>초기 X 좌표 저장 및 실행 Blackboard 참조 캐시.</summary>
        private void Start()
        {
            _chaseReturn.Initialize(transform, transform.position.x);
            _agent.SetVariableValue("HomeX", HomeX);
            _agent.GetVariable("IsDead", out _deadVariable);
            _agent.GetVariable("NeedsReturn", out _returnVariable);
            _agent.GetVariable("NeedsLostTargetWait", out _lostVariable);
            _agent.GetVariable("HasValidTarget", out _targetValidVariable);
            _agent.GetVariable("Target", out _targetVariable);
        }

        /// <summary>감지·높이·추적 제한 결과 갱신. 이동·공격 명령 제외.</summary>
        private void Update()
        {
            UpdateTargetReferences(!IsDead && _detector.IsTargetInView() ? _detector.Target : null);
            bool hasTarget = _targetHealth != null && !_targetHealth.IsDead.Value;
            HasValidTarget = hasTarget
                && _chaseReturn.IsWithinChaseRange(TargetRoot.position.x)
                && _attack.IsWithinHeightRange(_targetBodyCollider);
            _chaseReturn.UpdateState();
            PublishFlags();
        }

        /// <summary>대상 변경 시 체력·몸통 Collider 캐시, 감지 해제 시 초기화.</summary>
        private void UpdateTargetReferences(Transform target)
        {
            if (_target == target)
                return;

            _target = target;
            _targetHealth = target != null ? target.GetComponentInParent<Health>() : null;
            _targetBodyCollider = _targetHealth != null ? _targetHealth.GetComponent<Collider2D>() : null;
        }

        /// <summary>추적 시작 기록. 일반 순찰과 대상 상실을 구분.</summary>
        public void BeginEngagement() => _chaseReturn.BeginEngagement();

        /// <summary>복귀 완료 전 재추적 방지.</summary>
        public void BeginReturn()
        {
            _chaseReturn.BeginReturn();
            PublishFlags();
        }

        /// <summary>복귀 완료 후 교전·복귀 기록 초기화.</summary>
        public void CompleteReturn()
        {
            _chaseReturn.CompleteReturn();
            PublishFlags();
        }

        /// <summary>공격 관리자의 수평 거리 판정 전달.</summary>
        public bool IsInAttackRange(float deltaX) => _attack.IsInAttackRange(deltaX);

        /// <summary>우선순위 조건과 대상 참조를 Blackboard에 반영.</summary>
        private void PublishFlags()
        {
            _deadVariable.Value = IsDead;
            _returnVariable.Value = NeedsReturn;
            _lostVariable.Value = NeedsLostTargetWait;
            _targetValidVariable.Value = HasValidTarget;
            _targetVariable.Value = TargetRoot != null ? TargetRoot.gameObject : null;
        }

        /// <summary>비활성화 시 이동 입력 초기화.</summary>
        private void OnDisable()
        {
            if (_input != null)
                _input.SetMovement(Vector2.zero);
        }
    }
}
