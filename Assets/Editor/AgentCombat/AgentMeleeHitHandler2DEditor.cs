using UnityEngine;
using UnityEditor;
using ProjectRE;

[CustomEditor(typeof(AgentMeleeHitHandler2D))]
public class AgentMeleeHitHandler2DEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        AgentMeleeHitHandler2D handler = (AgentMeleeHitHandler2D)target;

        EditorGUILayout.Space();
        if (GUILayout.Button("Open Agent Combat Editor", GUILayout.Height(30)))
        {
            AgentCombatEditor.OpenWithTarget(handler);
        }
    }
}
