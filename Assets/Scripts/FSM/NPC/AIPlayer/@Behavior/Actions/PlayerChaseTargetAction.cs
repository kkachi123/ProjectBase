using System;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;
using Action = Unity.Behavior.Action;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "PlayerChaseTargetAction"
    , description: "PlayerChaseTargetAction uses [Input] to move towards [Target] for a random duration between [Min] and [Max] seconds. If the target is above, it will jump."
    , story: "[Input] Move [Target] between [Min] ~ [Max].", category: "Action", id: "ffce954764ae1e1e95cc1726070678d7")]
public partial class PlayerChaseTargetAction : Action
{
    [SerializeReference] public BlackboardVariable<AIPlayerInput> Input;
    [SerializeReference] public BlackboardVariable<bool> CanChase;
    [SerializeReference] public BlackboardVariable<Transform> Self;
    [SerializeReference] public BlackboardVariable<Transform> Target;
    [SerializeReference] public BlackboardVariable<float> Min = new BlackboardVariable<float>(1);
    [SerializeReference] public BlackboardVariable<float> Max = new BlackboardVariable<float>(3);

    Vector2 inputDir;
    float duration;
    float elapsedTime = 0f;
    protected override Status OnStart()
    {
        if(Target.Value == null || Self.Value == null || !CanChase.Value) return Status.Failure;
        CanChase.Value = false;

        duration = UnityEngine.Random.Range(Min.Value, Max.Value);
        elapsedTime = 0f;

        Vector2 dirToTarget = (Target.Value.position - Self.Value.position).normalized;
        inputDir = new Vector2(dirToTarget.x > 0 ? 1 : -1, 0);

        if (dirToTarget.y > 0.5f) Input.Value.Jump(true);
        return Status.Running;
    }

    protected override Status OnUpdate()
    {
        if (Target.Value == null || Self.Value == null) return Status.Failure;
        else if ((elapsedTime > duration) || TargetDistanceX(Target.Value.position) < 0.1f) return Status.Success;

        elapsedTime += Time.deltaTime;

        inputDir = CalcInputDir(Target.Value.position);
        Input.Value.Move(inputDir);

        return Status.Running;
    }

    protected override void OnEnd()
    {
        Input.Value.Move(Vector2.zero);
    }

    private float TargetDistanceX(Vector2 target) => Mathf.Abs(Self.Value.position.x - target.x);

    private Vector2 CalcInputDir(Vector2 target)
    {
        Vector2 dirToTarget = (target - (Vector2)Self.Value.position).normalized;
        return new Vector2(dirToTarget.x > 0 ? 1 : -1, 0);
    }
}



