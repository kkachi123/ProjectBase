using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "AIGrondDetectorAction", story: "[Detector] Find [JumpFloor]", category: "Action", id: "96c350971e8c73dae7dcc0b286c39a9b")]
public partial class AIJumpGrondDetectorAction : Action
{
    [SerializeReference] public BlackboardVariable<JumpFloorDetector> Detector;
    [SerializeReference] public BlackboardVariable<Transform> JumpFloor; // Output

    Transform ground;
    protected override Status OnStart()
    {
        ground = null;
        return Status.Running;
    }

    protected override Status OnUpdate()
    {
        ground = Detector.Value.GetClosedGround();
        if(ground != null) return Status.Success;
        return Status.Failure;
    }

    protected override void OnEnd()
    {
        if(ground != null) JumpFloor.Value = ground;
    }
}

