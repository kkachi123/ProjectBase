using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "AIDodgeTargetAction", story: "[Input] Dodge from [target] [DodgeDistance]", category: "Action", id: "ff069399fb0272e3600d1bf97b5629db")]
public partial class AIDodgeTargetAction : Action
{
    [SerializeReference] public BlackboardVariable<AIPlayerInput> Input;
    [SerializeReference] public BlackboardVariable<Transform> Self;
    [SerializeReference] public BlackboardVariable<Transform> Target;
    [SerializeReference] public BlackboardVariable<float> DodgeDistance;
    [SerializeReference] public BlackboardVariable<WallDetector> WallDetector;

    private Vector2 _targetPos;
    private Vector2 inputDir;
    protected override Status OnStart()
    {
        if (Target.Value == null || Self.Value == null) return Status.Failure;
        Vector2 dirToTarget = (Target.Value.position - Self.Value.position).normalized;
        inputDir = new Vector2(dirToTarget.x > 0 ? -1 : 1, 0); // 타겟의 반대 방향으로 회피

        _targetPos = (Vector2)Self.Value.position + (inputDir * DodgeDistance.Value); // 회피 목표 위치 계산

        return Status.Running;
    }

    protected override Status OnUpdate()
    {
        if(WallDetector.Value.IsWallInFront() || Mathf.Abs(Self.Value.position.x - _targetPos.x) < 0.1f) return Status.Success;

        Input.Value.Move(inputDir);

        return Status.Running;
    }

    protected override void OnEnd()
    {
        Input.Value.Move(Vector2.zero);
    }
}

