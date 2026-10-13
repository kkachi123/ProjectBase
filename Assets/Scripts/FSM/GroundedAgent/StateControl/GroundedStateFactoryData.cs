namespace ProjectRE
{
    /// <summary>공통 정지 참조를 재사용한 지상 Factory 의존성.</summary>
    public class GroundedStateFactoryData : StateFactoryData
    {
        public GroundedMotor2D Motor { get; set; }
        public GroundedMovementHandler2D MovementHandler { get; set; }
        public override IAgentMovementStop MovementStop => MovementHandler;
    }
}
