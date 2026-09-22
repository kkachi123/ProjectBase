public class IdleToMoveTransition : ITransitionRule
{
    // 현재 상태가 Idle이면 Move로 전환, Move이면 Idle로 전환
    public StateType NextState => _isIdle ? StateType.Move : StateType.Idle;

    private IAgentMovementInput _movementInput;

    private bool _isIdle;

    public IdleToMoveTransition(IAgentMovementInput movementInput , bool isIdle)
    {
        _movementInput = movementInput;
        _isIdle = isIdle;
    }

    public bool ShouldTransition(float deltatime)
    {
        if( _isIdle && _movementInput.GetMovementInput().magnitude > 0)
        {
            return true;
        }
        else if(!_isIdle && _movementInput.GetMovementInput().magnitude == 0)
        {
            return true;
        }
        return false;
    }
}