namespace ProjectRE.MapGeneration
{
    using System;
    using System.Collections.Generic;
    using UnityEngine;

    /// <summary>방에 배치한 함정 후보와 설치 공간.</summary>
    [Serializable]
    public sealed class RoomHazardSlot
    {
        [HideInInspector] public string id;
        public GameObject candidate;
        public RectInt clearance;
        public RectInt support;
        public int spawnWeight = 1;
        public int emptyWeight = 1;
    }

    public sealed class RoomHazardSnapshot
    {
        public string Id { get; }
        public int SpawnWeight { get; }
        public int EmptyWeight { get; }

        public RoomHazardSnapshot(string id, int spawnWeight, int emptyWeight)
        { Id = id; SpawnWeight = spawnWeight; EmptyWeight = emptyWeight; }
    }

    public sealed class RoomHazardSelection
    {
        public string Id { get; }
        public bool Spawn { get; }

        public RoomHazardSelection(string id, bool spawn) { Id = id; Spawn = spawn; }
    }

    public static class RoomHazardPlanner
    {
        public static IReadOnlyList<RoomHazardSelection> Select(int seed, Vector2Int cell, RoomTemplateSnapshot template)
        {
            var result = new List<RoomHazardSelection>();
            foreach (RoomHazardSnapshot slot in template.Hazards)
            {
                uint hash = 2166136261u;
                void Add(uint value) { hash = unchecked((hash ^ value) * 16777619u); }
                Add(unchecked((uint)seed)); Add(unchecked((uint)cell.x)); Add(unchecked((uint)cell.y));
                foreach (char c in template.Id) Add(c);
                Add(0xA591D71Bu);
                foreach (char c in slot.Id) Add(c);
                hash = unchecked(hash * 1664525u + 1013904223u);
                int total = slot.SpawnWeight + slot.EmptyWeight;
                int pick = (int)(((ulong)hash * (uint)total) >> 32);
                result.Add(new RoomHazardSelection(slot.Id, pick < slot.SpawnWeight));
            }
            return result.AsReadOnly();
        }
    }
}
