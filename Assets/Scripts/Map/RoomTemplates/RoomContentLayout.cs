namespace ProjectRE.MapGeneration
{
    using System.Collections.Generic;
    using UnityEngine;

    /// <summary>몬스터와 아이템 설정을 공통 슬롯으로 읽는다.</summary>
    internal static class RoomContentLayout
    {
        internal readonly struct Entry
        {
            public readonly RoomContentSlot Slot;
            public readonly Vector2Int Position;
            public readonly bool HasPosition;
            public readonly string Label;
            public Entry(RoomContentSlot slot, Vector2Int position, bool hasPosition, string label)
            { Slot = slot; Position = position; HasPosition = hasPosition; Label = label; }
        }

        internal static List<Entry> Read(RoomTemplate template)
        {
            var entries = new List<Entry>();
            Add(entries, template.GetComponent<RoomMonsterSettings>(), "monster");
            Add(entries, template.GetComponent<RoomItemSettings>(), "item");
            return entries;
        }

        private static void Add(List<Entry> entries, RoomSpawnSettings settings, string domain)
        {
            if (settings == null) return;
            for (int index = 0; index < settings.Slots.Count; index++)
            {
                var source = settings.Slots[index];
                string label = $"{(settings.Kind == ContentKind.Item ? "아이템" : "몬스터")} 슬롯 #{index + 1}";
                if (source == null || string.IsNullOrWhiteSpace(source.Id))
                { entries.Add(new Entry(null, default, false, label)); continue; }
                var slot = new RoomContentSlot
                {
                    slotIndex = StableIndex(domain, source.Id), clearance = source.clearance,
                    surface = source.surface, facing = source.facing, emptyWeight = source.emptyWeight
                };
                foreach (var option in source.options)
                    slot.options.Add(option == null ? null : new RoomContentOption
                    { id = option.Id, prefab = option.prefab, weight = option.weight, offset = option.offset, kind = settings.Kind });
                entries.Add(new Entry(slot, source.position, true, label));
            }
        }

        private static int StableIndex(string domain, string id)
        {
            uint hash = 2166136261u;
            foreach (char c in domain) hash = unchecked((hash ^ c) * 16777619u);
            foreach (char c in id) hash = unchecked((hash ^ c) * 16777619u);
            return (int)(hash & 0x7fffffffu);
        }

        internal static RectInt Support(RectInt area, ContentSurface surface) => surface switch
        {
            ContentSurface.Floor => new RectInt(area.xMin, area.yMin - 1, area.width, 1),
            ContentSurface.Ceiling => new RectInt(area.xMin, area.yMax, area.width, 1),
            ContentSurface.WestWall => new RectInt(area.xMin - 1, area.yMin, 1, area.height),
            ContentSurface.EastWall => new RectInt(area.xMax, area.yMin, 1, area.height),
            _ => new RectInt()
        };
    }
}
