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
    [SerializeReference] public BlackboardVariable<List<float>> AttackRanges;
    [SerializeReference] public BlackboardVariable<int> AttackType;
    [SerializeReference] public BlackboardVariable<bool> CanAttack;

    protected override Status OnStart()
    {
        if (Input == null || !CanAttack.Value) return Status.Failure;
        CanAttack.Value = false;
        int attackType = AttackType.Value;
        // 범위 밖이면 가장 긴 공격을 사용하도록 설정
        if (AttackType == 0) attackType = 2; 

        Input.Value.Attack(attackType);

        return Status.Success;
    }
}

