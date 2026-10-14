using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using ProjectRE;

public class AgentCombatEditor : EditorWindow
{
    private AgentMeleeHitHandler2D _targetHandler;
    private Animator _animator;
    private AgentStatData _statData;
    private PlayerController _playerController;
    private readonly List<PlayerAttackDataSO> _attackAssets = new();
    private readonly List<string> _attackLabels = new();
    private int _selectedIndex = 0;
    private int AttackCount => _playerController != null ? _attackAssets.Count : _statData != null ? _statData.attackDatas.Count : 0;

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

    public static void OpenWithTarget(AgentMeleeHitHandler2D handler)
    {
        AgentCombatEditor window = GetWindow<AgentCombatEditor>("Agent Combat Editor");
        window._targetHandler = handler;
        window.RefreshTarget();
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
        _targetHandler = (AgentMeleeHitHandler2D)EditorGUILayout.ObjectField("Target Handler", _targetHandler, typeof(AgentMeleeHitHandler2D), true);
        if (EditorGUI.EndChangeCheck()) RefreshTarget();
        if (_targetHandler == null)
        {
            EditorGUILayout.HelpBox("Assign an AgentMeleeHitHandler2D to edit its attack ranges.", MessageType.Info);
            return;
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Data Management", EditorStyles.boldLabel);

        if (_playerController == null)
        {
            EditorGUI.BeginChangeCheck();
            _statData = (AgentStatData)EditorGUILayout.ObjectField("Stat Data Asset", _statData, typeof(AgentStatData), false);
            if (EditorGUI.EndChangeCheck())
            {
                _selectedIndex = 0;
                LoadFromSO();
            }
        }
        if (AttackCount > 0)
        {
            string[] options = new string[AttackCount];
            for (int i = 0; i < AttackCount; i++)
                options[i] = _playerController != null ? _attackLabels[i] : $"Attack {i + 1}";
            _selectedIndex = Mathf.Clamp(_selectedIndex, 0, AttackCount - 1);
            EditorGUI.BeginChangeCheck();
            _selectedIndex = EditorGUILayout.Popup("Select Index", _selectedIndex, options);
            if (EditorGUI.EndChangeCheck()) LoadFromSO();

            if (_playerController != null)
            {
                using (new EditorGUI.DisabledScope(true))
                    EditorGUILayout.ObjectField("Attack Data Asset", _attackAssets[_selectedIndex], typeof(PlayerAttackDataSO), false);
            }

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Load from SO")) LoadFromSO();
            if (GUILayout.Button("Save to SO")) SaveToSO();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Editing Values", EditorStyles.boldLabel);
            _tempData.damage = EditorGUILayout.FloatField("Damage", _tempData.damage);
            _tempData.usedStamina = EditorGUILayout.FloatField("Used Stamina", _tempData.usedStamina);
            _tempData.offset = EditorGUILayout.Vector2Field("Offset", _tempData.offset);
            _tempData.size = EditorGUILayout.Vector2Field("Size", _tempData.size);

            _isSceneHandleEnabled = EditorGUILayout.Toggle("Show Scene Handles", _isSceneHandleEnabled);
        }
        else
            EditorGUILayout.HelpBox("Assign attack data in the Player Attack Handler or Monster Stat Data.", MessageType.Info);

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

    // 대상 변경 시에만 Controller·Animator 및 편집할 SO 목록 확보.
    private void RefreshTarget()
    {
        StopAnimationMode();
        _animator = null;
        _statData = null;
        _playerController = null;
        _attackAssets.Clear();
        _attackLabels.Clear();
        _selectedIndex = 0;
        _tempData = default;
        if (_targetHandler == null) return;

        _animator = _targetHandler.GetComponentInChildren<Animator>();
        AgentController controller = _targetHandler.GetComponent<AgentController>();
        _playerController = controller as PlayerController;
        if (_playerController != null)
        {
            foreach (PlayerAttackHandler.AttackDefinition definition in _playerController.AttackHandler.AttackDefinitions)
            {
                for (int i = 0; i < definition.Attacks.Length; i++)
                {
                    if (definition.Attacks[i] == null) continue;
                    _attackAssets.Add(definition.Attacks[i]);
                    _attackLabels.Add($"{definition.Role} / {i + 1}");
                }
            }
        }
        else if (controller != null)
            _statData = controller.StatData;
        LoadFromSO();
    }

    private void LoadFromSO()
    {
        if (_selectedIndex >= AttackCount) return;
        _tempData = _playerController != null ? _attackAssets[_selectedIndex].Data : _statData.attackDatas[_selectedIndex];
        SceneView.RepaintAll();
    }

    private void SaveToSO()
    {
        if (_selectedIndex >= AttackCount) return;
        Object asset = _playerController != null ? _attackAssets[_selectedIndex] : (Object)_statData;
        Undo.RecordObject(asset, "Save Attack Range");
        if (_playerController != null)
            _attackAssets[_selectedIndex].Data = _tempData;
        else
            _statData.attackDatas[_selectedIndex] = _tempData;
        EditorUtility.SetDirty(asset);
        AssetDatabase.SaveAssetIfDirty(asset);
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
        if (!_isSceneHandleEnabled || _targetHandler == null || AttackCount == 0) return;

        float facing = _targetHandler.transform.localScale.x > 0 ? 1f : -1f;
        Vector3 pos = _targetHandler.transform.position;
        _targetHandler.GetHitBox(_tempData, out Vector2 center, out Vector2 size);
        Vector3 areaCenter = new Vector3(center.x, center.y, pos.z);

        Handles.color = Color.yellow;
        Handles.DrawWireCube(areaCenter, new Vector3(size.x, size.y, 0.1f));

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
