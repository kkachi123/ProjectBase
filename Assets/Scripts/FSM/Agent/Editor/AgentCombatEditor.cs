using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class AgentCombatEditor : EditorWindow
{
    private AgentCombatHandler _targetHandler;
    private Animator _animator;
    private AgentStatData _statData;
    private int _selectedIndex = 0;

    private AnimationClip _currentClip;
    private float _animationTime;
    private bool _isSceneHandleEnabled = true;

    // Local storage for editing
    private AttackData _tempData;

    [MenuItem("Window/Custom/Agent Combat Editor")]
    public static void ShowWindow()
    {
        GetWindow<AgentCombatEditor>("Agent Combat Editor");
    }

    public static void OpenWithTarget(AgentCombatHandler handler)
    {
        AgentCombatEditor window = GetWindow<AgentCombatEditor>("Agent Combat Editor");
        window._targetHandler = handler;
        window._animator = handler.GetComponentInChildren<Animator>();
        window.AutoFindStatData();
        window.Show();
    }

    private void OnEnable()
    {
        SceneView.duringSceneGui += OnSceneGUI;
    }

    private void OnDisable()
    {
        SceneView.duringSceneGui -= OnSceneGUI;
        StopAnimationMode();
    }

    private void OnGUI()
    {
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Target Setup", EditorStyles.boldLabel);

        EditorGUI.BeginChangeCheck();
        _targetHandler = (AgentCombatHandler)EditorGUILayout.ObjectField("Target Handler", _targetHandler, typeof(AgentCombatHandler), true);
        if (EditorGUI.EndChangeCheck() && _targetHandler != null)
        {
            _animator = _targetHandler.GetComponentInChildren<Animator>();
            AutoFindStatData();
        }
        if (_targetHandler == null)
        {
            EditorGUILayout.HelpBox("Assign an AgentCombatHandler to edit its attack ranges.", MessageType.Info);
            return;
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Data Management", EditorStyles.boldLabel);

        EditorGUI.BeginChangeCheck();
        _statData = (AgentStatData)EditorGUILayout.ObjectField("Stat Data Asset", _statData, typeof(AgentStatData), false);
        if(EditorGUI.EndChangeCheck() && _statData == null)
        {
            EditorGUILayout.HelpBox("Assign an AgentStatData asset to edit attack ranges.", MessageType.Warning);
            return;
        }
        if (_statData != null)
        {
            string[] options = new string[_statData.attackDatas.Count];
            for (int i = 0; i < _statData.attackDatas.Count; i++)
                options[i] = $"Attack {i + 1}";
            _selectedIndex = EditorGUILayout.Popup("Select Index", _selectedIndex, options);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Load from SO")) LoadFromSO();
            if (GUILayout.Button("Save to SO")) SaveToSO();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Editing Values", EditorStyles.boldLabel);
            _tempData.damage = EditorGUILayout.FloatField("Damage", _tempData.damage);
            _tempData.offset = EditorGUILayout.Vector2Field("Offset", _tempData.offset);
            _tempData.size = EditorGUILayout.Vector2Field("Size", _tempData.size);

            _isSceneHandleEnabled = EditorGUILayout.Toggle("Show Scene Handles", _isSceneHandleEnabled);
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Animation Preview", EditorStyles.boldLabel);

        if (_animator != null && _animator.runtimeAnimatorController != null)
        {
            AnimationClip[] clips = _animator.runtimeAnimatorController.animationClips;
            if (clips.Length > 0)
            {
                string[] clipNames = new string[clips.Length];
                int currentClipIndex = -1;

                for (int i = 0; i < clips.Length; i++)
                {
                    clipNames[i] = clips[i].name;
                    if (clips[i] == _currentClip) currentClipIndex = i;
                }

                EditorGUI.BeginChangeCheck();
                int newIndex = EditorGUILayout.Popup("Select Clip", currentClipIndex, clipNames);
                if (EditorGUI.EndChangeCheck() && newIndex >= 0)
                {
                    _currentClip = clips[newIndex];
                    _animationTime = 0f;
                }
            }
        }

        _currentClip = (AnimationClip)EditorGUILayout.ObjectField("Clip Asset", _currentClip, typeof(AnimationClip), false);

        EditorGUI.BeginChangeCheck();
        _animationTime = EditorGUILayout.Slider("Timeline", _animationTime, 0f, 1f);
        if (EditorGUI.EndChangeCheck()) SampleAnimation();
        if (GUILayout.Button("Reset Pose / Stop Mode")) StopAnimationMode();
        if (GUI.changed) SceneView.RepaintAll();
    }

    private void AutoFindStatData()
    {
        var controller = _targetHandler.GetComponent<AgentController>();
        if (controller != null)
        {
            _statData = controller.StatData;
            if (_statData == null) Debug.LogWarning("AgentController does not have a reference to AgentStatData.");
        }
    }

    private void LoadFromSO()
    {
        if (_statData == null || _selectedIndex >= _statData.attackDatas.Count) return;
        _tempData = _statData.attackDatas[_selectedIndex];
        SceneView.RepaintAll();
    }

    private void SaveToSO()
    {
        if (_statData == null || _selectedIndex >= _statData.attackDatas.Count) return;
        Undo.RecordObject(_statData, "Save Attack Range");
        _statData.attackDatas[_selectedIndex] = _tempData;
        EditorUtility.SetDirty(_statData);
        AssetDatabase.SaveAssets();
        Debug.Log($"Saved AttackData index {_selectedIndex} to {_statData.name}");
    }

    private void SampleAnimation()
    {
        if (_animator == null || _currentClip == null || _targetHandler == null) return;

        if (!AnimationMode.InAnimationMode())
            AnimationMode.StartAnimationMode();

        AnimationMode.BeginSampling();
        AnimationMode.SampleAnimationClip(_animator.gameObject, _currentClip, _animationTime * _currentClip.length);
        AnimationMode.EndSampling();

        EditorApplication.QueuePlayerLoopUpdate();
        SceneView.RepaintAll();
    }

    private void StopAnimationMode()
    {
        if (AnimationMode.InAnimationMode())
            AnimationMode.StopAnimationMode();
        if (_animator != null) _animator.Rebind();
        SceneView.RepaintAll();
    }

    private void OnSceneGUI(SceneView sceneView)
    {
        if (!_isSceneHandleEnabled || _targetHandler == null) return;

        float facing = _targetHandler.transform.localScale.x > 0 ? 1f : -1f;
        Vector3 pos = _targetHandler.transform.position;
        Vector3 areaCenter = pos + new Vector3(_tempData.offset.x * facing, _tempData.offset.y, 0f);

        Handles.color = Color.yellow;
        Handles.DrawWireCube(areaCenter, new Vector3(_tempData.size.x, _tempData.size.y, 0.1f));

        // Center Handle
        EditorGUI.BeginChangeCheck();
        Vector3 newCenter = Handles.FreeMoveHandle(areaCenter, 0.1f, Vector3.zero, Handles.RectangleHandleCap);
        if (EditorGUI.EndChangeCheck())
        {
            Vector3 delta = newCenter - pos;
            _tempData.offset = new Vector2(delta.x * facing, delta.y);
            Repaint();
        }

        // Size Handles
        Handles.color = Color.white;
        EditorGUI.BeginChangeCheck();
        Vector3 rightPoint = areaCenter + new Vector3(_tempData.size.x * 0.5f, 0, 0);
        Vector3 newRight = Handles.Slider(rightPoint, Vector3.right, 0.05f, Handles.DotHandleCap, 0f);
        if (EditorGUI.EndChangeCheck())
        {
            _tempData.size.x = Mathf.Max(0.1f, (newRight.x - areaCenter.x) * 2f);
            Repaint();
        }

        EditorGUI.BeginChangeCheck();
        Vector3 topPoint = areaCenter + new Vector3(0, _tempData.size.y * 0.5f, 0);
        Vector3 newTop = Handles.Slider(topPoint, Vector3.up, 0.05f, Handles.DotHandleCap, 0f);
        if (EditorGUI.EndChangeCheck())
        {
            _tempData.size.y = Mathf.Max(0.1f, (newTop.y - areaCenter.y) * 2f);
            Repaint();
        }
    }
}
