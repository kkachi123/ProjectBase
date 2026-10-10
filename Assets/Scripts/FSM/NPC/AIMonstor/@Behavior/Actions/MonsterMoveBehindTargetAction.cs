using System;
using ProjectRE;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;
using Action = Unity.Behavior.Action;

/// <summary>대상 배후로 접근. 시간 초과 시 실패해 일반 추적으로 대체.</summary>
[Serializable, GeneratePropertyBag]
[NodeDescription(name: "Monster Move Behind Target", story: "[Context] 배후 접근",
    category: "Action/Monster", id: "b310aa1e5f0e4f5bb5abb1110cab0206")]
public partial class MonsterMoveBehindTargetAction : Action
{
    [SerializeReference] public BlackboardVariable<MonsterBehaviorContext> Context;
    private float _targetFacing;
    private float _endTime;

    /// <summary>접근 시작 시 대상 방향·종료 시각 저장.</summary>
    protected override Status OnStart()
    {
        var context = Context.Value;
        _targetFacing = Mathf.Sign(context.Target.Root.localScale.x);
        _endTime = Time.time + context.Attack.RearApproachTimeout;
        return Status.Running;
    }

    /// <summary>저장 방향 기준 배후 목적지로 이동, 접근 시간 제한.</summary>
    protected override Status OnUpdate()
    {
        var context = Context.Value;
        float deltaX = context.Target.DeltaX - _targetFacing * context.Attack.AttackDistance * 0.8f;
        if (Mathf.Abs(deltaX) <= context.ChaseReturn.ArrivalDistance)
            return Status.Success;
        if (Time.time >= _endTime)
            return Status.Failure;

        context.Input.SetMovement(new Vector2(Mathf.Sign(deltaX), 0f));
        return Status.Running;
    }

    /// <summary>배후 도착·실패·우선순위 중단 시 이동 입력 초기화.</summary>
    protected override void OnEnd() => Context.Value.Input.SetMovement(Vector2.zero);
}
