using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "AIAttackAction", story: "[Input] Attack [AttackType]", category: "Action", id: "cd66dc1e3204e7e18256738d55600b2a")]
public partial class AIAttackAction : Action
{
    [SerializeReference] public BlackboardVariable<AIPlayerInput> Input;
    [SerializeReference] public BlackboardVariable<int> AttackType;

    protected override Status OnStart()
    {
        if (Input == null) return Status.Success;

        Input.Value.Attack(AttackType.Value);

        return Status.Success;
    }

    protected override void OnEnd()
    {

    }
}

