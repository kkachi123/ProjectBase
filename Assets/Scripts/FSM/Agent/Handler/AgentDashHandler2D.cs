namespace ProjectRE
{
    using UnityEngine;

    /// <summary>
    /// Dash의 물리 실행 수명만 관리한다.
    /// State 전이는 담당하지 않으며, 거리 도달 또는 전방 벽 감지 결과만 제공한다.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class AgentDashHandler2D : MonoBehaviour
    {
        private Rigidbody2D _rigidbody;
        private PlayerMotorData _motorData;
        private GroundDetector _groundDetector;
        private WallDetector _wallDetector;

        // DashState가 실행 중인 동안 Dash 종료 조건을 만족했는지 나타낸다.
        private bool _isCompleted;
        // DashState 진입 시 Player Scale을 저장.
        private float _savedGravityScale;
        // DashState 진입 시 X축 위치를 저장.
        private float _startPositionX;
        // DashState 진입 시 이동 방향을 저장. Scale.x >= 0이면 1, Scale.x < 0이면 -1.
        private float _dashDirection;
        // 공중 Dash 횟수 제한을 위해 사용한 횟수를 저장한다.
        private int _usedAirDashCount;

        /// <summary>거리 도달 또는 WallDetector 감지로 Dash를 종료할 수 있는 상태인지 나타낸다.</summary>
        public bool IsCompleted => _isCompleted;

        /// <summary>Dash 실행에 필요한 데이터와 감지기를 주입하고 착지 이벤트를 구독합니다.</summary>
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

            // 초기화 시점에 이미 지상이라면, 이전에 남아 있을 수 있는 공중 Dash 횟수를 정리한다.
            if (_groundDetector.IsGrounded)
                ResetAirDashCount();
        }
        /// <summary>현재 지면 상태와 남은 공중 Dash 횟수를 기준으로 Dash 시작 가능 여부를 반환합니다.</summary>
        public bool CanStartDash()
        {
            // 지상에 있거나 공중 Dash 횟수가 남아있으면 DashState 진입을 허용한다.
            return _groundDetector.IsGrounded || _usedAirDashCount < _motorData.maxAirDashCount;
        }

        /// <summary>
        /// DashState 진입 후 실제 Dash 세션을 시작한다.
        /// </summary>
        public void BeginDash()
        {
            // 착지 시점의 초기화는 GroundDetector 이벤트가 담당한다.
            if (!_groundDetector.IsGrounded)
                _usedAirDashCount++;

            _isCompleted = false;
            _startPositionX = _rigidbody.position.x;
            _dashDirection = transform.localScale.x >= 0f ? 1f : -1f;
            _savedGravityScale = _rigidbody.gravityScale;

            // Dash 중에는 X축만 이동하므로 기존 수직 운동을 제거하고 중력을 잠시 정지한다.
            _rigidbody.linearVelocity = Vector2.zero;
            _rigidbody.gravityScale = 0f;
        }

        /// <summary>
        /// DashState가 실행 중인 프레임에만 호출해 Dash 이동 및 종료 조건을 갱신한다.
        /// Handler는 MonoBehaviour 생명주기에서 독립적으로 Dash를 진행하지 않는다.
        /// </summary>
        public void ExecuteDash()
        {
            if ( _isCompleted)
                return;

            // 전방에 벽이 감지되면 Dash를 완료한다.
            if (_wallDetector.IsWallInFront())
            {
                CompleteDash();
                return;
            }

            // 설정 목표까지 이동했는지 확인.
            float travelledDistance = Mathf.Abs(_rigidbody.position.x - _startPositionX);
            float remainingDistance = _motorData.dashDistance - travelledDistance;
            // 부동소수점 오차로 인해 목표 거리를 약간 초과할 수 있으므로, 0.001f 이하로 남으면 Dash를 완료한다.
            if (remainingDistance <= 0.001f)
            {
                CompleteDash();
                return;
            }

            // 설정한 dashSpeed로 이동한다. 
            float maxStepSpeed = remainingDistance / Time.fixedDeltaTime;
            float dashSpeed = Mathf.Min(_motorData.dashSpeed, maxStepSpeed);
            _rigidbody.linearVelocity = new Vector2(_dashDirection * dashSpeed, 0f);
        }
        
        /// <summary>
        /// DashState 종료 시 항상 호출해 물리 설정을 원래 상태로 복구한다.
        /// </summary>
        public void EndDash()
        {
            _rigidbody.linearVelocity = Vector2.zero;
            _rigidbody.gravityScale = _savedGravityScale;
        }


        // Dash State가 종료 조건을 만족했음을 나타내고, Rigidbody를 정지시킨다.
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
