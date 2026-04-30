using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "PlayerTurnAction", story: "Check [Self] [Target] Direction And Turn", category: "Action", id: "dc7ccefa1c25b74b04cb4b6bdd34ebcf")]
public partial class PlayerTurnAction : Action
{
    [SerializeReference] public BlackboardVariable<AIPlayerInput> Input;
    [SerializeReference] public BlackboardVariable<Transform> Self;
    [SerializeReference] public BlackboardVariable<Transform> Target;
    float elapsedTime = 0f;
    protected override Status OnStart()
    {
        if (Input == null || Self?.Value == null || Target?.Value == null) return Status.Failure;
        return Status.Running;
    }

    protected override Status OnUpdate()
    {
        if (elapsedTime >= 0.5f) return Status.Success;
        int dir = Target.Value.position.x > Self.Value.position.x ? 1 : -1;
        Input.Value.Move(new Vector2(dir, 0));
        elapsedTime += Time.deltaTime;
        return Status.Running;
    }

}

