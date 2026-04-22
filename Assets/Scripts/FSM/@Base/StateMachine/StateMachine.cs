public class StateMachine<T> where T : class , IState
{
    protected T _currentState;
    public T CurrentState => _currentState;

    public void ChangeState(T newState)
    {
        if(_currentState == newState) return;
        _currentState?.Exit();   
        _currentState = newState;
        _currentState.Enter(); 
    }

    public void Operate()
    {
        _currentState?.Execute();
    }
    
}
