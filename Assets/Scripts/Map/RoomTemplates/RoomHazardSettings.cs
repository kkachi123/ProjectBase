namespace ProjectRE.MapGeneration
{
    using System.Collections.Generic;
    using UnityEngine;

    /// <summary>방에 배치한 함정 후보와 출현 설정을 보관한다.</summary>
    [DisallowMultipleComponent]
    public sealed class RoomHazardSettings : MonoBehaviour
    {
        [SerializeField] private List<RoomHazardSlot> _hazards = new();

        public IReadOnlyList<RoomHazardSlot> Hazards => _hazards;
    }
}
