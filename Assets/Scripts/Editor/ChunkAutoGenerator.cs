using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace MapSystem.Editor
{
    /// <summary>
    /// 청크 4변 x 2지점 = 8개의 고정 연결 지점. 실제 프리팹들의 entrance 좌표를 실측해서 정한 값:
    /// West/East(세로변)는 y=3(낮음)/y=7(높음), North/South(가로변)는 모서리에서 폭의 10% 지점.
    /// entrance1과 entrance2로 같은 지점을 고르면 "구석진 방"(문이 하나뿐인 방)으로 취급된다.
    /// </summary>
    public enum ChunkEntrancePoint
    {
        West1, West2,
        East1, East2,
        North1, North2,
        South1, South2,
    }

    /// <summary>
    /// 청크 하나의 내부 지형·콘텐츠를 자동 생성한다 (chunk_system_spec_v3.md 7장의 파이프라인 구현체).
    ///
    /// 절차:
    /// 1) entrance1/entrance2 사이를 잇는 후보 발판 그래프(레이어드)를 만들고 Dijkstra로 메인 경로를 고른다
    ///    (간선은 항상 ChunkReachability.CanJump로 검사하므로 Player Limit을 구조적으로 위반할 수 없다).
    /// 2) 메인 경로 주변에 보너스 발판(코인/보물/몬스터용)을 무작위로 추가하되, 기존 발판 중 하나와
    ///    CanJump 관계가 성립할 때만 채택한다.
    /// 3) 실제로 구운 Tilemap에서 ExtractSegments를 다시 돌려 entrance1&lt;-&gt;entrance2 양방향 BFS로
    ///    최종 검증하고, 실패하면 스펙 5.3의 수선 전략(중간 발판 추가)을 1회 적용한다.
    /// 4) 6장 규칙에 따라 코인/몬스터/아이템을 배치하고 ChunkData에 기록한다.
    ///
    /// entrance 좌표 규칙(실제 프리팹 실측): West/East/North는 "실제 발판 표면 + 1"(플레이어 스폰
    /// 피벗 기준으로 보임), South(y=0)는 실제 발판이 필요 없는 "낙하 통과" 지점 — 근처 발판에서
    /// CanJump로 도달만 가능하면 되고 타일을 칠하지 않는다(IsVirtualExit/EntranceAnchorSegment 참고).
    /// </summary>
    public static class ChunkAutoGenerator
    {
        public static Vector2Int ResolveEntrancePoint(ChunkEntrancePoint point, int width, int height)
        {
            int nearCorner = Mathf.RoundToInt(width * 0.1f);
            switch (point)
            {
                case ChunkEntrancePoint.West1: return new Vector2Int(0, 3);
                case ChunkEntrancePoint.West2: return new Vector2Int(0, 7);
                case ChunkEntrancePoint.East1: return new Vector2Int(width, 3);
                case ChunkEntrancePoint.East2: return new Vector2Int(width, 7);
                case ChunkEntrancePoint.North1: return new Vector2Int(nearCorner, height);
                case ChunkEntrancePoint.North2: return new Vector2Int(width - nearCorner, height);
                case ChunkEntrancePoint.South1: return new Vector2Int(nearCorner, 0);
                case ChunkEntrancePoint.South2: return new Vector2Int(width - nearCorner, 0);
                default: return Vector2Int.zero;
            }
        }

        public class Settings
        {
            public ChunkType chunkType;
            public ChunkDifficulty difficulty;
            public int width;
            public int height = 10;
            public Vector2Int entrance1;
            public Vector2Int entrance2;
            public int maxJumpX = 6;
            public int maxRiseY = 3;
            public int playerWidth = 1;
            public int playerHeight = 2;
            public TileBase groundTile;
            public int seed;
        }

        private class ChunkContent
        {
            public List<Vector2Int> coins = new List<Vector2Int>();
            public List<Vector2Int> monsters = new List<Vector2Int>();
            public List<Vector2Int> items = new List<Vector2Int>();
            public List<Vector2Int> arrowTraps = new List<Vector2Int>();
            public List<Vector2Int> spikeTraps = new List<Vector2Int>();
        }

        public static GameObject GeneratePreview(Settings s)
        {
            var rng = new System.Random(s.seed);

            string rootName = $"NewChunk_{s.chunkType}_{s.difficulty}_Preview";
            var existing = GameObject.Find(rootName);
            if (existing != null) Object.DestroyImmediate(existing);

            var root = new GameObject(rootName);
            var grid = root.AddComponent<Grid>();
            grid.cellSize = Vector3.one;

            var tilemapGO = new GameObject("GroundTilemap");
            tilemapGO.transform.SetParent(root.transform, false);
            var tilemap = tilemapGO.AddComponent<Tilemap>();
            tilemapGO.AddComponent<TilemapRenderer>();
            tilemapGO.AddComponent<TilemapCollider2D>();

            // entrance1/entrance2로 같은 지점을 고르면 "구석진 방"(문이 하나뿐인 방) — 두 entrance를
            // 잇는 경로 대신, 그 한 지점에서 모든 콘텐츠에 도달 가능하기만 하면 되는 단일 허브로 취급한다.
            bool isCornerRoom = s.entrance1 == s.entrance2;

            List<ChunkSegment> mainPath;
            if (isCornerRoom)
            {
                mainPath = new List<ChunkSegment> { EntranceAnchorSegment(s.entrance1, s) };
            }
            else
            {
                var layers = BuildCandidateLayers(s, rng);
                mainPath = FindMainPathDijkstra(layers, s.maxJumpX, s.maxRiseY, s.playerHeight, rng);
                if (mainPath == null)
                {
                    Debug.LogWarning("[ChunkAutoGenerator] 후보 그래프에서 경로를 찾지 못해 선형 보간 높이로 대체합니다.");
                    mainPath = BuildFallbackLinearPath(layers, s.maxJumpX, s.maxRiseY, s.playerHeight);
                }
            }

            int bonusCount = isCornerRoom ? rng.Next(3, 6) : rng.Next(2, 5); // 구석진 방은 문이 하나뿐이라 콘텐츠용 발판을 좀 더 넉넉히
            var allSegments = GenerateBonusSegments(s, mainPath, rng, bonusCount);

            PaintSegments(tilemap, s.groundTile, allSegments);

            VerifyAndRepair(tilemap, s.groundTile, s, allSegments, isCornerRoom);

            // 콘텐츠는 "계획 단계"의 allSegments가 아니라, 검증/수선까지 끝난 뒤 실제로 구워진 타일에서
            // 다시 뽑은 세그먼트 중 entrance에서 진짜로 도달 가능한 것에만 배치한다 — 그래야 몬스터/코인이
            // 플레이어가 갈 수 없는 발판 위에 놓이는 일이 없다.
            var placeableSegments = ComputeReachableContentSegments(tilemap, s, isCornerRoom);
            var content = PlaceContent(s, placeableSegments, rng);

            var chunkData = root.AddComponent<ChunkData>();
            ApplyChunkData(chunkData, s, content);

            Undo.RegisterCreatedObjectUndo(root, "Generate Chunk Preview");
            Selection.activeGameObject = root;
            return root;
        }

        public static void SaveAsPrefab(GameObject previewRoot, ChunkType type, ChunkDifficulty difficulty, string chunkName)
        {
            string folder = type == ChunkType.Transition
                ? "Assets/Prefabs/Map/ChunkMap/Transitions"
                : $"Assets/Prefabs/Map/ChunkMap/{type}/{difficulty}";

            EnsureFolder(folder);

            string path = $"{folder}/{chunkName}.prefab";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null)
            {
                if (!EditorUtility.DisplayDialog("덮어쓰기 확인", $"{path} 가 이미 존재합니다. 덮어쓸까요?", "덮어쓰기", "취소"))
                    return;
            }

            PrefabUtility.SaveAsPrefabAsset(previewRoot, path, out bool success);
            if (!success)
            {
                Debug.LogError($"[ChunkAutoGenerator] 프리팹 저장 실패: {path}");
                return;
            }

            Object.DestroyImmediate(previewRoot);
            ChunkDatabaseBuilder.Rebuild();
            Debug.Log($"[ChunkAutoGenerator] 저장 완료: {path}");
        }

        private static void EnsureFolder(string folder)
        {
            string[] parts = folder.Split('/');
            string cur = parts[0]; // "Assets"
            for (int i = 1; i < parts.Length; i++)
            {
                string next = cur + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(cur, parts[i]);
                cur = next;
            }
        }

        // ---- 1) 후보 그래프 + Dijkstra 메인 경로 ----

        private static List<List<ChunkSegment>> BuildCandidateLayers(Settings s, System.Random rng)
        {
            int x0 = s.entrance1.x;
            int x1 = s.entrance2.x;
            int y0 = SurfaceHeight(s.entrance1);
            int y1 = SurfaceHeight(s.entrance2);
            int span = Mathf.Abs(x1 - x0);
            int heightDelta = Mathf.Abs(y1 - y0);

            int slotCountByX = Mathf.Max(1, Mathf.RoundToInt((float)span / Mathf.Max(1, s.maxJumpX)));
            int slotCountByHeight = Mathf.CeilToInt((float)heightDelta / Mathf.Max(1, s.maxRiseY));
            int slotCount = Mathf.Max(1, Mathf.Max(slotCountByX, slotCountByHeight));

            var layers = new List<List<ChunkSegment>>();

            for (int i = 0; i <= slotCount; i++)
            {
                var layer = new List<ChunkSegment>();
                bool isStart = i == 0;
                bool isEnd = i == slotCount;

                if (isStart)
                {
                    layer.Add(EntranceAnchorSegment(s.entrance1, s));
                }
                else if (isEnd)
                {
                    layer.Add(EntranceAnchorSegment(s.entrance2, s));
                }
                else
                {
                    float t = (float)i / slotCount;
                    int slotX = Mathf.RoundToInt(Mathf.Lerp(x0, x1, t));
                    int lerpHeight = Mathf.Clamp(Mathf.RoundToInt(Mathf.Lerp(y0, y1, t)), 1, s.height - 2);

                    // index 0은 항상 순수 선형보간 높이 -> 실패 시 폴백에서 이것만 골라도 항상 유효함이 보장됨
                    var heightCandidates = new List<int> { lerpHeight };
                    for (int c = 0; c < 2; c++)
                    {
                        int h = Mathf.Clamp(lerpHeight + rng.Next(-2, 3), 1, s.height - 2);
                        if (!heightCandidates.Contains(h)) heightCandidates.Add(h);
                    }

                    int width = rng.Next(3, 7);
                    foreach (int h in heightCandidates)
                        layer.Add(MakeSegmentAround(slotX, h, width, s));
                }

                layers.Add(layer);
            }

            return layers;
        }

        // centerX 기준으로 폭 width인 세그먼트를 만들되, 청크 경계에 걸리면 "잘라내는" 대신 안쪽으로
        // "밀어서" 원래 폭을 최대한 유지한다. entrance2처럼 centerX가 오른쪽 끝(=width)에 붙어 있을 때
        // 예전에는 xStart만 안쪽으로 유지되고 xEnd가 잘려서 실제로는 1칸짜리 발판이 되어버렸다 —
        // 그러면 CanJump 상으로는 유효해도 플레이어가 착지할 여유가 없는 비현실적인 발판이 된다.
        private static ChunkSegment MakeSegmentAround(int centerX, int heightY, int width, Settings s)
        {
            width = Mathf.Min(width, s.width);
            int half = width / 2;
            int xStart = centerX - half;
            int xEnd = xStart + width - 1;

            if (xStart < 0)
            {
                xEnd -= xStart;
                xStart = 0;
            }
            if (xEnd > s.width - 1)
            {
                int shift = xEnd - (s.width - 1);
                xStart -= shift;
                xEnd -= shift;
            }
            xStart = Mathf.Max(xStart, 0);

            return new ChunkSegment { xStart = xStart, xEnd = xEnd, height = heightY };
        }

        // South(y=0)는 실측 결과 실제 발판 없이 "떨어져서 통과하는" 지점이었다(GoUp/GoDown 프리팹 참고).
        // North/West/East는 반대로 실제 발판이 있어야 하는 지점이었다.
        private static bool IsVirtualExit(Vector2Int entrance) => entrance.y == 0;

        // West/East/North의 entrance.y는 실제 발판 표면(top)이 아니라 "표면 + 1"로 기록되어 있었다
        // (실측: 프리팹들 전부 정확히 +1 차이).
        //
        // South(virtual)도 같은 -1 보정을 그대로 적용한다 — 그렇지 않으면 아래 청크의 North(실제 발판,
        // entrance.y-1)와 위 청크의 South(가상, entrance.y 그대로)가 이음매에서 비대칭이 되어, 두 청크를
        // 이어붙였을 때 실제 필요한 점프 높이가 이론상 maxRiseY보다 최대 1칸 더 커질 수 있었다
        // (아래쪽 실제 발판→이음매 1칸 + 이음매→위쪽 실제 발판 최대 maxRiseY칸 = 최대 maxRiseY+1칸).
        // South의 "발판 없는 앵커"에도 -1을 적용하면 이음매 건너 실제 발판 사이의 거리가 항상 maxRiseY
        // 이내로 정확히 맞아떨어진다.
        private static int SurfaceHeight(Vector2Int entrance) => entrance.y - 1;

        // entrance 지점에 실제로 깔릴(혹은 깔리지 않을) 발판 세그먼트를 만든다.
        private static ChunkSegment EntranceAnchorSegment(Vector2Int entrance, Settings s)
        {
            if (IsVirtualExit(entrance))
            {
                return new ChunkSegment
                {
                    xStart = Mathf.Max(0, entrance.x - 1),
                    xEnd = Mathf.Min(s.width - 1, entrance.x + 1),
                    height = SurfaceHeight(entrance),
                    isVirtual = true,
                };
            }

            return MakeSegmentAround(entrance.x, SurfaceHeight(entrance), 3, s);
        }

        // 실제로 구운 Tilemap에서 뽑은 세그먼트 목록(segs) 안에서 entrance가 위치한 세그먼트의 인덱스를 찾는다.
        // South처럼 실제 타일이 없는 entrance는 검증용 가상 세그먼트를 segs 끝에 추가하고 그 인덱스를 반환한다.
        private static int ResolveEntranceIndex(List<ChunkSegment> segs, Vector2Int entrance, Settings s)
        {
            if (IsVirtualExit(entrance))
            {
                segs.Add(new ChunkSegment
                {
                    xStart = Mathf.Max(0, entrance.x - 1),
                    xEnd = Mathf.Min(s.width - 1, entrance.x + 1),
                    height = SurfaceHeight(entrance),
                    isVirtual = true,
                });
                return segs.Count - 1;
            }

            return ChunkReachability.FindSegmentAt(segs, new Vector2Int(entrance.x, SurfaceHeight(entrance)));
        }

        private static List<ChunkSegment> FindMainPathDijkstra(List<List<ChunkSegment>> layers, int maxJumpX, int maxRiseY, int playerHeight, System.Random rng)
        {
            var flatNodes = new List<ChunkSegment>();
            var layerStart = new int[layers.Count];
            for (int l = 0; l < layers.Count; l++)
            {
                layerStart[l] = flatNodes.Count;
                flatNodes.AddRange(layers[l]);
            }

            int n = flatNodes.Count;
            var nodeLayer = new int[n];
            for (int l = 0; l < layers.Count; l++)
                for (int k = 0; k < layers[l].Count; k++)
                    nodeLayer[layerStart[l] + k] = l;

            var dist = new double[n];
            var prev = new int[n];
            for (int i = 0; i < n; i++) { dist[i] = double.MaxValue; prev[i] = -1; }

            int source = layerStart[0];
            dist[source] = 0;
            var visited = new bool[n];

            for (int iter = 0; iter < n; iter++)
            {
                int u = -1;
                double best = double.MaxValue;
                for (int i = 0; i < n; i++)
                    if (!visited[i] && dist[i] < best) { best = dist[i]; u = i; }
                if (u == -1) break;
                visited[u] = true;

                int uLayer = nodeLayer[u];
                if (uLayer + 1 >= layers.Count) continue;

                int vStart = layerStart[uLayer + 1];
                int vCount = layers[uLayer + 1].Count;
                for (int k = 0; k < vCount; k++)
                {
                    int v = vStart + k;
                    if (!ChunkReachability.CanJump(flatNodes[u], flatNodes[v], maxJumpX, maxRiseY)) continue;
                    if (!ChunkReachability.HasClearance(flatNodes[u], flatNodes[v], playerHeight)) continue;

                    double weight = 1.0 + rng.NextDouble() * 2.0; // 시드마다 다른 경로가 나오도록 무작위 가중치
                    double alt = dist[u] + weight;
                    if (alt < dist[v]) { dist[v] = alt; prev[v] = u; }
                }
            }

            int target = layerStart[layers.Count - 1];
            if (dist[target] >= double.MaxValue) return null;

            var pathIdx = new List<int>();
            for (int cur = target; cur != -1; cur = prev[cur]) pathIdx.Add(cur);
            pathIdx.Reverse();

            var result = new List<ChunkSegment>();
            foreach (int idx in pathIdx) result.Add(flatNodes[idx]);
            return result;
        }

        private static List<ChunkSegment> BuildFallbackLinearPath(List<List<ChunkSegment>> layers, int maxJumpX, int maxRiseY, int playerHeight)
        {
            var result = new List<ChunkSegment>();
            ChunkSegment? prev = null;

            foreach (var layer in layers)
            {
                ChunkSegment chosen = layer[0]; // 기본값: 순수 선형보간 후보
                if (prev.HasValue)
                {
                    foreach (var candidate in layer)
                    {
                        if (ChunkReachability.CanJump(prev.Value, candidate, maxJumpX, maxRiseY) &&
                            ChunkReachability.HasClearance(prev.Value, candidate, playerHeight))
                        {
                            chosen = candidate;
                            break;
                        }
                    }
                }
                result.Add(chosen);
                prev = chosen;
            }

            return result;
        }

        // ---- 2) 보너스 발판 ----

        private static List<ChunkSegment> GenerateBonusSegments(Settings s, List<ChunkSegment> mainPath, System.Random rng, int count)
        {
            var result = new List<ChunkSegment>(mainPath);
            int added = 0;
            int attempts = 0;

            while (added < count && attempts < count * 20)
            {
                attempts++;
                int width = rng.Next(2, 5);
                int cx = rng.Next(0, s.width);
                int h = rng.Next(1, s.height - 1);
                var candidate = MakeSegmentAround(cx, h, width, s);

                if (IsBlocked(result, candidate, s.playerHeight)) continue;

                bool reachable = false;
                foreach (var existing in result)
                {
                    if (ChunkReachability.CanJump(existing, candidate, s.maxJumpX, s.maxRiseY) ||
                        ChunkReachability.CanJump(candidate, existing, s.maxJumpX, s.maxRiseY))
                    {
                        reachable = true;
                        break;
                    }
                }
                if (!reachable) continue;

                result.Add(candidate);
                added++;
            }

            return result;
        }

        private static bool IsBlocked(List<ChunkSegment> existingSegments, ChunkSegment candidate, int playerHeight)
        {
            foreach (var existing in existingSegments)
            {
                if (!XRangesOverlap(existing, candidate)) continue;

                if (existing.height == candidate.height && ChunkReachability.SegGap(existing, candidate) < 2)
                    return true; // 6장: 발판 간격 최소 2타일

                if (!ChunkReachability.HasClearance(existing, candidate, playerHeight))
                    return true; // 플레이어 세로 크기만큼 여유 없이 겹치면 끼임
            }
            return false;
        }

        private static bool XRangesOverlap(ChunkSegment a, ChunkSegment b)
        {
            return a.xStart <= b.xEnd && b.xStart <= a.xEnd;
        }

        private static void PaintSegments(Tilemap tilemap, TileBase tile, IEnumerable<ChunkSegment> segments)
        {
            foreach (var seg in segments)
            {
                if (seg.isVirtual) continue; // South 같은 낙하 통과 지점은 실제 타일을 칠하지 않는다
                for (int x = seg.xStart; x <= seg.xEnd; x++)
                    tilemap.SetTile(new Vector3Int(x, seg.height - 1, 0), tile);
            }
        }

        private static HashSet<Vector2Int> ReadTilesFromTilemap(Tilemap tilemap)
        {
            var result = new HashSet<Vector2Int>();
            tilemap.CompressBounds();
            BoundsInt bounds = tilemap.cellBounds;
            foreach (var pos in bounds.allPositionsWithin)
                if (tilemap.HasTile(pos))
                    result.Add(new Vector2Int(pos.x, pos.y));
            return result;
        }

        // ---- 3) 최종 검증 + 수선 (스펙 5장) ----

        private static void VerifyAndRepair(Tilemap tilemap, TileBase groundTile, Settings s, List<ChunkSegment> allSegments, bool isCornerRoom)
        {
            var tiles = ReadTilesFromTilemap(tilemap);
            var segs = ChunkReachability.ExtractSegments(tiles);

            int idx1 = ResolveEntranceIndex(segs, s.entrance1, s);
            int idx2 = isCornerRoom ? idx1 : ResolveEntranceIndex(segs, s.entrance2, s);

            if (idx1 < 0 || idx2 < 0)
            {
                Debug.LogWarning("[ChunkAutoGenerator] entrance가 어떤 세그먼트 위에도 있지 않습니다. 수동 확인이 필요합니다.");
                return;
            }

            if (CheckReachability(segs, idx1, idx2, isCornerRoom, s, out var reach1))
            {
                Debug.Log(isCornerRoom
                    ? "[ChunkAutoGenerator] 검증 통과: 단일 entrance에서 모든 발판에 도달 가능 (구석진 방)."
                    : "[ChunkAutoGenerator] 검증 통과: entrance1 <-> entrance2 양방향 도달 가능.");
                return;
            }

            Debug.LogWarning("[ChunkAutoGenerator] 도달 불가 감지. 중간 발판 추가를 시도합니다.");

            if (!TryFindBridge(segs, reach1, out ChunkSegment a, out ChunkSegment b))
            {
                Debug.LogWarning("[ChunkAutoGenerator] 수선할 지점을 찾지 못했습니다. 수동으로 확인해주세요.");
                return;
            }

            int midHeight = Mathf.Clamp((a.height + b.height) / 2,
                Mathf.Max(a.height, b.height) - s.maxRiseY,
                Mathf.Min(a.height, b.height) + s.maxRiseY);
            midHeight = Mathf.Clamp(midHeight, 1, s.height - 2);

            int bridgeXStart, bridgeXEnd;
            if (a.xEnd < b.xStart) { bridgeXStart = a.xEnd + 1; bridgeXEnd = b.xStart - 1; }
            else if (b.xEnd < a.xStart) { bridgeXStart = b.xEnd + 1; bridgeXEnd = a.xStart - 1; }
            else { bridgeXStart = Mathf.Max(a.xStart, b.xStart); bridgeXEnd = Mathf.Min(a.xEnd, b.xEnd); }
            if (bridgeXEnd < bridgeXStart) bridgeXEnd = bridgeXStart;
            bridgeXStart = Mathf.Clamp(bridgeXStart, 0, s.width - 1);
            bridgeXEnd = Mathf.Clamp(bridgeXEnd, 0, s.width - 1);

            var bridge = new ChunkSegment { xStart = bridgeXStart, xEnd = bridgeXEnd, height = midHeight };
            PaintSegments(tilemap, groundTile, new[] { bridge });
            allSegments.Add(bridge);

            tiles = ReadTilesFromTilemap(tilemap);
            segs = ChunkReachability.ExtractSegments(tiles);
            idx1 = ResolveEntranceIndex(segs, s.entrance1, s);
            idx2 = isCornerRoom ? idx1 : ResolveEntranceIndex(segs, s.entrance2, s);

            if (CheckReachability(segs, idx1, idx2, isCornerRoom, s, out _))
                Debug.Log("[ChunkAutoGenerator] 중간 발판 추가로 검증 통과.");
            else
                Debug.LogWarning("[ChunkAutoGenerator] 자동 수선 실패 — 프리뷰를 열어 수동으로 조정해주세요.");
        }

        // isCornerRoom이면 "entrance1에서 모든 발판에 도달 가능한가", 아니면 "entrance1<->entrance2 양방향 도달 가능한가".
        private static bool CheckReachability(List<ChunkSegment> segs, int idx1, int idx2, bool isCornerRoom, Settings s, out HashSet<int> reach1)
        {
            reach1 = ChunkReachability.ReachableFrom(segs, idx1, s.maxJumpX, s.maxRiseY, s.playerHeight);
            if (isCornerRoom)
                return reach1.Count == segs.Count;

            var reach2 = ChunkReachability.ReachableFrom(segs, idx2, s.maxJumpX, s.maxRiseY, s.playerHeight);
            return reach1.Contains(idx2) && reach2.Contains(idx1);
        }

        private static bool TryFindBridge(List<ChunkSegment> segs, HashSet<int> reachableFromEntrance1, out ChunkSegment a, out ChunkSegment b)
        {
            int bestGap = int.MaxValue;
            a = default; b = default;
            bool found = false;

            foreach (int ai in reachableFromEntrance1)
            {
                for (int bi = 0; bi < segs.Count; bi++)
                {
                    if (reachableFromEntrance1.Contains(bi)) continue;
                    int gap = ChunkReachability.SegGap(segs[ai], segs[bi]);
                    if (gap < bestGap)
                    {
                        bestGap = gap;
                        a = segs[ai];
                        b = segs[bi];
                        found = true;
                    }
                }
            }

            return found;
        }

        // ---- 4) 콘텐츠 배치 (6장) ----

        // 검증/수선까지 끝난 실제 Tilemap을 다시 읽어서 entrance(들)에서 실제로 도달 가능한 세그먼트만
        // 골라낸다. entrance 자기 자신의 발판과, South처럼 타일이 없는 가상 세그먼트는 제외한다.
        private static List<ChunkSegment> ComputeReachableContentSegments(Tilemap tilemap, Settings s, bool isCornerRoom)
        {
            var result = new List<ChunkSegment>();

            var tiles = ReadTilesFromTilemap(tilemap);
            var segs = ChunkReachability.ExtractSegments(tiles);

            int idx1 = ResolveEntranceIndex(segs, s.entrance1, s);
            int idx2 = isCornerRoom ? idx1 : ResolveEntranceIndex(segs, s.entrance2, s);
            if (idx1 < 0) return result; // VerifyAndRepair가 이미 경고를 남겼다

            var reachable = new HashSet<int>(ChunkReachability.ReachableFrom(segs, idx1, s.maxJumpX, s.maxRiseY, s.playerHeight));
            if (!isCornerRoom && idx2 >= 0)
                reachable.UnionWith(ChunkReachability.ReachableFrom(segs, idx2, s.maxJumpX, s.maxRiseY, s.playerHeight));

            for (int i = 0; i < segs.Count; i++)
            {
                if (i == idx1 || i == idx2) continue; // entrance 발판 자체는 배치 대상에서 제외
                if (segs[i].isVirtual) continue; // 타일이 없는 가상 세그먼트에는 아무것도 못 놓는다
                if (!reachable.Contains(i)) continue; // 도달 불가능한 발판에는 배치 금지 — 핵심 수정
                result.Add(segs[i]);
            }

            return result;
        }

        private static ChunkContent PlaceContent(Settings s, List<ChunkSegment> placeable, System.Random rng)
        {
            var content = new ChunkContent();
            if (s.chunkType == ChunkType.Transition) return content;
            if (placeable.Count == 0) return content; // 도달 가능한 발판이 없으면 콘텐츠도 배치하지 않는다

            if (s.chunkType == ChunkType.Treasure)
            {
                PlaceOnRandomSegments(content.coins, placeable, rng.Next(0, 3), rng, margin: 0);
                PlaceOnRandomSegments(content.items, placeable, 1, rng, margin: 0);
            }
            else
            {
                PlaceOnRandomSegments(content.coins, placeable, rng.Next(0, 4), rng, margin: 0);

                if (s.chunkType == ChunkType.Combat)
                {
                    int monsterCount = s.difficulty switch
                    {
                        ChunkDifficulty.Easy => 1,
                        ChunkDifficulty.Medium => rng.Next(1, 4),
                        ChunkDifficulty.Hard => rng.Next(1, 6),
                        _ => 1,
                    };
                    PlaceOnRandomSegments(content.monsters, placeable, monsterCount, rng, margin: 1);
                }
            }

            return content;
        }

        private static void PlaceOnRandomSegments(List<Vector2Int> target, List<ChunkSegment> segments, int count, System.Random rng, int margin)
        {
            for (int i = 0; i < count; i++)
            {
                var seg = segments[rng.Next(segments.Count)];
                int usableStart = seg.xStart + margin;
                int usableEnd = seg.xEnd - margin;
                if (usableEnd < usableStart) { usableStart = seg.xStart; usableEnd = seg.xEnd; }
                int x = rng.Next(usableStart, usableEnd + 1);
                target.Add(new Vector2Int(x, seg.height));
            }
        }

        // ---- ChunkData 기록 ----

        private static void ApplyChunkData(ChunkData chunkData, Settings s, ChunkContent content)
        {
            var so = new SerializedObject(chunkData);
            so.FindProperty("chunkType").enumValueIndex = (int)s.chunkType;
            so.FindProperty("entrance1").vector2IntValue = s.entrance1;
            so.FindProperty("entrance2").vector2IntValue = s.entrance2;
            SetVector2IntList(so.FindProperty("coins"), content.coins);
            SetVector2IntList(so.FindProperty("monsters"), content.monsters);
            SetVector2IntList(so.FindProperty("items"), content.items);
            SetVector2IntList(so.FindProperty("arrowTraps"), content.arrowTraps);
            SetVector2IntList(so.FindProperty("spikeTraps"), content.spikeTraps);
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetVector2IntList(SerializedProperty listProp, List<Vector2Int> values)
        {
            listProp.ClearArray();
            for (int i = 0; i < values.Count; i++)
            {
                listProp.InsertArrayElementAtIndex(i);
                listProp.GetArrayElementAtIndex(i).vector2IntValue = values[i];
            }
        }
    }
}
