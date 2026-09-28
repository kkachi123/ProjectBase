namespace ProjectRE
{
    using UnityEngine;

    public class GroundedState : AgentStateBase
    {
        private readonly IGroundedAnimation _animator;
        private readonly AgentMovementHandler2D _movementHandler;
        private readonly IAgentMovementInput _movementInput;

        public GroundedState(
            IGroundedAnimation animator,
            AgentMovementHandler2D movementHandler,
            IAgentMovementInput movementInput)
        {
            _animator = animator;
            _movementHandler = movementHandler;
            _movementInput = movementInput;
        }

        protected override void OnEnter()
        {
            _animator.SetGrounded(true);
            UpdateMovement();
        }

        protected override void OnExecute(float deltaTime)
        {
            UpdateMovement();
        }

        protected override void OnExit()
        {
            _animator.SetGrounded(false);
            _animator.SetMoveSpeed(0f);
        }

        private void UpdateMovement()
        {
            Vector2 movement = _movementInput.GetMovementInput();
            _movementHandler.HandleMove(movement);
            _animator.SetMoveSpeed(movement.magnitude);
        }
    }
}
