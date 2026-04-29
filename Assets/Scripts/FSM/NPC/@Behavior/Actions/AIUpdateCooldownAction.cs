using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "AIUpdateCooldown", story: "Update [CanValue] [Delay] delay", category: "Action", id: "72757c75e1ed74cd127feaead103ecba")]
public partial class AIUpdateCooldownAction : Action
{
    [SerializeReference] public BlackboardVariable<bool> CanValue;
    [SerializeReference] public BlackboardVariable<float> Delay;
    private float _timer = 0;
    protected override Status OnUpdate()
    {
        if (CanValue == null) return Status.Failure;
        if(!CanValue.Value)
        {
            _timer += Time.deltaTime;
            if(Delay != null && _timer >= Delay.Value)
            {
                CanValue.Value = true;
                _timer = 0;
            }
        }

        return Status.Success;
    }
}


