namespace ProjectRE.MapGeneration
{
    using System;
    using System.Collections.Generic;
    using UnityEngine;

    [Serializable]
    public sealed class RoomSpawnOption
    {
        [SerializeField, HideInInspector] private string _id;
        [Tooltip("선택되면 이 프리팹을 슬롯 위치에 생성합니다.")] public GameObject prefab;
        [Tooltip("이 후보의 상대 출현 가중치입니다.")] public int weight = 1;
        [Tooltip("슬롯 기준점에서 프리팹을 옮길 거리입니다.")] public Vector2 offset;
        public string Id => _id;

    }

    [Serializable]
    public sealed class RoomSpawnSlot
    {
        [SerializeField, HideInInspector] private string _id;
        [Tooltip("방 내부의 기준 셀입니다.")] public Vector2Int position;
        [Tooltip("방 내부 좌표로 지정하는 후보 충돌체의 빈 공간입니다.")] public RectInt clearance;
        [Tooltip("공간에 인접한 바닥·천장·벽 지지면입니다.")] public ContentSurface surface;
        [Tooltip("생성된 프리팹이 바라볼 방향입니다.")] public ContentFacing facing;
        [Tooltip("후보를 생성하지 않을 상대 가중치입니다.")] public int emptyWeight = 1;
        [Tooltip("이 슬롯에서 선택할 프리팹 후보들입니다.")] public List<RoomSpawnOption> options = new();
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
            {
                if (slot != null)
                {
                    Gizmos.DrawWireSphere(new Vector3(slot.position.x + 0.5f, slot.position.y + 0.5f), 0.25f);
                    RectInt area = slot.clearance;
                    Gizmos.DrawWireCube(new Vector3(area.center.x, area.center.y), new Vector3(area.width, area.height));
                    RectInt support = RoomContentLayout.Support(area, slot.surface);
                    Gizmos.color = Color.blue;
                    Gizmos.DrawWireCube(new Vector3(support.center.x, support.center.y), new Vector3(support.width, support.height));
                    Gizmos.color = Kind == ContentKind.Item ? Color.yellow : Color.magenta;
                }
            }
        }

    }
}
