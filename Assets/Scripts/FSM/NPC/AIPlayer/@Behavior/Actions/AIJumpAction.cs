using System;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;
using Action = Unity.Behavior.Action;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "AIJumpAction", story: "[Input] Jump [JumpFloor] [IsGround]", category: "Action", id: "b7b03e72f3028b868b5a3bd7e07b30e1")]
public partial class AIJumpAction : Action
{
    [SerializeReference] public BlackboardVariable<Transform> Self;
    [SerializeReference] public BlackboardVariable<Transform> JumpFloor;
    [SerializeReference] public BlackboardVariable<AIPlayerInput> Input;
    [SerializeReference] public BlackboardVariable<bool> IsGround;
    private Vector2 dirToGround;
    private Transform jumpfloor;

    protected override Status OnStart()
    {
        if(JumpFloor.Value == null || !IsGround.Value) return Status.Failure;
        jumpfloor = JumpFloor.Value;
        dirToGround = (jumpfloor.position - Self.Value.position).normalized;

        Input.Value.Move(dirToGround.x > 0 ? Vector2.right : Vector2.left); 
        Input.Value.Jump(true);
        return Status.Running;
    }

    protected override Status OnUpdate()
    {
        if (Mathf.Abs(Self.Value.position.x - jumpfloor.position.x) < 0.1f) return Status.Success;
        Input.Value.Move(new Vector2(dirToGround.x > 0 ? 1 : -1, 0));
        return Status.Running;
    }

    protected override void OnEnd()
    {
        Input.Value.Move(Vector2.zero);
    }
}

