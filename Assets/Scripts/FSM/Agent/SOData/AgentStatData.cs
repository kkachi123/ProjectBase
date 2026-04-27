using System;
using UnityEngine;
using System.Collections.Generic;

[Serializable]
public struct AttackData
{
    public float damage;
    public Vector2 offset;
    public Vector2 size;
}

[CreateAssetMenu(fileName = "AgentStats", menuName = "Agent/Stats")]
public class AgentStatData : ScriptableObject
{
    public float maxHealth = 10f;
    public List<AttackData> attackDatas = new List<AttackData>();
}
