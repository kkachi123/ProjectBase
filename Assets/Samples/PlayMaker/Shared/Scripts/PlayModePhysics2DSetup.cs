using System;
using System.Collections.Generic;
using HutongGames.PlayMaker.Actions;
using UnityEngine;

namespace HutongGames.PlayMaker.Samples
{
    /// <summary>
    /// Applies temporary 2D physics overrides while the editor is in play mode.
    /// Intended for samples that should not leave project settings changed.
    /// </summary>
    [AddComponentMenu("PlayMaker/Samples/Play Mode Physics2D Setup")]
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-10000)]
    [Icon(Strings.EditorIconsPath+"PlayMakerUtilityIcon.png")]
    public sealed class PlayModePhysics2DSetup : MonoBehaviour
    {
        [Serializable]
        public struct CollisionRule2D
        {
            [SerializeField] private int _layerA;
            [SerializeField] private int _layerB;
            [SerializeField] private bool _ignoreCollision;

            public int LayerA => _layerA;
            public int LayerB => _layerB;
            public bool IgnoreCollision => _ignoreCollision;
        }

        [SerializeField]
        [Tooltip("Override Time.fixedDeltaTime while in play mode.")]
        private bool _overrideFixedDeltaTime = true;

        [SerializeField]
        [Tooltip("Temporary fixed delta time to use while in play mode.")]
        private float _fixedDeltaTime = 1f / 30f;

        [SerializeField]
        [Tooltip("Temporary 2D layer collision overrides to apply while in play mode.")]
        private CollisionRule2D[] _collisionRules = Array.Empty<CollisionRule2D>();

        private static readonly Dictionary<ulong, bool> _originalLayerCollisionStates = new();

        private static PlayModePhysics2DSetup _activeSetup;
        private static bool _hasSnapshot;
        private static float _originalFixedDeltaTime;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStaticState()
        {
            _activeSetup = null;
            _hasSnapshot = false;
            _originalFixedDeltaTime = 0f;
            _originalLayerCollisionStates.Clear();
        }

        private void Awake()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            ApplyPlayModeOverrides();
        }

        private void OnValidate()
        {
            if (_fixedDeltaTime < 0.0001f)
            {
                _fixedDeltaTime = 0.0001f;
            }
        }

        public void ApplyPlayModeOverrides()
        {
            if (_activeSetup != null && _activeSetup != this)
            {
                Debug.LogWarning(
                    "PlayModePhysics2DSetup already applied by another object. " +
                    "Only one active setup is supported at a time.",
                    this);
                return;
            }

            if (!_hasSnapshot)
            {
                CaptureOriginalState();
            }

            _activeSetup = this;

            if (_overrideFixedDeltaTime)
            {
                Time.fixedDeltaTime = _fixedDeltaTime;
            }

            foreach (var rule in _collisionRules)
            {
                if (!IsValidLayer(rule.LayerA) || !IsValidLayer(rule.LayerB))
                {
                    Debug.LogWarning(
                        $"PlayModePhysics2DSetup ignored an invalid layer pair: {rule.LayerA}, {rule.LayerB}.",
                        this);
                    continue;
                }

                Physics2D.IgnoreLayerCollision(rule.LayerA, rule.LayerB, rule.IgnoreCollision);
            }
        }

        public static void RestorePlayModeOverrides()
        {
            if (!_hasSnapshot)
            {
                return;
            }

            Time.fixedDeltaTime = _originalFixedDeltaTime;

            foreach (var pair in _originalLayerCollisionStates)
            {
                DecodeLayerPairKey(pair.Key, out var layerA, out var layerB);
                Physics2D.IgnoreLayerCollision(layerA, layerB, pair.Value);
            }

            _originalLayerCollisionStates.Clear();
            _activeSetup = null;
            _hasSnapshot = false;
            _originalFixedDeltaTime = 0f;
        }

        private void CaptureOriginalState()
        {
            _originalFixedDeltaTime = Time.fixedDeltaTime;
            _originalLayerCollisionStates.Clear();

            foreach (var rule in _collisionRules)
            {
                if (!IsValidLayer(rule.LayerA) || !IsValidLayer(rule.LayerB))
                {
                    continue;
                }

                var key = GetLayerPairKey(rule.LayerA, rule.LayerB);
                if (_originalLayerCollisionStates.ContainsKey(key))
                {
                    continue;
                }

                _originalLayerCollisionStates.Add(
                    key,
                    Physics2D.GetIgnoreLayerCollision(rule.LayerA, rule.LayerB));
            }

            _hasSnapshot = true;
        }

        private static bool IsValidLayer(int layer)
        {
            return layer >= 0 && layer < 32;
        }

        private static ulong GetLayerPairKey(int layerA, int layerB)
        {
            if (layerA > layerB)
            {
                (layerA, layerB) = (layerB, layerA);
            }

            return ((ulong)(uint)layerA << 32) | (uint)layerB;
        }

        private static void DecodeLayerPairKey(ulong key, out int layerA, out int layerB)
        {
            layerA = (int)(key >> 32);
            layerB = (int)(key & uint.MaxValue);
        }
    }
}
