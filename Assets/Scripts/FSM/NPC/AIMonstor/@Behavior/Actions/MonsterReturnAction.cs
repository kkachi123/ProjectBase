using System;
using ProjectRE;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;
using Action = Unity.Behavior.Action;

/// <summary>초기 X 좌표로 복귀. 재인식 시 Graph에서 중단.</summary>
[Serializable, GeneratePropertyBag]
[NodeDescription(name: "Monster Return", story: "[Context] 복귀",
    category: "Action/Monster", id: "b310aa1e5f0e4f5bb5abb1110cab0204")]
public partial class MonsterReturnAction : Action
{
    [SerializeReference] public BlackboardVariable<MonsterBehaviorContext> Context;

    /// <summary>초기 위치 이동 시작.</summary>
    protected override Status OnStart() => Status.Running;

    /// <summary>복귀 도착 범위 확인 및 이동 입력 전달.</summary>
    protected override Status OnUpdate()
    {
        var context = Context.Value;
        float deltaX = context.ChaseReturn.HomeX - context.transform.position.x;
        if (Mathf.Abs(deltaX) <= context.ChaseReturn.ArrivalDistance)
            return Status.Success;

        context.Input.SetMovement(new Vector2(Mathf.Sign(deltaX), 0f));
        return Status.Running;
    }

    /// <summary>모든 종료에서 이동 정지. 정상 도착 시에만 교전 이력 초기화.</summary>
    protected override void OnEnd()
    {
        var context = Context.Value;
        context.Input.SetMovement(Vector2.zero);
        if (CurrentStatus == Status.Success)
            context.ChaseReturn.CompleteReturn();
    }
}
