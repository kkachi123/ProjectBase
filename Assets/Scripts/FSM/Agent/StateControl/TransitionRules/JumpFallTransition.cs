public class JumpFallTransition : ITransitionRule
{
    public StateType NextState => StateType.Fall;

    private float _jumpTimer = 0f;
    private const float MIN_JUMP_TIME = 0.1f;

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
