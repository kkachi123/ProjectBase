using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace MapSystem.Editor
{
    /// <summary>
    /// Assets/Prefabs/Map/ChunkMap/ 아래 프리팹을 전부 스캔해서 ChunkDatabase를 자동으로
    /// 재생성한다. 난이도는 ChunkData에 필드를 추가하는 대신, 프리팹 경로의 폴더명
    /// (Easy/Medium/Hard)에서 추론한다. entrances 등은 ChunkData의 공개 프로퍼티를
    /// 통해 그대로 읽어와 캐싱한다.
    /// </summary>
    public static class ChunkDatabaseBuilder
    {
        private const string ScanRoot = "Assets/Prefabs/Map/ChunkMap";
        private const string DatabasePath = "Assets/Prefabs/@Data/Map/ChunkDatabase.asset";

        [MenuItem("Map/Rebuild Chunk Database")]
        // Chunk Prefab을 전부 스캔해서 ChunkDatabase를 재생성한다. (ChunkData가 없는 프리팹은 스킵)
        public static void Rebuild()
        {
            ChunkDatabase db = AssetDatabase.LoadAssetAtPath<ChunkDatabase>(DatabasePath);
            if (db == null)
            {
                string dir = "Assets/Prefabs/@Data/Map";
                if (!AssetDatabase.IsValidFolder(dir))
                    AssetDatabase.CreateFolder("Assets/Prefabs/@Data/Map", "ChunkDatabase.asset");
                db = ScriptableObject.CreateInstance<ChunkDatabase>();
                AssetDatabase.CreateAsset(db, DatabasePath);
            }

            db.entries.Clear();
            // 지정된 경로 아래의 모든 프리팹을 스캔해서 ChunkData를 가진 것만 등록한다.
            // 1. Guid 검색
            string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { ScanRoot });

            int found = 0; // ChunkData가 있는 프리팹 수
            int skipped = 0; // ChunkData가 없는 프리팹 수

            for (int i = 0; i < guids.Length; i++)
            {
                // 2. Guid -> 파일 경로 -> 프리팹 로드
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null) { skipped++; continue; }
                // 3. ChunkData 컴포넌트 확인
                ChunkData cd = prefab.GetComponent<ChunkData>();
                if (cd == null) { skipped++; continue; }

                ChunkType type = cd.ChunkType;
                List<Vector2Int> entrances = cd.GetAllEntrances();
                EndLineRole role = cd.EndLineRole;
                
                // 난이도 추론: 경로에 Easy/Medium/Hard가 포함되어 있으면 해당 난이도로 설정, 없으면 None
                ChunkDifficulty diff = ChunkDifficulty.None;
                if (path.Contains("/Easy/")) diff = ChunkDifficulty.Easy;
                else if (path.Contains("/Medium/")) diff = ChunkDifficulty.Medium;
                else if (path.Contains("/Hard/")) diff = ChunkDifficulty.Hard;

                int width = (type == ChunkType.Transition || type == ChunkType.EndLine) ? 10 : 20;
                int height = 10;

                ChunkDatabaseEntry entry = new ChunkDatabaseEntry
                {
                    prefab = prefab,
                    prefabName = prefab.name,
                    type = type,
                    difficulty = diff,
                    entrances = entrances,
                    endLineRole = role,
                    width = width,
                    height = height,
                };
                db.entries.Add(entry);
                found++;
            }

            EditorUtility.SetDirty(db);
            AssetDatabase.SaveAssets();
            Debug.Log($"[ChunkDatabaseBuilder] Rebuilt: {found} chunk(s) registered, {skipped} prefab(s) skipped (no ChunkData). Saved to {DatabasePath}");
        }
    }
}
