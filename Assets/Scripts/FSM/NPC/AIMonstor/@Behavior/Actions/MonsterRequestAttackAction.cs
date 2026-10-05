using System;
using ProjectRE;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;
using Action = Unity.Behavior.Action;

/// <summary>기존 이동 입력으로 방향 정렬 후 단발 공격 요청.</summary>
[Serializable, GeneratePropertyBag]
[NodeDescription(name: "Monster Request Attack", story: "[Input] attack target of [Context] every [Interval]",
    category: "Action/Monster", id: "b310aa1e5f0e4f5bb5abb1110cab0207")]
public partial class MonsterRequestAttackAction : Action
{
    [SerializeReference] public BlackboardVariable<MonsterBehaviorContext> Context;
    [SerializeReference] public BlackboardVariable<MonsterInput> Input;
    [SerializeReference] public BlackboardVariable<float> Interval = new(1f);
    [SerializeReference] public BlackboardVariable<float> NextRequestTime;

    /// <summary>방향 정렬과 요청 가능 시각 확인 시작.</summary>
    protected override Status OnStart() => Status.Running;

    /// <summary>대상·거리·방향·요청 간격 확인 후 공격 입력 발행.</summary>
    protected override Status OnUpdate()
    {
        var context = Context.Value;
        if (!context.HasValidTarget || context.NeedsReturn)
            return Status.Failure;

        float deltaX = context.TargetRoot.position.x - context.transform.position.x;
        if (Mathf.Abs(deltaX) > context.AttackDistance + MonsterBehaviorContext.DistanceTolerance)
            return Status.Failure;

        if (Mathf.Abs(deltaX) > MonsterBehaviorContext.DistanceTolerance
            && Mathf.Sign(deltaX) != Mathf.Sign(context.transform.localScale.x))
        {
            Input.Value.SetMovement(new Vector2(Mathf.Sign(deltaX), 0f));
            return Status.Running;
        }

        Input.Value.SetMovement(Vector2.zero);
        if (Time.time < NextRequestTime.Value)
            return Status.Running;

        NextRequestTime.Value = Time.time + Interval.Value;
        Input.Value.RequestAttack();
        return Status.Success;
    }

    /// <summary>공격 요청·대상 상실·우선순위 중단 시 방향 입력 초기화.</summary>
    protected override void OnEnd() => Input.Value.SetMovement(Vector2.zero);
}
