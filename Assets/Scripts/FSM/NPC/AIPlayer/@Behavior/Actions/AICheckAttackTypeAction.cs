using System;
using System.Collections.Generic;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;
using Action = Unity.Behavior.Action;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "AICheckAttackType", story: "Update [AttackType] to Check [TargetDistance]", category: "Action", id: "be17d0558cd47f6d4a67332e444fca35")]
public partial class AICheckAttackTypeAction : Action
{
    [SerializeReference] public BlackboardVariable<float> TargetDistance;
    [SerializeReference] public BlackboardVariable<int> AttackType;
    [SerializeReference] public BlackboardVariable<List<float>> AttackRanges;
    
    protected override Status OnStart()
    {
        if(TargetDistance == null || AttackType == null || AttackRanges == null) return Status.Failure;
        AttackType.Value = 0;
        for (int i = 0; i < AttackRanges.Value.Count - 1; i++)
        {
            if (TargetDistance.Value < AttackRanges.Value[i])
            {
                AttackType.Value = i + 1;
                break;
            }
        }

        return Status.Success;
    }
}

