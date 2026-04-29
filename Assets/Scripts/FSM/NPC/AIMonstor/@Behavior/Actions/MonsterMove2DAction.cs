using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "MonsterMove2DAction", story: "[Input] RandomMove between [Min] ~ [Max]", category: "Action", id: "8b7b8dd3e41ecc07bda2b765d5f3eb92")]
public partial class MonsterMove2DAction : Action
{
    [SerializeReference] public BlackboardVariable<AIMonsterInput> Input;
    [SerializeReference] public BlackboardVariable<float> Min = new BlackboardVariable<float>(1);
    [SerializeReference] public BlackboardVariable<float> Max = new BlackboardVariable<float>(3);
    [SerializeReference] public BlackboardVariable<bool> IsLeftGrounded;
    [SerializeReference] public BlackboardVariable<bool> IsRightGrounded;
    Vector2 dir;
    float duration;
    float elapsedTime = 0f;
    int direction;
    bool IsFrontGrounded => IsLeftGrounded.Value && direction == -1 || IsRightGrounded.Value && direction == 1;
    protected override Status OnStart()
    {
        direction = UnityEngine.Random.value > 0.5f ? 1 : -1;
        dir = new Vector2(direction, 0); 

        duration = UnityEngine.Random.Range(Min.Value, Max.Value);
        elapsedTime = 0f;

        return Status.Running;
    }

    protected override Status OnUpdate()
    {
        if(elapsedTime > duration) return Status.Success;

        elapsedTime += Time.deltaTime;

        if(!IsFrontGrounded)
        {
            direction *= -1;
            dir = new Vector2(direction, 0);
        }

        Input.Value.Move(dir);

        return Status.Running;
    }

    protected override void OnEnd()
    {
        Input.Value.Move(Vector2.zero);
    }
}

