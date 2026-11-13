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
            public Entry(RoomContentSlot slot, Vector2Int position, bool hasPosition)
            { Slot = slot; Position = position; HasPosition = hasPosition; }
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
            foreach (var source in settings.Slots)
            {
                if (source == null || string.IsNullOrWhiteSpace(source.Id))
                { entries.Add(new Entry(null, default, false)); continue; }
                var slot = new RoomContentSlot
                {
                    slotIndex = StableIndex(domain, source.Id), clearance = source.clearance,
                    surface = source.surface, facing = source.facing, emptyWeight = source.emptyWeight
                };
                foreach (var option in source.options)
                    slot.options.Add(option == null ? null : new RoomContentOption
                    { id = option.Id, prefab = option.prefab, weight = option.weight, offset = option.offset, kind = settings.Kind });
                entries.Add(new Entry(slot, source.position, true));
            }
        }

        private static int StableIndex(string domain, string id)
        {
            uint hash = 2166136261u;
            foreach (char c in domain) hash = unchecked((hash ^ c) * 16777619u);
            foreach (char c in id) hash = unchecked((hash ^ c) * 16777619u);
            return (int)(hash & 0x7fffffffu);
        }
    }
}
