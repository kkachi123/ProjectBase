using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace GridMapSystem.Editor
{
    /// <summary>
    /// Assets/Prefabs/Map/ChunkMap/ 아래 프리팹을 전부 스캔해서 GridChunkDatabase를 자동으로
    /// 재생성한다. GridChunkData 컴포넌트가 없는 프리팹은 건너뛴다(기존 방식 1 전용 ChunkData만
    /// 있는 프리팹도 여기 해당). 난이도는 ChunkDatabaseBuilder와 동일하게 프리팹 경로의 폴더명
    /// (Easy/Medium/Hard)에서 추론한다.
    /// </summary>
    public static class GridChunkDatabaseBuilder
    {
        private const string ScanRoot = "Assets/Prefabs/Map/ChunkMap";
        private const string DatabasePath = "Assets/Prefabs/@Data/Map/GridChunkDatabase.asset";

        [MenuItem("Map/Rebuild Grid Chunk Database")]
        public static void Rebuild()
        {
            GridChunkDatabase db = AssetDatabase.LoadAssetAtPath<GridChunkDatabase>(DatabasePath);
            if (db == null)
            {
                string dir = "Assets/Prefabs/@Data/Map";
                if (!AssetDatabase.IsValidFolder(dir))
                    AssetDatabase.CreateFolder("Assets/Prefabs/@Data", "Map");
                db = ScriptableObject.CreateInstance<GridChunkDatabase>();
                AssetDatabase.CreateAsset(db, DatabasePath);
            }

            db.entries.Clear();

            string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { ScanRoot });

            int found = 0;
            int skipped = 0;

            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null) { skipped++; continue; }

                GridChunkData cd = prefab.GetComponent<GridChunkData>();
                if (cd == null) { skipped++; continue; }

                GridChunkType type = cd.ChunkType;
                List<Vector2Int> entrances = cd.GetAllEntrances();
                GridEndLineRole role = cd.EndLineRole;
                Vector2Int footprint = cd.Footprint;

                GridChunkDifficulty diff = GridChunkDifficulty.None;
                if (path.Contains("/Easy/")) diff = GridChunkDifficulty.Easy;
                else if (path.Contains("/Medium/")) diff = GridChunkDifficulty.Medium;
                else if (path.Contains("/Hard/")) diff = GridChunkDifficulty.Hard;

                int width = footprint.x * GridChunkData.CellSize;
                int height = footprint.y * GridChunkData.CellSize;

                GridChunkDatabaseEntry entry = new GridChunkDatabaseEntry
                {
                    prefab = prefab,
                    prefabName = prefab.name,
                    type = type,
                    difficulty = diff,
                    entrances = entrances,
                    endLineRole = role,
                    width = width,
                    height = height,
                    footprint = footprint,
                };
                db.entries.Add(entry);
                found++;
            }

            EditorUtility.SetDirty(db);
            AssetDatabase.SaveAssets();
            Debug.Log($"[GridChunkDatabaseBuilder] Rebuilt: {found} chunk(s) registered, {skipped} prefab(s) skipped (no GridChunkData). Saved to {DatabasePath}");
        }
    }
}
