using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "AIMoveAction", story: "[Input] Move between [Min] and [Max] Seconds", category: "Action", id: "882cdd58bb7f90ddbfb2fbfb44fc63a6")]
public partial class AIMoveAction : Action
{
    [SerializeReference] public BlackboardVariable<AIPlayerInput> Input;
    [SerializeReference] public BlackboardVariable<float> Min = new BlackboardVariable<float>(1);
    [SerializeReference] public BlackboardVariable<float> Max = new BlackboardVariable<float>(3);
    Vector2 dir;
    float duration;
    float elapsedTime = 0f;
    protected override Status OnStart()
    {
        dir = UnityEngine.Random.Range(0,2) == 0 ? Vector2.left : Vector2.right;
        duration = UnityEngine.Random.Range(Min.Value, Max.Value);
        elapsedTime = 0f;

        return Status.Running;
    }

    protected override Status OnUpdate()
    {
        if(elapsedTime > duration) return Status.Success;

        elapsedTime += Time.deltaTime;
        Input.Value.Move(dir);

        return Status.Running;
    }

    protected override void OnEnd()
    {
        Input.Value.Move(Vector2.zero);
    }
}

