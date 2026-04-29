using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "AIUpdateAttackDelay", story: "Update [CanAttack] [AttackDelay] delay", category: "Action", id: "72757c75e1ed74cd127feaead103ecba")]
public partial class AIUpdateAttackDelayAction : Action
{
    [SerializeReference] public BlackboardVariable<bool> CanAttack;
    [SerializeReference] public BlackboardVariable<float> AttackDelay;
    private float _timer = 0;
    protected override Status OnUpdate()
    {
        if (CanAttack == null) return Status.Failure;
        if(!CanAttack.Value)
        {
            _timer += Time.deltaTime;
            if(AttackDelay != null && _timer >= AttackDelay.Value)
            {
                CanAttack.Value = true;
                _timer = 0;
            }
        }

        return Status.Success;
    }
}


