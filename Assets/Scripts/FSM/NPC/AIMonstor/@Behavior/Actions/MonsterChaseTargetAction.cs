using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "MonsterChaseTargetAction", story: "[Input] Move [Target] in [LimitDistance]", category: "Action", id: "f38f827caf0d0ae2824cb916daa218e8")]
public partial class MonsterChaseTargetAction : Action
{
    [SerializeReference] public BlackboardVariable<AIMonsterInput> Input;
    [SerializeReference] public BlackboardVariable<Transform> Self;
    [SerializeReference] public BlackboardVariable<Transform> Target;
    [SerializeReference] public BlackboardVariable<float> LimitDistance;

    [SerializeReference] public BlackboardVariable<bool> IsLeftGrounded;
    [SerializeReference] public BlackboardVariable<bool> IsRightGrounded;

    int direction;
    Vector2 inputDir;

    bool IsFrontGrounded => IsLeftGrounded.Value && direction == -1 || IsRightGrounded.Value && direction == 1;
    protected override Status OnStart()
    {
        if (Target.Value == null || Self.Value == null) return Status.Failure;

        Vector2 dirToTarget = (Target.Value.position - Self.Value.position).normalized;
        direction = dirToTarget.x > 0 ? 1 : -1;
        inputDir = new Vector2(direction, 0);

        return Status.Running;
    }

    protected override Status OnUpdate()
    {
        if (Target.Value == null || Self.Value == null) return Status.Failure;
        else if (TargetDistanceX(Target.Value.position) < LimitDistance.Value) return Status.Success;

        if(IsFrontGrounded) Input.Value.Move(Vector2.zero);
        else
        {
            inputDir = CalcInputDir(Target.Value.position);
            Input.Value.Move(inputDir);
        }

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
        direction = dirToTarget.x > 0 ? 1 : -1;
        return new Vector2(direction, 0);
    }
}

