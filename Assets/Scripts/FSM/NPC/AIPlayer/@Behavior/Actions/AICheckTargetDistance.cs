using System;
using System.Collections.Generic;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;
using Action = Unity.Behavior.Action;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "AICheckTargetDistance", story: "Update [Self] to [Target] [TargetDistance]", category: "Action", id: "a52acac53effe945e9092b35ab024080")]
public partial class AICheckTargetDistance : Action
{
    [SerializeReference] public BlackboardVariable<Transform> Self;
    [SerializeReference] public BlackboardVariable<Transform> Target;
    [SerializeReference] public BlackboardVariable<float> TargetDistance;

    protected override Status OnUpdate()
    {
        if (Self.Value == null || Target.Value == null || TargetDistance == null)
        {
            return Status.Failure;
        }
        TargetDistance.Value = Vector2.Distance(Self.Value.position, Target.Value.position);
        
        return Status.Success;
    }
}

