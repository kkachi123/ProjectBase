using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "MonsterTurnAction", story: "Check [Self] [Target] Direction And Turn", category: "Action", id: "3ee5fd9bbbdf80873e5c976e7160c4b3")]
public partial class MonsterTurnAction : Action
{
    [SerializeReference] public BlackboardVariable<AIMonsterInput> Input;
    [SerializeReference] public BlackboardVariable<Transform> Self;
    [SerializeReference] public BlackboardVariable<Transform> Target;

    float elapsedTime = 0f;
    protected override Status OnStart()
    {
        if (Input == null || Self?.Value == null || Target?.Value == null) return Status.Failure;
        elapsedTime = 0f;
        return Status.Running;
    }

    protected override Status OnUpdate()
    {
        if (elapsedTime >= 0.1f) return Status.Success;
        int dir = Target.Value.position.x > Self.Value.position.x ? 1 : -1;
        Input.Value.Move(new Vector2(dir, 0));
        elapsedTime += Time.deltaTime;
        return Status.Running;
    }
}

