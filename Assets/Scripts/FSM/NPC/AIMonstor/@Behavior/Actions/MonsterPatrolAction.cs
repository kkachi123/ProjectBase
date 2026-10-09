using System;
using ProjectRE;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;
using Action = Unity.Behavior.Action;

/// <summary>초기 위치 주변 랜덤 X 목적지로 이동.</summary>
[Serializable, GeneratePropertyBag]
[NodeDescription(name: "Monster Patrol", story: "[Input] patrol around [Context] within [Radius]",
    category: "Action/Monster", id: "b310aa1e5f0e4f5bb5abb1110cab0202")]
public partial class MonsterPatrolAction : Action
{
    [SerializeReference] public BlackboardVariable<MonsterBehaviorContext> Context;
    [SerializeReference] public BlackboardVariable<MonsterInput> Input;
    [SerializeReference] public BlackboardVariable<float> Radius = new(2f);
    private float _destinationX;

    /// <summary>순찰 목적지 1회 선택.</summary>
    protected override Status OnStart()
    {
        _destinationX = Context.Value.ChaseReturn.HomeX + UnityEngine.Random.Range(-Radius.Value, Radius.Value);
        return Status.Running;
    }

    /// <summary>도착 전까지 수평 이동 입력 전달.</summary>
    protected override Status OnUpdate()
    {
        float deltaX = _destinationX - Context.Value.transform.position.x;
        if (Mathf.Abs(deltaX) <= Context.Value.ChaseReturn.ArrivalDistance)
            return Status.Success;

        Input.Value.SetMovement(new Vector2(Mathf.Sign(deltaX), 0f));
        return Status.Running;
    }

    /// <summary>도착·우선순위 중단 시 이동 입력 초기화.</summary>
    protected override void OnEnd() => Input.Value.SetMovement(Vector2.zero);
}
