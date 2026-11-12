namespace ProjectRE.MapGeneration
{
    using System.Collections.Generic;
    using UnityEngine;

    /// <summary>방의 몬스터·아이템 배치 지점과 후보를 설정한다.</summary>
    [DisallowMultipleComponent]
    public sealed class RoomContentSettings : MonoBehaviour
    {
        [SerializeField] private List<Vector2Int> _contentSlots = new();
        [SerializeField] private List<RoomContentSlot> _contentSettings = new();

        public IReadOnlyList<Vector2Int> ContentSlots => _contentSlots;
        public IReadOnlyList<RoomContentSlot> ContentSettings => _contentSettings;

        private void OnDrawGizmosSelected()
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.color = Color.yellow;
            foreach (Vector2Int point in _contentSlots)
                Gizmos.DrawWireSphere(new Vector3(point.x + 0.5f, point.y + 0.5f), 0.25f);
        }
    }
}
