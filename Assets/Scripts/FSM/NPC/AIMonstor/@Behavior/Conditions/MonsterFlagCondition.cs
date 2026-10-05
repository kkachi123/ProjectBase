using System;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;

/// <summary>감지 Component가 갱신한 Blackboard 플래그 확인.</summary>
[Serializable, GeneratePropertyBag]
[Condition(name: "Monster Flag", story: "[Flag] is true", category: "Monster",
    id: "b310aa1e5f0e4f5bb5abb1110cab0101")]
public partial class MonsterFlagCondition : Condition
{
    [SerializeReference] public BlackboardVariable<bool> Flag;

    /// <summary>현재 개체의 Blackboard 조건 반환.</summary>
    public override bool IsTrue() => Flag.Value;
}
