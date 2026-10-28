namespace ProjectRE.MapGeneration
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using UnityEngine;

    public sealed class RoomTerrainSnapshot
    {
        public int RegionIndex { get; }
        public bool AllowEmptyPattern { get; }
        public IReadOnlyList<string> PatternIds { get; }
        public IReadOnlyList<Vector2Int> Cells { get; }
        public IReadOnlyList<int> Chances { get; }

        public RoomTerrainSnapshot(int index, bool allowEmpty, IEnumerable<string> patternIds,
            IEnumerable<Vector2Int> cells, IEnumerable<int> chances)
        {
            RegionIndex = index;
            AllowEmptyPattern = allowEmpty;
            var ids = new List<string>(patternIds);
            ids.Sort(StringComparer.Ordinal);
            PatternIds = ids.AsReadOnly();
            Cells = new List<Vector2Int>(cells).AsReadOnly();
            Chances = new List<int>(chances).AsReadOnly();
        }
    }

    public sealed class RoomTerrainSelection
    {
        public int RegionIndex { get; }
        // 빈 문자열은 빈 패턴이다.
        public string PatternId { get; }
        public IReadOnlyList<Vector2Int> FilledCells { get; }

        public RoomTerrainSelection(int regionIndex, string patternId, IEnumerable<Vector2Int> filledCells)
        {
            RegionIndex = regionIndex;
            PatternId = patternId;
            FilledCells = new List<Vector2Int>(filledCells).AsReadOnly();
        }
    }

    public static class RoomTerrainPlanner
    {
        public static IReadOnlyList<RoomTerrainSelection> Select(int seed, Vector2Int roomCell, RoomTemplateSnapshot template)
        {
            var result = new List<RoomTerrainSelection>();
            foreach (RoomTerrainSnapshot region in template.Terrain)
            {
                int count = region.PatternIds.Count + (region.AllowEmptyPattern ? 1 : 0);
                uint random = Hash(seed, roomCell, template.Id, region.RegionIndex);
                int selected = Next(ref random, count);
                string id = region.AllowEmptyPattern && selected == 0 ? "" : region.PatternIds[selected - (region.AllowEmptyPattern ? 1 : 0)];
                var cells = new List<Vector2Int>();
                for (int i = 0; i < region.Cells.Count; i++)
                    if (Next(ref random, 100) < region.Chances[i]) cells.Add(region.Cells[i]);
                result.Add(new RoomTerrainSelection(region.RegionIndex, id, cells));
            }
            return new ReadOnlyCollection<RoomTerrainSelection>(result);
        }

        private static uint Hash(int seed, Vector2Int cell, string id, int region)
        {
            uint h = 2166136261u;
            void Add(uint value) { h = unchecked((h ^ value) * 16777619u); }
            Add(unchecked((uint)seed)); Add(unchecked((uint)cell.x)); Add(unchecked((uint)cell.y));
            foreach (char c in id) Add(c);
            Add(unchecked((uint)region));
            return h;
        }

        private static int Next(ref uint state, int max)
        {
            state = unchecked(state * 1664525u + 1013904223u);
            return (int)(((ulong)state * (uint)max) >> 32);
        }
    }
}
