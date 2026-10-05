using System;
using ProjectRE;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;
using Action = Unity.Behavior.Action;

/// <summary>대상 배후로 접근. 시간 초과 시 실패해 일반 추적으로 대체.</summary>
[Serializable, GeneratePropertyBag]
[NodeDescription(name: "Monster Move Behind Target", story: "[Input] approach rear of [Context] within [Timeout]",
    category: "Action/Monster", id: "b310aa1e5f0e4f5bb5abb1110cab0206")]
public partial class MonsterMoveBehindTargetAction : Action
{
    [SerializeReference] public BlackboardVariable<MonsterBehaviorContext> Context;
    [SerializeReference] public BlackboardVariable<MonsterInput> Input;
    [SerializeReference] public BlackboardVariable<float> Timeout = new(1.5f);
    private Transform _target;
    private float _targetFacing;
    private float _endTime;

    /// <summary>접근 시작 시 대상과 방향 저장. 매 프레임 방향 재선택 제외.</summary>
    protected override Status OnStart()
    {
        if (!Context.Value.HasValidTarget)
            return Status.Failure;

        _target = Context.Value.TargetRoot;
        _targetFacing = Mathf.Sign(_target.localScale.x);
        _endTime = Time.time + Timeout.Value;
        return Status.Running;
    }

    /// <summary>저장 방향 기준 배후 목적지로 이동, 접근 시간 제한.</summary>
    protected override Status OnUpdate()
    {
        var context = Context.Value;
        if (!context.HasValidTarget || context.NeedsReturn || context.TargetRoot != _target)
            return Status.Failure;

        float destinationX = _target.position.x - _targetFacing * context.AttackDistance * 0.8f;
        float deltaX = destinationX - context.transform.position.x;
        if (Mathf.Abs(deltaX) <= context.ArrivalDistance)
            return Status.Success;
        if (Time.time >= _endTime)
            return Status.Failure;

        Input.Value.SetMovement(new Vector2(Mathf.Sign(deltaX), 0f));
        return Status.Running;
    }

    /// <summary>배후 도착·실패·우선순위 중단 시 이동 입력 초기화.</summary>
    protected override void OnEnd() => Input.Value.SetMovement(Vector2.zero);
}
