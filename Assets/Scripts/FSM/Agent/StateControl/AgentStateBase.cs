namespace ProjectRE
{
    using System;
    using System.Collections.Generic;

    public abstract class AgentStateBase
    {
        public List<ITransitionRule> _transitionRules = new();
        public Action<Type> OnTransition;

        public void Enter()
        {
            foreach (ITransitionRule rule in _transitionRules)
            {
                if (rule is IEventTransitionRule eventRule)
                    eventRule.Subscribe();
            }

            OnEnter();
        }

        protected abstract void OnEnter();

        public void Execute(float deltaTime)
        {
            if (ShouldTransition(deltaTime))
                return;

            OnExecute(deltaTime);
        }

        protected abstract void OnExecute(float deltaTime);

        public void Exit()
        {
            foreach (ITransitionRule rule in _transitionRules)
            {
                if (rule is IEventTransitionRule eventRule)
                    eventRule.Unsubscribe();
            }
            OnExit();
        }

        protected virtual void OnExit() { }

        private bool ShouldTransition(float deltaTime)
        {
            foreach (ITransitionRule rule in _transitionRules)
            {
                if (!rule.ShouldTransition(deltaTime))
                    continue;

                OnTransition?.Invoke(rule.NextStateType);
                return true;
            }

            return false;
        }

        public void AddTransition(ITransitionRule rule)
        {
            _transitionRules.Add(rule);
        }
    }
}
