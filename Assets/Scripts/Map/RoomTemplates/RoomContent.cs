namespace ProjectRE.MapGeneration
{
    using System;
    using System.Collections.Generic;
    using UnityEngine;

    public enum ContentSurface { Floor, WestWall, EastWall, Ceiling }
    public enum ContentFacing { Right, Left }
    public enum ContentKind { Unspecified, Item, Monster, EliteMonster }
    public enum ContentRoomRole { MainPath, Optional }

    [Serializable]
    public sealed class RoomContentOption
    {
        public string id;
        public GameObject prefab;
        public int weight = 1;
        public Vector2 offset;
        public ContentKind kind;
    }

    [Serializable]
    public sealed class RoomContentSlot
    {
        public int slotIndex;
        public RectInt clearance;
        public ContentSurface surface;
        public ContentFacing facing;
        public int emptyWeight = 1;
        public List<RoomContentOption> options = new();
    }

    public sealed class ContentOptionSnapshot
    {
        public string Id { get; }
        public int Weight { get; }
        public ContentKind Kind { get; }
        public ContentOptionSnapshot(string id, int weight, ContentKind kind = ContentKind.Item)
        { Id = id; Weight = weight; Kind = kind; }
    }

    public sealed class ContentSlotSnapshot
    {
        public int SlotIndex { get; }
        public int EmptyWeight { get; }
        public IReadOnlyList<ContentOptionSnapshot> Options { get; }
        public ContentSlotSnapshot(int index, int emptyWeight, IEnumerable<ContentOptionSnapshot> options)
        {
            SlotIndex = index;
            EmptyWeight = emptyWeight;
            var list = new List<ContentOptionSnapshot>(options);
            list.Sort((a, b) => string.CompareOrdinal(a.Id, b.Id));
            Options = list.AsReadOnly();
        }
    }

    public sealed class ContentSelection
    {
        public int SlotIndex { get; }
        // 빈 문자열은 슬롯을 비운다.
        public string OptionId { get; }
        public ContentSelection(int index, string optionId) { SlotIndex = index; OptionId = optionId; }
    }

    public static class RoomContentPlanner
    {
        public static IReadOnlyList<ContentSelection> Select(int seed, Vector2Int room, RoomTemplateSnapshot template,
            ContentRoomRole role)
        {
            var result = new List<ContentSelection>();
            foreach (ContentSlotSnapshot slot in template.Content)
            {
                uint h = 2166136261u;
                void Add(uint value) { h = unchecked((h ^ value) * 16777619u); }
                Add(unchecked((uint)seed)); Add(unchecked((uint)room.x)); Add(unchecked((uint)room.y));
                foreach (char c in template.Id) Add(c);
                Add(0xC071E17u); Add(unchecked((uint)slot.SlotIndex));
                h = unchecked(h * 1664525u + 1013904223u);
                int total = slot.EmptyWeight;
                foreach (ContentOptionSnapshot option in slot.Options)
                    if (Allowed(option.Kind, role)) total += option.Weight;
                if (total == 0)
                {
                    result.Add(new ContentSelection(slot.SlotIndex, ""));
                    continue;
                }
                int pick = (int)(((ulong)h * (uint)total) >> 32);
                string selected = "";
                if (pick >= slot.EmptyWeight)
                {
                    pick -= slot.EmptyWeight;
                    foreach (ContentOptionSnapshot option in slot.Options)
                    {
                        if (!Allowed(option.Kind, role)) continue;
                        if (pick < option.Weight) { selected = option.Id; break; }
                        pick -= option.Weight;
                    }
                }
                result.Add(new ContentSelection(slot.SlotIndex, selected));
            }
            return result.AsReadOnly();
        }

        private static bool Allowed(ContentKind kind, ContentRoomRole role) =>
            kind == ContentKind.Item ||
            (role == ContentRoomRole.MainPath && kind == ContentKind.Monster) ||
            (role == ContentRoomRole.Optional && kind == ContentKind.EliteMonster);
    }
}
