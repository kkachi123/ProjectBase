using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;
using System.Collections.Generic;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "MonsterAttackAction", story: "[Input] Attack", category: "Action", id: "3fb2fb1376c02e4e25525ef90fd6a62f")]
public partial class MonsterAttackAction : Action
{
    [SerializeReference] public BlackboardVariable<AIMonsterInput> Input;
    [SerializeReference] public BlackboardVariable<int> AttackTypeCount;
    [SerializeReference] public BlackboardVariable<bool> CanAttack;

    protected override Status OnStart()
    {
        if (Input == null || AttackTypeCount == 0 || !CanAttack.Value) return Status.Failure;
        CanAttack.Value = false;
        Input.Value.Attack(UnityEngine.Random.Range(1, AttackTypeCount.Value + 1));
        return Status.Success;
    }
}

