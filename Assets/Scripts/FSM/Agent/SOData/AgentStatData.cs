using UnityEngine;

[CreateAssetMenu(fileName = "AgentStats", menuName = "Agent/Stats")]
public class AgentStatData : ScriptableObject
{
    public float maxHealth = 10f;
    public float attackDamage = 1f;
}
