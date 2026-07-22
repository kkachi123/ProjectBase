using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(AgentCombatHandler))]
public class AgentCombatHandlerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        AgentCombatHandler handler = (AgentCombatHandler)target;

        EditorGUILayout.Space();
        if (GUILayout.Button("Open Agent Combat Editor", GUILayout.Height(30)))
        {
            AgentCombatEditor.OpenWithTarget(handler);
        }
    }
}
