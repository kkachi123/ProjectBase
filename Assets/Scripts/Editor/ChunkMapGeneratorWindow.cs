using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace MapSystem.Editor
{
    /// <summary>
    /// ChunkDatabase를 이용해 청크를 순서대로 이어붙여 랜덤 맵을 생성한다.
    /// 매 스텝마다 (1) 소켓 호환성(진입 높이 일치) 필터링 -> (2) 겹침 검사 -> (3) 배치
    /// 순서로 진행한다. Transition/Content 타입을 번갈아 배치하며, 이름이 "6_" 또는 "7_"로
    /// 시작하는 통로를 통과하면 진행 방향(Right/Left) 플래그가 뒤집힌다.
    /// </summary>
    public class ChunkMapGeneratorWindow : EditorWindow
    {
        private enum Direction { Right, Left }

        private const string RootName = "GeneratedMap";

        private ChunkDatabase database;
        private int stepCount = 10;
        private int seed = 0;
        private bool useRandomSeed = true;

        [MenuItem("Map/Generate Random Map")]
        public static void Open()
        {
            GetWindow<ChunkMapGeneratorWindow>("Chunk Map Generator");
        }

        private void OnEnable()
        {
            if (database == null)
                database = AssetDatabase.LoadAssetAtPath<ChunkDatabase>("Assets/Data/ChunkDatabase.asset");
        }

        private void OnGUI()
        {
            database = (ChunkDatabase)EditorGUILayout.ObjectField("Chunk Database", database, typeof(ChunkDatabase), false);
            stepCount = EditorGUILayout.IntField("Step Count (Transition+Content 합)", stepCount);
            useRandomSeed = EditorGUILayout.Toggle("Random Seed", useRandomSeed);
            if (!useRandomSeed)
                seed = EditorGUILayout.IntField("Seed", seed);

            EditorGUILayout.Space();
            if (GUILayout.Button("Generate Map"))
            {
                int actualSeed = useRandomSeed ? System.Environment.TickCount : seed;
                Generate(database, stepCount, actualSeed);
            }
            if (GUILayout.Button("Clear Generated Map"))
            {
                GameObject existingRoot = GameObject.Find(RootName);
                if (existingRoot != null) Object.DestroyImmediate(existingRoot);
            }
        }

        private static void Generate(ChunkDatabase db, int steps, int seed)
        {
            if (db == null) { Debug.LogError("[ChunkMapGenerator] ChunkDatabase가 없습니다."); return; }

            GameObject oldRoot = GameObject.Find(RootName);
            if (oldRoot != null) Object.DestroyImmediate(oldRoot);
            GameObject root = new GameObject(RootName);

            System.Random rng = new System.Random(seed);
            List<Rect> placedBoxes = new List<Rect>();

            Vector2 cursor = new Vector2(0f, 3f); // 기본 진입 높이(y=3, ChunkData entrance1 기본값과 동일)
            Direction dir = Direction.Right;
            int placedCount = 0;
            int failCount = 0;

            for (int step = 0; step < steps; step++)
            {
                bool wantTransition = (step % 2 == 0);
                ChunkType desiredType = wantTransition ? ChunkType.Transition : RandomContentType(rng);

                List<ChunkDatabaseEntry> candidates = new List<ChunkDatabaseEntry>(db.GetByTypeAndDifficulty(
                    desiredType, desiredType == ChunkType.Transition ? ChunkDifficulty.None : ChunkDifficulty.Easy));

                // 소켓 호환성 필터: 이 스텝에서 "진입 소켓"으로 쓸 쪽(direction에 따라 entrance1 또는 entrance2)의
                // 높이가 현재 요구 높이(cursor.y)와 같아야 한다.
                candidates.RemoveAll(e =>
                {
                    Vector2Int inSocket = (dir == Direction.Right) ? e.entrance1 : e.entrance2;
                    return inSocket.y != Mathf.RoundToInt(cursor.y);
                });

                Shuffle(candidates, rng);

                bool placed = false;
                foreach (ChunkDatabaseEntry entry in candidates)
                {
                    Vector2Int inSocket = (dir == Direction.Right) ? entry.entrance1 : entry.entrance2;
                    Vector2Int outSocket = (dir == Direction.Right) ? entry.entrance2 : entry.entrance1;

                    Vector2 chunkOrigin = cursor - new Vector2(inSocket.x, inSocket.y);
                    Rect newBox = new Rect(chunkOrigin.x, chunkOrigin.y, entry.width, entry.height);

                    if (Overlaps(newBox, placedBoxes)) continue;

                    GameObject inst = (GameObject)PrefabUtility.InstantiatePrefab(entry.prefab, root.transform);
                    inst.transform.position = new Vector3(chunkOrigin.x, chunkOrigin.y, 0f);
                    inst.name = entry.prefabName + "_" + step;

                    placedBoxes.Add(newBox);
                    cursor = chunkOrigin + new Vector2(outSocket.x, outSocket.y);

                    if (entry.prefabName.StartsWith("6_") || entry.prefabName.StartsWith("7_"))
                        dir = (dir == Direction.Right) ? Direction.Left : Direction.Right;

                    placed = true;
                    placedCount++;
                    break;
                }

                if (!placed)
                {
                    failCount++;
                    Debug.LogWarning($"[ChunkMapGenerator] step {step}: 배치 가능한 후보 없음 (요구 타입={desiredType}, 요구 높이={cursor.y}, 방향={dir}). 스킵.");
                }
            }

            Debug.Log($"[ChunkMapGenerator] 생성 완료: {placedCount}개 배치, {failCount}개 실패(스킵). seed={seed}");
        }

        private static ChunkType RandomContentType(System.Random rng)
        {
            ChunkType[] options = { ChunkType.Combat, ChunkType.Puzzle, ChunkType.Treasure };
            return options[rng.Next(options.Length)];
        }

        private static bool Overlaps(Rect box, List<Rect> others)
        {
            foreach (Rect o in others)
                if (box.Overlaps(o)) return true;
            return false;
        }

        private static void Shuffle<T>(List<T> list, System.Random rng)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }
    }
}
