namespace ProjectRE
{
    /// <summary>공통 정지 참조를 재사용한 비행 Factory 의존성.</summary>
    public class FlyingStateFactoryData : StateFactoryData
    {
        public FlyingMotor2D Motor { get; set; }
        public FlyingMovementHandler2D MovementHandler { get; set; }
        public override IAgentMovementStop MovementStop => MovementHandler;
    }
}
