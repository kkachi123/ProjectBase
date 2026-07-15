using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace MapSystem.Editor
{
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
    /// </summary>
    public static class ChunkAutoGenerator
    {
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

            var layers = BuildCandidateLayers(s, rng);
            var mainPath = FindMainPathDijkstra(layers, s.maxJumpX, s.maxRiseY, rng);
            if (mainPath == null)
            {
                Debug.LogWarning("[ChunkAutoGenerator] 후보 그래프에서 경로를 찾지 못해 선형 보간 높이로 대체합니다.");
                mainPath = BuildFallbackLinearPath(layers);
            }

            int bonusCount = rng.Next(2, 5);
            var allSegments = GenerateBonusSegments(s, mainPath, rng, bonusCount);

            PaintSegments(tilemap, s.groundTile, allSegments);

            VerifyAndRepair(tilemap, s.groundTile, s, allSegments);

            var content = PlaceContent(s, allSegments, mainPath.Count, rng);

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
            int span = Mathf.Abs(x1 - x0);
            int heightDelta = Mathf.Abs(s.entrance2.y - s.entrance1.y);

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
                    layer.Add(MakeSegmentAround(s.entrance1.x, s.entrance1.y, 3, s));
                }
                else if (isEnd)
                {
                    layer.Add(MakeSegmentAround(s.entrance2.x, s.entrance2.y, 3, s));
                }
                else
                {
                    float t = (float)i / slotCount;
                    int slotX = Mathf.RoundToInt(Mathf.Lerp(x0, x1, t));
                    int lerpHeight = Mathf.Clamp(Mathf.RoundToInt(Mathf.Lerp(s.entrance1.y, s.entrance2.y, t)), 1, s.height - 2);

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

        private static List<ChunkSegment> FindMainPathDijkstra(List<List<ChunkSegment>> layers, int maxJumpX, int maxRiseY, System.Random rng)
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

        private static List<ChunkSegment> BuildFallbackLinearPath(List<List<ChunkSegment>> layers)
        {
            var result = new List<ChunkSegment>();
            foreach (var layer in layers)
                result.Add(layer[0]); // 각 레이어의 0번째는 항상 순수 선형보간 후보
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

                if (Mathf.Abs(existing.height - candidate.height) < playerHeight)
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
                for (int x = seg.xStart; x <= seg.xEnd; x++)
                    tilemap.SetTile(new Vector3Int(x, seg.height - 1, 0), tile);
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

        private static void VerifyAndRepair(Tilemap tilemap, TileBase groundTile, Settings s, List<ChunkSegment> allSegments)
        {
            var tiles = ReadTilesFromTilemap(tilemap);
            var segs = ChunkReachability.ExtractSegments(tiles);

            int idx1 = ChunkReachability.FindSegmentAt(segs, s.entrance1);
            int idx2 = ChunkReachability.FindSegmentAt(segs, s.entrance2);

            if (idx1 < 0 || idx2 < 0)
            {
                Debug.LogWarning("[ChunkAutoGenerator] entrance가 어떤 세그먼트 위에도 있지 않습니다. 수동 확인이 필요합니다.");
                return;
            }

            var reach1 = ChunkReachability.ReachableFrom(segs, idx1, s.maxJumpX, s.maxRiseY);
            var reach2 = ChunkReachability.ReachableFrom(segs, idx2, s.maxJumpX, s.maxRiseY);

            bool forwardOk = reach1.Contains(idx2);
            bool backwardOk = reach2.Contains(idx1);

            if (forwardOk && backwardOk)
            {
                Debug.Log("[ChunkAutoGenerator] 검증 통과: entrance1 <-> entrance2 양방향 도달 가능.");
                return;
            }

            Debug.LogWarning($"[ChunkAutoGenerator] 도달 불가 감지(forward={forwardOk}, backward={backwardOk}). 중간 발판 추가를 시도합니다.");

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
            idx1 = ChunkReachability.FindSegmentAt(segs, s.entrance1);
            idx2 = ChunkReachability.FindSegmentAt(segs, s.entrance2);
            reach1 = ChunkReachability.ReachableFrom(segs, idx1, s.maxJumpX, s.maxRiseY);
            reach2 = ChunkReachability.ReachableFrom(segs, idx2, s.maxJumpX, s.maxRiseY);

            if (reach1.Contains(idx2) && reach2.Contains(idx1))
                Debug.Log("[ChunkAutoGenerator] 중간 발판 추가로 검증 통과.");
            else
                Debug.LogWarning("[ChunkAutoGenerator] 자동 수선 실패 — 프리뷰를 열어 수동으로 조정해주세요.");
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

        private static ChunkContent PlaceContent(Settings s, List<ChunkSegment> segments, int mainPathCount, System.Random rng)
        {
            var content = new ChunkContent();
            if (s.chunkType == ChunkType.Transition) return content;

            // segments[0] = entrance1 세그먼트, segments[mainPathCount-1] = entrance2 세그먼트.
            // 그 사이의 메인 경로 중간 발판들 + 보너스 발판들만 콘텐츠 배치 대상으로 삼는다.
            var placeable = new List<ChunkSegment>();
            for (int i = 0; i < segments.Count; i++)
            {
                if (i == 0 || i == mainPathCount - 1) continue;
                placeable.Add(segments[i]);
            }
            if (placeable.Count == 0) placeable = segments;

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
