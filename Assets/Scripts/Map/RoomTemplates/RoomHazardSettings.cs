namespace ProjectRE.MapGeneration
{
    using System;
    using System.Collections.Generic;
    using UnityEngine;

    /// <summary>방에 배치한 함정 후보와 출현 설정을 보관한다.</summary>
    [DisallowMultipleComponent]
    public sealed class RoomHazardSettings : MonoBehaviour
    {
        [SerializeField] private List<RoomHazardSlot> _hazards = new();

        public IReadOnlyList<RoomHazardSlot> Hazards => _hazards;

        private void OnDrawGizmosSelected()
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            foreach (var slot in _hazards)
            {
                if (slot == null) continue;
                Gizmos.color = new Color(1f, .5f, 0f);
                Gizmos.DrawWireCube(new Vector3(slot.clearance.center.x, slot.clearance.center.y),
                    new Vector3(slot.clearance.width, slot.clearance.height));
                Gizmos.color = Color.blue;
                Gizmos.DrawWireCube(new Vector3(slot.support.center.x, slot.support.center.y),
                    new Vector3(slot.support.width, slot.support.height));
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            var ids = new HashSet<string>(StringComparer.Ordinal);
            foreach (var slot in _hazards)
            {
                if (slot == null) continue;
                if (!string.IsNullOrWhiteSpace(slot.id) && ids.Add(slot.id)) continue;
                do { slot.id = Guid.NewGuid().ToString("N"); } while (!ids.Add(slot.id));
            }
        }
#endif
    }
}
