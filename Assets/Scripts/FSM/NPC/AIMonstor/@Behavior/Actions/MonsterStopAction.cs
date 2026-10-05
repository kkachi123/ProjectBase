using System;
using ProjectRE;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;
using Action = Unity.Behavior.Action;

/// <summary>이동 입력 중단.</summary>
[Serializable, GeneratePropertyBag]
[NodeDescription(name: "Monster Stop", story: "Stop [Input]", category: "Action/Monster",
    id: "b310aa1e5f0e4f5bb5abb1110cab0201")]
public partial class MonsterStopAction : Action
{
    [SerializeReference] public BlackboardVariable<MonsterInput> Input;

    /// <summary>정지 입력 전달 후 완료.</summary>
    protected override Status OnStart()
    {
        Input.Value.SetMovement(Vector2.zero);
        return Status.Success;
    }
}
