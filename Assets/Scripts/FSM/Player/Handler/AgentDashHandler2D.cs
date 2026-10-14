namespace ProjectRE
{
    using UnityEngine;

    /// <summary>
    /// Dash 물리 실행 수명 관리.
    /// 이동 거리 도달·전방 벽 감지 결과 제공. 상태 전이는 Transition에서 처리.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class AgentDashHandler2D : MonoBehaviour
    {
        private Rigidbody2D _rigidbody;
        private PlayerMotorData _motorData;
        private GroundDetector _groundDetector;
        private WallDetector _wallDetector;

        // Dash 종료 조건 충족 여부.
        private bool _isCompleted;
        // DashState 진입 시 gravityScale 저장.
        private float _savedGravityScale;
        // DashState 진입 시 X축 위치 저장.
        private float _startPositionX;
        // Dash 이동 방향. Scale.x >= 0이면 1, 음수면 -1.
        private float _dashDirection;
        // 사용한 공중 Dash 횟수.
        private int _usedAirDashCount;

        /// <summary>목표 거리 도달·전방 벽 감지에 따른 Dash 완료 여부.</summary>
        public bool IsCompleted => _isCompleted;

        /// <summary>Dash 데이터·감지기 주입 및 착지 이벤트 구독.</summary>
        public void Initialize(
            PlayerMotorData motorData,
            GroundDetector groundDetector,
            WallDetector wallDetector)
        {
            if (_groundDetector != null)
                _groundDetector.OnGroundedChanged -= HandleGroundedChanged;

            _rigidbody = GetComponent<Rigidbody2D>();
            _motorData = motorData;
            _groundDetector = groundDetector;
            _wallDetector = wallDetector;
            bool _isInitialized = _rigidbody != null && _motorData != null && _groundDetector != null && _wallDetector != null;

            if (!_isInitialized)
            {
                Debug.LogError("AgentDashHandler2D 초기화에 필요한 참조가 없습니다.", this);
                return;
            }

            _groundDetector.OnGroundedChanged += HandleGroundedChanged;

            // 초기화 시 이미 지상이면 공중 Dash 횟수 초기화.
            if (_groundDetector.IsGrounded)
                ResetAirDashCount();
        }
        /// <summary>지면 상태·남은 공중 Dash 횟수 기반 시작 가능 여부 반환.</summary>
        public bool CanStartDash()
        {
            // 지상이거나 공중 Dash 횟수가 남아 있으면 진입 허용.
            return _groundDetector.IsGrounded || _usedAirDashCount < _motorData.maxAirDashCount;
        }

        /// <summary>
        /// DashState 진입 후 Dash 세션 시작.
        /// </summary>
        public void BeginDash()
        {
            // 착지 시 공중 Dash 횟수 초기화는 GroundDetector 이벤트에서 처리.
            if (!_groundDetector.IsGrounded)
                _usedAirDashCount++;

            _isCompleted = false;
            _startPositionX = _rigidbody.position.x;
            _dashDirection = transform.localScale.x >= 0f ? 1f : -1f;
            _savedGravityScale = _rigidbody.gravityScale;

            // X축 이동을 위해 기존 수직 운동 제거 및 중력 정지.
            _rigidbody.linearVelocity = Vector2.zero;
            _rigidbody.gravityScale = 0f;
        }

        /// <summary>
        /// DashState 실행 중 이동·종료 조건 갱신.
        /// 실행 호출은 DashState에서 관리.
        /// </summary>
        public void ExecuteDash()
        {
            if ( _isCompleted)
                return;

            // 전방 벽 감지 시 Dash 완료.
            if (_wallDetector.IsWallInFront())
            {
                CompleteDash();
                return;
            }

            // 설정 목표까지 이동했는지 확인.
            float travelledDistance = Mathf.Abs(_rigidbody.position.x - _startPositionX);
            float remainingDistance = _motorData.dashDistance - travelledDistance;
            // 부동소수점 오차 허용: 남은 거리 0.001f 이하에서 Dash 완료.
            if (remainingDistance <= 0.001f)
            {
                CompleteDash();
                return;
            }

            // dashSpeed 적용. 마지막 물리 틱은 남은 거리로 속도 보정.
            float maxStepSpeed = remainingDistance / Time.fixedDeltaTime;
            float dashSpeed = Mathf.Min(_motorData.dashSpeed, maxStepSpeed);
            _rigidbody.linearVelocity = new Vector2(_dashDirection * dashSpeed, 0f);
        }
        
        /// <summary>
        /// DashState 종료 시 속도 초기화 및 기존 중력 복구.
        /// </summary>
        public void EndDash()
        {
            _rigidbody.linearVelocity = Vector2.zero;
            _rigidbody.gravityScale = _savedGravityScale;
        }


        // Dash 완료 표시 및 Rigidbody 정지.
        private void CompleteDash()
        {
            _rigidbody.linearVelocity = Vector2.zero;
            _isCompleted = true;
        }

        private void HandleGroundedChanged(bool isGrounded)
        {
            if (isGrounded)
                ResetAirDashCount();
        }

        private void ResetAirDashCount()
        {
            _usedAirDashCount = 0;
        }

        private void OnDestroy()
        {
            if (_groundDetector != null)
                _groundDetector.OnGroundedChanged -= HandleGroundedChanged;
        }
    }
}
