using System;

public readonly struct TransitionContext
{
    public readonly float DeltaTime; // 이번 Tick에서 경과한 시간
    public readonly float StateElapsedTime; // 현재 상태에서 경과한 시간
    public TransitionContext(float dt, float elapsed)
    {
        DeltaTime = dt;
        StateElapsedTime = elapsed;
    }
}

public interface ITransitionRule
{
    bool ShouldTransition(in TransitionContext context);
}

public sealed class StateTransition
{
    // null이면 모든 상태에서 평가하는 전역 전환.
    public StateType? From { get; }
    public StateType To { get; }
    public int Priority { get; }
    public ITransitionRule Rule { get; }

    // 최종 선택 시 호출. 실패하면 현재 상태를 유지한다.
    public Func<bool> TryCommit { get; }

    public StateTransition(
        StateType? from, // 전이 출발점.
        StateType to, // 전이 목적지.
        int priority, // 전이 우선순위. 높을수록 먼저 평가.
        ITransitionRule rule, // 전이 조건.
        Func<bool> tryCommit = null) // 규칙 평가 후, 참이면 상태 전환을 요청하는 진입점
    {
        From = from;
        To = to;
        Priority = priority;
        Rule = rule;
        TryCommit = tryCommit;
    }
}