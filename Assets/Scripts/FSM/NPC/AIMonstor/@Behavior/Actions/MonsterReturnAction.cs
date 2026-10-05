using System;
using ProjectRE;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;
using Action = Unity.Behavior.Action;

/// <summary>초기 X 좌표로 복귀. 도착 전 재추적 제외.</summary>
[Serializable, GeneratePropertyBag]
[NodeDescription(name: "Monster Return", story: "[Input] return home of [Context]",
    category: "Action/Monster", id: "b310aa1e5f0e4f5bb5abb1110cab0204")]
public partial class MonsterReturnAction : Action
{
    [SerializeReference] public BlackboardVariable<MonsterBehaviorContext> Context;
    [SerializeReference] public BlackboardVariable<MonsterInput> Input;

    /// <summary>복귀 진행 기록 후 초기 위치 이동 시작.</summary>
    protected override Status OnStart()
    {
        Context.Value.BeginReturn();
        return Status.Running;
    }

    /// <summary>복귀 도착 범위 확인 및 이동 입력 전달.</summary>
    protected override Status OnUpdate()
    {
        var context = Context.Value;
        float deltaX = context.HomeX - context.transform.position.x;
        if (Mathf.Abs(deltaX) <= context.ArrivalDistance + MonsterBehaviorContext.DistanceTolerance)
        {
            context.CompleteReturn();
            return Status.Success;
        }

        Input.Value.SetMovement(new Vector2(Mathf.Sign(deltaX), 0f));
        return Status.Running;
    }

    /// <summary>복귀 완료·사망 중단 시 이동 입력 초기화.</summary>
    protected override void OnEnd() => Input.Value.SetMovement(Vector2.zero);
}
