namespace ProjectRE.MapGeneration
{
    using System;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.Tilemaps;

    public sealed class MapBuildResult
    {
        public bool Success => Root != null;
        public GameObject Root { get; }
        public MapPlan Plan { get; }
        public string Error { get; }
        public IReadOnlyList<BuiltRoom> Rooms { get; }

        private MapBuildResult(GameObject root, MapPlan plan, string error, IReadOnlyList<BuiltRoom> rooms)
        { Root = root; Plan = plan; Error = error; Rooms = rooms; }

        public static MapBuildResult Succeeded(GameObject root, MapPlan plan, IReadOnlyList<BuiltRoom> rooms) => new(root, plan, null, rooms);
        public static MapBuildResult Failed(string error) => new(null, null, error, Array.Empty<BuiltRoom>());
    }

    /// <summary>확정된 방과 생성된 콘텐츠를 런타임 연결 단계에 전달한다.</summary>
    public sealed class BuiltRoom
    {
        public MapRoomPlan Plan { get; }
        public RoomTemplate Template { get; }
        public IReadOnlyDictionary<int, GameObject> Content { get; }
        public IReadOnlyList<GameObject> Monsters { get; }

        public BuiltRoom(MapRoomPlan plan, RoomTemplate template, IReadOnlyDictionary<int, GameObject> content)
            : this(plan, template, content, Array.Empty<GameObject>()) { }

        public BuiltRoom(MapRoomPlan plan, RoomTemplate template, IReadOnlyDictionary<int, GameObject> content,
            IReadOnlyList<GameObject> monsters)
        { Plan = plan; Template = template; Content = content; Monsters = monsters; }
    }

    /// <summary>검증된 계획을 비활성 루트에 생성한다. 기존 맵은 다루지 않는다.</summary>
    public static class MapBuilder
    {
        public static MapBuildResult Build(MapPlan plan, RoomTemplateCatalog catalog,
            GameObject goalPrefab, MapBuildContext context = null, TileBase visualGround = null,
            Material visualGroundMaterial = null)
        {
            if (catalog == null || goalPrefab == null) return MapBuildResult.Failed("카탈로그 또는 Goal 프리팹이 없습니다.");
            if (context == null) context = new MapBuildContext();
            if (context.CreateRoot == null || context.InstantiatePrefab == null || context.DestroyObject == null || context.SetActive == null)
                return MapBuildResult.Failed("맵 생성 컨텍스트가 올바르지 않습니다.");

            if (!catalog.TryCreateSnapshot(out var snapshots, out string catalogError))
                return MapBuildResult.Failed(catalogError);
            var errors = MapPlanValidator.Validate(plan, snapshots);
            if (errors.Count > 0) return MapBuildResult.Failed(string.Join("; ", errors));
            var prefabs = new Dictionary<string, RoomTemplate>(StringComparer.Ordinal);
            foreach (RoomTemplate template in catalog.Templates)
            {
                if (template == null || string.IsNullOrWhiteSpace(template.TemplateId) || prefabs.ContainsKey(template.TemplateId))
                    return MapBuildResult.Failed("카탈로그의 템플릿 또는 ID가 올바르지 않습니다.");
                prefabs.Add(template.TemplateId, template);
            }
            foreach (MapRoomPlan room in plan.Rooms)
                if (!prefabs.ContainsKey(room.TemplateId))
                    return MapBuildResult.Failed($"템플릿을 찾을 수 없습니다: {room.TemplateId}");

            GameObject root = null;
            try
            {
                root = context.CreateRoot($"RoomMap seed={plan.Seed}");
                if (root == null) return MapBuildResult.Failed("맵 루트를 생성하지 못했습니다.");
                root.SetActive(false);
                root.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
                root.transform.localScale = Vector3.one;
                var terrainMaps = new List<MapVisualBuilder.TerrainMap>();
                var builtRooms = new List<BuiltRoom>();
                foreach (MapRoomPlan room in plan.Rooms)
                {
                    GameObject instance = context.InstantiatePrefab(prefabs[room.TemplateId].gameObject, root.transform);
                    if (instance == null) throw new InvalidOperationException($"방 생성 실패: {room.TemplateId}");
                    instance.transform.SetParent(root.transform, false);
                    instance.transform.localPosition = RoomDirection.Origin(room.Cell);
                    instance.transform.localRotation = Quaternion.identity;
                    instance.transform.localScale = Vector3.one;
                    instance.name = $"Room ({room.Cell.x}, {room.Cell.y}) {room.TemplateId}";
                    var template = instance.GetComponent<RoomTemplate>();
                    var roomOrigin = new Vector3Int(room.Cell.x * RoomTemplate.RoomSize, -room.Cell.y * RoomTemplate.RoomSize, 0);
                    RoomTerrainBuilder.Apply(room, template, roomOrigin, terrainMaps, visualGround != null, context);
                    var monsters = new List<GameObject>();
                    var builtContent = RoomContentBuilder.Build(room, template, context, monsters);
                    RoomHazardBuilder.Apply(room, template.Hazard, context);
                    builtRooms.Add(new BuiltRoom(room, template, builtContent, monsters.AsReadOnly()));
                }
                GameObject goal = context.InstantiatePrefab(goalPrefab, root.transform);
                if (goal == null) throw new InvalidOperationException("Goal 생성 실패");
                goal.transform.SetParent(root.transform, false);
                goal.transform.localPosition = plan.Goal;
                goal.transform.localRotation = Quaternion.identity;
                if (visualGround != null) MapVisualBuilder.Build(root, terrainMaps, visualGround, context, visualGroundMaterial);
                return MapBuildResult.Succeeded(root, plan, builtRooms.AsReadOnly());
            }
            catch (Exception exception)
            {
                if (root != null) context.DestroyObject(root);
                return MapBuildResult.Failed($"맵 생성 실패: {exception.Message}");
            }
        }
    }
}
