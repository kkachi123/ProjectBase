using System;
using System.Collections.Generic;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;
using Action = Unity.Behavior.Action;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "AIAttackAction", story: "[Input] Attack", category: "Action", id: "cd66dc1e3204e7e18256738d55600b2a")]
public partial class AIAttackAction : Action
{
    [SerializeReference] public BlackboardVariable<AIPlayerInput> Input;
    [SerializeReference] public BlackboardVariable<float> TargetDistance;
    [SerializeReference] public BlackboardVariable<List<float>> AttackRanges;
    [SerializeReference] public BlackboardVariable<int> AttackType;

    protected override Status OnStart()
    {
        if (Input == null) return Status.Success;
        int attackType = AttackType.Value;
        // 범위 밖이면 견제 공격
        if (AttackType == 0) attackType = UnityEngine.Random.Range(1, AttackRanges.Value.Count); 

        Input.Value.Attack(attackType);

        return Status.Success;
    }

    protected override void OnEnd()
    {
        //if(UnityEngine.Random.Range(0, 2) != 0) Input.Value.Attack(1);
        Input.Value.Attack(1);
    }
}

