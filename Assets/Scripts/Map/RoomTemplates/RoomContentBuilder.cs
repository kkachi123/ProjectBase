namespace ProjectRE.MapGeneration
{
    using System;
    using System.Collections.Generic;
    using UnityEngine;

    /// <summary>방 계획의 콘텐츠를 생성하고 슬롯별 결과를 반환한다.</summary>
    internal static class RoomContentBuilder
    {
        public static Dictionary<int, GameObject> Build(MapRoomPlan room, RoomTemplate template, MapBuildContext context,
            List<GameObject> monsters)
        {
            var result = new Dictionary<int, GameObject>();
            var entries = RoomContentLayout.Read(template);
            foreach (ContentSelection selection in room.Content)
            {
                if (string.IsNullOrEmpty(selection.OptionId)) continue;
                RoomContentSlot slot = null;
                Vector2Int point = default;
                foreach (var entry in entries)
                    if (entry.Slot != null && entry.Slot.slotIndex == selection.SlotIndex)
                    { slot = entry.Slot; point = entry.Position; break; }
                if (slot == null) throw new InvalidOperationException("콘텐츠 슬롯을 찾을 수 없습니다.");
                RoomContentOption option = null;
                foreach (RoomContentOption candidate in slot.options)
                    if (candidate.id == selection.OptionId) { option = candidate; break; }
                if (option == null || option.prefab == null) throw new InvalidOperationException($"콘텐츠를 찾을 수 없습니다: {selection.OptionId}");
                GameObject content = context.InstantiatePrefab(option.prefab, template.transform);
                if (content == null) throw new InvalidOperationException($"콘텐츠 생성 실패: {selection.OptionId}");
                content.transform.localPosition = new Vector3(point.x + .5f + option.offset.x, point.y + option.offset.y, 0);
                content.transform.localRotation = Quaternion.identity;
                Vector3 scale = content.transform.localScale;
                scale.x = Mathf.Abs(scale.x) * (slot.facing == ContentFacing.Left ? -1 : 1);
                content.transform.localScale = scale;
                result.Add(selection.SlotIndex, content);
                if (option.kind == ContentKind.Monster || option.kind == ContentKind.EliteMonster)
                    monsters.Add(content);
            }
            return result;
        }
    }
}
