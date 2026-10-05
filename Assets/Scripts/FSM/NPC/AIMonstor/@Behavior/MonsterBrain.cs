namespace ProjectRE
{
    using System;
    using UnityEngine;

    /// <summary>대상 추적·공격 요청·초기 위치 복귀를 입력으로 전달하는 독립 판단 Component.</summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-100)]
    [RequireComponent(typeof(MonsterInput), typeof(PlayerDetector), typeof(Health))]
    public class MonsterBrain : MonoBehaviour
    {
        private const float DistanceTolerance = 0.01f;

        [Header("Attack Requests")]
        [Min(0.01f)]
        [SerializeField] private float _attackDistance = 1.8f;
        [Min(0f)]
        [SerializeField] private float _maxAttackHeightDifference = 0.75f;
        [Min(0.01f)]
        [SerializeField] private float _attackRequestInterval = 1f;

        [Header("Chase and Return")]
        [Min(0.01f)]
        [SerializeField] private float _maxChaseDistance = 8f;
        [Min(0.01f)]
        [SerializeField] private float _returnArrivalDistance = 0.15f;

        private MonsterInput _input;
        private PlayerDetector _detector;
        private Health _health;
        private float _homeX;
        private bool _isReturning;
        private float _nextAttackRequestTime;

        private void Awake()
        {
            Initialize(GetComponent<MonsterInput>(), GetComponent<PlayerDetector>(), GetComponent<Health>());
        }

        private void Initialize(MonsterInput input, PlayerDetector detector, Health health)
        {
            if (input == null || detector == null || health == null)
                throw new InvalidOperationException($"{name}: MonsterBrain requires MonsterInput, PlayerDetector and Health.");

            if (!IsFinitePositive(_attackDistance)
                || float.IsNaN(_maxAttackHeightDifference) || float.IsInfinity(_maxAttackHeightDifference)
                || _maxAttackHeightDifference < 0f
                || !IsFinitePositive(_attackRequestInterval)
                || !IsFinitePositive(_maxChaseDistance)
                || !IsFinitePositive(_returnArrivalDistance)
                || _maxChaseDistance <= _returnArrivalDistance)
                throw new InvalidOperationException($"{name}: Invalid MonsterBrain distances or request interval.");

            _input = input;
            _detector = detector;
            _health = health;
        }

        private void Start()
        {
            _homeX = transform.position.x;
        }

        private void Update()
        {
            if (_input == null || _detector == null || _health == null)
                return;

            if (_health.IsDead.Value)
            {
                SetMovement(0f);
                return;
            }

            float currentX = transform.position.x;
            if (_isReturning)
            {
                ReturnHome(currentX);
                return;
            }

            if (Mathf.Abs(currentX - _homeX) > _maxChaseDistance + DistanceTolerance)
            {
                _isReturning = true;
                ReturnHome(currentX);
                return;
            }

            if (!_detector.IsTargetInView())
            {
                ReturnOrIdle(currentX);
                return;
            }

            Health targetHealth = _detector.Target.GetComponentInParent<Health>();
            if (targetHealth == null)
            {
                ReturnOrIdle(currentX);
                return;
            }

            Vector3 targetPosition = targetHealth.transform.position;
            if (Mathf.Abs(targetPosition.x - _homeX) > _maxChaseDistance + DistanceTolerance
                || Mathf.Abs(targetPosition.y - transform.position.y) > _maxAttackHeightDifference + DistanceTolerance)
            {
                ReturnOrIdle(currentX);
                return;
            }

            float deltaX = targetPosition.x - currentX;
            if (Mathf.Abs(deltaX) > _attackDistance + DistanceTolerance)
            {
                SetMovement(Mathf.Sign(deltaX));
                return;
            }

            // 기존 이동 입력으로 먼저 방향 정렬. 공격 요청은 다음 판단에서 전달.
            if (Mathf.Abs(deltaX) > DistanceTolerance
                && Mathf.Sign(deltaX) != Mathf.Sign(transform.localScale.x))
            {
                SetMovement(Mathf.Sign(deltaX));
                return;
            }

            SetMovement(0f);
            if (Time.time < _nextAttackRequestTime)
                return;

            // 실제 공격 시작 시각이 아닌 입력 발행 간격 관리.
            _nextAttackRequestTime = Time.time + _attackRequestInterval;
            _input.RequestAttack();
        }

        private void ReturnOrIdle(float currentX)
        {
            _isReturning = Mathf.Abs(currentX - _homeX) > _returnArrivalDistance + DistanceTolerance;
            if (_isReturning)
                ReturnHome(currentX);
            else
                SetMovement(0f);
        }

        private void ReturnHome(float currentX)
        {
            float deltaX = _homeX - currentX;
            if (Mathf.Abs(deltaX) <= _returnArrivalDistance + DistanceTolerance)
            {
                _isReturning = false;
                SetMovement(0f);
                return;
            }

            SetMovement(Mathf.Sign(deltaX));
        }

        private void SetMovement(float direction)
        {
            _input.SetMovement(new Vector2(direction, 0f));
        }

        private void OnDisable()
        {
            if (_input != null)
                SetMovement(0f);
        }

        private static bool IsFinitePositive(float value)
        {
            return value > 0f && !float.IsInfinity(value);
        }
    }
}
