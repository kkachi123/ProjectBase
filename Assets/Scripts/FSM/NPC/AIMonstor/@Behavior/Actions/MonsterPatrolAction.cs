using System;
using ProjectRE;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;
using Action = Unity.Behavior.Action;

/// <summary>초기 위치 주변 랜덤 X 목적지로 이동.</summary>
[Serializable, GeneratePropertyBag]
[NodeDescription(name: "Monster Patrol", story: "[Context] 순찰",
    category: "Action/Monster", id: "b310aa1e5f0e4f5bb5abb1110cab0202")]
public partial class MonsterPatrolAction : Action
{
    [SerializeReference] public BlackboardVariable<MonsterBehaviorContext> Context;
    private float _destinationX;

    /// <summary>순찰 목적지 1회 선택.</summary>
    protected override Status OnStart()
    {
        var context = Context.Value;
        float radius = context.Patrol.Radius;
        _destinationX = context.ChaseReturn.HomeX + UnityEngine.Random.Range(-radius, radius);
        return Status.Running;
    }

    /// <summary>도착 전까지 수평 이동 입력 전달.</summary>
    protected override Status OnUpdate()
    {
        var context = Context.Value;
        float deltaX = _destinationX - context.transform.position.x;
        if (Mathf.Abs(deltaX) <= context.ChaseReturn.ArrivalDistance)
            return Status.Success;

        context.Input.SetMovement(new Vector2(Mathf.Sign(deltaX), 0f));
        return Status.Running;
    }

    /// <summary>도착·우선순위 중단 시 이동 입력 초기화.</summary>
    protected override void OnEnd() => Context.Value.Input.SetMovement(Vector2.zero);
}
