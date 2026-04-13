using System;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;
using Action = Unity.Behavior.Action;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "AIJumpAction", story: "[Input] Jump [Target]", category: "Action", id: "b7b03e72f3028b868b5a3bd7e07b30e1")]
public partial class AIJumpAction : Action
{
    [SerializeReference] public BlackboardVariable<Transform> Self;
    [SerializeReference] public BlackboardVariable<Transform> Target;
    [SerializeReference] public BlackboardVariable<AIPlayerInput> Input;
    private Vector2 dirToGround;

    protected override Status OnStart()
    {
        dirToGround = (Target.Value.position - Self.Value.position).normalized;
        Input.Value.Jump(true);
        return Status.Running;
    }

    protected override Status OnUpdate()
    {
        if (Mathf.Abs(Self.Value.position.x - Target.Value.position.x) < 0.1f) return Status.Success;
        Input.Value.Move(new Vector2(dirToGround.x > 0 ? 1 : -1, 0));
        return Status.Running;
    }

    protected override void OnEnd()
    {
        Input.Value.Move(Vector2.zero);
    }
}

