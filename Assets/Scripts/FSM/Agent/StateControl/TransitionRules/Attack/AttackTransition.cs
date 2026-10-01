namespace ProjectRE
{
    using System;

    /// <summary>
    /// 공격 요청을 수신하고 실제 공격 시작이 가능한 경우 AttackState로 진입하는 공용 전이다.
    /// </summary>
    public class AttackTransition : IEventTransitionRule
    {
        public Type NextStateType => typeof(AttackState);

        protected readonly IAgentCombatInput CombatInput;
        private readonly IAttackStarter _attackStarter;
        private bool _isSubscribed;
        private bool _shouldTransition;

        public AttackTransition(
            IAgentCombatInput combatInput,
            IAttackStarter attackStarter)
        {
            CombatInput = combatInput;
            _attackStarter = attackStarter;
        }

        public virtual bool ShouldTransition(float deltaTime)
        {
            if (!_shouldTransition)
                return false;
            return _attackStarter.TryStartAttack();
        }

        public void Subscribe()
        {
            if (_isSubscribed)
                return;

            CombatInput.OnAttackRequested += TriggerTransition;
            _isSubscribed = true;
        }

        public void Unsubscribe()
        {
            if (_isSubscribed)
            {
                CombatInput.OnAttackRequested -= TriggerTransition;
                _isSubscribed = false;
            }

            _shouldTransition = false;
        }

        private void TriggerTransition()
        {
            _shouldTransition = true;
        }
    }
}
