namespace ProjectRE
{
    using UnityEngine;

    public class GroundedState : AgentStateBase
    {
        private readonly AgentAnimator _animator;
        private readonly AgentMovementHandler2D _movementHandler;
        private readonly IAgentMovementInput _movementInput;

        public GroundedState(
            AgentAnimator animator,
            AgentMovementHandler2D movementHandler,
            IAgentMovementInput movementInput)
        {
            _animator = animator;
            _movementHandler = movementHandler;
            _movementInput = movementInput;
        }

        protected override void OnEnter()
        {
            _animator.SetBool(StateType.Grounded, true);
            UpdateMovement();
        }

        protected override void OnExecute(float deltaTime)
        {
            UpdateMovement();
        }

        public override void Exit()
        {
            _animator.SetBool(StateType.Grounded, false);
            _animator.SetFloat(AnimationFloatType.Speed, 0f);
        }

        private void UpdateMovement()
        {
            Vector2 movement = _movementInput.GetMovementInput();
            _movementHandler.HandleMove(movement);
            _animator.SetFloat(AnimationFloatType.Speed, movement.magnitude);
        }
    }
}
