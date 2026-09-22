public class JumpFallTransition : ITransitionRule
{
    public StateType NextState => _groundDetector.IsGrounded ? StateType.Idle : StateType.Fall;

    private float _jumpTimer = 0f;
    private const float MIN_JUMP_TIME = 0.1f;
    private GroundDetector _groundDetector;

    public JumpFallTransition(GroundDetector groundDetector)
    {
        _groundDetector = groundDetector;
    }

    public bool ShouldTransition(float deltatime)
    {
        _jumpTimer += deltatime;
        if (_jumpTimer >= MIN_JUMP_TIME)
        {
            _jumpTimer = 0f;
            return true;
        }
        return false;
    }
}
