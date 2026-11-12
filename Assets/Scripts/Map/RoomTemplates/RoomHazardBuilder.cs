namespace ProjectRE.MapGeneration
{
    using System;

    /// <summary>방 계획에서 선택한 함정 후보를 활성화한다.</summary>
    internal static class RoomHazardBuilder
    {
        public static void Apply(MapRoomPlan room, RoomHazardSettings settings, MapBuildContext context)
        {
            foreach (RoomHazardSelection selection in room.Hazards)
            {
                if (!selection.Spawn) continue;
                RoomHazardSlot slot = null;
                foreach (RoomHazardSlot candidate in settings.Hazards)
                    if (candidate.id == selection.Id) { slot = candidate; break; }
                if (slot == null || slot.candidate == null) throw new InvalidOperationException($"함정 후보를 찾을 수 없습니다: {selection.Id}");
                context.SetActive(slot.candidate, true);
            }
        }
    }
}
