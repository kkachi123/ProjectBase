using System;
using System.Collections.Generic;
public class AgentStateMachine<T> : StateMachine<T> where T : class, IAgentState
{
    public void FixedOperate()
    {
        _currentState?.FixedExecute();
    }
}

public sealed class AgentStateMachine
{
    private readonly Dictionary<StateType, IAgentState> _states;
    private readonly List<StateTransition> _transitions = new();
    private readonly AgentSignals _signals;

    public IAgentState CurrentState { get; private set; }
    public StateType CurrentType { get; private set; }
    public float StateElapsedTime { get; private set; }
    public int StateVersion { get; private set; }

    public AgentStateMachine(
        Dictionary<StateType, IAgentState> states,
        AgentSignals signals)
    {
        _states = states;
        _signals = signals;
    }

    public void AddTransition(StateTransition transition)
    {
        if (!_states.ContainsKey(transition.To))
            throw new ArgumentException("등록되지 않은 목적 상태입니다.");

        // 같은 우선순위는 등록 순서 유지.
        int index = _transitions.FindIndex(
            existing => existing.Priority < transition.Priority);

        if (index < 0)
            _transitions.Add(transition);
        else
            _transitions.Insert(index, transition);
    }

    public void Initialize(StateType initialState)
    {
        if (CurrentState != null)
            throw new InvalidOperationException("이미 초기화된 FSM입니다.");

        EnterState(initialState);
    }

    public void Tick(float deltaTime)
    {
        if (CurrentState == null)
            return;

        StateElapsedTime += deltaTime;

        var context = new TransitionContext(deltaTime, StateElapsedTime);

        if (CurrentType != StateType.Death)
        {
            foreach (var transition in _transitions)
            {
                if (transition.From.HasValue &&
                    transition.From.Value != CurrentType)
                    continue;

                if (transition.To == CurrentType)
                    continue;

                if (!transition.Rule.ShouldTransition(in context))
                    continue;

                if (transition.TryCommit != null &&
                    !transition.TryCommit())
                    continue;

                EnterState(transition.To);
                break;
            }
        }

        // 상태의 Execute에서 새로 발생하는 요청은 다음 평가까지 유지.
        _signals.ClearRequests();
        CurrentState.Execute();
    }

    public void FixedTick()
    {
        CurrentState?.FixedExecute();
    }

    private void EnterState(StateType next)
    {
        CurrentState?.Exit();

        CurrentType = next;
        CurrentState = _states[next];
        StateElapsedTime = 0f;
        StateVersion++;

        _signals.BeginState(StateVersion);
        CurrentState.Enter();
    }
}