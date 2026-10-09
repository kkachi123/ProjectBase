using System;
using ProjectRE;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;
using Action = Unity.Behavior.Action;

/// <summary>대상 상실 후 정지 대기. 재감지 시 취소, 만료 시 복귀.</summary>
[Serializable, GeneratePropertyBag]
[NodeDescription(name: "Monster Wait For Target", story: "[Input] wait [Seconds] for target of [Context]",
    category: "Action/Monster", id: "b310aa1e5f0e4f5bb5abb1110cab0205")]
public partial class MonsterWaitForTargetAction : Action
{
    [SerializeReference] public BlackboardVariable<MonsterBehaviorContext> Context;
    [SerializeReference] public BlackboardVariable<MonsterInput> Input;
    [SerializeReference] public BlackboardVariable<float> Seconds = new(2f);
    private float _endTime;

    /// <summary>정지 입력 전달 및 이번 상실의 대기 종료 시각 저장.</summary>
    protected override Status OnStart()
    {
        Input.Value.SetMovement(Vector2.zero);
        _endTime = Time.time + Seconds.Value;
        return Status.Running;
    }

    /// <summary>재감지 또는 대기 만료로 대기 종료.</summary>
    protected override Status OnUpdate()
    {
        if (Context.Value.HasValidTarget)
            return Status.Success;
        if (Time.time < _endTime)
            return Status.Running;

        Context.Value.ChaseReturn.BeginReturn();
        return Status.Success;
    }

    /// <summary>대기 종료·중단 시 이동 입력 초기화.</summary>
    protected override void OnEnd() => Input.Value.SetMovement(Vector2.zero);
}
