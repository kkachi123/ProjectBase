using System;
using System.Collections.Generic;

public abstract class AgentStateBase
{
    public List<ITransitionRule> _transitionRules = new();
    public Action<StateType> OnTransition;

    public void Enter()
    {
        foreach (var rule in _transitionRules)
        {
            if (rule is IEventTransitionRule eventRule)
            {
                eventRule.Subscribe();
            }
        }
        OnEnter();
    }
    // Enter로 IEventTransitionRule 구독 및 상태 진입 시 초기화 로직 수행
    // 상속된 클래스에서 OnEnter를 구현하여 상태 진입 시 초기화 로직을 수행
    protected abstract void OnEnter();
    public void Execute(float deltaTime)
    {
        if (ShouldTransition(deltaTime))
            return;
        OnExecute(deltaTime);
    }
    // Execute로 전환조건(ShouldTransition) 체크
    // 상속된 클래스에서 OnExecute를 구현하여 상태별 로직을 수행
    protected abstract void OnExecute(float deltaTime);
    public abstract void Exit();

    // Exit로 IEventTransitionRule 구독 해제 및 상태 종료 시 정리 로직 수행
    private bool ShouldTransition(float deltaTime)
    {
        foreach (var rule in _transitionRules)
        {
            if (rule.ShouldTransition(deltaTime))
            {
                if(rule is IEventTransitionRule eventRule)
                {
                    eventRule.Unsubscribe();
                }
                OnTransition?.Invoke(rule.NextState);
                return true;
            }
        }
        return false;
    }

    public void AddTransition(ITransitionRule rule)
    {
        _transitionRules.Add(rule);
        if (rule is IEventTransitionRule eventRule)
        {
            eventRule.Subscribe();
        }
    }
}