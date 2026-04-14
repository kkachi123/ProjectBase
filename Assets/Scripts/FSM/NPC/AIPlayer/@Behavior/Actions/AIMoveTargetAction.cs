using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "AIMoveTargetAction"
    , description: "Chase or Dodge to Target. if Wall in front or Arrived, Success"
    , story: "[Input] Move to [Target] [Approach]", category: "Action", id: "ffce954764ae1e1e95cc1726070678d7")]
public partial class AIMoveTargetAction : Action
{
    [SerializeReference] public BlackboardVariable<Transform> Self;
    [SerializeReference] public BlackboardVariable<bool> IsWallInfront;
    [SerializeReference] public BlackboardVariable<AIPlayerInput> Input;
    [SerializeReference] public BlackboardVariable<Transform> Target;
    [SerializeReference] public BlackboardVariable<bool> Approach;

    private Transform _targetTransform;
    private Vector2 dirToTarget;
    protected override Status OnStart()
    {
        _targetTransform = Target.Value;
        dirToTarget = (_targetTransform.position - Self.Value.position).normalized;
        // Approach false면 target 반대 방향으로 2f 떨어진 지점으로 이동 (회피 행동)
        if (!Approach.Value) 
            _targetTransform.position = (Vector2)Self.Value.position + new Vector2(-dirToTarget.x, 0) * 2f; 

        return Status.Running;
    }

    protected override Status OnUpdate()
    {
        // 타겟에 도달하거나 벽이 앞에 있으면 성공
        if (Mathf.Abs(Self.Value.position.x - _targetTransform.position.x) < 0.1f
            || IsWallInfront.Value) return Status.Success; 

        Input.Value.Move(new Vector2(dirToTarget.x > 0 ? 1 : -1, 0));

        return Status.Running;
    }

    protected override void OnEnd()
    {
        Input.Value.Move(Vector2.zero);
    }
}

