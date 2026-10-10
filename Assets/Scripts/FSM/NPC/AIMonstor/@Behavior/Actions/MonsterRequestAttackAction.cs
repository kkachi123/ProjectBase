using System;
using ProjectRE;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;
using Action = Unity.Behavior.Action;

/// <summary>기존 이동 입력으로 방향 정렬 후 단발 공격 요청.</summary>
[Serializable, GeneratePropertyBag]
[NodeDescription(name: "Monster Request Attack", story: "[Context] 공격 요청",
    category: "Action/Monster", id: "b310aa1e5f0e4f5bb5abb1110cab0207")]
public partial class MonsterRequestAttackAction : Action
{
    private const float FacingDeadZone = 0.01f;

    [SerializeReference] public BlackboardVariable<MonsterBehaviorContext> Context;

    /// <summary>방향 정렬과 요청 가능 시각 확인 시작.</summary>
    protected override Status OnStart() => Status.Running;

    /// <summary>방향 정렬·요청 간격 확인 후 공격 입력 발행. 거리 재검사 제외.</summary>
    protected override Status OnUpdate()
    {
        var context = Context.Value;
        float deltaX = context.Target.DeltaX;

        if (Mathf.Abs(deltaX) > FacingDeadZone
            && Mathf.Sign(deltaX) != Mathf.Sign(context.transform.localScale.x))
        {
            context.Input.SetMovement(new Vector2(Mathf.Sign(deltaX), 0f));
            return Status.Running;
        }

        context.Input.SetMovement(Vector2.zero);
        if (!context.Attack.CanRequestAttack())
            return Status.Running;

        context.Attack.RecordAttackRequest();
        context.Input.RequestAttack();
        return Status.Success;
    }

    /// <summary>공격 요청·대상 상실·우선순위 중단 시 방향 입력 초기화.</summary>
    protected override void OnEnd() => Context.Value.Input.SetMovement(Vector2.zero);
}
