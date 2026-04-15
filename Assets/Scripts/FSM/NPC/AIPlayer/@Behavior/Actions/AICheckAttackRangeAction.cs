using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;
using System.Collections.Generic;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "AICheckAttackRange", story: "Update [Self] to [Target] [TargetDistance]", category: "Action", id: "a52acac53effe945e9092b35ab024080")]
public partial class AICheckAttackRangeAction : Action
{
    [SerializeReference] public BlackboardVariable<Transform> Self;
    [SerializeReference] public BlackboardVariable<Transform> Target;
    [SerializeReference] public BlackboardVariable<float> TargetDistance;
    [SerializeReference] public BlackboardVariable<List<float>> AttackRanges;
    [SerializeReference] public BlackboardVariable<int> AttackType;

    protected override Status OnUpdate()
    {
        if (Self.Value == null || Target.Value == null || TargetDistance == null || AttackRanges == null)
        {
            AttackType.Value = 0; // No attack range
            return Status.Failure;
        }

        TargetDistance.Value = Vector2.Distance(Self.Value.position, Target.Value.position);
        for(int i = 0; i < AttackRanges.Value.Count; i++)
        {
            if (TargetDistance.Value <= AttackRanges.Value[i])
            {
                AttackType.Value = i + 1;
                return Status.Success;
            }
        }

        AttackType.Value = 0; 

        return Status.Success;
    }
}

