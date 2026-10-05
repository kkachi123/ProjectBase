using System;
using ProjectRE;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;
using Action = Unity.Behavior.Action;

/// <summary>유효 대상을 공격 거리까지 추적.</summary>
[Serializable, GeneratePropertyBag]
[NodeDescription(name: "Monster Chase", story: "[Input] chase target of [Context]",
    category: "Action/Monster", id: "b310aa1e5f0e4f5bb5abb1110cab0203")]
public partial class MonsterChaseAction : Action
{
    [SerializeReference] public BlackboardVariable<MonsterBehaviorContext> Context;
    [SerializeReference] public BlackboardVariable<MonsterInput> Input;

    /// <summary>교전 시작 기록 후 추적 시작.</summary>
    protected override Status OnStart()
    {
        Context.Value.BeginEngagement();
        return Status.Running;
    }

    /// <summary>공격 거리 도착 시 추적 완료.</summary>
    protected override Status OnUpdate()
    {
        var context = Context.Value;
        if (!context.HasValidTarget || context.NeedsReturn)
            return Status.Failure;

        float deltaX = context.TargetRoot.position.x - context.transform.position.x;
        if (context.IsInAttackRange(deltaX))
            return Status.Success;

        Input.Value.SetMovement(new Vector2(Mathf.Sign(deltaX), 0f));
        return Status.Running;
    }

    /// <summary>도착·대상 상실·우선순위 중단 시 이동 입력 초기화.</summary>
    protected override void OnEnd() => Input.Value.SetMovement(Vector2.zero);
}
