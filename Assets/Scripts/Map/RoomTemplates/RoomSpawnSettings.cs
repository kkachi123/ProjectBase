namespace ProjectRE.MapGeneration
{
    using System;
    using System.Collections.Generic;
    using UnityEngine;

    [Serializable]
    public sealed class RoomSpawnOption
    {
        [SerializeField, HideInInspector] private string _id;
        public GameObject prefab;
        public int weight = 1;
        public Vector2 offset;
        public string Id => _id;

#if UNITY_EDITOR
        internal void EnsureId(HashSet<string> used)
        {
            if (string.IsNullOrEmpty(_id) || !used.Add(_id))
            {
                do { _id = Guid.NewGuid().ToString("N"); } while (!used.Add(_id));
            }
        }
#endif
    }

    [Serializable]
    public sealed class RoomSpawnSlot
    {
        [SerializeField, HideInInspector] private string _id;
        public Vector2Int position;
        public RectInt clearance;
        public ContentSurface surface;
        public ContentFacing facing;
        public int emptyWeight = 1;
        public List<RoomSpawnOption> options = new();
        public string Id => _id;

#if UNITY_EDITOR
        internal void EnsureId(HashSet<string> used)
        {
            if (string.IsNullOrEmpty(_id) || !used.Add(_id))
            {
                do { _id = Guid.NewGuid().ToString("N"); } while (!used.Add(_id));
            }
            var optionIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (var option in options) option?.EnsureId(optionIds);
        }
#endif
    }

    /// <summary>방에 설정할 생성 위치와 프리팹 후보.</summary>
    public abstract class RoomSpawnSettings : MonoBehaviour
    {
        [SerializeField] private List<RoomSpawnSlot> _slots = new();
        public IReadOnlyList<RoomSpawnSlot> Slots => _slots;
        public abstract ContentKind Kind { get; }

        protected void OnDrawGizmosSelected()
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.color = Kind == ContentKind.Item ? Color.yellow : Color.magenta;
            foreach (var slot in _slots)
                if (slot != null)
                    Gizmos.DrawWireSphere(new Vector3(slot.position.x + 0.5f, slot.position.y + 0.5f), 0.25f);
        }

#if UNITY_EDITOR
        protected void OnValidate()
        {
            var ids = new HashSet<string>(StringComparer.Ordinal);
            foreach (var slot in _slots) slot?.EnsureId(ids);
        }
#endif
    }
}
