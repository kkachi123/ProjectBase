using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace MapSystem.Editor
{
    /// <summary>
    /// Assets/Prefabs/Map/ChunkMap/ 아래 프리팹을 전부 스캔해서 ChunkDatabase를 자동으로
    /// 재생성한다. 난이도는 ChunkData에 필드를 추가하는 대신, 프리팹 경로의 폴더명
    /// (Easy/Medium/Hard)에서 추론한다. entrance1/entrance2는 ChunkData의 private
    /// 필드를 리플렉션으로 읽어와 캐싱한다.
    /// </summary>
    public static class ChunkDatabaseBuilder
    {
        private const string ScanRoot = "Assets/Prefabs/Map/ChunkMap";
        private const string DatabasePath = "Assets/Data/ChunkDatabase.asset";

        [MenuItem("Map/Rebuild Chunk Database")]
        public static void Rebuild()
        {
            ChunkDatabase db = AssetDatabase.LoadAssetAtPath<ChunkDatabase>(DatabasePath);
            if (db == null)
            {
                string dir = "Assets/Data";
                if (!AssetDatabase.IsValidFolder(dir))
                    AssetDatabase.CreateFolder("Assets", "Data");
                db = ScriptableObject.CreateInstance<ChunkDatabase>();
                AssetDatabase.CreateAsset(db, DatabasePath);
            }

            db.entries.Clear();

            string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { ScanRoot });
            System.Type cdType = typeof(ChunkData);
            BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Instance;
            FieldInfo f_type = cdType.GetField("chunkType", flags);
            FieldInfo f_e1 = cdType.GetField("entrance1", flags);
            FieldInfo f_e2 = cdType.GetField("entrance2", flags);

            int found = 0;
            int skipped = 0;

            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null) { skipped++; continue; }

                ChunkData cd = prefab.GetComponent<ChunkData>();
                if (cd == null) { skipped++; continue; }

                ChunkType type = (ChunkType)f_type.GetValue(cd);
                Vector2Int e1 = (Vector2Int)f_e1.GetValue(cd);
                Vector2Int e2 = (Vector2Int)f_e2.GetValue(cd);

                ChunkDifficulty diff = ChunkDifficulty.None;
                if (path.Contains("/Easy/")) diff = ChunkDifficulty.Easy;
                else if (path.Contains("/Medium/")) diff = ChunkDifficulty.Medium;
                else if (path.Contains("/Hard/")) diff = ChunkDifficulty.Hard;

                int width = (type == ChunkType.Transition) ? 10 : 20;
                int height = 10;

                ChunkDatabaseEntry entry = new ChunkDatabaseEntry
                {
                    prefab = prefab,
                    prefabName = prefab.name,
                    type = type,
                    difficulty = diff,
                    entrance1 = e1,
                    entrance2 = e2,
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
