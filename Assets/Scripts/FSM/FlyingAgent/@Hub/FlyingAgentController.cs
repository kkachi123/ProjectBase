namespace ProjectRE
{
    using UnityEngine;

    /// <summary>비행 이동 Handler 초기화. 구체 State·시작 정책은 파생 Controller에서 구성.</summary>
    [RequireComponent(typeof(FlyingMotor2D))]
    public abstract class FlyingAgentController : AgentController
    {
        [SerializeField] protected FlyingMotorData _motorData;
        protected FlyingMotor2D _flyingMotor;
        protected FlyingMovementHandler2D _movementHandler;

        protected override void Awake()
        {
            base.Awake();
            _flyingMotor = GetComponent<FlyingMotor2D>();
            _movementHandler = new FlyingMovementHandler2D(_flyingMotor, _motorData);
        }

        protected override void FixedUpdate() { }
    }
}
