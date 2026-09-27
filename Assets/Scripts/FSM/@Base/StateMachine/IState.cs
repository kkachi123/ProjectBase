namespace ProjectRE
{
    public interface IState
    {
        public void Enter();
        public void Execute();
        public void Exit();
    }


    public enum StateType
    {
        // Common States
        Attack,
        Hit,
        Death,
        // Grounded States
        Grounded,
        Jump,
        Fall,
    }
}
