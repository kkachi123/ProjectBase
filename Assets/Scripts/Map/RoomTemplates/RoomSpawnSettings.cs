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

    }
}
