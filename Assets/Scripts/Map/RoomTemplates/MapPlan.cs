namespace ProjectRE.MapGeneration
{
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using UnityEngine;

    public sealed class MapRoomPlan
    {
        public Vector2Int Cell { get; }
        public string TemplateId { get; }
        public RoomType Type { get; }
        public RoomOpening Openings { get; }
        public RoomOpening Entry { get; }
        public RoomOpening Exit { get; }
        public IReadOnlyList<RoomTerrainSelection> Terrain { get; }
        public RoomOpening Branch { get; }
        public bool IsOptional { get; }
        public IReadOnlyList<ContentSelection> Content { get; }

        public MapRoomPlan(Vector2Int cell, string templateId, RoomType type,
            RoomOpening openings, RoomOpening entry, RoomOpening exit, IEnumerable<RoomTerrainSelection> terrain = null,
            RoomOpening branch = RoomOpening.None, bool isOptional = false, IEnumerable<ContentSelection> content = null)
        {
            Cell = cell;
            TemplateId = templateId;
            Type = type;
            Openings = openings;
            Entry = entry;
            Exit = exit;
            Terrain = new List<RoomTerrainSelection>(terrain ?? System.Array.Empty<RoomTerrainSelection>()).AsReadOnly();
            Branch = branch;
            IsOptional = isOptional;
            Content = new List<ContentSelection>(content ?? System.Array.Empty<ContentSelection>()).AsReadOnly();
        }
    }

    /// <summary>격자 y는 위에서 아래로 증가한다.</summary>
    public sealed class MapPlan
    {
        private readonly ReadOnlyCollection<MapRoomPlan> _rooms;
        private readonly ReadOnlyCollection<Vector2Int> _path;
        public int Width { get; }
        public int Height { get; }
        public int Seed { get; }
        public IReadOnlyList<MapRoomPlan> Rooms => _rooms;
        public IReadOnlyList<Vector2Int> Path => _path;
        public Vector3 PlayerStart { get; }
        public Vector3 Goal { get; }

        public MapPlan(int width, int height, int seed, IEnumerable<MapRoomPlan> rooms,
            IEnumerable<Vector2Int> path, Vector3 playerStart, Vector3 goal)
        {
            Width = width;
            Height = height;
            Seed = seed;
            _rooms = new List<MapRoomPlan>(rooms).AsReadOnly();
            _path = new List<Vector2Int>(path).AsReadOnly();
            PlayerStart = playerStart;
            Goal = goal;
        }
    }

    public sealed class MapPlanningResult
    {
        public bool Success => Plan != null;
        public MapPlan Plan { get; }
        public string Error { get; }
        public int Attempts { get; }

        private MapPlanningResult(MapPlan plan, string error, int attempts)
        {
            Plan = plan;
            Error = error;
            Attempts = attempts;
        }

        public static MapPlanningResult Succeeded(MapPlan plan, int attempts) => new(plan, null, attempts);
        public static MapPlanningResult Failed(string error, int attempts) => new(null, error, attempts);
    }
}
