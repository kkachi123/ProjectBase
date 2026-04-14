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
    public List<AttackData> attackDatas = new List<AttackData>()
        { 
            new AttackData() { damage = 1f, offset = new Vector2(0.8f, 0f), size = new Vector2(0.5f, 1f) }, 
            new AttackData() { damage = 1f, offset = new Vector2(0.95f, 0f), size = new Vector2(1f, 1f) },
            new AttackData() { damage = 1f, offset = new Vector2(0.4f, 0f), size = new Vector2(1f, 1f) }, 
        };
}
