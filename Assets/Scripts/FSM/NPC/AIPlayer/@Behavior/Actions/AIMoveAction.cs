using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "AIMoveAction", story: "[Input] Move [Duration] Time", category: "Action", id: "882cdd58bb7f90ddbfb2fbfb44fc63a6")]
public partial class AIMoveAction : Action
{
    [SerializeReference] public BlackboardVariable<AIPlayerInput> Input;
    [SerializeReference] public BlackboardVariable<float> Duration;
    Vector2 dir;
    float elapsedTime = 0f;
    protected override Status OnStart()
    {
        dir = UnityEngine.Random.Range(0,2) == 0 ? Vector2.left : Vector2.right;
        elapsedTime = 0f;
        return Status.Running;
    }

    protected override Status OnUpdate()
    {
        if(elapsedTime > Duration.Value) return Status.Success;

        elapsedTime += Time.deltaTime;
        Input.Value.Move(dir);

        return Status.Running;
    }

    protected override void OnEnd()
    {
        Input.Value.Move(Vector2.zero);
    }
}

