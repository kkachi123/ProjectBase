using System;
using System.Collections.Generic;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;
using Action = Unity.Behavior.Action;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "PlayerAttackAction", story: "[Input] Attack", category: "Action", id: "cd66dc1e3204e7e18256738d55600b2a")]
public partial class PlayerAttackAction : Action
{
    [SerializeReference] public BlackboardVariable<AIPlayerInput> Input;
    [SerializeReference] public BlackboardVariable<int> AttackTypeCount;
    [SerializeReference] public BlackboardVariable<bool> CanAttack;

    protected override Status OnStart()
    {
        if (Input == null || !CanAttack.Value || AttackTypeCount == 0) return Status.Failure;
        CanAttack.Value = false;
        Input.Value.Attack(UnityEngine.Random.Range(1, AttackTypeCount.Value + 1));
        return Status.Success;
    }
}


