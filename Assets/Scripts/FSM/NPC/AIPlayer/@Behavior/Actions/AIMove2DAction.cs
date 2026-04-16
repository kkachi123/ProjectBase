using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "AIMove2DAction"
    , description: "AIMoveAction use [Input] to move RandomDir for a random duration between [Min] and [Max] seconds. If [WallDetector] detects a wall in front, it will reverse the horizontal direction."
    , story: "[Input] RandomMove between [Min] ~ [Max].", category: "Action", id: "882cdd58bb7f90ddbfb2fbfb44fc63a6")]
public partial class AIMove2DAction : Action
{
    [SerializeReference] public BlackboardVariable<AIPlayerInput> Input;
    [SerializeReference] public BlackboardVariable<float> Min = new BlackboardVariable<float>(1);
    [SerializeReference] public BlackboardVariable<float> Max = new BlackboardVariable<float>(3);
    [SerializeReference] public BlackboardVariable<WallDetector> WallDetector;
    Vector2 dir;
    float duration;
    float elapsedTime = 0f;
    protected override Status OnStart()
    {
        dir = UnityEngine.Random.value > 0.5f ? Vector2.right : Vector2.left;

        duration = UnityEngine.Random.Range(Min.Value, Max.Value);
        elapsedTime = 0f;

        return Status.Running;
    }

    protected override Status OnUpdate()
    {
        if(elapsedTime > duration) return Status.Success;

        elapsedTime += Time.deltaTime;

        if(WallDetector.Value.IsWallInFront()) dir = new Vector2(-dir.x, dir.y);

        Input.Value.Move(dir);

        return Status.Running;
    }

    protected override void OnEnd()
    {
        Input.Value.Move(Vector2.zero);
    }
}

