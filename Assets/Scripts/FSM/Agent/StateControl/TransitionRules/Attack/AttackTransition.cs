namespace ProjectRE
{
    using System;

    /// <summary>
    /// 공격 요청 수신 및 시작 조건 확인 후 AttackState 진입.
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
